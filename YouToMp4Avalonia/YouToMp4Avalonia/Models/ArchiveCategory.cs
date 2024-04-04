namespace YouToMp4Avalonia.Models;

public sealed record ArchiveCategory
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}