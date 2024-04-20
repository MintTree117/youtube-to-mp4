namespace YoutubeToMp4Blazor.Models;

public sealed record DlKeyRequest
{
    public DateTime DateCreated { get; init; } = DateTime.Now;
    public DlRequestType RequestType { get; init; }
}