using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using YouToMp4Avalonia.Models;

namespace YouToMp4Avalonia.Services;

public sealed class HttpService
{
    // Services
    readonly HttpClient _http = new();
    readonly FileLogger _logger = FileLogger.Instance;
    
    // Public Methods
    public async Task<ServiceReply<Stream?>> TryGetStream( string url )
    {
        try
        {
            HttpResponseMessage httpResponse = await _http.GetAsync( url );
            return await HandleImageStreamHttpResponse( httpResponse );
        }
        catch ( Exception e )
        {
            _logger.LogWithConsole( e );
            return new ServiceReply<Stream?>( ServiceErrorType.ServerError, $"Get Stream: Exception occurred while sending API request." );
        }
    }
    
    // Private Utils
    async Task<ServiceReply<Stream?>> HandleImageStreamHttpResponse( HttpResponseMessage httpResponse )
    {
        if ( !httpResponse.IsSuccessStatusCode ) 
            return await HandleHttpError<Stream?>( httpResponse );
        
        await using Stream stream = await httpResponse.Content.ReadAsStreamAsync();
        MemoryStream memoryStream = new();
        await stream.CopyToAsync( memoryStream ); // Copy the stream to a MemoryStream
        await stream.DisposeAsync();
        
        return new ServiceReply<Stream?>( memoryStream );
    }
    async Task<ServiceReply<T?>> HandleHttpError<T>( HttpResponseMessage httpResponse )
    {
        string errorContent = await httpResponse.Content.ReadAsStringAsync();
        ServiceErrorType errorType = ServiceReply<object>.GetHttpError( httpResponse.StatusCode );
        _logger.LogWithConsole( $"{errorContent}" );
        return new ServiceReply<T?>( errorType, errorContent );
    }
}