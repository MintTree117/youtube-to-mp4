namespace YouToMp4Avalonia.Models;

public sealed record AppSettingsModel
{
    public string DownloadLocation { get; init; } = string.Empty;
}