using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AILevelGenerator.Utils
{
    using Core;
    using Generation;
    
    /// <summary>
    /// Serializes/deserializes level data to JSON for saving and loading.
    /// Supports full level state including rooms, connections, and metadata.
    /// </summary>
    public static class LevelSerializer
    {
        #region Constants
        
        private const string FILE_EXTENSION = ".lvl";
        private const int FORMAT_VERSION = 1;
        
        #endregion
        
        #region Serialization
        
        /// <summary>
        /// Serializes level data to JSON string.
        /// </summary>
        public static string Serialize(LevelData level, bool prettyPrint = false)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }
            
            var wrapper = new LevelDataWrapper
            {
                FormatVersion = FORMAT_VERSION,
                SaveDate = DateTime.UtcNow.ToString("o"),
                Level = level
            };
            
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }
        
        /// <summary>
        /// Deserializes level data from JSON string.
        /// </summary>
        public static LevelData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("JSON string is null or empty");
            }
            
            try
            {
                var wrapper = JsonUtility.FromJson<LevelDataWrapper>(json);
                
                if (wrapper.FormatVersion > FORMAT_VERSION)
                {
                    DebugLogger.LogWarning($"Level format version {wrapper.FormatVersion} is newer than supported {FORMAT_VERSION}");
                }
                
                // Rebuild connections dictionary
                wrapper.Level.RebuildRoomLookup();
                
                return wrapper.Level;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"Failed to deserialize level: {ex.Message}");
                throw;
            }
        }
        
        #endregion
        
        #region File Operations
        
        /// <summary>
        /// Saves level to file.
        /// </summary>
        public static void SaveToFile(LevelData level, string filePath)
        {
            if (!filePath.EndsWith(FILE_EXTENSION))
            {
                filePath += FILE_EXTENSION;
            }
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = Serialize(level, prettyPrint: true);
            File.WriteAllText(filePath, json);
            
            DebugLogger.LogCore($"Level saved to: {filePath}");
        }
        
        /// <summary>
        /// Loads level from file.
        /// </summary>
        public static LevelData LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Level file not found", filePath);
            }
            
            var json = File.ReadAllText(filePath);
            var level = Deserialize(json);
            
            DebugLogger.LogCore($"Level loaded from: {filePath}");
            return level;
        }
        
        /// <summary>
        /// Gets the default save directory.
        /// </summary>
        public static string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "SavedLevels");
        }
        
        /// <summary>
        /// Gets all saved level files.
        /// </summary>
        public static string[] GetSavedLevels()
        {
            var directory = GetSaveDirectory();
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }
            
            return Directory.GetFiles(directory, $"*{FILE_EXTENSION}");
        }
        
        /// <summary>
        /// Deletes a saved level file.
        /// </summary>
        public static bool DeleteSavedLevel(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                DebugLogger.LogCore($"Deleted level: {filePath}");
                return true;
            }
            return false;
        }
        
        #endregion
        
        #region Export Formats
        
        /// <summary>
        /// Exports level to simple CSV format for external tools.
        /// </summary>
        public static string ExportToCSV(LevelData level)
        {
            var sb = new System.Text.StringBuilder();
            
            // Header
            sb.AppendLine("Id,Type,X,Y,Z,Difficulty,Connections");
            
            // Rooms
            foreach (var room in level.Rooms)
            {
                var connections = string.Join(";", room.ConnectedRoomIds);
                sb.AppendLine($"{room.Id},{room.Type},{room.GridPosition.x},{room.GridPosition.y},{room.GridPosition.z},{room.DifficultyModifier},{connections}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Exports level to graph format for visualization.
        /// </summary>
        public static string ExportToGraphML(LevelData level)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");
            sb.AppendLine("  <key id=\"type\" for=\"node\" attr.name=\"type\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"x\" for=\"node\" attr.name=\"x\" attr.type=\"double\"/>");
            sb.AppendLine("  <key id=\"y\" for=\"node\" attr.name=\"y\" attr.type=\"double\"/>");
            sb.AppendLine("  <graph id=\"G\" edgedefault=\"undirected\">");
            
            // Nodes (rooms)
            foreach (var room in level.Rooms)
            {
                sb.AppendLine($"    <node id=\"{room.Id}\">");
                sb.AppendLine($"      <data key=\"type\">{room.Type}</data>");
                sb.AppendLine($"      <data key=\"x\">{room.GridPosition.x}</data>");
                sb.AppendLine($"      <data key=\"y\">{room.GridPosition.z}</data>");
                sb.AppendLine("    </node>");
            }
            
            // Edges (connections)
            var processedEdges = new HashSet<string>();
            foreach (var room in level.Rooms)
            {
                foreach (var connectedId in room.ConnectedRoomIds)
                {
                    var edgeId = room.Id.CompareTo(connectedId) < 0 
                        ? $"{room.Id}_{connectedId}" 
                        : $"{connectedId}_{room.Id}";
                    
                    if (!processedEdges.Contains(edgeId))
                    {
                        sb.AppendLine($"    <edge id=\"{edgeId}\" source=\"{room.Id}\" target=\"{connectedId}\"/>");
                        processedEdges.Add(edgeId);
                    }
                }
            }
            
            sb.AppendLine("  </graph>");
            sb.AppendLine("</graphml>");
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Data Wrapper
        
        [Serializable]
        private class LevelDataWrapper
        {
            public int FormatVersion;
            public string SaveDate;
            public LevelData Level;
        }
        
        #endregion
    }
    
    #region Extensions
    
    public static class LevelDataExtensions
    {
        /// <summary>
        /// Rebuilds internal lookups after deserialization.
        /// </summary>
        public static void RebuildRoomLookup(this LevelData level)
        {
            // Relink entrance/exit by finding by type
            foreach (var room in level.Rooms)
            {
                switch (room.Type)
                {
                    case RoomType.Entrance:
                        level.EntranceRoom = room;
                        break;
                    case RoomType.Exit:
                        level.ExitRoom = room;
                        break;
                    case RoomType.Boss:
                        if (!level.BossRooms.Contains(room))
                            level.BossRooms.Add(room);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Gets level statistics.
        /// </summary>
        public static LevelStats GetStats(this LevelData level)
        {
            var stats = new LevelStats
            {
                RoomCount = level.Rooms.Count,
                Theme = level.Theme,
                Difficulty = level.BaseDifficulty
            };
            
            foreach (var room in level.Rooms)
            {
                stats.ConnectionCount += room.ConnectedRoomIds.Count;
                
                switch (room.Type)
                {
                    case RoomType.Boss: stats.BossRoomCount++; break;
                    case RoomType.Treasure: stats.TreasureRoomCount++; break;
                    case RoomType.Secret: stats.SecretRoomCount++; break;
                }
            }
            
            stats.ConnectionCount /= 2; // Each connection counted twice
            stats.AverageConnections = stats.RoomCount > 0 
                ? (float)stats.ConnectionCount * 2 / stats.RoomCount 
                : 0f;
            
            return stats;
        }
    }
    
    [Serializable]
    public struct LevelStats
    {
        public int RoomCount;
        public int ConnectionCount;
        public float AverageConnections;
        public int BossRoomCount;
        public int TreasureRoomCount;
        public int SecretRoomCount;
        public ThemeType Theme;
        public Difficulty Difficulty;
    }
    
    #endregion
}
