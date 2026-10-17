using System.IO;
using ModManagerGUI.Models;

namespace ModManagerGUI.Services;


public class GameService {
    public static readonly string GAME_PATH = SteamLocator.GetGameInstallPath("636480");
    public static readonly ConfigReader<Configs.GameConfig> GameConfig = new(Path.Combine(GAME_PATH, "BepInEx/config/ModManager_Plugin.json"));
    public static bool IsBepInExInstalled() {
        return  File.Exists(Path.Combine(GAME_PATH, "BepInEx\\core\\BepInEx.dll"));
    }
    public void PushConfig(List<Playset> playsets) {
        GameConfig.Update(config => {
            config.Playsets = playsets;
        });
    }
}