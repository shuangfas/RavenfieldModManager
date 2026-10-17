using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModManagerGUI.Services;

/// <summary>
/// 缩略图处理（对应 Python util/thumbnails.py）：
/// 居中裁方 + 缩放到 200×200，无图时使用深灰 "None" 占位图
/// </summary>
public static class ThumbnailService
{
    private const int ThumbSize = 200;

    private static ImageSource? _placeholder;

    /// <summary>占位缩略图（对应 Python build_placeholder，惰性生成一次）</summary>
    public static ImageSource Placeholder => _placeholder ??= BuildPlaceholder();

    private static ImageSource BuildPlaceholder()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0x2A, 0x2F, 0x3A)),
                null,
                new Rect(0, 0, ThumbSize, ThumbSize));

            var text = new FormattedText(
                "None",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                24,
                Brushes.Gray,
                1);
            dc.DrawText(text, new Point(
                (ThumbSize - text.Width) / 2,
                (ThumbSize - text.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(
            ThumbSize, ThumbSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// 加载模组图标：居中裁方并缩放到 200×200。
    /// 可在任意线程调用（返回的位图已 Freeze）。
    /// 文件缺失或解码失败时返回 null（调用方保留占位图）。
    /// </summary>
    public static ImageSource? Load(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var source = new BitmapImage();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            source.UriSource = new Uri(path);
            source.EndInit();

            var width = source.PixelWidth;
            var height = source.PixelHeight;
            if (width == 0 || height == 0)
                return null;

            var side = Math.Min(width, height);
            var cropped = new CroppedBitmap(source, new Int32Rect(
                (width - side) / 2, (height - side) / 2, side, side));
            var scaled = new TransformedBitmap(
                cropped, new ScaleTransform(ThumbSize / (double)side, ThumbSize / (double)side));
            scaled.Freeze();
            return scaled;
        }
        catch
        {
            return null;
        }
    }
}
