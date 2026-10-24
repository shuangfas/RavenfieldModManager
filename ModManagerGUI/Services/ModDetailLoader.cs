using System.Collections.ObjectModel;
using ModManagerGUI.Models;
using ModManagerGUI.ViewModels;
using SteamServices = ModManagerGUI.Services.Steam.SteamServices;
using WorkshopApiService = ModManagerGUI.Services.Steam.WorkshopApiService;

namespace ModManagerGUI.Services;

/// <summary>
/// 模组详情批量加载：本地缓存优先，过期条目走创意工坊接口分批拉取并写回缓存
/// （从 ModsViewModel 拆出）
/// </summary>
public class ModDetailLoader(ObservableCollection<ModCardViewModel> mods)
{
    private readonly CancellationTokenSource _cts = new();

    /// <summary>一批详情入库后触发（UI 据此刷新列表视图）</summary>
    public event Action? BatchLoaded;

    public async Task LoadAsync(bool skipCache = false) {
        var cardsById = mods.ToDictionary(m => m.Id);
        var ids = mods.Select(m => m.Id).ToList();
        // skipCache = true 时跳过缓存读取，全部 id 保留，走创意工坊接口重新获取
        if (!skipCache) {
            foreach (var (id, entry) in await Task.Run(() => ModDatabase.GetMany(ids))) {
                if (!ModDatabase.IsStale(entry) && cardsById.TryGetValue(id, out var card)) {
                    card.SetWorkshopData(ModDatabase.ParseData(entry));
                    ids.Remove(id);
                }
            }
            BatchLoaded?.Invoke();
        }
        foreach (var batch in ids.Chunk(SteamServices.WorkshopDetailBatch)) {
            Dictionary<string, WorkshopData> details;
            try {
                details = await WorkshopApiService.GetDetailsAsync(batch);
            }
            catch {
                continue; // 网络失败时跳过该批，卡片保留 id 占位文本
            }
            foreach (var id in batch) {
                if (!details.TryGetValue(id, out var detail) || !cardsById.TryGetValue(id, out var card))
                    continue;

                card.SetWorkshopData(detail);
                // 写入本地缓存：获取时间 = 现在，下次更新时间 = +1 小时
                await Task.Run(() => ModDatabase.Save(id, detail));
            }
            BatchLoaded?.Invoke();
        }
    }

    /// <summary>停止后台加载（窗口关闭时调用，对应 Python stop()）</summary>
    public void Shutdown() => _cts.Cancel();
}
