using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using YouToMp4Avalonia.Models;
using YoutubeExplode.Search;

namespace YouToMp4Avalonia.Services;

public sealed class YoutubeBrowser : BaseService
{
    // Constants
    const int MaxSearchResults = 200;
    
    // Services
    readonly YoutubeClientHolder _youtubeService = Program.ServiceProvider.GetService<YoutubeClientHolder>()!;
    readonly HttpController _http = Program.ServiceProvider.GetService<HttpController>()!;

    // Public Methods
    public async Task<ServiceReply<IReadOnlyList<YoutubeSearchResult>>> GetStreams( string query, int resultsPerPage )
    {
        IAsyncEnumerator<VideoSearchResult> enumerator = _youtubeService.YoutubeClient.Search.GetVideosAsync( query ).GetAsyncEnumerator();
        List<VideoSearchResult> results = [ ];
        int cappedResultsPerPage = Math.Min( resultsPerPage, MaxSearchResults );

        // Move to the first item in the enumerator
        bool hasResults = await enumerator.MoveNextAsync();

        for ( int i = 0; i < cappedResultsPerPage && hasResults; i++ )
        {
            VideoSearchResult c = enumerator.Current;

            if ( !( string.IsNullOrWhiteSpace( c.Title ) && string.IsNullOrWhiteSpace( c.Url ) ) )
                results.Add( c );

            hasResults = await enumerator.MoveNextAsync();
        }

        await enumerator.DisposeAsync();
        
        // CUSTOM MAPPING
        List<YoutubeSearchResult> customResults = [ ];
        
        foreach ( VideoSearchResult v in results )
        {
            string url = v.Thumbnails.Count > 0
                ? v.Thumbnails[ 0 ].Url
                : "";
            
            ServiceReply<Bitmap?> reply = await GetImageBitmap( url );
            
            customResults.Add( new YoutubeSearchResult
            {
                Title = v.Title,
                Duration = v.Duration.ToString() ?? "00:00:00",
                Url = v.Url,
                Image = reply.Data
            } );
        }
        
        return new ServiceReply<IReadOnlyList<YoutubeSearchResult>>( customResults );
    }

    async Task<ServiceReply<Bitmap?>> GetImageBitmap( string imageUrl )
    {
        ServiceReply<Stream?> reply = await _http.TryGetStream( imageUrl );

        if ( !reply.Success || reply.Data is null )
            return new ServiceReply<Bitmap?>( reply.ErrorType, reply.Message );

        Stream stream = reply.Data;
        stream.Position = 0; // Never forget again! Stream pointer/index lol

        try
        {
            Bitmap map = new( stream );
            await stream.DisposeAsync();
            return new ServiceReply<Bitmap?>( map );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            await stream.DisposeAsync();
            return new ServiceReply<Bitmap?>( ServiceErrorType.AppError, "Fail get image stream" );
        }
    }
}