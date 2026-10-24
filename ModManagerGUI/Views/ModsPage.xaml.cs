using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Views;

/// <summary>
/// 模组页：模组识别列表 + 当前播放集（对应 Python ui_qt/mods_page.py）
/// </summary>
public partial class ModsPage : UserControl
{
    public ModsPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    /// <summary>首次加载时导航到详情描述页（NavigationView 不会自动选中第一项；内部 Frame 是继承边界，需显式传入 DataContext）</summary>
    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        DetailNavigation.Navigate(typeof(ModDetailPage), DataContext);
    }

    /// <summary>打开 "..." 下拉菜单（对应 Python show_detail_menu 的 RoundMenu）</summary>
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>切换标签筛选弹窗的显示/隐藏</summary>
    private void TagsButton_Click(object sender, RoutedEventArgs e)
    {
        TagsPopup.IsOpen = !TagsPopup.IsOpen;
    }

    /// <summary>标签选中项变化：合并两个列表的选中项写入 ViewModel</summary>
    private void TagsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ModsViewModel vm)
        {
            vm.UpdateSelectedTags(
                ContentTypeTagsList.SelectedItems.Cast<string>(),
                MapTagsList.SelectedItems.Cast<string>());
        }
    }

    private void ToggleButton_OnClick(object sender, RoutedEventArgs e) {
        if (e.Source is CheckBox checkBox &&checkBox.DataContext is ModCardViewModel model) {
            Console.WriteLine(model.Title);
        }
    }
}
