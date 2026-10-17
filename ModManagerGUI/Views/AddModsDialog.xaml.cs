using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ModManagerGUI.Services;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Views;

/// <summary>
/// 添加模组弹窗：展示所有模组（过滤掉已在当前播放集内的），多选后添加
/// </summary>
public partial class AddModsDialog : Wpf.Ui.Controls.FluentWindow
{
    private readonly PlaysetViewModel _vm;
    private readonly ObservableCollection<ModCardViewModel> _available = [];
    private ListCollectionView? _modsView;

    public AddModsDialog(PlaysetViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        // 排序下拉：与模组页相同的四个选项
        SortBox.ItemsSource = ModSorting.SortKeys
            .Select(k => Application.Current.TryFindResource(k) as string ?? k)
            .ToList();

        // 只展示未加入当前播放集的模组
        foreach (var mod in _vm.AllMods)
        {
            if (!_vm.CurrentModIds.Contains(mod.Id))
                _available.Add(mod);
        }

        _modsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_available);
        _modsView.Filter = FilterMod;
        ModsList.ItemsSource = _modsView;
    }

    private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_modsView is { } view && SortBox.SelectedIndex >= 0)
            ModSorting.Apply(view, SortBox.SelectedIndex);
    }

    /// <summary>搜索过滤：匹配 id 或标题</summary>
    private bool FilterMod(object obj)
    {
        if (obj is not ModCardViewModel mod)
            return false;

        var keyword = SearchBox.Text.Trim().ToLowerInvariant();
        if (keyword.Length == 0)
            return true;

        return $"{mod.Id} {mod.Title}".ToLowerInvariant().Contains(keyword);
    }

    private void SearchBox_TextChanged(object sender,
        Wpf.Ui.Controls.AutoSuggestBoxTextChangedEventArgs _) =>
        _modsView?.Refresh();

    /// <summary>把选中的模组加入当前播放集（弹窗保持打开，可继续添加）</summary>
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // ToList 避免在修改集合时枚举
        var selected = ModsList.SelectedItems.Cast<ModCardViewModel>().ToList();
        if (selected.Count == 0)
            return;

        _vm.AddMods(selected);

        // 从弹窗列表移除已添加的条目并清空选择
        foreach (var mod in selected)
            _available.Remove(mod);
        ModsList.UnselectAll();
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
