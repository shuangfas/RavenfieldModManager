using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AssetStudio;
using ModManagerGUI.Models;
using ModManagerGUI.Models.Exceptions;
using ModManagerGUI.ViewModels;

namespace ModManagerGUI.Services;

public static class ModContentReader {
    /// <summary>参与解析的 .rfc 文件总大小上限（200 MB）；超过则停止加载并抛出 <see cref="ModContentTooLargeException"/>。</summary>
    //public const long MaxSingleBytes = 50L * 1048576;
    public const long MaxTotalBytes = 200L * 1048576;

    /// <summary>单个文件内容树缓存格式版本；解析逻辑变化时递增，使旧缓存全部失效。</summary>
    private const int ContentCacheVersion = 1;

    private static List<string> FindModFiles(string path) {
        var fileList = new List<string>();
        if (Directory.Exists(path)) {
            fileList.AddRange(Directory.GetFiles(path, "*.rfc", SearchOption.TopDirectoryOnly));
        }

        return fileList;
    }

    /// <summary>统计 .rfc 文件总大小；读取失败的文件不计入。</summary>
    private static long GetTotalBytes(IEnumerable<string> files) {
        long total = 0;
        foreach (var file in files) {
            try {
                total += new FileInfo(file).Length;
            }
            catch (IOException) {
                // 忽略无法读取大小的文件
            }
        }

        return total;
    }

    /// <summary>单个 .rfc 文件的指纹：相对路径、大小、最后修改时间的 SHA-256（只读元数据）。读取失败返回 null。</summary>
    private static string? ComputeFileHash(string file) {
        try {
            var info = new FileInfo(file);
            var text = $"v{ContentCacheVersion}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        }
        catch (IOException) {
            return null; // 无法读取元数据的文件不参与缓存
        }
    }

    private static AssetsManager CreateManager() {
        var manager = new AssetsManager();
        AssetsManagerReflection.SetFilterTypes(manager,
            [ClassIDType.MonoBehaviour, ClassIDType.MonoScript]);
        manager.Options.BundleOptions.DecompressToDisk = true;
        return manager;
    }

    /// <summary>解析一组文件，返回 {文件相对路径: 内容树根节点}。</summary>
    private static Dictionary<string, TreeNode> ParseFiles(string[] files, ref AssetsManager manager) {
        var sw = Stopwatch.StartNew();
        // AssetsFileList 会跨多次 Load 累积；先清空，避免上一块的文件混入本次结果并释放内存
        manager.Clear();
        AssetsManagerReflection.Load(manager, files);
        Dictionary<string, TreeNode> treeNodes = [];
        foreach (var file in manager.AssetsFileList) {
            string originalName = Path.GetFileName(file.originalPath);
            TreeNode<string> root = new($"Contents of {originalName}");
            ContentParser.BuildContents(file, root);
            treeNodes[originalName] = root;
        }

        sw.Stop();
        Console.WriteLine(
            $"[CurrentID: {Environment.CurrentManagedThreadId}] had processed {files.Length} files,taking {sw.Elapsed.TotalMilliseconds:F1} ms");
        return treeNodes;
    }

    /// <summary>解析整条路径（不使用缓存）。</summary>
    public static Dictionary<string, TreeNode> Read(string path) {
        var paths = FindModFiles(path);
        long totalBytes = GetTotalBytes(paths);
        if (totalBytes > MaxTotalBytes)
            throw new ModContentTooLargeException(totalBytes, MaxTotalBytes);
        return Read(paths);
    }

    /// <summary>并行解析指定的 .rfc 文件，返回 {文件相对路径: 内容树根节点}；调用方需自行做大小检查。</summary>
    public static Dictionary<string, TreeNode> Read(IReadOnlyList<string> files) {
        if (files.Count == 0)
            return [];
        object gate = new();
        Dictionary<string, TreeNode> treeNodes = [];
        ParallelOptions options = new() { MaxDegreeOfParallelism = 8 };
        int chunkSize = Math.Max(1, (int)Math.Ceiling(files.Count / (double)options.MaxDegreeOfParallelism));
        Parallel.ForEach(
            files.Chunk(chunkSize), options,
            // 每个线程一个独立的 AssetsManager，避免共享状态
            localInit: CreateManager,
            body: (chunk, _, manager) => {
                var caches = ParseFiles(chunk, ref manager);
                lock (gate) {
                    foreach (var (fileName, node) in caches) {
                        treeNodes[fileName] = node;
                    }
                }

                return manager;
            },
            localFinally: manager => manager.Clear()
        );
        return treeNodes;
    }

    /// <summary>
    /// 按文件粒度读取内容树：每个 .rfc 文件的指纹与数据库缓存一致时直接用缓存，
    /// 只有变化的文件才重新解析并写回数据库；已删除的文件缓存会被清理。
    /// 超过大小上限时抛 <see cref="ModContentTooLargeException"/>，且不读写缓存。
    /// </summary>
    public static List<TreeNode> ReadCached(ModCardViewModel mod, bool shouldSave = true) {
        string rootPath = mod.Path;
        string id = mod.Id;
        var files = FindModFiles(rootPath)
            .ToDictionary(file => file, file => Path.GetFileName(file),
                StringComparer.OrdinalIgnoreCase); //key:fullPath value:fileName
        //判断文件大小，太大丢出异常
        long totalBytes = GetTotalBytes(files.Keys);
        if (totalBytes > MaxTotalBytes)
            throw new ModContentTooLargeException(totalBytes, MaxTotalBytes);
        if (files.Count == 0)
            return [];

        // 计算每个文件的哈希值（无哈希值的文件直接视为未命中）
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (fullPath, fileName) in files) {
            if (ComputeFileHash(fullPath) is { } hash)
                hashes[fileName] = hash;
        }

        //读取数据库
        var cached = ModDatabase.GetContentFiles(id);
        var roots = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
        var misses = new List<string>();
        //比对当前计算的哈希值和数据库缓存的值
        foreach (var (fileName, hash) in hashes) {
            if (cached.TryGetValue(fileName, out var entry) && entry.Hash == hash) {
                try {
                    if (TreeNodeConverter.Deserialize(entry.Data) is { } root) {
                        roots[fileName] = root;
                        continue;
                    }
                }
                catch {
                    // 缓存损坏：当作未命中重新解析
                }
            }

            misses.Add(fileName);
        }

        // 只解析未命中的文件（复用 Read 的并行解析逻辑）
        if (misses.Count > 0) {
            foreach (var (fileName, root) in Read([.. misses.Select(item => Path.Combine(rootPath, item))]))
                roots[fileName] = root;
            if (shouldSave) {
                foreach (var fileName in misses) {
                    if (!hashes.TryGetValue(fileName, out var hash) || !roots.TryGetValue(fileName, out var root))
                        continue;
                    try {
                        ModDatabase.SaveContentFile(id, fileName, hash, TreeNodeConverter.Serialize(root));
                    }
                    catch {
                        // 缓存写入失败不影响本次结果
                    }
                }
            }
        }

        // 清理已不存在的文件对应的缓存
        try {
            ModDatabase.PruneContentFiles(id, files.Values);
        }
        catch {
            // 清理失败只影响缓存体积，不影响本次结果
        }

        return [.. roots.Values.ToList()];
    }
}