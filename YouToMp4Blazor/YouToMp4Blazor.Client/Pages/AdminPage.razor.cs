using Microsoft.AspNetCore.Components;
using YouToMp4Blazor.Client.Services;

namespace YouToMp4Blazor.Client.Pages;

public sealed partial class AdminPage : PageBase
{
    [Inject] ClientAdminService ClientAdminService { get; init; } = default!;

    string _keyString = string.Empty;
    string _connectionString = string.Empty;
    
    async Task OnClickPutJson()
    {
        ToggleLoading( true, "Pushing Json to server..." );
        
        bool result = await ClientAdminService.TryPutJson( _keyString );

        if ( result )
            ShowAlert( AlertType.Success, "Changes Saved." );
        else
            ShowAlert( AlertType.Danger, "Failed to save changes!" );

        ToggleLoading( false );
    }
    async Task OnClickPostDb()
    {
        ToggleLoading( true, "Attempting to load from database..." );
        
        bool result = await ClientAdminService.TryInitFromDb( _connectionString );

        if ( result )
            ShowAlert( AlertType.Success, "Changes Saved." );
        else
            ShowAlert( AlertType.Danger, "Failed to save changes!" );

        ToggleLoading( false );
    }
    async Task OnClickUpdateDbRecords()
    {
        ToggleLoading( true, "Attempting to add server records to database..." );

        bool result = await ClientAdminService.TryUpdateDbRecords( _connectionString );

        if ( result )
            ShowAlert( AlertType.Success, "Changes Saved." );
        else
            ShowAlert( AlertType.Danger, "Failed to save changes!" );

        ToggleLoading( false );
    }
    async Task PrintServerRecords()
    {
        ToggleLoading( true, "Attempting to print server records to console..." );

        bool result = await ClientAdminService.TryPrintServerRecords( _connectionString );

        if ( result )
            ShowAlert( AlertType.Success, "Server records printed to console." );
        else
            ShowAlert( AlertType.Danger, "Failed to print records to console!" );

        ToggleLoading( false );
    }
}