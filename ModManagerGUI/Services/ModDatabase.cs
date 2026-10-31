using System.IO;
using Microsoft.Data.Sqlite;
using ModManagerGUI.Models;
using Newtonsoft.Json;

namespace ModManagerGUI.Services;

/// <summary>
/// 模组数据本地缓存数据库（data/mods.db）：工坊元数据与内容树缓存
/// </summary>
public static class ModDatabase
{
    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ssK";

    /// <summary>默认缓存有效期：下次更新时间 = 获取时间 + 1 小时</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    private static readonly string DbPath = Path.Combine(
        AppContext.BaseDirectory, "data", "mods.db");

    private static readonly JsonSerializerSettings JsonOpts = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
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

    /// <summary>数据库中一条模组内容树缓存记录（按 .rfc 文件粒度）</summary>
    /// <param name="Id">工坊条目 ID</param>
    /// <param name="Path">.rfc 文件的相对路径</param>
    /// <param name="Hash">该文件的指纹（相对路径/大小/修改时间的 SHA-256）</param>
    /// <param name="Data">该文件内容树（单个节点树的 JSON）</param>
    /// <param name="UpdatedAt">写入缓存的时间（UTC）</param>
    public sealed record ContentFileEntry(
        string Id,
        string Path,
        string Hash,
        string Data,
        DateTime UpdatedAt);

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
            CREATE TABLE IF NOT EXISTS content_file_cache (
                mod_id     INTEGER NOT NULL,
                path       TEXT NOT NULL,
                hash       TEXT NOT NULL,
                data       TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                PRIMARY KEY (mod_id, path)
            );
            -- 旧版按模组整体缓存内容树的表已废弃
            DROP TABLE IF EXISTS content_tree;
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
            JsonConvert.SerializeObject(data, JsonOpts),
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

    /// <summary>保存（或覆盖）单个 .rfc 文件的内容树缓存，updated_at 为当前时间</summary>
    public static void SaveContentFile(string id, string path, string hash, string data)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO content_file_cache (mod_id, path, hash, data, updated_at)
            VALUES ($id, $path, $hash, $data, $updated_at)
            ON CONFLICT(mod_id, path) DO UPDATE SET
                hash = excluded.hash,
                data = excluded.data,
                updated_at = excluded.updated_at;
            """;
        cmd.Parameters.AddWithValue("$id", long.Parse(id));
        cmd.Parameters.AddWithValue("$path", path);
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$data", data);
        cmd.Parameters.AddWithValue("$updated_at", DateTime.UtcNow.ToString(DateFormat));
        cmd.ExecuteNonQuery();
    }

    /// <summary>读取一个模组的全部文件级内容树缓存，返回 {文件相对路径: Entry}；无缓存返回空字典</summary>
    public static Dictionary<string, ContentFileEntry> GetContentFiles(string id)
    {
        var result = new Dictionary<string, ContentFileEntry>(StringComparer.OrdinalIgnoreCase);
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT mod_id, path, hash, data, updated_at FROM content_file_cache WHERE mod_id = $id";
        cmd.Parameters.AddWithValue("$id", long.Parse(id));
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var entry = new ContentFileEntry(
                reader.GetInt64(0).ToString(),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                ReadDate(reader, 4));
            result[entry.Path] = entry;
        }
        return result;
    }

    /// <summary>删除该模组中已不存在（不在 keepPaths 中）的文件缓存</summary>
    public static void PruneContentFiles(string id, IReadOnlyCollection<string> keepPaths)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (keepPaths.Count == 0)
        {
            cmd.CommandText = "DELETE FROM content_file_cache WHERE mod_id = $id";
            cmd.Parameters.AddWithValue("$id", long.Parse(id));
        }
        else
        {
            var placeholders = new string[keepPaths.Count];
            var index = 0;
            foreach (var path in keepPaths)
            {
                placeholders[index] = $"$path{index}";
                cmd.Parameters.AddWithValue($"$path{index}", path);
                index++;
            }
            cmd.CommandText =
                "DELETE FROM content_file_cache WHERE mod_id = $id " +
                $"AND path NOT IN ({string.Join(", ", placeholders)})";
            cmd.Parameters.AddWithValue("$id", long.Parse(id));
        }
        cmd.ExecuteNonQuery();
    }

    /// <summary>反序列化 Entry.Data 为 WorkshopData</summary>
    public static WorkshopData ParseData(Entry entry) =>
        JsonConvert.DeserializeObject<WorkshopData>(entry.Data, JsonOpts)
        ?? new WorkshopData("", "",new List<string>());

    /// <summary>判断记录是否已过期（需要重新从 Steam 获取）</summary>
    public static bool IsStale(Entry entry, DateTime? now = null) =>
        entry.NextUpdateAt <= (now ?? DateTime.UtcNow);
}
