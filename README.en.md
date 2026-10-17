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

## AI Usage

Claude Code was used during the development of this project for code completion, structural design, and snippet suggestions. Most AI-generated code has been reviewed, tested, and modified by the author.