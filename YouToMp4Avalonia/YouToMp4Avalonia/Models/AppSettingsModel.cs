namespace YouToMp4Avalonia.Models;

public sealed record AppSettingsModel
{
    public string DownloadLocation { get; init; } = string.Empty;
    public string FFmpegFilepath { get; init; } = string.Empty;
}