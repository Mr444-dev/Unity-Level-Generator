using UnityEngine;
using Cysharp.Threading.Tasks;

namespace AILevelGenerator.Examples
{
    using Core;
    using Data;
    
    /// <summary>
    /// Simple test script for generating levels.
    /// Add to an empty GameObject and press Play.
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Generation Command")]
        [TextArea(2, 4)]
        [SerializeField] private string _command = "Create dark dungeon with 10 rooms, medium difficulty";
        
        [Header("Settings")]
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _createTestPlayer = true;
        
        [Header("Debug")]
        [SerializeField] private bool _logDetails = true;
        
        private LevelData _currentLevel;
        private GameObject _levelInstance;
        private GameObject _testPlayer;
        
        async void Start()
        {
            if (_generateOnStart)
            {
                await GenerateAndInstantiate();
            }
        }
        
        /// <summary>
        /// Generates and instantiates a level.
        /// </summary>
        public async UniTask GenerateAndInstantiate()
        {
            Log("=== AI Level Generator Test ===");
            Log($"Command: {_command}");
            
            // Get facade
            var facade = AILevelGeneratorFacade.Instance;
            if (facade == null)
            {
                LogError("AILevelGeneratorFacade not found!");
                return;
            }
            
            // Subscribe to events
            facade.OnProgressChanged += OnProgress;
            facade.OnGenerationComplete += OnComplete;
            facade.OnGenerationFailed += OnFailed;
            
            try
            {
                Log("Starting generation...");
                
                // Generate
                _currentLevel = await facade.GenerateAsync(_command);
                
                if (_currentLevel != null)
                {
                    LogLevelDetails();
                    
                    // Remove old level
                    if (_levelInstance != null)
                    {
                        Destroy(_levelInstance);
                    }
                    
                    // Instantiate
                    _levelInstance = facade.InstantiateLevel(_currentLevel);
                    Log($"Level instantiated in scene: {_levelInstance.name}");
                    
                    // Create test player
                    if (_createTestPlayer)
                    {
                        CreateTestPlayer();
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Error: {ex.Message}");
            }
            finally
            {
                facade.OnProgressChanged -= OnProgress;
                facade.OnGenerationComplete -= OnComplete;
                facade.OnGenerationFailed -= OnFailed;
            }
        }
        
        private void CreateTestPlayer()
        {
            if (_testPlayer != null)
            {
                Destroy(_testPlayer);
            }
            
            // Start position
            Vector3 spawnPos = Vector3.up * 2f;
            if (_currentLevel?.EntranceRoom != null)
            {
                spawnPos = _currentLevel.EntranceRoom.WorldPosition + Vector3.up * 2f;
            }
            
            // Create a simple player
            _testPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _testPlayer.name = "TestPlayer";
            _testPlayer.transform.position = spawnPos;
            _testPlayer.GetComponent<Renderer>().material.color = Color.blue;
            
            // Add CharacterController
            var cc = _testPlayer.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.center = new Vector3(0, 1f, 0);
            
            // Add simple movement
            _testPlayer.AddComponent<SimpleMovement>();
            
            Log($"Test player created at position: {spawnPos}");
        }
        
        private void LogLevelDetails()
        {
            if (!_logDetails || _currentLevel == null) return;
            
            Log("--- Level Details ---");
            Log($"ID: {_currentLevel.LevelId}");
            Log($"Theme: {_currentLevel.Theme}");
            Log($"Difficulty: {_currentLevel.BaseDifficulty}");
            Log($"Room count: {_currentLevel.Rooms.Count}");
            Log($"Entrance: {_currentLevel.EntranceRoom?.Id ?? "none"}");
            Log($"Exit: {_currentLevel.ExitRoom?.Id ?? "none"}");
            Log($"Boss rooms: {_currentLevel.BossRooms.Count}");
            
            // Count room types
            int normal = 0, treasure = 0, secret = 0;
            foreach (var room in _currentLevel.Rooms)
            {
                if (room.Type == RoomType.Normal) normal++;
                else if (room.Type == RoomType.Treasure) treasure++;
                else if (room.Type == RoomType.Secret) secret++;
            }
            Log($"Types: Normal={normal}, Treasure={treasure}, Secret={secret}");
        }
        
        private void OnProgress(float progress)
        {
            Log($"Progress: {progress:P0}");
        }
        
        private void OnComplete(LevelData level)
        {
            Log($"Generation complete! Rooms: {level.Rooms.Count}");
        }
        
        private void OnFailed(string error)
        {
            LogError($"Generation failed: {error}");
        }
        
        private void Log(string message)
        {
            Debug.Log($"[LevelGen] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[LevelGen] {message}");
        }
        
        // Context menu for testing in editor
        [ContextMenu("Generate Now")]
        private void GenerateNow()
        {
            GenerateAndInstantiate().Forget();
        }
    }
    
    /// <summary>
    /// Simple movement controller for testing.
    /// </summary>
    public class SimpleMovement : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _rotSpeed = 120f;
        
        private CharacterController _cc;
        
        void Start()
        {
            _cc = GetComponent<CharacterController>();
        }
        
        void Update()
        {
            if (_cc == null) return;
            
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            // Rotate
            transform.Rotate(0, h * _rotSpeed * Time.deltaTime, 0);
            
            // Move
            Vector3 move = transform.forward * v * _speed;
            move += Physics.gravity;
            
            _cc.Move(move * Time.deltaTime);
        }
    }
}
