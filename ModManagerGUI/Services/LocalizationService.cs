using System.Windows;

namespace ModManagerGUI.Services;

/// <summary>
/// 中英双语切换：交换 Application 资源中的语言字典。
/// 界面文本用 DynamicResource 绑定，切换即时生效。
/// </summary>
public static class LocalizationService
{
    private static string _current = "zh-CN";

    public static string Current => _current;

    public static void SetLanguage(string code)
    {
        if (code == _current)
            return;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var uri = new Uri(
            $"pack://application:,,,/ModManagerGUI;component/Localization/{code}.xaml");

        var existing = dictionaries.FirstOrDefault(
            d => d.Source?.OriginalString.Contains("/Localization/") == true);
        if (existing is null)
            dictionaries.Add(new ResourceDictionary { Source = uri });
        else
            existing.Source = uri;

        _current = code;
    }

    public static void Toggle() =>
        SetLanguage(_current == "zh-CN" ? "en-US" : "zh-CN");

    /// <summary>按 key 取当前语言的界面文本；资源缺失时返回 key 本身</summary>
    public static string L(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;
}
