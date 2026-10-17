using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace BepInExPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource LOGGER;
    private static readonly List<Harmony> harmonies = new();
    public static readonly string CONFIG_PATH = Path.Combine(Paths.ConfigPath, "ModManager_Plugin.json");
    public static ConfigReader ConfigReader = new(CONFIG_PATH);
    [CanBeNull] public static List<Playset> CACHE;
    public static List<string> MOD_LIST = new();

    private void Awake() {
        LOGGER = Logger;
        LOGGER.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        harmonies.Add(Harmony.CreateAndPatchAll(typeof(ModInformationPatch)));
        LoadModList();
        LOGGER.LogInfo($"{MOD_LIST.Count} mods are needed to load");
        foreach (var v in CACHE) {
            foreach (var VARIABLE in v.ModIds) {
                LOGGER.LogInfo(VARIABLE);
            }
        }
    }
    private void OnDestroy() {
        foreach (Harmony harmony in harmonies) {
            harmony.UnpatchSelf();
        }
    }
    private void LoadModList() {
        CACHE = ConfigReader.Load();
        if (CACHE == null) {
            return;
        }
        foreach (var playset in CACHE) {
            if (playset.Enabled) {
                MOD_LIST.AddRange(playset.ModIds);
            }
        }
    }
}