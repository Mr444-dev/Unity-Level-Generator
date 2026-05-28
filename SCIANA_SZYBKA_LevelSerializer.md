# LevelSerializer - ŚCIĄGA SZYBKA 📋

## 3 Rzeczy musisz wiedzieć:

```csharp
1️⃣ ZAPISZ:
   LevelSerializer.SaveToFile(level, "MyLevel");
   // Plik pojawia się w persiste

ent data path

2️⃣ ZAŁADUJ:
   LevelData loaded = LevelSerializer.LoadFromFile("MyLevel.lvl");

3️⃣ EKSPORTUJ:
   string csv = LevelSerializer.ExportToCSV(level);
   string graphml = LevelSerializer.ExportToGraphML(level);
```

---

## Wszystkie funkcje w 1 tabeli:

| Funkcja | Co robi | Przykład |
|---------|---------|----------|
| `SaveToFile()` | Zapisz na dysk | `LevelSerializer.SaveToFile(level, "map1")` |
| `LoadFromFile()` | Załaduj z dysku | `var l = LevelSerializer.LoadFromFile("map1.lvl")` |
| `GetSaveDirectory()` | Gdzie są zapisy | `string path = LevelSerializer.GetSaveDirectory()` |
| `GetSavedLevels()` | Lista wszystkich | `var files = LevelSerializer.GetSavedLevels()` |
| `DeleteSavedLevel()` | Usuń plik | `LevelSerializer.DeleteSavedLevel("map1.lvl")` |
| `ExportToCSV()` | Tabela danych | `var csv = LevelSerializer.ExportToCSV(level)` |
| `ExportToGraphML()` | Mapa graficzna | `var xml = LevelSerializer.ExportToGraphML(level)` |

---

## Struktura Folderu:

```
Users/YourName/AppData/LocalLow/DefaultCompany/Level Generator AI/
└── SavedLevels/ ← TUTAJ ZAWSZE
    ├── MyLevel.lvl       ← JSON z danymi
    ├── Level_1.lvl
    ├── export_1430.csv   ← Tabela
    └── struktura.graphml ← Mapa
```

---

## Kod do Copy-Paste (4 warianty):

### Wariant 1: Tylko Zapisz i Załaduj
```csharp
using AILevelGenerator.Utils;

// ZAPISZ
LevelSerializer.SaveToFile(myLevel, "game1");

// ZAŁADUJ
LevelData loaded = LevelSerializer.LoadFromFile("game1.lvl");
```

### Wariant 2: Ze statusem
```csharp
try 
{
    LevelSerializer.SaveToFile(myLevel, "game_" + System.DateTime.Now.Ticks);
    Debug.Log("✓ Zapisano!");
}
catch (System.Exception ex)
{
    Debug.LogError("✗ Błąd: " + ex.Message);
}
```

### Wariant 3: Lista i odświeżanie
```csharp
var allFiles = LevelSerializer.GetSavedLevels();

foreach (var filePath in allFiles)
{
    var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
    Debug.Log(fileName);
    
    LevelData level = LevelSerializer.LoadFromFile(filePath);
    Debug.Log($"  - {level.Rooms.Count} pokoi");
}
```

### Wariant 4: Export dla Excela
```csharp
// CSV - otwiera się w Excelu
string data = LevelSerializer.ExportToCSV(myLevel);
string path = System.IO.Path.Combine(
    LevelSerializer.GetSaveDirectory(), 
    "analiza.csv"
);
System.IO.File.WriteAllText(path, data);
System.Diagnostics.Process.Start("excel.exe", path);
```

---

## Błędy i Rozwiązania:

| Problem | Rozwiązanie |
|---------|-----------fact|
| "Plik nie istnieje" | Sprawdź `GetSavedLevels()` i nazwy |
| "JSON parse error" | Plik może być uszkodzony, spróbuj inny |
| "Brak folderu SavedLevels" | `SaveToFile()` go tworzy automatycznie |
| "Null reference" | Sprawdź `_currentLevel != null` przed Save |
| "Plik się nie otwiera w Excelu" | CSV musi być UTF-8, nie UNICODE |

---

## 5 Rzeczy Musisz Zrobić Raz:

- [x] Dodać `using AILevelGenerator.Utils;` na górze
- [x] Wziąć referencję do `AILevelGeneratorFacade.Instance`
- [x] W `OnClick` buttona: `LevelSerializer.SaveToFile(level, name)`
- [x] Na ładowanie: `LevelData loaded = LevelSerializer.LoadFromFile(path)`
- [x] Grać w załadowany poziom!

---

## Testowanie bez UI:

W Editor dodaj ten plik do empty GameObject i kliknij Play:
```csharp
// Demo będzie logować WSZYSTKO do Console
GameObject demoObj = new GameObject("Demo");
demoObj.AddComponent<LevelSerializerDemo>();
```

Lub w Console:
```csharp
// Szybki test - kliknij Play, otwórz Console
LevelSerializerDemo demo = FindObjectOfType<LevelSerializerDemo>();
demo.GetComponent<LevelSerializerDemo>().RunDemo();
```

---

## Dla Artistów (bez kodu):

1. **Wygeneruj poziom** w edytorze
2. Kliknij **Save Level** button
3. Plik pojawia się w **SavedLevels/** folder
4. **Otwórz w Explorer** i skopiuj sobie

---

##Dla Programistów 🤓

Pełny flow async/await:
```csharp
public async UniTask SaveAndPlayLevel(string command)
{
    // Generuj
    var level = await _facade.GenerateAsync(command);
    
    // Zapisz
    LevelSerializer.SaveToFile(level, "current_game");
    
    // Załaduj z powrotem (aby test czy działa)
    var loadedLevel = LevelSerializer.LoadFromFile("current_game.lvl");
    
    // Sprawdź czy ważny
    if (!loadedLevel.IsValid())
        throw new System.Exception("Level validation failed!");
    
    // Graj!
    await PlayLevel(loadedLevel);
}
```

---

## Przydatne Fragmenty:

**Wypisz wszystkie dane poziomu:**
```csharp
Debug.Log($"Pokoi: {level.Rooms.Count}");
Debug.Log($"Połączeń: {level.Rooms.Sum(r => r.ConnectedRoomIds.Count) / 2}");
Debug.Log($"Wejście: {level.EntranceRoom?.Id}");
Debug.Log($"Wyjście: {level.ExitRoom?.Id}");
```

**Usuń wszystkie zapisy:**
```csharp
var files = LevelSerializer.GetSavedLevels();
foreach (var f in files)
{
    LevelSerializer.DeleteSavedLevel(f);
}
```

**Znajdź największy zapis:**
```csharp
var files = LevelSerializer.GetSavedLevels();
var largest = files.OrderByDescending(f => new FileInfo(f).Length).First();
Debug.Log($"Największy: {Path.GetFileName(largest)}");
```

---

## Kiedy Użyć Co:

- **SaveToFile** → Gracz skończył levelzie, zapytaj "Zapisać?"
- **LoadFromFile** → Menu Główne "Wczytaj grę"
- **ExportToCSV** → Designer chce analizować w Excelu
- **ExportToGraphML** → Designer chce wizualną mapę
- **GetSavedLevels** → Menu Wczytaj - pokaż listę
- **DeleteSavedLevel** → Menu Usuń - skasuj stary zapis

---

## GOTOWY KOD - PRZEPIS NA SUKCES:

```csharp
public class SimpleGameFlow : MonoBehaviour
{
    // 1. GENERUJ
    public async void GenerateNewLevel()
    {
        var level = await AILevelGeneratorFacade.Instance
            .GenerateAsync("Create dark dungeon with 10 rooms");
        CurrentLevel = level;
    }
    
    // 2. ZAPISZ
    public void SaveGame()
    {
        LevelSerializer.SaveToFile(CurrentLevel, "my_save");
    }
    
    // 3. ZAŁADUJ
    public void LoadGame()
    {
        CurrentLevel = LevelSerializer.LoadFromFile("my_save.lvl");
    }
    
    // 4. GRAJ
    public void PlayGame()
    {
        // Załaduj scenę z poziomem
        InstantiateLevel(CurrentLevel);
    }
    
    private LevelData CurrentLevel { get; set; }
}
```

---

## DEMO SETTINGS W INSPECTORZE:

```
LevelSerializerDemo
├── Demo Mode = SaveAndLoad
├── Run Demo On Start = ✓
├── Custom Save Name = "MyLevel"
├── Use Custom Path = ☐
├── Generation Command = "Create dark dungeon with 8 rooms"
├── Export as CSV = ✓
└── Export as GraphML = ✓
```

**Kliknij Play** → Sprawdź Console → Plik się pojawił! ✓

---

## Ostatnie Słowo:

Wszystko jest już podłączone i gotowe. Po prostu:

1. **Wpisz to :**
   ```csharp
   LevelSerializer.SaveToFile(yourLevel, "name");
   ```

2. **Lub to:**
   ```csharp
   LevelData level = LevelSerializer.LoadFromFile("name.lvl");
   ```

3. **Koniec!** ✓

---

*Pytania? Sprawdź `LEVEL_SERIALIZER_GUIDE_PL.md` lub uruchom Demo!*

