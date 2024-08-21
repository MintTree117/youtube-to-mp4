using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using YoutubeToMp4.Models;

namespace YoutubeToMp4.Services;

public sealed class FFmpegService
{
    // Fields
    readonly FileLogger _logger = FileLogger.Instance;
    
    const string FFmpegFolderName = "ffmpeg";
    const string FFmpegFileName = "ffmpeg";
    const string TempVideoFileName = "temp_video.mp4";
    const string TempThumbnailFileName = "temp_thumbnail.jpg";
    const string TempThumbnailConvertedFileName = "temp_thumbnail_converted.jpg";

    const string MessageNoBytesFound = "The image bytes for thumbnail are null!";
    const string MessageFailGetFFmpegPath = "Failed to get ffmpeg path!";
    const string MessageFailStartCutProcess = "Failed to start ffmpeg cut process!";
    const string MessageManualKillProcess = "Process was killed manually!";
    const string MessageFailImage = "Failed to create thumbnail image!";
    
    // Public Methods
    public async Task<Reply<bool>> TryCutVideo( string videoPath, TimeSpan startTime, TimeSpan endTime )
    {
        if ( !GetFFmpegPath( out string ffmpegPath ) )
        {
            Console.WriteLine( MessageFailGetFFmpegPath );
            return new Reply<bool>( ServiceErrorType.IoError, MessageFailGetFFmpegPath );   
        }

        string tempVideoPath = GetTempVideoPath(); // the cut video
        TimeSpan duration = endTime - startTime; // Calculate duration
        string args = $"-i \"{videoPath}\" -ss {startTime} -t {duration} -c:v copy -c:a copy \"{tempVideoPath}\""; // ffmpeg arguments

        using Process cutProcess = new();
        cutProcess.StartInfo.FileName = ffmpegPath;
        cutProcess.StartInfo.Arguments = args;
        cutProcess.StartInfo.UseShellExecute = false;
        cutProcess.StartInfo.RedirectStandardOutput = true;
        cutProcess.StartInfo.RedirectStandardError = true;
        cutProcess.StartInfo.CreateNoWindow = true;
        
        try
        {
            if ( !cutProcess.Start() )
            {
                _logger.LogWithConsole( MessageFailStartCutProcess );
                return new Reply<bool>( ServiceErrorType.ExternalError, MessageFailStartCutProcess );
            }
            
            await cutProcess.WaitForExitAsync();

            string message = await cutProcess.StandardOutput.ReadToEndAsync();
            string error = await cutProcess.StandardError.ReadToEndAsync();
            Console.WriteLine( $"FFMPEG CUT MESSAGE: {message}" );
            Console.WriteLine( $"FFMPEG CUT ERROR: {error}" );

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the cut video file to original path

            return new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            return new Reply<bool>( ServiceErrorType.ExternalError, e.Message );
        }
        finally
        {
            if ( !cutProcess.HasExited )
            {
                cutProcess.Kill();
                Console.WriteLine( MessageManualKillProcess );
            }
        }
    }
    public async Task<Reply<bool>> TryAddImage( string videoPath, byte[]? _thumbnailBytes )
    {
        if ( _thumbnailBytes is null )
        {
            _logger.LogWithConsole( MessageNoBytesFound );
            return new Reply<bool>( ServiceErrorType.NotFound, MessageNoBytesFound );
        }
        
        if ( !GetFFmpegPath( out string ffmpegPath ) )
        {
            _logger.LogWithConsole( MessageFailGetFFmpegPath );
            return new Reply<bool>( ServiceErrorType.IoError, MessageFailGetFFmpegPath );
        }

        string tempThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailFileName );
        string tempConvertedThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailConvertedFileName );
        string tempVideoPath = Path.Combine( Path.GetTempPath(), $"{TempVideoFileName}{Path.GetExtension( videoPath )}" );

        try
        {
            await File.WriteAllBytesAsync( tempThumbnailPath, _thumbnailBytes );
            await CreateJpgCopyFFMPEG( tempThumbnailPath, tempConvertedThumbnailPath, ffmpegPath );
            await CreateVideoWithImageFFMPEG( videoPath, tempConvertedThumbnailPath, tempVideoPath, ffmpegPath );

            if ( !File.Exists( tempVideoPath ) )
            {
                _logger.LogWithConsole( MessageFailImage );
                return new Reply<bool>( ServiceErrorType.ExternalError, MessageFailImage );
            }

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the temp file to original path

            return new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            _logger.LogWithConsole( e );
            return new Reply<bool>( ServiceErrorType.ExternalError, $"{MessageFailImage} : {e.Message}" );
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
    public async Task<Reply<bool>> MergeAudioAndVideo( string videoPath, string audioPath, string outputFilePath )
    {
        if (!GetFFmpegPath( out string ffmpegPath ))
        {
            Console.WriteLine( MessageFailGetFFmpegPath );
            return new Reply<bool>( ServiceErrorType.IoError, MessageFailGetFFmpegPath );
        }

        string args = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -strict experimental \"{outputFilePath}\"";

        using Process mergeProcess = new();
        mergeProcess.StartInfo.FileName = ffmpegPath;
        mergeProcess.StartInfo.Arguments = args;
        mergeProcess.StartInfo.UseShellExecute = false;
        mergeProcess.StartInfo.RedirectStandardOutput = true;
        mergeProcess.StartInfo.RedirectStandardError = true;
        mergeProcess.StartInfo.CreateNoWindow = true;

        try
        {
            if (!mergeProcess.Start())
            {
                _logger.LogWithConsole( "Failed to start FFmpeg merge process!" );
                return new Reply<bool>( ServiceErrorType.ExternalError, "Failed to start FFmpeg merge process!" );
            }

            await mergeProcess.WaitForExitAsync();

            var message = await mergeProcess.StandardOutput.ReadToEndAsync();
            var error = await mergeProcess.StandardError.ReadToEndAsync();

            return mergeProcess.ExitCode != 0 
                ? new Reply<bool>( ServiceErrorType.ExternalError, $"FFmpeg merge process failed with exit code {mergeProcess.ExitCode} : {message} : {error}" ) 
                : new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            return new Reply<bool>( ServiceErrorType.ExternalError, e.Message );
        }
        finally
        {
            if (!mergeProcess.HasExited)
            {
                mergeProcess.Kill();
                Console.WriteLine( "FFmpeg merge process was killed manually!" );
            }
        }
    }
    
    // Private Utils
    static async Task CreateJpgCopyFFMPEG( string inputPath, string outputPath, string ffmpegPath )
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
            Console.WriteLine( e );
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
            string message = await createProcess.StandardOutput.ReadToEndAsync();
            string error = await createProcess.StandardError.ReadToEndAsync();
            Console.WriteLine( $"FFmpeg Output: {message}" );
            Console.WriteLine($"FFmpeg Error: {error}");
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
        }
        finally
        {
            if ( !createProcess.HasExited )
                createProcess.Kill();
        }
    }
    
    static bool GetFFmpegPath( out string path )
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string ffmpegFolder = Path.Combine( baseDirectory, FFmpegFolderName );
        path = Path.Combine( ffmpegFolder, FFmpegFileName );
        Console.WriteLine($"FFmpeg path: {path}");
        return File.Exists( path );
    }
    static string GetTempVideoPath()
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine( baseDirectory, TempVideoFileName );
    }
}