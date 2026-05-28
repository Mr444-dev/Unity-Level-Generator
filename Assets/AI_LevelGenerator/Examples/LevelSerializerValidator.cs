using System;
using System.IO;
using UnityEngine;

namespace AILevelGenerator.Examples
{
    using Core;
    using Utils;
    
    /// <summary>
    /// VALIDATION: Sprawdź czy LevelSerializer jest poprawnie zainstalowany
    /// 
    /// Jak użyć:
    /// 1. Dodaj do GameObject w scenie
    /// 2. Play
    /// 3. Sprawdź Console - zobaczy GREEN ✓ jeśli wszystko OK
    /// </summary>
    public class LevelSerializerValidator : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("\n" + new string('═', 60));
            Debug.Log("VALIDATING LevelSerializer Setup...");
            Debug.Log(new string('═', 60) + "\n");
            
            int passed = 0;
            int failed = 0;
            
            // Test 1: Namespace
            if (ValidateNamespace())
                passed++;
            else
                failed++;
            
            // Test 2: Save Directory
            if (ValidateSaveDirectory())
                passed++;
            else
                failed++;
            
            // Test 3: File Operations
            if (ValidateFileOperations())
                passed++;
            else
                failed++;
            
            // Test 4: JSON Serialization
            if (ValidateJsonSerialization())
                passed++;
            else
                failed++;
            
            // Test 5: Export Functions
            if (ValidateExportFunctions())
                passed++;
            else
                failed++;
            
            // Summary
            Debug.Log("\n" + new string('═', 60));
            Debug.Log($"✓ PASSED: {passed}");
            Debug.Log($"✗ FAILED: {failed}");
            
            if (failed == 0)
            {
                Debug.Log("\n🎉 ALL TESTS PASSED! LevelSerializer is ready to use!");
                Debug.Log("\nNext steps:");
                Debug.Log("1. Run LevelSerializerDemo.cs");
                Debug.Log("2. Read LEVEL_SERIALIZER_GUIDE_PL.md");
                Debug.Log("3. Implement in your game!");
            }
            else
            {
                Debug.LogError($"\n⚠️  {failed} test(s) failed! Check errors above.");
            }
            
            Debug.Log(new string('═', 60) + "\n");
        }
        
        private bool ValidateNamespace()
        {
            Debug.Log("1. Checking namespace access...");
            
            try
            {
                // Spróbuj zaacesować StaticLevelSerializer
                var type = Type.GetType("AILevelGenerator.Utils.LevelSerializer");
                if (type != null)
                {
                    Debug.Log("   ✓ LevelSerializer found");
                    return true;
                }
                else
                {
                    Debug.LogError("   ✗ LevelSerializer not found - check namespace");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"   ✗ Error: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateSaveDirectory()
        {
            Debug.Log("2. Checking save directory...");
            
            try
            {
                string saveDir = LevelSerializer.GetSaveDirectory();
                Debug.Log($"   Save directory: {saveDir}");
                
                if (string.IsNullOrEmpty(saveDir))
                {
                    Debug.LogError("   ✗ Save directory is empty");
                    return false;
                }
                
                // Create if not exists
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                    Debug.Log("   ✓ Created save directory");
                }
                else
                {
                    Debug.Log("   ✓ Save directory exists");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"   ✗ Error: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateFileOperations()
        {
            Debug.Log("3. Testing file operations...");
            
            try
            {
                // Wygeneruj dummy level
                var testLevel = new LevelData
                {
                    LevelId = "test_validator_" + Guid.NewGuid().ToString("N")[..8],
                    Name = "Validator Test",
                    Theme = ThemeType.DarkDungeon,
                    BaseDifficulty = Difficulty.Medium,
                    Size = new Vector3Int(10, 1, 10)
                };
                
                // Dodaj test room
                var entranceRoom = new RoomData("entrance_template", Vector3Int.zero)
                {
                    Type = RoomType.Entrance
                };
                var exitRoom = new RoomData("exit_template", new Vector3Int(5, 0, 5))
                {
                    Type = RoomType.Exit
                };
                
                testLevel.Rooms.Add(entranceRoom);
                testLevel.Rooms.Add(exitRoom);
                testLevel.EntranceRoom = entranceRoom;
                testLevel.ExitRoom = exitRoom;
                
                // Test Save
                string testPath = $"validator_test_{Guid.NewGuid().ToString("N")[..8]}";
                LevelSerializer.SaveToFile(testLevel, testPath);
                Debug.Log("   ✓ Save operation successful");
                
                // Test Load
                LevelData loadedLevel = LevelSerializer.LoadFromFile($"{testPath}.lvl");
                if (loadedLevel == null || loadedLevel.LevelId != testLevel.LevelId)
                {
                    Debug.LogError("   ✗ Loaded level doesn't match saved level");
                    return false;
                }
                Debug.Log("   ✓ Load operation successful");
                
                // Cleanup
                LevelSerializer.DeleteSavedLevel($"{testPath}.lvl");
                Debug.Log("   ✓ Delete operation successful");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"   ✗ Error: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateJsonSerialization()
        {
            Debug.Log("4. Testing JSON serialization...");
            
            try
            {
                var testLevel = new LevelData
                {
                    LevelId = Guid.NewGuid().ToString("N"),
                    Name = "JSON Test",
                    Theme = ThemeType.Castle,
                    BaseDifficulty = Difficulty.Hard
                };
                
                // Serialize
                string json = LevelSerializer.Serialize(testLevel, false);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError("   ✗ Serialization produced empty result");
                    return false;
                }
                Debug.Log("   ✓ Serialization successful");
                
                // Deserialize
                LevelData deserialized = LevelSerializer.Deserialize(json);
                if (deserialized.LevelId != testLevel.LevelId)
                {
                    Debug.LogError("   ✗ Deserialization failed");
                    return false;
                }
                Debug.Log("   ✓ Deserialization successful");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"   ✗ Error: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateExportFunctions()
        {
            Debug.Log("5. Testing export functions...");
            
            try
            {
                // Wygeneruj test level z pokojami
                var testLevel = new LevelData
                {
                    LevelId = Guid.NewGuid().ToString("N"),
                    Name = "Export Test",
                    Theme = ThemeType.Cave
                };
                
                // Dodaj pokoje
                for (int i = 0; i < 3; i++)
                {
                    var room = new RoomData($"template_{i}", new Vector3Int(i, 0, 0));
                    if (i == 0)
                        room.Type = RoomType.Entrance;
                    else if (i == 2)
                        room.Type = RoomType.Exit;
                    else
                        room.Type = RoomType.Normal;
                    
                    if (i > 0)
                        room.ConnectedRoomIds.Add("room_0");
                    
                    testLevel.Rooms.Add(room);
                }
                
                testLevel.EntranceRoom = testLevel.Rooms[0];
                testLevel.ExitRoom = testLevel.Rooms[2];
                
                // Test CSV Export
                string csv = LevelSerializer.ExportToCSV(testLevel);
                if (string.IsNullOrEmpty(csv) || !csv.Contains("Id,Type"))
                {
                    Debug.LogError("   ✗ CSV export failed");
                    return false;
                }
                Debug.Log("   ✓ CSV export successful");
                
                // Test GraphML Export
                string graphml = LevelSerializer.ExportToGraphML(testLevel);
                if (string.IsNullOrEmpty(graphml) || !graphml.Contains("<graphml"))
                {
                    Debug.LogError("   ✗ GraphML export failed");
                    return false;
                }
                Debug.Log("   ✓ GraphML export successful");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"   ✗ Error: {ex.Message}");
                return false;
            }
        }
    }
}

