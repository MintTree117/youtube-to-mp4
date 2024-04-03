using System.Text.Json;
using YouToMp4Blazor.Models;

namespace YouToMp4Blazor.Features.Authentication;

public sealed class AuthManager
{
    // Const
    const int MaximumKeys = 30; 
    
    // Fields
    readonly ILogger<AuthManager> _logger;
    readonly AuthRepository _repository;
    
    // Access Keys
    readonly Dictionary<string, DlKey> _cachedKeyStrings = new( MaximumKeys );
    readonly Dictionary<string, Dictionary<string, List<DlKeyRequest>>> _keyIps = new();
    readonly HashSet<DlKey> _cachedKeys = new( MaximumKeys );
    bool _loaded;
    
    // Special Keys
    readonly List<string> _specialKeys;
    string? _adminKeyString;
    
    // Initializations`
    public AuthManager( ILogger<AuthManager> logger, AuthRepository repository )
    {
        _logger = logger;
        _repository = repository;
        _specialKeys = TryGetSpecialKeys();
        _adminKeyString = TryGetSpecialAdminKey();
    }
    
    // Public Methods
    public bool LoadKeysFromJson( string keysString )
    {
        ClearCache();
        _loaded = false;
        
        TryGetEnvKeys();

        if ( string.IsNullOrWhiteSpace( keysString ) )
            return false;

        var keys = JsonSerializer.Deserialize<List<DlKey>>( keysString );

        if ( keys is null )
            return false;

        foreach ( DlKey k in keys )
            SetKey( k );

        _loaded = true;
        return true;
    }
    public async Task<bool> TryLoadKeysFromDb()
    {
        ClearCache();
        _loaded = false;

        TryGetEnvKeys();

        IEnumerable<DlKey> info = await _repository.TryGetKeys();
        
        foreach ( DlKey k in info )
        {
            SetKey( k );
        }

        _loaded = true;
        return true;
    }
    public async Task<bool> SaveRecords()
    {
        List<DlKeyRecord> records = GetKeyRecords();
        return await _repository.TryAddRecords( records );
    }
    
    public async Task<bool> ValidateUserKey( string key, string ip )
    {
        if ( !_loaded )
            await TryLoadKeysFromDb();
        
        RecordKeyRequest( DlRequestType.User, key, ip );
        return _cachedKeyStrings.ContainsKey( key );
    }
    public bool ValidateKeyAdmin( string key, string ip )
    {
        RecordKeyRequest( DlRequestType.Admin, key, ip );

        if ( _adminKeyString is not null && key == _adminKeyString )
            return true;
        
        return !string.IsNullOrWhiteSpace( _adminKeyString ) && key == _adminKeyString;
    }
    public List<DlKeyRecord> PrintRecords()
    {
        return GetKeyRecords();
    }
    
    List<DlKeyRecord> GetKeyRecords()
    {
        List<DlKeyRecord> records = [ ];

        foreach ( DlKey key in _cachedKeys! )
        {
            if ( !_keyIps.TryGetValue( key.KeyString, out Dictionary<string, List<DlKeyRequest>>? dict ) )
                continue;

            foreach ( string ip in dict.Keys )
            {
                if ( !dict.TryGetValue( ip, out List<DlKeyRequest>? requests ) )
                    continue;

                records.AddRange( requests.Select( r => new DlKeyRecord
                {
                    KeyId = key.Id, IpAddress = ip, RequestType = r.RequestType, DateCreated = r.DateCreated

                } ) );
            }
        }

        return records;
    }
    void TryGetEnvKeys()
    {
        _specialKeys.AddRange( TryGetSpecialKeys() );
        _adminKeyString = TryGetSpecialAdminKey();
    }
    List<string> TryGetSpecialKeys()
    {
        try
        {
            string? keysString = Environment.GetEnvironmentVariable( "SpecialKeys" );

            if ( keysString is not null )
                return keysString.Split( ',' ).ToList();
            
            _logger.LogError( "Special Keys are null from Environment!" );
            return [ ];

        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            return [ ];
        }
    }
    string? TryGetSpecialAdminKey()
    {
        try
        {
            string? key = Environment.GetEnvironmentVariable( "SpecialAdminKey" );

            if ( key is null )
                _logger.LogError( "Special Admin Key is null from Environment!" );

            return key;
        }
        catch ( Exception e )
        {
            _logger.LogError( e, e.Message );
            return null;
        }
    }
    void RecordKeyRequest( DlRequestType type, string key, string ipAddress )
    {
        if ( !_keyIps.TryGetValue( key, out Dictionary<string, List<DlKeyRequest>>? dict ) )
        {
            dict = [ ];
            _keyIps.Add( key, dict );
        }

        if ( !dict.TryGetValue( ipAddress, out List<DlKeyRequest>? requests ) )
        {
            requests = [ ];
            dict.Add( ipAddress, requests );
        }

        requests.Add( new DlKeyRequest
        {
            RequestType = type
        } );
    }
    void ClearCache()
    {
        _cachedKeys.Clear();
        _cachedKeyStrings.Clear();
        _specialKeys.Clear();
        _adminKeyString = null;
    }
    void SetKey( DlKey key )
    {
        _cachedKeys.Add( key );
        _cachedKeyStrings.TryAdd( key.KeyString, key );
    }
}