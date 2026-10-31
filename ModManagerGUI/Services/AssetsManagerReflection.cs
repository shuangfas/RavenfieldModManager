using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AssetStudio;

namespace ModManagerGUI.Services;

public static class AssetsManagerReflection {
    private static readonly FieldInfo FilterField = typeof(AssetsManager).GetField(
        "filteredAssetTypesList", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo LoadMethod = typeof(AssetsManager).GetMethod(
        "Load", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static HashSet<ClassIDType> GetFilterTypes(AssetsManager manager) {
        return (HashSet<ClassIDType>)FilterField.GetValue(manager);
    }

    /// <summary>直接覆盖资产类型过滤器（绕过 SetAssetFilter 的级联规则）。</summary>
    public static void SetFilterTypes(AssetsManager manager, List<ClassIDType> typeList) {
        GetFilterTypes(manager).UnionWith(typeList);
    }

    /// <summary>调用私有的 AssetsManager.Load：按文件类型分发（含 ProcessAssets）。</summary>
    public static void Load(AssetsManager manager, string[] fileList) {
        LoadMethod?.Invoke(manager, [fileList]);
    }
}
