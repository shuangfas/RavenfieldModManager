using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using ModManagerGUI.Base;
using ModManagerGUI.Models;
using ModManagerGUI.Services;
using SteamBBCodeParser = ModManagerGUI.Services.Steam.SteamBBCodeParser;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 右侧详情栏 ViewModel：选中模组的标题、ID、描述文本与 HTML 渲染开关（从 ModsViewModel 拆出）
/// </summary>
public partial class ModInformationViewModel : ObservableObject
{
    [ObservableProperty]
    private ModCardViewModel? _selectedMod;

    /// <summary>内容页的数据源：由 ModContentReader 解析选中模组的 .rfc 得到的内容树</summary>
    [ObservableProperty]
    private List<TreeNode> _contentNodes = [];

    /// <summary>内容页提示文本（如模组过大而停止加载）；空串表示不显示</summary>
    [ObservableProperty]
    private string _contentHint = "";

    /// <summary>选中项变更时按需从数据库加载描述，并重新解析模组内容</summary>
    partial void OnSelectedModChanged(ModCardViewModel? value)
    {
        _ = LoadDetailDescriptionAsync();
        _ = LoadContentNodesAsync();
    }

    /// <summary>在后台解析选中模组的 .rfc 内容树；选中项已切换时丢弃结果</summary>
    private async Task LoadContentNodesAsync()
    {
        if (SelectedMod is not { } mod)
        {
            ContentNodes = [];
            ContentHint = "";
            return;
        }

        ContentNodes = [];
        ContentHint = "";
        List<TreeNode> nodes;
        string hint = "";
        try {
            nodes = await Task.Run(() => ModContentReader.ReadCached(mod));
        }
        catch (ModContentTooLargeException e) {
            nodes = [];
            hint = $"{LocalizationService.L("Right.ContentTooLarge")} ({FormatSize(e.TotalBytes)})";
        }
        catch {
            nodes = []; // 解析失败时保留空树，避免打断 UI
        }

        if (!ReferenceEquals(SelectedMod, mod))
            return; // 解析期间选中项已切换

        ContentNodes = nodes;
        ContentHint = hint;
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1048576 ? $"{bytes / 1048576.0:F1} MB" : $"{bytes / 1024.0:F0} KB";

    /// <summary>右侧描述文本：选中模组时才从数据库查询（后台尚未获取到时显示加载中）</summary>
    [ObservableProperty]
    private string _detailDescription = "";

    /// <summary>设置页开关：右侧详情用 HtmlPanel 渲染（否则退化为纯文本 TextBlock）</summary>
    [ObservableProperty]
    private bool _enableHtmlRendering;

    /// <summary>HTML 渲染开关切换后按新格式重新生成右侧描述</summary>
    partial void OnEnableHtmlRenderingChanged(bool value) =>
        _ = LoadDetailDescriptionAsync();

    /// <summary>按需从数据库加载选中模组的描述（数据库未命中说明后台还没获取到）</summary>
    private async Task LoadDetailDescriptionAsync()
    {
        if (SelectedMod is not { } mod)
        {
            DetailDescription = LocalizationService.L("Right.ClickHint");
            return;
        }

        DetailDescription = LocalizationService.L("Right.Loading");
        var entry = await Task.Run(() => ModDatabase.Get(mod.Id));
        if (!ReferenceEquals(SelectedMod, mod))
            return; // 查询期间选中项已切换

        DetailDescription = entry is null
            ? LocalizationService.L("Right.Loading")
            : FormatDescription(ModDatabase.ParseData(entry).Description);
    }

    private string FormatDescription(string? desc) => desc switch {
        null => LocalizationService.L("Right.Loading"),
        "" => LocalizationService.L("Right.NoDescription"),
        _ when EnableHtmlRendering
            => "<font color='white'>" + SteamBBCodeParser.ToHtml(desc) + "</font>",
        _ => StripBbCode(desc),
    };

    /// <summary>去掉 BBCode 标记（如 [b]、[url=...]），只保留原始文本</summary>
    private static string StripBbCode(string text) =>
        BbCodeTagRegex().Replace(text, string.Empty);

    [GeneratedRegex(@"\[/?[a-zA-Z][^\[\]]*(?:=[^\[\]]*)?\]")]
    private static partial Regex BbCodeTagRegex();

    /// <summary>语言切换后刷新详情栏侧文本（标题占位、描述占位文本）</summary>
    public void OnLanguageChanged()
    {
        _ = LoadDetailDescriptionAsync(); // 重新解析占位文本
    }
}
