using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AILevelGenerator.AI
{
    using Core;
    
    /// <summary>
    /// Learning system that tracks user preferences and adapts generation weights.
    /// Stores feedback data persistently to improve future generations.
    /// </summary>
    public class LearningSystem : ILearningSystem
    {
        #region Constants
        
        private const string PREFS_FILENAME = "ai_level_generator_prefs.json";
        private const float MIN_WEIGHT = 0.1f;
        private const float MAX_WEIGHT = 5.0f;
        private const float LEARNING_RATE = 0.1f;
        private const int MAX_HISTORY_SIZE = 100;
        
        #endregion
        
        #region Fields
        
        private UserPreferencesData _preferences;
        private string _savePath;
        
        #endregion
        
        #region Constructor
        
        public LearningSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, PREFS_FILENAME);
            LoadPreferences();
            DebugLogger.LogLearning($"LearningSystem initialized. Save path: {_savePath}");
        }
        
        public LearningSystem(string customSavePath)
        {
            _savePath = customSavePath;
            LoadPreferences();
        }
        
        #endregion
        
        #region ILearningSystem Implementation
        
        /// <summary>
        /// Records feedback for a generated level and updates preferences.
        /// </summary>
        public void RecordFeedback(LevelData level, float rating, string comments = null)
        {
            if (level == null)
            {
                DebugLogger.LogWarning("Cannot record feedback for null level");
                return;
            }
            
            rating = Mathf.Clamp01(rating);
            
            // Create feedback entry
            var feedback = new FeedbackEntry
            {
                LevelId = level.LevelId,
                Theme = level.Theme,
                RoomCount = level.Rooms.Count,
                Difficulty = level.BaseDifficulty,
                Rating = rating,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Comments = comments
            };
            
            // Count template usage
            foreach (var room in level.Rooms)
            {
                if (!string.IsNullOrEmpty(room.TemplateId))
                {
                    if (feedback.TemplateUsage.ContainsKey(room.TemplateId))
                        feedback.TemplateUsage[room.TemplateId]++;
                    else
                        feedback.TemplateUsage[room.TemplateId] = 1;
                }
            }
            
            // Add to history
            _preferences.FeedbackHistory.Add(feedback);
            
            // Trim history if too large
            while (_preferences.FeedbackHistory.Count > MAX_HISTORY_SIZE)
            {
                _preferences.FeedbackHistory.RemoveAt(0);
            }
            
            // Update template weights based on feedback
            UpdateTemplateWeights(feedback);
            
            // Update theme preference
            UpdateThemePreference(level.Theme, rating);
            
            // Update preferred room count
            UpdatePreferredRoomCount(level.Rooms.Count, rating);
            
            DebugLogger.LogLearning($"Recorded feedback: Level {level.LevelId}, Rating: {rating:P0}");
            
            // Auto-save
            SavePreferences();
        }
        
        /// <summary>
        /// Gets adjusted template weights based on learned preferences.
        /// </summary>
        public Dictionary<string, float> GetTemplateWeights()
        {
            return new Dictionary<string, float>(_preferences.TemplateWeights);
        }
        
        /// <summary>
        /// Saves preferences to disk.
        /// </summary>
        public void SavePreferences()
        {
            try
            {
                var json = JsonUtility.ToJson(_preferences, true);
                File.WriteAllText(_savePath, json);
                DebugLogger.LogLearning("Preferences saved");
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "SavePreferences");
            }
        }
        
        /// <summary>
        /// Loads preferences from disk.
        /// </summary>
        public void LoadPreferences()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    _preferences = JsonUtility.FromJson<UserPreferencesData>(json);
                    DebugLogger.LogLearning($"Preferences loaded: {_preferences.FeedbackHistory.Count} feedback entries");
                }
                else
                {
                    _preferences = new UserPreferencesData();
                    DebugLogger.LogLearning("No existing preferences found, using defaults");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "LoadPreferences");
                _preferences = new UserPreferencesData();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Gets the learned preferred room count for a given theme.
        /// </summary>
        public int GetPreferredRoomCount(ThemeType theme)
        {
            if (_preferences.PreferredRoomCounts.TryGetValue(theme.ToString(), out float count))
            {
                return Mathf.RoundToInt(count);
            }
            return 10; // Default
        }
        
        /// <summary>
        /// Gets the learned preferred difficulty for a given theme.
        /// </summary>
        public Difficulty GetPreferredDifficulty(ThemeType theme)
        {
            if (_preferences.PreferredDifficulties.TryGetValue(theme.ToString(), out int diff))
            {
                return (Difficulty)Mathf.Clamp(diff, 0, (int)Difficulty.Nightmare);
            }
            return Difficulty.Medium;
        }
        
        /// <summary>
        /// Gets the user's preferred theme based on usage history.
        /// </summary>
        public ThemeType GetPreferredTheme()
        {
            ThemeType preferred = ThemeType.DarkDungeon;
            float maxScore = 0f;
            
            foreach (var kvp in _preferences.ThemePreferences)
            {
                if (kvp.Value > maxScore && Enum.TryParse<ThemeType>(kvp.Key, out var theme))
                {
                    maxScore = kvp.Value;
                    preferred = theme;
                }
            }
            
            return preferred;
        }
        
        /// <summary>
        /// Gets weight modifier for a specific template.
        /// </summary>
        public float GetTemplateWeight(string templateId)
        {
            if (_preferences.TemplateWeights.TryGetValue(templateId, out float weight))
            {
                return weight;
            }
            return 1.0f;
        }
        
        /// <summary>
        /// Resets all learning data.
        /// </summary>
        public void ResetLearning()
        {
            _preferences = new UserPreferencesData();
            SavePreferences();
            DebugLogger.LogLearning("Learning data reset");
        }
        
        /// <summary>
        /// Gets statistics about the learning data.
        /// </summary>
        public LearningStats GetStats()
        {
            var stats = new LearningStats
            {
                TotalFeedbackCount = _preferences.FeedbackHistory.Count,
                AverageRating = 0f,
                MostUsedTheme = GetPreferredTheme(),
                LearnedTemplateCount = _preferences.TemplateWeights.Count
            };
            
            if (_preferences.FeedbackHistory.Count > 0)
            {
                float totalRating = 0f;
                foreach (var fb in _preferences.FeedbackHistory)
                {
                    totalRating += fb.Rating;
                }
                stats.AverageRating = totalRating / _preferences.FeedbackHistory.Count;
            }
            
            return stats;
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateTemplateWeights(FeedbackEntry feedback)
        {
            // Adjust weights based on rating
            // High ratings increase weight, low ratings decrease
            float adjustment = (feedback.Rating - 0.5f) * 2f * LEARNING_RATE;
            
            foreach (var kvp in feedback.TemplateUsage)
            {
                string templateId = kvp.Key;
                int usageCount = kvp.Value;
                
                if (!_preferences.TemplateWeights.ContainsKey(templateId))
                {
                    _preferences.TemplateWeights[templateId] = 1.0f;
                }
                
                // Weight adjustment proportional to usage
                float currentWeight = _preferences.TemplateWeights[templateId];
                float newWeight = currentWeight + (adjustment * usageCount / feedback.TemplateUsage.Count);
                
                _preferences.TemplateWeights[templateId] = Mathf.Clamp(newWeight, MIN_WEIGHT, MAX_WEIGHT);
            }
        }
        
        private void UpdateThemePreference(ThemeType theme, float rating)
        {
            string themeKey = theme.ToString();
            
            if (!_preferences.ThemePreferences.ContainsKey(themeKey))
            {
                _preferences.ThemePreferences[themeKey] = 0.5f;
            }
            
            // Smooth update towards new rating
            float current = _preferences.ThemePreferences[themeKey];
            _preferences.ThemePreferences[themeKey] = Mathf.Lerp(current, rating, LEARNING_RATE);
        }
        
        private void UpdatePreferredRoomCount(int roomCount, float rating)
        {
            // Only update if rating is good
            if (rating < 0.6f) return;
            
            // Update based on all recent positive feedback
            if (!_preferences.PreferredRoomCounts.ContainsKey("_global"))
            {
                _preferences.PreferredRoomCounts["_global"] = roomCount;
            }
            
            float current = _preferences.PreferredRoomCounts["_global"];
            _preferences.PreferredRoomCounts["_global"] = Mathf.Lerp(current, roomCount, LEARNING_RATE);
        }
        
        #endregion
    }
    
    #region Data Structures
    
    /// <summary>
    /// Persistent user preferences data.
    /// </summary>
    [Serializable]
    public class UserPreferencesData
    {
        public List<FeedbackEntry> FeedbackHistory = new List<FeedbackEntry>();
        public SerializableDictionary<string, float> TemplateWeights = new SerializableDictionary<string, float>();
        public SerializableDictionary<string, float> ThemePreferences = new SerializableDictionary<string, float>();
        public SerializableDictionary<string, float> PreferredRoomCounts = new SerializableDictionary<string, float>();
        public SerializableDictionary<string, int> PreferredDifficulties = new SerializableDictionary<string, int>();
        public long LastModifiedTimestamp;
    }
    
    /// <summary>
    /// Single feedback entry.
    /// </summary>
    [Serializable]
    public class FeedbackEntry
    {
        public string LevelId;
        public ThemeType Theme;
        public int RoomCount;
        public Difficulty Difficulty;
        public float Rating; // 0-1
        public long Timestamp;
        public string Comments;
        public SerializableDictionary<string, int> TemplateUsage = new SerializableDictionary<string, int>();
    }
    
    /// <summary>
    /// Learning statistics summary.
    /// </summary>
    public struct LearningStats
    {
        public int TotalFeedbackCount;
        public float AverageRating;
        public ThemeType MostUsedTheme;
        public int LearnedTemplateCount;
    }
    
    /// <summary>
    /// Serializable dictionary wrapper for Unity JSON serialization.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();
        
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in this)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }
        
        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
            {
                this[_keys[i]] = _values[i];
            }
        }
    }
    
    #endregion
}
