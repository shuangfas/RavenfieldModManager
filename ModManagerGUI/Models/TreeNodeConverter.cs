using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModManagerGUI.Models;

/// <summary>
/// TreeNode 与 JSON 的互转：写入 Type（具体节点类型，见 <see cref="TypesRegistry"/>）与
/// Value（可还原的字符串），子节点递归写入 Children。
/// </summary>
public class TreeNodeConverter : JsonConverter<TreeNode> {
    /// <summary>内容树缓存使用的序列化设置（已注册本转换器）</summary>
    public static readonly JsonSerializerSettings Settings = new() {
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new TreeNodeConverter() }
    };

    /// <summary>把单个节点的内容树序列化为 JSON</summary>
    public static string Serialize(TreeNode node) =>
        JsonConvert.SerializeObject(node, Settings);

    /// <summary>把 JSON 反序列化为单个节点的内容树；JSON 无效时返回 null</summary>
    public static TreeNode? Deserialize(string json) =>
        JsonConvert.DeserializeObject<TreeNode>(json, Settings);

    public override void WriteJson(
        JsonWriter writer,
        TreeNode? value,
        JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        if (value.Value is ISpecifiedTypeSerializing type) {
            writer.WritePropertyName("Type");
            writer.WriteValue(type.GetSerializedType());
        }

        writer.WritePropertyName("Value");
        writer.WriteValue(value.Value is ISpecifiedTypeSerializing typed
            ? typed.GetJsonKey()
            : value.Value);

        writer.WritePropertyName("Children");
        writer.WriteStartArray();
        foreach (var child in value.ChildNodes) {
            serializer.Serialize(writer, child);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    public override TreeNode? ReadJson(
        JsonReader reader,
        Type objectType,
        TreeNode? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var obj = JObject.Load(reader);

        string value = obj["Value"]?.Value<string>() ?? "";
        string? typeName = obj["Type"]?.Value<string>();

        TreeNode? node = typeName is not null && TypesRegistry.GetType(typeName) is { } nodeType
            ? CreateNode(nodeType, value)
            : new TreeNode<string>(value);
        if (node == null)
            return null;

        if (obj["Children"] is JArray children) {
            foreach (var childToken in children) {
                TreeNode? child = childToken.ToObject<TreeNode>(serializer);

                if (child != null)
                    node.AddChild(child);
            }
        }

        return node;
    }

    /// <summary>按注册的节点类型还原强类型节点；无法识别的值返回 null</summary>
    private static TreeNode? CreateNode(Type nodeType, string value) {
        if (nodeType == typeof(TreeNode<ContentType>))
            return ContentType.TryFromName(value, out ContentType content)
                ? new TreeNode<ContentType>(content)
                : null;
        if (nodeType == typeof(TreeNode<VehicleType>))
            return VehicleType.TryFromName(value, out VehicleType vehicle)
                ? new TreeNode<VehicleType>(vehicle)
                : null;
        return null;
    }
}
