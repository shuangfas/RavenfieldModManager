using System.Net.Http;
using ModManagerGUI.Models;
using Newtonsoft.Json.Linq;

namespace ModManagerGUI.Services.Steam;

/// <summary>
/// Steam 创意工坊详情 API（对应 Python SteamLocator.get_workshop_detail）
/// </summary>
public static class WorkshopApiService
{
    private const string DetailsUrl =
        "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

    private static readonly HttpClient Http = new();

    /// <summary>批量获取创意工坊条目详情，返回 {id: WorkshopData}</summary>
    public static async Task<Dictionary<string, WorkshopData>> GetDetailsAsync(
        IReadOnlyList<string> ids)
    {
        var form = new Dictionary<string, string>
        {
            ["itemcount"] = ids.Count.ToString(),
        };
        for (var i = 0; i < ids.Count; i++)
            form[$"publishedfileids[{i}]"] = ids[i];

        using var response = await Http.PostAsync(
            DetailsUrl, new FormUrlEncodedContent(form));
        response.EnsureSuccessStatusCode();

        var root = JObject.Parse(await response.Content.ReadAsStringAsync());
        var details = new Dictionary<string, WorkshopData>();
        foreach (var item in root["response"]?["publishedfiledetails"] as JArray ?? [])
        {
            var id = item.Value<string>("publishedfileid");
            if (id is null)
                continue;
            var title = item.Value<string>("title") ?? "";
            var description = item.Value<string>("description") ?? "";
            var timeUpdated = item.Value<long?>("time_updated") ?? 0;
            List<string> tags = (item["tags"] as JArray)?
                .Select(t => t.Value<string>("tag") ?? "")
                .ToList() ?? [];
            details[id] = new WorkshopData(title, description, tags, timeUpdated);
        }
        return details;
    }
}
