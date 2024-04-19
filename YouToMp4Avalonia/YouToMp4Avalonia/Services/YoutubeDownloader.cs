using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using YouToMp4Avalonia.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YouToMp4Avalonia.Services;

public sealed class YoutubeDownloader( string videoUrl ) : BaseService
{
    // Constants
    const string TempThumbnailFileName = "thumbnail.jpg";
    const string TempThumbnailConvertedFileName = "thumbnail_converted.jpg";
    
    // Services
    readonly SettingsManager _settingsManager = Program.ServiceProvider.GetService<SettingsManager>()!;
    readonly HttpController _httpService = Program.ServiceProvider.GetService<HttpController>()!;
    
    // Youtube Client
    readonly YoutubeClient _youtubeClient = new();
    
    // Video Url From Constructor
    readonly string _videoUrl = videoUrl;
    
    // Stream Data
    public string? VideoName => _video?.Title;
    public TimeSpan? VideoDuration => _video?.Duration;
    public string? VideoImage => _video?.Thumbnails.FirstOrDefault()?.Url;
    public byte[]? ThumbnailBytes => _thumbnailBytes;
    public Bitmap? ThumbnailBitmap => _thumbnailBitmap;
    StreamManifest? _streamManifest;
    Video? _video;
    Bitmap? _thumbnailBitmap;
    byte[]? _thumbnailBytes;
    
    // Streams
    List<MuxedStreamInfo> _mixedStreams = [ ];
    List<AudioOnlyStreamInfo> _audioStreams = [ ];
    List<VideoOnlyStreamInfo> _videoStreams = [ ];
    
    // Stream Qualities
    List<string>? _mixedSteamQualities;
    List<string>? _audioSteamQualities;
    List<string>? _videoSteamQualities;
    
    // Public Methods
    public async Task<ServiceReply<bool>> TryInitialize()
    {
        try
        {
            _streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync( _videoUrl );
            _video = await _youtubeClient.Videos.GetAsync( _videoUrl );
            
            // Get Video Image Data
            await LoadStreamThumbnailImage( _video.Thumbnails.Count > 0 ? _video.Thumbnails[ 0 ].Url : "" );
            
            return _streamManifest is not null && _video is not null
                ? new ServiceReply<bool>( true )
                : new ServiceReply<bool>( ServiceErrorType.NotFound );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            return new ServiceReply<bool>( ServiceErrorType.ServerError );
        }
    }
    public async Task<List<string>> GetStreamInfo( StreamType steamType )
    {
        return steamType switch
        {
            StreamType.Mixed => await GetMixedStreams(),
            StreamType.Audio => await GetAudioStreams(),
            StreamType.Video => await GetVideoStreams(),
            _ => throw new Exception( "Invalid Stream Type!" )
        };
    }
    public async Task<ServiceReply<bool>> Download( string filepath, StreamType type, int qualityIndex )
    {
        try
        {
            IStreamInfo streamInfo = type switch
            {
                StreamType.Mixed => _mixedStreams[ qualityIndex ],
                StreamType.Audio => _audioStreams[ qualityIndex ],
                StreamType.Video => _videoStreams[ qualityIndex ],
                _ => throw new ArgumentOutOfRangeException( nameof( type ), type, null )
            };
            
            string path = ConstructDownloadPath( filepath, streamInfo.Container.Name );
            
            await _youtubeClient.Videos.Streams.DownloadAsync( streamInfo, path );
            await AddImage( path );
            
            return new ServiceReply<bool>( true );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            return new ServiceReply<bool>( ServiceErrorType.ServerError );
        }
    }

    // Get Streams
    async Task<List<string>> GetMixedStreams()
    {
        return await Task.Run( () => {
            if ( _mixedSteamQualities is not null )
                return _mixedSteamQualities;

            _mixedStreams = _streamManifest!.GetMuxedStreams().ToList();
            _mixedSteamQualities = [ ];

            for ( int i = 0; i < _mixedStreams.Count; i++ )
            {
                MuxedStreamInfo stream = _mixedStreams[ i ];
                _mixedSteamQualities.Add( $"{i + 1} : {stream.VideoResolution} px - {stream.Bitrate} bps - {stream.Container}" );
            }

            return _mixedSteamQualities;
        } );
    }
    async Task<List<string>> GetAudioStreams()
    {
        return await Task.Run( () => {
            if ( _audioSteamQualities is not null )
                return _audioSteamQualities;

            _audioStreams = _streamManifest!.GetAudioOnlyStreams().ToList();

            _audioSteamQualities = [ ];

            for ( int i = 0; i < _audioStreams.Count; i++ )
            {
                AudioOnlyStreamInfo stream = _audioStreams[ i ];
                _audioSteamQualities.Add( $"{i + 1} : {stream.Bitrate} bps - {stream.Container}" );
            }

            return _audioSteamQualities;
        } );
    }
    async Task<List<string>> GetVideoStreams()
    {
        return await Task.Run( () => {
            if ( _videoSteamQualities is not null )
                return _videoSteamQualities;

            _videoStreams = _streamManifest!.GetVideoOnlyStreams().ToList();

            _videoSteamQualities = [ ];

            for ( int i = 0; i < _videoStreams.Count; i++ )
            {
                VideoOnlyStreamInfo stream = _videoStreams[ i ];
                _videoSteamQualities.Add( $"{i + 1} : {stream.VideoResolution} px - {stream.Container}" );
            }

            return _videoSteamQualities;
        } );
    }
    
    // Get Download Path
    string ConstructDownloadPath( string outputDirectory, string fileExtension )
    {
        string videoName = SanitizeVideoName( _video!.Title );
        string fileName = $"{videoName}.{fileExtension}";
        return Path.Combine( outputDirectory, fileName );

        static string SanitizeVideoName( string videoName )
        {
            return Path.GetInvalidFileNameChars().Aggregate( videoName,
                ( current, invalidChar ) => current.Replace( invalidChar, '-' ) );
        }
    }
    
    // Image
    async Task LoadStreamThumbnailImage( string url )
    {
        ServiceReply<Stream?> reply = await _httpService.TryGetStream( url );

        if ( !reply.Success || reply.Data is null )
            return;
        
        reply.Data.Position = 0; // reset stream pointer!
        _thumbnailBitmap = new Bitmap( reply.Data );
        
        await using MemoryStream memoryStream = new();
        await reply.Data.CopyToAsync( memoryStream );
        await reply.Data.DisposeAsync();
        
        _thumbnailBytes = memoryStream.ToArray();
    }
    async Task AddImage( string videoPath )
    {
        if ( _thumbnailBytes is null )
            return;

        string tempThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailFileName );
        string tempConvertedThumbnailPath = Path.Combine( Path.GetTempPath(), TempThumbnailConvertedFileName );
        string tempVideoPath = Path.Combine( Path.GetTempPath(), $"video_temp{Path.GetExtension( videoPath )}" );

        try
        {
            await File.WriteAllBytesAsync( tempThumbnailPath, _thumbnailBytes );
            await CreateJpgCopyFFMPEG( tempThumbnailPath, tempConvertedThumbnailPath );
            await CreateVideoWithImageFFMPEG( videoPath, tempConvertedThumbnailPath, tempVideoPath );

            if ( !File.Exists( tempVideoPath ) )
                return;

            File.Delete( videoPath ); // Delete original file
            File.Move( tempVideoPath, videoPath ); // Move the temp file to original path
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
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
    async Task CreateJpgCopyFFMPEG( string inputPath, string outputPath )
    {
        using Process conversionProcess = new();
        conversionProcess.StartInfo.FileName = "ffmpeg";
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
            Logger.LogWithConsole( ExString( e ) );
        }
        finally
        {
            if ( !conversionProcess.HasExited )
                conversionProcess.Kill();
        }
    }
    async Task CreateVideoWithImageFFMPEG( string videoPath, string convertedThumbnailPath, string tempOutputPath )
    {
        using Process createProcess = new();
        createProcess.StartInfo.FileName = "ffmpeg"; // Or the full path to the ffmpeg executable
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
            Logger.LogWithConsole( ExString( e ) );
        }
        finally
        {
            if ( !createProcess.HasExited )
                createProcess.Kill();
        }
    }
}