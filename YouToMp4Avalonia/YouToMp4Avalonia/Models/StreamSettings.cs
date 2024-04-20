using System;

namespace YouToMp4Avalonia.Models;

public readonly record struct StreamSettings( 
    string Filepath, StreamType Type, int QualityIndex, TimeSpan? Start, TimeSpan? End );