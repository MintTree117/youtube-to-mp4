using System;

namespace YoutubeToMp4.Models;

public readonly record struct StreamSettings( 
    string Filepath, StreamType Type, int QualityIndex, TimeSpan? Start, TimeSpan? End );