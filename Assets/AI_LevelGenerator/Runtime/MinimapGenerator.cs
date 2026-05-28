using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AILevelGenerator.Runtime
{
    using Core;
    using Data;
    
    /// <summary>
    /// Generates and displays a minimap from level data.
    /// Supports real-time player tracking and room discovery.
    /// </summary>
    public class MinimapGenerator : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _playerMarker;
        [SerializeField] private GameObject _roomMarkerPrefab;
        [SerializeField] private Transform _markerContainer;
        
        [Header("Rendering")]
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private float _pixelsPerUnit = 10f;
        [SerializeField] private bool _fogOfWar = true;
        
        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        [SerializeField] private Color _roomColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _visitedRoomColor = new Color(0.3f, 0.5f, 0.3f);
        [SerializeField] private Color _currentRoomColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _entranceColor = Color.green;
        [SerializeField] private Color _exitColor = Color.blue;
        [SerializeField] private Color _bossColor = Color.red;
        [SerializeField] private Color _treasureColor = Color.yellow;
        [SerializeField] private Color _connectionColor = new Color(0.4f, 0.4f, 0.4f);
        
        [Header("Player Tracking")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private float _updateInterval = 0.1f;
        
        #endregion
        
        #region Fields
        
        private LevelData _level;
        private Texture2D _minimapTexture;
        private HashSet<string> _visitedRooms = new HashSet<string>();
        private HashSet<string> _discoveredRooms = new HashSet<string>();
        private string _currentRoomId;
        private Vector3 _levelOrigin;
        private Vector3 _levelSize;
        private float _lastUpdateTime;
        private Dictionary<string, RectTransform> _roomMarkers = new Dictionary<string, RectTransform>();
        
        #endregion
        
        #region Properties
        
        public HashSet<string> VisitedRooms => _visitedRooms;
        public string CurrentRoomId => _currentRoomId;
        
        #endregion
        
        #region Events
        
        public event System.Action<RoomData> OnRoomDiscovered;
        public event System.Action<RoomData> OnRoomEntered;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the minimap with level data.
        /// </summary>
        public void Initialize(LevelData level)
        {
            _level = level;
            
            // Calculate level bounds
            CalculateLevelBounds();
            
            // Create texture
            CreateMinimapTexture();
            
            // Initial render
            RenderMinimap();
            
            // Create room markers if using UI approach
            if (_roomMarkerPrefab != null && _markerContainer != null)
            {
                CreateRoomMarkers();
            }
            
            // Discover entrance
            if (level.EntranceRoom != null)
            {
                DiscoverRoom(level.EntranceRoom.Id);
                EnterRoom(level.EntranceRoom.Id);
            }
            
            DebugLogger.LogCore($"Minimap initialized for level with {level.Rooms.Count} rooms");
        }
        
        /// <summary>
        /// Marks a room as discovered (visible on map).
        /// </summary>
        public void DiscoverRoom(string roomId)
        {
            if (_discoveredRooms.Add(roomId))
            {
                var room = _level.FindRoom(roomId);
                if (room != null)
                {
                    OnRoomDiscovered?.Invoke(room);
                    
                    // Also discover connected rooms if fog of war is partial
                    foreach (var connectedId in room.ConnectedRoomIds)
                    {
                        _discoveredRooms.Add(connectedId);
                    }
                }
                
                RenderMinimap();
                UpdateRoomMarkers();
            }
        }
        
        /// <summary>
        /// Marks a room as entered (visited).
        /// </summary>
        public void EnterRoom(string roomId)
        {
            _currentRoomId = roomId;
            DiscoverRoom(roomId);
            
            if (_visitedRooms.Add(roomId))
            {
                var room = _level.FindRoom(roomId);
                if (room != null)
                {
                    OnRoomEntered?.Invoke(room);
                }
            }
            
            RenderMinimap();
            UpdateRoomMarkers();
        }
        
        /// <summary>
        /// Gets the room at the given world position.
        /// </summary>
        public RoomData GetRoomAtPosition(Vector3 worldPosition)
        {
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            float minDist = float.MaxValue;
            RoomData closest = null;
            
            foreach (var room in _level.Rooms)
            {
                float dist = Vector3.Distance(room.WorldPosition, worldPosition);
                if (dist < cellSize && dist < minDist)
                {
                    minDist = dist;
                    closest = room;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Reveals the entire map (disables fog of war).
        /// </summary>
        public void RevealAll()
        {
            foreach (var room in _level.Rooms)
            {
                _discoveredRooms.Add(room.Id);
            }
            RenderMinimap();
            UpdateRoomMarkers();
        }
        
        /// <summary>
        /// Clears all discovery progress.
        /// </summary>
        public void Reset()
        {
            _visitedRooms.Clear();
            _discoveredRooms.Clear();
            _currentRoomId = null;
            RenderMinimap();
            UpdateRoomMarkers();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Update()
        {
            if (_playerTransform != null && Time.time - _lastUpdateTime > _updateInterval)
            {
                _lastUpdateTime = Time.time;
                UpdatePlayerPosition();
                
                // Auto-discover rooms as player moves
                var room = GetRoomAtPosition(_playerTransform.position);
                if (room != null && room.Id != _currentRoomId)
                {
                    EnterRoom(room.Id);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_minimapTexture != null)
            {
                Destroy(_minimapTexture);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CalculateLevelBounds()
        {
            if (_level.Rooms.Count == 0) return;
            
            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            
            foreach (var room in _level.Rooms)
            {
                min = Vector3.Min(min, room.WorldPosition);
                max = Vector3.Max(max, room.WorldPosition);
            }
            
            float padding = GenerationSettingsData.Instance.GridCellSize;
            _levelOrigin = min - Vector3.one * padding;
            _levelSize = max - min + Vector3.one * padding * 2;
        }
        
        private void CreateMinimapTexture()
        {
            _minimapTexture = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
            _minimapTexture.filterMode = FilterMode.Point;
            _minimapTexture.wrapMode = TextureWrapMode.Clamp;
            
            if (_minimapImage != null)
            {
                _minimapImage.texture = _minimapTexture;
            }
        }
        
        private void RenderMinimap()
        {
            if (_minimapTexture == null || _level == null) return;
            
            // Clear to background
            var colors = new Color[_textureSize * _textureSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = _backgroundColor;
            }
            _minimapTexture.SetPixels(colors);
            
            // Draw connections first
            foreach (var room in _level.Rooms)
            {
                if (_fogOfWar && !_discoveredRooms.Contains(room.Id)) continue;
                
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    var connected = _level.FindRoom(connectedId);
                    if (connected == null) continue;
                    if (_fogOfWar && !_discoveredRooms.Contains(connectedId)) continue;
                    
                    DrawLine(
                        WorldToMinimap(room.WorldPosition),
                        WorldToMinimap(connected.WorldPosition),
                        _connectionColor,
                        2
                    );
                }
            }
            
            // Draw rooms
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            int roomRadius = Mathf.Max(2, Mathf.RoundToInt(cellSize * _pixelsPerUnit / 3f));
            
            foreach (var room in _level.Rooms)
            {
                if (_fogOfWar && !_discoveredRooms.Contains(room.Id)) continue;
                
                var pos = WorldToMinimap(room.WorldPosition);
                var color = GetRoomColor(room);
                
                DrawFilledCircle(pos, roomRadius, color);
            }
            
            _minimapTexture.Apply();
        }
        
        private Color GetRoomColor(RoomData room)
        {
            if (room.Id == _currentRoomId) return _currentRoomColor;
            if (_visitedRooms.Contains(room.Id)) return _visitedRoomColor;
            
            return room.Type switch
            {
                RoomType.Entrance => _entranceColor,
                RoomType.Exit => _exitColor,
                RoomType.Boss => _bossColor,
                RoomType.Treasure => _treasureColor,
                _ => _roomColor
            };
        }
        
        private Vector2Int WorldToMinimap(Vector3 worldPos)
        {
            float normalizedX = (worldPos.x - _levelOrigin.x) / _levelSize.x;
            float normalizedZ = (worldPos.z - _levelOrigin.z) / _levelSize.z;
            
            return new Vector2Int(
                Mathf.RoundToInt(normalizedX * _textureSize),
                Mathf.RoundToInt(normalizedZ * _textureSize)
            );
        }
        
        private void DrawFilledCircle(Vector2Int center, int radius, Color color)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        SetPixelSafe(center.x + x, center.y + y, color);
                    }
                }
            }
        }
        
        private void DrawLine(Vector2Int from, Vector2Int to, Color color, int thickness)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1;
            int sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            
            int x = from.x;
            int y = from.y;
            
            while (true)
            {
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                    {
                        SetPixelSafe(x + tx, y + ty, color);
                    }
                }
                
                if (x == to.x && y == to.y) break;
                
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }
        }
        
        private void SetPixelSafe(int x, int y, Color color)
        {
            if (x >= 0 && x < _textureSize && y >= 0 && y < _textureSize)
            {
                _minimapTexture.SetPixel(x, y, color);
            }
        }
        
        private void CreateRoomMarkers()
        {
            foreach (var room in _level.Rooms)
            {
                var marker = Instantiate(_roomMarkerPrefab, _markerContainer);
                var rt = marker.GetComponent<RectTransform>();
                _roomMarkers[room.Id] = rt;
                marker.SetActive(false);
            }
        }
        
        private void UpdateRoomMarkers()
        {
            if (_markerContainer == null) return;
            
            var containerSize = (transform as RectTransform)?.sizeDelta ?? new Vector2(200, 200);
            
            foreach (var room in _level.Rooms)
            {
                if (!_roomMarkers.TryGetValue(room.Id, out var marker)) continue;
                
                bool visible = !_fogOfWar || _discoveredRooms.Contains(room.Id);
                marker.gameObject.SetActive(visible);
                
                if (visible)
                {
                    var pos = WorldToMinimap(room.WorldPosition);
                    float normalizedX = (float)pos.x / _textureSize;
                    float normalizedY = (float)pos.y / _textureSize;
                    
                    marker.anchoredPosition = new Vector2(
                        (normalizedX - 0.5f) * containerSize.x,
                        (normalizedY - 0.5f) * containerSize.y
                    );
                    
                    // Update color
                    var image = marker.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = GetRoomColor(room);
                    }
                }
            }
        }
        
        private void UpdatePlayerPosition()
        {
            if (_playerMarker == null || _playerTransform == null) return;
            
            var containerSize = (transform as RectTransform)?.sizeDelta ?? new Vector2(200, 200);
            var pos = WorldToMinimap(_playerTransform.position);
            
            float normalizedX = (float)pos.x / _textureSize;
            float normalizedY = (float)pos.y / _textureSize;
            
            _playerMarker.anchoredPosition = new Vector2(
                (normalizedX - 0.5f) * containerSize.x,
                (normalizedY - 0.5f) * containerSize.y
            );
            
            // Rotate marker based on player facing
            float angle = _playerTransform.eulerAngles.y;
            _playerMarker.localRotation = Quaternion.Euler(0, 0, -angle);
        }
        
        #endregion
    }
}
