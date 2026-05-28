using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Utils
{
    /// <summary>
    /// Object pool for efficient room instantiation and recycling.
    /// Reduces garbage collection during level generation.
    /// </summary>
    public class RoomPool : MonoBehaviour
    {
        #region Singleton
        
        private static RoomPool _instance;
        
        public static RoomPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[RoomPool]");
                    _instance = go.AddComponent<RoomPool>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Fields
        
        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> _prefabRegistry = new Dictionary<string, GameObject>();
        private Transform _poolContainer;
        
        [SerializeField] private int _defaultPoolSize = 10;
        [SerializeField] private bool _expandPoolOnDemand = true;
        
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
            
            _poolContainer = new GameObject("PooledObjects").transform;
            _poolContainer.SetParent(transform);
            _poolContainer.gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Registers a prefab for pooling.
        /// </summary>
        public void RegisterPrefab(string id, GameObject prefab, int initialSize = -1)
        {
            if (string.IsNullOrEmpty(id) || prefab == null)
                return;
            
            if (_prefabRegistry.ContainsKey(id))
            {
                Core.DebugLogger.LogWarning($"Prefab '{id}' already registered");
                return;
            }
            
            _prefabRegistry[id] = prefab;
            _pools[id] = new Queue<GameObject>();
            
            int size = initialSize > 0 ? initialSize : _defaultPoolSize;
            Prewarm(id, size);
            
            Core.DebugLogger.LogCore($"Registered pool '{id}' with {size} instances");
        }
        
        /// <summary>
        /// Pre-instantiates objects for a pool.
        /// </summary>
        public void Prewarm(string id, int count)
        {
            if (!_prefabRegistry.TryGetValue(id, out var prefab))
                return;
            
            if (!_pools.TryGetValue(id, out var pool))
            {
                pool = new Queue<GameObject>();
                _pools[id] = pool;
            }
            
            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(prefab, _poolContainer);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }
        
        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        public GameObject Get(string id, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_pools.TryGetValue(id, out var pool))
            {
                Core.DebugLogger.LogWarning($"Pool '{id}' not found");
                return null;
            }
            
            GameObject obj;
            
            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (_expandPoolOnDemand && _prefabRegistry.TryGetValue(id, out var prefab))
            {
                obj = Instantiate(prefab);
                Core.DebugLogger.LogPerformance($"Pool '{id}' expanded - consider increasing initial size");
            }
            else
            {
                Core.DebugLogger.LogWarning($"Pool '{id}' is empty and expansion is disabled");
                return null;
            }
            
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            
            return obj;
        }
        
        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public void Return(string id, GameObject obj)
        {
            if (obj == null) return;
            
            if (!_pools.TryGetValue(id, out var pool))
            {
                // Create pool if doesn't exist
                pool = new Queue<GameObject>();
                _pools[id] = pool;
            }
            
            obj.SetActive(false);
            obj.transform.SetParent(_poolContainer);
            pool.Enqueue(obj);
        }
        
        /// <summary>
        /// Returns all objects in a parent to the pool.
        /// </summary>
        public void ReturnAll(string id, Transform parent)
        {
            if (parent == null) return;
            
            var children = new List<Transform>();
            foreach (Transform child in parent)
            {
                children.Add(child);
            }
            
            foreach (var child in children)
            {
                Return(id, child.gameObject);
            }
        }
        
        /// <summary>
        /// Clears a specific pool.
        /// </summary>
        public void ClearPool(string id)
        {
            if (!_pools.TryGetValue(id, out var pool))
                return;
            
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
            
            Core.DebugLogger.LogCore($"Pool '{id}' cleared");
        }
        
        /// <summary>
        /// Clears all pools.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var id in _pools.Keys)
            {
                ClearPool(id);
            }
            _pools.Clear();
            _prefabRegistry.Clear();
            
            Core.DebugLogger.LogCore("All pools cleared");
        }
        
        /// <summary>
        /// Gets pool statistics.
        /// </summary>
        public PoolStats GetStats(string id)
        {
            var stats = new PoolStats { PoolId = id };
            
            if (_pools.TryGetValue(id, out var pool))
            {
                stats.AvailableCount = pool.Count;
                stats.IsRegistered = true;
            }
            
            return stats;
        }
        
        /// <summary>
        /// Gets total statistics for all pools.
        /// </summary>
        public Dictionary<string, PoolStats> GetAllStats()
        {
            var allStats = new Dictionary<string, PoolStats>();
            
            foreach (var kvp in _pools)
            {
                allStats[kvp.Key] = new PoolStats
                {
                    PoolId = kvp.Key,
                    AvailableCount = kvp.Value.Count,
                    IsRegistered = true
                };
            }
            
            return allStats;
        }
        
        #endregion
        
        #region Data Structures
        
        public struct PoolStats
        {
            public string PoolId;
            public int AvailableCount;
            public bool IsRegistered;
        }
        
        #endregion
    }
}
