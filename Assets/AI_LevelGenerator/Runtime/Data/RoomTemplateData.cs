using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AILevelGenerator.Data
{
    using Core;
    
    /// <summary>
    /// ScriptableObject defining a room template for level generation.
    /// Contains all necessary data for room placement and decoration.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "AI Level Generator/Room Template")]
    public class RoomTemplateData : ScriptableObject, IRoomTemplate
    {
        #region Basic Info
        
        #if ODIN_INSPECTOR
        [BoxGroup("Basic Info")]
        [LabelText("Template ID")]
        #endif
        [SerializeField] private string _id;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Basic Info")]
        #endif
        [SerializeField] private string _displayName = "New Room";
        
        #if ODIN_INSPECTOR
        [BoxGroup("Basic Info")]
        [TextArea(2, 4)]
        #endif
        [SerializeField] private string _description;
        
        #endregion
        
        #region Room Properties
        
        #if ODIN_INSPECTOR
        [BoxGroup("Properties")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private RoomType _type = RoomType.Normal;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Properties")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private ThemeType _theme = ThemeType.DarkDungeon;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Properties")]
        [MinValue(1)]
        #endif
        [SerializeField] private Vector3Int _size = new Vector3Int(1, 1, 1);
        
        #if ODIN_INSPECTOR
        [BoxGroup("Properties")]
        [Range(1, 100)]
        [Tooltip("Higher weight = more likely to be selected during generation")]
        #endif
        [SerializeField] private int _weight = 10;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Properties")]
        [Range(0, 5)]
        #endif
        [SerializeField] private int _difficultyModifier = 0;
        
        #endregion
        
        #region Connections
        
        #if ODIN_INSPECTOR
        [BoxGroup("Connections")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private ConnectionDirection _availableConnections = ConnectionDirection.All;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Connections")]
        [InfoBox("Define connection points relative to room center")]
        #endif
        [SerializeField] private List<ConnectionPointData> _connectionPoints = new List<ConnectionPointData>();
        
        #endregion
        
        #region Prefab
        
        #if ODIN_INSPECTOR
        [BoxGroup("Prefab")]
        [Required]
        [AssetsOnly]
        #endif
        [SerializeField] private GameObject _prefab;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Prefab")]
        [Tooltip("Offset applied when instantiating the prefab")]
        #endif
        [SerializeField] private Vector3 _prefabOffset = Vector3.zero;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Prefab")]
        #endif
        [SerializeField] private bool _allowRotation = true;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Prefab")]
        [ShowIf("_allowRotation")]
        [Tooltip("Allowed rotation angles in degrees")]
        #endif
        [SerializeField] private List<float> _allowedRotations = new List<float> { 0f, 90f, 180f, 270f };
        
        #endregion
        
        #region Spawn Points
        
        #if ODIN_INSPECTOR
        [BoxGroup("Spawn Points")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<SpawnPointData> _enemySpawnPoints = new List<SpawnPointData>();
        
        #if ODIN_INSPECTOR
        [BoxGroup("Spawn Points")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<SpawnPointData> _lootSpawnPoints = new List<SpawnPointData>();
        
        #if ODIN_INSPECTOR
        [BoxGroup("Spawn Points")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<SpawnPointData> _decorationSpawnPoints = new List<SpawnPointData>();
        
        #endregion
        
        #region Compatible Templates
        
        #if ODIN_INSPECTOR
        [BoxGroup("Compatibility")]
        [Tooltip("If empty, compatible with all templates")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<RoomTemplateData> _compatibleTemplates = new List<RoomTemplateData>();
        
        #if ODIN_INSPECTOR
        [BoxGroup("Compatibility")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<RoomTemplateData> _incompatibleTemplates = new List<RoomTemplateData>();
        
        #endregion
        
        #region IRoomTemplate Implementation
        
        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => _displayName;
        public RoomType Type => _type;
        public ThemeType Theme => _theme;
        public Vector3Int Size => _size;
        public ConnectionDirection AvailableConnections => _availableConnections;
        public GameObject Prefab => _prefab;
        public int Weight => _weight;
        
        /// <summary>
        /// Checks if this template can connect to another template from a specific direction.
        /// </summary>
        public bool IsCompatibleWith(IRoomTemplate other, ConnectionDirection direction)
        {
            if (other == null) return false;
            
            // Check if this room has a connection in the given direction
            if ((AvailableConnections & direction) == 0) return false;
            
            // Check if the other room has a connection in the opposite direction
            var oppositeDirection = GetOppositeDirection(direction);
            if ((other.AvailableConnections & oppositeDirection) == 0) return false;
            
            // Check explicit compatibility lists
            if (other is RoomTemplateData otherTemplate)
            {
                if (_incompatibleTemplates.Contains(otherTemplate)) return false;
                if (_compatibleTemplates.Count > 0 && !_compatibleTemplates.Contains(otherTemplate)) return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Public Properties
        
        public Vector3 PrefabOffset => _prefabOffset;
        public bool AllowRotation => _allowRotation;
        public IReadOnlyList<float> AllowedRotations => _allowedRotations;
        public IReadOnlyList<ConnectionPointData> ConnectionPoints => _connectionPoints;
        public IReadOnlyList<SpawnPointData> EnemySpawnPoints => _enemySpawnPoints;
        public IReadOnlyList<SpawnPointData> LootSpawnPoints => _lootSpawnPoints;
        public IReadOnlyList<SpawnPointData> DecorationSpawnPoints => _decorationSpawnPoints;
        public int DifficultyModifier => _difficultyModifier;
        public string Description => _description;
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the opposite connection direction.
        /// </summary>
        public static ConnectionDirection GetOppositeDirection(ConnectionDirection direction)
        {
            return direction switch
            {
                ConnectionDirection.North => ConnectionDirection.South,
                ConnectionDirection.South => ConnectionDirection.North,
                ConnectionDirection.East => ConnectionDirection.West,
                ConnectionDirection.West => ConnectionDirection.East,
                ConnectionDirection.Up => ConnectionDirection.Down,
                ConnectionDirection.Down => ConnectionDirection.Up,
                _ => ConnectionDirection.None
            };
        }
        
        /// <summary>
        /// Gets the direction as a Vector3Int offset.
        /// </summary>
        public static Vector3Int DirectionToOffset(ConnectionDirection direction)
        {
            return direction switch
            {
                ConnectionDirection.North => new Vector3Int(0, 0, 1),
                ConnectionDirection.South => new Vector3Int(0, 0, -1),
                ConnectionDirection.East => new Vector3Int(1, 0, 0),
                ConnectionDirection.West => new Vector3Int(-1, 0, 0),
                ConnectionDirection.Up => new Vector3Int(0, 1, 0),
                ConnectionDirection.Down => new Vector3Int(0, -1, 0),
                _ => Vector3Int.zero
            };
        }
        
        #if ODIN_INSPECTOR
        [Button("Generate ID"), BoxGroup("Basic Info")]
        #endif
        private void GenerateId()
        {
            _id = System.Guid.NewGuid().ToString("N")[..8];
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = name;
            }
            
            // Ensure at least one connection
            if (_availableConnections == ConnectionDirection.None)
            {
                Debug.LogWarning($"[RoomTemplate] {name} has no connections defined!");
            }
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    /// <summary>
    /// Defines a connection point on a room.
    /// </summary>
    [System.Serializable]
    public class ConnectionPointData
    {
        public ConnectionDirection Direction;
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        
        #if ODIN_INSPECTOR
        [Tooltip("Size of the door/connection opening")]
        #endif
        public Vector2 OpeningSize = new Vector2(2f, 3f);
        
        #if ODIN_INSPECTOR
        [Tooltip("Tags for this connection (e.g., 'large', 'secret')")]
        #endif
        public List<string> Tags = new List<string>();
    }
    
    /// <summary>
    /// Defines a spawn point in a room.
    /// </summary>
    [System.Serializable]
    public class SpawnPointData
    {
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        
        #if ODIN_INSPECTOR
        [Range(0f, 1f)]
        [Tooltip("Base probability of spawning")]
        #endif
        public float SpawnProbability = 1f;
        
        #if ODIN_INSPECTOR
        [Tooltip("Tags for spawn filtering")]
        #endif
        public List<string> Tags = new List<string>();
        
        #if ODIN_INSPECTOR
        [Tooltip("Minimum difficulty for spawning here")]
        #endif
        public Difficulty MinDifficulty = Difficulty.VeryEasy;
    }
    
    #endregion
}
