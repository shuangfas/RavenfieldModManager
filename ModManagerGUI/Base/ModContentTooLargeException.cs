namespace ModManagerGUI.Base;

public sealed class ModContentTooLargeException(long totalBytes, long limitBytes)
    : Exception($"Mod content size {totalBytes} bytes exceeds limit {limitBytes} bytes") {
    public long TotalBytes { get; } = totalBytes;
    public long LimitBytes { get; } = limitBytes;
}