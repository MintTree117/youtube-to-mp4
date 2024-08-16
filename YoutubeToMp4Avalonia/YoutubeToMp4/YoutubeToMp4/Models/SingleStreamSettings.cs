namespace YoutubeToMp4.Models;

public readonly record struct SingleStreamSettings( 
    string Filepath, 
    StreamType Type, 
    int QualityIndex );