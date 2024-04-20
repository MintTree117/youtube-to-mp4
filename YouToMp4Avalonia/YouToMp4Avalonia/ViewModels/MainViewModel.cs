using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using YouToMp4Avalonia.Models;
using YouToMp4Avalonia.Services;

namespace YouToMp4Avalonia.ViewModels;

public sealed class MainViewModel : ReactiveObject
{
    // Services
    readonly FileLogger _logger = FileLogger.Instance;
    readonly SettingsManager _settingsManager = SettingsManager.Instance;
    YoutubeDownloader? _dlService;
    
    // Constants
    const string DefaultVideoName = "No Video Selected";
    const string LoadingVideoName = "Loading Video...";
    const string InvalidVideoName = "Invalid Video Link";
    const string SuccessDownloadMessage = "Download success!";
    const string FailDownloadMessage = "Failed to download!";
    const string DefaultVideoImage = "avares://YouToMp4Avalonia/Assets/default_stream_image.png";
    
    // Property Field List Values
    Bitmap? _videoImageBitmap;
    List<string> _streamTypes = Enum.GetNames<StreamType>().ToList();
    List<string> _streamQualities = [ ];
    string _youtubeLink = string.Empty;
    string _videoName = DefaultVideoName;
    string _selectedStreamTypeName = string.Empty; // saves state between downloads for user convenience
    string _selectedStreamQualityName = string.Empty;
    string _streamStartTime = string.Empty;
    string _streamEndTime = string.Empty;
    string _downloadLocation = string.Empty;
    string _message = string.Empty;
    bool _isLinkBoxEnabled;
    bool _isVideoSettingsEnabled;
    bool _isDownloadButtonEnabled;
    bool _hasMessage;

    // Commands
    public ReactiveCommand<Unit, Unit> DownloadStreamCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseMessageCommand { get; }
    ReactiveCommand<Unit, Unit> HandleNewLinkCommand { get; }
    ReactiveCommand<Unit, Unit> NewStreamCommand { get; }
    
    // Constructor
    public MainViewModel()
    {
        HandleNewLinkCommand = ReactiveCommand.CreateFromTask( HandleNewLink );
        DownloadStreamCommand = ReactiveCommand.CreateFromTask( DownloadStream );
        NewStreamCommand = ReactiveCommand.CreateFromTask( HandleNewStreamType );
        SettingsCommand = ReactiveCommand.CreateFromTask( UpdateSettings );
        CloseMessageCommand = ReactiveCommand.Create( CloseMessage );
        
        IsLinkBoxEnabled = true;
        DownloadLocation = _settingsManager.Settings.DownloadLocation;
        LoadDefaultImage();
    }

    // Reactive Properties
    public Bitmap? VideoImageBitmap
    {
        get => _videoImageBitmap;
        set => this.RaiseAndSetIfChanged( ref _videoImageBitmap, value );
    }
    public List<string> StreamTypes
    {
        get => _streamTypes;
        set => this.RaiseAndSetIfChanged( ref _streamTypes, value );
    }
    public List<string> StreamQualities
    {
        get => _streamQualities;
        set => this.RaiseAndSetIfChanged( ref _streamQualities, value );
    }
    public string YoutubeLink
    {
        get => _youtubeLink;
        set
        {
            this.RaiseAndSetIfChanged( ref _youtubeLink, value );
            HandleNewLinkCommand.Execute();
        }
    }
    public string VideoName
    {
        get => _videoName;
        set => this.RaiseAndSetIfChanged( ref _videoName, value );
    }
    public string SelectedStreamType // saves state between downloads for user convenience
    {
        get => _selectedStreamTypeName;
        set
        {
            this.RaiseAndSetIfChanged( ref _selectedStreamTypeName, value );
            NewStreamCommand.Execute();
        }
    }
    public string SelectedStreamQuality
    {
        get => _selectedStreamQualityName;
        set => this.RaiseAndSetIfChanged( ref _selectedStreamQualityName, value );
    }
    public string StreamStartTime
    {
        get => _streamStartTime;
        set => this.RaiseAndSetIfChanged( ref _streamStartTime, value );
    }
    public string StreamEndTime
    {
        get => _streamEndTime;
        set => this.RaiseAndSetIfChanged( ref _streamEndTime, value );
    }
    public string DownloadLocation
    {
        get => _downloadLocation;
        set => this.RaiseAndSetIfChanged( ref _downloadLocation, value );
    }
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged( ref _message, value );
    }
    public bool IsLinkBoxEnabled
    {
        get => _isLinkBoxEnabled;
        set => this.RaiseAndSetIfChanged( ref _isLinkBoxEnabled, value );
    }
    public bool IsVideoSettingsEnabled
    {
        get => _isVideoSettingsEnabled;
        set => this.RaiseAndSetIfChanged( ref _isVideoSettingsEnabled, value );
    }
    public bool IsDownloadButtonEnabled
    {
        get => _isDownloadButtonEnabled;
        set => this.RaiseAndSetIfChanged( ref _isDownloadButtonEnabled, value );
    }
    public bool HasMessage
    {
        get => _hasMessage;
        set => this.RaiseAndSetIfChanged( ref _hasMessage, value );
    }

    // Command Delegates
    async Task HandleNewLink()
    {
        if ( LinkIsEmptyAfterChangesApplied() )
            return;
        
        _dlService = new YoutubeDownloader( _youtubeLink );

        Reply<bool> reply = await _dlService.TryInitialize();
        
        if ( !reply.Success )
        {
            _logger.LogWithConsole( $"Failed to obtain stream manifest! Reply message: {reply.PrintDetails()}" );
            VideoName = InvalidVideoName;
            Message = reply.PrintDetails();
            _dlService = null;
            return;
        }

        IsVideoSettingsEnabled = true;
        VideoName = $"{_dlService.VideoName ?? DefaultVideoName} : Length = {_dlService.VideoDuration}";

        VideoImageBitmap = _dlService?.ThumbnailBitmap;
        await HandleNewStreamType();
    }
    async Task DownloadStream()
    {
        if ( !ValidateBeforeTryDownload( out StreamType streamType ) )
            return;

        IsLinkBoxEnabled = false;
        IsVideoSettingsEnabled = false;

        await ExecuteDownload( streamType );

        HasMessage = true;
        IsLinkBoxEnabled = true;
        IsVideoSettingsEnabled = true;
    }
    async Task UpdateSettings()
    {
        IsLinkBoxEnabled = false;
        IsVideoSettingsEnabled = false;

        AppSettingsModel settingsModel = new()
        {
            DownloadLocation = _downloadLocation
        };

        Reply<bool> reply = await _settingsManager.SaveSettings( settingsModel );

        if ( !reply.Success )
        {
            HasMessage = true;
            Message = reply.PrintDetails();
        }
        
        IsLinkBoxEnabled = true;
        IsVideoSettingsEnabled = true;
    }

    // Private Methods
    async Task ExecuteDownload( StreamType streamType)
    {
        TryParseVideoDlTimes( _streamStartTime, _streamEndTime, out TimeSpan? start, out TimeSpan? end );
        
        StreamSettings streamSettings = new(
            _settingsManager.Settings.DownloadLocation, streamType, _streamQualities.IndexOf( _selectedStreamQualityName ), start, end );
        
        Reply<bool> reply = await _dlService!.Download( streamSettings );

        Message = reply.Success
            ? SuccessDownloadMessage
            : reply.PrintDetails();
    }
    static void TryParseVideoDlTimes( string start, string end, out TimeSpan? startTime, out TimeSpan? endTime )
    {
        startTime = null;
        endTime = null;

        if ( string.IsNullOrWhiteSpace( start ) || string.IsNullOrWhiteSpace( end ) )
            return;

        if ( !TimeSpan.TryParseExact( start, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan parsedStartTime ) )
            return;

        if ( !TimeSpan.TryParseExact( end, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan parsedEndTime ) )
            return;

        startTime = parsedStartTime;
        endTime = parsedEndTime;
    }
    async Task HandleNewStreamType()
    {
        if ( _dlService is null || !Enum.TryParse( _selectedStreamTypeName, out StreamType streamType ) )
        {
            _logger.LogWithConsole( ServiceErrorType.AppError.ToString() );
            return;
        }

        List<string> streamQualities = await _dlService.GetStreamInfo( streamType );

        StreamQualities = streamQualities.Count > 0
            ? streamQualities
            : [ ];

        SelectedStreamQuality = string.Empty;
    }
    void CloseMessage()
    {
        HasMessage = false;
    }
    bool LinkIsEmptyAfterChangesApplied()
    {
        bool linkIsEmpty = string.IsNullOrWhiteSpace( _youtubeLink );

        LoadDefaultImage();
        IsVideoSettingsEnabled = false;
        HasMessage = false;
        SelectedStreamType = string.Empty;
        Message = string.Empty;
        VideoName = linkIsEmpty ? DefaultVideoName : LoadingVideoName;
        StreamQualities = [ ];
        _dlService = null;

        return linkIsEmpty;
    }
    void LoadDefaultImage()
    {
        VideoImageBitmap = new Bitmap( AssetLoader.Open( new Uri( DefaultVideoImage ) ) );
    }
    bool ValidateBeforeTryDownload( out StreamType streamType )
    {
        streamType = StreamType.Mixed;
        
        // No Service
        if ( _dlService is null )
        {
            Message = "Youtube download service is null!";
            HasMessage = true;
            return false;
        }
        // Invalid Selected Stream Type
        if ( !Enum.TryParse( _selectedStreamTypeName, out streamType ) )
        {
            Message = "Invalid Stream Type!";
            HasMessage = true;
            return false;
        }
        // Invalid Selected Stream Quality
        if ( !_streamQualities.Contains( _selectedStreamQualityName ) )
        {
            Message = "Invalid Stream Quality!";
            HasMessage = true;
            return false;
        }
        
        return true;
    }
}