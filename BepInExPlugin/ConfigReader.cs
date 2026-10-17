using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;

namespace BepInExPlugin;

public class Playset {
    public string Name { get; set; } = "";
    public int Id;
    public bool Enabled;
    public List<string> ModIds { get; set; } = new();
}

public class ConfigReader {
    private readonly string _path;

    public List<Playset> Current { get; private set; }

    public ConfigReader(string path) {
        _path = path;
        Current = new List<Playset>();
    }

    /// <summary>从磁盘加载；文件不存在时创建默认文件。失败抛异常。</summary>
    public List<Playset> Load() {
        if (!File.Exists(_path)) {
            WriteToDisk(Current);
            return Current;
        }

        JSONNode root;
        try {
            root = JSONNode.Parse(File.ReadAllText(_path, Encoding.UTF8));
        }
        catch (Exception ex) {
            throw new InvalidDataException("配置文件解析失败: " + ex.Message, ex);
        }

        if (root is not JSONClass) {
            throw new InvalidDataException("配置文件根节点不是 JSON 对象");
        }

        var loaded = new List<Playset>();
        JSONArray playsets = root["Playsets"].AsArray;
        if (playsets != null) {
            foreach (JSONNode node in playsets) {
                var playset = new Playset {
                    Id = node["Id"].AsInt,
                    Enabled = node["Enabled"].AsBool,
                    Name = node["Name"].Value,
                };
                JSONArray modIds = node["ModIds"].AsArray;
                if (modIds != null) {
                    foreach (JSONNode modId in modIds) {
                        playset.ModIds.Add(modId.Value);
                    }
                }
                loaded.Add(playset);
            }
        }

        OnLoaded(loaded);
        Current = loaded;
        return Current;
    }

    /// <summary>整体替换并保存。</summary>
    public void Save(List<Playset> playsets) {
        WriteToDisk(playsets);
        Current = playsets;
    }

    /// <summary>就地修改若干项后自动保存，适合多字段一起改。</summary>
    public void Update(Action<List<Playset>> mutate) {
        mutate(Current);
        Save(Current);
    }

    /// <summary>反序列化后的修正钩子，默认什么也不做。</summary>
    protected virtual void OnLoaded(List<Playset> playsets) { }

    private void WriteToDisk(List<Playset> playsets) {
        var root = new JSONClass();
        var playsetsArray = new JSONArray();
        foreach (Playset playset in playsets) {
            var node = new JSONClass();
            node["Id"] = new JSONData(playset.Id);
            node["Enabled"] = new JSONData(playset.Enabled);
            node["Name"] = new JSONData(playset.Name);
            var modIds = new JSONArray();
            foreach (string modId in playset.ModIds) {
                modIds.Add(new JSONData(modId));
            }
            node["ModIds"] = modIds;
            playsetsArray.Add(node);
        }
        root["Playsets"] = playsetsArray;

        string directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }
        // SimpleJSON 的 ToString(prefix) 生成带缩进的多行 JSON
        File.WriteAllText(_path, root.ToString(" "), Encoding.UTF8);
    }
}
