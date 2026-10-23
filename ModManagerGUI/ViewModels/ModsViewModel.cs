using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModManagerGUI.Models;
using ModManagerGUI.Services;
using ModManagerGUI.Services.Steam;
using SteamBBCodeParser = ModManagerGUI.Services.Steam.SteamBBCodeParser;
using SteamServices = ModManagerGUI.Services.Steam.SteamServices;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 模组页 ViewModel：列表、搜索、选中详情、"..."菜单命令与按需加载
/// （对应 Python ui_qt/mods_page.py 的 ModsPage）
/// </summary>
public partial class ModsViewModel : ObservableObject
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, ModCardViewModel> _cardsById = [];

    public ModsViewModel()
    {
        ModsView = (ListCollectionView)CollectionViewSource.GetDefaultView(Mods);
    }

    public ObservableCollection<ModCardViewModel> Mods { get; } = [];

    public ListCollectionView ModsView { get; }

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailTitle), nameof(DetailId))]
    [NotifyCanExecuteChangedFor(nameof(OpenModFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenWorkshopPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyModIdCommand))]
    private ModCardViewModel? _selectedMod;

    /// <summary>选中项变更时按需从数据库加载描述</summary>
    partial void OnSelectedModChanged(ModCardViewModel? value) =>
        _ = LoadDetailDescriptionAsync();

    /// <summary>排序下拉选项（顺序对应 SortKeys）</summary>
    [ObservableProperty]
    private List<string> _sortOptions = [];

    /// <summary>当前选中的排序方式索引（0=当前顺序 1=名称 2=更新时间 3=下载日期）</summary>
    [ObservableProperty]
    private int _selectedSortIndex;

    partial void OnSelectedSortIndexChanged(int value)
    {
        if (value == 2)
            _ = LoadTimeUpdatedAsync(); // 更新时间排序：从数据库按需加载
        else
            ApplySort();
    }

    // =========================================================
    // 右侧详情栏文本
    // =========================================================

    public string DetailId =>
        SelectedMod is null ? "" : $"ID {SelectedMod.Id}";

    public string DetailTitle =>
        SelectedMod?.Title ?? L("Right.NoSelection");

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
            DetailDescription = L("Right.ClickHint");
            return;
        }

        DetailDescription = L("Right.Loading");
        var entry = await Task.Run(() => ModDatabase.Get(mod.Id));
        if (!ReferenceEquals(SelectedMod, mod))
            return; // 查询期间选中项已切换

        DetailDescription = entry is null
            ? L("Right.Loading")
            : FormatDescription(ModDatabase.ParseData(entry).Description);
    }

    private string FormatDescription(string? desc) => desc switch {
        null => L("Right.Loading"),
        "" => L("Right.NoDescription"),
        _ when EnableHtmlRendering
            => "<font color='white'>" + SteamBBCodeParser.ToHtml(desc) + "</font>",
        _ => StripBbCode(desc),
    };

    /// <summary>去掉 BBCode 标记（如 [b]、[url=...]），只保留原始文本</summary>
    private static string StripBbCode(string text) =>
        BbCodeTagRegex().Replace(text, string.Empty);

    [GeneratedRegex(@"\[/?[a-zA-Z][^\[\]]*(?:=[^\[\]]*)?\]")]
    private static partial Regex BbCodeTagRegex();

    // =========================================================
    // 初始化：模组发现（内容加载见 ModCardViewModel.Load）
    // =========================================================

    /// <summary>同步扫描创意工坊目录并创建卡片；缩略图与详情由卡片滚入可视区时按需加载</summary>
    public void Initialize()
    {
        SortOptions = [.. SortKeys.Select(L)];
        EnableHtmlRendering = MainWindow.CommonConfig.Current.EnableHtmlRendering;

        var ids = SteamServices.GetWorkshopInfo(SteamServices.RavenfieldAppId);

        foreach (var id in ids.OrderBy(long.Parse))
        {
            var card = new ModCardViewModel(id);
            Mods.Add(card);
            _cardsById[id] = card;
        }

        // 对应 Python start() 的两个后台线程
        _ = LoadDetailsAsync(_cts.Token);
    }

    private async Task LoadDetailsAsync(CancellationToken ct) {
        var ids = Mods.Select(m => m.Id).ToList();
        foreach (var (id, entry) in await Task.Run(() => ModDatabase.GetMany(ids))) {
            if (!ModDatabase.IsStale(entry) && _cardsById.TryGetValue(id, out var card)) {
                card.SetWorkshopData(ModDatabase.ParseData(entry));
                ids.Remove(id);
            }
        }
        RefreshList();
        foreach (var batch in ids.Chunk(SteamServices.WorkshopDetailBatch)) {
            Dictionary<string, WorkshopData> details;
            try {
                details = await WorkshopApiService.GetDetailsAsync(batch);
            }
            catch {
                continue; // 网络失败时跳过该批，卡片保留 id 占位文本
            }
            foreach (var id in batch) {
                if (!details.TryGetValue(id, out var detail) || !_cardsById.TryGetValue(id, out var card))
                    continue;

                card.SetWorkshopData(detail);
                // 写入本地缓存：获取时间 = 现在，下次更新时间 = +1 小时
                await Task.Run(() => ModDatabase.Save(id, detail));
            }
            RefreshList();
        }
    }

    /// <summary>停止后台加载（窗口关闭时调用，对应 Python stop()）</summary>
    public void Shutdown() => _cts.Cancel();

    // =========================================================
    // 卡片 "..." 菜单操作
    // =========================================================

    private bool HasSelection => SelectedMod is not null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenModFolder() => Process.Start(new ProcessStartInfo(SelectedMod!.Path)
    {
        UseShellExecute = true,
    });

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenWorkshopPage() => Process.Start(new ProcessStartInfo(
        $"https://steamcommunity.com/sharedfiles/filedetails/?id={SelectedMod!.Id}")
    {
        UseShellExecute = true,
    });

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CopyModId() => Clipboard.SetText(SelectedMod!.Id);

    // =========================================================
    // 搜索
    // =========================================================

    /// <summary>选中的筛选标签（标签弹窗尚未实现，空集 = 不按标签过滤）</summary>
    private readonly HashSet<string> _selectedTags = [];

    partial void OnSearchTextChanged(string value) =>
        ModFiltering.Apply(ModsView, value, _selectedTags);

    // =========================================================
    // 排序
    // =========================================================

    /// <summary>更新时间排序用的 TimeUpdated 映射（选中该排序时才从数据库加载）</summary>
    private IReadOnlyDictionary<string, long> _timeUpdatedById =
        new Dictionary<string, long>();

    /// <summary>按当前排序方式刷新列表视图（逻辑见 ModSorting）</summary>
    private void ApplySort() => ModSorting.Apply(ModsView, SelectedSortIndex, _timeUpdatedById);

    /// <summary>从数据库按需加载 TimeUpdated 映射，加载完成后应用排序</summary>
    private async Task LoadTimeUpdatedAsync()
    {
        var ids = Mods.Select(m => m.Id).ToList();
        var entries = await Task.Run(() => ModDatabase.GetMany(ids));
        if (SelectedSortIndex != 2)
            return; // 排序方式已切换

        _timeUpdatedById = entries.ToDictionary(
            kv => kv.Key, kv => ModDatabase.ParseData(kv.Value).TimeUpdated);
        ApplySort();
    }

    /// <summary>工坊详情到达后刷新</summary>
    private void RefreshList() {
        if (SelectedSortIndex == 2)
            _ = LoadTimeUpdatedAsync(); // 新详情已入库，重新加载
        else if (SelectedSortIndex == 1)
            ModsView.Refresh();
    }

    // =========================================================
    // 本地化
    // =========================================================

    private static readonly string[] SortKeys = ModSorting.SortKeys;

    private static string L(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;

    /// <summary>语言切换后刷新 VM 侧文本（排序选项、详情栏占位文本）</summary>
    public void OnLanguageChanged()
    {
        SortOptions = [.. SortKeys.Select(L)];
        OnPropertyChanged(nameof(DetailTitle));
        _ = LoadDetailDescriptionAsync(); // 重新解析占位文本
    }
}
