# 🎮 AI Level Generator - Instrukcja Krok po Kroku

## 📦 KROK 1: Instalacja pakietów

### 1.1 Zainstaluj UniTask (WYMAGANE)
1. Otwórz `Window > Package Manager`
2. Kliknij `+` w lewym górnym rogu
3. Wybierz `Add package from git URL...`
4. Wklej: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
5. Kliknij `Add`
6. Poczekaj na instalację

### 1.2 Zainstaluj AI Navigation (OPCJONALNE)
1. W Package Manager znajdź `AI Navigation`
2. Kliknij `Install`

---

## 🎬 KROK 2: Utwórz nową scenę

1. `File > New Scene`
2. Wybierz `Basic (Built-in)` lub `Basic (URP)`
3. `File > Save As...` → nazwij np. `LevelGeneratorTest`

---

## 🔧 KROK 3: Konfiguracja sceny (automatyczna)

1. W menu Unity kliknij: `AI Level Generator > Setup Scene`
2. Pojawi się dialog "Scene Setup Complete"
3. W Hierarchy zobaczysz obiekt `[AI Level Generator]`

---

## 📋 KROK 4: Utwórz szablony pokoi

1. W menu Unity kliknij: `AI Level Generator > Create Example Templates`
2. W `Assets/AI_LevelGenerator/Resources/ExampleTemplates/` pojawią się szablony
3. Dialog potwierdzi utworzenie

---

## 🎯 KROK 5: Przypisz szablony do Facade

1. W Hierarchy kliknij `[AI Level Generator]`
2. W Inspector znajdź sekcję `Templates`
3. Rozwiń `Room Templates`
4. Kliknij `+` aby dodać elementy
5. Przeciągnij szablony z `Resources/ExampleTemplates/`:
   - `DungeonRoom_Normal`
   - `DungeonRoom_Entrance`
   - `DungeonRoom_Exit`
   - `DungeonRoom_Boss`
   - `DungeonRoom_Treasure`

**LUB automatycznie:**
- Użyj ponownie `AI Level Generator > Setup Scene`

---

## 🚀 KROK 6: Otwórz okno generatora

1. `Window > AI Level Generator` (lub `Ctrl+Shift+G`)
2. Otworzy się okno generatora

---

## ✍️ KROK 7: Wpisz komendę i generuj

1. W polu tekstowym wpisz np.:
   - `"Stwórz ciemny loch z 10 pokojami"`
   - `"Create dark dungeon 15 rooms medium"`
2. Kliknij przycisk `🎲 Generate`
3. Poczekaj na pasek postępu
4. Zobaczysz podgląd 2D poziomu

---

## 🏗️ KROK 8: Umieść poziom w scenie

1. W oknie generatora kliknij `🏗️ Instantiate in Scene`
2. W Hierarchy pojawi się obiekt `Level_xxxxx`
3. W Scene View zobaczysz wygenerowane pokoje (jako kostki jeśli nie masz prefabów)

---

## 🎮 KROK 9: Test w runtime (Play Mode)

### Opcja A: Prosty test
1. Naciśnij `Play` w Unity
2. Poziom już jest w scenie

### Opcja B: Generowanie w runtime
1. Utwórz nowy skrypt `TestGenerator.cs`
2. Dodaj do pustego GameObject
3. Naciśnij `Play`

---

## 🔍 STRUKTURA HIERARCHII

Po poprawnej konfiguracji powinieneś mieć:

```
📁 Hierarchy
├── Main Camera
├── Directional Light
├── [AI Level Generator]  ← Facade (singleton)
└── Level_abc123          ← Wygenerowany poziom
    ├── Room_xxx_Normal
    ├── Room_xxx_Entrance
    ├── Room_xxx_Exit
    └── ...
```

---

## ⚠️ ROZWIĄZYWANIE PROBLEMÓW

| Problem | Rozwiązanie |
|---------|-------------|
| Brak menu "AI Level Generator" | Poczekaj na kompilację lub sprawdź błędy w Console |
| "No templates found" | Wykonaj Krok 4 i 5 |
| Poziom się nie generuje | Sprawdź czy UniTask jest zainstalowany |
| Puste pokoje (tylko kostki) | To normalne - dodaj prefaby do szablonów |
| Błędy kompilacji | Sprawdź Console (`Ctrl + '`) |

---

## 📁 LOKALIZACJE PLIKÓW

| Element | Ścieżka |
|---------|---------|
| Szablony | `Assets/AI_LevelGenerator/Resources/ExampleTemplates/` |
| Ustawienia | `Assets/AI_LevelGenerator/Resources/GenerationSettings.asset` |
| Skrypty | `Assets/AI_LevelGenerator/Runtime/` |
| Edytor | `Assets/AI_LevelGenerator/Editor/` |
