namespace YouToMp4.Shared.Dtos;

public sealed record YoutubeDownloadTimeParams
{
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
}