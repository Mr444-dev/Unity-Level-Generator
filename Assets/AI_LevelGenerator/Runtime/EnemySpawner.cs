using System;
using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Runtime
{
    using Core;
    using Data;
    
    /// <summary>
    /// Controls enemy spawning based on room difficulty and progression.
    /// Supports enemy pools, wave spawning, and dynamic difficulty.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        #region Enums
        
        public enum SpawnTrigger
        {
            OnRoomEnter,
            OnTimer,
            OnEvent,
            Manual
        }
        
        public enum SpawnPattern
        {
            Single,
            Group,
            Wave,
            Ambush
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private EnemyPoolData _enemyPool;
        [SerializeField] private SpawnTrigger _trigger = SpawnTrigger.OnRoomEnter;
        [SerializeField] private SpawnPattern _pattern = SpawnPattern.Group;
        
        [Header("Spawning")]
        [SerializeField] private float _spawnDelay = 0.5f;
        [SerializeField] private float _waveCooldown = 5f;
        [SerializeField] private int _maxActiveEnemies = 10;
        
        [Header("Difficulty Scaling")]
        [SerializeField] private AnimationCurve _difficultyMultiplier;
        [SerializeField] private bool _scaleToDifficulty = true;
        
        #endregion
        
        #region Fields
        
        private LevelData _currentLevel;
        private RoomData _currentRoom;
        private List<GameObject> _activeEnemies = new List<GameObject>();
        private Dictionary<string, Queue<GameObject>> _enemyPools = new Dictionary<string, Queue<GameObject>>();
        private bool _isSpawning;
        private int _currentWave;
        private int _totalWaves;
        
        #endregion
        
        #region Events
        
        public event Action<GameObject> OnEnemySpawned;
        public event Action<GameObject> OnEnemyDefeated;
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnRoomCleared;
        
        #endregion
        
        #region Properties
        
        public int ActiveEnemyCount => _activeEnemies.Count;
        public bool IsSpawning => _isSpawning;
        public int CurrentWave => _currentWave;
        public bool RoomCleared => _activeEnemies.Count == 0 && !_isSpawning;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes spawner for a level.
        /// </summary>
        public void Initialize(LevelData level, EnemyPoolData pool = null)
        {
            _currentLevel = level;
            if (pool != null) _enemyPool = pool;
            
            PrewarmPools();
            DebugLogger.LogCore("EnemySpawner initialized");
        }
        
        /// <summary>
        /// Triggers spawning for a room.
        /// </summary>
        public void EnterRoom(RoomData room)
        {
            _currentRoom = room;
            
            if (_trigger == SpawnTrigger.OnRoomEnter && room.Type != RoomType.Entrance)
            {
                StartSpawning(room);
            }
        }
        
        /// <summary>
        /// Manually starts spawning.
        /// </summary>
        public void StartSpawning(RoomData room)
        {
            if (_isSpawning) return;
            
            _currentRoom = room;
            _currentWave = 0;
            
            int baseCount = GetSpawnCount(room);
            _totalWaves = _pattern == SpawnPattern.Wave 
                ? Mathf.CeilToInt(baseCount / 3f) 
                : 1;
            
            StartCoroutine(SpawnRoutine(room));
        }
        
        /// <summary>
        /// Gets spawn points for a room.
        /// </summary>
        public List<Vector3> GetSpawnPoints(RoomData room, RoomTemplateData template, int count)
        {
            var points = new List<Vector3>();
            
            // Use template spawn points if available
            if (template != null)
            {
                foreach (var sp in template.EnemySpawnPoints)
                {
                    if (points.Count >= count) break;
                    if (sp.MinDifficulty > _currentLevel.BaseDifficulty) continue;
                    
                    points.Add(room.WorldPosition + sp.LocalPosition);
                }
            }
            
            // Fill remaining with procedural points
            while (points.Count < count)
            {
                var offset = new Vector3(
                    UnityEngine.Random.Range(-3f, 3f),
                    0f,
                    UnityEngine.Random.Range(-3f, 3f)
                );
                points.Add(room.WorldPosition + offset);
            }
            
            return points;
        }
        
        /// <summary>
        /// Notifies spawner that an enemy was defeated.
        /// </summary>
        public void NotifyEnemyDefeated(GameObject enemy)
        {
            if (_activeEnemies.Remove(enemy))
            {
                OnEnemyDefeated?.Invoke(enemy);
                
                // Return to pool
                if (enemy != null)
                {
                    ReturnToPool(enemy);
                }
                
                // Check if room is cleared
                if (RoomCleared)
                {
                    OnRoomCleared?.Invoke();
                    DebugLogger.LogGeneration($"Room cleared: {_currentRoom?.Id}");
                }
            }
        }
        
        /// <summary>
        /// Clears all active enemies.
        /// </summary>
        public void ClearAllEnemies()
        {
            StopAllCoroutines();
            _isSpawning = false;
            
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null)
                {
                    ReturnToPool(enemy);
                }
            }
            
            _activeEnemies.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private System.Collections.IEnumerator SpawnRoutine(RoomData room)
        {
            _isSpawning = true;
            
            int totalSpawns = GetSpawnCount(room);
            int spawnedThisRoom = 0;
            
            while (spawnedThisRoom < totalSpawns)
            {
                _currentWave++;
                OnWaveStarted?.Invoke(_currentWave);
                
                int waveSize = _pattern switch
                {
                    SpawnPattern.Single => 1,
                    SpawnPattern.Wave => Mathf.Min(3, totalSpawns - spawnedThisRoom),
                    SpawnPattern.Ambush => totalSpawns,
                    _ => Mathf.Min(3, totalSpawns - spawnedThisRoom)
                };
                
                // Wait for room in active enemies
                while (_activeEnemies.Count >= _maxActiveEnemies)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                
                // Spawn wave
                for (int i = 0; i < waveSize && spawnedThisRoom < totalSpawns; i++)
                {
                    if (_activeEnemies.Count >= _maxActiveEnemies) break;
                    
                    SpawnEnemy(room);
                    spawnedThisRoom++;
                    
                    if (i < waveSize - 1)
                    {
                        yield return new WaitForSeconds(_spawnDelay);
                    }
                }
                
                OnWaveCompleted?.Invoke(_currentWave);
                
                if (spawnedThisRoom < totalSpawns && _pattern == SpawnPattern.Wave)
                {
                    yield return new WaitForSeconds(_waveCooldown);
                }
            }
            
            _isSpawning = false;
        }
        
        private void SpawnEnemy(RoomData room)
        {
            if (_enemyPool == null || _enemyPool.Enemies.Count == 0)
            {
                SpawnPlaceholderEnemy(room);
                return;
            }
            
            // Select enemy based on difficulty
            var validEnemies = _enemyPool.Enemies.FindAll(e => 
                (int)e.MinDifficulty <= room.DifficultyModifier + (int)_currentLevel.BaseDifficulty);
            
            if (validEnemies.Count == 0)
            {
                validEnemies = _enemyPool.Enemies;
            }
            
            var enemyDef = SelectWeightedEnemy(validEnemies);
            if (enemyDef?.Prefab == null)
            {
                SpawnPlaceholderEnemy(room);
                return;
            }
            
            // Get from pool or instantiate
            var enemy = GetFromPool(enemyDef.Prefab.name);
            if (enemy == null)
            {
                enemy = Instantiate(enemyDef.Prefab);
            }
            
            // Position
            var spawnPoint = GetRandomSpawnPoint(room);
            enemy.transform.position = spawnPoint;
            enemy.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            enemy.SetActive(true);
            
            // Scale stats based on difficulty
            if (_scaleToDifficulty)
            {
                float scale = _difficultyMultiplier?.Evaluate(
                    (float)room.DifficultyModifier / 10f) ?? 1f;
                // Apply scaling to enemy components here
            }
            
            _activeEnemies.Add(enemy);
            OnEnemySpawned?.Invoke(enemy);
        }
        
        private void SpawnPlaceholderEnemy(RoomData room)
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            placeholder.transform.position = GetRandomSpawnPoint(room);
            placeholder.name = "Enemy_Placeholder";
            
            var renderer = placeholder.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
            
            _activeEnemies.Add(placeholder);
            OnEnemySpawned?.Invoke(placeholder);
        }
        
        private Vector3 GetRandomSpawnPoint(RoomData room)
        {
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            float range = cellSize * 0.4f;
            
            return room.WorldPosition + new Vector3(
                UnityEngine.Random.Range(-range, range),
                0f,
                UnityEngine.Random.Range(-range, range)
            );
        }
        
        private int GetSpawnCount(RoomData room)
        {
            int baseDifficulty = (int)_currentLevel.BaseDifficulty + room.DifficultyModifier;
            
            int baseCount = room.Type switch
            {
                RoomType.Boss => baseDifficulty + 5,
                RoomType.Normal => Mathf.Max(1, baseDifficulty),
                RoomType.Corridor => Mathf.Max(0, baseDifficulty - 2),
                _ => 0
            };
            
            return Mathf.Clamp(baseCount, 0, _maxActiveEnemies);
        }
        
        private EnemyDefinition SelectWeightedEnemy(List<EnemyDefinition> enemies)
        {
            float totalWeight = 0f;
            foreach (var e in enemies) totalWeight += e.SpawnWeight;
            
            float random = UnityEngine.Random.value * totalWeight;
            float current = 0f;
            
            foreach (var e in enemies)
            {
                current += e.SpawnWeight;
                if (random <= current) return e;
            }
            
            return enemies[enemies.Count - 1];
        }
        
        private void PrewarmPools()
        {
            if (_enemyPool == null) return;
            
            foreach (var enemy in _enemyPool.Enemies)
            {
                if (enemy.Prefab == null) continue;
                
                var pool = new Queue<GameObject>();
                for (int i = 0; i < enemy.PoolSize; i++)
                {
                    var instance = Instantiate(enemy.Prefab, transform);
                    instance.SetActive(false);
                    pool.Enqueue(instance);
                }
                
                _enemyPools[enemy.Prefab.name] = pool;
            }
        }
        
        private GameObject GetFromPool(string prefabName)
        {
            if (_enemyPools.TryGetValue(prefabName, out var pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }
            return null;
        }
        
        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            // Try to find matching pool
            foreach (var kvp in _enemyPools)
            {
                if (obj.name.StartsWith(kvp.Key))
                {
                    kvp.Value.Enqueue(obj);
                    return;
                }
            }
            
            Destroy(obj);
        }
        
        #endregion
    }
    
    #region Data Structures
    
    /// <summary>
    /// Pool of enemy types for spawning.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPool", menuName = "AI Level Generator/Enemy Pool")]
    public class EnemyPoolData : ScriptableObject
    {
        public string PoolName;
        public ThemeType Theme;
        public List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
    }
    
    [Serializable]
    public class EnemyDefinition
    {
        public string Name;
        public GameObject Prefab;
        public Difficulty MinDifficulty = Difficulty.VeryEasy;
        [Range(0.1f, 10f)] public float SpawnWeight = 1f;
        public int PoolSize = 5;
        
        [Header("Stats")]
        public float BaseHealth = 100f;
        public float BaseDamage = 10f;
        public float MoveSpeed = 3f;
    }
    
    #endregion
}
