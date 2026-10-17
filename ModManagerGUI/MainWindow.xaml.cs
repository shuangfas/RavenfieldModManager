using System.IO;
using ModManagerGUI.Base;
using ModManagerGUI.Models;
using ModManagerGUI.Services;
using ModManagerGUI.ViewModels;
using ModManagerGUI.Views;
using Wpf.Ui.Abstractions;

namespace ModManagerGUI;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow {
    public static readonly ConfigReader<Configs.CommonConfig> CommonConfig = new(Path.Combine(AppContext.BaseDirectory, "data", "config.json"));
    public static readonly GameService GameService = new();
    public readonly ModsViewModel _modsViewModel = new();
    private readonly PlaysetViewModel _playsetViewModel;
    private readonly ModsPage _modsPage;
    private readonly PlaysetPage _otherPage;
    private readonly SettingsPage _settingsPage;

    public MainWindow() {
        InitializeComponent();
        CommonConfig.Load();
        if (!GameService.IsBepInExInstalled()) {
            throw new UnavailableException("Couldn't find BepInEx installation");
        }
        _modsViewModel.Initialize();
        _modsPage = new ModsPage { DataContext = _modsViewModel };
        _playsetViewModel = new PlaysetViewModel(_modsViewModel);
        _otherPage = new PlaysetPage { DataContext = _playsetViewModel };
        _settingsPage = new SettingsPage();
        // 用缓存的页面实例导航（对应 Python 版 pages 字典）
        RootNavigation.SetPageProviderService(
            new PageProvider(new Dictionary<Type, object> {
                { typeof(ModsPage), _modsPage },
                { typeof(PlaysetPage), _otherPage },
                { typeof(SettingsPage), _settingsPage }
            }));
        Loaded += (_, _) => RootNavigation.Navigate(typeof(ModsPage));

        Closing += (_, _) => _modsViewModel.Shutdown();
    }

    /// <summary>按 TargetPageType 返回缓存的页面实例</summary>
    private sealed class PageProvider(Dictionary<Type, object> providers)
        : INavigationViewPageProvider {
        public object? GetPage(Type pageType) {
            return providers.ContainsKey(pageType) ? providers[pageType] : null;
        }
    }
}