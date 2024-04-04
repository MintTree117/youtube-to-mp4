using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using YouToMp4Avalonia.Models;

namespace YouToMp4Avalonia.Services;

public sealed class ArchiveService : BaseService
{
    // Constants
    const string ApiPath = "api/archive";
    const string ApiPathGetCategories = $"{ApiPath}/categories";
    const string ApiPathSearch = $"{ApiPath}/search";
    
    // Services
    readonly HttpController _http = Program.ServiceProvider.GetService<HttpController>()!;
    
    // Public Methods
    public async Task<ServiceReply<List<ArchiveCategory>?>> GetCategoriesAsync( string? apiKey )
    {
        return await _http.TryGetRequest<List<ArchiveCategory>>( ApiPathGetCategories, null, apiKey );
    }
    public async Task<ServiceReply<ArchiveSearch?>> SearchVideosAsync( string? apiKey, Dictionary<string,object>? parameters )
    {
        return await _http.TryGetRequest<ArchiveSearch>( ApiPathSearch, parameters, apiKey );
    }
    public async Task<ServiceReply<bool>> DownloadStreamAsync( string? apiKey, Dictionary<string, object>? httpParameters, string downloadPath )
    {
        ServiceReply<Stream?> streamReply = await _http.TryGetRequest<Stream>( ApiPathSearch, httpParameters, apiKey );

        if ( !streamReply.Success || streamReply.Data is null )
            return new ServiceReply<bool>( streamReply.ErrorType, streamReply.Message );

        try
        {
            await using Stream inputStream = streamReply.Data;
            await using Stream outputStream = File.Open( downloadPath, FileMode.Create );
            await inputStream.CopyToAsync( outputStream );

            return new ServiceReply<bool>( true );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            return new ServiceReply<bool>( ServiceErrorType.IoError, "Error at file creation/write!" );
        }
    }
}