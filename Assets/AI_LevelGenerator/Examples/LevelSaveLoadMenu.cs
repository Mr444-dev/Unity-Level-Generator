using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace AILevelGenerator.Examples
{
    using Core;
    using Utils;
    
    /// <summary>
    /// PRAKTYKA: Menu Gracz → Zapisz Poziom → Zagrań Później
    /// 
    /// Pokazuje rzeczywiste zastosowanie LevelSerializer w grze:
    /// - Generuj niestandardowych poziomy
    /// - Zapisz ulubione
    /// - Zagrań je odsiana
    /// - Podziel się plikami
    /// </summary>
    public class LevelSaveLoadMenu : MonoBehaviour
    {
        #region Inspector
        
        [Header("UI References")]
        [SerializeField] private Button _generateButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _playButton;
        
        [SerializeField] private InputField _levelNameInput;
        [SerializeField] private Text _statusText;
        [SerializeField] private Dropdown _savedLevelsDropdown;
        
        [Header("Settings")]
        [SerializeField] private string _generationCommand = "Create castle with 12 rooms, medium";
        
        #endregion
        
        #region Fields
        
        private LevelData _currentLevel;
        private List<string> _savedLevelPaths;
        private AILevelGeneratorFacade _facade;
        
        #endregion
        
        #region Lifecycle
        
        private void Start()
        {
            _facade = AILevelGeneratorFacade.Instance;
            _savedLevelPaths = new List<string>();
            
            // Setup buttons
            _generateButton?.onClick.AddListener(OnGenerateClicked);
            _saveButton?.onClick.AddListener(OnSaveClicked);
            _loadButton?.onClick.AddListener(OnLoadClicked);
            _playButton?.onClick.AddListener(OnPlayClicked);
            
            // Setup dropdown
            _savedLevelsDropdown?.onValueChanged.AddListener(OnLevelSelected);
            
            // Initial state
            UpdateSavedLevelsList();
            UpdateUI();
            
            ShowStatus("🎮 Menu gotowe!");
        }
        
        #endregion
        
        #region Button Events
        
        private void OnGenerateClicked()
        {
            GenerateNewLevel().Forget();
        }
        
        private void OnSaveClicked()
        {
            if (_currentLevel == null)
            {
                ShowStatus("⚠️ Brak poziomu do zapisania!");
                return;
            }
            
            SaveCurrentLevel();
        }
        
        private void OnLoadClicked()
        {
            if (_savedLevelPaths.Count == 0)
            {
                ShowStatus("⚠️ Brak zapisanych poziomów!");
                return;
            }
            
            LoadSelectedLevel();
        }
        
        private void OnPlayClicked()
        {
            if (_currentLevel == null)
            {
                ShowStatus("⚠️ Najpierw załaduj poziom!");
                return;
            }
            
            PlayLevel();
        }
        
        private void OnLevelSelected(int index)
        {
            UpdateUI();
        }
        
        #endregion
        
        #region Main Logic
        
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
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Odśwież listę zapisanych poziomów
        /// </summary>
        private void UpdateSavedLevelsList()
        {
            _savedLevelPaths.Clear();
            
            var files = LevelSerializer.GetSavedLevels();
            foreach (var file in files)
            {
                _savedLevelPaths.Add(file);
            }
            
            // Update dropdown
            if (_savedLevelsDropdown != null)
            {
                _savedLevelsDropdown.ClearOptions();
                
                var options = new List<Dropdown.OptionData>();
                foreach (var path in _savedLevelPaths)
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    options.Add(new Dropdown.OptionData(name));
                }
                
                _savedLevelsDropdown.AddOptions(options);
            }
        }
        
        /// <summary>
        /// Odśwież stan UI
        /// </summary>
        private void UpdateUI()
        {
            // Generate button - zawsze dostępny
            if (_generateButton != null)
                _generateButton.interactable = true;
            
            // Save button - tylko jeśli mamy poziom
            if (_saveButton != null)
                _saveButton.interactable = (_currentLevel != null);
            
            // Load button - tylko jeśli mamy zapisane poziomy
            if (_loadButton != null)
                _loadButton.interactable = (_savedLevelPaths.Count > 0);
            
            // Play button - tylko jeśli załadowaliśmy poziom
            if (_playButton != null)
                _playButton.interactable = (_currentLevel != null);
            
            // Update status info
            if (_statusText != null)
            {
                string info = _currentLevel != null 
                    ? $"Poziom: {_currentLevel.Name ?? "bez nazwy"} ({_currentLevel.Rooms.Count} pokoi)"
                    : "Brak załadowanego poziomu";
                    
                _statusText.text = info;
            }
        }
        
        /// <summary>
        /// Wypisz status
        /// </summary>
        private void ShowStatus(string message)
        {
            Debug.Log(message);
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }
        
        #endregion
        
        #region Context Menu - Testing
        
        [ContextMenu("Generate Test Level")]
        private void TestGenerate() => OnGenerateClicked();
        
        [ContextMenu("Save Test Level")]
        private void TestSave()
        {
            _levelNameInput.text = $"TestLevel_{Random.Range(100, 999)}";
            OnSaveClicked();
        }
        
        [ContextMenu("List All Saved Levels")]
        private void ListAllLevels()
        {
            var levels = LevelSerializer.GetSavedLevels();
            Debug.Log($"=== Saved Levels ({levels.Length}) ===");
            for (int i = 0; i < levels.Length; i++)
            {
                var info = new FileInfo(levels[i]);
                Debug.Log($"{i}. {info.Name}");
            }
        }
        
        [ContextMenu("Open Save Folder")]
        private void OpenSaveFolder()
        {
            string path = LevelSerializer.GetSaveDirectory();
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        
        [ContextMenu("Export Current as CSV")]
        private void ExportCSV()
        {
            if (_currentLevel == null)
            {
                Debug.LogError("Brak poziomu!");
                return;
            }
            
            string csv = LevelSerializer.ExportToCSV(_currentLevel);
            string path = Path.Combine(
                LevelSerializer.GetSaveDirectory(),
                $"export_{System.DateTime.Now:HHmm}.csv"
            );
            File.WriteAllText(path, csv);
            Debug.Log($"✓ CSV: {path}");
        }
        
        #endregion
    }
}

