using Avalonia.Media.Imaging;

namespace YouToMp4Avalonia.Models;

public sealed record YoutubeSearchResult
{
    public string Title { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public Bitmap? Image { get; init; }
}