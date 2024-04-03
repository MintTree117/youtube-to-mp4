using System.Diagnostics;

namespace YouToMp4Blazor.Features.FFmpeg;

public sealed class FFmpegService( ILogger<FFmpegService> logger )
{
    readonly ILogger<FFmpegService> _logger = logger;

    const string FFmpegPath = "/ffmpeg-6.1.1/ffmpeg";
    const string TempVideoFileName = "temp_video.mp4";
    const string TempThumbnailFileName = "temp_thumbnail.jpg";
    const string TempThumbnailConvertedFileName = "temp_thumbnail_converted.jpg";

    public async Task<bool> TryCutVideo( string videoPath, TimeSpan startTime, TimeSpan endTime )
    {
        if ( !GetFFmpegPath( out string ffmpegPath ) )
            return false;

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
                _logger.LogError( "Failed to start ffmpeg cut process!" );
                return false;
            }
            
            await cutProcess.WaitForExitAsync();

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the cut video file to original path

            _logger.LogInformation( "Video cut successfully!" );
            return true;
        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            return false;
        }
        finally
        {
            if ( !cutProcess.HasExited )
            {
                cutProcess.Kill();
                _logger.LogInformation( "Process was killed manually!" );
            }
        }
    }
    public async Task<bool> TryAddThumbnail( string videoPath, string thumbnailUrl )
    {
        if ( !GetFFmpegPath( out string ffmpegPath ) )
            return false;
        
        string tempPath = Path.GetTempPath();
        
        string tempThumbnailPath = Path.Combine( tempPath, TempThumbnailFileName );
        string tempConvertedThumbnailPath = Path.Combine( tempPath, TempThumbnailConvertedFileName );
        string tempVideoPath = GetTempVideoPath();

        try
        {
            // Load the image bytes from net and write to temp filepath
            byte[] imgBytes = await LoadStreamThumbnailImage( thumbnailUrl );
            await File.WriteAllBytesAsync( tempThumbnailPath, imgBytes );
            
            // Create a copy file and write basically create a new .mp4 with the thumbnail in it
            await CreateJpgCopyFFMPEG( tempThumbnailPath, tempConvertedThumbnailPath, ffmpegPath );
            await CreateVideoWithImageFFMPEG( videoPath, tempConvertedThumbnailPath, tempVideoPath, ffmpegPath );
            
            // It failed if we dont have the final file
            if ( !File.Exists( tempVideoPath ) )
            {
                _logger.LogError( "Failed to generate thumbnail for video!" );
                return false;   
            }

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the temp file to original path
            return true;
        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            return false;
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

    static async Task<byte[]> LoadStreamThumbnailImage( string url )
    {
        HttpClient client = new();

        Stream stream = await client.GetStreamAsync( url );

        if ( !stream.CanRead )
        {
            Console.WriteLine( "Can't read image stream! IN ffmpeg service LoadStreamThumbnailImage" );
            return [ ];
        }
        
        // stream.Position = 0; // not supported 

        await using MemoryStream memoryStream = new();
        await stream.CopyToAsync( memoryStream );
        await stream.DisposeAsync();

        client.Dispose();

        return memoryStream.ToArray();
    }
    async Task CreateJpgCopyFFMPEG( string inputFilepath, string outputPath, string ffmpegPath )
    {
        string args = $"-i \"{inputFilepath}\" \"{outputPath}\"";
        
        using Process conversionProcess = new();
        conversionProcess.StartInfo.FileName = ffmpegPath;
        conversionProcess.StartInfo.Arguments = args;
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
            _logger.LogError( e, e.Message );
        }
        finally
        {
            if ( !conversionProcess.HasExited )
            {
                conversionProcess.Kill();
                _logger.LogInformation( "Process was killed manually" );
            }
        }
    }
    async Task CreateVideoWithImageFFMPEG( string videoPath, string convertedThumbnailPath, string tempOutputPath, string ffmpegPath )
    {
        string args = $"-i \"{videoPath}\" -i \"{convertedThumbnailPath}\" -map 0 -map 1 -c copy -disposition:v:1 attached_pic \"{tempOutputPath}\"";
        
        using Process createProcess = new();
        createProcess.StartInfo.FileName = ffmpegPath;
        createProcess.StartInfo.Arguments = args;
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
            _logger.LogError( e, e.Message );
        }
        finally
        {
            if ( !createProcess.HasExited )
            {
                createProcess.Kill();
                _logger.LogInformation( "Process was killed manually" );
            }
        }
    }
    
    static bool GetFFmpegPath( out string path )
    {
        path = $"{Directory.GetCurrentDirectory()}{FFmpegPath}";
        return File.Exists( path );
    }
    static string GetTempVideoPath()
    {
        return Path.Combine( Path.GetTempPath(), TempVideoFileName );
    }
}