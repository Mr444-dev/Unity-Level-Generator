using System;
using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Generation
{
    using Core;
    using Data;
    
    /// <summary>
    /// Decorates rooms with props, furniture, and environmental details.
    /// Uses weighted random placement based on room type and theme.
    /// </summary>
    public class RoomDecorator
    {
        #region Enums
        
        public enum DecorationType
        {
            Floor,      // Tables, rugs, chests
            Wall,       // Torches, banners, shelves
            Ceiling,    // Chandeliers, hanging objects
            Corner,     // Statues, pillars
            Center,     // Fountains, altars
            Random      // Anywhere valid
        }
        
        public enum DecorationDensity
        {
            Sparse = 1,
            Light = 2,
            Normal = 3,
            Dense = 4,
            Cluttered = 5
        }
        
        #endregion
        
        #region Fields
        
        private System.Random _random;
        private Dictionary<ThemeType, List<DecorationSet>> _decorationSets;
        
        #endregion
        
        #region Data Structures
        
        [Serializable]
        public class DecorationSet
        {
            public string Name;
            public ThemeType Theme;
            public RoomType[] ValidRoomTypes;
            public List<DecorationItem> Items = new List<DecorationItem>();
        }
        
        [Serializable]
        public class DecorationItem
        {
            public GameObject Prefab;
            public DecorationType Type;
            public float Weight = 1f;
            public float MinScale = 0.9f;
            public float MaxScale = 1.1f;
            public bool RandomRotation = true;
            public float MinSpacing = 1f;
            public int MaxPerRoom = 5;
        }
        
        [Serializable]
        public class PlacedDecoration
        {
            public string ItemName;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public DecorationType Type;
        }
        
        #endregion
        
        #region Constructor
        
        public RoomDecorator(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            _decorationSets = new Dictionary<ThemeType, List<DecorationSet>>();
            DebugLogger.LogGeneration("RoomDecorator initialized");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Registers a decoration set for a theme.
        /// </summary>
        public void RegisterDecorationSet(DecorationSet set)
        {
            if (!_decorationSets.ContainsKey(set.Theme))
            {
                _decorationSets[set.Theme] = new List<DecorationSet>();
            }
            _decorationSets[set.Theme].Add(set);
        }
        
        /// <summary>
        /// Decorates a single room based on type and theme.
        /// </summary>
        public List<PlacedDecoration> DecorateRoom(
            RoomData room, 
            RoomTemplateData template,
            ThemeType theme,
            DecorationDensity density = DecorationDensity.Normal)
        {
            var decorations = new List<PlacedDecoration>();
            
            if (!_decorationSets.TryGetValue(theme, out var sets) || sets.Count == 0)
            {
                // Use procedural placement if no sets defined
                return GenerateProceduralDecorations(room, template, density);
            }
            
            // Find valid decoration sets for this room type
            var validSets = sets.FindAll(s => 
                s.ValidRoomTypes == null || 
                s.ValidRoomTypes.Length == 0 || 
                Array.Exists(s.ValidRoomTypes, t => t == room.Type));
            
            if (validSets.Count == 0) return decorations;
            
            // Calculate number of decorations based on room size and density
            int targetCount = CalculateDecorationCount(template.Size, density);
            
            // Place decorations
            var placedPositions = new List<Vector3>();
            
            for (int i = 0; i < targetCount * 3 && decorations.Count < targetCount; i++)
            {
                var set = validSets[_random.Next(validSets.Count)];
                var item = SelectWeightedItem(set.Items);
                if (item == null || item.Prefab == null) continue;
                
                // Find valid position
                var position = FindDecorationPosition(room, template, item.Type, item.MinSpacing, placedPositions);
                if (!position.HasValue) continue;
                
                // Check max per room
                int existingCount = decorations.FindAll(d => d.ItemName == item.Prefab.name).Count;
                if (existingCount >= item.MaxPerRoom) continue;
                
                var decoration = new PlacedDecoration
                {
                    ItemName = item.Prefab.name,
                    Position = position.Value,
                    Rotation = item.RandomRotation 
                        ? Quaternion.Euler(0, (float)_random.NextDouble() * 360f, 0) 
                        : Quaternion.identity,
                    Scale = Vector3.one * Lerp(item.MinScale, item.MaxScale, (float)_random.NextDouble()),
                    Type = item.Type
                };
                
                decorations.Add(decoration);
                placedPositions.Add(position.Value);
            }
            
            return decorations;
        }
        
        /// <summary>
        /// Decorates all rooms in a level.
        /// </summary>
        public Dictionary<string, List<PlacedDecoration>> DecoratLevel(
            LevelData level,
            Func<RoomData, RoomTemplateData> templateProvider,
            DecorationDensity density = DecorationDensity.Normal)
        {
            var allDecorations = new Dictionary<string, List<PlacedDecoration>>();
            
            foreach (var room in level.Rooms)
            {
                var template = templateProvider(room);
                if (template == null) continue;
                
                // Adjust density based on room type
                var roomDensity = room.Type switch
                {
                    RoomType.Boss => DecorationDensity.Sparse,
                    RoomType.Corridor => DecorationDensity.Light,
                    RoomType.Treasure => DecorationDensity.Dense,
                    RoomType.Shop => DecorationDensity.Dense,
                    _ => density
                };
                
                var decorations = DecorateRoom(room, template, level.Theme, roomDensity);
                if (decorations.Count > 0)
                {
                    allDecorations[room.Id] = decorations;
                }
            }
            
            DebugLogger.LogGeneration($"Placed {CountDecorations(allDecorations)} decorations in {allDecorations.Count} rooms");
            return allDecorations;
        }
        
        /// <summary>
        /// Instantiates decorations in the scene.
        /// </summary>
        public List<GameObject> InstantiateDecorations(
            List<PlacedDecoration> decorations,
            Dictionary<string, GameObject> prefabLookup,
            Transform parent)
        {
            var instances = new List<GameObject>();
            
            foreach (var decoration in decorations)
            {
                if (!prefabLookup.TryGetValue(decoration.ItemName, out var prefab)) continue;
                
                var instance = UnityEngine.Object.Instantiate(
                    prefab,
                    decoration.Position,
                    decoration.Rotation,
                    parent
                );
                instance.transform.localScale = decoration.Scale;
                instances.Add(instance);
            }
            
            return instances;
        }
        
        #endregion
        
        #region Private Methods
        
        private List<PlacedDecoration> GenerateProceduralDecorations(
            RoomData room, 
            RoomTemplateData template,
            DecorationDensity density)
        {
            var decorations = new List<PlacedDecoration>();
            int count = CalculateDecorationCount(template?.Size ?? Vector3Int.one, density);
            
            // Use spawn points from template if available
            if (template != null)
            {
                foreach (var spawnPoint in template.DecorationSpawnPoints)
                {
                    if (decorations.Count >= count) break;
                    if (_random.NextDouble() > spawnPoint.SpawnProbability) continue;
                    
                    decorations.Add(new PlacedDecoration
                    {
                        ItemName = "Procedural",
                        Position = room.WorldPosition + spawnPoint.LocalPosition,
                        Rotation = Quaternion.Euler(spawnPoint.LocalRotation),
                        Scale = Vector3.one,
                        Type = DecorationType.Random
                    });
                }
            }
            
            return decorations;
        }
        
        private int CalculateDecorationCount(Vector3Int roomSize, DecorationDensity density)
        {
            float volume = roomSize.x * roomSize.y * roomSize.z;
            float baseCount = Mathf.Sqrt(volume) * (int)density;
            return Mathf.Max(1, Mathf.RoundToInt(baseCount));
        }
        
        private DecorationItem SelectWeightedItem(List<DecorationItem> items)
        {
            if (items == null || items.Count == 0) return null;
            
            float totalWeight = 0f;
            foreach (var item in items) totalWeight += item.Weight;
            
            float random = (float)_random.NextDouble() * totalWeight;
            float current = 0f;
            
            foreach (var item in items)
            {
                current += item.Weight;
                if (random <= current) return item;
            }
            
            return items[items.Count - 1];
        }
        
        private Vector3? FindDecorationPosition(
            RoomData room,
            RoomTemplateData template,
            DecorationType type,
            float minSpacing,
            List<Vector3> existingPositions)
        {
            float cellSize = GenerationSettingsData.Instance.GridCellSize;
            var roomCenter = room.WorldPosition;
            var roomSize = template?.Size ?? Vector3Int.one;
            var roomExtents = new Vector3(roomSize.x, roomSize.y, roomSize.z) * cellSize * 0.4f;
            
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector3 position = type switch
                {
                    DecorationType.Floor => roomCenter + new Vector3(
                        ((float)_random.NextDouble() - 0.5f) * 2f * roomExtents.x,
                        0,
                        ((float)_random.NextDouble() - 0.5f) * 2f * roomExtents.z
                    ),
                    DecorationType.Wall => GetWallPosition(roomCenter, roomExtents),
                    DecorationType.Corner => GetCornerPosition(roomCenter, roomExtents),
                    DecorationType.Center => roomCenter,
                    DecorationType.Ceiling => roomCenter + Vector3.up * cellSize * 0.8f,
                    _ => roomCenter + new Vector3(
                        ((float)_random.NextDouble() - 0.5f) * 2f * roomExtents.x,
                        0,
                        ((float)_random.NextDouble() - 0.5f) * 2f * roomExtents.z
                    )
                };
                
                // Check spacing
                bool valid = true;
                foreach (var existing in existingPositions)
                {
                    if (Vector3.Distance(position, existing) < minSpacing)
                    {
                        valid = false;
                        break;
                    }
                }
                
                if (valid) return position;
            }
            
            return null;
        }
        
        private Vector3 GetWallPosition(Vector3 center, Vector3 extents)
        {
            int wall = _random.Next(4);
            float along = (float)_random.NextDouble() - 0.5f;
            
            return wall switch
            {
                0 => center + new Vector3(along * extents.x * 2f, 0, extents.z), // North
                1 => center + new Vector3(along * extents.x * 2f, 0, -extents.z), // South
                2 => center + new Vector3(extents.x, 0, along * extents.z * 2f), // East
                _ => center + new Vector3(-extents.x, 0, along * extents.z * 2f) // West
            };
        }
        
        private Vector3 GetCornerPosition(Vector3 center, Vector3 extents)
        {
            int corner = _random.Next(4);
            float inset = 0.8f;
            
            return corner switch
            {
                0 => center + new Vector3(extents.x * inset, 0, extents.z * inset),
                1 => center + new Vector3(-extents.x * inset, 0, extents.z * inset),
                2 => center + new Vector3(extents.x * inset, 0, -extents.z * inset),
                _ => center + new Vector3(-extents.x * inset, 0, -extents.z * inset)
            };
        }
        
        private int CountDecorations(Dictionary<string, List<PlacedDecoration>> all)
        {
            int count = 0;
            foreach (var list in all.Values) count += list.Count;
            return count;
        }
        
        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
        
        #endregion
    }
}
