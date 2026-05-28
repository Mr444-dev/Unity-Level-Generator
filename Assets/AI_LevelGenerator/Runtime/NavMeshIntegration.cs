using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace AILevelGenerator.Runtime
{
    using Core;
    using Data;
    
    /// <summary>
    /// Integrates level generation with Unity's NavMesh system.
    /// Supports both runtime baking and NavMesh surface components.
    /// </summary>
    public class NavMeshIntegration : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("NavMesh Settings")]
        [SerializeField] private float _agentRadius = 0.5f;
        [SerializeField] private float _agentHeight = 2f;
        [SerializeField] private float _maxSlope = 45f;
        [SerializeField] private float _stepHeight = 0.4f;
        
        [Header("Runtime Baking")]
        [SerializeField] private bool _bakeOnLevelLoad = true;
        [SerializeField] private float _voxelSize = 0.1f;
        [SerializeField] private int _tileSize = 256;
        
        [Header("Links")]
        [SerializeField] private bool _createOffMeshLinks = true;
        [SerializeField] private float _jumpDistance = 3f;
        [SerializeField] private float _dropHeight = 2f;
        
        #endregion
        
        #region Fields
        
        private NavMeshData _navMeshData;
        private NavMeshDataInstance _navMeshInstance;
        private List<NavMeshBuildSource> _sources;
        private Bounds _levelBounds;
#if AI_NAVIGATION_INSTALLED
        private List<NavMeshLink> _createdLinks = new List<NavMeshLink>();
#endif

        
        #endregion
        
        #region Properties
        
        public bool IsNavMeshReady => _navMeshInstance.valid;
        
        #endregion
        
        #region Events
        
        public event System.Action OnNavMeshBaked;
        public event System.Action<string> OnNavMeshError;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes NavMesh for a level.
        /// </summary>
        public void Initialize(LevelData level, GameObject levelRoot)
        {
            if (levelRoot == null)
            {
                OnNavMeshError?.Invoke("Level root is null");
                return;
            }
            
            CalculateLevelBounds(level);
            
            if (_bakeOnLevelLoad)
            {
                BakeNavMesh(levelRoot);
            }
        }
        
        /// <summary>
        /// Bakes NavMesh for the level.
        /// </summary>
        public void BakeNavMesh(GameObject levelRoot)
        {
            // Collect sources from level geometry
            CollectNavMeshSources(levelRoot);
            
            if (_sources.Count == 0)
            {
                DebugLogger.LogWarning("No NavMesh sources found in level");
                OnNavMeshError?.Invoke("No geometry found for NavMesh");
                return;
            }
            
            // Create build settings
            var buildSettings = NavMesh.CreateSettings();
            buildSettings.agentRadius = _agentRadius;
            buildSettings.agentHeight = _agentHeight;
            buildSettings.agentSlope = _maxSlope;
            buildSettings.agentClimb = _stepHeight;
            buildSettings.voxelSize = _voxelSize;
            buildSettings.tileSize = _tileSize;
            
            // Remove old NavMesh
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
            }
            
            // Build NavMesh
            _navMeshData = NavMeshBuilder.BuildNavMeshData(
                buildSettings,
                _sources,
                _levelBounds,
                Vector3.zero,
                Quaternion.identity
            );
            
            if (_navMeshData == null)
            {
                OnNavMeshError?.Invoke("Failed to build NavMesh");
                return;
            }
            
            // Add to scene
            _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData);
            
            DebugLogger.LogCore($"NavMesh baked: {_sources.Count} sources, bounds: {_levelBounds}");
            
            // Create off-mesh links if enabled
            if (_createOffMeshLinks)
            {
#if AI_NAVIGATION_INSTALLED
                CreateOffMeshLinks(levelRoot);
#endif
            }
            
            OnNavMeshBaked?.Invoke();
        }
        
        /// <summary>
        /// Updates NavMesh after level changes.
        /// </summary>
        public void UpdateNavMesh(GameObject levelRoot, Bounds updateBounds)
        {
            if (!_navMeshInstance.valid) return;
            
            CollectNavMeshSources(levelRoot);
            
            var buildSettings = NavMesh.GetSettingsByIndex(0);
            
            NavMeshBuilder.UpdateNavMeshData(
                _navMeshData,
                buildSettings,
                _sources,
                updateBounds
            );
            
            DebugLogger.LogCore("NavMesh updated");
        }
        
        /// <summary>
        /// Adds a NavMesh obstacle for dynamic blocking.
        /// </summary>
        public NavMeshObstacle AddObstacle(GameObject target, Vector3 size)
        {
            var obstacle = target.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            return obstacle;
        }
        
        /// <summary>
        /// Checks if a position is on the NavMesh.
        /// </summary>
        public bool IsPositionOnNavMesh(Vector3 position, float maxDistance = 1f)
        {
            return NavMesh.SamplePosition(position, out _, maxDistance, NavMesh.AllAreas);
        }
        
        /// <summary>
        /// Gets the nearest point on NavMesh.
        /// </summary>
        public Vector3 GetNearestNavMeshPoint(Vector3 position, float maxDistance = 10f)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return position;
        }
        
        /// <summary>
        /// Calculates path between two points.
        /// </summary>
        public NavMeshPath CalculatePath(Vector3 start, Vector3 end)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
            return path;
        }
        
        /// <summary>
        /// Gets all walkable positions in a room.
        /// </summary>
        public List<Vector3> GetWalkablePositions(RoomData room, int sampleCount = 10)
        {
            var positions = new List<Vector3>();
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            float radius = cellSize * 0.4f;
            
            for (int i = 0; i < sampleCount * 3 && positions.Count < sampleCount; i++)
            {
                var randomOffset = new Vector3(
                    Random.Range(-radius, radius),
                    0,
                    Random.Range(-radius, radius)
                );
                var testPos = room.WorldPosition + randomOffset;
                
                if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                {
                    positions.Add(hit.position);
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// Cleans up NavMesh data.
        /// </summary>
        public void Cleanup()
        {
#if AI_NAVIGATION_INSTALLED
            foreach (var link in _createdLinks)
            {
                if (link != null)
                {
                    Destroy(link.gameObject);
                }
            }
            _createdLinks.Clear();
#endif
            
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
            }
            
            if (_navMeshData != null)
            {
                Destroy(_navMeshData);
                _navMeshData = null;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        #endregion
        
        #region Private Methods
        
        private void CalculateLevelBounds(LevelData level)
        {
            if (level.Rooms.Count == 0)
            {
                _levelBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
                return;
            }
            
            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            
            foreach (var room in level.Rooms)
            {
                min = Vector3.Min(min, room.WorldPosition - Vector3.one * cellSize);
                max = Vector3.Max(max, room.WorldPosition + Vector3.one * cellSize + Vector3.up * 5f);
            }
            
            var center = (min + max) / 2f;
            var size = max - min;
            _levelBounds = new Bounds(center, size);
        }
        
        private void CollectNavMeshSources(GameObject root)
        {
            _sources = new List<NavMeshBuildSource>();
            
            // Get all mesh filters
            var meshFilters = root.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                
                // Skip tagged objects
                if (mf.gameObject.CompareTag("NavMeshIgnore")) continue;
                
                var source = new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix,
                    area = 0 // Walkable
                };
                
                _sources.Add(source);
            }
            
            // Get all colliders for primitive shapes
            var colliders = root.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col.gameObject.CompareTag("NavMeshIgnore")) continue;
                if (col is MeshCollider) continue; // Already handled by mesh filters
                
                NavMeshBuildSource source;
                
                if (col is BoxCollider box)
                {
                    source = new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        size = box.size,
                        transform = Matrix4x4.TRS(
                            col.transform.TransformPoint(box.center),
                            col.transform.rotation,
                            col.transform.lossyScale
                        ),
                        area = 0
                    };
                    _sources.Add(source);
                }
                else if (col is CapsuleCollider capsule)
                {
                    source = new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Capsule,
                        size = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2),
                        transform = Matrix4x4.TRS(
                            col.transform.TransformPoint(capsule.center),
                            col.transform.rotation,
                            col.transform.lossyScale
                        ),
                        area = 0
                    };
                    _sources.Add(source);
                }
            }
        }
        
#if AI_NAVIGATION_INSTALLED
        private void CreateOffMeshLinks(GameObject root)
        {
            // Find potential link points between rooms
            var level = root.GetComponentInParent<AILevelGeneratorFacade>()?.LastGeneratedLevel;
            if (level == null) return;
            
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            
            foreach (var room in level.Rooms)
            {
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    var connected = level.FindRoom(connectedId);
                    if (connected == null) continue;
                    
                    // Check height difference
                    float heightDiff = Mathf.Abs(room.WorldPosition.y - connected.WorldPosition.y);
                    if (heightDiff > _stepHeight && heightDiff <= _dropHeight)
                    {
                        CreateNavMeshLink(room.WorldPosition, connected.WorldPosition, root.transform);
                    }
                }
            }
        }
        
        private void CreateNavMeshLink(Vector3 start, Vector3 end, Transform parent)
        {
            var linkObj = new GameObject("NavMeshLink");
            linkObj.transform.SetParent(parent);
            linkObj.transform.position = (start + end) / 2f;
            
            var link = linkObj.AddComponent<NavMeshLink>();
            link.startPoint = linkObj.transform.InverseTransformPoint(start);
            link.endPoint = linkObj.transform.InverseTransformPoint(end);
            link.width = 1f;
            link.bidirectional = false; // One way for drops
            link.autoUpdate = true;
            
            _createdLinks.Add(link);
        }
#endif
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (!_navMeshInstance.valid) return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_levelBounds.center, _levelBounds.size);
        }
        
        #endregion
    }
}
