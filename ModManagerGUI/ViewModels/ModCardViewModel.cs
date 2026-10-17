using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ModManagerGUI.Models;

namespace ModManagerGUI.ViewModels;

/// <summary>
/// 单个模组卡片的状态（对应 Python ModCard + ModInformation）
/// </summary>
public partial class ModCardViewModel : ObservableObject
{
    public ModCardViewModel(string id, IReadOnlyList<string> rfcFiles, string path)
    {
        Id = id;
        RfcFiles = rfcFiles;
        Path = path;
        _title = id; // 详情到达前先显示 id 占位（对应 Python BodyLabel(mod.id)）
    }

    /// <summary>创意工坊条目 ID</summary>
    public string Id { get; }

    /// <summary>模组目录下的 .rfc 文件名列表</summary>
    public IReadOnlyList<string> RfcFiles { get; }

    /// <summary>模组所在目录</summary>
    public string Path { get; }

    /// <summary>工坊元数据（详情线程返回前为 null）</summary>
    public WorkshopData? Data { get; private set; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>工坊详情到达：写回标题与描述（对应 Python apply_detail）</summary>
    public void SetWorkshopData(WorkshopData data)
    {
        Data = data;
        Title = data.Title;
        Description = data.Description;
    }
}
