namespace YoutubeToMp4.Shared.Dtos;

public sealed record StreamQualityInfoDto
{
    public List<string> IndexedQualityContainers { get; init; } = [ ];
    public List<string> IndexedQualityDetails { get; init; } = [ ];
}