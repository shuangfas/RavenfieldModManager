namespace ModManagerGUI.Models;

/// <param name="TimeUpdated">工坊条目最后更新时间（Unix 秒，旧缓存缺该字段时为 0）</param>
public sealed record WorkshopData(string Title, string Description,List<string> Tags, long TimeUpdated = 0);
