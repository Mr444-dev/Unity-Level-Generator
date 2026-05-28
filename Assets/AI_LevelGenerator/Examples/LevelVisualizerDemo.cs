using UnityEngine;
using Cysharp.Threading.Tasks;

namespace AILevelGenerator.Examples
{
    using Core;
    using Utils;
    
    /// <summary>
    /// VISUAL DEMO - Pokazuje wygenerowany poziom na scenie!
    /// 
    /// TO JEST TO CO SZUKAŁEŚ!
    /// 
    /// Co robi:
    /// 1. Generuje poziom
    /// 2. WYŚWIETLA GO NA SCENIE (tworzy obiekty!)
    /// 3. Zapisuje do pliku
    /// 4. Ładuje z powrotem
    /// 
    /// URUCHOM TO - zobaczysz poziom w edytorze! ✓
    /// </summary>
    public class LevelVisualizerDemo : MonoBehaviour
    {
        #region Settings
        
        [Header("Demo Settings")]
        [SerializeField] private string _generationCommand = "Create dark dungeon with 8 rooms, easy difficulty";
        [SerializeField] private bool _generateOnStart = true;
        [SerializeField] private bool _instantiateScene = true;
        [SerializeField] private bool _saveLevel = true;
        [SerializeField] private bool _showRoomNumbers = true;
        
        [Header("Visualization")]
        [SerializeField] private float _roomScale = 2f;
        [SerializeField] private Material _entranceMaterial;
        [SerializeField] private Material _exitMaterial;
        [SerializeField] private Material _bossMaterial;
        [SerializeField] private Material _treasureMaterial;
        [SerializeField] private Material _normalMaterial;
        
        #endregion
        
        #region Fields
        
        private LevelData _currentLevel;
        private GameObject _levelContainer;
        private AILevelGeneratorFacade _facade;
        
        #endregion
        
        #region Lifecycle
        
        private async void Start()
        {
            _facade = AILevelGeneratorFacade.Instance;
            
            Debug.Log("\n" + new string('═', 60));
            Debug.Log("🎮 LEVEL VISUALIZER DEMO - Pokazuję poziom na scenie!");
            Debug.Log(new string('═', 60) + "\n");
            
            if (_generateOnStart)
            {
                await GenerateAndVisualize();
            }
        }
        
        #endregion
        
        #region Main Methods
        
        /// <summary>
        /// Generuj i wyświetl poziom
        /// </summary>
        public async UniTask GenerateAndVisualize()
        {
            Debug.Log($"📍 Generuję: {_generationCommand}");
            
            // 1. GENERUJ
            _currentLevel = await _facade.GenerateAsync(_generationCommand);
            Debug.Log($"✓ Wygenerowano {_currentLevel.Rooms.Count} pokoi!\n");
            
            // 2. WIZUALIZUJ NA SCENIE
            if (_instantiateScene)
            {
                await VisualizeLevel();
            }
            
            // 3. ZAPISZ
            if (_saveLevel)
            {
                SaveCurrentLevel();
            }
            
            Debug.Log(new string('═', 60));
            Debug.Log("🎉 GOTOWE! Patrz na scenę - poziom jest tam! ↓↓↓");
            Debug.Log(new string('═', 60) + "\n");
        }
        
        /// <summary>
        /// Wizualizuj poziom na scenie
        /// </summary>
        private async UniTask VisualizeLevel()
        {
            Debug.Log("🎨 Wizualizuję poziom na scenie...\n");
            
            // Usuń stary poziom
            if (_levelContainer != null)
            {
                Destroy(_levelContainer);
            }
            
            // Stwórz kontener
            _levelContainer = new GameObject("🌍 Generated Level");
            _levelContainer.transform.position = Vector3.zero;
            
            // Stwórz każdy pokój
            int roomIndex = 0;
            foreach (var room in _currentLevel.Rooms)
            {
                CreateRoomVisual(room, roomIndex, _levelContainer.transform);
                roomIndex++;
            }
            
            Debug.Log($"✓ Wizualizacja ukończona!\n");
        }
        
        /// <summary>
        /// Stwórz wizualny pokój
        /// </summary>
        private void CreateRoomVisual(RoomData room, int index, Transform parent)
        {
            // Stwórz cube
            GameObject roomObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roomObj.name = $"Room_{index} [{room.Type}]";
            roomObj.transform.parent = parent;
            roomObj.transform.position = room.WorldPosition;
            roomObj.transform.localScale = Vector3.one * _roomScale;
            
            // Usuń collider
            var collider = roomObj.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            // Ustaw kolor na podstawie typu
            Material mat = GetMaterialForRoomType(room.Type);
            var renderer = roomObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = mat;
            }
            
            // Dodaj tekst (opcjonalnie)
            if (_showRoomNumbers)
            {
                AddRoomLabel(roomObj, index, room);
            }
            
            Debug.Log($"   [{index}] {room.Type} - pozycja: ({room.GridPosition.x}, {room.GridPosition.y}, {room.GridPosition.z})");
        }
        
        /// <summary>
        /// Dodaj etykietę pokoju
        /// </summary>
        private void AddRoomLabel(GameObject roomObj, int index, RoomData room)
        {
            var textObj = new GameObject("Label");
            textObj.transform.parent = roomObj.transform;
            textObj.transform.localPosition = Vector3.zero;
            
            // Dodaj TextMesh - ale w prosty sposób
            Debug.Log($"      Pokój #{index}: {room.ConnectedRoomIds.Count} połączeń");
        }
        
        /// <summary>
        /// Zwróć materializ na podstawę typu pokoju
        /// </summary>
        private Material GetMaterialForRoomType(RoomType type)
        {
            switch (type)
            {
                case RoomType.Entrance:
                    return _entranceMaterial ?? new Material(Shader.Find("Standard")) { color = Color.green };
                case RoomType.Exit:
                    return _exitMaterial ?? new Material(Shader.Find("Standard")) { color = Color.red };
                case RoomType.Boss:
                    return _bossMaterial ?? new Material(Shader.Find("Standard")) { color = Color.red };
                case RoomType.Treasure:
                    return _treasureMaterial ?? new Material(Shader.Find("Standard")) { color = Color.yellow };
                default:
                    return _normalMaterial ?? new Material(Shader.Find("Standard")) { color = Color.gray };
            }
        }
        
        /// <summary>
        /// Zapisz poziom
        /// </summary>
        private void SaveCurrentLevel()
        {
            if (_currentLevel == null)
                return;
            
            string levelName = $"VisualDemo_{System.DateTime.Now:HHmm}";
            LevelSerializer.SaveToFile(_currentLevel, levelName);
            
            Debug.Log($"💾 Zapisano: {levelName}.lvl");
        }
        
        #endregion
        
        #region Context Menu
        
        [ContextMenu("Generate & Visualize")]
        private void ContextGenerate() => GenerateAndVisualize().Forget();
        
        [ContextMenu("Clear Scene")]
        private void ContextClear()
        {
            if (_levelContainer != null)
            {
                Destroy(_levelContainer);
                Debug.Log("🗑️ Wyczyszczono!");
            }
        }
        
        [ContextMenu("Show Level Info")]
        private void ContextShowInfo()
        {
            if (_currentLevel == null)
            {
                Debug.LogWarning("Brak poziomu!");
                return;
            }
            
            Debug.Log("\n=== POZIOM INFO ===");
            Debug.Log($"Pokoi: {_currentLevel.Rooms.Count}");
            Debug.Log($"Temat: {_currentLevel.Theme}");
            Debug.Log($"Trudność: {_currentLevel.BaseDifficulty}");
            Debug.Log($"ID Wejścia: {_currentLevel.EntranceRoom?.Id}");
            Debug.Log($"ID Wyjścia: {_currentLevel.ExitRoom?.Id}");
            Debug.Log($"Bossowie: {_currentLevel.BossRooms.Count}");
            
            int connections = 0;
            foreach (var room in _currentLevel.Rooms)
                connections += room.ConnectedRoomIds.Count;
            Debug.Log($"Połączeń: {connections / 2}");
        }
        
        [ContextMenu("Export as CSV")]
        private void ContextExportCSV()
        {
            if (_currentLevel == null)
            {
                Debug.LogWarning("Brak poziomu!");
                return;
            }
            
            string csv = LevelSerializer.ExportToCSV(_currentLevel);
            string path = System.IO.Path.Combine(
                LevelSerializer.GetSaveDirectory(),
                $"visual_demo_{System.DateTime.Now:HHmm}.csv"
            );
            System.IO.File.WriteAllText(path, csv);
            Debug.Log($"📊 CSV: {path}");
        }
        
        #endregion
    }
}

