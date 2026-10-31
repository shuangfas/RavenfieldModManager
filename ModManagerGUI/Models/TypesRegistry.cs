namespace ModManagerGUI.Models;

/// <summary>节点类型名（JSON 中的 Type 字段）与具体节点类的映射</summary>
public static class TypesRegistry {
    private static readonly Dictionary<string, Type> Types = new() {
        [nameof(VehicleType)] = typeof(TreeNode<VehicleType>),
        [nameof(ContentType)] = typeof(TreeNode<ContentType>)
    };

    public static Type? GetType(string typeName) {
        return Types.GetValueOrDefault(typeName);
    }
}
