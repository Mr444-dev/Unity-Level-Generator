using System;
using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Runtime
{
    using Core;
    
    /// <summary>
    /// Tracks player behavior and generates analytics for level optimization.
    /// Records heatmaps, paths, deaths, and engagement metrics.
    /// </summary>
    public class PlayerAnalytics : MonoBehaviour
    {
        #region Singleton
        
        private static PlayerAnalytics _instance;
        public static PlayerAnalytics Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[PlayerAnalytics]");
                    _instance = go.AddComponent<PlayerAnalytics>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Tracking Settings")]
        [SerializeField] private float _positionSampleInterval = 0.5f;
        [SerializeField] private float _heatmapCellSize = 1f;
        [SerializeField] private int _maxSamples = 10000;
        
        [Header("Debug")]
        [SerializeField] private bool _drawHeatmap = false;
        [SerializeField] private Gradient _heatmapGradient;
        
        #endregion
        
        #region Fields
        
        private Transform _playerTransform;
        private float _lastSampleTime;
        private string _currentLevelId;
        private string _currentSessionId;
        
        // Analytics data
        private List<PositionSample> _positionSamples = new List<PositionSample>();
        private Dictionary<Vector2Int, int> _heatmap = new Dictionary<Vector2Int, int>();
        private List<DeathEvent> _deathEvents = new List<DeathEvent>();
        private List<RoomEvent> _roomEvents = new List<RoomEvent>();
        private Dictionary<string, float> _roomDwellTime = new Dictionary<string, float>();
        
        // Session stats
        private float _sessionStartTime;
        private string _lastRoomId;
        private float _lastRoomEnterTime;
        
        #endregion
        
        #region Properties
        
        public string CurrentSessionId => _currentSessionId;
        public int SampleCount => _positionSamples.Count;
        public IReadOnlyDictionary<Vector2Int, int> Heatmap => _heatmap;
        public IReadOnlyList<DeathEvent> DeathEvents => _deathEvents;
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        public class PositionSample
        {
            public float Timestamp;
            public Vector3 Position;
            public string RoomId;
        }
        
        [Serializable]
        public class DeathEvent
        {
            public float Timestamp;
            public Vector3 Position;
            public string RoomId;
            public string Cause;
        }
        
        [Serializable]
        public class RoomEvent
        {
            public string RoomId;
            public float EnterTime;
            public float ExitTime;
            public RoomEventType Type;
        }
        
        public enum RoomEventType
        {
            Enter,
            Exit,
            Clear,
            Death
        }
        
        [Serializable]
        public class AnalyticsReport
        {
            public string SessionId;
            public string LevelId;
            public float TotalPlayTime;
            public int DeathCount;
            public int RoomsVisited;
            public Dictionary<string, float> RoomDwellTimes;
            public List<Vector2> HotspotPositions;
            public List<Vector2> DeathPositions;
            public float AverageTimePerRoom;
            public string MostVisitedRoom;
            public string MostDeathyRoom;
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
            
            InitializeDefaultGradient();
        }
        
        private void Update()
        {
            if (_playerTransform != null && Time.time - _lastSampleTime > _positionSampleInterval)
            {
                _lastSampleTime = Time.time;
                RecordPosition();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_drawHeatmap || _heatmap.Count == 0) return;
            
            int maxValue = 1;
            foreach (var value in _heatmap.Values)
            {
                if (value > maxValue) maxValue = value;
            }
            
            foreach (var kvp in _heatmap)
            {
                float t = (float)kvp.Value / maxValue;
                Gizmos.color = _heatmapGradient?.Evaluate(t) ?? Color.Lerp(Color.blue, Color.red, t);
                
                var pos = new Vector3(
                    kvp.Key.x * _heatmapCellSize + _heatmapCellSize / 2f,
                    0.1f,
                    kvp.Key.y * _heatmapCellSize + _heatmapCellSize / 2f
                );
                
                Gizmos.DrawCube(pos, new Vector3(_heatmapCellSize * 0.9f, 0.1f, _heatmapCellSize * 0.9f));
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts a new analytics session.
        /// </summary>
        public void StartSession(string levelId, Transform playerTransform)
        {
            _currentLevelId = levelId;
            _currentSessionId = Guid.NewGuid().ToString("N")[..8];
            _playerTransform = playerTransform;
            _sessionStartTime = Time.time;
            
            ClearSession();
            DebugLogger.LogCore($"Analytics session started: {_currentSessionId}");
        }
        
        /// <summary>
        /// Ends current session and generates report.
        /// </summary>
        public AnalyticsReport EndSession()
        {
            var report = GenerateReport();
            
            DebugLogger.LogCore($"Analytics session ended: {_currentSessionId}");
            DebugLogger.LogCore($"  Play time: {report.TotalPlayTime:F1}s, Deaths: {report.DeathCount}, Rooms: {report.RoomsVisited}");
            
            return report;
        }
        
        /// <summary>
        /// Records a player death.
        /// </summary>
        public void RecordDeath(Vector3 position, string roomId, string cause = null)
        {
            _deathEvents.Add(new DeathEvent
            {
                Timestamp = Time.time - _sessionStartTime,
                Position = position,
                RoomId = roomId,
                Cause = cause ?? "Unknown"
            });
            
            RecordRoomEvent(roomId, RoomEventType.Death);
            
            // Add to heatmap with high weight
            var cell = WorldToCell(position);
            _heatmap[cell] = _heatmap.GetValueOrDefault(cell) + 5;
        }
        
        /// <summary>
        /// Records room entry.
        /// </summary>
        public void RecordRoomEnter(string roomId)
        {
            // Record exit from previous room
            if (!string.IsNullOrEmpty(_lastRoomId))
            {
                RecordRoomEvent(_lastRoomId, RoomEventType.Exit);
                
                // Update dwell time
                float dwellTime = Time.time - _lastRoomEnterTime;
                _roomDwellTime[_lastRoomId] = _roomDwellTime.GetValueOrDefault(_lastRoomId) + dwellTime;
            }
            
            _lastRoomId = roomId;
            _lastRoomEnterTime = Time.time;
            RecordRoomEvent(roomId, RoomEventType.Enter);
        }
        
        /// <summary>
        /// Records room clear.
        /// </summary>
        public void RecordRoomClear(string roomId)
        {
            RecordRoomEvent(roomId, RoomEventType.Clear);
        }
        
        /// <summary>
        /// Gets hotspot positions (high-traffic areas).
        /// </summary>
        public List<Vector3> GetHotspots(int count = 5)
        {
            var sortedCells = new List<KeyValuePair<Vector2Int, int>>(_heatmap);
            sortedCells.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            var hotspots = new List<Vector3>();
            for (int i = 0; i < Mathf.Min(count, sortedCells.Count); i++)
            {
                var cell = sortedCells[i].Key;
                hotspots.Add(new Vector3(
                    cell.x * _heatmapCellSize + _heatmapCellSize / 2f,
                    0,
                    cell.y * _heatmapCellSize + _heatmapCellSize / 2f
                ));
            }
            
            return hotspots;
        }
        
        /// <summary>
        /// Gets death hotspots.
        /// </summary>
        public List<Vector3> GetDeathHotspots(int count = 5)
        {
            // Group deaths by cell
            var deathCells = new Dictionary<Vector2Int, int>();
            foreach (var death in _deathEvents)
            {
                var cell = WorldToCell(death.Position);
                deathCells[cell] = deathCells.GetValueOrDefault(cell) + 1;
            }
            
            var sortedCells = new List<KeyValuePair<Vector2Int, int>>(deathCells);
            sortedCells.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            var hotspots = new List<Vector3>();
            for (int i = 0; i < Mathf.Min(count, sortedCells.Count); i++)
            {
                var cell = sortedCells[i].Key;
                hotspots.Add(new Vector3(
                    cell.x * _heatmapCellSize,
                    0,
                    cell.y * _heatmapCellSize
                ));
            }
            
            return hotspots;
        }
        
        /// <summary>
        /// Exports analytics data to JSON.
        /// </summary>
        public string ExportToJson()
        {
            var report = GenerateReport();
            return JsonUtility.ToJson(report, true);
        }
        
        /// <summary>
        /// Clears current session data.
        /// </summary>
        public void ClearSession()
        {
            _positionSamples.Clear();
            _heatmap.Clear();
            _deathEvents.Clear();
            _roomEvents.Clear();
            _roomDwellTime.Clear();
            _lastRoomId = null;
        }
        
        #endregion
        
        #region Private Methods
        
        private void RecordPosition()
        {
            if (_positionSamples.Count >= _maxSamples)
            {
                _positionSamples.RemoveAt(0);
            }
            
            var pos = _playerTransform.position;
            
            _positionSamples.Add(new PositionSample
            {
                Timestamp = Time.time - _sessionStartTime,
                Position = pos,
                RoomId = _lastRoomId
            });
            
            // Update heatmap
            var cell = WorldToCell(pos);
            _heatmap[cell] = _heatmap.GetValueOrDefault(cell) + 1;
        }
        
        private void RecordRoomEvent(string roomId, RoomEventType type)
        {
            _roomEvents.Add(new RoomEvent
            {
                RoomId = roomId,
                EnterTime = type == RoomEventType.Enter ? Time.time - _sessionStartTime : 0,
                ExitTime = type == RoomEventType.Exit ? Time.time - _sessionStartTime : 0,
                Type = type
            });
        }
        
        private Vector2Int WorldToCell(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / _heatmapCellSize),
                Mathf.FloorToInt(position.z / _heatmapCellSize)
            );
        }
        
        private AnalyticsReport GenerateReport()
        {
            var report = new AnalyticsReport
            {
                SessionId = _currentSessionId,
                LevelId = _currentLevelId,
                TotalPlayTime = Time.time - _sessionStartTime,
                DeathCount = _deathEvents.Count,
                RoomsVisited = _roomDwellTime.Count,
                RoomDwellTimes = new Dictionary<string, float>(_roomDwellTime)
            };
            
            // Calculate average time per room
            if (_roomDwellTime.Count > 0)
            {
                float totalDwell = 0f;
                string mostVisited = null;
                float maxDwell = 0f;
                
                foreach (var kvp in _roomDwellTime)
                {
                    totalDwell += kvp.Value;
                    if (kvp.Value > maxDwell)
                    {
                        maxDwell = kvp.Value;
                        mostVisited = kvp.Key;
                    }
                }
                
                report.AverageTimePerRoom = totalDwell / _roomDwellTime.Count;
                report.MostVisitedRoom = mostVisited;
            }
            
            // Find most deathy room
            var deathsByRoom = new Dictionary<string, int>();
            foreach (var death in _deathEvents)
            {
                if (!string.IsNullOrEmpty(death.RoomId))
                {
                    deathsByRoom[death.RoomId] = deathsByRoom.GetValueOrDefault(death.RoomId) + 1;
                }
            }
            
            string mostDeaths = null;
            int maxDeaths = 0;
            foreach (var kvp in deathsByRoom)
            {
                if (kvp.Value > maxDeaths)
                {
                    maxDeaths = kvp.Value;
                    mostDeaths = kvp.Key;
                }
            }
            report.MostDeathyRoom = mostDeaths;
            
            // Hotspots
            var hotspots = GetHotspots(5);
            report.HotspotPositions = hotspots.ConvertAll(v => new Vector2(v.x, v.z));
            
            var deathHotspots = GetDeathHotspots(5);
            report.DeathPositions = deathHotspots.ConvertAll(v => new Vector2(v.x, v.z));
            
            return report;
        }
        
        private void InitializeDefaultGradient()
        {
            if (_heatmapGradient == null)
            {
                _heatmapGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.blue, 0f),
                    new GradientColorKey(Color.cyan, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.yellow, 0.75f),
                    new GradientColorKey(Color.red, 1f)
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.5f, 0f),
                    new GradientAlphaKey(0.8f, 1f)
                };
                _heatmapGradient.SetKeys(colorKeys, alphaKeys);
            }
        }
        
        #endregion
    }
}
