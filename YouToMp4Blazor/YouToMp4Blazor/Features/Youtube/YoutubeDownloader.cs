using System.Globalization;
using YouToMp4.Shared.Dtos;
using YouToMp4.Shared.Enums;
using YouToMp4Blazor.Features.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YouToMp4Blazor.Features.Youtube;

public sealed class YoutubeDownloader( IServiceProvider serviceProvider, ILogger<YoutubeDownloader> logger )
{
    // Fields
    readonly string TempDownloadPath = Path.Combine( Directory.GetCurrentDirectory(), "temp_download.mp4" );
    
    // Services
    readonly IServiceProvider _serviceProvider = serviceProvider;
    readonly ILogger<YoutubeDownloader> _logger = logger;
    readonly YoutubeClient _youtube = new();
    
    // Stream
    StreamManifest _streamManifest = null!;

    // Public Methods
    public async Task<bool> TryInitialize( string videoUrl )
    {
        try
        {
            _streamManifest = await _youtube.Videos.Streams.GetManifestAsync( videoUrl );
            return true;
        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            return false;
        }
    }
    public async Task<Stream?> Download( YoutubeDownloadParams dlParams )
    {
        Stream memoryStream = new MemoryStream();
        
        try
        {
            // Get Stream Information
            if ( !GetStreamInfo( dlParams, out IStreamInfo? streamInfo ) || streamInfo is null )
                return null;
            
            // Early Exit
            if ( !RequiresFFmpeg( dlParams ) )
            {
                await _youtube.Videos.Streams.CopyToAsync( streamInfo, memoryStream );
                memoryStream.Position = 0;
                return memoryStream;
            }
            
            // FFmpeg
            if ( await HandleFFmpeg( dlParams, streamInfo, TempDownloadPath ) )
            {
                await using FileStream filestream = new( TempDownloadPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
                await filestream.CopyToAsync( memoryStream );

                if ( File.Exists( TempDownloadPath ) )
                    File.Delete( TempDownloadPath );
                
                memoryStream.Seek( 0, SeekOrigin.Begin );
                return memoryStream;
            }
            
            // Cleanup
            await memoryStream.DisposeAsync();
            if ( File.Exists( TempDownloadPath ) )
                File.Delete( TempDownloadPath );
            
            return null;
        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            await memoryStream.DisposeAsync();
            return null;
        }
    }
    
    // Private Methods
    bool GetStreamInfo( YoutubeDownloadParams dlParams, out IStreamInfo? info )
    {
        info = dlParams.Type switch
        {
            StreamType.Mixed => GetMuxedStreamInfo( _streamManifest, dlParams.QualityIndex ),
            StreamType.Audio => GetAudioOnlyStreamInfo( _streamManifest, dlParams.QualityIndex ),
            StreamType.Video => GetVideoOnlyStreamInfo( _streamManifest, dlParams.QualityIndex ),
            _ => null
        };
        
        if ( info is not null ) 
            return true;
        
        _logger.LogError( "StreamInfo is null upon info request!" );
        return false;
    }
    static MuxedStreamInfo? GetMuxedStreamInfo( StreamManifest manifest, int qualityIndex )
    {
        List<MuxedStreamInfo> streams = manifest.GetMuxedStreams().ToList();
        bool isValidQualityIndex = qualityIndex >= 0 && qualityIndex < streams.Count;
        return isValidQualityIndex ? streams[ qualityIndex ] : null;
    }
    static AudioOnlyStreamInfo? GetAudioOnlyStreamInfo( StreamManifest manifest, int qualityIndex )
    {
        List<AudioOnlyStreamInfo> streams = manifest.GetAudioOnlyStreams().ToList();
        bool isValidQualityIndex = qualityIndex >= 0 && qualityIndex < streams.Count;
        return isValidQualityIndex ? streams[ qualityIndex ] : null;
    }
    static VideoOnlyStreamInfo? GetVideoOnlyStreamInfo( StreamManifest manifest, int qualityIndex )
    {
        List<VideoOnlyStreamInfo> streams = manifest.GetVideoOnlyStreams().ToList();
        bool isValidQualityIndex = qualityIndex >= 0 && qualityIndex < streams.Count;
        return isValidQualityIndex ? streams[ qualityIndex ] : null;
    }
    static bool RequiresFFmpeg( YoutubeDownloadParams dlParams )
    {
        return dlParams.Times is not null || !string.IsNullOrWhiteSpace( dlParams.ThumbnailUrl );
    }
    async Task<bool> HandleFFmpeg( YoutubeDownloadParams dlParams, IStreamInfo streamInfo, string tempVideoPath )
    {
        FFmpegService? ffmpeg = GetFFmpegService();

        if ( ffmpeg is null )
            return false;
        
        await _youtube.Videos.Streams.DownloadAsync( streamInfo, tempVideoPath );

        if ( !await HandleVideoCutting( tempVideoPath, dlParams, ffmpeg ) )
            return false;

        if ( !await HandleThumbnail( tempVideoPath, dlParams, ffmpeg ) )
            _logger.LogError( "Failed to handle thumbnail for youtube video download!" );

        return true;

        FFmpegService? GetFFmpegService()
        {
            var _ffmpegService = _serviceProvider.GetService<FFmpegService>();

            if ( _ffmpegService is not null )
                return _ffmpegService;

            _logger.LogError( "Failed to get ffmpeg service from provider!" );
            return null;
        }
    }
    async Task<bool> HandleVideoCutting( string inputPath, YoutubeDownloadParams dlParams, FFmpegService ffmpeg )
    {
        if ( dlParams.Times is null ) 
            return true;

        if ( !TryParseVideoDlTimes( dlParams.Times, out TimeSpan? start, out TimeSpan? end ) )
        {
            _logger.LogError( "Failed to parse dl time params on server!" );
            return false;   
        }
        
        bool success = await ffmpeg.TryCutVideo( inputPath, start!.Value, end!.Value );
        
        if ( success ) 
            return true;
        
        _logger.LogError( "Failed to cut video!" );
        return false;
    }
    async Task<bool> HandleThumbnail( string videoPath, YoutubeDownloadParams dlParams, FFmpegService ffmpeg )
    {
        if ( string.IsNullOrWhiteSpace( dlParams.ThumbnailUrl ) ) 
            return true;
        
        if ( await ffmpeg.TryAddThumbnail( videoPath, dlParams.ThumbnailUrl ) ) 
            return true;
            
        _logger.LogError( "Failed to get thumbnail for video!" );
        return false;
    }
    static bool TryParseVideoDlTimes( YoutubeDownloadTimeParams timeParams , out TimeSpan? startTime, out TimeSpan? endTime )
    {
        startTime = null;
        endTime = null;
        
        if ( string.IsNullOrWhiteSpace( timeParams.Start ) || string.IsNullOrWhiteSpace( timeParams.End ) )
            return false;
        
        if ( !TimeSpan.TryParseExact( timeParams.Start, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan parsedStartTime ) )
            return false;
        
        if ( !TimeSpan.TryParseExact( timeParams.End, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan parsedEndTime ) )
            return false;

        startTime = parsedStartTime;
        endTime = parsedEndTime;

        return true;
    }
}