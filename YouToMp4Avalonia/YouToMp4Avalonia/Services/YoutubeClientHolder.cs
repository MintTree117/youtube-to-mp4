using YoutubeExplode;

namespace YouToMp4Avalonia.Services;

public sealed class YoutubeClientHolder
{
    public YoutubeClient YoutubeClient { get; init; } = new();
}