using UnityEngine;
using UnityEditor;
using AILevelGenerator;
using AILevelGenerator.Core;
using AILevelGenerator.Data;

namespace AILevelGenerator.Editor
{
    /// <summary>
    /// Creates example room templates for quick testing.
    /// </summary>
    public static class ExampleTemplateCreator
    {
        [MenuItem("AI Level Generator/Create Example Templates")]
        public static void CreateExampleTemplates()
        {
            var folderPath = "Assets/AI_LevelGenerator/Resources/ExampleTemplates";
            
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/AI_LevelGenerator/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/AI_LevelGenerator", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/AI_LevelGenerator/Resources", "ExampleTemplates");
            }
            
            // Create templates
            CreateTemplate(folderPath, "DungeonRoom_Normal", RoomType.Normal, ThemeType.DarkDungeon, ConnectionDirection.All);
            CreateTemplate(folderPath, "DungeonRoom_Entrance", RoomType.Entrance, ThemeType.DarkDungeon, ConnectionDirection.North | ConnectionDirection.East);
            CreateTemplate(folderPath, "DungeonRoom_Exit", RoomType.Exit, ThemeType.DarkDungeon, ConnectionDirection.South | ConnectionDirection.West);
            CreateTemplate(folderPath, "DungeonRoom_Boss", RoomType.Boss, ThemeType.DarkDungeon, ConnectionDirection.North | ConnectionDirection.South);
            CreateTemplate(folderPath, "DungeonRoom_Treasure", RoomType.Treasure, ThemeType.DarkDungeon, ConnectionDirection.North);
            
            CreateTemplate(folderPath, "CaveRoom_Normal", RoomType.Normal, ThemeType.Cave, ConnectionDirection.All);
            CreateTemplate(folderPath, "CaveRoom_Entrance", RoomType.Entrance, ThemeType.Cave, ConnectionDirection.North | ConnectionDirection.East | ConnectionDirection.West);
            
            CreateTemplate(folderPath, "TempleRoom_Normal", RoomType.Normal, ThemeType.BrightTemple, ConnectionDirection.All);
            CreateTemplate(folderPath, "TempleRoom_Boss", RoomType.Boss, ThemeType.BrightTemple, ConnectionDirection.North | ConnectionDirection.South);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created example templates in {folderPath}");
            EditorUtility.DisplayDialog("Templates Created", 
                $"Created example room templates in:\n{folderPath}\n\nAdd these to your AILevelGeneratorFacade to use them.", 
                "OK");
        }
        
        private static void CreateTemplate(string folderPath, string name, RoomType type, ThemeType theme, ConnectionDirection connections)
        {
            var path = $"{folderPath}/{name}.asset";
            
            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<RoomTemplateData>(path) != null)
            {
                return;
            }
            
            var template = ScriptableObject.CreateInstance<RoomTemplateData>();
            
            // Use reflection to set private fields (since they're serialized)
            var templateType = typeof(RoomTemplateData);
            
            SetPrivateField(template, "_id", name);
            SetPrivateField(template, "_displayName", name.Replace("_", " "));
            SetPrivateField(template, "_type", type);
            SetPrivateField(template, "_theme", theme);
            SetPrivateField(template, "_availableConnections", connections);
            SetPrivateField(template, "_size", new Vector3Int(1, 1, 1));
            SetPrivateField(template, "_weight", type == RoomType.Normal ? 10 : 5);
            
            AssetDatabase.CreateAsset(template, path);
        }
        
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
        
        [MenuItem("AI Level Generator/Setup Scene")]
        public static void SetupScene()
        {
            // Check if facade exists
            var facade = Object.FindObjectOfType<AILevelGeneratorFacade>();
            if (facade == null)
            {
                var go = new GameObject("[AI Level Generator]");
                facade = go.AddComponent<AILevelGeneratorFacade>();
                Debug.Log("Created AILevelGeneratorFacade in scene");
            }
            
            // Load example templates
            var templates = Resources.LoadAll<RoomTemplateData>("ExampleTemplates");
            if (templates.Length > 0)
            {
                var templateList = (System.Collections.Generic.List<RoomTemplateData>)
                    typeof(AILevelGeneratorFacade).GetField("_roomTemplates", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance)?.GetValue(facade);
                
                if (templateList != null)
                {
                    templateList.Clear();
                    templateList.AddRange(templates);
                    Debug.Log($"Loaded {templates.Length} example templates");
                }
            }
            
            Selection.activeGameObject = facade.gameObject;
            EditorUtility.SetDirty(facade);
            
            EditorUtility.DisplayDialog("Scene Setup Complete",
                "AI Level Generator is ready to use!\n\n" +
                "1. Open Window > AI Level Generator\n" +
                "2. Type a command and generate!",
                "OK");
        }
    }
}
