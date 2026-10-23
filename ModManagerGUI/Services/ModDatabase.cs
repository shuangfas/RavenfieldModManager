using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using ModManagerGUI.Models;

namespace ModManagerGUI.Services;

/// <summary>
/// 模组工坊数据本地缓存数据库（data/mods.db）
/// </summary>
public static class ModDatabase
{
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ssK";

    /// <summary>默认缓存有效期：下次更新时间 = 获取时间 + 1 小时</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private static readonly string DbPath = Path.Combine(
        AppContext.BaseDirectory, "data", "mods.db");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>数据库中一条工坊数据记录</summary>
    /// <param name="Id">工坊条目 ID</param>
    /// <param name="Data">工坊元数据（JSON 序列化的 WorkshopData）</param>
    /// <param name="FetchedAt">获取时间（UTC）</param>
    /// <param name="NextUpdateAt">下次需要更新的时间（UTC，默认 FetchedAt + 1 小时）</param>
    public sealed record Entry(
        string Id,
        string Data,
        DateTime FetchedAt,
        DateTime NextUpdateAt);

    static ModDatabase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS workshop_data (
                id             INTEGER PRIMARY KEY,
                data           TEXT NOT NULL,
                fetched_at     TEXT NOT NULL,
                next_update_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        return conn;
    }

    private static DateTime ReadDate(SqliteDataReader reader, int ordinal) =>
        DateTime.Parse(reader.GetString(ordinal), null,
            System.Globalization.DateTimeStyles.RoundtripKind);

    /// <summary>保存（或覆盖）一条工坊数据，next_update_at 默认为当前时间 + 1 小时</summary>
    public static void Save(string id, WorkshopData data,
        DateTime? fetchedAt = null, TimeSpan? ttl = null)
    {
        var now = fetchedAt ?? DateTime.UtcNow;
        var entry = new Entry(
            id,
            JsonSerializer.Serialize(data, JsonOpts),
            now,
            now + (ttl ?? DefaultTtl));
        Save(entry);
    }

    /// <summary>保存（或覆盖）一条记录</summary>
    public static void Save(Entry entry)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO workshop_data (id, data, fetched_at, next_update_at)
            VALUES ($id, $data, $fetched_at, $next_update_at)
            ON CONFLICT(id) DO UPDATE SET
                data = excluded.data,
                fetched_at = excluded.fetched_at,
                next_update_at = excluded.next_update_at;
            """;
        cmd.Parameters.AddWithValue("$id", long.Parse(entry.Id));
        cmd.Parameters.AddWithValue("$data", entry.Data);
        cmd.Parameters.AddWithValue("$fetched_at",
            entry.FetchedAt.ToString(DateFormat));
        cmd.Parameters.AddWithValue("$next_update_at",
            entry.NextUpdateAt.ToString(DateFormat));
        cmd.ExecuteNonQuery();
    }

    /// <summary>读取一条记录；不存在返回 null</summary>
    public static Entry? Get(string id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, data, fetched_at, next_update_at FROM workshop_data WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", long.Parse(id));
        using var reader = cmd.ExecuteReader();
        return reader.Read()
            ? new Entry(
                reader.GetInt64(0).ToString(),
                reader.GetString(1),
                ReadDate(reader, 2),
                ReadDate(reader, 3))
            : null;
    }

    /// <summary>批量读取记录，返回 {id: Entry}；不存在的 id 不包含在结果中</summary>
    public static Dictionary<string, Entry> GetMany(IReadOnlyList<string> ids)
    {
        var result = new Dictionary<string, Entry>();
        if (ids.Count == 0)
            return result;

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        var placeholders = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            placeholders[i] = $"$id{i}";
            cmd.Parameters.AddWithValue($"$id{i}", long.Parse(ids[i]));
        }
        cmd.CommandText =
            "SELECT id, data, fetched_at, next_update_at FROM workshop_data " +
            $"WHERE id IN ({string.Join(", ", placeholders)})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var entry = new Entry(
                reader.GetInt64(0).ToString(),
                reader.GetString(1),
                ReadDate(reader, 2),
                ReadDate(reader, 3));
            result[entry.Id] = entry;
        }
        return result;
    }

    /// <summary>反序列化 Entry.Data 为 WorkshopData</summary>
    public static WorkshopData ParseData(Entry entry) =>
        JsonSerializer.Deserialize<WorkshopData>(entry.Data, JsonOpts)
        ?? new WorkshopData("", "",new List<string>());

    /// <summary>判断记录是否已过期（需要重新从 Steam 获取）</summary>
    public static bool IsStale(Entry entry, DateTime? now = null) =>
        entry.NextUpdateAt <= (now ?? DateTime.UtcNow);
}
