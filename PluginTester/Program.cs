using System;
using System.Collections.Generic;
using System.IO;
using BepInExPlugin;

namespace ConfigReaderTest;

public static class Program {
    public static void Main() {
        string path = Path.Combine(AppContext.BaseDirectory, "data", "test.json");
        var reader = new ConfigReader(path);

        Console.WriteLine($"[1] Load (文件不存在, 应生成默认文件): {path}");
        List<Playset> playsets = reader.Load();
        Console.WriteLine($"    Playsets 数量: {playsets.Count}");

        Console.WriteLine("[2] Update 添加两个 playset 并保存");
        reader.Update(list => {
            list.Add(new Playset { Name = "默认清单", Id = 1, Enabled = true, ModIds = new List<string> { "mod.a", "mod.b" } });
            list.Add(new Playset { Name = "测试清单", Id = 2, Enabled = false, ModIds = new List<string>() });
        });

        Console.WriteLine("[3] 重新 Load, 验证文件内容");
        var reloaded = new ConfigReader(path).Load();
        foreach (Playset p in reloaded) {
            Console.WriteLine($"    Id={p.Id} Name={p.Name} Enabled={p.Enabled} ModIds=[{string.Join(", ", p.ModIds)}]");
        }

        Console.WriteLine("[4] 生成的 JSON 内容:");
        Console.WriteLine(File.ReadAllText(path));
    }
}
