namespace ModManagerGUI.Models;

/// <summary>序列化时带具体类型标记的值对象（内容树缓存用）</summary>
public interface ISpecifiedTypeSerializing {
    /// <summary>JSON 中 Type 字段的类型名（对应 <see cref="TypesRegistry"/> 的键）</summary>
    string GetSerializedType();

    /// <summary>JSON 中 Value 字段的字符串，反序列化时据此还原具体值</summary>
    string GetJsonKey();
}
