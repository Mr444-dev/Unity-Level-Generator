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
    /// Wave Function Collapse based level generator.
    /// Generates levels by iteratively collapsing cell possibilities based on constraints.
    /// </summary>
    public class WaveFunctionCollapseGenerator : LevelGeneratorBase
    {
        #region Fields
        
        private Cell[,,] _grid;
        private List<IRoomTemplate> _templates;
        private int _width, _height, _depth;
        private Stack<GenerationState> _history;
        
        #endregion
        
        #region Cell Structure
        
        private class Cell
        {
            public Vector3Int Position;
            public HashSet<string> PossibleTemplates;
            public string CollapsedTemplate;
            public bool IsCollapsed;
            public int Entropy => PossibleTemplates?.Count ?? 0;
            
            public Cell(Vector3Int position, IEnumerable<string> templateIds)
            {
                Position = position;
                PossibleTemplates = new HashSet<string>(templateIds);
                IsCollapsed = false;
            }
        }
        
        private struct GenerationState
        {
            public Vector3Int Position;
            public HashSet<string>[] GridState;
            public string ChosenTemplate;
        }
        
        #endregion
        
        #region Constructor
        
        public WaveFunctionCollapseGenerator()
        {
            _history = new Stack<GenerationState>();
            DebugLogger.LogGeneration("WFC Generator initialized");
        }
        
        #endregion
        
        #region Generation Pipeline Implementation
        
        protected override async UniTask InitializeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Initializing WFC grid...");
            
            _width = profile.LevelSize.x;
            _height = profile.LevelSize.y;
            _depth = profile.LevelSize.z;
            
            // Get available templates
            if (TemplateProvider == null)
            {
                throw new InvalidOperationException("TemplateProvider not set");
            }
            
            var allTemplates = TemplateProvider.GetTemplates(RoomType.Normal, profile.Theme);
            _templates = new List<IRoomTemplate>(allTemplates);
            
            if (_templates.Count == 0)
            {
                throw new InvalidOperationException($"No templates found for theme: {profile.Theme}");
            }
            
            // Initialize grid with all possibilities
            _grid = new Cell[_width, _height, _depth];
            var templateIds = new List<string>();
            foreach (var t in _templates) templateIds.Add(t.Id);
            
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        _grid[x, y, z] = new Cell(new Vector3Int(x, y, z), templateIds);
                    }
                }
            }
            
            _history.Clear();
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Grid initialized: {_width}x{_height}x{_depth} with {_templates.Count} templates");
        }
        
        protected override async UniTask GenerateRoomLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Starting WFC propagation...");
            
            int maxIterations = _width * _height * _depth;
            int iterations = 0;
            int roomsGenerated = 0;
            int targetRooms = profile.RoomCount;
            int attemptsRemaining = profile.MaxAttempts;
            
            while (roomsGenerated < targetRooms && iterations < maxIterations && attemptsRemaining > 0)
            {
                token.ThrowIfCancellationRequested();
                
                // Find cell with lowest entropy (most constrained)
                var cell = FindLowestEntropyCell();
                
                if (cell == null)
                {
                    // All cells collapsed or contradiction
                    if (!HasContradiction())
                        break;
                    
                    // Backtrack
                    if (!Backtrack())
                    {
                        attemptsRemaining--;
                        await InitializeLayoutAsync(level, profile, token);
                        roomsGenerated = 0;
                        continue;
                    }
                }
                else
                {
                    // Collapse this cell
                    if (!CollapseCell(cell))
                    {
                        // Contradiction - backtrack
                        if (!Backtrack())
                        {
                            attemptsRemaining--;
                            await InitializeLayoutAsync(level, profile, token);
                            roomsGenerated = 0;
                            continue;
                        }
                    }
                    else
                    {
                        // Propagate constraints
                        Propagate(cell.Position);
                        roomsGenerated++;
                    }
                }
                
                iterations++;
                
                // Update progress and yield periodically
                if (iterations % 10 == 0)
                {
                    float progress = 0.3f + (0.2f * roomsGenerated / targetRooms);
                    SetProgress(progress);
                    await UniTask.Yield(token);
                }
            }
            
            // Convert collapsed cells to rooms
            ConvertGridToRooms(level, profile);
            
            DebugLogger.LogGeneration($"WFC complete: {level.Rooms.Count} rooms generated in {iterations} iterations");
        }
        
        protected override async UniTask GenerateConnectionsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Generating room connections...");
            
            // Build connections based on adjacency
            foreach (var room in level.Rooms)
            {
                token.ThrowIfCancellationRequested();
                
                // Check all 6 adjacent positions
                var directions = new[]
                {
                    (ConnectionDirection.North, new Vector3Int(0, 0, 1)),
                    (ConnectionDirection.South, new Vector3Int(0, 0, -1)),
                    (ConnectionDirection.East, new Vector3Int(1, 0, 0)),
                    (ConnectionDirection.West, new Vector3Int(-1, 0, 0)),
                    (ConnectionDirection.Up, new Vector3Int(0, 1, 0)),
                    (ConnectionDirection.Down, new Vector3Int(0, -1, 0))
                };
                
                foreach (var (dir, offset) in directions)
                {
                    var neighborPos = room.GridPosition + offset;
                    var neighbor = level.Rooms.Find(r => r.GridPosition == neighborPos);
                    
                    if (neighbor != null && !room.ConnectedRoomIds.Contains(neighbor.Id))
                    {
                        // Check compatibility
                        var template = TemplateProvider?.GetTemplate(room.TemplateId);
                        var neighborTemplate = TemplateProvider?.GetTemplate(neighbor.TemplateId);
                        
                        if (template != null && neighborTemplate != null)
                        {
                            if (template.IsCompatibleWith(neighborTemplate, dir))
                            {
                                room.Connections |= dir;
                                room.ConnectedRoomIds.Add(neighbor.Id);
                            }
                        }
                    }
                }
            }
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Connections generated: {CountConnections(level)} total");
        }
        
        protected override async UniTask PlaceSpecialRoomsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Placing special rooms...");
            
            if (level.Rooms.Count == 0)
            {
                throw new InvalidOperationException("No rooms to designate as special");
            }
            
            // Find optimal entrance (edge room with fewest connections)
            var edgeRooms = level.Rooms.FindAll(r => IsEdgeRoom(r, profile.LevelSize));
            if (edgeRooms.Count == 0) edgeRooms = level.Rooms;
            
            level.EntranceRoom = SelectOptimalRoom(edgeRooms, r => -r.ConnectedRoomIds.Count);
            level.EntranceRoom.Type = RoomType.Entrance;
            level.EntranceRoom.IsRequired = true;
            
            // Find optimal exit (furthest from entrance)
            var distances = CalculateDistances(level, level.EntranceRoom);
            
            RoomData furthestRoom = null;
            int maxDistance = 0;
            foreach (var kvp in distances)
            {
                if (kvp.Value > maxDistance && kvp.Key != level.EntranceRoom.Id)
                {
                    maxDistance = kvp.Value;
                    furthestRoom = level.FindRoom(kvp.Key);
                }
            }
            
            level.ExitRoom = furthestRoom ?? level.Rooms[level.Rooms.Count - 1];
            level.ExitRoom.Type = RoomType.Exit;
            level.ExitRoom.IsRequired = true;
            
            // Place boss rooms along the critical path
            if (profile.BossRoomCount > 0)
            {
                var criticalPath = GetCriticalPath(level, distances);
                var bossPositions = GetDistributedPositions(criticalPath, profile.BossRoomCount);
                
                foreach (var pos in bossPositions)
                {
                    if (pos < criticalPath.Count)
                    {
                        var bossRoom = criticalPath[pos];
                        if (bossRoom.Type == RoomType.Normal)
                        {
                            bossRoom.Type = RoomType.Boss;
                            level.BossRooms.Add(bossRoom);
                        }
                    }
                }
            }
            
            // Place treasure rooms based on probability
            var normalRooms = level.Rooms.FindAll(r => r.Type == RoomType.Normal);
            foreach (var room in normalRooms)
            {
                if (RandomFloat() < profile.TreasureRoomChance)
                {
                    room.Type = RoomType.Treasure;
                }
            }
            
            // Place secret rooms
            normalRooms = level.Rooms.FindAll(r => r.Type == RoomType.Normal);
            foreach (var room in normalRooms)
            {
                if (RandomFloat() < profile.SecretRoomChance)
                {
                    room.Type = RoomType.Secret;
                }
            }
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration($"Special rooms: Entrance, Exit, {level.BossRooms.Count} boss, " +
                $"{level.Rooms.FindAll(r => r.Type == RoomType.Treasure).Count} treasure");
        }
        
        protected override async UniTask FinalizeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token)
        {
            DebugLogger.LogGeneration("Finalizing level layout...");
            
            // Calculate world positions
            var cellSize = GenerationSettingsData.Instance.GridCellSize;
            foreach (var room in level.Rooms)
            {
                room.WorldPosition = new Vector3(
                    room.GridPosition.x * cellSize,
                    room.GridPosition.y * cellSize,
                    room.GridPosition.z * cellSize
                );
            }
            
            // Assign difficulty modifiers based on distance from entrance
            if (level.EntranceRoom != null)
            {
                var distances = CalculateDistances(level, level.EntranceRoom);
                foreach (var room in level.Rooms)
                {
                    if (distances.TryGetValue(room.Id, out int distance))
                    {
                        room.DifficultyModifier = Mathf.RoundToInt(
                            distance * profile.DifficultyProgression + 
                            (RandomFloat() - 0.5f) * 2f * GenerationSettingsData.Instance.DifficultyRandomness
                        );
                    }
                }
            }
            
            // Set level metadata
            level.Name = $"{profile.Theme} Level ({level.Rooms.Count} rooms)";
            
            await UniTask.Yield(token);
            DebugLogger.LogGeneration("Level finalized");
        }
        
        #endregion
        
        #region WFC Core Methods
        
        private Cell FindLowestEntropyCell()
        {
            Cell lowestCell = null;
            int lowestEntropy = int.MaxValue;
            
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        var cell = _grid[x, y, z];
                        if (!cell.IsCollapsed && cell.Entropy > 0 && cell.Entropy < lowestEntropy)
                        {
                            // Add slight randomization to break ties
                            if (lowestCell == null || cell.Entropy < lowestEntropy || RandomFloat() < 0.3f)
                            {
                                lowestEntropy = cell.Entropy;
                                lowestCell = cell;
                            }
                        }
                    }
                }
            }
            
            return lowestCell;
        }
        
        private bool CollapseCell(Cell cell)
        {
            if (cell.Entropy == 0) return false;
            
            // Save state for backtracking
            SaveState(cell.Position, cell.PossibleTemplates);
            
            // Select template based on weights
            var weights = new List<float>();
            var options = new List<string>(cell.PossibleTemplates);
            
            foreach (var templateId in options)
            {
                var template = _templates.Find(t => t.Id == templateId);
                weights.Add(template?.Weight ?? 1);
            }
            
            cell.CollapsedTemplate = WeightedRandom(options, weights);
            cell.IsCollapsed = true;
            cell.PossibleTemplates.Clear();
            cell.PossibleTemplates.Add(cell.CollapsedTemplate);
            
            return true;
        }
        
        private void Propagate(Vector3Int startPos)
        {
            var stack = new Stack<Vector3Int>();
            stack.Push(startPos);
            
            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                var cell = GetCell(pos);
                if (cell == null) continue;
                
                // Check all neighbors
                var neighbors = new[]
                {
                    (ConnectionDirection.North, new Vector3Int(0, 0, 1)),
                    (ConnectionDirection.South, new Vector3Int(0, 0, -1)),
                    (ConnectionDirection.East, new Vector3Int(1, 0, 0)),
                    (ConnectionDirection.West, new Vector3Int(-1, 0, 0)),
                    (ConnectionDirection.Up, new Vector3Int(0, 1, 0)),
                    (ConnectionDirection.Down, new Vector3Int(0, -1, 0))
                };
                
                foreach (var (dir, offset) in neighbors)
                {
                    var neighborPos = pos + offset;
                    var neighbor = GetCell(neighborPos);
                    if (neighbor == null || neighbor.IsCollapsed) continue;
                    
                    // Remove incompatible templates from neighbor
                    var toRemove = new List<string>();
                    foreach (var neighborTemplateId in neighbor.PossibleTemplates)
                    {
                        bool anyCompatible = false;
                        foreach (var cellTemplateId in cell.PossibleTemplates)
                        {
                            var cellTemplate = _templates.Find(t => t.Id == cellTemplateId);
                            var neighborTemplate = _templates.Find(t => t.Id == neighborTemplateId);
                            
                            if (cellTemplate != null && neighborTemplate != null)
                            {
                                if (cellTemplate.IsCompatibleWith(neighborTemplate, dir))
                                {
                                    anyCompatible = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!anyCompatible)
                        {
                            toRemove.Add(neighborTemplateId);
                        }
                    }
                    
                    if (toRemove.Count > 0)
                    {
                        foreach (var id in toRemove)
                        {
                            neighbor.PossibleTemplates.Remove(id);
                        }
                        stack.Push(neighborPos);
                    }
                }
            }
        }
        
        private bool HasContradiction()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        var cell = _grid[x, y, z];
                        if (!cell.IsCollapsed && cell.Entropy == 0)
                            return true;
                    }
                }
            }
            return false;
        }
        
        private void SaveState(Vector3Int position, HashSet<string> possibleTemplates)
        {
            var state = new GenerationState
            {
                Position = position,
                GridState = new HashSet<string>[_width * _height * _depth],
                ChosenTemplate = null
            };
            
            int i = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        state.GridState[i++] = new HashSet<string>(_grid[x, y, z].PossibleTemplates);
                    }
                }
            }
            
            _history.Push(state);
        }
        
        private bool Backtrack()
        {
            if (_history.Count == 0) return false;
            
            var state = _history.Pop();
            
            // Restore grid state
            int i = 0;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        _grid[x, y, z].PossibleTemplates = state.GridState[i++];
                        _grid[x, y, z].IsCollapsed = false;
                        _grid[x, y, z].CollapsedTemplate = null;
                    }
                }
            }
            
            // Remove the bad option at the saved position
            var cell = GetCell(state.Position);
            if (cell != null && !string.IsNullOrEmpty(state.ChosenTemplate))
            {
                cell.PossibleTemplates.Remove(state.ChosenTemplate);
            }
            
            DebugLogger.LogGeneration($"Backtracked to position {state.Position}");
            return true;
        }
        
        private Cell GetCell(Vector3Int pos)
        {
            if (pos.x < 0 || pos.x >= _width ||
                pos.y < 0 || pos.y >= _height ||
                pos.z < 0 || pos.z >= _depth)
                return null;
            
            return _grid[pos.x, pos.y, pos.z];
        }
        
        private void ConvertGridToRooms(LevelData level, GenerationProfile profile)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int z = 0; z < _depth; z++)
                    {
                        var cell = _grid[x, y, z];
                        if (cell.IsCollapsed && !string.IsNullOrEmpty(cell.CollapsedTemplate))
                        {
                            var room = new RoomData(cell.CollapsedTemplate, cell.Position)
                            {
                                Type = RoomType.Normal
                            };
                            level.Rooms.Add(room);
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsEdgeRoom(RoomData room, Vector3Int levelSize)
        {
            return room.GridPosition.x == 0 || room.GridPosition.x == levelSize.x - 1 ||
                   room.GridPosition.z == 0 || room.GridPosition.z == levelSize.z - 1;
        }
        
        private RoomData SelectOptimalRoom(List<RoomData> rooms, Func<RoomData, float> scorer)
        {
            if (rooms.Count == 0) return null;
            
            RoomData best = rooms[0];
            float bestScore = scorer(best);
            
            foreach (var room in rooms)
            {
                float score = scorer(room);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = room;
                }
            }
            
            return best;
        }
        
        private Dictionary<string, int> CalculateDistances(LevelData level, RoomData start)
        {
            var distances = new Dictionary<string, int>();
            var queue = new Queue<(RoomData room, int dist)>();
            queue.Enqueue((start, 0));
            
            while (queue.Count > 0)
            {
                var (room, dist) = queue.Dequeue();
                
                if (distances.ContainsKey(room.Id)) continue;
                distances[room.Id] = dist;
                
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    var connected = level.FindRoom(connectedId);
                    if (connected != null && !distances.ContainsKey(connectedId))
                    {
                        queue.Enqueue((connected, dist + 1));
                    }
                }
            }
            
            return distances;
        }
        
        private List<RoomData> GetCriticalPath(LevelData level, Dictionary<string, int> distances)
        {
            var path = new List<RoomData>();
            if (level.ExitRoom == null || level.EntranceRoom == null) return path;
            
            var current = level.ExitRoom;
            while (current != null && current != level.EntranceRoom)
            {
                path.Add(current);
                
                RoomData next = null;
                int minDist = int.MaxValue;
                
                foreach (var connectedId in current.ConnectedRoomIds)
                {
                    if (distances.TryGetValue(connectedId, out int dist) && dist < minDist)
                    {
                        minDist = dist;
                        next = level.FindRoom(connectedId);
                    }
                }
                
                current = next;
            }
            
            if (level.EntranceRoom != null)
                path.Add(level.EntranceRoom);
            
            path.Reverse();
            return path;
        }
        
        private List<int> GetDistributedPositions(List<RoomData> path, int count)
        {
            var positions = new List<int>();
            if (path.Count < 3 || count == 0) return positions;
            
            int step = path.Count / (count + 1);
            for (int i = 1; i <= count; i++)
            {
                positions.Add(i * step);
            }
            
            return positions;
        }
        
        private int CountConnections(LevelData level)
        {
            int count = 0;
            foreach (var room in level.Rooms)
            {
                count += room.ConnectedRoomIds.Count;
            }
            return count / 2; // Each connection counted twice
        }
        
        #endregion
    }
}
