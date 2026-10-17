using System.Net.Http;
using System.Text.Json;
using ModManagerGUI.Models;

namespace ModManagerGUI.Services;

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

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var details = new Dictionary<string, WorkshopData>();
        foreach (var item in doc.RootElement
                     .GetProperty("response")
                     .GetProperty("publishedfiledetails")
                     .EnumerateArray())
        {
            var id = item.GetProperty("publishedfileid").GetString();
            if (id is null)
                continue;
            var title = item.TryGetProperty("title", out var t) ? t.GetString() : "";
            var description = item.TryGetProperty("description", out var d) ? d.GetString() : "";
            var timeUpdated = item.TryGetProperty("time_updated", out var tu) && tu.ValueKind == JsonValueKind.Number
                ? tu.GetInt64() : 0;
            details[id] = new WorkshopData(title ?? "", description ?? "", timeUpdated);
        }
        return details;
    }
}
