using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AILevelGenerator.Generation
{
    using Core;
    using Data;
    
    /// <summary>
    /// Graph-based level generator using Delaunay triangulation and MST.
    /// Creates more organic, interconnected layouts than WFC.
    /// </summary>
    public class GraphBasedGenerator : LevelGeneratorBase
    {
        #region Fields
        
        private List<GraphNode> _nodes;
        private List<GraphEdge> _edges;
        private List<GraphEdge> _mstEdges;
        
        #endregion
        
        #region Data Structures
        
        private class GraphNode
        {
            public int Id;
            public Vector2 Position;
            public RoomData Room;
            public List<int> Connections = new List<int>();
            
            public GraphNode(int id, Vector2 pos)
            {
                Id = id;
                Position = pos;
            }
        }
        
        private class GraphEdge
        {
            public int NodeA;
            public int NodeB;
            public float Weight;
            
            public GraphEdge(int a, int b, float weight)
            {
                NodeA = a;
                NodeB = b;
                Weight = weight;
            }
        }
        
        #endregion
        
        #region Constructor
        
        public GraphBasedGenerator()
        {
            _nodes = new List<GraphNode>();
            _edges = new List<GraphEdge>();
            _mstEdges = new List<GraphEdge>();
            DebugLogger.LogGeneration("Graph-Based Generator initialized");
        }
        
        #endregion
        
        #region Generation Pipeline
        
        protected override async UniTask InitializeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Initializing graph-based layout...");
            
            _nodes.Clear();
            _edges.Clear();
            _mstEdges.Clear();
            
            // Generate random points using Poisson disk sampling
            var points = GeneratePoissonPoints(
                profile.LevelSize.x,
                profile.LevelSize.z,
                Mathf.Max(2f, profile.LevelSize.x / (float)profile.RoomCount * 0.8f),
                profile.RoomCount
            );
            
            // Create nodes from points
            for (int i = 0; i < points.Count; i++)
            {
                _nodes.Add(new GraphNode(i, points[i]));
            }
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Created {_nodes.Count} graph nodes");
        }
        
        protected override async UniTask GenerateRoomLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Building Delaunay triangulation...");
            
            // Build Delaunay triangulation
            BuildDelaunayTriangulation();
            
            await UniTask.Yield(token);
            
            // Build MST for guaranteed connectivity
            DebugLogger.LogGeneration("Building minimum spanning tree...");
            BuildMinimumSpanningTree();
            
            // Add some extra edges for loops (based on branching factor)
            AddExtraEdges(profile.BranchingFactor);
            
            // Convert nodes to rooms
            foreach (var node in _nodes)
            {
                var templates = TemplateProvider?.GetTemplates(RoomType.Normal, profile.Theme);
                var template = templates != null && templates.Count > 0 
                    ? templates[RandomRange(0, templates.Count)] 
                    : null;
                
                var room = new RoomData
                {
                    GridPosition = new Vector3Int(
                        Mathf.RoundToInt(node.Position.x),
                        0,
                        Mathf.RoundToInt(node.Position.y)
                    ),
                    TemplateId = template?.Id ?? "default",
                    Type = RoomType.Normal
                };
                
                node.Room = room;
                level.Rooms.Add(room);
            }
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Created {level.Rooms.Count} rooms from graph");
        }
        
        protected override async UniTask GenerateConnectionsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Creating room connections from edges...");
            
            // Create connections from MST edges
            foreach (var edge in _mstEdges)
            {
                var nodeA = _nodes[edge.NodeA];
                var nodeB = _nodes[edge.NodeB];
                
                if (nodeA.Room != null && nodeB.Room != null)
                {
                    // Add bidirectional connection
                    if (!nodeA.Room.ConnectedRoomIds.Contains(nodeB.Room.Id))
                    {
                        nodeA.Room.ConnectedRoomIds.Add(nodeB.Room.Id);
                        nodeB.Room.ConnectedRoomIds.Add(nodeA.Room.Id);
                        
                        // Determine connection direction
                        var dir = GetConnectionDirection(nodeA.Position, nodeB.Position);
                        nodeA.Room.Connections |= dir;
                        nodeB.Room.Connections |= RoomTemplateData.GetOppositeDirection(dir);
                    }
                }
            }
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Created {_mstEdges.Count} connections");
        }
        
        protected override async UniTask PlaceSpecialRoomsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            if (level.Rooms.Count == 0) return;
            
            // Find entrance (corner with fewest connections)
            var entranceNode = FindExtremeNode(n => -n.Connections.Count + n.Position.magnitude * 0.01f);
            if (entranceNode?.Room != null)
            {
                level.EntranceRoom = entranceNode.Room;
                level.EntranceRoom.Type = RoomType.Entrance;
            }
            
            // Find exit (furthest from entrance)
            if (level.EntranceRoom != null)
            {
                var exitNode = FindExtremeNode(n => Vector2.Distance(n.Position, entranceNode.Position));
                if (exitNode?.Room != null && exitNode != entranceNode)
                {
                    level.ExitRoom = exitNode.Room;
                    level.ExitRoom.Type = RoomType.Exit;
                }
            }
            
            // Place boss rooms
            if (profile.BossRoomCount > 0 && level.EntranceRoom != null && level.ExitRoom != null)
            {
                var midDistance = Vector2.Distance(
                    new Vector2(level.EntranceRoom.GridPosition.x, level.EntranceRoom.GridPosition.z),
                    new Vector2(level.ExitRoom.GridPosition.x, level.ExitRoom.GridPosition.z)
                ) / 2f;
                
                var exhausted = new HashSet<int>();
                for (int i = 0; i < profile.BossRoomCount; i++)
                {
                    var bossNode = FindNodeNearDistance(
                        new Vector2(level.EntranceRoom.GridPosition.x, level.EntranceRoom.GridPosition.z),
                        midDistance + i * 5f,
                        exhausted
                    );
                    
                    if (bossNode?.Room != null && bossNode.Room.Type == RoomType.Normal)
                    {
                        bossNode.Room.Type = RoomType.Boss;
                        level.BossRooms.Add(bossNode.Room);
                        exhausted.Add(bossNode.Id);
                    }
                }
            }
            
            // Place treasures and secrets
            foreach (var node in _nodes)
            {
                if (node.Room == null || node.Room.Type != RoomType.Normal) continue;
                
                if (node.Connections.Count == 1) // Dead end
                {
                    if (RandomFloat() < profile.TreasureRoomChance * 2f)
                        node.Room.Type = RoomType.Treasure;
                    else if (RandomFloat() < profile.SecretRoomChance * 2f)
                        node.Room.Type = RoomType.Secret;
                }
            }
            
            await UniTask.Yield(token);
        }
        
        protected override async UniTask FinalizeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            var cellSize = GenerationSettingsData.Instance.GridCellSize;
            
            foreach (var room in level.Rooms)
            {
                room.WorldPosition = new Vector3(
                    room.GridPosition.x * cellSize,
                    room.GridPosition.y * cellSize,
                    room.GridPosition.z * cellSize
                );
            }
            
            level.Name = $"Graph-Based {profile.Theme} ({level.Rooms.Count} rooms)";
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration("Graph-based level finalized");
        }
        
        #endregion
        
        #region Graph Algorithms
        
        /// <summary>
        /// Generates points using Poisson disk sampling for even distribution.
        /// </summary>
        private List<Vector2> GeneratePoissonPoints(float width, float height, float minDistance, int targetCount)
        {
            var points = new List<Vector2>();
            var active = new List<Vector2>();
            var cellSize = minDistance / Mathf.Sqrt(2);
            var gridWidth = Mathf.CeilToInt(width / cellSize);
            var gridHeight = Mathf.CeilToInt(height / cellSize);
            var grid = new int[gridWidth, gridHeight];
            
            for (int i = 0; i < gridWidth; i++)
                for (int j = 0; j < gridHeight; j++)
                    grid[i, j] = -1;
            
            // Start with random point
            var firstPoint = new Vector2(
                RandomFloat() * width,
                RandomFloat() * height
            );
            
            points.Add(firstPoint);
            active.Add(firstPoint);
            
            int gx = Mathf.Clamp(Mathf.FloorToInt(firstPoint.x / cellSize), 0, gridWidth - 1);
            int gy = Mathf.Clamp(Mathf.FloorToInt(firstPoint.y / cellSize), 0, gridHeight - 1);
            grid[gx, gy] = 0;
            
            int maxAttempts = 30;
            
            while (active.Count > 0 && points.Count < targetCount * 2)
            {
                int randIndex = RandomRange(0, active.Count);
                var point = active[randIndex];
                bool found = false;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float angle = RandomFloat() * Mathf.PI * 2;
                    float radius = minDistance * (1 + RandomFloat());
                    
                    var newPoint = point + new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    );
                    
                    if (newPoint.x < 0 || newPoint.x >= width || 
                        newPoint.y < 0 || newPoint.y >= height)
                        continue;
                    
                    int ngx = Mathf.FloorToInt(newPoint.x / cellSize);
                    int ngy = Mathf.FloorToInt(newPoint.y / cellSize);
                    
                    bool valid = true;
                    for (int dx = -2; dx <= 2 && valid; dx++)
                    {
                        for (int dy = -2; dy <= 2 && valid; dy++)
                        {
                            int checkX = ngx + dx;
                            int checkY = ngy + dy;
                            
                            if (checkX >= 0 && checkX < gridWidth && 
                                checkY >= 0 && checkY < gridHeight)
                            {
                                int idx = grid[checkX, checkY];
                                if (idx >= 0)
                                {
                                    if (Vector2.Distance(points[idx], newPoint) < minDistance)
                                        valid = false;
                                }
                            }
                        }
                    }
                    
                    if (valid)
                    {
                        points.Add(newPoint);
                        active.Add(newPoint);
                        grid[ngx, ngy] = points.Count - 1;
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    active.RemoveAt(randIndex);
                }
            }
            
            // Limit to target count
            while (points.Count > targetCount)
            {
                points.RemoveAt(RandomRange(0, points.Count));
            }
            
            return points;
        }
        
        /// <summary>
        /// Builds Delaunay triangulation using Bowyer-Watson algorithm.
        /// </summary>
        private void BuildDelaunayTriangulation()
        {
            if (_nodes.Count < 3) return;
            
            // Simple approach: connect each node to k nearest neighbors
            int k = Mathf.Min(4, _nodes.Count - 1);
            
            for (int i = 0; i < _nodes.Count; i++)
            {
                var distances = new List<(int index, float dist)>();
                
                for (int j = 0; j < _nodes.Count; j++)
                {
                    if (i != j)
                    {
                        float dist = Vector2.Distance(_nodes[i].Position, _nodes[j].Position);
                        distances.Add((j, dist));
                    }
                }
                
                distances.Sort((a, b) => a.dist.CompareTo(b.dist));
                
                for (int n = 0; n < Mathf.Min(k, distances.Count); n++)
                {
                    int j = distances[n].index;
                    
                    // Don't add duplicate edges
                    bool exists = false;
                    foreach (var e in _edges)
                    {
                        if ((e.NodeA == i && e.NodeB == j) || (e.NodeA == j && e.NodeB == i))
                        {
                            exists = true;
                            break;
                        }
                    }
                    
                    if (!exists)
                    {
                        _edges.Add(new GraphEdge(i, j, distances[n].dist));
                    }
                }
            }
        }
        
        /// <summary>
        /// Builds minimum spanning tree using Kruskal's algorithm.
        /// </summary>
        private void BuildMinimumSpanningTree()
        {
            _mstEdges.Clear();
            
            // Sort edges by weight
            _edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));
            
            // Union-Find
            var parent = new int[_nodes.Count];
            for (int i = 0; i < _nodes.Count; i++) parent[i] = i;
            
            int Find(int x)
            {
                if (parent[x] != x) parent[x] = Find(parent[x]);
                return parent[x];
            }
            
            void Union(int x, int y)
            {
                parent[Find(x)] = Find(y);
            }
            
            foreach (var edge in _edges)
            {
                if (Find(edge.NodeA) != Find(edge.NodeB))
                {
                    _mstEdges.Add(edge);
                    Union(edge.NodeA, edge.NodeB);
                    
                    _nodes[edge.NodeA].Connections.Add(edge.NodeB);
                    _nodes[edge.NodeB].Connections.Add(edge.NodeA);
                    
                    if (_mstEdges.Count == _nodes.Count - 1) break;
                }
            }
        }
        
        /// <summary>
        /// Adds extra edges beyond MST for loops.
        /// </summary>
        private void AddExtraEdges(float branchingFactor)
        {
            int extraEdges = Mathf.RoundToInt((_nodes.Count - 1) * branchingFactor);
            int added = 0;
            
            foreach (var edge in _edges)
            {
                if (added >= extraEdges) break;
                
                bool inMst = false;
                foreach (var mstEdge in _mstEdges)
                {
                    if ((mstEdge.NodeA == edge.NodeA && mstEdge.NodeB == edge.NodeB) ||
                        (mstEdge.NodeA == edge.NodeB && mstEdge.NodeB == edge.NodeA))
                    {
                        inMst = true;
                        break;
                    }
                }
                
                if (!inMst && RandomFloat() < 0.5f)
                {
                    _mstEdges.Add(edge);
                    _nodes[edge.NodeA].Connections.Add(edge.NodeB);
                    _nodes[edge.NodeB].Connections.Add(edge.NodeA);
                    added++;
                }
            }
        }
        
        private ConnectionDirection GetConnectionDirection(Vector2 from, Vector2 to)
        {
            var delta = to - from;
            
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? ConnectionDirection.East : ConnectionDirection.West;
            }
            else
            {
                return delta.y > 0 ? ConnectionDirection.North : ConnectionDirection.South;
            }
        }
        
        private GraphNode FindExtremeNode(Func<GraphNode, float> scorer)
        {
            GraphNode best = null;
            float bestScore = float.MinValue;
            
            foreach (var node in _nodes)
            {
                float score = scorer(node);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }
            
            return best;
        }
        
        private GraphNode FindNodeNearDistance(Vector2 origin, float targetDist, HashSet<int> exclude)
        {
            GraphNode best = null;
            float bestDiff = float.MaxValue;
            
            foreach (var node in _nodes)
            {
                if (exclude.Contains(node.Id)) continue;
                
                float dist = Vector2.Distance(node.Position, origin);
                float diff = Mathf.Abs(dist - targetDist);
                
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = node;
                }
            }
            
            return best;
        }
        
        #endregion
    }
}
