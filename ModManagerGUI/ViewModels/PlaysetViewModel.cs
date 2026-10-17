using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModManagerGUI.Models;

namespace ModManagerGUI.ViewModels;

public partial class PlaysetViewModel : ObservableObject {
    /// <summary>内置播放集，始终存在且不可删除</summary>
    public const string DefaultPlaysetName = "Default";

    private readonly ModsViewModel _modsVm;
    private readonly List<Playset> _playsets;

    public PlaysetViewModel(ModsViewModel modsVm) {
        _modsVm = modsVm;
        _playsets = MainWindow.CommonConfig.Load().Playsets;
        // 确保 "None" 播放集始终位于首位
        if (_playsets.All(p => p.Name != DefaultPlaysetName)) {
            _playsets.Insert(0, new Playset(DefaultPlaysetName,GetLastId()+1,true, []));
            Save();
        }
        RefreshPlaysetItems();
    }

    public int GetLastId() {
        return _playsets.LastOrDefault()?.Id ?? 0;
    }

    public sealed record NewPlaysetActionItem;

    public ObservableCollection<object> PlaysetItems { get; } = [];

    public ObservableCollection<ModCardViewModel> Mods { get; } = [];

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(RemovePlaysetCommand))]
    [NotifyPropertyChangedFor(nameof(NotDefaultPlayset))]
    [NotifyPropertyChangedFor(nameof(SelectedPlaysetName))]
    [NotifyPropertyChangedFor(nameof(Current))]
    private object? _selectedPlaysetItem;

    private string? SelectedPlaysetName => SelectedPlaysetItem as string;

    private Playset Current => _playsets.FirstOrDefault(p => p.Name == SelectedPlaysetName, _playsets[0]);

    /// <summary>仅当选中的是可删除的普通播放集时允许删除（"None" 不可删除）</summary>
    public bool NotDefaultPlayset => Current.Name != DefaultPlaysetName;

    public bool IsNameInvalid(string name) {
        name = name.Trim();
        if (name.Length == 0 || _playsets.Any(p => p.Name == name))
            return false;
        return true;
    }

    public bool CreatePlayset(string name) {
        if (IsNameInvalid(name)) {
            name = name.Trim();
            _playsets.Add(new Playset(name,GetLastId()+1,true, []));
            PlaysetItems.Add(name);
            Save();
            SelectedPlaysetItem = name;
            return true;
        }
        return false;
    }

    [RelayCommand(CanExecute = nameof(NotDefaultPlayset))]
    private void RemovePlayset() {
        SelectedPlaysetItem = DefaultPlaysetName;
        _playsets.Remove(Current);
        Save();
    }

    public bool RenamePlayset(string name) {
        if (IsNameInvalid(name)) {
            Current.Name = name.Trim();
            Save();
            RefreshPlaysetItems();
            return true;
        }
        return false;
    }

    public void RefreshPlaysetItems() {
        PlaysetItems.Clear();
        PlaysetItems.Add(new NewPlaysetActionItem());
        foreach (var playset in _playsets)
            PlaysetItems.Add(playset.Name);
        if (_playsets.Count > 1)
            SelectedPlaysetItem ??= _playsets[1].Name;
    }

    /// <summary>当前播放集包含的模组 ID（用于弹窗过滤已添加的模组）</summary>
    public IReadOnlyCollection<string> CurrentModIds => Current.ModIds;

    /// <summary>所有模组卡片（添加模组弹窗数据源）</summary>
    public IReadOnlyList<ModCardViewModel> AllMods => _modsVm.Mods;

    // =========================================================
    // 播放集内模组的增删
    // =========================================================

    /// <summary>把模组加入当前播放集（已存在则忽略）并持久化</summary>
    public void AddMods(IEnumerable<ModCardViewModel> mods) {
        if (Current is not { } playset)
            return;

        var changed = false;
        int count = 0;
        foreach (var mod in mods) {
            if (!playset.ModIds.Contains(mod.Id)) {
                playset.ModIds.Add(mod.Id);
                count++;
                changed = true;
            }
        }

        if (changed) {
            Save();
            RebuildMods();
        }
    }

    public void RemoveMod(ModCardViewModel mod) {
        if (Current is not { } playset)
            return;

        if (playset.ModIds.Remove(mod.Id)) {
            Save();
            RebuildMods();
        }
    }

    private void Save() {
        MainWindow.CommonConfig.Update(config => config.Playsets = _playsets);
        MainWindow.GameService.PushConfig(_playsets);
    } 

    // =========================================================
    // 列表刷新
    // =========================================================

    partial void OnSelectedPlaysetItemChanged(object? value) {
        // 选中的是"新建播放集"操作项时不重建列表，由视图还原选择并弹出对话框
        if (value is NewPlaysetActionItem)
            return;
        RebuildMods();
    }

    /// <summary>按当前播放集的模组 ID 顺序重建卡片列表</summary>
    private void RebuildMods() {
        Mods.Clear();
        if (Current is { } playset) {
            foreach (var id in playset.ModIds) {
                var card = _modsVm.Mods.FirstOrDefault(m => m.Id == id);
                if (card is not null)
                    Mods.Add(card);
            }
        }
    }
}