using System;
using Microsoft.Extensions.DependencyInjection;

namespace YouToMp4Avalonia.Services;

public abstract class BaseService
{
    protected readonly FileLogger Logger = Program.ServiceProvider.GetService<FileLogger>()!;
    protected static string ExString( Exception e, string? message = null )
    {
        return string.IsNullOrWhiteSpace( message )
            ? $"{e} : {e.Message}"
            : $"{message} : {e} : {e.Message}";
    }
}