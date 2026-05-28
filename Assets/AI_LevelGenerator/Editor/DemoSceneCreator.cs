using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace AILevelGenerator.Editor
{
    using Examples;

    public class DemoSceneCreator : UnityEditor.EditorWindow
    {
        private const string SCENE_DIR = "Assets/AI_LevelGenerator/Scenes";
        private const string SCENE_PATH = "Assets/AI_LevelGenerator/Scenes/DemoScene.unity";

        [MenuItem("Tools/AI Level Generator/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorUtility.DisplayDialog("Create Demo Scene", "This will create and open a new Demo Scene. Any unsaved changes in your current scene will be lost. Do you want to proceed?", "Yes", "No"))
            {
                return;
            }

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(SCENE_DIR))
            {
                string parentFolder = "Assets/AI_LevelGenerator";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "AI_LevelGenerator");
                }
                AssetDatabase.CreateFolder(parentFolder, "Scenes");
            }

            // Create new empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Setup Lighting
            GameObject dirLight = new GameObject("Directional Light");
            Light lightComp = dirLight.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Setup Camera
            GameObject mainCam = new GameObject("Main Camera");
            mainCam.tag = "MainCamera";
            Camera camComp = mainCam.AddComponent<Camera>();
            mainCam.transform.position = new Vector3(0, 10, -10);
            mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);

            // Setup Generator Facade
            GameObject generatorObj = new GameObject("Level Generator");
            var facade = generatorObj.AddComponent<AILevelGeneratorFacade>();
            
            // Setup Test Scene Script
            var testSetup = generatorObj.AddComponent<TestSceneSetup>();
            // Since fields are serialized, they will have their default values
            
            // Save Scene
            bool success = EditorSceneManager.SaveScene(newScene, SCENE_PATH);

            if (success)
            {
                Debug.Log($"[AI Level Generator] Demo scene created and saved at {SCENE_PATH}. Press Play to generate a level!");
            }
            else
            {
                Debug.LogError("[AI Level Generator] Failed to save demo scene.");
            }
        }
    }
}
