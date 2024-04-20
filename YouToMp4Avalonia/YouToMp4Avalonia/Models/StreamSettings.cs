using System;

namespace YouToMp4Avalonia.Models;

public record StreamSettings( string Filepath, StreamType Type, int QualityIndex, TimeSpan? Start, TimeSpan? End );