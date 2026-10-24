using System.Windows.Data;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Services;

/// <summary>
/// 模组列表搜索筛选：模组页与添加模组弹窗共用
/// 对应 Python：详情未到达时只匹配 id，否则匹配 id + 标题
/// </summary>
public static class ModFiltering {
    /// <summary>按搜索关键字和选中标签设置视图的 Filter 并刷新；两者都为空时清除筛选（显示全部）</summary>
    /// <param name="matchAny">标签匹配模式：false = AND（需命中全部选中标签），true = OR（命中任一即可）</param>
    public static void Apply(ListCollectionView view, string searchText, HashSet<string> availableTags, bool matchAny) {
        var keyword = searchText.Trim().ToLowerInvariant();
        view.Filter = keyword.Length == 0 && availableTags.Count == 0
            ? null
            : obj => obj is ModCardViewModel mod && Filter(mod, keyword, availableTags, matchAny);
        view.Refresh();
    }

    private static bool Filter(ModCardViewModel mod, string keyword, HashSet<string> availableTags, bool matchAny) {
        var tagMatch = availableTags.Count == 0
            || (matchAny ? availableTags.Overlaps(mod.Tags) : availableTags.IsSubsetOf(mod.Tags));
        return GetSearchText(mod).Contains(keyword) && tagMatch;
    }

    /// <summary>参与匹配的文本：工坊详情到达后包含标题，此前仅 id</summary>
    private static string GetSearchText(ModCardViewModel mod) =>
        (mod.HasWorkshopData ? $"{mod.Id} {mod.Title}" : mod.Id)
        .ToLowerInvariant();
}