using YoutubeToMp4.Shared.Enums;

namespace YoutubeToMp4.Shared.Dtos;

public sealed record YoutubeDownloadParams
{
    public static YoutubeDownloadParams Create( string url, StreamType type, int quality, string start, string end, string thumbnail )
    {
        YoutubeDownloadTimeParams? times = !string.IsNullOrWhiteSpace( start ) && !string.IsNullOrWhiteSpace( end )
            ? new YoutubeDownloadTimeParams { Start = start, End = end }
            : null;
        
        return new YoutubeDownloadParams
        {
            VideoUrl = url,
            Type = type,
            QualityIndex = quality,
            Times = times,
            ThumbnailUrl = thumbnail
        };
    }
    
    public string VideoUrl { get; init; } = string.Empty;
    public StreamType Type { get; init; }
    public int QualityIndex { get; init; }
    public YoutubeDownloadTimeParams? Times { get; init; }
    public string ThumbnailUrl { get; init; } = string.Empty;
}