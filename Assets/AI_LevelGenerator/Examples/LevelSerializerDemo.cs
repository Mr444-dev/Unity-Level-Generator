using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace AILevelGenerator.Examples
{
    using Core;
    using Utils;
    
    /// <summary>
    /// DEMO: LevelSerializer - Jak zapisywać i ładować poziomy
    /// 
    /// LevelSerializer automatycznie:
    /// - Konwertuje dane poziomów do JSON
    /// - Zapisuje na dysk
    /// - Ładuje z dysku
    /// - Eksportuje do CSV i GraphML
    /// 
    /// DO CZEGO SŁUŻY:
    /// - Trwałe zapisywanie wygenerowanych poziomów
    /// - Ładowanie wcześniej zapisanych poziomów
    /// - Dzielenie się poziomami między graczami
    /// - Analiza struktur (GraphML dla narzędzi)
    /// </summary>
    public class LevelSerializerDemo : MonoBehaviour
    {
        #region Inspector Settings
        
        [Header("Demo Mode")]
        [SerializeField] private DemoMode _demoMode = DemoMode.SaveAndLoad;
        [SerializeField] private bool _runDemoOnStart = true;
        
        [Header("Save Location")]
        [SerializeField] private string _customSaveName = "MyDemoLevel";
        [SerializeField] private bool _useCustomPath = false;
        [SerializeField] private string _customPath = "";
        
        [Header("Export Options")]
        [SerializeField] private bool _exportAsCSV = true;
        [SerializeField] private bool _exportAsGraphML = true;
        
        [Header("Generation")]
        [SerializeField] private string _generationCommand = "Create dark dungeon with 8 rooms, easy difficulty";
        
        #endregion
        
        #region Enums
        
        public enum DemoMode
        {
            /// <summary> Generować → Zapisać → Załadować </summary>
            SaveAndLoad,
            
            /// <summary> Tylko Pokazać co zostało zapisane </summary>
            ListSavedLevels,
            
            /// <summary> Generować i Eksportować do CSV/GraphML </summary>
            ExportFormats,
            
            /// <summary> Pełny workflow: generacja, zapis, export, analiza </summary>
            FullWorkflow
        }
        
        #endregion
        
        #region Fields
        
        private AILevelGeneratorFacade _facade;
        private LevelData _currentLevel;
        private string _lastSavePath;
        private List<string> _savedLevelPaths;
        private UnityEngine.UI.Dropdown _savedLevelsDropdown;
        private UnityEngine.UI.InputField _levelNameInput;
        
        #endregion
        
        #region Lifecycle
        
        private async void Start()
        {
            _facade = AILevelGeneratorFacade.Instance;
            Debug.Log("=== LevelSerializer DEMO ===");
            Debug.Log($"Tryb: {_demoMode}");
            
            if (_runDemoOnStart)
            {
                await RunDemo();
            }
        }
        
        #endregion
        
        #region Main Demo Methods
        
        /// <summary>
        /// Uruchamia demo na podstawie wybranego trybu
        /// </summary>
        public async UniTask RunDemo()
        {
            try
            {
                switch (_demoMode)
                {
                    case DemoMode.SaveAndLoad:
                        await DemoSaveAndLoad();
                        break;
                    case DemoMode.ListSavedLevels:
                        DemoListSavedLevels();
                        break;
                    case DemoMode.ExportFormats:
                        await DemoExportFormats();
                        break;
                    case DemoMode.FullWorkflow:
                        await DemoFullWorkflow();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DEMO ERROR: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// DEMO 1: Zapisz → Załaduj
        /// 
        /// Kroki:
        /// 1. Wygeneruj poziom
        /// 2. Zapisz do pliku JSON
        /// 3. Załaduj z pliku
        /// 4. Porównaj dane
        /// </summary>
        private async UniTask DemoSaveAndLoad()
        {
            Debug.Log("\n--- DEMO: SAVE AND LOAD ---\n");
            
            // KROK 1: Wygeneruj poziom
            Debug.Log("1️⃣  Generuję poziom...");
            _currentLevel = await _facade.GenerateAsync(_generationCommand);
            LogLevelInfo(_currentLevel);
            
            // KROK 2: Zapisz do pliku
            Debug.Log("\n2️⃣  Zapisuję na dysk...");
            _lastSavePath = SaveLevel(_currentLevel, _customSaveName);
            Debug.Log($"✓ Zapisano: {_lastSavePath}");
            Debug.Log($"  Rozmiar pliku: {new FileInfo(_lastSavePath).Length} bajtów");
            
            // KROK 3: Załaduj z pliku
            Debug.Log("\n3️⃣  Ładuję z dysku...");
            LevelData loadedLevel = LevelSerializer.LoadFromFile(_lastSavePath);
            Debug.Log("✓ Załadowano!");
            
            // KROK 4: Porównaj
            Debug.Log("\n4️⃣  Porównanie danych:");
            Debug.Log($"   Oryginalny ID:  {_currentLevel.LevelId}");
            Debug.Log($"   Załadowany ID:  {loadedLevel.LevelId}");
            Debug.Log($"   Zgodne? {_currentLevel.LevelId == loadedLevel.LevelId} ✓");
            Debug.Log($"\n   Oryginalnych pokoi: {_currentLevel.Rooms.Count}");
            Debug.Log($"   Załadowanych pokoi: {loadedLevel.Rooms.Count}");
            Debug.Log($"   Zgodne? {_currentLevel.Rooms.Count == loadedLevel.Rooms.Count} ✓");
        }
        
        /// <summary>
        /// DEMO 2: List Saved Levels
        /// 
        /// Pokazuje gdzie są zapisane poziomy
        /// i jakie pliki istnieją
        /// </summary>
        private void DemoListSavedLevels()
        {
            Debug.Log("\n--- DEMO: LIST SAVED LEVELS ---\n");
            
            string saveDir = LevelSerializer.GetSaveDirectory();
            Debug.Log($"📁 Katalog zapisów: {saveDir}\n");
            
            if (!Directory.Exists(saveDir))
            {
                Debug.Log("⚠️  Katalog nie istnieje (brak zapisanych poziomów)");
                return;
            }
            
            string[] savedFiles = LevelSerializer.GetSavedLevels();
            
            if (savedFiles.Length == 0)
            {
                Debug.Log("⚠️  Brak zapisanych poziomów");
                return;
            }
            
            Debug.Log($"Znaleziono {savedFiles.Length} poziomów:\n");
            
            for (int i = 0; i < savedFiles.Length; i++)
            {
                var fileInfo = new FileInfo(savedFiles[i]);
                Debug.Log($"{i + 1}. {fileInfo.Name}");
                Debug.Log($"   Rozmiar: {fileInfo.Length} bajtów");
                Debug.Log($"   Zmieniono: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
        
        /// <summary>
        /// DEMO 3: Export Formats
        /// 
        /// Eksportuj poziom do:
        /// - CSV (tabela danych)
        /// - GraphML (graf do wizualizacji)
        /// </summary>
        private async UniTask DemoExportFormats()
        {
            Debug.Log("\n--- DEMO: EXPORT FORMATS ---\n");
            
            // Wygeneruj poziom
            Debug.Log("1️⃣  Generuję poziom...");
            _currentLevel = await _facade.GenerateAsync(_generationCommand);
            LogLevelInfo(_currentLevel);
            
            // Export CSV
            if (_exportAsCSV)
            {
                Debug.Log("\n2️⃣  Eksportuję do CSV...");
                string csv = LevelSerializer.ExportToCSV(_currentLevel);
                string csvPath = GetExportPath("csv");
                File.WriteAllText(csvPath, csv);
                Debug.Log($"✓ Zapisano: {csvPath}");
                Debug.Log("📊 Zawartość (pierwsze 3 linie):");
                var lines = csv.Split('\n');
                for (int i = 0; i < Mathf.Min(3, lines.Length); i++)
                {
                    Debug.Log($"   {lines[i]}");
                }
            }
            
            // Export GraphML
            if (_exportAsGraphML)
            {
                Debug.Log("\n3️⃣  Eksportuję do GraphML...");
                string graphml = LevelSerializer.ExportToGraphML(_currentLevel);
                string graphmlPath = GetExportPath("graphml");
                File.WriteAllText(graphmlPath, graphml);
                Debug.Log($"✓ Zapisano: {graphmlPath}");
                Debug.Log("🔗 Możesz otworzyć w: https://graphonline.ru/");
                Debug.Log($"   Pokoi: {_currentLevel.Rooms.Count}");
                Debug.Log($"   Połączeń: {CountConnections(_currentLevel)}");
            }
        }
        
        /// <summary>
        /// DEMO 4: Full Workflow
        /// 
        /// Kompleksowy workflow:
        /// 1. Generacja
        /// 2. Zapis
        /// 3. Statystyki
        /// 4. Export
        /// 5. Usunięcie
        /// </summary>
        private async UniTask DemoFullWorkflow()
        {
            Debug.Log("\n--- DEMO: FULL WORKFLOW ---\n");
            
            // KROK 1: Generacja
            Debug.Log("1️⃣  [GENERACJA]");
            _currentLevel = await _facade.GenerateAsync(_generationCommand);
            LogLevelInfo(_currentLevel);
            
            // KROK 2: Zapis
            Debug.Log("\n2️⃣  [ZAPIS]");
            _lastSavePath = SaveLevel(_currentLevel, $"Workflow_{DateTime.Now:yyyyMMdd_HHmmss}");
            Debug.Log($"✓ Zapisano: {Path.GetFileName(_lastSavePath)}");
            
            // KROK 3: Statystyki
            Debug.Log("\n3️⃣  [STATYSTYKI]");
            var stats = _currentLevel.GetStats();
            Debug.Log($"📊 Statystyki:");
            Debug.Log($"   Pokoi: {stats.RoomCount}");
            Debug.Log($"   Połączeń: {stats.ConnectionCount}");
            Debug.Log($"   Średnio na pokój: {stats.AverageConnections:F2}");
            Debug.Log($"   Pokoi bossów: {stats.BossRoomCount}");
            Debug.Log($"   Pokoi skarbu: {stats.TreasureRoomCount}");
            Debug.Log($"   Pokoi tajnych: {stats.SecretRoomCount}");
            Debug.Log($"   Temat: {stats.Theme}");
            Debug.Log($"   Trudność: {stats.Difficulty}");
            
            // KROK 4: Export
            Debug.Log("\n4️⃣  [EXPORT]");
            string csvData = LevelSerializer.ExportToCSV(_currentLevel);
            string csvPath = GetExportPath("workflow.csv");
            File.WriteAllText(csvPath, csvData);
            Debug.Log($"✓ CSV: {Path.GetFileName(csvPath)}");
            
            string graphmlData = LevelSerializer.ExportToGraphML(_currentLevel);
            string graphmlPath = GetExportPath("workflow.graphml");
            File.WriteAllText(graphmlPath, graphmlData);
            Debug.Log($"✓ GraphML: {Path.GetFileName(graphmlPath)}");
            
            // KROK 5: Weryfikacja
            Debug.Log("\n5️⃣  [WERYFIKACJA]");
            LevelData reloadedLevel = LevelSerializer.LoadFromFile(_lastSavePath);
            bool isValid = reloadedLevel.IsValid();
            Debug.Log($"   Załadowany poziom ważny? {(isValid ? "✓ TAK" : "✗ NIE")}");
            
            // KROK 6: Cleanup (opcjonalnie)
            Debug.Log("\n6️⃣  [PODSUMOWANIE]");
            Debug.Log($"✓ Kompletny workflow zakończony!");
            Debug.Log($"  - Plik JSON:  {Path.GetFileName(_lastSavePath)}");
            Debug.Log($"  - Plik CSV:   {Path.GetFileName(csvPath)}");
            Debug.Log($"  - Plik GRAPHML: {Path.GetFileName(graphmlPath)}");
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Zapisz poziom z automatycznym nagłówkiem
        /// </summary>
        private string SaveLevel(LevelData level, string fileName)
        {
            string saveDir = LevelSerializer.GetSaveDirectory();
            string fullPath = Path.Combine(saveDir, fileName);
            
            if (!_useCustomPath)
            {
                LevelSerializer.SaveToFile(level, fullPath);
            }
            else
            {
                LevelSerializer.SaveToFile(level, _customPath);
                fullPath = _customPath;
            }
            
            return fullPath;
        }
        
        /// <summary>
        /// Wypisz informacje o poziomie
        /// </summary>
        private void LogLevelInfo(LevelData level)
        {
            Debug.Log($"✓ Wygenerowany poziom:");
            Debug.Log($"  ID: {level.LevelId}");
            Debug.Log($"  Nazwa: {level.Name}");
            Debug.Log($"  Pokoi: {level.Rooms.Count}");
            Debug.Log($"  Temat: {level.Theme}");
            Debug.Log($"  Trudność: {level.BaseDifficulty}");
            Debug.Log($"  Wejście: {(level.EntranceRoom != null ? "✓" : "✗")}");
            Debug.Log($"  Wyjście: {(level.ExitRoom != null ? "✓" : "✗")}");
            Debug.Log($"  Bossowie: {level.BossRooms.Count}");
        }
        
        /// <summary>
        /// Policz wszystkie połączenia
        /// </summary>
        private int CountConnections(LevelData level)
        {
            int count = 0;
            foreach (var room in level.Rooms)
            {
                count += room.ConnectedRoomIds.Count;
            }
            return count / 2; // Każde połączenie policzone 2x
        }
        
        /// <summary>
        /// Wygeneruj ścieżkę do pliku eksportu
        /// </summary>
        private string GetExportPath(string extension)
        {
            string dir = LevelSerializer.GetSaveDirectory();
            string filename = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
            return Path.Combine(dir, filename);
        }
        
        #endregion
        
        #region Context Menu Commands
        
        [ContextMenu("DEMO: Save & Load")]
        private void ContextSaveLoad() => DemoSaveAndLoad().Forget();
        
        [ContextMenu("DEMO: List Saved Levels")]
        private void ContextListLevels() => DemoListSavedLevels();
        
        [ContextMenu("DEMO: Export Formats")]
        private void ContextExportFormats() => DemoExportFormats().Forget();
        
        [ContextMenu("DEMO: Full Workflow")]
        private void ContextFullWorkflow() => DemoFullWorkflow().Forget();
        
        [ContextMenu("Open Save Directory")]
        private void OpenSaveDirectory()
        {
            string path = LevelSerializer.GetSaveDirectory();
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        
        #endregion
        
        #region Gameplay Methods
        
        /// <summary>
        /// Generuj nowy poziom
        /// </summary>
        private async UniTask GenerateNewLevel()
        {
            ShowStatus("⏳ Generuję poziom...");
            
            try
            {
                _currentLevel = await _facade.GenerateAsync(_generationCommand);
                ShowStatus($"✓ Wygenerowano! {_currentLevel.Rooms.Count} pokoi");
                UpdateUI();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"✗ Błąd: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Zapisz bieżący poziom
        /// </summary>
        private void SaveCurrentLevel()
        {
            if (_currentLevel == null)
            {
                ShowStatus("⚠️ Brak poziomu!");
                return;
            }

            string levelName = _levelNameInput?.text ?? "Level";
            
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = $"Level_{System.DateTime.Now:HHmm}";
            }
            
            try
            {
                LevelSerializer.SaveToFile(_currentLevel, levelName);
                ShowStatus($"✓ Zapisano: {levelName}");
                UpdateSavedLevelsList();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"✗ Błąd przy zapisie: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Załaduj wybrany poziom
        /// </summary>
        private void LoadSelectedLevel()
        {
            if (_savedLevelPaths.Count == 0)
            {
                ShowStatus("⚠️ Brak poziomów!");
                return;
            }
            
            int selectedIndex = _savedLevelsDropdown?.value ?? 0;
            if (selectedIndex >= _savedLevelPaths.Count)
            {
                ShowStatus("⚠️ Zły indeks!");
                return;
            }
            
            try
            {
                string path = _savedLevelPaths[selectedIndex];
                _currentLevel = LevelSerializer.LoadFromFile(path);
                ShowStatus($"✓ Załadowano: {Path.GetFileNameWithoutExtension(path)}");
                UpdateUI();
            }
            catch (System.Exception ex)
            {
                ShowStatus($"✗ Błąd przy ładowaniu: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Zagraj w załadowany poziom
        /// </summary>
        private void PlayLevel()
        {
            if (_currentLevel == null)
            {
                ShowStatus("⚠️ Brak poziomu!");
                return;
            }
            
            // TODO: Załaduj scenę gry i pokaż poziom
            ShowStatus($"▶️ Grasz w: {_currentLevel.Name} ({_currentLevel.Rooms.Count} pokoi)");
            
            // Przykład - załadowanie sceny
            // SceneManager.LoadScene("GameScene");
            // GameManager.Instance.SetCurrentLevel(_currentLevel);
        }
        
        private void ShowStatus(string message)
        {
            Debug.Log(message);
        }
        
        private void UpdateUI()
        {
            // Placeholder
        }
        
        private void UpdateSavedLevelsList()
        {
            _savedLevelPaths = _savedLevelPaths ?? new List<string>();
            _savedLevelPaths.Clear();
            var files = LevelSerializer.GetSavedLevels();
            foreach (var file in files)
            {
                _savedLevelPaths.Add(file);
            }
        }
        
        #endregion
    }
}
