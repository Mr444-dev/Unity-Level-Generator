<h1 align="center">🧠 AI Level Generator for Unity</h1>

<p align="center">
  <strong>A powerful, natural language-driven procedural level generation system for Unity.</strong><br>
  Build complex, themed dungeons, caves, temples, and more using simple text commands.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2021.3%2B-blue.svg" alt="Unity Version">
  <img src="https://img.shields.io/badge/License-MIT-green.svg" alt="License">
  <img src="https://img.shields.io/badge/NLP-Bilingual-orange.svg" alt="Bilingual NLP">
</p>

---

## ✨ Key Features

* 🗣️ **Natural Language Processing (NLP)**
  Describe your level layout directly to the AI, and it will parse your intent into structural data.
  * *Example: "Create a massive dark dungeon with 25 rooms and extreme difficulty."*
  * *Bilingual Support: The parser fully supports both **English** and **Polish** commands natively!*

* 🧩 **Advanced Wave Function Collapse (WFC)**
  The generator uses a custom Wave Function Collapse algorithm to ensure rooms fit together perfectly, entrances and exits are logically placed, and there are no physically impossible connections.

* ⚔️ **Dynamic Difficulty System**
  The AI scales layouts, mob density, and room compositions based on the requested difficulty level. From "Trivial" to "Nightmare," the level adapts to the command.

* 🏗️ **Extensible Architecture**
  Built on a clean facade pattern and modular components, making it incredibly easy to swap out room templates, add new themes (like Sci-Fi or Industrial), or integrate your own enemy spawner logic.

* 🖥️ **Custom Editor Window**
  Comes with a beautiful, fully-featured custom Unity Editor window. Type your commands, view a 2D grid preview of the generated layout in real-time, tweak settings, and instantiate the level directly into your scene with one click.

---

## 🚀 Getting Started

### Prerequisites
- Unity 2021.3 LTS or newer.
- [UniTask](https://github.com/Cysharp/UniTask) (Included or requires installation via package manager).

### Installation
1. Clone or download this repository.
2. Open the `Level Generator AI` project folder in Unity.

### Quick Demo
To see the tool in action instantly:
1. In the top Unity menu bar, click `Tools -> AI Level Generator -> Create Demo Scene`.
2. A new `DemoScene` will be created and opened automatically.
3. Press **Play** in the editor. 
4. The system will automatically generate a level based on the command configured in the `TestSceneSetup` component and spawn a simple player capsule so you can immediately run around and explore!

---

## 📖 How to Use

### In the Unity Editor
1. Open the tool via `Window -> AI Level Generator`.
2. Type a command in the input field, for example: `Create an industrial factory with 8 rooms`.
3. Click **Generate** and view the layout in the interactive preview window.
4. Tweak your settings, explore different templates, and when you are happy, spawn it directly into your scene!

### Via C# Script (Runtime)
You can easily trigger the generation via code during gameplay to create roguelike infinite levels:

```csharp
using AILevelGenerator;
using AILevelGenerator.Core;

public async void GenerateNewLevel()
{
    var facade = AILevelGeneratorFacade.Instance;
    
    // Generate the level data asynchronously
    LevelData newLevel = await facade.GenerateAsync("Generate a hard temple with 15 rooms");
    
    // Instantiate the physical GameObjects into the scene
    GameObject levelInstance = facade.InstantiateLevel(newLevel);
}
```

---

## 🛠️ Customization

The AI Level Generator is built to be easily modded by developers:
- **Room Templates**: Create new prefabs (e.g. `Corridor_T_Shape`, `BossRoom_Lava`), attach the `RoomTemplateData` scriptable object, and add them to the generator's template library.
- **Themes & Keywords**: Expand the vocabulary of the AI by adding new keywords and environment themes to the dictionaries found in `CommandParser.cs`.

## 📜 License
This project is licensed under the MIT License. Feel free to use this in your commercial or personal projects!
