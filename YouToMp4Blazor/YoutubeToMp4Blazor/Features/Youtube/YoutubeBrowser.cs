using YoutubeToMp4.Shared.Dtos;
using YoutubeToMp4.Shared.Enums;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMp4Blazor.Features.Youtube;

public sealed class YoutubeBrowser( ILogger<YoutubeBrowser> logger )
{
    readonly YoutubeClient _youtube = new();
    
    StreamManifest? _streamManifest;
    Video? _video;
    
    // "Constructor"
    public async Task<bool> TryInitialize( string videoUrl )
    {
        try
        {
            _streamManifest = await _youtube.Videos.Streams.GetManifestAsync( videoUrl );
            _video = await _youtube.Videos.GetAsync( videoUrl );

            return _streamManifest is not null && _video is not null;
        }
        catch ( Exception e )
        {
            logger.LogError( e, e.Message );
            return false;
        }
    }
    public async Task<StreamInfoDto?> GetStreamInfo( StreamType type )
    {
        if ( _video is null )
            return null;

        return new StreamInfoDto
        {
            Title = _video.Title,
            Duration = _video.Duration.ToString() ?? "00:00:00",
            ImageUrl = _video.Thumbnails.Any() ? _video.Thumbnails[ 0 ].Url : string.Empty,
            Qualities = type switch
            {
                StreamType.Mixed => await GetMuxedStreams(),
                StreamType.Audio => await GetAudioOnlyStreams(),
                StreamType.Video => await GetVideoOnlyStreams(),
                _ => await GetMuxedStreams()
            }
        };
    }

    // Get Streams
    async Task<StreamQualityInfoDto> GetMuxedStreams()
    {
        return await Task.Run( () => {
            List<MuxedStreamInfo> streams = _streamManifest!.GetMuxedStreams().ToList();
            List<string> containers = [ ];
            List<string> details = [ ];

            for ( int i = 0; i < streams.Count; i++ ) {
                containers.Add( streams[ i ].Container.ToString() );
                
                MuxedStreamInfo stream = streams[ i ];
                details.Add( $"{i + 1} : {stream.VideoResolution} px - {stream.Bitrate} bps - {stream.Container}" );
            }

            return new StreamQualityInfoDto
            {
                IndexedQualityContainers = containers,
                IndexedQualityDetails = details
            };
        } );
    }
    async Task<StreamQualityInfoDto> GetAudioOnlyStreams()
    {
        return await Task.Run( () => {
            List<AudioOnlyStreamInfo> streams = _streamManifest!.GetAudioOnlyStreams().ToList();
            List<string> containers = [ ];
            List<string> details = [ ];

            for ( int i = 0; i < streams.Count; i++ ) {
                containers.Add( streams[ i ].Container.ToString() );

                AudioOnlyStreamInfo stream = streams[ i ];
                details.Add( $"{i + 1} : {stream.Bitrate} bps - {stream.Container}" );
            }

            return new StreamQualityInfoDto
            {
                IndexedQualityContainers = containers,
                IndexedQualityDetails = details
            };
        } );
    }
    async Task<StreamQualityInfoDto> GetVideoOnlyStreams()
    {
        return await Task.Run( () => {
            List<VideoOnlyStreamInfo> streams = _streamManifest!.GetVideoOnlyStreams().ToList();
            List<string> containers = [ ];
            List<string> details = [ ];

            for ( int i = 0; i < streams.Count; i++ ) {
                containers.Add( streams[ i ].Container.ToString() );

                VideoOnlyStreamInfo stream = streams[ i ];
                details.Add( $"{i + 1} : {stream.VideoResolution} px - {stream.Container}" );
            }

            return new StreamQualityInfoDto
            {
                IndexedQualityContainers = containers,
                IndexedQualityDetails = details
            };
        } );
    }
}