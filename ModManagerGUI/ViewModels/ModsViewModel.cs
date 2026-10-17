using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModManagerGUI.Models;
using ModManagerGUI.Services;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 模组页 ViewModel：列表、搜索、选中详情、"..."菜单命令与后台加载
/// （对应 Python ui_qt/mods_page.py 的 ModsPage）
/// </summary>
public partial class ModsViewModel : ObservableObject
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, ModCardViewModel> _cardsById = [];

    public ModsViewModel()
    {
        ModsView = (ListCollectionView)CollectionViewSource.GetDefaultView(Mods);
        ModsView.Filter = FilterMod;
    }

    public ObservableCollection<ModCardViewModel> Mods { get; } = [];

    public ListCollectionView ModsView { get; }

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetailTitle), nameof(DetailDescription), nameof(DetailId))]
    [NotifyCanExecuteChangedFor(nameof(OpenModFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenWorkshopPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyModIdCommand))]
    private ModCardViewModel? _selectedMod;

    /// <summary>排序下拉选项（顺序对应 SortKeys）</summary>
    [ObservableProperty]
    private List<string> _sortOptions = [];

    /// <summary>当前选中的排序方式索引（0=当前顺序 1=名称 2=更新时间 3=下载日期）</summary>
    [ObservableProperty]
    private int _selectedSortIndex;

    partial void OnSelectedSortIndexChanged(int value) => ApplySort();

    // =========================================================
    // 右侧详情栏文本
    // =========================================================

    public string DetailId =>
        SelectedMod is null ? "" : $"ID {SelectedMod.Id}";

    public string DetailTitle =>
        SelectedMod?.Title ?? L("Right.NoSelection");

    public string DetailDescription => SelectedMod switch
    {
        null => L("Right.ClickHint"),
        { Description: null } => L("Right.Loading"), // 详情还没返回
        { Description: "" } => L("Right.NoDescription"),
        { Description: var desc } => StripBbCode(desc),
    };

    /// <summary>去掉 BBCode 标记（如 [b]、[url=...]），只保留原始文本</summary>
    private static string StripBbCode(string text) =>
        BbCodeTagRegex().Replace(text, string.Empty);

    [GeneratedRegex(@"\[/?[a-zA-Z][^\[\]]*(?:=[^\[\]]*)?\]")]
    private static partial Regex BbCodeTagRegex();

    // =========================================================
    // 初始化：模组发现 + 启动后台加载
    // =========================================================

    /// <summary>同步扫描创意工坊目录并创建卡片，随后异步加载缩略图与详情</summary>
    public void Initialize()
    {
        SortOptions = [.. SortKeys.Select(L)];

        var ids = SteamLocator.GetWorkshopInfo(SteamLocator.RavenfieldAppId);
        if (ids is null)
            return;

        foreach (var id in ids.OrderBy(long.Parse))
        {
            var modDir = Path.Combine(SteamLocator.WorkshopPath ?? "", id);
            List<string> rfcFiles = Directory.Exists(modDir)
                ? [.. Directory.EnumerateFiles(modDir, "*.rfc")
                    .Select(f => Path.GetFileName(f))
                    .Where(name => name is not null)]
                : [];

            var card = new ModCardViewModel(id, rfcFiles, modDir)
            {
                Thumbnail = ThumbnailService.Placeholder,
            };
            Mods.Add(card);
            _cardsById[id] = card;
        }

        // 对应 Python start() 的两个后台线程
        _ = LoadThumbnailsAsync(_cts.Token);
        _ = LoadDetailsAsync(_cts.Token);
    }

    // =========================================================
    // 后台加载：缩略图 + 工坊详情
    // =========================================================

    private async Task LoadThumbnailsAsync(CancellationToken ct)
    {
        foreach (var card in Mods)
        {
            if (ct.IsCancellationRequested)
                return; // 窗口已关闭

            var iconPath = Path.Combine(
                SteamLocator.WorkshopPath ?? "", card.Id, "icon.png");
            var image = await Task.Run(() => ThumbnailService.Load(iconPath));

            if (ct.IsCancellationRequested)
                return;
            if (image is not null)
                card.Thumbnail = image;
        }
    }

    private async Task LoadDetailsAsync(CancellationToken ct)
    {
        var ids = Mods.Select(m => m.Id).ToList();

        // 先用本地数据库缓存填充未过期条目，只向 Steam 请求过期的
        var cached = await Task.Run(() => ModDatabase.GetMany(ids));
        foreach (var (id, entry) in cached)
        {
            if (ct.IsCancellationRequested)
                return;
            if (ModDatabase.IsStale(entry))
                continue;
            if (_cardsById.TryGetValue(id, out var card))
                card.SetWorkshopData(ModDatabase.ParseData(entry));
        }

        RefreshIfSorted();

        var staleIds = ids.Where(id =>
            !cached.TryGetValue(id, out var entry) || ModDatabase.IsStale(entry)).ToList();
        for (var start = 0; start < staleIds.Count; start += SteamLocator.WorkshopDetailBatch)
        {
            if (ct.IsCancellationRequested)
                return; // 窗口已关闭

            var batch = staleIds.Skip(start).Take(SteamLocator.WorkshopDetailBatch).ToList();
            Dictionary<string, WorkshopData> details;
            try
            {
                details = await WorkshopApiService.GetDetailsAsync(batch);
            }
            catch
            {
                continue; // 网络失败时跳过该批，卡片保留 id 占位文本
            }

            if (ct.IsCancellationRequested)
                return;

            foreach (var id in batch)
            {
                if (!details.TryGetValue(id, out var detail) ||
                    !_cardsById.TryGetValue(id, out var card))
                    continue;

                card.SetWorkshopData(detail);
                // 写入本地缓存：获取时间 = 现在，下次更新时间 = +1 小时
                await Task.Run(() => ModDatabase.Save(id, detail));
            }

            // 选中项的详情刚到达：同步刷新右侧详情栏（对应 Python apply_detail）
            if (SelectedMod is { } selected && details.ContainsKey(selected.Id))
            {
                OnPropertyChanged(nameof(DetailTitle));
                OnPropertyChanged(nameof(DetailDescription));
            }

            RefreshIfSorted();
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

    partial void OnSearchTextChanged(string value) => ModsView.Refresh();

    // =========================================================
    // 排序
    // =========================================================

    /// <summary>按当前排序方式刷新列表视图（逻辑见 ModSorting）</summary>
    private void ApplySort() => ModSorting.Apply(ModsView, SelectedSortIndex);

    /// <summary>工坊详情到达后，名称/更新时间排序需要重新应用</summary>
    private void RefreshIfSorted()
    {
        if (SelectedSortIndex is 1 or 2)
            ModsView.Refresh();
    }

    private bool FilterMod(object obj)
    {
        if (obj is not ModCardViewModel mod)
            return false;

        var keyword = SearchText.Trim().ToLowerInvariant();
        if (keyword.Length == 0)
            return true;

        // 对应 Python：data 为 None 时只匹配 id，否则匹配 id + 标题
        var text = mod.Data is null ? mod.Id : $"{mod.Id} {mod.Data.Title}";
        return text.ToLowerInvariant().Contains(keyword);
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
        OnPropertyChanged(nameof(DetailDescription));
    }
}
