# LevelSerializer - KOMPLETNE WYJAŚNIENIE 🎮

## Czym jest LevelSerializer?

**LevelSerializer** to narzędzie do zapisywania, ładowania i eksportowania danych poziomów. Pozwala na:
- 💾 **Zapis** poziomów do pliku JSON
- 📂 **Ładowanie** wcześniej zapisanych poziomów
- 📊 **Export** do CSV (tabela) i GraphML (graf)
- 📈 **Statystyki** poziomów

---

## Jak to działa? (Techniczny flow)

```
┌─────────────────────────────────────────────────────────────┐
│  WYGENEROWANY POZIOM (LevelData w pamięci)                   │
│  - 8-10 pokoi                                                 │
│  - Połączenia między pokojami                                │
│  - Metadane (temat, trudność, etc.)                          │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    [LevelSerializer]
                           ↓
    ┌───────────────────────┬───────────────────────┐
    ↓                       ↓                       ↓
 [JSON]               [CSV (tabela)]         [GraphML (graf)]
  .lvl               pokoje.csv               struktura.xml
  plik               dane w linijkach        wizualizacja
```

---

## Praktyczne kroki: Jak używać?

### 1️⃣ Wygeneruj poziom
```csharp
// Generujesz poziom normalnie
var facade = AILevelGeneratorFacade.Instance;
var level = await facade.GenerateAsync("Create dark dungeon with 10 rooms");
```

### 2️⃣ Zapisz do pliku
```csharp
// LevelSerializer automatycznie:
// - Konwertuje dane do JSON
// - Tworzy folder (jeśli go brak)
// - Zapisuje plik
LevelSerializer.SaveToFile(level, "MyLevel.lvl");

// Plik pojawia się tutaj:
// C:\\Users\\[Użytkownik]\\AppData\\Persistent DataPath\\SavedLevels\\MyLevel.lvl
```

### 3️⃣ Załaduj poziom z pliku
```csharp
// Ładowanie z powrotem
LevelData loadedLevel = LevelSerializer.LoadFromFile("MyLevel.lvl");

// Teraz masz pełne dane poziomów z powrotem w pamięci
Debug.Log($"Pokoi: {loadedLevel.Rooms.Count}");
```

### 4️⃣ Eksportuj do innych formatów

#### CSV (tabela - dla Excela)
```csharp
string csv = LevelSerializer.ExportToCSV(level);
File.WriteAllText("poziom.csv", csv);

// Wynik:
// Id,Type,X,Y,Z,Difficulty,Connections
// abc123,Normal,0,0,0,1,def456;ghi789
// def456,Treasure,1,0,0,2,abc123;jkl012
```

#### GraphML (graf - dla wizualizacji)
```csharp
string graphml = LevelSerializer.ExportToGraphML(level);
File.WriteAllText("poziom.graphml", graphml);

// Możesz otworzyć w: https://graphonline.ru/
// Zobaczysz wizualną mapę pokoi i połączeń!
```

---

## Gdzie są zapisane poziomy?

```
🖥️ Windows:
   C:\Users\[TwojaNazwa]\AppData\LocalLow\[CompanyName]\[GameName]\SavedLevels\

📱 Mobile:
   /data/data/com.yourcompany.game/files/SavedLevels/

Mac:
   ~/Library/Application Support/[CompanyName]/[GameName]/SavedLevels/
```

**W Unity łatwo:**
```csharp
string path = LevelSerializer.GetSaveDirectory();
Debug.Log(path); // Wypisze dokładną ścieżkę
```

---

## Praktyczne zastosowania

### ✅ Co możesz zrobić:

1. **Gracz generuje poziom** → **Zapisujesz go** → **Gracz może zagrać później**
   ```csharp
   var level = await GenerateLevel();
   LevelSerializer.SaveToFile(level, "my_hard_level");
   ```

2. **Twórca poziomów eksportuje do CSV** → **Analiza w Excelu**
   ```csharp
   var csv = LevelSerializer.ExportToCSV(level);
   // Widzisz: ile pokoi, jakie trudności, które są połączone
   ```

3. **Dizajner chce wizualizacji** → **GraphML**
   ```csharp
   var graphml = LevelSerializer.ExportToGraphML(level);
   // Otwiera online i widzi mapę struktury
   ```

4. **Lista wszystkich zapisanych poziomów**
   ```csharp
   var allLevels = LevelSerializer.GetSavedLevels();
   // array z wszystkimi ścieżkami
   ```

5. **Usuwanie starych poziomów**
   ```csharp
   LevelSerializer.DeleteSavedLevel("old_level.lvl");
   ```

---

## Struktura JSON pliku (.lvl)

```json
{
  "FormatVersion": 1,
  "SaveDate": "2024-05-28T14:30:45.1234567Z",
  "Level": {
    "LevelId": "abc123def456",
    "Name": "Dark Dungeon",
    "Theme": 0,
    "BaseDifficulty": 2,
    "Size": {
      "x": 10,
      "y": 1,
      "z": 10
    },
    "Rooms": [
      {
        "Id": "room1",
        "TemplateId": "corridor_dark_1",
        "Type": 1,
        "GridPosition": { "x": 0, "y": 0, "z": 0 },
        "ConnectedRoomIds": ["room2", "room3"],
        ...
      },
      ...
    ],
    "EntranceRoom": { "Id": "room1", ... },
    "ExitRoom": { "Id": "room8", ... },
    ...
  }
}
```

---

## Co trzeba podłączyć? (Zależności)

```
LevelSerializer.cs potrzebuje:
├─ LevelData.cs ✓ (już istnieje - Core)
├─ RoomData.cs ✓ (już istnieje - Core)
├─ LevelDataExtensions.cs ✓ (w tym pliku)
├─ DebugLogger.cs ✓ (do logów)
└─ UnityEngine.JsonUtility ✓ (wbudowany w Unity)
```

**Wszystko jest już podłączone!** 🎉

---

## Jak zrobić DEMO? (Krok po kroku)

### Kroki:

1. **Otwórz Unity Project** (Level Generator AI)

2. **Dodaj Script do Sceny:**
   - Kliknij prawy przycisk myszy w Hierarchy
   - Create Empty → Rename na "DemoManager"
   - Przeciągnij `LevelSerializerDemo.cs` na GameObject

3. **Skonfiguruj w Inspector:**
   ```
   ☑ Run Demo On Start = true
   Demo Mode = "SaveAndLoad"  (lub inny)
   Generation Command = "Create dark dungeon with 8 rooms, easy difficulty"
   Export as CSV = ☑
   Export as GraphML = ☑
   ```

4. **Play! ▶️**
   - Otwórz Console (Ctrl+Shift+C)
   - Zobaczysz całe demo z logami
   - Pliki będą w `SavedLevels/`

### Tryby Demo:

| Tryb | Co robi | Dla kogo |
|------|---------|----------|
| **SaveAndLoad** | Generuje → Zapisuje → Ładuje | Początkujący |
| **ListSavedLevels** | Pokazuje już zapisane pliki | Sprawdzenie co jest |
| **ExportFormats** | Exportuje CSV i GraphML | Analiza danych |
| **FullWorkflow** | Kompletny flow z analizą | Zaawansowani |

---

## Praktyczne kody do skopiowania

### Kod 1: Podstawowy Zapis/Ładowanie
```csharp
// Wpisz w swoim skrypcie
using AILevelGenerator.Utils;
using AILevelGenerator.Core;

public class MyLevelManager : MonoBehaviour
{
    public void SaveCurrentLevel(LevelData level)
    {
        LevelSerializer.SaveToFile(level, "MojaMapka");
        Debug.Log("✓ Zapisano!");
    }
    
    public LevelData LoadLevel()
    {
        string path = System.IO.Path.Combine(
            LevelSerializer.GetSaveDirectory(), 
            "MojaMapka.lvl"
        );
        return LevelSerializer.LoadFromFile(path);
    }
}
```

### Kod 2: Export dla Designera
```csharp
public void ExportForDesigner(LevelData level)
{
    // CSV - otwiera się w Excelu
    string csv = LevelSerializer.ExportToCSV(level);
    string csvPath = System.IO.Path.Combine(
        LevelSerializer.GetSaveDirectory(),
        "struktura.csv"
    );
    System.IO.File.WriteAllText(csvPath, csv);
    
    // GraphML - wizualizacja online
    string graphml = LevelSerializer.ExportToGraphML(level);
    string graphmlPath = System.IO.Path.Combine(
        LevelSerializer.GetSaveDirectory(),
        "mapa.graphml"
    );
    System.IO.File.WriteAllText(graphmlPath, graphml);
    
    Debug.Log($"✓ Eksportowano: {csvPath}");
    Debug.Log($"✓ Mapa: {graphmlPath}");
}
```

### Kod 3: Menu - Załaduj zapisany poziom
```csharp
public void ShowSavedLevelsList()
{
    var allLevels = LevelSerializer.GetSavedLevels();
    
    if (allLevels.Length == 0)
    {
        Debug.Log("Brak zapisanych poziomów!");
        return;
    }
    
    for (int i = 0; i < allLevels.Length; i++)
    {
        var info = new System.IO.FileInfo(allLevels[i]);
        Debug.Log($"{i}. {info.Name} - {info.Length} bajtów");
    }
}
```

---

## FAQ - Często zadawane pytania

**P: Na czym mogę zagrać zaraz?**
A: Na pliku Demo! Dodaj `LevelSerializerDemo.cs`, play i sprawdź Console.

**P: Czy poziomy są bezpieczne?**
A: JSON jest tekstowe, ale Unity JsonUtility nie zmienia hasłami. Do pełnego bezpieczeństwa dodaj szyfrowanie.

**P: Czy mogę edytować JSON ręcznie?**
A: Tak! Ale Structure musi być ważna, bo LevelData.IsValid() to sprawdzi.

**P: Co to GraphML?**
A: Format dla grafów. Otwarty w https://graphonline.ru/ pokazuje wizualną mapę.

**P: Jak usunąć wszystkie zapisy?**
A: `Directory.Delete(LevelSerializer.GetSaveDirectory());`

**P: Czy działa na Mobile?**
A: Tak! `Application.persistentDataPath` automatycznie ustawia prawidłową ścieżkę.

---

## Podsumowanie - Co trzeba wiedzieć

```
┌─────────────────────────────────────────────────────────────────┐
│ CELEM:  Trwałe zapisywanie wygenerowanych poziomów             │
│                                                                   │
│ GŁÓWNE FUNKCJE:                                                  │
│  1. SaveToFile()        → Zapisz JSON na dysk                  │
│  2. LoadFromFile()      → Załaduj z dysku                      │
│  3. ExportToCSV()       → Dane w tabelę                        │
│  4. ExportToGraphML()   → Wizualna mapa                        │
│  5. GetSavedLevels()    → Lista wszystkich                     │
│  6. DeleteSavedLevel()  → Usuń plik                            │
│                                                                   │
│ DO CZEGO: Gracz może grać inne poziomy, Twórca może            │
│           analizować strukturę, Dizajner może eksportować       │
│           do narzędzi profesjonalnych.                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Złapałeś błąd?

Spróbuj:
1. Sprawdź `Console` - tam są wszystkie błędy
2. Czy LevelData jest ważna? `level.IsValid()` 
3. Czy folder się utworzył? `LevelSerializer.GetSaveDirectory()`
4. Sprawdź pliki Explorer - czy JSON się zapisał

---

## Przydatne linki

- 📖 Dokumentacja Unity JsonUtility: https://docs.unity3d.com/ScriptReference/JsonUtility.html
- 🔗 GraphML Online Viewer: https://graphonline.ru/
- 📊 CSV Format: https://pl.wikipedia.org/wiki/CSV

---

**Gotowe do eksperymentowania?** 🚀

Otwórz projekt, uruchom DEMO i zobacz co się dzieje! Wszystkie kody są gotowe do użycia.

