using System;
using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Runtime
{
    using Core;
    using Data;
    
    /// <summary>
    /// Dynamically adjusts level difficulty based on player performance.
    /// Tracks player stats and modifies enemy spawns, room difficulty, and rewards.
    /// </summary>
    public class DynamicDifficultySystem : MonoBehaviour
    {
        #region Singleton
        
        private static DynamicDifficultySystem _instance;
        public static DynamicDifficultySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[DynamicDifficulty]");
                    _instance = go.AddComponent<DynamicDifficultySystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Difficulty Settings")]
        [SerializeField] private float _baseDifficulty = 1f;
        [SerializeField] private float _minDifficulty = 0.5f;
        [SerializeField] private float _maxDifficulty = 3f;
        [SerializeField] private float _adjustmentRate = 0.1f;
        
        [Header("Performance Weights")]
        [SerializeField] private float _deathPenalty = -0.2f;
        [SerializeField] private float _quickKillBonus = 0.1f;
        [SerializeField] private float _damageTakenPenalty = -0.05f;
        [SerializeField] private float _perfectClearBonus = 0.15f;
        
        [Header("Smoothing")]
        [SerializeField] private float _smoothingWindow = 5f;
        [SerializeField] private int _minSamplesForAdjustment = 3;
        
        #endregion
        
        #region Fields
        
        private float _currentDifficulty;
        private Queue<PerformanceSample> _recentSamples = new Queue<PerformanceSample>();
        private PlayerStats _sessionStats = new PlayerStats();
        private Dictionary<string, RoomPerformance> _roomPerformance = new Dictionary<string, RoomPerformance>();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current difficulty multiplier (1.0 = normal).
        /// </summary>
        public float CurrentDifficulty => _currentDifficulty;
        
        /// <summary>
        /// Difficulty as enum value.
        /// </summary>
        public Difficulty DifficultyLevel
        {
            get
            {
                if (_currentDifficulty < 0.7f) return Difficulty.VeryEasy;
                if (_currentDifficulty < 0.9f) return Difficulty.Easy;
                if (_currentDifficulty < 1.15f) return Difficulty.Medium;
                if (_currentDifficulty < 1.5f) return Difficulty.Hard;
                if (_currentDifficulty < 2f) return Difficulty.VeryHard;
                return Difficulty.Nightmare;
            }
        }
        
        /// <summary>
        /// Session statistics.
        /// </summary>
        public PlayerStats SessionStats => _sessionStats;
        
        #endregion
        
        #region Events
        
        public event Action<float, float> OnDifficultyChanged;
        public event Action<PerformanceSample> OnPerformanceRecorded;
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        public class PerformanceSample
        {
            public float Timestamp;
            public string RoomId;
            public float TimeToComplete;
            public int DamageTaken;
            public int EnemiesKilled;
            public bool PlayerDied;
            public bool PerfectClear;
            
            public float CalculateScore()
            {
                float score = 0f;
                
                if (PlayerDied) score -= 0.5f;
                else score += 0.2f;
                
                if (PerfectClear) score += 0.3f;
                
                // Fast clear bonus
                if (TimeToComplete < 30f) score += 0.1f;
                
                // Damage penalty
                score -= DamageTaken * 0.01f;
                
                return Mathf.Clamp(score, -1f, 1f);
            }
        }
        
        [Serializable]
        public class PlayerStats
        {
            public int TotalDeaths;
            public int TotalKills;
            public int RoomsCleared;
            public int PerfectClears;
            public float TotalDamageTaken;
            public float TotalPlayTime;
            public float HighestDifficulty;
            public float AveragePerformance;
        }
        
        [Serializable]
        public class RoomPerformance
        {
            public string RoomId;
            public int AttemptCount;
            public int ClearCount;
            public float BestTime;
            public float AverageDamageTaken;
        }
        
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
            
            _currentDifficulty = _baseDifficulty;
        }
        
        private void Update()
        {
            _sessionStats.TotalPlayTime += Time.deltaTime;
            
            // Remove old samples
            while (_recentSamples.Count > 0 && 
                   Time.time - _recentSamples.Peek().Timestamp > _smoothingWindow * 60f)
            {
                _recentSamples.Dequeue();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Records a performance sample for difficulty adjustment.
        /// </summary>
        public void RecordPerformance(PerformanceSample sample)
        {
            sample.Timestamp = Time.time;
            _recentSamples.Enqueue(sample);
            
            // Update session stats
            if (sample.PlayerDied) _sessionStats.TotalDeaths++;
            _sessionStats.TotalKills += sample.EnemiesKilled;
            _sessionStats.RoomsCleared++;
            if (sample.PerfectClear) _sessionStats.PerfectClears++;
            _sessionStats.TotalDamageTaken += sample.DamageTaken;
            
            // Update room performance
            UpdateRoomPerformance(sample);
            
            // Adjust difficulty
            if (_recentSamples.Count >= _minSamplesForAdjustment)
            {
                AdjustDifficulty();
            }
            
            OnPerformanceRecorded?.Invoke(sample);
            DebugLogger.LogCore($"Performance recorded: score={sample.CalculateScore():F2}, difficulty={_currentDifficulty:F2}");
        }
        
        /// <summary>
        /// Records a player death.
        /// </summary>
        public void RecordDeath(string roomId)
        {
            RecordPerformance(new PerformanceSample
            {
                RoomId = roomId,
                PlayerDied = true,
                PerfectClear = false
            });
        }
        
        /// <summary>
        /// Records room clear.
        /// </summary>
        public void RecordRoomClear(string roomId, float timeToComplete, int damageTaken, int enemiesKilled)
        {
            RecordPerformance(new PerformanceSample
            {
                RoomId = roomId,
                TimeToComplete = timeToComplete,
                DamageTaken = damageTaken,
                EnemiesKilled = enemiesKilled,
                PlayerDied = false,
                PerfectClear = damageTaken == 0
            });
        }
        
        /// <summary>
        /// Gets adjusted spawn count based on difficulty.
        /// </summary>
        public int GetAdjustedSpawnCount(int baseCount)
        {
            return Mathf.RoundToInt(baseCount * _currentDifficulty);
        }
        
        /// <summary>
        /// Gets adjusted enemy health.
        /// </summary>
        public float GetAdjustedHealth(float baseHealth)
        {
            return baseHealth * Mathf.Lerp(0.8f, 1.5f, (_currentDifficulty - _minDifficulty) / (_maxDifficulty - _minDifficulty));
        }
        
        /// <summary>
        /// Gets adjusted enemy damage.
        /// </summary>
        public float GetAdjustedDamage(float baseDamage)
        {
            return baseDamage * Mathf.Lerp(0.7f, 1.3f, (_currentDifficulty - _minDifficulty) / (_maxDifficulty - _minDifficulty));
        }
        
        /// <summary>
        /// Gets adjusted reward multiplier.
        /// </summary>
        public float GetRewardMultiplier()
        {
            // Higher difficulty = better rewards
            return Mathf.Lerp(0.8f, 1.5f, (_currentDifficulty - _minDifficulty) / (_maxDifficulty - _minDifficulty));
        }
        
        /// <summary>
        /// Resets difficulty to base.
        /// </summary>
        public void ResetDifficulty()
        {
            float oldDifficulty = _currentDifficulty;
            _currentDifficulty = _baseDifficulty;
            _recentSamples.Clear();
            
            OnDifficultyChanged?.Invoke(oldDifficulty, _currentDifficulty);
        }
        
        /// <summary>
        /// Manually sets difficulty.
        /// </summary>
        public void SetDifficulty(float difficulty)
        {
            float oldDifficulty = _currentDifficulty;
            _currentDifficulty = Mathf.Clamp(difficulty, _minDifficulty, _maxDifficulty);
            
            if (Mathf.Abs(oldDifficulty - _currentDifficulty) > 0.01f)
            {
                OnDifficultyChanged?.Invoke(oldDifficulty, _currentDifficulty);
            }
        }
        
        /// <summary>
        /// Sets difficulty from enum.
        /// </summary>
        public void SetDifficulty(Difficulty level)
        {
            float value = level switch
            {
                Difficulty.VeryEasy => 0.6f,
                Difficulty.Easy => 0.8f,
                Difficulty.Medium => 1f,
                Difficulty.Hard => 1.3f,
                Difficulty.VeryHard => 1.7f,
                Difficulty.Nightmare => 2.5f,
                _ => 1f
            };
            
            SetDifficulty(value);
        }
        
        /// <summary>
        /// Gets performance statistics for a specific room.
        /// </summary>
        public RoomPerformance GetRoomPerformance(string roomId)
        {
            return _roomPerformance.TryGetValue(roomId, out var perf) ? perf : null;
        }
        
        /// <summary>
        /// Resets all session stats.
        /// </summary>
        public void ResetSession()
        {
            _sessionStats = new PlayerStats();
            _roomPerformance.Clear();
            _recentSamples.Clear();
            ResetDifficulty();
        }
        
        #endregion
        
        #region Private Methods
        
        private void AdjustDifficulty()
        {
            // Calculate average performance from recent samples
            float totalScore = 0f;
            foreach (var sample in _recentSamples)
            {
                totalScore += sample.CalculateScore();
            }
            float avgScore = totalScore / _recentSamples.Count;
            
            _sessionStats.AveragePerformance = avgScore;
            
            // Adjust difficulty based on performance
            float oldDifficulty = _currentDifficulty;
            
            // Positive score = player doing well = increase difficulty
            // Negative score = player struggling = decrease difficulty
            float adjustment = avgScore * _adjustmentRate;
            
            _currentDifficulty = Mathf.Clamp(
                _currentDifficulty + adjustment,
                _minDifficulty,
                _maxDifficulty
            );
            
            // Track highest difficulty reached
            if (_currentDifficulty > _sessionStats.HighestDifficulty)
            {
                _sessionStats.HighestDifficulty = _currentDifficulty;
            }
            
            // Notify if significant change
            if (Mathf.Abs(oldDifficulty - _currentDifficulty) > 0.05f)
            {
                OnDifficultyChanged?.Invoke(oldDifficulty, _currentDifficulty);
                DebugLogger.LogCore($"Difficulty adjusted: {oldDifficulty:F2} -> {_currentDifficulty:F2} (avg score: {avgScore:F2})");
            }
        }
        
        private void UpdateRoomPerformance(PerformanceSample sample)
        {
            if (string.IsNullOrEmpty(sample.RoomId)) return;
            
            if (!_roomPerformance.TryGetValue(sample.RoomId, out var perf))
            {
                perf = new RoomPerformance { RoomId = sample.RoomId, BestTime = float.MaxValue };
                _roomPerformance[sample.RoomId] = perf;
            }
            
            perf.AttemptCount++;
            
            if (!sample.PlayerDied)
            {
                perf.ClearCount++;
                if (sample.TimeToComplete < perf.BestTime)
                {
                    perf.BestTime = sample.TimeToComplete;
                }
            }
            
            // Running average of damage taken
            perf.AverageDamageTaken = (perf.AverageDamageTaken * (perf.AttemptCount - 1) + sample.DamageTaken) / perf.AttemptCount;
        }
        
        #endregion
    }
}
