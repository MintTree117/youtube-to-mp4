using Microsoft.AspNetCore.Components;
using YouToMp4Blazor.Client.Services;
using YoutubeToMp4.Shared.Dtos;
using YoutubeToMp4.Shared.Enums;

namespace YoutubeToMp4.Client.Pages;

public sealed partial class Home : PageBase
{
    // Services
    [Inject] ClientAuthenticator ClientAuthenticator { get; init; } = default!;
    [Inject] ClientYoutube ClientYoutube { get; init; } = default!;

    // Defaults
    const string DefaultStreamStartTime = "00:00:00";
    const string DefaultStreamImage = "defaultplayer.png";
    const string DefaultStreamName = "Download Youtube";
    const string DefaultTypeName = "No Stream Types Found!";
    const string DefaultQualityName = "No Quality Selected";
    const string FailGetStreamName = "Failed to get Stream";
    const string SuccessDownloadMessage = "Download Success";
    const string FailDownloadMessage = "Failed to Download";
    const string LoadingStreamName = "Loading Stream Information...";
    const string DownloadingStreamMessage = "Downloading Stream...";
    const string GetStreamText = "Get Stream Info";
    const string DownloadStreamText = "Download Stream File";
    const string StreamInfoIcon = "fa-cloud-arrow-down";
    const string StreamDownloadIcon = "fa-file-arrow-down";

    // Youtube Fields
    readonly List<StreamType> _streamTypes = Enum.GetValues<StreamType>().ToList();
    readonly List<string> _streamTypeNames = GetStreamTypeNames( Enum.GetNames<StreamType>().ToList() );
    List<string> _streamContainers = [ ];
    List<string> _streamQualities = [ ];
    
    string _youtubeLink = string.Empty;
    string _streamTitle = DefaultStreamName;
    string _streamAuthor = string.Empty;
    string _streamDuration = string.Empty;
    string _streamImage = DefaultStreamImage;
    string _selectedStreamTypeName = DefaultTypeName;
    string _selectedStreamQuality = DefaultQualityName;
    string _streamStartTime = string.Empty;
    string _streamEndTime = string.Empty;
    string _submitButtonText = GetStreamText;

    // State
    bool _hasStream;
    string _youtubeIconCss = string.Empty; 
    string _submitIconCss = StreamInfoIcon;
    string? _key;
    string? _newKey;
    
    // Initialization
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ToggleLoading( true, "Loading Api Key" );
    }
    protected override async Task OnAfterRenderAsync( bool firstRender )
    {
        await base.OnAfterRenderAsync( firstRender );

        if ( firstRender ) {
            _selectedStreamTypeName = _streamTypeNames.Count > 0 ? _streamTypeNames[ 0 ] : DefaultTypeName;
            await TryGetKey();
            ToggleLoading( false );
        }
    }
    static List<string> GetStreamTypeNames( List<string> streamTypes )
    {
        List<string> names = [ ];
        names.AddRange( from t in streamTypes select $"Stream Type: {t}" );
        return names;
    }

    // User Actions
    async Task TryGetKey()
    {
        if ( !await ClientAuthenticator.TryLoadKey() )
        {
            ShowAlert( AlertType.Warning, "You need an access key to use this service." );
            return;
        }

        _key = ClientAuthenticator.Key;
        _newKey = _key;
    }
    async Task TrySaveKey()
    {
        ToggleLoading( true, "Saving Access Key" );
        bool success = await ClientAuthenticator.TrySetKey( _newKey );

        if ( !success )
            ShowAlert( AlertType.Danger, "IO Error: Failed to save the key to storage!" );
        else
            ShowAlert( AlertType.Success, "Saved the key to storage." );

        _key = null;

        ToggleLoading( false );
    }
    async Task OnSubmit()
    {
        if ( !_hasStream )
            await GetStreamInfo();
        else
            await GetStreamDownload();
    }
    async Task GetStreamInfo()
    {
        ToggleLoading( true, LoadingStreamName );
        ResetState();

        StreamInfoDto? result = await ClientYoutube.GetStreamInfo( GetStreamInfoParams() );

        if ( result is null )
        {
            _streamTitle = FailGetStreamName;
            ShowAlert( AlertType.Danger, "Failed to fetch stream info for url! Check console for logs." );
            ToggleLoading( false );
            return;
        }

        SetNewState( result );
        ToggleLoading( false );
    }
    async Task GetStreamDownload()
    {
        ToggleLoading( true, DownloadingStreamMessage );

        int index = GetSelectedQualityIndex();
        bool result = await ClientYoutube.TryDownloadStream( _streamTitle, _streamContainers[ index ], GetStreamDownloadParams() );

        if ( result ) {
            ShowAlert( AlertType.Success, SuccessDownloadMessage );
        }
        else {
            ShowAlert( AlertType.Danger, FailDownloadMessage );
        }

        ToggleLoading( false );
    }

    // UI Events
    void OnNewLink( ChangeEventArgs e )
    {
        ResetState();
        
        if ( e.Value is not null )
        {
            _youtubeLink = ( string ) e.Value;
        }
        
        StateHasChanged();
    }
    void OnNewKey( ChangeEventArgs e )
    {
        string? value = e.Value?.ToString();
        _newKey = value ?? string.Empty;
    }

    // Utilities
    bool GetStreamTypeDropdownDisabled()
    {
        return _isLoading || !_hasStream;
    }
    bool GetSettingsDisabled()
    {
        return !_hasStream || _isLoading;
    }
    string GetSettingsCss()
    {
        return GetSettingsDisabled() ? "disabled-alt" : "icon-alt";
    }
    bool GetSubmitButtonDisabled()
    {
        return _isLoading || string.IsNullOrWhiteSpace( _youtubeLink );
    }
    bool GetAccessKeyButtonDisabled()
    {
        return _newKey == _key;
    }
    void ResetState()
    {
        _hasStream = false;
        _submitIconCss = StreamInfoIcon;
        _youtubeIconCss = string.Empty;
        _streamTitle = DefaultStreamName;
        _streamAuthor = string.Empty;
        _streamImage = string.Empty;
        _submitButtonText = GetStreamText;
        _streamImage = DefaultStreamImage;
        _streamContainers = [ ];
        _streamQualities = [ ];
        _selectedStreamQuality = DefaultQualityName;
        _streamStartTime = string.Empty;
        _streamEndTime = string.Empty;
    }
    void SetNewState( StreamInfoDto result )
    {
        _hasStream = true;
        _streamAuthor = result.Author;
        _streamTitle = $"{result.Title}";
        _streamImage = result.ImageUrl;
        _streamContainers = result.Qualities.IndexedQualityContainers;
        _streamQualities = result.Qualities.IndexedQualityDetails;
        _selectedStreamQuality = _streamQualities.Count > 0 ? _streamQualities[ 0 ] : DefaultQualityName;
        _submitButtonText = DownloadStreamText;
        _streamStartTime = DefaultStreamStartTime;
        _streamEndTime = result.Duration;
        _streamDuration = result.Duration;
        _submitIconCss = StreamDownloadIcon;
    }
    Dictionary<string, object> GetStreamInfoParams()
    {
        return new Dictionary<string, object>()
        {
            { "url", _youtubeLink },
            { "type", GetSelectedStreamType() }
        };
    }
    Dictionary<string, object> GetStreamDownloadParams()
    {
        string start = string.Empty;
        string end = string.Empty;

        if ( _streamStartTime != DefaultStreamStartTime || _streamEndTime != _streamDuration )
        {
            start = _streamStartTime;
            end = _streamEndTime;
        }

        return new Dictionary<string, object>()
        {
            { "url", _youtubeLink },
            { "type", GetSelectedStreamType() },
            { "quality", GetSelectedQualityIndex() },
            { "start", start },
            { "end", end }
        };
    }
    StreamType GetSelectedStreamType()
    {
        int index = _streamTypeNames.IndexOf( _selectedStreamTypeName );
        StreamType type = _streamTypes[ index ];
        return type;
    }
    int GetSelectedQualityIndex()
    {
        return Math.Max( 0, _streamQualities.IndexOf( _selectedStreamQuality ) );
    }
}