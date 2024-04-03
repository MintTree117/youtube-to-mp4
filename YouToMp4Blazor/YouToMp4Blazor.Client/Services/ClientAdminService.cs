using System.Net;
using System.Net.Http.Json;
using YouToMp4.Shared;

namespace YouToMp4Blazor.Client.Services;

public sealed class ClientAdminService( IHttpClientFactory httpClientFactory, ClientAuthenticator authenticator )
{
    // Fields
    readonly HttpClient _http = httpClientFactory.CreateClient( "client" );
    readonly ClientAuthenticator _authenticator = authenticator;
    
    // Public Methods
    public async Task<bool> TryPutJson( string keyInfo )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            
            HttpResponseMessage reply = await _http.PutAsJsonAsync( HttpConsts.InitFromJson, keyInfo );
            
            if ( !reply.IsSuccessStatusCode )
                await HandleError( reply, "Put" );

            return reply.IsSuccessStatusCode;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    public async Task<bool> TryInitFromDb( string connectionString )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            HttpResponseMessage reply = await _http.PostAsJsonAsync( HttpConsts.InitFromDb, connectionString );
            
            if ( !reply.IsSuccessStatusCode )
                await HandleError( reply, "Post" );

            return reply.IsSuccessStatusCode;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    public async Task<bool> TryUpdateDbRecords( string connectionString )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            HttpResponseMessage reply = await _http.PutAsJsonAsync( HttpConsts.UpdateDbRecords, connectionString );

            if ( !reply.IsSuccessStatusCode )
                await HandleError( reply, "Put" );

            return reply.IsSuccessStatusCode;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    public async Task<bool> TryPrintServerRecords( string connectionString )
    {
        try
        {
            await _authenticator.SetHttpAuthHeader( _http );
            HttpResponseMessage reply = await _http.GetAsync( HttpConsts.PrintKeyRecords );

            if ( !reply.IsSuccessStatusCode )
                await HandleError( reply, "Put" );

            string records = await reply.Content.ReadAsStringAsync();
            Console.WriteLine( records );

            return reply.IsSuccessStatusCode;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    
    // Private Methods
    static async Task HandleError( HttpResponseMessage httpResponse, string requestTypeName )
    {
        string errorContent = await httpResponse.Content.ReadAsStringAsync();

        switch ( httpResponse.StatusCode )
        {
            case HttpStatusCode.BadRequest:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return;
            case HttpStatusCode.NotFound:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return;
            case HttpStatusCode.Unauthorized:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return;
            case HttpStatusCode.Conflict:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return;
            case HttpStatusCode.InternalServerError:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {errorContent}" );
                return;
            default:
                Console.WriteLine( $"{requestTypeName}: {httpResponse.StatusCode}: {httpResponse.StatusCode}, Content: {errorContent}" );
                return;
        }
    }
}