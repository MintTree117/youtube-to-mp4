using Microsoft.AspNetCore.Mvc;
using YouToMp4.Shared;

namespace YouToMp4Blazor.Features.Authentication;

public static class AuthEndpoints
{
    public static void MapAuthenticatorEndpoints( this IEndpointRouteBuilder app )
    {
        app.MapPost( HttpConsts.InitFromDb, async ( AuthManager auth ) => await auth.TryLoadKeysFromDb()
            ? Results.Ok( true )
            : Results.Problem() );
        
        app.MapPut( HttpConsts.InitFromJson, ( [FromBody] string keysString, AuthManager auth ) => auth.LoadKeysFromJson( keysString )
            ? Results.Ok( true )
            : Results.Problem() );

        app.MapPut( HttpConsts.UpdateDbRecords, async ( AuthManager auth ) => await auth.SaveRecords()
            ? Results.Ok( true )
            : Results.Problem() );

        app.MapGet( HttpConsts.PrintKeyRecords, ( AuthManager auth ) => Results.Ok( auth.PrintRecords() ) );
    }
}