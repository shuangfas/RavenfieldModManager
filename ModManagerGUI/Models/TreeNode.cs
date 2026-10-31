using System.Diagnostics.CodeAnalysis;

namespace ModManagerGUI.Models;

public abstract class TreeNode {
    private readonly Dictionary<object, TreeNode> _children = [];

    public TreeNode? Parent { get; private set; }
    public IReadOnlyDictionary<object, TreeNode> Children => _children;

    public IEnumerable<TreeNode> ChildNodes => _children.Values;

    public string Text {
        get {
            if (Value is ITranslatableKey translatableKey) {
                return translatableKey.GetTranslatableKey();
            }

            return Value.ToString();
        }
    }

    public abstract object Value { get; }

    public void AddChild(TreeNode child) {
        ArgumentNullException.ThrowIfNull(child);
        // 如果已经是当前父节点的子节点，直接返回
        if (ReferenceEquals(child.Parent, this))
            return;
        // 查找同级是否已经存在相同 Value 的节点
        _children.TryGetValue(child.Value, out var existing);
        if (existing != null) {
            existing.AddChildren(child._children);
            child.Discard();
            return;
        }

        // 添加到当前父节点
        child.Parent = this;
        _children[child.Value] = child;
    }

    public void AddChildren(IEnumerable<TreeNode> children) {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var child in children)
            AddChild(child);
    }

    public void AddChildren(IDictionary<object, TreeNode> children) {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var child in children.Values)
            AddChild(child);
    }

    public bool RemoveChild(TreeNode child) {
        ArgumentNullException.ThrowIfNull(child);
        _children.Remove(child);
        child.Discard();
        return true;
    }

    public void Discard() {
        _children.Clear();
        Parent = null;
    }
}

/// <summary>泛型节点：保留强类型值</summary>
public class TreeNode<T> : TreeNode {
    public TreeNode(T value) {
        ArgumentNullException.ThrowIfNull(value);
        TypedValue = value;
    }

    /// <summary>强类型值</summary>
    [NotNull]
    private T TypedValue { get;}

    public override object Value => TypedValue;
}