# AI Level Generator Pro for Unity 6.3

An intelligent, AI-powered procedural level generation system that understands natural language commands and creates coherent dungeon/level layouts automatically.

## ✨ Features

- 🧠 **Natural Language Commands** - "Create a dark dungeon with 10 rooms, hard difficulty"
- 🌐 **Multilingual** - Supports English and Polish commands
- ⚙️ **Wave Function Collapse** - Advanced constraint-based generation
- 📚 **Learning System** - Adapts to your preferences over time
- 🎨 **Visual Editor** - Real-time 2D preview with zoom/pan
- 🏗️ **One-Click Instantiation** - Generate and place in scene instantly

## 🚀 Quick Start

### 1. Open the Editor Window
`Window > AI Level Generator` (or `Ctrl+Shift+G`)

### 2. Enter a Command
Type in natural language:
- `"Create a dark dungeon with 15 rooms"`
- `"Generate a large cave, medium difficulty"`
- `"Stwórz mały loch z 5 pokojami"` (Polish)

### 3. Generate & Preview
Click **Generate** or press **Enter**. Preview appears in the editor.

### 4. Instantiate
Click **Instantiate in Scene** to place the level.

## 📖 API Usage

```csharp
using AILevelGenerator;

// Simple command
var facade = AILevelGeneratorFacade.Instance;
var level = await facade.GenerateAsync("Create a dark dungeon with 10 rooms");

// With explicit profile
var profile = new GenerationProfile
{
    RoomCount = 15,
    Theme = ThemeType.DarkDungeon,
    TargetDifficulty = Difficulty.Hard
};
var level = await facade.GenerateAsync(profile);

// Instantiate
var levelObject = facade.InstantiateLevel(level);

// Record feedback (for learning)
facade.RecordFeedback(level, 0.9f); // 90% satisfaction
```

## 🛠️ Creating Room Templates

1. `Assets > Create > AI Level Generator > Room Template`
2. Configure:
   - **Prefab** - The room geometry
   - **Type** - Normal, Entrance, Exit, Boss, etc.
   - **Theme** - Which theme this room belongs to
   - **Connections** - Available connection directions
3. Add to `AILevelGeneratorFacade` templates list

## 📐 Supported Parameters

| Parameter | English Keywords | Polish Keywords |
|-----------|------------------|-----------------|
| **Size** | tiny, small, medium, large, huge | mały, średni, duży, ogromny |
| **Difficulty** | easy, medium, hard, nightmare | łatwy, średni, trudny, koszmar |
| **Theme** | dungeon, cave, temple, castle, crypt | loch, jaskinia, świątynia, zamek, krypta |
| **Count** | "X rooms" | "X pokoi" |

## 📁 Project Structure

```
AI_LevelGenerator/
├── Runtime/
│   ├── Core/           # Interfaces, base classes, data structures
│   ├── Generation/     # WFC algorithm, placement solvers
│   ├── AI/             # Command parser, learning system
│   └── Data/           # ScriptableObjects
├── Editor/             # Editor window, custom inspectors
└── Documentation/      # This file
```

## 🔧 Requirements

- Unity 6.3+
- UniTask (recommended for async)
- Odin Inspector (optional, enhances UI)

## 📝 License

MIT License - Free for commercial and personal use.
