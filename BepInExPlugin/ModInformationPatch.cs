using HarmonyLib;
namespace BepInExPlugin;

public static class ModInformationPatch {
    [HarmonyPatch(typeof(ModInformation), "IsActive")]
    [HarmonyPrefix]
    public static bool IncludedInList(ref bool __result,ModInformation __instance) {
        __result = __instance.HasLoadedContent() && IsIncluded(__instance);
        return false;
    }

    private static bool IsIncluded(ModInformation info) {
        return Plugin.MOD_LIST.Contains(info.workshopItemId.ToString());
    }
}