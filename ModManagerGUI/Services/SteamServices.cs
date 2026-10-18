using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using ModManagerGUI.Base;

namespace ModManagerGUI.Services;

public partial class SteamServices {
    public const string RavenfieldAppId = "636480";
    
    public const int WorkshopDetailBatch = 50;
    public static string WorkshopPath { get; private set; }
    
    public static string? GetSteamPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (key?.GetValue("SteamPath") is not string steamPath)
            return null;
        return Path.GetFullPath(steamPath);
    }

    public static List<string> GetAllLibraryPaths()
    {
        var steamPath = GetSteamPath();
        if (steamPath is null)
            return [];

        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
            return [steamPath];

        var content = File.ReadAllText(vdfPath);
        var paths = LibraryPathRegex().Matches(content)
            .Select(m => Path.GetFullPath(m.Groups[1].Value.Replace(@"\\", @"\")))
            .ToList();

        return paths.Count > 0 ? paths : [steamPath];
    }
    
    public static string GetGameInstallPath(string appId)
    {
        var manifestFilename = $"appmanifest_{appId}.acf";
        foreach (var libPath in GetAllLibraryPaths())
        {
            var manifestPath = Path.Combine(libPath, "steamapps", manifestFilename);
            if (!File.Exists(manifestPath))
                continue;

            var match = InstallDirRegex().Match(File.ReadAllText(manifestPath));
            if (match.Success)
                return Path.Combine(libPath, "steamapps", "common", match.Groups[1].Value);
        }
        throw new UnavailableException("Couldn't find game installation path");
    }

    public static List<string> GetWorkshopInfo(string appId)
    {
        foreach (var libPath in GetAllLibraryPaths())
        {
            var workshopPath = Path.Combine(libPath, "steamapps", "workshop", "content", appId);
            if (!Directory.Exists(workshopPath))
                continue;
            WorkshopPath = workshopPath;
            return [
                .. Directory.EnumerateDirectories(workshopPath)
                    .Select(d => Path.GetFileName(d))
            ];
        }
        throw new UnavailableException("Couldn't find Workshop path");
    }

    [GeneratedRegex(@"""path""\s+""([^""]+)""")]
    private static partial Regex LibraryPathRegex();

    [GeneratedRegex(@"""installdir""\s+""([^""]+)""")]
    private static partial Regex InstallDirRegex();
}
