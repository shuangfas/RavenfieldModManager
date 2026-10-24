using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Views;

/// <summary>
/// 模组页：模组识别列表 + 当前播放集（对应 Python ui_qt/mods_page.py）
/// </summary>
public partial class ModsPage : UserControl {
    public ModsPage() {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    /// <summary>首次加载时导航到详情描述页（NavigationView 不会自动选中第一项；内部 Frame 是继承边界，需显式传入 DataContext）</summary>
    private void OnPageLoaded(object sender, RoutedEventArgs e) {
        DetailNavigation.Navigate(typeof(ModDetailPage), DataContext);
    }

    /// <summary>按目标页设置切换动画：详情页从右侧滑入，内容页从左侧滑入（Transition 在导航完成后才读取，此处提前改即可生效）</summary>
    private void DetailNavigation_Navigating(Wpf.Ui.Controls.NavigationView sender,
        Wpf.Ui.Controls.NavigatingCancelEventArgs args) {
        sender.Transition = args.Page is ModContentPage
            ? Wpf.Ui.Animations.Transition.SlideLeft
            : Wpf.Ui.Animations.Transition.SlideRight;
    }

    /// <summary>打开 "..." 下拉菜单（对应 Python show_detail_menu 的 RoundMenu）</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e) {
        if (sender is Button { ContextMenu: { } menu } button) {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>切换标签筛选弹窗的显示/隐藏</summary>
    private void TagsButton_Click(object sender, RoutedEventArgs e) {
        TagsPopup.IsOpen = !TagsPopup.IsOpen;
    }

    /// <summary>刷新按钮：跳过本地缓存，重新拉取全部模组详情</summary>
    private void RefreshButton_Click(object sender, RoutedEventArgs e) {
        if (DataContext is ModsViewModel vm) {
            vm.RefreshDetails();
        }
    }

    /// <summary>标签选中项变化：合并两个列表的选中项写入 ViewModel</summary>
    private void TagsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (DataContext is ModsViewModel vm) {
            vm.UpdateSelectedTags(
                ContentTypeTagsList.SelectedItems.Cast<string>(),
                MapTagsList.SelectedItems.Cast<string>());
        }
    }
}