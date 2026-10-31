using System.Collections.Specialized;
using AssetStudio;
using ModManagerGUI.Models;

namespace ModManagerGUI.Services;

public interface IContentParser {
    void Parse(MonoBehaviour behaviour, MonoScript script, TreeNode parent);
}

public abstract class ContentParser : IContentParser {
    private static readonly Dictionary<ContentType, IContentParser> parsers =
        new() {
            { ContentType.WEAPON_CONTENT, new WeaponContentParser() },
            { ContentType.VEHICLE_CONTENT, new VehicleContentParser() },
        };

    public abstract void Parse(MonoBehaviour behaviour, MonoScript script, TreeNode parent);

    public static void BuildContents(SerializedFile file, TreeNode root) {
        foreach (var obj in file.Objects) {
            if (obj is MonoBehaviour behaviour) {
                if (behaviour.m_Script.TryGet(out var script)) {
                    if (ContentType.TryFromValue(script.m_ClassName, out var entry)) {
                        TreeNode<ContentType> content = new(entry);
                        parsers[entry].Parse(behaviour, script, content); //给prase的父节点是内容类型
                        root.AddChild(content);
                    }
                }
            }
        }
    }

    public static void PrintTree(TreeNode node, string prefix = "", string connector = "") {
        Console.WriteLine(prefix + connector + node.Value);

        // 当前节点是最后一个 -> 后代前缀补 4 空格；否则补 "│   "；根节点不补
        string childPrefix = connector switch {
            "└── " => prefix + "    ",
            "├── " => prefix + "│   ",
            _ => prefix // 根节点
        };

        var children = node.Children.Values.ToList();
        for (int i = 0; i < children.Count; i++) {
            bool isLast = i == children.Count - 1;
            PrintTree(children[i], childPrefix, isLast ? "└── " : "├── ");
        }
    }
}

public sealed class WeaponContentParser : ContentParser {
    public override void Parse(MonoBehaviour behaviour, MonoScript script, TreeNode parent) {
        var dict = behaviour.ToType();
        if (dict?["weaponEntries"] is object[] entries) {
            foreach (var x in entries) {
                if (x is OrderedDictionary entry && entry["name"] is string value) {
                    parent.AddChild(new TreeNode<string>(value));
                }
            }
        }
    }
}

public sealed class VehicleContentParser : ContentParser {
    private MonoBehaviour? FindObject(long id, Dictionary<long, AssetStudio.Object> assetsFile) {
        foreach (var pair in assetsFile) {
            if (pair.Value is MonoBehaviour b && b.m_GameObject.m_PathID == id && b.m_Script.TryGet(out var script) &&
                script.m_Namespace == "") {
                return b;
            }
        }

        return null;
    }

    private void ParseVehicles(OrderedDictionary definition,
        Dictionary<long, AssetStudio.Object> assetsFile, TreeNode parent) {
        foreach (var type in VehicleType.List) {
            TreeNode<VehicleType> typeTree = new(type);
            string key = type.Value;
            if (definition.Contains(key) && definition[key] is OrderedDictionary entry) {
                if (entry["m_PathID"] is long id && id != 0) {
                    var behaviour = FindObject(id, assetsFile);
                    if (behaviour != null) {
                        var dict = behaviour.ToType();
                        if (dict.Contains("name") && dict["name"] is string name && name != "") {
                            typeTree.AddChild(new TreeNode<string>(name));
                        }
                    }
                }
            }

            if (typeTree.Children.Count != 0) {
                parent.AddChild(typeTree);
            }
        }
    }

    public override void Parse(MonoBehaviour behaviour, MonoScript script, TreeNode parent) {
        var dict = behaviour.ToType();
        if(dict==null)
            return;
        if (dict.Contains("variants") && dict["variants"] is object[] variants) {
            foreach (var variant in variants) {
                if (variant is OrderedDictionary definition) {
                    ParseVehicles(definition, behaviour.assetsFile.ObjectsDic, parent);
                }
            }
        }
        //源码里说是老的加载方式
        else {
            ParseVehicles(dict, behaviour.assetsFile.ObjectsDic, parent);
        }
    }
}
