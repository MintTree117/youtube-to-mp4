using Avalonia.Media.Imaging;

namespace YouToMp4Avalonia.Models;

public sealed record ArchiveItem
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string UploadDate { get; init; } = string.Empty;
    public Bitmap? Image { get; init; }
}