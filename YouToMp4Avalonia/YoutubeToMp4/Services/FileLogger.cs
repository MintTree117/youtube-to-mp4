using System;
using System.IO;
using System.Threading;

namespace YoutubeToMp4.Services;

public sealed class FileLogger : SingletonService<FileLogger>
{
    // Consts
    static readonly string LogDirectory = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "dlTubeAvalonia", "Logs" );
    static readonly string LogPath = Path.Combine( LogDirectory, "logs.txt" );
    static readonly SemaphoreSlim _semaphore = new( 1, 1 );

    public static async void Log( string message )
    {
        try
        {
            await _semaphore.WaitAsync();

            if ( !Directory.Exists( LogDirectory ) )
                Directory.CreateDirectory( LogDirectory );

            if ( !File.Exists( LogPath ) )
                await File.WriteAllTextAsync( LogPath, message + Environment.NewLine );
            else
                await File.AppendAllTextAsync( LogPath, message );
        }
        catch ( Exception e )
        {
            Console.WriteLine( $"{e} : {e.Message}" );
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public static void LogEx( Exception e, string? message = null )
    {
        string m = string.IsNullOrWhiteSpace( message )
            ? $"{e} : {e.Message}"
            : $"{message} : {e} : {e.Message}";

        Log( m );
    }
    public void LogWithConsole( string message )
    {
        Console.WriteLine( message );
        Log( message );
    }
    public void LogWithConsole( Exception e, string? message = null )
    {
        string m = string.IsNullOrWhiteSpace( message )
            ? $"{e} : {e.Message}"
            : $"{message} : {e} : {e.Message}";

        Console.WriteLine( m );
        Log( m );
    }
}