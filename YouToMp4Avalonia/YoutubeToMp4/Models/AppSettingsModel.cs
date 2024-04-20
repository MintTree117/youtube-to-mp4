using System.Text.Json.Serialization;

namespace YoutubeToMp4.Models;

public sealed record AppSettingsModel
{
    const string DefaultDownloadDirectory = "./";
    public string DownloadLocation { get; init; } = DefaultDownloadDirectory;
}

[JsonSourceGenerationOptions( WriteIndented = true )] [JsonSerializable( typeof( AppSettingsModel ) )]
internal sealed partial class AppSettingsModelContext : JsonSerializerContext
{
}