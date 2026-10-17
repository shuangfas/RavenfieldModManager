using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Views;

/// <summary>
/// 播放集页：播放集切换 + 播放集内模组管理
/// </summary>
public partial class PlaysetPage : UserControl
{
    public PlaysetPage()
    {
        InitializeComponent();
    }

    /// <summary>打开添加模组弹窗（列出所有模组）</summary>
    private void AddModButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PlaysetViewModel vm)
            return;

        var dialog = new AddModsDialog(vm)
        {
            Owner = Window.GetWindow(this),
        };
        dialog.Show();
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e) {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>上一个选中的播放集名称（用于还原"新建播放集"操作项的选择）</summary>
    private string? _lastPlayset;

    /// <summary>选中"新建播放集"操作项：还原选择并弹出新建对话框</summary>
    private void PlaysetComboBox_SelectionChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DataContext is not PlaysetViewModel vm)
            return;

        if (PlaysetComboBox.SelectedItem is PlaysetViewModel.NewPlaysetActionItem)
        {
            // 还原到上一个播放集（触发绑定写回 VM）
            PlaysetComboBox.SelectedItem = _lastPlayset;

            var dialog = new NewPlaysetDialog(vm)
            {
                Owner = Window.GetWindow(this),
            };
            dialog.ShowDialog();
        }
        else if (PlaysetComboBox.SelectedItem is string name)
        {
            _lastPlayset = name;
        }
    }

    /// <summary>移除播放集内模组</summary>
    private void RemoveModButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ModCardViewModel card } &&
            DataContext is PlaysetViewModel vm)
        {
            vm.RemoveMod(card);
        }
    }
    
    private void OpenRenamePlaysetDialog(object sender, RoutedEventArgs e) {
        if (DataContext is not PlaysetViewModel vm)
            return;
        var dialog = new RenamePlaysetDialog(vm)
        {
            Owner = Window.GetWindow(this),
        };
        dialog.ShowDialog();
    }
}
