using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using Microsoft.JSInterop;
using YouToMp4.Shared;
using YouToMp4.Shared.Dtos;

namespace YouToMp4Blazor.Client.Services;

public sealed class ClientYoutube( IHttpClientFactory httpClientFactory, ClientAuthenticator authenticator, IJSRuntime jsRuntime )
{
    // Fields
    const string JsDownloadFunction = "downloadFileFromStream";
    const string DefaultDownloadFileName = "yourFileName.mp4";
    
    readonly HttpClient _http = httpClientFactory.CreateClient( "client" ); // TODO: Move to local usage scope
    readonly ClientAuthenticator _authenticator = authenticator;
    readonly IJSRuntime _jsRuntime = jsRuntime;
    
    // Public Methods
    public async Task<StreamInfoDto?> GetStreamInfo( Dictionary<string, object> streamParams )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            
            HttpResponseMessage httpResponse = await _http.GetAsync(
                GetQueryParameters( HttpConsts.GetStreamInfo, streamParams ) );

            if ( !httpResponse.IsSuccessStatusCode )
                return await HandleError<StreamInfoDto>( httpResponse, "Get" );

            return await httpResponse.Content.ReadFromJsonAsync<StreamInfoDto>();
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return default;
        }
    }
    public async Task<bool> TryDownloadStream( string filename, string? container, Dictionary<string, object> streamParams )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            
            string path = GetQueryParameters( HttpConsts.GetStreamDownload, streamParams );
            
            await using Stream stream = await _http.GetStreamAsync( path );
            await using MemoryStream memoryStream = new();
            await stream.CopyToAsync( memoryStream );

            string filepath = ConstructFilePath( filename, ".mp4" );
            
            byte[] bytes = memoryStream.ToArray();
            await _jsRuntime.InvokeVoidAsync( JsDownloadFunction, filepath, bytes );
            return true;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    
    // Private Methods
    static string GetQueryParameters( string apiPath, Dictionary<string, object>? parameters )
    {
        if ( parameters is null )
            return apiPath;

        NameValueCollection query = HttpUtility.ParseQueryString( string.Empty );

        foreach ( KeyValuePair<string, object> param in parameters )
        {
            query[ param.Key ] = param.Value.ToString();
        }

        return $"{apiPath}?{query}";
    }
    static string ConstructFilePath( string name, string? container )
    {
        string extension = !string.IsNullOrWhiteSpace( container ) ? container : ".mp4";
        return $"{name}{extension}";
    }
    static async Task<T?> HandleError<T>( HttpResponseMessage httpResponse, string requestTypeName )
    {
        string errorContent = await httpResponse.Content.ReadAsStringAsync();

        switch ( httpResponse.StatusCode )
        {
            case HttpStatusCode.BadRequest:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return default;
            case HttpStatusCode.NotFound:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return default;
            case HttpStatusCode.Unauthorized:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return default;
            case HttpStatusCode.Conflict:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return default;
            case HttpStatusCode.InternalServerError:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return default;
            default:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {httpResponse.StatusCode}, Content: {errorContent}" );
                return default;
        }
    }
}