namespace YoutubeToMp4.Models;

public readonly record struct MuxedStreamSettings(
    string Filepath,
    int VideoQualityIndex,
    int AudioQualityIndex );