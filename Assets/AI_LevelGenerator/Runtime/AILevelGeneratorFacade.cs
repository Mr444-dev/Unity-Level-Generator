using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AILevelGenerator
{
    using Core;
    using Data;
    using AI;
    using Generation;
    
    /// <summary>
    /// Main facade class for the AI Level Generator system.
    /// Provides a simple API for generating levels from natural language commands.
    /// </summary>
    public class AILevelGeneratorFacade : MonoBehaviour
    {
        #region Singleton
        
        private static AILevelGeneratorFacade _instance;
        
        /// <summary>
        /// Singleton instance. Creates if not exists.
        /// </summary>
        public static AILevelGeneratorFacade Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AILevelGeneratorFacade>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[AI Level Generator]");
                        _instance = go.AddComponent<AILevelGeneratorFacade>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Settings")]
        [SerializeField] private GenerationSettingsData _settings;
        
        [Header("Templates")]
        [SerializeField] private List<RoomTemplateData> _roomTemplates = new List<RoomTemplateData>();
        
        [Header("Themes")]
        [SerializeField] private List<ThemeDefinitionData> _themeDefinitions = new List<ThemeDefinitionData>();
        
        #endregion
        
        #region Private Fields
        
        private ILevelGenerator _generator;
        private ICommandParser _commandParser;
        private ILearningSystem _learningSystem;
        private RoomTemplateProvider _templateProvider;
        private LevelValidator _levelValidator;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the current generation settings.
        /// </summary>
        public GenerationSettingsData Settings => _settings ?? GenerationSettingsData.Instance;
        
        /// <summary>
        /// Gets the command parser.
        /// </summary>
        public ICommandParser CommandParser => _commandParser;
        
        /// <summary>
        /// Gets the learning system.
        /// </summary>
        public ILearningSystem LearningSystem => _learningSystem;
        
        /// <summary>
        /// Indicates if generation is in progress.
        /// </summary>
        public bool IsGenerating => _generator?.IsGenerating ?? false;
        
        /// <summary>
        /// Current generation progress (0-1).
        /// </summary>
        public float Progress => _generator?.Progress ?? 0f;
        
        /// <summary>
        /// The last generated level.
        /// </summary>
        public LevelData LastGeneratedLevel { get; private set; }
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when generation progress updates.</summary>
        public event Action<float> OnProgressChanged;
        
        /// <summary>Fired when generation completes successfully.</summary>
        public event Action<LevelData> OnGenerationComplete;
        
        /// <summary>Fired when generation fails.</summary>
        public event Action<string> OnGenerationFailed;
        
        /// <summary>Fired after level is instantiated in scene.</summary>
        public event Action<GameObject> OnLevelInstantiated;
        
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
            
            Initialize();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Cleanup
            _learningSystem?.SavePreferences();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes all subsystems.
        /// </summary>
        public void Initialize()
        {
            DebugLogger.LogCore("Initializing AI Level Generator...");
            
            // Apply settings
            if (_settings != null)
            {
                _settings.ApplyDebugSettings();
            }
            
            // Initialize subsystems
            _commandParser = new CommandParser();
            _learningSystem = new LearningSystem();
            _templateProvider = new RoomTemplateProvider(_roomTemplates);
            _levelValidator = new LevelValidator();
            
            // Initialize default generator (WFC)
            var wfcGenerator = new WaveFunctionCollapseGenerator();
            wfcGenerator.TemplateProvider = _templateProvider;
            wfcGenerator.Validator = _levelValidator;
            wfcGenerator.OnProgressChanged += progress => OnProgressChanged?.Invoke(progress);
            wfcGenerator.OnGenerationComplete += level =>
            {
                LastGeneratedLevel = level;
                OnGenerationComplete?.Invoke(level);
            };
            wfcGenerator.OnGenerationFailed += error => OnGenerationFailed?.Invoke(error);
            _generator = wfcGenerator;
            
            DebugLogger.LogCore($"AI Level Generator initialized with {_roomTemplates.Count} templates");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Generates a level from a natural language command.
        /// </summary>
        /// <param name="command">Natural language command (e.g., "Create a dark dungeon with 10 rooms")</param>
        /// <returns>Generated level data</returns>
        public async UniTask<LevelData> GenerateAsync(string command)
        {
            DebugLogger.LogAI($"Received command: \"{command}\"");
            
            // Parse command
            var intent = _commandParser.Parse(command);
            if (!intent.IsValid)
            {
                var error = $"Could not parse command: {command}";
                OnGenerationFailed?.Invoke(error);
                throw new ArgumentException(error);
            }
            
            // Build profile from intent
            var profile = GenerationProfile.FromIntent(intent);
            
            // Generate
            return await GenerateAsync(profile);
        }
        
        /// <summary>
        /// Generates a level using explicit generation profile.
        /// </summary>
        public async UniTask<LevelData> GenerateAsync(GenerationProfile profile)
        {
            if (profile == null)
            {
                profile = Settings.CreateDefaultProfile();
            }
            
            DebugLogger.LogGeneration($"Starting generation: {profile.RoomCount} rooms, {profile.Theme} theme");
            
            try
            {
                var level = await _generator.GenerateAsync(profile);
                LastGeneratedLevel = level;
                return level;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "GenerateAsync");
                throw;
            }
        }
        
        /// <summary>
        /// Cancels current generation.
        /// </summary>
        public void CancelGeneration()
        {
            _generator?.CancelGeneration();
        }
        
        /// <summary>
        /// Instantiates the level in the scene.
        /// </summary>
        public GameObject InstantiateLevel(LevelData level, Transform parent = null)
        {
            if (level == null || level.Rooms.Count == 0)
            {
                DebugLogger.LogWarning("Cannot instantiate empty level");
                return null;
            }
            
            var levelRoot = new GameObject($"Level_{level.LevelId}");
            if (parent != null) levelRoot.transform.SetParent(parent);
            
            foreach (var room in level.Rooms)
            {
                var template = _templateProvider.GetTemplate(room.TemplateId);
                if (template?.Prefab != null)
                {
                    var roomInstance = Instantiate(
                        template.Prefab,
                        room.WorldPosition,
                        room.Rotation,
                        levelRoot.transform
                    );
                    roomInstance.name = $"Room_{room.Id}_{room.Type}";
                }
                else
                {
                    // Create placeholder
                    var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    placeholder.transform.position = room.WorldPosition;
                    placeholder.transform.localScale = Vector3.one * Settings.GridCellSize * 0.9f;
                    placeholder.transform.SetParent(levelRoot.transform);
                    placeholder.name = $"Placeholder_{room.Id}";
                    
                    // Color based on room type
                    var renderer = placeholder.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = GetRoomTypeColor(room.Type);
                    }
                }
            }
            
            OnLevelInstantiated?.Invoke(levelRoot);
            DebugLogger.LogCore($"Level instantiated with {level.Rooms.Count} rooms");
            
            return levelRoot;
        }
        
        /// <summary>
        /// Records user feedback for learning.
        /// </summary>
        public void RecordFeedback(LevelData level, float rating, string comments = null)
        {
            _learningSystem?.RecordFeedback(level, rating, comments);
        }
        
        /// <summary>
        /// Gets suggestions for partial input.
        /// </summary>
        public IReadOnlyList<string> GetSuggestions(string partialInput)
        {
            return _commandParser?.GetSuggestions(partialInput) ?? new List<string>();
        }
        
        /// <summary>
        /// Adds a room template at runtime.
        /// </summary>
        public void AddTemplate(RoomTemplateData template)
        {
            if (template != null && !_roomTemplates.Contains(template))
            {
                _roomTemplates.Add(template);
                _templateProvider = new RoomTemplateProvider(_roomTemplates);
                if (_generator is WaveFunctionCollapseGenerator wfc)
                {
                    wfc.TemplateProvider = _templateProvider;
                }
            }
        }
        
        /// <summary>
        /// Adds a theme definition at runtime.
        /// </summary>
        public void AddTheme(ThemeDefinitionData theme)
        {
            if (theme != null && !_themeDefinitions.Contains(theme))
            {
                _themeDefinitions.Add(theme);
            }
        }
        
        /// <summary>
        /// Gets theme definition by type.
        /// </summary>
        public ThemeDefinitionData GetTheme(ThemeType themeType)
        {
            return _themeDefinitions.Find(t => t.ThemeType == themeType);
        }
        
        #endregion
        
        #region Private Methods
        
        private Color GetRoomTypeColor(RoomType type)
        {
            return type switch
            {
                RoomType.Entrance => Color.green,
                RoomType.Exit => Color.blue,
                RoomType.Boss => Color.red,
                RoomType.Treasure => Color.yellow,
                RoomType.Secret => Color.magenta,
                RoomType.Shop => Color.cyan,
                _ => Color.gray
            };
        }
        
        #endregion
    }
    
    #region Room Template Provider
    
    /// <summary>
    /// Provides room templates for generation.
    /// </summary>
    public class RoomTemplateProvider : IRoomTemplateProvider
    {
        private readonly List<RoomTemplateData> _templates;
        private readonly Dictionary<string, RoomTemplateData> _templateLookup;
        
        public RoomTemplateProvider(List<RoomTemplateData> templates)
        {
            _templates = templates ?? new List<RoomTemplateData>();
            _templateLookup = new Dictionary<string, RoomTemplateData>();
            
            foreach (var t in _templates)
            {
                if (t != null && !string.IsNullOrEmpty(t.Id))
                {
                    _templateLookup[t.Id] = t;
                }
            }
        }
        
        public IReadOnlyList<IRoomTemplate> GetAllTemplates()
        {
            return _templates.ConvertAll<IRoomTemplate>(t => t);
        }
        
        public IReadOnlyList<IRoomTemplate> GetTemplates(RoomType type, ThemeType theme)
        {
            var result = new List<IRoomTemplate>();
            foreach (var t in _templates)
            {
                if (t != null && (t.Type == type || type == RoomType.Normal) && t.Theme == theme)
                {
                    result.Add(t);
                }
            }
            
            // If no theme match, return all matching types
            if (result.Count == 0)
            {
                foreach (var t in _templates)
                {
                    if (t != null && (t.Type == type || type == RoomType.Normal))
                    {
                        result.Add(t);
                    }
                }
            }
            
            // If still empty, return all templates
            if (result.Count == 0)
            {
                return GetAllTemplates();
            }
            
            return result;
        }
        
        public IRoomTemplate GetTemplate(string templateId)
        {
            return _templateLookup.TryGetValue(templateId, out var t) ? t : null;
        }
    }
    
    #endregion
    
    #region Level Validator
    
    /// <summary>
    /// Validates generated levels.
    /// </summary>
    public class LevelValidator : ILevelValidator
    {
        public ValidationResult Validate(LevelData level, GenerationProfile profile)
        {
            if (level == null)
            {
                return ValidationResult.Failure("Level is null");
            }
            
            var result = new ValidationResult { IsValid = true, QualityScore = 1f };
            
            // Check room count
            if (level.Rooms.Count < 3)
            {
                result.Errors.Add($"Too few rooms: {level.Rooms.Count}");
                result.IsValid = false;
            }
            
            // Check entrance/exit
            if (level.EntranceRoom == null)
            {
                result.Warnings.Add("No entrance room designated");
                result.QualityScore -= 0.2f;
            }
            
            if (level.ExitRoom == null)
            {
                result.Warnings.Add("No exit room designated");
                result.QualityScore -= 0.2f;
            }
            
            // Check connectivity
            if (!level.IsValid())
            {
                result.Errors.Add("Not all rooms are reachable");
                result.IsValid = false;
                result.QualityScore = 0f;
            }
            
            // Check room count vs target
            float roomCountRatio = (float)level.Rooms.Count / profile.RoomCount;
            if (roomCountRatio < 0.8f)
            {
                result.Warnings.Add($"Room count ({level.Rooms.Count}) is below target ({profile.RoomCount})");
                result.QualityScore -= 0.1f;
            }
            
            result.QualityScore = Mathf.Clamp01(result.QualityScore);
            return result;
        }
    }
    
    #endregion
}
