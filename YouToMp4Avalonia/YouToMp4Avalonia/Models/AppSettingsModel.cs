using System.Collections.Generic;

namespace YouToMp4Avalonia.Models;

public sealed record AppSettingsModel
{
    public string ApiKey { get; init; } = string.Empty;
    public string DownloadLocation { get; init; } = string.Empty;
    public string FFmpegFilepath { get; init; } = string.Empty;
    public string SelectedBackgroundImage { get; init; } = string.Empty;

    public const string TransparentBackgroundKeyword = "Transparent";
    public const string DefaultBackgroundImage = "YouToMp4Avalonia.Assets.Backgrounds.forest.jpg";
    public static IReadOnlyList<string> BackgroundImages { get; } = [
        TransparentBackgroundKeyword,
        "YouToMp4Avalonia.Assets.Backgrounds.space.jpg",
        "YouToMp4Avalonia.Assets.Backgrounds.night_lights.jpg",
        "YouToMp4Avalonia.Assets.Backgrounds.forest.jpg",
        "YouToMp4Avalonia.Assets.Backgrounds.concert.jpg",
    ];
}