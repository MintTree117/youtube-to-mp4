namespace YoutubeToMp4Blazor.Models;

public sealed record DlKeyRecord
{
    public int KeyId { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public DlRequestType RequestType { get; init; }
    public DateTime DateCreated { get; init; }
}