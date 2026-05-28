using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AILevelGenerator.Core
{
    #region Enums
    
    /// <summary>
    /// Difficulty levels for generated content.
    /// </summary>
    public enum Difficulty
    {
        VeryEasy = 0,
        Easy = 1,
        Medium = 2,
        Hard = 3,
        VeryHard = 4,
        Nightmare = 5
    }
    
    /// <summary>
    /// Visual/thematic categories for level generation.
    /// </summary>
    public enum ThemeType
    {
        DarkDungeon,
        BrightTemple,
        Cave,
        Castle,
        Crypt,
        Forest,
        Industrial,
        Custom
    }
    
    /// <summary>
    /// Room connection directions.
    /// </summary>
    [Flags]
    public enum ConnectionDirection
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        Up = 1 << 4,
        Down = 1 << 5,
        All = North | East | South | West | Up | Down
    }
    
    /// <summary>
    /// Room functional types.
    /// </summary>
    public enum RoomType
    {
        Normal,
        Entrance,
        Exit,
        Boss,
        Treasure,
        Shop,
        Secret,
        Corridor,
        Hub
    }
    
    /// <summary>
    /// Generation algorithm types.
    /// </summary>
    public enum GenerationAlgorithm
    {
        WaveFunctionCollapse,
        ConstraintSatisfaction,
        GraphBased,
        Hybrid
    }
    
    #endregion
    
    #region Data Structures
    
    /// <summary>
    /// Represents a single room in the generated level.
    /// </summary>
    [Serializable]
    public class RoomData
    {
        public string Id;
        public string TemplateId;
        public RoomType Type;
        public Vector3Int GridPosition;
        public Vector3 WorldPosition;
        public Quaternion Rotation;
        public ConnectionDirection Connections;
        public List<string> ConnectedRoomIds = new List<string>();
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
        public int DifficultyModifier;
        public bool IsRequired;
        
        public RoomData()
        {
            Id = Guid.NewGuid().ToString("N")[..8];
        }
        
        public RoomData(string templateId, Vector3Int position) : this()
        {
            TemplateId = templateId;
            GridPosition = position;
        }
    }
    
    /// <summary>
    /// Complete level data structure containing all rooms and metadata.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public string LevelId;
        public string Name;
        public ThemeType Theme;
        public Difficulty BaseDifficulty;
        public Vector3Int Size;
        public List<RoomData> Rooms = new List<RoomData>();
        public RoomData EntranceRoom;
        public RoomData ExitRoom;
        public List<RoomData> BossRooms = new List<RoomData>();
        public long GeneratedTimestamp;
        public int Seed;
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
        
        public LevelData()
        {
            LevelId = Guid.NewGuid().ToString("N");
            GeneratedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Finds a room by its unique identifier.
        /// </summary>
        public RoomData FindRoom(string roomId)
        {
            return Rooms.Find(r => r.Id == roomId);
        }
        
        /// <summary>
        /// Gets all rooms of a specific type.
        /// </summary>
        public List<RoomData> GetRoomsByType(RoomType type)
        {
            return Rooms.FindAll(r => r.Type == type);
        }
        
        /// <summary>
        /// Validates the level has required rooms and connections.
        /// </summary>
        public bool IsValid()
        {
            return EntranceRoom != null && 
                   ExitRoom != null && 
                   Rooms.Count > 0 &&
                   AreAllRoomsReachable();
        }
        
        private bool AreAllRoomsReachable()
        {
            if (EntranceRoom == null || Rooms.Count == 0) return false;
            
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(EntranceRoom.Id);
            
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (visited.Contains(currentId)) continue;
                visited.Add(currentId);
                
                var room = FindRoom(currentId);
                if (room == null) continue;
                
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    if (!visited.Contains(connectedId))
                    {
                        queue.Enqueue(connectedId);
                    }
                }
            }
            
            return visited.Count == Rooms.Count;
        }
    }
    
    /// <summary>
    /// Parameters for level generation.
    /// </summary>
    [Serializable]
    public class GenerationProfile
    {
        [Header("Basic Settings")]
        public int RoomCount = 10;
        public Vector3Int LevelSize = new Vector3Int(10, 1, 10);
        public Difficulty TargetDifficulty = Difficulty.Medium;
        public ThemeType Theme = ThemeType.DarkDungeon;
        
        [Header("Room Distribution")]
        [Range(0f, 1f)] public float TreasureRoomChance = 0.1f;
        [Range(0f, 1f)] public float SecretRoomChance = 0.05f;
        public int BossRoomCount = 1;
        
        [Header("Generation")]
        public GenerationAlgorithm Algorithm = GenerationAlgorithm.WaveFunctionCollapse;
        public int Seed = -1; // -1 = random
        public int MaxAttempts = 100;
        
        [Header("Layout")]
        public bool EnsureLinearPath = true;
        public bool AllowDeadEnds = true;
        [Range(0f, 1f)] public float BranchingFactor = 0.3f;
        
        [Header("Advanced")]
        public float DifficultyProgression = 0.1f; // Per room increase
        public List<string> RequiredRoomTemplates = new List<string>();
        public List<string> ExcludedRoomTemplates = new List<string>();
        
        /// <summary>
        /// Creates a profile from a parsed natural language command.
        /// </summary>
        public static GenerationProfile FromIntent(ParsedIntent intent)
        {
            var profile = new GenerationProfile
            {
                RoomCount = intent.RoomCount ?? 10,
                TargetDifficulty = intent.Difficulty ?? Difficulty.Medium,
                Theme = intent.Theme ?? ThemeType.DarkDungeon
            };
            
            // Apply size modifiers
            if (intent.SizeModifier.HasValue)
            {
                var modifier = intent.SizeModifier.Value;
                profile.RoomCount = Mathf.RoundToInt(profile.RoomCount * modifier);
                profile.LevelSize = Vector3Int.RoundToInt(new Vector3(
                    profile.LevelSize.x * modifier,
                    profile.LevelSize.y,
                    profile.LevelSize.z * modifier
                ));
            }
            
            return profile;
        }
    }
    
    /// <summary>
    /// Result of natural language parsing.
    /// </summary>
    [Serializable]
    public class ParsedIntent
    {
        public string RawInput;
        public bool IsValid;
        public float Confidence;
        
        // Extracted parameters
        public int? RoomCount;
        public Difficulty? Difficulty;
        public ThemeType? Theme;
        public float? SizeModifier; // Small = 0.5, Normal = 1.0, Large = 1.5, Huge = 2.0
        public List<string> Keywords = new List<string>();
        public List<string> Warnings = new List<string>();
        
        public override string ToString()
        {
            return $"[Intent] Valid:{IsValid} Conf:{Confidence:P0} Rooms:{RoomCount} Diff:{Difficulty} Theme:{Theme}";
        }
    }
    
    #endregion
    
    #region Interfaces
    
    /// <summary>
    /// Core interface for level generation implementations.
    /// </summary>
    public interface ILevelGenerator
    {
        /// <summary>
        /// Generates a level using the provided profile.
        /// </summary>
        UniTask<LevelData> GenerateAsync(GenerationProfile profile);
        
        /// <summary>
        /// Validates a generated level.
        /// </summary>
        bool ValidateLevel(LevelData level);
        
        /// <summary>
        /// Cancels the current generation in progress.
        /// </summary>
        void CancelGeneration();
        
        /// <summary>
        /// Gets the current generation progress (0-1).
        /// </summary>
        float Progress { get; }
        
        /// <summary>
        /// Indicates if generation is currently in progress.
        /// </summary>
        bool IsGenerating { get; }
        
        /// <summary>
        /// Event fired when generation progress updates.
        /// </summary>
        event Action<float> OnProgressChanged;
        
        /// <summary>
        /// Event fired when generation completes.
        /// </summary>
        event Action<LevelData> OnGenerationComplete;
        
        /// <summary>
        /// Event fired when generation fails.
        /// </summary>
        event Action<string> OnGenerationFailed;
    }
    
    /// <summary>
    /// Interface for room template providers.
    /// </summary>
    public interface IRoomTemplateProvider
    {
        /// <summary>
        /// Gets all available room templates.
        /// </summary>
        IReadOnlyList<IRoomTemplate> GetAllTemplates();
        
        /// <summary>
        /// Gets templates matching the specified criteria.
        /// </summary>
        IReadOnlyList<IRoomTemplate> GetTemplates(RoomType type, ThemeType theme);
        
        /// <summary>
        /// Gets a specific template by ID.
        /// </summary>
        IRoomTemplate GetTemplate(string templateId);
    }
    
    /// <summary>
    /// Interface for room template definitions.
    /// </summary>
    public interface IRoomTemplate
    {
        string Id { get; }
        string DisplayName { get; }
        RoomType Type { get; }
        ThemeType Theme { get; }
        Vector3Int Size { get; }
        ConnectionDirection AvailableConnections { get; }
        GameObject Prefab { get; }
        int Weight { get; } // Higher = more likely to be selected
        bool IsCompatibleWith(IRoomTemplate other, ConnectionDirection direction);
    }
    
    /// <summary>
    /// Interface for natural language command parsing.
    /// </summary>
    public interface ICommandParser
    {
        /// <summary>
        /// Parses a natural language command into structured intent.
        /// </summary>
        ParsedIntent Parse(string input);
        
        /// <summary>
        /// Gets suggestions for partial input.
        /// </summary>
        IReadOnlyList<string> GetSuggestions(string partialInput);
    }
    
    /// <summary>
    /// Interface for the learning system.
    /// </summary>
    public interface ILearningSystem
    {
        /// <summary>
        /// Records user feedback on a generated level.
        /// </summary>
        void RecordFeedback(LevelData level, float rating, string comments = null);
        
        /// <summary>
        /// Gets adjusted weights based on user preferences.
        /// </summary>
        Dictionary<string, float> GetTemplateWeights();
        
        /// <summary>
        /// Saves learned preferences.
        /// </summary>
        void SavePreferences();
        
        /// <summary>
        /// Loads learned preferences.
        /// </summary>
        void LoadPreferences();
    }
    
    /// <summary>
    /// Interface for level validators.
    /// </summary>
    public interface ILevelValidator
    {
        /// <summary>
        /// Validates the level and returns detailed results.
        /// </summary>
        ValidationResult Validate(LevelData level, GenerationProfile profile);
    }
    
    /// <summary>
    /// Result of level validation.
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public bool IsValid;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public float QualityScore; // 0-1
        
        public static ValidationResult Success(float qualityScore = 1f)
        {
            return new ValidationResult { IsValid = true, QualityScore = qualityScore };
        }
        
        public static ValidationResult Failure(string error)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { error },
                QualityScore = 0f
            };
        }
    }
    
    #endregion
}
