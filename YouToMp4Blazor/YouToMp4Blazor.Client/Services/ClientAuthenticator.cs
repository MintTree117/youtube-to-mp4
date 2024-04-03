using Blazored.LocalStorage;
using YouToMp4.Shared;

namespace YouToMp4Blazor.Client.Services;

public sealed class ClientAuthenticator( ILogger<ClientAuthenticator> logger, ILocalStorageService storage )
{
    readonly ILogger<ClientAuthenticator> _logger = logger;
    readonly ILocalStorageService _storage = storage;

    const string TokenStorageKey = "Token";

    public string? Key { get; private set; }

    public async Task<bool> TryLoadKey()
    {
        try
        {
            Key = await _storage.GetItemAsStringAsync( TokenStorageKey );
            return !string.IsNullOrWhiteSpace( Key );
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    public async Task<bool> TrySetKey( string? newToken )
    {
        try
        {
            Key = newToken;
            await _storage.SetItemAsStringAsync( TokenStorageKey, newToken ?? string.Empty );
            return true;
        }
        catch ( Exception e )
        {
            Utils.WriteLine( e );
            return false;
        }
    }
    public async Task SetHttpAuthHeader( HttpClient http )
    {
        if ( string.IsNullOrWhiteSpace( Key ) )
            await TryLoadKey();
        
        string key = string.IsNullOrWhiteSpace( Key ) ? "No_Key" : Key;
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue( HttpConsts.HttpAuthKey, key );
    }
}