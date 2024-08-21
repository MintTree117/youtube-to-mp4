using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeToMp4.Models;

namespace YoutubeToMp4.Services;

public sealed class YoutubeDownloader( string videoUrl )
{
    // Services
    readonly FileLogger _logger = FileLogger.Instance;
    readonly HttpService _httpService = new();
    readonly YoutubeClient _youtubeClient = new();
    readonly FFmpegService _ffmpegService = new();
    
    // Video Url From Constructor
    readonly string _videoUrl = videoUrl;
    
    // Stream Data
    public string? VideoName => _video?.Title;
    public string? VideoAuthor => _video?.Author.ToString();
    public TimeSpan? VideoDuration => _video?.Duration;
    public Bitmap? ThumbnailBitmap { get; private set; }
    StreamManifest? _streamManifest;
    Video? _video;
    byte[]? _thumbnailBytes;
    
    // Streams
    List<AudioOnlyStreamInfo> _audioStreams = [ ];
    List<VideoOnlyStreamInfo> _videoStreams = [ ];
    
    // Stream Qualities
    List<string>? _audioSteamQualities;
    List<string>? _videoSteamQualities;
    
    // Public Methods
    public async Task<Reply<bool>> TryInitialize()
    {
        try
        {
            _streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync( _videoUrl );
            _video = await _youtubeClient.Videos.GetAsync( _videoUrl );
            
            // Get Video Image Data
            var thumbnail = _video.Thumbnails.TryGetWithHighestResolution();
            if (thumbnail is not null)
                await LoadStreamThumbnailImage( thumbnail.Url );

            return _streamManifest is not null && _video is not null
                ? new Reply<bool>( true )
                : new Reply<bool>( ServiceErrorType.NotFound, "Stream manifest failed to load" );
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            return new Reply<bool>( ServiceErrorType.ServerError, e.Message );
        }
    }
    public async Task<List<string>> GetStreamInfo( StreamType steamType )
    {
        return steamType switch
        {
            StreamType.Mixed => await GetMixedStreams(),
            StreamType.Audio => await GetAudioStreams(),
            StreamType.Video => await GetVideoStreams(),
            _ => await GetMixedStreams()
        };
    }
    public async Task<Reply<bool>> DownloadSingle( SingleStreamSettings settings )
    {
        IStreamInfo streamInfo = settings.Type switch {
            StreamType.Audio => _audioStreams[settings.QualityIndex],
            StreamType.Video => _videoStreams[settings.QualityIndex],
            _ => throw new ArgumentOutOfRangeException()
        };
        
        try
        {
            string path = ConstructDownloadPath( settings.Filepath, streamInfo.Container.Name );

            Console.WriteLine( $"Starting {settings.Type} download..." );
            
            await _youtubeClient.Videos.Streams.DownloadAsync( streamInfo, path );

            Console.WriteLine( $"{settings.Type} downloaded." );
            
            var imageResult = await _ffmpegService.TryAddImage( path, _thumbnailBytes );

            Console.WriteLine( !imageResult.Success ? $"Failed to add image thumbnail. {imageResult.Message}" : "Thumbnail added." );
            Console.WriteLine( $"Finished download process. {path}" );
            
            return new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            return new Reply<bool>( ServiceErrorType.AppError, e.Message );
        }
    }
    public async Task<Reply<bool>> DownloadMuxed( MuxedStreamSettings settings )
    {
        try
        {
            Console.WriteLine( "Reached Downloader" );
            await GetVideoStreams();
            await GetAudioStreams();
            Console.WriteLine( "Got Stream Info" );

            var video = _videoStreams[settings.VideoQualityIndex];
            var audio = _audioStreams[settings.AudioQualityIndex];
            var path = ConstructDownloadPath( settings.Filepath, "mp4" );
            var streamInfos = new IStreamInfo[] { audio, video };
            
            Console.WriteLine( "Starting muxed download..." );
            
            await _youtubeClient.Videos.DownloadAsync( streamInfos, new ConversionRequestBuilder( path ).Build() );

            Console.WriteLine( "Muxed downloaded." );
            Console.WriteLine( "Starting thumbnail processing..." );
            
            var imageResult = await _ffmpegService.TryAddImage( path, _thumbnailBytes );

            Console.WriteLine( !imageResult.Success ? $"Failed to add image thumbnail. {imageResult.Message}" : "Thumbnail added." );
            Console.WriteLine( $"Finished download process. {path}" );
            
            return new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            Console.WriteLine( e );
            return new Reply<bool>( ServiceErrorType.AppError, e.Message );
        }
    }

    // Private Utils
    async Task<List<string>> GetMixedStreams()
    {
        await GetAudioStreams();
        return await GetVideoStreams();
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
    
    async Task LoadStreamThumbnailImage( string thumbnailUrl )
    {
        var reply = await _httpService.TryGetStream( thumbnailUrl );

        if ( !reply.Success || reply.Data is null )
        {
            Console.WriteLine( $"Failed to load thumbnail image! : {reply.PrintDetails()}" );
            return;
        }

        reply.Data.Position = 0; // reset stream pointer!
        ThumbnailBitmap = new Bitmap( reply.Data );

        reply.Data.Position = 0;
        await using MemoryStream memoryStream = new();
        await reply.Data.CopyToAsync( memoryStream );
        await reply.Data.DisposeAsync();

        memoryStream.Position = 0;
        _thumbnailBytes = memoryStream.ToArray();
    }
    string ConstructDownloadPath( string outputDirectory, string fileExtension )
    {
        string videoName = SanitizeVideoName( _video!.Title );
        string fileName = $"{videoName}.{fileExtension}";
        return Path.Combine( outputDirectory, fileName );

        static string SanitizeVideoName( string videoName )
        {
            return Path.GetInvalidFileNameChars().Aggregate( videoName, static ( current, invalidChar ) => current.Replace( invalidChar, '-' ) );
        }
    }
}