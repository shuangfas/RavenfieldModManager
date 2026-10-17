using System.IO;
using System.Reflection;
using AssetStudio;
using Newtonsoft.Json;

namespace ModManagerGUI.Services;

public class ModContentReader {
    private static List<string> findRFC(string path) {
        var fileList = new List<string>();
        if (Directory.Exists(path)) {
            fileList.AddRange(Directory.GetFiles(path, "*.rfc", SearchOption.AllDirectories));
        }

        return fileList;
    }

    private static void load(AssetsManager manager, List<string> fileList) {
        Type type = manager.GetType();
        MethodInfo? load = type.GetMethod(
            "Load",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        load?.Invoke(manager, new object[] { fileList.ToArray() });
    }

    public static Dictionary<string, string> read(string path, string[] classNameFilters) {
        AssetsManager manager = new AssetsManager();
        manager.SetAssetFilter(new List<ClassIDType> { ClassIDType.MonoBehaviour });
        load(manager, findRFC(path));
        Dictionary<string, string> result = new Dictionary<string, string>();
        foreach (var file in manager.AssetsFileList) {
            foreach (var obj in file.Objects) {
                if (obj is MonoBehaviour behaviour) {
                    var dict = behaviour.ToType();
                    behaviour.m_Script.TryGet(out var m_Script);
                    string className = m_Script.m_ClassName;
                    foreach (var filter in classNameFilters) {
                        if (className.Contains(filter)) {
                            var str = JsonConvert.SerializeObject(dict, Formatting.Indented);
                            result.Add(Path.GetFileName(obj.assetsFile.originalPath), str);
                            break;
                        }
                    }
                }
            }
        }
        return result;
    }
}