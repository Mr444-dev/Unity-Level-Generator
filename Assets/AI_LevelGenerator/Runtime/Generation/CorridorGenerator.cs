using System;
using System.Collections.Generic;
using UnityEngine;

namespace AILevelGenerator.Generation
{
    using Core;
    using Data;
    
    /// <summary>
    /// Generates corridors between rooms with multiple style options.
    /// Supports straight, L-shaped, curved, and organic corridor types.
    /// </summary>
    public class CorridorGenerator
    {
        #region Enums
        
        public enum CorridorStyle
        {
            Straight,       // Direct A to B
            LShaped,        // Single 90-degree turn
            ZigZag,         // Multiple turns
            Organic,        // Curved/natural looking
            AStar           // Pathfinding based
        }
        
        #endregion
        
        #region Fields
        
        private float _corridorWidth = 2f;
        private float _maxSegmentLength = 10f;
        private int _gridResolution = 1;
        private bool[,] _occupancyGrid;
        private int _gridWidth, _gridHeight;
        
        #endregion
        
        #region Properties
        
        public float CorridorWidth
        {
            get => _corridorWidth;
            set => _corridorWidth = Mathf.Max(0.5f, value);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generates corridor data between two rooms.
        /// </summary>
        public CorridorData GenerateCorridor(
            RoomData roomA, 
            RoomData roomB, 
            CorridorStyle style = CorridorStyle.Straight)
        {
            var corridor = new CorridorData
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                RoomAId = roomA.Id,
                RoomBId = roomB.Id,
                Style = style,
                Width = _corridorWidth
            };
            
            var startPos = roomA.WorldPosition;
            var endPos = roomB.WorldPosition;
            
            corridor.Points = style switch
            {
                CorridorStyle.Straight => GenerateStraightCorridor(startPos, endPos),
                CorridorStyle.LShaped => GenerateLShapedCorridor(startPos, endPos),
                CorridorStyle.ZigZag => GenerateZigZagCorridor(startPos, endPos),
                CorridorStyle.Organic => GenerateOrganicCorridor(startPos, endPos),
                CorridorStyle.AStar => GenerateAStarCorridor(startPos, endPos),
                _ => GenerateStraightCorridor(startPos, endPos)
            };
            
            corridor.Length = CalculateLength(corridor.Points);
            
            return corridor;
        }
        
        /// <summary>
        /// Generates all corridors for a level.
        /// </summary>
        public List<CorridorData> GenerateAllCorridors(
            LevelData level, 
            CorridorStyle defaultStyle = CorridorStyle.LShaped)
        {
            var corridors = new List<CorridorData>();
            var processedPairs = new HashSet<string>();
            
            foreach (var room in level.Rooms)
            {
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    var connectedRoom = level.FindRoom(connectedId);
                    if (connectedRoom == null) continue;
                    
                    // Avoid duplicate corridors
                    var pairKey = room.Id.CompareTo(connectedId) < 0 
                        ? $"{room.Id}_{connectedId}" 
                        : $"{connectedId}_{room.Id}";
                    
                    if (processedPairs.Contains(pairKey)) continue;
                    processedPairs.Add(pairKey);
                    
                    // Determine corridor style based on distance
                    var distance = Vector3.Distance(room.WorldPosition, connectedRoom.WorldPosition);
                    var style = distance < 10f ? CorridorStyle.Straight : defaultStyle;
                    
                    var corridor = GenerateCorridor(room, connectedRoom, style);
                    corridors.Add(corridor);
                }
            }
            
            DebugLogger.LogGeneration($"Generated {corridors.Count} corridors");
            return corridors;
        }
        
        /// <summary>
        /// Initializes occupancy grid for A* pathfinding.
        /// </summary>
        public void InitializeOccupancyGrid(LevelData level, Vector3Int levelSize, int resolution = 1)
        {
            _gridResolution = resolution;
            _gridWidth = levelSize.x * resolution;
            _gridHeight = levelSize.z * resolution;
            _occupancyGrid = new bool[_gridWidth, _gridHeight];
            
            // Mark room positions as occupied
            foreach (var room in level.Rooms)
            {
                int gx = Mathf.RoundToInt(room.GridPosition.x * resolution);
                int gz = Mathf.RoundToInt(room.GridPosition.z * resolution);
                
                // Mark surrounding cells
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int x = Mathf.Clamp(gx + dx, 0, _gridWidth - 1);
                        int z = Mathf.Clamp(gz + dz, 0, _gridHeight - 1);
                        _occupancyGrid[x, z] = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a GameObject with LineRenderer for the corridor.
        /// </summary>
        public GameObject CreateCorridorVisual(CorridorData corridor, Material material = null)
        {
            var go = new GameObject($"Corridor_{corridor.Id}");
            var lr = go.AddComponent<LineRenderer>();
            
            lr.positionCount = corridor.Points.Count;
            lr.SetPositions(corridor.Points.ToArray());
            lr.startWidth = corridor.Width;
            lr.endWidth = corridor.Width;
            lr.useWorldSpace = true;
            
            if (material != null)
            {
                lr.material = material;
            }
            
            return go;
        }
        
        /// <summary>
        /// Creates a mesh for the corridor floor.
        /// </summary>
        public Mesh CreateCorridorMesh(CorridorData corridor)
        {
            var mesh = new Mesh();
            mesh.name = $"CorridorMesh_{corridor.Id}";
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            float halfWidth = corridor.Width / 2f;
            
            for (int i = 0; i < corridor.Points.Count - 1; i++)
            {
                var current = corridor.Points[i];
                var next = corridor.Points[i + 1];
                
                var direction = (next - current).normalized;
                var perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                
                int baseIndex = vertices.Count;
                
                // Add quad vertices
                vertices.Add(current - perpendicular * halfWidth);
                vertices.Add(current + perpendicular * halfWidth);
                vertices.Add(next - perpendicular * halfWidth);
                vertices.Add(next + perpendicular * halfWidth);
                
                // Add triangles
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                
                // UVs
                float segmentLength = Vector3.Distance(current, next);
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, segmentLength));
                uvs.Add(new Vector2(1, segmentLength));
            }
            
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        #endregion
        
        #region Corridor Generation Methods
        
        private List<Vector3> GenerateStraightCorridor(Vector3 start, Vector3 end)
        {
            return new List<Vector3> { start, end };
        }
        
        private List<Vector3> GenerateLShapedCorridor(Vector3 start, Vector3 end)
        {
            var points = new List<Vector3>();
            points.Add(start);
            
            // Choose horizontal or vertical first based on distance
            bool horizontalFirst = Mathf.Abs(end.x - start.x) > Mathf.Abs(end.z - start.z);
            
            if (horizontalFirst)
            {
                points.Add(new Vector3(end.x, start.y, start.z));
            }
            else
            {
                points.Add(new Vector3(start.x, start.y, end.z));
            }
            
            points.Add(end);
            return points;
        }
        
        private List<Vector3> GenerateZigZagCorridor(Vector3 start, Vector3 end)
        {
            var points = new List<Vector3>();
            points.Add(start);
            
            float dx = end.x - start.x;
            float dz = end.z - start.z;
            
            int segments = Mathf.Max(2, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) / _maxSegmentLength));
            
            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector3 basePoint = Vector3.Lerp(start, end, t);
                
                // Alternate between horizontal and vertical movement
                if (i % 2 == 0)
                {
                    basePoint.x = points[points.Count - 1].x;
                }
                else
                {
                    basePoint.z = points[points.Count - 1].z;
                }
                
                points.Add(basePoint);
            }
            
            points.Add(end);
            return points;
        }
        
        private List<Vector3> GenerateOrganicCorridor(Vector3 start, Vector3 end)
        {
            var points = new List<Vector3>();
            
            float distance = Vector3.Distance(start, end);
            int segments = Mathf.Max(4, Mathf.CeilToInt(distance / 3f));
            
            // Use Bezier curve with random control points
            var midPoint = (start + end) / 2f;
            var perpendicular = Vector3.Cross((end - start).normalized, Vector3.up);
            float offset = distance * 0.2f * (UnityEngine.Random.value - 0.5f);
            var control = midPoint + perpendicular * offset;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                
                // Quadratic Bezier
                Vector3 point = 
                    (1 - t) * (1 - t) * start +
                    2 * (1 - t) * t * control +
                    t * t * end;
                
                point.y = Mathf.Lerp(start.y, end.y, t);
                points.Add(point);
            }
            
            return points;
        }
        
        private List<Vector3> GenerateAStarCorridor(Vector3 start, Vector3 end)
        {
            if (_occupancyGrid == null)
            {
                return GenerateLShapedCorridor(start, end);
            }
            
            var startGrid = WorldToGrid(start);
            var endGrid = WorldToGrid(end);
            
            var path = FindPath(startGrid, endGrid);
            
            if (path.Count == 0)
            {
                return GenerateLShapedCorridor(start, end);
            }
            
            var points = new List<Vector3>();
            foreach (var gridPoint in path)
            {
                points.Add(GridToWorld(gridPoint, start.y));
            }
            
            return SimplifyPath(points, 0.1f);
        }
        
        #endregion
        
        #region A* Pathfinding
        
        private Vector2Int WorldToGrid(Vector3 world)
        {
            return new Vector2Int(
                Mathf.RoundToInt(world.x * _gridResolution),
                Mathf.RoundToInt(world.z * _gridResolution)
            );
        }
        
        private Vector3 GridToWorld(Vector2Int grid, float y)
        {
            return new Vector3(
                grid.x / (float)_gridResolution,
                y,
                grid.y / (float)_gridResolution
            );
        }
        
        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();
            
            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);
            openSet.Add(new PathNode(start, fScore[start]));
            
            var neighbors = new[] {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            
            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) => a.FScore.CompareTo(b.FScore));
                var current = openSet[0].Position;
                
                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }
                
                openSet.RemoveAt(0);
                closedSet.Add(current);
                
                foreach (var offset in neighbors)
                {
                    var neighbor = current + offset;
                    
                    if (closedSet.Contains(neighbor)) continue;
                    if (!IsWalkable(neighbor)) continue;
                    
                    float tentativeG = gScore[current] + 1;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
                        
                        if (!openSet.Exists(n => n.Position == neighbor))
                        {
                            openSet.Add(new PathNode(neighbor, fScore[neighbor]));
                        }
                    }
                }
            }
            
            return new List<Vector2Int>();
        }
        
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        private bool IsWalkable(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= _gridWidth || pos.y < 0 || pos.y >= _gridHeight)
                return false;
            return !_occupancyGrid[pos.x, pos.y];
        }
        
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }
        
        private List<Vector3> SimplifyPath(List<Vector3> path, float tolerance)
        {
            if (path.Count <= 2) return path;
            
            var simplified = new List<Vector3> { path[0] };
            Vector3 lastDir = Vector3.zero;
            
            for (int i = 1; i < path.Count; i++)
            {
                var dir = (path[i] - path[i - 1]).normalized;
                if (Vector3.Distance(dir, lastDir) > tolerance || i == path.Count - 1)
                {
                    simplified.Add(path[i]);
                    lastDir = dir;
                }
            }
            
            return simplified;
        }
        
        private struct PathNode
        {
            public Vector2Int Position;
            public float FScore;
            
            public PathNode(Vector2Int pos, float f)
            {
                Position = pos;
                FScore = f;
            }
        }
        
        #endregion
        
        #region Utility
        
        private float CalculateLength(List<Vector3> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }
            return length;
        }
        
        #endregion
    }
    
    #region Data Structures
    
    /// <summary>
    /// Data representing a corridor connection between two rooms.
    /// </summary>
    [Serializable]
    public class CorridorData
    {
        public string Id;
        public string RoomAId;
        public string RoomBId;
        public CorridorGenerator.CorridorStyle Style;
        public List<Vector3> Points = new List<Vector3>();
        public float Width;
        public float Length;
        public bool HasDoor;
    }
    
    #endregion
}
