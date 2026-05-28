using UnityEngine;
using Cysharp.Threading.Tasks;

namespace AILevelGenerator.Examples
{
    using Core;
    using Data;
    using Runtime;
    
    /// <summary>
    /// Example scene controller demonstrating AI Level Generator usage.
    /// </summary>
    public class ExampleLevelController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Generator Settings")]
        [SerializeField] private string _defaultCommand = "Create a dark dungeon with 10 rooms, medium difficulty";
        [SerializeField] private bool _generateOnStart = true;
        
        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private Transform _playerSpawnPoint;
        
        [Header("UI")]
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UnityEngine.UI.Text _loadingText;
        [SerializeField] private UnityEngine.UI.Slider _loadingBar;
        
        [Header("Optional Components")]
        [SerializeField] private MinimapGenerator _minimapGenerator;
        [SerializeField] private EnemySpawner _enemySpawner;
        
        #endregion
        
        #region Fields
        
        private AILevelGeneratorFacade _facade;
        private LevelLoader _levelLoader;
        private DynamicDifficultySystem _difficultySystem;
        private PlayerAnalytics _analytics;
        private LevelData _currentLevel;
        private GameObject _player;
        
        #endregion
        
        #region Unity Lifecycle
        
        private async void Start()
        {
            // Initialize systems
            _facade = AILevelGeneratorFacade.Instance;
            _levelLoader = LevelLoader.Instance;
            _difficultySystem = DynamicDifficultySystem.Instance;
            _analytics = PlayerAnalytics.Instance;
            
            // Subscribe to events
            _levelLoader.OnLoadingProgress += OnLoadingProgress;
            _levelLoader.OnLevelLoaded += OnLevelLoaded;
            _levelLoader.OnLoadError += OnLoadError;
            
            if (_generateOnStart)
            {
                await GenerateLevel(_defaultCommand);
            }
        }
        
        private void OnDestroy()
        {
            if (_levelLoader != null)
            {
                _levelLoader.OnLoadingProgress -= OnLoadingProgress;
                _levelLoader.OnLevelLoaded -= OnLevelLoaded;
                _levelLoader.OnLoadError -= OnLoadError;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generates a new level from command.
        /// </summary>
        public async UniTask GenerateLevel(string command)
        {
            ShowLoading("Generating level...");
            
            _currentLevel = await _levelLoader.GenerateAndLoadAsync(command);
            
            HideLoading();
        }
        
        /// <summary>
        /// Regenerates the current level.
        /// </summary>
        public async UniTask RegenerateLevel()
        {
            if (_facade != null && _currentLevel != null)
            {
                var profile = new GenerationProfile
                {
                    RoomCount = _currentLevel.Rooms.Count,
                    Theme = _currentLevel.Theme,
                    TargetDifficulty = _currentLevel.BaseDifficulty,
                    Seed = UnityEngine.Random.Range(0, int.MaxValue)
                };
                
                ShowLoading("Regenerating...");
                
                var newLevel = await _facade.GenerateAsync(profile);
                await _levelLoader.LoadLevelAsync(newLevel);
                _currentLevel = newLevel;
                
                HideLoading();
            }
        }
        
        /// <summary>
        /// Increases difficulty and regenerates.
        /// </summary>
        public async UniTask IncreaseChallenge()
        {
            _difficultySystem?.SetDifficulty(_difficultySystem.CurrentDifficulty + 0.2f);
            await RegenerateLevel();
        }
        
        /// <summary>
        /// Unloads and returns to main menu.
        /// </summary>
        public void ReturnToMenu()
        {
            // Save analytics
            var report = _analytics?.EndSession();
            if (report != null)
            {
                Debug.Log($"Session report: {report.TotalPlayTime}s played, {report.DeathCount} deaths");
            }
            
            _levelLoader?.UnloadCurrentLevel();
            
            if (_player != null)
            {
                Destroy(_player);
            }
        }
        
        #endregion
        
        #region UI Methods
        
        /// <summary>
        /// Called from UI button.
        /// </summary>
        public void OnGenerateButtonClicked()
        {
            GenerateLevel(_defaultCommand).Forget();
        }
        
        /// <summary>
        /// Called from UI input field.
        /// </summary>
        public void OnCustomCommandSubmit(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                GenerateLevel(command).Forget();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLoadingProgress(float progress)
        {
            if (_loadingBar != null)
            {
                _loadingBar.value = progress;
            }
            
            if (_loadingText != null)
            {
                _loadingText.text = $"Loading... {progress:P0}";
            }
        }
        
        private void OnLevelLoaded(LevelData level)
        {
            _currentLevel = level;
            
            // Setup minimap
            if (_minimapGenerator != null)
            {
                _minimapGenerator.Initialize(level);
            }
            
            // Setup enemy spawner
            if (_enemySpawner != null)
            {
                _enemySpawner.Initialize(level);
            }
            
            // Spawn player at entrance
            SpawnPlayer();
            
            // Start analytics
            if (_player != null)
            {
                _analytics?.StartSession(level.LevelId, _player.transform);
            }
            
            DebugLogger.LogCore($"Level ready: {level.Name}");
        }
        
        private void OnLoadError(string error)
        {
            HideLoading();
            Debug.LogError($"Level loading error: {error}");
            
            if (_loadingText != null)
            {
                _loadingText.text = $"Error: {error}";
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void SpawnPlayer()
        {
            Vector3 spawnPos = Vector3.zero;
            
            // Use entrance position
            if (_currentLevel?.EntranceRoom != null)
            {
                spawnPos = _currentLevel.EntranceRoom.WorldPosition + Vector3.up;
            }
            else if (_playerSpawnPoint != null)
            {
                spawnPos = _playerSpawnPoint.position;
            }
            
            if (_player != null)
            {
                Destroy(_player);
            }
            
            if (_playerPrefab != null)
            {
                _player = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Create placeholder player
                _player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _player.name = "Player";
                _player.transform.position = spawnPos;
                _player.GetComponent<Renderer>().material.color = Color.blue;
                
                // Add simple movement
                _player.AddComponent<SimplePlayerMovement>();
            }
            
            // Setup level loader streaming
            _levelLoader?.SetPlayerTransform(_player.transform);
            
            // Setup minimap player tracking
            if (_minimapGenerator != null)
            {
                var mmField = _minimapGenerator.GetType().GetField("_playerTransform", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mmField?.SetValue(_minimapGenerator, _player.transform);
            }
        }
        
        private void ShowLoading(string message)
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
            }
            
            if (_loadingText != null)
            {
                _loadingText.text = message;
            }
            
            if (_loadingBar != null)
            {
                _loadingBar.value = 0;
            }
        }
        
        private void HideLoading()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Simple player movement for testing.
    /// </summary>
    public class SimplePlayerMovement : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 120f;
        
        private CharacterController _controller;
        
        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                _controller = gameObject.AddComponent<CharacterController>();
                _controller.height = 2f;
                _controller.center = new Vector3(0, 1f, 0);
            }
        }
        
        private void Update()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            // Rotate
            transform.Rotate(0, h * _rotationSpeed * Time.deltaTime, 0);
            
            // Move
            var move = transform.forward * v * _moveSpeed;
            move += Physics.gravity;
            
            _controller.Move(move * Time.deltaTime);
        }
    }
}
