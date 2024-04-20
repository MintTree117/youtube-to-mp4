namespace YoutubeToMp4Blazor.Models;

public sealed record DlKey
{
    public int Id { get; init; }
    public string KeyString { get; init; } = string.Empty;
    public string KeyHolder { get; init; } = string.Empty;
}