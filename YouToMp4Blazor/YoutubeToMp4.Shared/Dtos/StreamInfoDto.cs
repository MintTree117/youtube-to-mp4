namespace YoutubeToMp4.Shared.Dtos;

public sealed record StreamInfoDto
{
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;

    public StreamQualityInfoDto Qualities { get; init; } = new();
}