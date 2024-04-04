using System.Collections.Generic;

namespace YouToMp4Avalonia.Models;

public sealed record ArchiveSearch
{
    public List<ArchiveItem> Items { get; init; } = [ ];
    public int TotalMatches { get; init; }
}