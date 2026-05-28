using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AILevelGenerator.Runtime
{
    using Core;
    using Data;
    using Utils;
    using Generation;
    
    /// <summary>
    /// Runtime level loader that handles level loading, instantiation, and cleanup.
    /// Supports async loading with progress, level caching, and streaming.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        #region Singleton
        
        private static LevelLoader _instance;
        public static LevelLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[LevelLoader]");
                    _instance = go.AddComponent<LevelLoader>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Loading Settings")]
        [SerializeField] private bool _useAsyncInstantiation = true;
        [SerializeField] private int _roomsPerFrame = 5;
        [SerializeField] private float _loadingScreenMinDuration = 1f;
        
        [Header("Level Container")]
        [SerializeField] private Transform _levelContainer;
        
        [Header("Streaming")]
        [SerializeField] private bool _enableStreaming = false;
        [SerializeField] private float _streamingRadius = 50f;
        [SerializeField] private float _streamingUpdateInterval = 0.5f;
        
        #endregion
        
        #region Fields
        
        private LevelData _currentLevel;
        private GameObject _currentLevelObject;
        private Dictionary<string, GameObject> _roomInstances = new Dictionary<string, GameObject>();
        private HashSet<string> _loadedRooms = new HashSet<string>();
        private Dictionary<string, LevelData> _levelCache = new Dictionary<string, LevelData>();
        private CancellationTokenSource _loadingCts;
        private float _lastStreamingUpdate;
        private Transform _playerTransform;
        
        // Dependencies
        private AILevelGeneratorFacade _facade;
        private ProceduralRoomGenerator _proceduralGenerator;
        private CorridorGenerator _corridorGenerator;
        private NavMeshIntegration _navMesh;
        
        #endregion
        
        #region Properties
        
        public LevelData CurrentLevel => _currentLevel;
        public bool IsLoading { get; private set; }
        public float LoadingProgress { get; private set; }
        public int LoadedRoomCount => _loadedRooms.Count;
        
        #endregion
        
        #region Events
        
        public event Action<float> OnLoadingProgress;
        public event Action<LevelData> OnLevelLoaded;
        public event Action OnLevelUnloaded;
        public event Action<RoomData> OnRoomStreamed;
        public event Action<RoomData> OnRoomUnstreamed;
        public event Action<string> OnLoadError;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _proceduralGenerator = new ProceduralRoomGenerator();
            _corridorGenerator = new CorridorGenerator();
        }
        
        private void Update()
        {
            if (_enableStreaming && _currentLevel != null && _playerTransform != null)
            {
                if (Time.time - _lastStreamingUpdate > _streamingUpdateInterval)
                {
                    _lastStreamingUpdate = Time.time;
                    UpdateStreaming();
                }
            }
        }
        
        private void OnDestroy()
        {
            _loadingCts?.Cancel();
            if (_instance == this) _instance = null;
        }
        
        #endregion
        
        #region Public Methods - Loading
        
        /// <summary>
        /// Loads a level from file.
        /// </summary>
        public async UniTask<LevelData> LoadFromFileAsync(string filePath, CancellationToken token = default)
        {
            if (IsLoading)
            {
                OnLoadError?.Invoke("Already loading a level");
                return null;
            }
            
            IsLoading = true;
            LoadingProgress = 0f;
            
            try
            {
                DebugLogger.LogCore($"Loading level from: {filePath}");
                
                // Load data
                var level = LevelSerializer.LoadFromFile(filePath);
                LoadingProgress = 0.2f;
                OnLoadingProgress?.Invoke(LoadingProgress);
                
                // Instantiate
                await InstantiateLevelAsync(level, token);
                
                return level;
            }
            catch (Exception ex)
            {
                OnLoadError?.Invoke(ex.Message);
                DebugLogger.LogError($"Failed to load level: {ex.Message}");
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Loads a pregenerated level.
        /// </summary>
        public async UniTask LoadLevelAsync(LevelData level, CancellationToken token = default)
        {
            if (level == null)
            {
                OnLoadError?.Invoke("Level data is null");
                return;
            }
            
            if (IsLoading)
            {
                OnLoadError?.Invoke("Already loading a level");
                return;
            }
            
            IsLoading = true;
            LoadingProgress = 0f;
            
            _loadingCts?.Cancel();
            _loadingCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            
            try
            {
                await InstantiateLevelAsync(level, _loadingCts.Token);
            }
            catch (OperationCanceledException)
            {
                DebugLogger.LogWarning("Level loading cancelled");
            }
            catch (Exception ex)
            {
                OnLoadError?.Invoke(ex.Message);
                DebugLogger.LogError($"Failed to load level: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Generates and loads a new level.
        /// </summary>
        public async UniTask<LevelData> GenerateAndLoadAsync(string command, CancellationToken token = default)
        {
            _facade = AILevelGeneratorFacade.Instance;
            if (_facade == null)
            {
                OnLoadError?.Invoke("AILevelGeneratorFacade not found");
                return null;
            }
            
            IsLoading = true;
            LoadingProgress = 0f;
            
            try
            {
                // Generate
                DebugLogger.LogCore($"Generating level: {command}");
                var level = await _facade.GenerateAsync(command);
                LoadingProgress = 0.3f;
                OnLoadingProgress?.Invoke(LoadingProgress);
                
                // Instantiate
                await InstantiateLevelAsync(level, token);
                
                return level;
            }
            catch (Exception ex)
            {
                OnLoadError?.Invoke(ex.Message);
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Unloads the current level.
        /// </summary>
        public void UnloadCurrentLevel()
        {
            if (_currentLevelObject != null)
            {
                Destroy(_currentLevelObject);
                _currentLevelObject = null;
            }
            
            _roomInstances.Clear();
            _loadedRooms.Clear();
            _currentLevel = null;
            
            OnLevelUnloaded?.Invoke();
            DebugLogger.LogCore("Level unloaded");
        }
        
        /// <summary>
        /// Caches level data for quick loading.
        /// </summary>
        public void CacheLevel(string key, LevelData level)
        {
            _levelCache[key] = level;
        }
        
        /// <summary>
        /// Gets cached level.
        /// </summary>
        public LevelData GetCachedLevel(string key)
        {
            return _levelCache.TryGetValue(key, out var level) ? level : null;
        }
        
        /// <summary>
        /// Clears level cache.
        /// </summary>
        public void ClearCache()
        {
            _levelCache.Clear();
        }
        
        /// <summary>
        /// Sets player transform for streaming.
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
        }
        
        #endregion
        
        #region Public Methods - Room Access
        
        /// <summary>
        /// Gets room instance by ID.
        /// </summary>
        public GameObject GetRoomInstance(string roomId)
        {
            return _roomInstances.TryGetValue(roomId, out var room) ? room : null;
        }
        
        /// <summary>
        /// Checks if a room is currently loaded.
        /// </summary>
        public bool IsRoomLoaded(string roomId)
        {
            return _loadedRooms.Contains(roomId);
        }
        
        /// <summary>
        /// Gets the room at a world position.
        /// </summary>
        public RoomData GetRoomAtPosition(Vector3 worldPos)
        {
            if (_currentLevel == null) return null;
            
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            
            foreach (var room in _currentLevel.Rooms)
            {
                if (Vector3.Distance(room.WorldPosition, worldPos) < cellSize)
                {
                    return room;
                }
            }
            
            return null;
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask InstantiateLevelAsync(LevelData level, CancellationToken token)
        {
            // Unload existing
            UnloadCurrentLevel();
            
            _currentLevel = level;
            
            // Create container
            _currentLevelObject = new GameObject($"Level_{level.LevelId}");
            if (_levelContainer != null)
            {
                _currentLevelObject.transform.SetParent(_levelContainer);
            }
            
            var roomsParent = new GameObject("Rooms").transform;
            roomsParent.SetParent(_currentLevelObject.transform);
            
            var corridorsParent = new GameObject("Corridors").transform;
            corridorsParent.SetParent(_currentLevelObject.transform);
            
            // Prepare template provider
            _facade = _facade ?? FindObjectOfType<AILevelGeneratorFacade>();
            
            // Instantiate rooms
            int totalRooms = level.Rooms.Count;
            int processedRooms = 0;
            
            foreach (var room in level.Rooms)
            {
                token.ThrowIfCancellationRequested();
                
                var roomObject = InstantiateRoom(room, roomsParent);
                if (roomObject != null)
                {
                    _roomInstances[room.Id] = roomObject;
                    _loadedRooms.Add(room.Id);
                }
                
                processedRooms++;
                LoadingProgress = 0.3f + (0.5f * processedRooms / totalRooms);
                OnLoadingProgress?.Invoke(LoadingProgress);
                
                if (_useAsyncInstantiation && processedRooms % _roomsPerFrame == 0)
                {
                    await UniTask.Yield(token);
                }
            }
            
            // Generate corridors
            LoadingProgress = 0.8f;
            OnLoadingProgress?.Invoke(LoadingProgress);
            
            var corridors = _corridorGenerator.GenerateAllCorridors(level);
            foreach (var corridor in corridors)
            {
                var corridorObj = _corridorGenerator.CreateCorridorVisual(corridor);
                if (corridorObj != null)
                {
                    corridorObj.transform.SetParent(corridorsParent);
                }
            }
            
            // Setup NavMesh
            LoadingProgress = 0.9f;
            OnLoadingProgress?.Invoke(LoadingProgress);
            
            _navMesh = _currentLevelObject.AddComponent<NavMeshIntegration>();
            _navMesh.Initialize(level, _currentLevelObject);
            
            // Complete
            LoadingProgress = 1f;
            OnLoadingProgress?.Invoke(LoadingProgress);
            
            // Wait minimum duration if set
            if (_loadingScreenMinDuration > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_loadingScreenMinDuration), cancellationToken: token);
            }
            
            OnLevelLoaded?.Invoke(level);
            DebugLogger.LogCore($"Level loaded: {level.Rooms.Count} rooms, {corridors.Count} corridors");
        }
        
        private GameObject InstantiateRoom(RoomData room, Transform parent)
        {
            GameObject roomObject = null;
            
            // Try to get prefab from template
            if (_facade != null)
            {
                roomObject = _facade.InstantiateLevel(new LevelData { Rooms = new List<RoomData> { room } }, parent);
                if (roomObject != null && roomObject.transform.childCount > 0)
                {
                    // Get the actual room child
                    var child = roomObject.transform.GetChild(0);
                    child.SetParent(parent);
                    Destroy(roomObject);
                    return child.gameObject;
                }
            }
            
            // Fall back to procedural generation
            var cellSize = GenerationSettingsData.Instance.GridCellSize;
            roomObject = _proceduralGenerator.GenerateRoom(room, cellSize);
            roomObject.transform.SetParent(parent);
            
            // Apply room type visual
            LevelAnimator.ApplyRoomTypeVisuals(roomObject, room.Type);
            
            return roomObject;
        }
        
        private void UpdateStreaming()
        {
            if (_currentLevel == null || _playerTransform == null) return;
            
            Vector3 playerPos = _playerTransform.position;
            
            foreach (var room in _currentLevel.Rooms)
            {
                float distance = Vector3.Distance(playerPos, room.WorldPosition);
                bool shouldBeLoaded = distance <= _streamingRadius;
                bool isLoaded = _loadedRooms.Contains(room.Id);
                
                if (shouldBeLoaded && !isLoaded)
                {
                    // Stream in
                    StreamInRoom(room);
                }
                else if (!shouldBeLoaded && isLoaded)
                {
                    // Stream out
                    StreamOutRoom(room);
                }
            }
        }
        
        private void StreamInRoom(RoomData room)
        {
            if (_roomInstances.TryGetValue(room.Id, out var roomObject))
            {
                roomObject.SetActive(true);
            }
            else
            {
                var parent = _currentLevelObject.transform.Find("Rooms");
                var newRoom = InstantiateRoom(room, parent);
                if (newRoom != null)
                {
                    _roomInstances[room.Id] = newRoom;
                    
                    // Animate in
                    LevelAnimator.AnimateRoom(newRoom);
                }
            }
            
            _loadedRooms.Add(room.Id);
            OnRoomStreamed?.Invoke(room);
        }
        
        private void StreamOutRoom(RoomData room)
        {
            if (_roomInstances.TryGetValue(room.Id, out var roomObject))
            {
                roomObject.SetActive(false);
            }
            
            _loadedRooms.Remove(room.Id);
            OnRoomUnstreamed?.Invoke(room);
        }
        
        #endregion
    }
}
