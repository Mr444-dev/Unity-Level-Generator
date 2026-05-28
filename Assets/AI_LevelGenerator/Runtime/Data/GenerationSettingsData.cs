using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AILevelGenerator.Data
{
    using Core;
    
    /// <summary>
    /// Global settings for level generation system.
    /// Configure default parameters and performance options.
    /// </summary>
    [CreateAssetMenu(fileName = "GenerationSettings", menuName = "AI Level Generator/Generation Settings")]
    public class GenerationSettingsData : ScriptableObject
    {
        #region Singleton Access
        
        private static GenerationSettingsData _instance;
        
        /// <summary>
        /// Gets the default generation settings asset.
        /// </summary>
        public static GenerationSettingsData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GenerationSettingsData>("GenerationSettings");
                    if (_instance == null)
                    {
                        DebugLogger.LogWarning("GenerationSettings not found in Resources. Using defaults.");
                        _instance = CreateInstance<GenerationSettingsData>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Default Parameters
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Defaults")]
        [Range(3, 100)]
        #endif
        [SerializeField] private int _defaultRoomCount = 10;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Defaults")]
        #endif
        [SerializeField] private Vector3Int _defaultLevelSize = new Vector3Int(20, 1, 20);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Defaults")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private Difficulty _defaultDifficulty = Difficulty.Medium;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Defaults")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private ThemeType _defaultTheme = ThemeType.DarkDungeon;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Defaults")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private GenerationAlgorithm _defaultAlgorithm = GenerationAlgorithm.WaveFunctionCollapse;
        
        #endregion
        
        #region Performance Settings
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Performance")]
        [Range(10, 1000)]
        [Tooltip("Maximum generation attempts before failing")]
        #endif
        [SerializeField] private int _maxGenerationAttempts = 100;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Performance")]
        [Range(1, 100)]
        [Tooltip("Rooms to process per frame during async generation")]
        #endif
        [SerializeField] private int _roomsPerFrame = 10;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Performance")]
        [Tooltip("Use Unity Jobs for heavy computation")]
        #endif
        [SerializeField] private bool _useBurstCompilation = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Performance")]
        [Tooltip("Enable object pooling for instantiated rooms")]
        #endif
        [SerializeField] private bool _useObjectPooling = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Performance")]
        [Range(10, 500)]
        [ShowIf("_useObjectPooling")]
        [Tooltip("Initial pool size per room template")]
        #endif
        [SerializeField] private int _initialPoolSize = 20;
        
        #endregion
        
        #region Layout Settings
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Layout")]
        [Range(0f, 1f)]
        [Tooltip("How much levels tend to branch vs stay linear")]
        #endif
        [SerializeField] private float _defaultBranchingFactor = 0.3f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Layout")]
        [Tooltip("Allow dead-end rooms by default")]
        #endif
        [SerializeField] private bool _allowDeadEnds = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Layout")]
        [Tooltip("Ensure at least one clear path from entrance to exit")]
        #endif
        [SerializeField] private bool _ensureLinearPath = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Layout")]
        [Range(1f, 10f)]
        [Tooltip("Grid cell size in world units")]
        #endif
        [SerializeField] private float _gridCellSize = 5f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Layout")]
        [Range(1f, 5f)]
        [Tooltip("Corridor width in grid cells")]
        #endif
        [SerializeField] private float _corridorWidth = 1f;
        
        #endregion
        
        #region Difficulty Progression
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty")]
        [Range(0f, 0.5f)]
        [Tooltip("Difficulty increase per room from entrance")]
        #endif
        [SerializeField] private float _difficultyProgressionRate = 0.1f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Difficulty")]
        [Range(0f, 1f)]
        [Tooltip("Randomness in difficulty distribution")]
        #endif
        [SerializeField] private float _difficultyRandomness = 0.2f;
        
        #endregion
        
        #region Room Distribution
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Room Distribution")]
        [Range(0f, 0.5f)]
        #endif
        [SerializeField] private float _treasureRoomChance = 0.1f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Room Distribution")]
        [Range(0f, 0.2f)]
        #endif
        [SerializeField] private float _secretRoomChance = 0.05f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Room Distribution")]
        [Range(0f, 0.3f)]
        #endif
        [SerializeField] private float _shopRoomChance = 0.08f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Room Distribution")]
        [Range(1, 5)]
        #endif
        [SerializeField] private int _defaultBossRoomCount = 1;
        
        #endregion
        
        #region Debug Settings
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [Tooltip("Enable detailed logging")]
        #endif
        [SerializeField] private bool _enableDebugLogging = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [EnumToggleButtons]
        [ShowIf("_enableDebugLogging")]
        #endif
        [SerializeField] private DebugLogger.LogCategory _activeLogCategories = DebugLogger.LogCategory.All;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [Tooltip("Show 3D gizmos in scene view")]
        #endif
        [SerializeField] private bool _showDebugGizmos = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowIf("_showDebugGizmos")]
        #endif
        [SerializeField] private Color _roomGizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Debug")]
        [ShowIf("_showDebugGizmos")]
        #endif
        [SerializeField] private Color _connectionGizmoColor = new Color(0.8f, 0.8f, 0.2f, 0.5f);
        
        #endregion
        
        #region Public Properties
        
        // Defaults
        public int DefaultRoomCount => _defaultRoomCount;
        public Vector3Int DefaultLevelSize => _defaultLevelSize;
        public Difficulty DefaultDifficulty => _defaultDifficulty;
        public ThemeType DefaultTheme => _defaultTheme;
        public GenerationAlgorithm DefaultAlgorithm => _defaultAlgorithm;
        
        // Performance
        public int MaxGenerationAttempts => _maxGenerationAttempts;
        public int RoomsPerFrame => _roomsPerFrame;
        public bool UseBurstCompilation => _useBurstCompilation;
        public bool UseObjectPooling => _useObjectPooling;
        public int InitialPoolSize => _initialPoolSize;
        
        // Layout
        public float DefaultBranchingFactor => _defaultBranchingFactor;
        public bool AllowDeadEnds => _allowDeadEnds;
        public bool EnsureLinearPath => _ensureLinearPath;
        public float GridCellSize => _gridCellSize;
        public float CorridorWidth => _corridorWidth;
        
        // Difficulty
        public float DifficultyProgressionRate => _difficultyProgressionRate;
        public float DifficultyRandomness => _difficultyRandomness;
        
        // Room Distribution
        public float TreasureRoomChance => _treasureRoomChance;
        public float SecretRoomChance => _secretRoomChance;
        public float ShopRoomChance => _shopRoomChance;
        public int DefaultBossRoomCount => _defaultBossRoomCount;
        
        // Debug
        public bool EnableDebugLogging => _enableDebugLogging;
        public DebugLogger.LogCategory ActiveLogCategories => _activeLogCategories;
        public bool ShowDebugGizmos => _showDebugGizmos;
        public Color RoomGizmoColor => _roomGizmoColor;
        public Color ConnectionGizmoColor => _connectionGizmoColor;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Creates a GenerationProfile from these default settings.
        /// </summary>
        public GenerationProfile CreateDefaultProfile()
        {
            return new GenerationProfile
            {
                RoomCount = _defaultRoomCount,
                LevelSize = _defaultLevelSize,
                TargetDifficulty = _defaultDifficulty,
                Theme = _defaultTheme,
                Algorithm = _defaultAlgorithm,
                BranchingFactor = _defaultBranchingFactor,
                AllowDeadEnds = _allowDeadEnds,
                EnsureLinearPath = _ensureLinearPath,
                TreasureRoomChance = _treasureRoomChance,
                SecretRoomChance = _secretRoomChance,
                BossRoomCount = _defaultBossRoomCount,
                MaxAttempts = _maxGenerationAttempts,
                DifficultyProgression = _difficultyProgressionRate
            };
        }
        
        /// <summary>
        /// Applies debug settings to the logger.
        /// </summary>
        public void ApplyDebugSettings()
        {
            DebugLogger.IsEnabled = _enableDebugLogging;
            DebugLogger.ActiveCategories = _activeLogCategories;
        }
        
        #if ODIN_INSPECTOR
        [Button("Apply Settings Now"), FoldoutGroup("Debug")]
        #endif
        private void ApplyNow()
        {
            ApplyDebugSettings();
            DebugLogger.LogCore("Generation settings applied");
        }
        
        #endregion
    }
}
