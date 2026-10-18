# Ravenfield ModManager

[English Version](README.en.md)

## 简介

Ravenfield ModManager 是一款为 Steam 版《Ravenfield》制作的创意工坊 mod 管理器。该项目分为：

- **前端**：GUI 程序
- **后端**：BepInEx 插件

用户可以通过前端方便地管理 mod 播放集，并覆盖掉原来通过注册表控制 mod 加载的 ~~傻波依~~ 方式，以此避免原游戏一次性加载过多无用且 ~~复杂~~ 的 mod 并导致 ~~内存爆炸~~。

## 主要内容

提供 Steam 创意工坊 mod 的预览，并且可以编辑播放集，指定游戏需要加载哪些 mod。

## TODO（按优先级排序）

- [ ] ~~前端浏览 mod 添加的武器、载具等并进行查重，避免过多重复内容导致性能下降。~~
- [ ] 修复AI生成的超级傻逼狗屎石山代码

## 后端安装方法

1. 首先安装 [BepInEx](https://bepinex.com/)。
2. 将 `ModManager_Plugin_xxxx.dll` 作为 Plugin 加载，即放入 `BepInEx/plugins` 文件夹。

## 开源协议

本项目采用 [MIT License](LICENSE) 开源。

## 依赖使用情况

- [AssetStudioMod](https://github.com/aelurum/AssetStudioMod)：用于读取 Unity 资源文件。
- [SimpleJSON](https://github.com/HenrikPoulsen/SimpleJSON)：后端内置，用于 JSON 解析。

## 构建

前端（ModManagerGUI）所需的依赖已包含在仓库的 `ModManagerGUI/libs/` 中，可直接编译。

后端（BepInExPlugin）所需的依赖 DLL 未包含在仓库中，请将下列 DLL 放入 `BepInExPlugin/libs/` 后再编译：

| DLL | 获取方式 |
| --- | --- |
| `Assembly-CSharp.dll` | 游戏目录 `Ravenfield_Data/Managed/` |
| `UnityEngine.dll` | 游戏目录 `Ravenfield_Data/Managed/` |
| `UnityEngine.CoreModule.dll` | 游戏目录 `Ravenfield_Data/Managed/` |
| `UnityEngine.UI.dll` | 游戏目录 `Ravenfield_Data/Managed/` |
| `com.rlabrecque.steamworks.net.dll` | 游戏目录 `Ravenfield_Data/Managed/`（Steamworks.NET） |
| `BepInEx.dll` | [BepInEx](https://bepinex.com/) 发布包 |
| `BepInEx.Harmony.dll` | [BepInEx](https://bepinex.com/) 发布包 |
| `0Harmony.dll` | [BepInEx](https://bepinex.com/) 发布包 |

## AI 使用情况

本项目在开发过程中使用了 Claude Code 进行代码补全、结构设计和片段建议，大部分 AI 生成代码均已由作者审查、测试和修改。