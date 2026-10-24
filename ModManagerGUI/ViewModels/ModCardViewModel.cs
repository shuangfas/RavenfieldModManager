using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModManagerGUI.Models;
using ModManagerGUI.Services;
using SteamServices = ModManagerGUI.Services.Steam.SteamServices;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 单个模组卡片的状态（对应 Python ModCard + ModInformation）
/// </summary>
public partial class ModCardViewModel : ObservableObject
{

    public ModCardViewModel(string id) {
        Id = id;
        Tags = [];
        _title = id; // 详情到达前先显示 id 占位（对应 Python BodyLabel(mod.id)）
    }

    /// <summary>创意工坊条目 ID</summary>
    public string Id { get; }
    public List<string> Tags { get; private set; }

    /// <summary>模组所在目录</summary>
    public string Path {
        get => System.IO.Path.Combine(SteamServices.WorkshopPath, Id);
    }
    public string ImagePath {
        get {
            string cache = System.IO.Path.Combine(Path, "icon.png");
            if(File.Exists(cache))
                return cache;
            return "..\\Assets\\placeholder.png";
        }
    }

    /// <summary>工坊标题是否已到达（搜索用：未到达时只按 id 匹配）</summary>
    public bool HasWorkshopData { get; private set; }

    [ObservableProperty]
    private string _title;

    /// <summary>工坊详情到达：只写回标题，描述等其他信息存数据库、按需查询</summary>
    public void SetWorkshopData(WorkshopData data)
    {
        HasWorkshopData = true;
        Title = data.Title;
        Tags =  data.Tags;
    }

    // =========================================================
    // 卡片 "..." 菜单操作（绑定见 ModsPage.xaml，经 SelectedMod 路由）
    // =========================================================

    [RelayCommand]
    private void OpenModFolder() => Process.Start(new ProcessStartInfo(Path)
    {
        UseShellExecute = true,
    });

    [RelayCommand]
    private void OpenWorkshopPage() => Process.Start(new ProcessStartInfo(
        $"https://steamcommunity.com/sharedfiles/filedetails/?id={Id}")
    {
        UseShellExecute = true,
    });

    [RelayCommand]
    private void CopyModId() => Clipboard.SetText(Id);
}
