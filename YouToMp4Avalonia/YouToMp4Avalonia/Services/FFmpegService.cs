using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace YouToMp4Avalonia.Services;

public sealed class FFmpegService
{
    readonly FileLogger Logger = FileLogger.Instance;

    
    const string FFmpegFolderName = "ffmpeg";
    const string FFmpegFileName = "ffmpeg";
    const string TempVideoFileName = "temp_video.mp4";
    const string TempThumbnailFileName = "temp_thumbnail.jpg";
    const string TempThumbnailConvertedFileName = "temp_thumbnail_converted.jpg";
    
    public async Task AddImage( string videoPath, byte[]? _thumbnailBytes )
    {
        if ( _thumbnailBytes is null || !GetFFmpegPath( out string ffmpegPath ) )
            return;

        string tempThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailFileName );
        string tempConvertedThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailConvertedFileName );
        string tempVideoPath = Path.Combine( Path.GetTempPath(), $"{TempVideoFileName}{Path.GetExtension( videoPath )}" );

        try
        {
            await File.WriteAllBytesAsync( tempThumbnailPath, _thumbnailBytes );
            await CreateJpgCopyFFMPEG( tempThumbnailPath, tempConvertedThumbnailPath, ffmpegPath );
            await CreateVideoWithImageFFMPEG( videoPath, tempConvertedThumbnailPath, tempVideoPath, ffmpegPath );

            if ( !File.Exists( tempVideoPath ) )
                return;

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the temp file to original path
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( e );
        }
        finally
        {
            if ( File.Exists( tempThumbnailPath ) )
                File.Delete( tempThumbnailPath );
            
            if ( File.Exists( tempConvertedThumbnailPath ) )
                File.Delete( tempConvertedThumbnailPath );
            
            if ( File.Exists( tempVideoPath ) )
                File.Delete( tempVideoPath );
        }
    }
    async Task CreateJpgCopyFFMPEG( string inputPath, string outputPath, string ffmpegPath )
    {
        using Process conversionProcess = new();
        conversionProcess.StartInfo.FileName = ffmpegPath;
        conversionProcess.StartInfo.Arguments = $"-i \"{inputPath}\" \"{outputPath}\"";
        conversionProcess.StartInfo.RedirectStandardOutput = true;
        conversionProcess.StartInfo.RedirectStandardError = true;
        conversionProcess.StartInfo.UseShellExecute = false;
        conversionProcess.StartInfo.CreateNoWindow = true;

        try
        {
            conversionProcess.Start();
            await conversionProcess.WaitForExitAsync();
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( e );
        }
        finally
        {
            if ( !conversionProcess.HasExited )
                conversionProcess.Kill();
        }
    }
    async Task CreateVideoWithImageFFMPEG( string videoPath, string convertedThumbnailPath, string tempOutputPath, string ffmpegPath )
    {
        using Process createProcess = new();
        createProcess.StartInfo.FileName = ffmpegPath; // Or the full path to the ffmpeg executable
        createProcess.StartInfo.Arguments = $"-i \"{videoPath}\" -i \"{convertedThumbnailPath}\" -map 0 -map 1 -c copy -disposition:v:1 attached_pic \"{tempOutputPath}\"";
        createProcess.StartInfo.RedirectStandardOutput = true;
        createProcess.StartInfo.RedirectStandardError = true;
        createProcess.StartInfo.UseShellExecute = false;
        createProcess.StartInfo.CreateNoWindow = true;

        try
        {
            createProcess.Start();
            await createProcess.WaitForExitAsync();
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( e );
        }
        finally
        {
            if ( !createProcess.HasExited )
                createProcess.Kill();
        }
    }

    static bool GetFFmpegPath( out string path )
    {
        /*string currentDirectory = Directory.GetCurrentDirectory();
        path = Path.Combine( currentDirectory, FFmpegPath );
        return File.Exists( path );*/

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string ffmpegFolder = Path.Combine( baseDirectory, FFmpegFolderName );
        path = Path.Combine( ffmpegFolder, FFmpegFileName );
        return File.Exists( path );
    }
}