using System.Net;
using System.Text.Json;
using YouToMp4.Shared;

namespace YouToMp4Blazor.Features.Authentication;

public sealed class AuthMiddleware
{
    const string InvalidKeyMessage = "Your access key is invalid or has expired.";
    
    readonly RequestDelegate _next;
    readonly ILogger<AuthMiddleware> _logger;
    AuthManager _authManager = null!;

    // Constructor
    public AuthMiddleware( RequestDelegate next, ILogger<AuthMiddleware> logger, IHostEnvironment environment )
    {
        _next = next;
        _logger = logger;
    }
    
    // Main
    public async Task InvokeAsync( HttpContext httpContext  )
    {
        try
        {
            IServiceScope scope = httpContext.RequestServices.CreateScope();
            _authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

            if ( !await TryAuthenticate( httpContext ) )
                return;
            
            await _next( httpContext );
        }
        catch ( Exception e )
        {
            await HandleAuthenticationException( httpContext, e );
        }
    }
    
    // Utils
    async Task<bool> TryAuthenticate( HttpContext http )
    {
        PathString path = http.Request.Path;

        if ( path.StartsWithSegments( HttpConsts.UserAuth ) )
        {
            if ( !await HandleKeyValidation( http ) ) {
                return false;
            }
        }
        else if ( path.StartsWithSegments( HttpConsts.AdminAuth ))
        {
            if ( !await HandleAdminKeyValidation( http ) ) {
                return false;
            }
        }

        return true;
    }
    async Task<bool> HandleKeyValidation( HttpContext http )
    {
        if ( !GetAccessGetFromHeader( http, out string key ) ) {
            return await ReturnUnauthorized( http );
        }

        if ( await _authManager.ValidateUserKey( key, http.Connection.RemoteIpAddress?.ToString() ?? "" ) ) {
            return true;
        }
        
        _logger.LogError( "Invalid User Key" );
        return await ReturnUnauthorized( http );
    }
    async Task<bool> HandleAdminKeyValidation( HttpContext http )
    {
        if ( !GetAccessGetFromHeader( http, out string? key ) ) {
            return await ReturnUnauthorized( http );   
        }

        if ( _authManager.ValidateKeyAdmin( key, http.Connection.RemoteIpAddress?.ToString() ?? "" ) ) {
            return true;
        }
        
        _logger.LogError( "Invalid Admin Key" );
        return await ReturnUnauthorized( http );
    }
    async Task HandleAuthenticationException( HttpContext http, Exception e )
    {
        _logger.LogError( e, e.Message );
        http.Response.StatusCode = ( int ) HttpStatusCode.InternalServerError;
        http.Response.ContentType = "application/json";

        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string json = JsonSerializer.Serialize( "Failed to authenticate your request!", options );
        await http.Response.WriteAsync( json );
    }
    static async Task<bool> ReturnUnauthorized( HttpContext http )
    {
        http.Response.StatusCode = ( int ) HttpStatusCode.Unauthorized;
        await http.Response.WriteAsync( InvalidKeyMessage );
        return false;
    }
    bool GetAccessGetFromHeader( HttpContext http, out string key )
    {
        key = string.Empty;

        if ( !http.Request.Headers.ContainsKey( HttpConsts.HttpAuthHeader ) ) {
            _logger.LogError( "No Header Key" );
            return false;
        }

        if ( !http.Request.Headers.ContainsKey( HttpConsts.HttpAuthHeader ) ) {
            _logger.LogError( "Not contains auth key" );
            return false;
        }
        
        string? authorizationHeader = http.Request.Headers[ HttpConsts.HttpAuthHeader ];

        if ( string.IsNullOrEmpty( authorizationHeader ) ) {
            _logger.LogError( "Auth header empty" );
            return false;   
        }

        // Split the Authorization header value to retrieve the key
        string[] authHeaderParts = authorizationHeader.Trim().Split( ' ' );

        if ( authHeaderParts.Length != 2 || authHeaderParts[ 0 ] != HttpConsts.HttpAuthKey ) {
            _logger.LogError( "Header Parts Error" );
            return false;
        }
        
        key = authHeaderParts[ 1 ];
        return true;
    }
}