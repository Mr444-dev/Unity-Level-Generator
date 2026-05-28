# AI Level Generator for Unity 🧠

A natural language-driven level generation system for Unity. Build complex, themed dungeons, caves, temples, and more using simple text commands (Supports both English and Polish inputs!).

## Features ✨

* **Natural Language Processing**: Describe what you want, and the AI parses your intent. 
  * *Example: "Create a large dark dungeon with 15 rooms, hard difficulty"*
  * *Example: "Stwórz małą jaskinię z 5 pokojami"*
* **Dynamic Difficulty System**: Adjusts enemy spawns and layouts based on the requested difficulty.
* **Smart Layout Generation**: Uses procedural generation to create logical, playable spaces with entrances, exits, and boss rooms.
* **Bilingual Support**: NLP parser fully supports both English and Polish commands natively.
* **Editor Integration**: Comes with a beautiful, custom Unity Editor window for real-time preview and generation.

## Getting Started 🚀

### Installation
1. Clone or download this repository.
2. Open the project in Unity (Tested on 2021.3 LTS and newer).
3. Ensure you have the required dependencies (if any are listed in the package manager).

### Quick Demo
To see the tool in action instantly:
1. In the top Unity menu bar, click **Tools -> AI Level Generator -> Create Demo Scene**.
2. A new `DemoScene` will be created and opened automatically.
3. Press **Play**. 
4. The system will automatically generate a level based on the command configured in the `TestSceneSetup` component and spawn a simple player capsule to explore it!

### Using the Editor Window
1. Open the tool via **Window -> AI Level Generator**.
2. Type a command in the input field, such as `Create an industrial factory with 8 rooms`.
3. Click **Generate** and view the layout in the interactive preview window.
4. Tweak your settings, explore different templates, and spawn it directly into your scene!

## Customization 🛠️

The AI Level Generator is built to be extensible.
- **Room Templates**: Create new prefabs and define them as `RoomTemplateData` to expand the visual themes.
- **Themes & Patterns**: Add more keywords and environments by expanding the dictionaries in `CommandParser.cs`.

## License
MIT License. Feel free to use this in your commercial or personal projects!
