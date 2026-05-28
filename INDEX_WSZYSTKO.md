# 📚 LevelSerializer - INDEKS KOMPLETNY

## 🚀 ZACZNIJ TUTAJ (10 sekund)

1. **Otwórz projekt w Unity**
2. **Dodaj `LevelSerializerDemo.cs` do sceny** (przeciągnij na GameObject)
3. **Kliknij PLAY** ▶️
4. **Otwórz Console** (Ctrl+Shift+C)
5. **Obserwuj jeśli pracuje!** ✓

Boom! Demo CI pokazuje wszystko.

---

## 📖 DOKUMENTY W PROJEKCIE

```
C:\Unity_Version\Level Generator AI\
│
├─ 📄 LEVEL_SERIALIZER_GUIDE_PL.md  ← POLNA DOKUMENTACJA (CZYTAJ TO!)
│  │
│  ├─ Wyjaśnienie CO to jest
│  ├─ Jak to działa (diagram)
│  ├─ Praktyczne kroki
│  ├─ Gdzie są zapisy
│  ├─ Struktura JSON
│  ├─ Kody do copy-paste
│  └─ FAQ
│
├─ 📋 SCIANA_SZYBKA_LevelSerializer.md  ← ŚCIĄGA (DRUKUJ!)
│  │
│  ├─ 3 najważniejsze rzeczy
│  ├─ Tabela wszystkich funkcji
│  ├─ Od razu działające kody
│  ├─ Błędy i rozwiązania
│  └─ Ready-to-use snippety
│
├─ 📊 DIAGRAM_VISUAL.txt  ← DIAGRAMY (PATRZ NA TO!)
│  │
│  ├─ Flow gracz/gra
│  ├─ Proces SAVE/LOAD
│  ├─ Proces EXPORT
│  ├─ Flowcharty
│  └─ ASCII art wizualizacja
│
└─ Assets/AI_LevelGenerator/Examples/
   │
   ├─ 🎮 LevelSerializerDemo.cs  ← 4 RÓŻNE DEMO (URUCHOM!)
   │  │
   │  ├─ DemoMode.SaveAndLoad         (Zapisuj/Ładuj)
   │  ├─ DemoMode.ListSavedLevels    (Lista plików)
   │  ├─ DemoMode.ExportFormats      (CSV+GraphML)
   │  └─ DemoMode.FullWorkflow       (Kompletny flow)
   │
   ├─ 🎮 LevelSaveLoadMenu.cs  ← PRAKTYCZNE MENU (INTEGRACJA!)
   │  │
   │  ├─ Guziki do UI
   │  ├─ Dropdown z listą zapisów
   │  ├─ Generuj/Zapisz/Załaduj/Graj
   │  └─ Gotowe do gry!
   │
   └─ (Inne przykłady...)
```

---

## 🎯 SZYBKA NAWIGACJA (WG POTRZEBY)

### "Chcę wiedzieć CO to jest"
👉 **Przeczytaj:** `LEVEL_SERIALIZER_GUIDE_PL.md` (rozdział "Czym jest LevelSerializer")

### "Chcę zobaczyć jak to działa"
👉 **Uruchom:** `LevelSerializerDemo.cs` (Play w Unity)

### "Chcę kod do copy-paste"
👉 **Otwórz:** `SCIANA_SZYBKA_LevelSerializer.md` (sekcja "Copy-Paste Kody")

### "Chcę zintegrować z moją grą"
👉 **Użyj:** `LevelSaveLoadMenu.cs` (przykład menu UI)

### "Chcę schematów/diagramów"
👉 **Patrz:** `DIAGRAM_VISUAL.txt` (wszystkie flowchary)

### "Mam błąd/problem"
👉 **Sprawdź:** `SCIANA_SZYBKA_LevelSerializer.md` (tabela błędów)

### "Nie wiem co robić"
👉 **Przeczytaj:** Ten index + `LEVEL_SERIALIZER_GUIDE_PL.md`

---

## 🔧 NAJCZĘŚCIEJ UŻYWANE FUNKCJE

```csharp
// Import
using AILevelGenerator.Utils;
using AILevelGenerator.Core;

// 1. ZAPISZ
LevelSerializer.SaveToFile(level, "MyGame");

// 2. ZAŁADUJ
LevelData loaded = LevelSerializer.LoadFromFile("MyGame.lvl");

// 3. LISTA ZAPISÓW
var saves = LevelSerializer.GetSavedLevels();

// 4. EKSPORT CSV
string csv = LevelSerializer.ExportToCSV(level);

// 5. EKSPORT GRAPHML
string graphml = LevelSerializer.ExportToGraphML(level);

// 6. FOLDER ZAPISÓW
string dir = LevelSerializer.GetSaveDirectory();

// 7. USUŃ
LevelSerializer.DeleteSavedLevel("MyGame.lvl");
```

---

## 📝 FLOW: OD POCZĄTKU DO KOŃCA

```
┌─────────────────────────────────────────┐
│ 1. GENERACJA POZIOMÓW                    │
│                                          │
│ var level = await GenerateAsync(        │
│     "Create dark dungeon with 10 rooms" │
│ );                                       │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ 2. DECYZJA: CO ROBIĆ?                    │
│                                          │
│ a) Graj teraz                            │
│ b) Zapisz na dysk                        │
│ c) Eksportuj dane                        │
└─────────────────────────────────────────┘
         ↙        ↓        ↘
         
a)   b)                    c)
GRAJ ZAPISZ              EKSPORT
  ↓    ↓                   ↓
  🎮  💾              📊📈
     
     SaveToFile()  →  .lvl plik
             ↓
        GRACZ WRACA PÓŹNIEJ
             ↓
        LoadFromFile()  →  GRAJ!
        
                      ExportToCSV()  → Excel
                      ExportToGraphML() → Wizualizacja
```

---

## 🎬 TRZY MINUTOWE TUTORIAL

### Krok 1: Przygotowanie (30 sekund)
```csharp
using AILevelGenerator.Utils;

// Wstawić we swoim skrypcie
```

### Krok 2: Wygeneruj (30 sekund)
```csharp
var level = await AILevelGeneratorFacade.Instance
    .GenerateAsync("Create castle with 8 rooms");
```

### Krok 3: Zapisz (30 sekund)
```csharp
LevelSerializer.SaveToFile(level, "MojaGra");
// Plik się pojawił w SavedLevels/
```

### Krok 4: Załaduj (30 sekund)
```csharp
LevelData loaded = LevelSerializer.LoadFromFile("MojaGra.lvl");
// Poziom załadowany z powrotem!
```

### Krok 5: Graj (30 sekund)
```csharp
InstantiateLevel(loaded);
// GRAJ! 🎮
```

---

## 📚 MATERIAŁY DO NAUKI

| Plik | Typ | Dla kogo | Czas |
|------|-----|---------|------|
| LEVEL_SERIALIZER_GUIDE_PL.md | Pełna dokumentacja | Wszyscy | 15 min |
| SCIANA_SZYBKA_LevelSerializer.md | Ściąga/Reference | Zajęci | 5 min |
| DIAGRAM_VISUAL.txt | Diagramy ASCII | Wzrokowo | 10 min |
| LevelSerializerDemo.cs | Kod + Demo | Praktykanci | 3 min |
| LevelSaveLoadMenu.cs | Integracja UI | Grę twórcy | 20 min |

---

## ✅ CHECKLIST - CO SPRAWDZIĆ

Jeśli coś nie działa, sprawdź:

- [ ] Czy `using AILevelGenerator.Utils;` jest na górze?
- [ ] Czy `LevelData level` nie jest `null`?
- [ ] Czy folder `SavedLevels/` istnieje? (tworzy się automatycznie)
- [ ] Czy plik .lvl ma rozszerzenie `.lvl`?
- [ ] Czy korzystasz z `async/await`?
- [ ] Czy sprawdzisz Console na błędy?
- [ ] Czy uruchomiłeś DEMO najpierw?

---

## 🔗 LINKUJĄCE

### Dokumentacja Unity
- https://docs.unity3d.com/ScriptReference/JsonUtility.html

### Wizualizacja GraphML
- https://graphonline.ru/

### CSV Format
- https://pl.wikipedia.org/wiki/CSV

---

## 🎓 DLA RÓŻNYCH RÓL

### 👨‍💻 Programista
1. Czytaj: `SCIANA_SZYBKA_LevelSerializer.md`
2. Copy-paste kod
3. Testuj w Console
4. Integruj `LevelSaveLoadMenu.cs`

### 🎮 Game Developer
1. Uruchom: `LevelSerializerDemo.cs`
2. Patrz: `DIAGRAM_VISUAL.txt`
3. Integruj z grą
4. Testuj Save/Load

### 📊 Designer/Analyzer
1. Generuj poziom
2. Export: `ExportToCSV()`
3. Otwórz w Excelu
4. Analiza danych

### 🎨 Level Designer
1. Generuj
2. Export: `ExportToGraphML()`
3. Otwórz: https://graphonline.ru/
4. Wizualizuj mapę

---

## 🚨 PROBLEMY I ROZWIĄZANIA

| Problem | Rozwiązanie |
|---------|------------|
| "FileNotFoundException" | Sprawdź `GetSavedLevels()` - czy plik istnieje? |
| "JSON parse error" | Plik JSON może być uszkodzony - spróbuj inny |
| "Brak SavedLevels folder" | Tworzy się automatycznie przy pierwszym SaveToFile() |
| "Null reference exception" | Sprawdź czy `level != null` przed Save |
| "Plik nie otwiera się w Excelu" | CSV musi mieć UTF-8 encoding |
| "GraphML nie rysuje poprawnie" | Sprawdź czy jest format - otwórz w https://graphonline.ru/ |

---

## 💡 PRO TIPS

1. **Automatyczne backupy**: Zapisz z timestampem
   ```csharp
   string name = $"save_{System.DateTime.Now:yyyyMMdd_HHmmss}";
   LevelSerializer.SaveToFile(level, name);
   ```

2. **Kompresja danych**: Dodaj GZip do JSON
   ```csharp
   // Zaawansowane - dla dużych poziomów
   ```

3. **Cloud save**: Upload na serwer
   ```csharp
   // Możliwe - wrgraj JSON na Firebase/Azure
   ```

4. **Multiplayer saves**: Syncj między graczami
   ```csharp
   // Podziel się plikiem .lvl!
   ```

---

## 🎁 GOTOWE SZABLONY

### Szablon 1: Prosty Save/Load
📋 `SCIANA_SZYBKA_LevelSerializer.md` → "Wariant 1"

### Szablon 2: Ze statusem
📋 `SCIANA_SZYBKA_LevelSerializer.md` → "Wariant 2"

### Szablon 3: Export do Excela  
📋 `SCIANA_SZYBKA_LevelSerializer.md` → "Wariant 4"

### Szablon 4: Menu UI
📄 `LevelSaveLoadMenu.cs` → Pełny kod

---

## 🏁 GOTOWE?

### Dla niecierpliwych (2 minuty):
```
1. Copy SCIANA_SZYBKA_LevelSerializer.md "Wariant 1"
2. Wkej w swój kod
3. Zmień "level" na swoją zmienną
4. Done! ✓
```

### Dla chcących zrozumieć (15 minut):
```
1. Przeczytaj LEVEL_SERIALIZER_GUIDE_PL.md
2. Patrz DIAGRAM_VISUAL.txt
3. Uruchom LevelSerializerDemo.cs
4. Zrozumiałeś! ✓
```

### Dla integrujących z grą (30 minut):
```
1. Przeczytaj wszystkie dokumenty
2. Uruchom LevelSerializerDemo.cs
3. Skopiuj LevelSaveLoadMenu.cs
4. Dostosuj do swojej UI
5. Integracja done! ✓
```

---

## 📞 SUPPORT

Problem? Sprawdź:
1. **Console** - błędy zawsze tam
2. **FAQ** w `LEVEL_SERIALIZER_GUIDE_PL.md`
3. **Błędy** w `SCIANA_SZYBKA_LevelSerializer.md`
4. **Diagramy** w `DIAGRAM_VISUAL.txt`
5. **DEMO** - `LevelSerializerDemo.cs` pokaże jak

---

## 🎉 PODSUMOWANIE

```
LevelSerializer = Narzędzie do ZAPISU/ŁADOWANIA/ANALIZY poziomów

3 rzeczy musiałeś wiedzieć:
1. SaveToFile(level, "name")
2. LoadFromFile("name.lvl")
3. ExportToCSV() / ExportToGraphML()

Koniec. Robisz grę. 🚀
```

---

**Ostatnie słowo:** Wszystko jest już wbudowane i działające. Po prostu zacznij od `LevelSerializerDemo.cs` - zobaczysz co się dzieje, a reszta będzie logiczna! 💪

**Powodzenia!** 🎮✨

