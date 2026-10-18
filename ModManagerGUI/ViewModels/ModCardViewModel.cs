using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ModManagerGUI.Models;
using ModManagerGUI.Services;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 单个模组卡片的状态（对应 Python ModCard + ModInformation）
/// </summary>
public partial class ModCardViewModel : ObservableObject
{

    public ModCardViewModel(string id) {
        Id = id;
        _title = id; // 详情到达前先显示 id 占位（对应 Python BodyLabel(mod.id)）
    }

    /// <summary>创意工坊条目 ID</summary>
    public string Id { get; }

    /// <summary>模组目录下的 .rfc 文件名列表</summary>
    public IReadOnlyList<string> ModFiles {
        get {
            return [
                .. Directory.EnumerateFiles(Path, "*.rfc")
                    .Select(f => System.IO.Path.GetFileName(f))
            ];
        }
    }

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
    }
}
