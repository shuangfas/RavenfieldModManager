using System.IO;
using System.Windows.Data;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Services;

/// <summary>
/// 模组列表排序：模组页与添加模组弹窗共用
/// 索引含义：0=当前顺序 1=名称 2=更新时间 3=下载日期
/// </summary>
public static class ModSorting
{
    /// <summary>排序选项的本地化资源键（顺序即索引含义）</summary>
    public static readonly string[] SortKeys =
        ["Sort.Current", "Sort.Name", "Sort.UpdateTime", "Sort.DownloadDate"];

    /// <summary>按排序方式设置视图的 CustomSort 并刷新；"当前顺序"清除排序（保持插入顺序）。
    /// 更新时间排序需调用方先从数据库按需加载 timeUpdatedById。</summary>
    public static void Apply(ListCollectionView view, int sortIndex,
        IReadOnlyDictionary<string, long>? timeUpdatedById = null)
    {
        view.CustomSort = sortIndex switch
        {
            1 => Comparer<object>.Create((a, b) => string.Compare(
                ((ModCardViewModel)a).Title, ((ModCardViewModel)b).Title,
                StringComparison.OrdinalIgnoreCase)),
            2 => Comparer<object>.Create((a, b) =>
                GetTimeUpdated((ModCardViewModel)b, timeUpdatedById)
                    .CompareTo(GetTimeUpdated((ModCardViewModel)a, timeUpdatedById))),
            3 => Comparer<object>.Create((a, b) =>
                GetDownloadTime((ModCardViewModel)b)
                    .CompareTo(GetDownloadTime((ModCardViewModel)a))),
            _ => null, // 当前顺序：保持插入顺序
        };
        view.Refresh();
    }

    /// <summary>工坊更新时间（从数据库按需加载）；未命中（或旧缓存缺字段）时为 0，排在最后</summary>
    private static long GetTimeUpdated(ModCardViewModel mod,
        IReadOnlyDictionary<string, long>? timeUpdatedById) =>
        timeUpdatedById?.GetValueOrDefault(mod.Id) ?? 0;

    /// <summary>下载日期 = 模组目录创建时间（不可用时取最后写入时间）</summary>
    private static DateTime GetDownloadTime(ModCardViewModel mod)
    {
        if (!Directory.Exists(mod.Path))
            return DateTime.MinValue;
        var dir = new DirectoryInfo(mod.Path);
        return dir.CreationTimeUtc != DateTime.MinValue
            ? dir.CreationTimeUtc
            : dir.LastWriteTimeUtc;
    }
}
