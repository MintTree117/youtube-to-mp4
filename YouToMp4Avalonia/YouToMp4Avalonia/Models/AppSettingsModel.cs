using System.Text.Json.Serialization;

namespace YouToMp4Avalonia.Models;

public sealed record AppSettingsModel
{
    public string DownloadLocation { get; init; } = string.Empty;
}

[JsonSourceGenerationOptions( WriteIndented = true )] [JsonSerializable( typeof( AppSettingsModel ) )]
internal sealed partial class AppSettingsModelContext : JsonSerializerContext
{
}