using YouToMp4.Shared;
using YouToMp4.Shared.Dtos;
using YouToMp4.Shared.Enums;

namespace YouToMp4Blazor.Features.Youtube;

public static class YoutubeEndpoints
{
    public static void MapYoutubeEndpoints( this IEndpointRouteBuilder app )
    {
        app.MapGet( HttpConsts.GetStreamInfo, async ( string url, StreamType type, YoutubeBrowser yt ) => 
        {
            if ( !await yt.TryInitialize( url ) )
                return Results.Problem( "Failed to initialize youtube client!" );

            StreamInfoDto? info = await yt.GetStreamInfo( type );
            
            return info is not null
                ? Results.Ok( info )
                : Results.NotFound();
        } );

        app.MapGet( HttpConsts.GetStreamDownload, async ( string url, StreamType type, int quality, string start, string end, YoutubeDownloader yt ) =>
        {
            if ( !await yt.TryInitialize( url ) )
                return Results.Problem( "Failed to initialize youtube client!" );
            
            var dlParams = YoutubeDownloadParams.Create( url, type, quality, start, end, string.Empty ); // TODO: null thumbnail for now

            Stream? stream = await yt.Download( dlParams );

            return stream is not null
                ? Results.File( stream )
                : Results.NotFound();
        } );
    }
}