# Ravenfield ModManager

[中文版](README.md)

## Introduction

Ravenfield ModManager is a Steam Workshop mod manager for the Steam version of *Ravenfield*. The project consists of:

- **Frontend**: GUI application
- **Backend**: BepInEx plugin

Users can conveniently manage mod playlists through the frontend and override the previous registry-based mod loading method, avoiding the original game loading too many useless and complex mods at once and causing memory explosion.

## Main Features

Preview Steam Workshop mods and edit playlists to specify which mods the game should load.

## TODO (Ordered by Priority)

- [ ] Frontend: browse weapons, vehicles, and other content added by mods and perform duplicate detection to avoid performance degradation caused by too much duplicate content.
- [ ] ...

## Backend Installation

1. Install [BepInEx](https://bepinex.com/).
2. Load `ModManager_Plugin_xxxx.dll` as a plugin, i.e., place it in the `BepInEx/plugins` folder.

## License

This project is licensed under the [MIT License](LICENSE).

## Dependencies

- [AssetStudioMod](https://github.com/aelurum/AssetStudioMod): Used for reading Unity asset files.
- [SimpleJSON](https://github.com/HenrikPoulsen/SimpleJSON): Bundled in the backend for JSON parsing.

## Building

The dependencies required by the frontend (ModManagerGUI) are already included in `ModManagerGUI/libs/` in the repository, so it can be compiled directly.

The dependency DLLs required by the backend (BepInExPlugin) are not included in the repository. Please place the following DLLs into `BepInExPlugin/libs/` before compiling:

| DLL | Source |
| --- | --- |
| `Assembly-CSharp.dll` | Game directory `Ravenfield_Data/Managed/` |
| `UnityEngine.dll` | Game directory `Ravenfield_Data/Managed/` |
| `UnityEngine.CoreModule.dll` | Game directory `Ravenfield_Data/Managed/` |
| `UnityEngine.UI.dll` | Game directory `Ravenfield_Data/Managed/` |
| `com.rlabrecque.steamworks.net.dll` | Game directory `Ravenfield_Data/Managed/` (Steamworks.NET) |
| `BepInEx.dll` | [BepInEx](https://bepinex.com/) release package |
| `BepInEx.Harmony.dll` | [BepInEx](https://bepinex.com/) release package |
| `0Harmony.dll` | [BepInEx](https://bepinex.com/) release package |

## AI Usage

Claude Code was used during the development of this project for code completion, structural design, and snippet suggestions. Most AI-generated code has been reviewed, tested, and modified by the author.