using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ModManagerGUI.Services;

public class ConfigReader<T> where T : class, new() {
    private static readonly JsonSerializerSettings DefaultSettings = new() {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    private readonly string _path;

    public T Current { get; private set; } = new();

    public ConfigReader(string path) {
        _path = path;
    }

    /// <summary>从磁盘加载；文件不存在时创建默认文件。失败抛异常。</summary>
    public T Load(JsonSerializerSettings? settings = null) {
        settings ??= DefaultSettings;

        if (!File.Exists(_path)) {
            WriteToDisk(Current, settings);
            return Current;
        }

        var json = File.ReadAllText(_path, Encoding.UTF8);
        var loaded = JsonConvert.DeserializeObject<T>(json, settings)
                     ?? throw new InvalidDataException("配置文件反序列化结果为 null");

        OnLoaded(loaded);
        Current = loaded;
        return Current;
    }

    /// <summary>整体替换并保存。</summary>
    public void Save(T config, JsonSerializerSettings? settings = null) {
        WriteToDisk(config, settings ?? DefaultSettings);
        Current = config;
    }

    /// <summary>就地修改若干项后自动保存，适合多字段一起改。</summary>
    public void Update(Action<T> mutate, JsonSerializerSettings? settings = null) {
        mutate(Current);
        Save(Current, settings);
    }

    /// <summary>反序列化后的修正钩子，默认什么也不做。</summary>
    protected virtual void OnLoaded(T config) { }

    private void WriteToDisk(T config, JsonSerializerSettings settings) {
        var json = JsonConvert.SerializeObject(config, settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_path));
        File.WriteAllText(_path, json, Encoding.UTF8);
    }
}