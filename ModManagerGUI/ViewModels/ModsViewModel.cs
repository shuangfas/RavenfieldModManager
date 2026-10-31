using System.Collections.ObjectModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using ModManagerGUI.Services;
using SteamServices = ModManagerGUI.Services.Steam.SteamServices;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 模组页 ViewModel：列表、搜索、选中详情、"..."菜单命令与按需加载
/// （对应 Python ui_qt/mods_page.py 的 ModsPage）
/// </summary>
public partial class ModsViewModel : ObservableObject
{
    private readonly ModDetailLoader _detailLoader;

    public static readonly IReadOnlyList<string> CONTENT_TYPE_TAGS = [
        "Maps", "Maps - Mod Tools", "Weapons", "Vehicles", "Mutators", "Game Configuration", "Skins",
        "Scripted Mission", "Campaign"
    ];

    public static readonly IReadOnlyList<string> MAP_TAGS = [
        "Trigger System", "Ground Vehicles", "Naval Vehicles", "Air Vehicles"
    ];

    public ModsViewModel()
    {
        ModsView = (ListCollectionView)CollectionViewSource.GetDefaultView(Mods);
        _detailLoader = new ModDetailLoader(Mods);
        _detailLoader.BatchLoaded += RefreshList; // 工坊详情到达后刷新列表
    }

    public ObservableCollection<ModCardViewModel> Mods { get; } = [];

    public ListCollectionView ModsView { get; }

    /// <summary>右侧详情栏（标题、描述、渲染开关见 ModDetailPanelViewModel）</summary>
    [ObservableProperty]
    private ModInformationViewModel _information = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ModCardViewModel? _selectedMod;

    /// <summary>选中项变更：同步到详情栏并按需加载描述</summary>
    partial void OnSelectedModChanged(ModCardViewModel? value) =>
        Information.SelectedMod = value;

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

    /// <summary>详情批量加载进行中（初始加载或手动刷新）</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRefresh))]
    private bool _isRefreshing;

    /// <summary>刷新按钮可用条件：没有正在进行的详情加载</summary>
    public bool CanRefresh => !IsRefreshing;

    // =========================================================
    // 初始化：模组发现（内容加载见 ModCardViewModel.Load）
    // =========================================================

    /// <summary>同步扫描创意工坊目录并创建卡片；缩略图与详情由卡片滚入可视区时按需加载</summary>
    public void Initialize()
    {
        SortOptions = [.. SortKeys.Select(L)];
        Information.EnableHtmlRendering = MainWindow.CommonConfig.Current.EnableHtmlRendering;

        var ids = SteamServices.GetWorkshopInfo();

        foreach (var id in ids.OrderBy(long.Parse))
        {
            var card = new ModCardViewModel(id);
            Mods.Add(card);
        }

        // 对应 Python start() 的两个后台线程
        _ = LoadDetailsAsync();
    }

    /// <summary>批量加载详情并维护加载状态（加载流程见 ModDetailLoader）</summary>
    private async Task LoadDetailsAsync(bool skipCache = false)
    {
        IsRefreshing = true;
        try {
            await _detailLoader.LoadAsync(skipCache);
        }
        finally {
            IsRefreshing = false;
        }
    }

    /// <summary>刷新按钮：跳过本地缓存，全部详情走创意工坊接口重新获取（加载中时忽略）</summary>
    public void RefreshDetails() {
        if (IsRefreshing)
            return;
        _ = LoadDetailsAsync(skipCache: true);
    }

    /// <summary>停止后台加载（窗口关闭时调用，对应 Python stop()）</summary>
    public void Shutdown() => _detailLoader.Shutdown();

    // =========================================================
    // 搜索
    // =========================================================

    /// <summary>选中的筛选标签（标签弹窗两个列表合并写入，空集 = 不按标签过滤）</summary>
    private readonly HashSet<string> _selectedTags = [];

    /// <summary>标签匹配模式：false = AND（需命中全部选中标签），true = OR（命中任一即可）</summary>
    [ObservableProperty]
    private bool _matchAnyTag;

    partial void OnMatchAnyTagChanged(bool value) =>
        ModFiltering.Apply(ModsView, SearchText, _selectedTags, value);

    partial void OnSearchTextChanged(string value) =>
        ModFiltering.Apply(ModsView, value, _selectedTags, MatchAnyTag);

    /// <summary>标签弹窗选中项变化时更新 _selectedTags 并重新过滤</summary>
    public void UpdateSelectedTags(IEnumerable<string> contentTypeTags, IEnumerable<string> mapTags) {
        _selectedTags.Clear();
        foreach (var tag in contentTypeTags)
            _selectedTags.Add(tag);
        foreach (var tag in mapTags)
            _selectedTags.Add(tag);
        ModFiltering.Apply(ModsView, SearchText, _selectedTags, MatchAnyTag);
    }

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

    private static string L(string key) => LocalizationService.L(key);

    /// <summary>语言切换后刷新 VM 侧文本（排序选项、详情栏占位文本）</summary>
    public void OnLanguageChanged()
    {
        SortOptions = [.. SortKeys.Select(L)];
        Information.OnLanguageChanged();
    }
}
