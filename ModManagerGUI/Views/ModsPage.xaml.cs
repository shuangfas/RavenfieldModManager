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

    private void ToggleButton_OnClick(object sender, RoutedEventArgs e) {
        if (e.Source is CheckBox checkBox &&checkBox.DataContext is ModCardViewModel model) {
            Console.WriteLine(model.Title);
        }
    }
}
