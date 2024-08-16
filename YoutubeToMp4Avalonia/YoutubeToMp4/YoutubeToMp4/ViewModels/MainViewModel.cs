﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using YoutubeToMp4.Models;
using YoutubeToMp4.Services;

namespace YoutubeToMp4.ViewModels;

public sealed class MainViewModel : ReactiveObject
{
    // Services
    readonly FileLogger _logger = FileLogger.Instance;
    readonly SettingsManager _settingsManager = SettingsManager.Instance;
    YoutubeDownloader? _dlService;
    
    // Constants
    const string DefaultVideoName = "Download Youtube Videos";
    const string LoadingVideoName = "Loading Video...";
    const string InvalidVideoName = "Invalid Video Link";
    const string SuccessDownloadMessage = "Download success!";
    const string FailDownloadMessage = "Failed to download!";
    const string DefaultVideoImage = "avares://YoutubeToMp4/Assets/default_stream_image.png";
    
    // Property Field List Values
    Bitmap? _videoImageBitmap;
    List<string> _streamTypes = Enum.GetNames<StreamType>().ToList();
    List<string> _singleStreamQualities = [ ];
    List<string> _muxedVideoStreamQualities = [];
    List<string> _muxedAudioStreamQualities = [];
    string _youtubeLink = string.Empty;
    string _videoName = DefaultVideoName;
    string _selectedStreamTypeName = string.Empty; // saves state between downloads for user convenience
    string _selectedStreamQualityName = string.Empty;
    string _selectedMuxedVideoStreamQualityName = string.Empty;
    string _selectedMuxedAudioStreamQualityName = string.Empty;
    /*string _streamStartTime = string.Empty;
    string _streamEndTime = string.Empty;*/
    string _downloadLocation = string.Empty;
    string _message = string.Empty;
    bool _isLinkBoxEnabled;
    bool _isStreamSettingsEnabled;
    bool _isSingleStreamSettingsEnabled;
    bool _isMuxedStreamSettingsEnabled;
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
    public List<string> SingleStreamQualities
    {
        get => _singleStreamQualities;
        set => this.RaiseAndSetIfChanged( ref _singleStreamQualities, value );
    }
    public List<string> MuxedVideoStreamQualities
    {
        get => _muxedVideoStreamQualities;
        set => this.RaiseAndSetIfChanged( ref _muxedVideoStreamQualities, value );
    }
    public List<string> MuxedAudioStreamQualities
    {
        get => _muxedAudioStreamQualities;
        set => this.RaiseAndSetIfChanged( ref _muxedAudioStreamQualities, value );
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
    public string SelectedStreamType
    {
        get => _selectedStreamTypeName;
        set
        {
            this.RaiseAndSetIfChanged( ref _selectedStreamTypeName, value );
            NewStreamCommand.Execute();
        }
    } // saves state between downloads for user conveniences
    public string SelectedStreamQuality
    {
        get => _selectedStreamQualityName;
        set => this.RaiseAndSetIfChanged( ref _selectedStreamQualityName, value );
    }
    public string SelectedMuxedVideoStreamQuality
    {
        get => _selectedMuxedVideoStreamQualityName;
        set => this.RaiseAndSetIfChanged( ref _selectedMuxedVideoStreamQualityName, value );
    }
    public string SelectedMuxedAudioStreamQuality
    {
        get => _selectedMuxedAudioStreamQualityName;
        set => this.RaiseAndSetIfChanged( ref _selectedMuxedAudioStreamQualityName, value );
    }
    /*public string StreamStartTime
    {
        get => _streamStartTime;
        set => this.RaiseAndSetIfChanged( ref _streamStartTime, value );
    }
    public string StreamEndTime
    {
        get => _streamEndTime;
        set => this.RaiseAndSetIfChanged( ref _streamEndTime, value );
    }*/
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
    public bool IsStreamSettingsEnabled
    {
        get => _isStreamSettingsEnabled;
        set => this.RaiseAndSetIfChanged( ref _isStreamSettingsEnabled, value );
    }
    public bool IsSingleStreamSettingsEnabled
    {
        get => _isSingleStreamSettingsEnabled;
        set => this.RaiseAndSetIfChanged( ref _isSingleStreamSettingsEnabled, value );
    }
    public bool IsMuxedStreamSettingsEnabled
    {
        get => _isMuxedStreamSettingsEnabled;
        set => this.RaiseAndSetIfChanged( ref _isMuxedStreamSettingsEnabled, value );
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

        if ( !await GetStreamInfo( _dlService ) )
            return;

        IsStreamSettingsEnabled = true;
        VideoName = ConstructStreamTitleDisplayName();

        VideoImageBitmap = _dlService?.ThumbnailBitmap;
        await HandleNewStreamType();
    }
    async Task DownloadStream()
    {
        Console.WriteLine( "Reached download stream in view model" );
        if ( !ValidateBeforeTryDownload( out StreamType streamType ) )
            return;

        Console.WriteLine( "Passed validation" );

        IsLinkBoxEnabled = false;
        IsStreamSettingsEnabled = false;
        IsMuxedStreamSettingsEnabled = false;
        IsSingleStreamSettingsEnabled = false;

        if (streamType is not StreamType.Mixed)
            await ExecuteSingleDownload( streamType );
        else
            await ExecuteMuxedDownload();

        Console.WriteLine( "Executed" );

        HasMessage = true;
        IsLinkBoxEnabled = true;
        IsStreamSettingsEnabled = true;
        DisplayCorrectStreamSettings();
    }
    async Task UpdateSettings()
    {
        IsLinkBoxEnabled = false;
        IsStreamSettingsEnabled = false;

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
        IsStreamSettingsEnabled = true;
        DisplayCorrectStreamSettings();
    }

    // Private Methods
    async Task<bool> GetStreamInfo( YoutubeDownloader dlService )
    {
        Reply<bool> reply = await dlService.TryInitialize();

        if ( reply.Success ) 
            return true;
        
        _logger.LogWithConsole( $"Failed to obtain stream manifest! Reply message: {reply.PrintDetails()}" );
        VideoName = InvalidVideoName;
        Message = reply.PrintDetails();
        _dlService = null;
        return false;
    }
    async Task ExecuteSingleDownload( StreamType streamType )
    {
        SingleStreamSettings singleStreamSettings = new(
            _settingsManager.Settings.DownloadLocation, 
            streamType, 
            _singleStreamQualities.IndexOf( _selectedStreamQualityName ) );

        Reply<bool> reply = await _dlService!.DownloadSingle( singleStreamSettings );

        Message = reply.Success
            ? SuccessDownloadMessage
            : reply.PrintDetails();
    }
    async Task ExecuteMuxedDownload()
    {
        MuxedStreamSettings muxedStreamSettings = new(
            _settingsManager.Settings.DownloadLocation,
            _muxedVideoStreamQualities.IndexOf( _selectedMuxedVideoStreamQualityName ),
            _muxedAudioStreamQualities.IndexOf( _selectedMuxedAudioStreamQualityName ) );

        Reply<bool> reply = await _dlService!.DownloadMuxed( muxedStreamSettings );

        Message = reply.Success
            ? SuccessDownloadMessage
            : reply.PrintDetails();
    }
    async Task HandleNewStreamType()
    {
        if ( _dlService is null || !Enum.TryParse( _selectedStreamTypeName, out StreamType streamType ) )
        {
            _logger.LogWithConsole( ServiceErrorType.AppError.ToString() );
            return;
        }
        
        DisplayCorrectStreamSettings();

        if (IsMuxedStreamSettingsEnabled)
        {
            List<string> videoStreamQualities = await _dlService.GetStreamInfo( StreamType.Video );
            List<string> audioStreamQualities = await _dlService.GetStreamInfo( StreamType.Audio );

            MuxedVideoStreamQualities = videoStreamQualities.Count > 0
                ? videoStreamQualities
                : [];

            MuxedAudioStreamQualities = audioStreamQualities.Count > 0
                ? audioStreamQualities
                : [];

            SelectedMuxedVideoStreamQuality = string.Empty;
            SelectedMuxedAudioStreamQuality = string.Empty;
        }
        else
        {
            List<string> streamQualities = await _dlService.GetStreamInfo( streamType );

            SingleStreamQualities = streamQualities.Count > 0
                ? streamQualities
                : [];

            SelectedStreamQuality = string.Empty;
        }
    }
    /*static void TryParseVideoDlTimes( string start, string end, out TimeSpan? startTime, out TimeSpan? endTime )
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
    }*/
    void CloseMessage()
    {
        HasMessage = false;
    }
    bool LinkIsEmptyAfterChangesApplied()
    {
        bool linkIsEmpty = string.IsNullOrWhiteSpace( _youtubeLink );

        LoadDefaultImage();
        IsStreamSettingsEnabled = false;
        IsSingleStreamSettingsEnabled = false;
        IsMuxedStreamSettingsEnabled = false;
        HasMessage = false;
        SelectedStreamType = string.Empty;
        Message = string.Empty;
        VideoName = linkIsEmpty ? DefaultVideoName : LoadingVideoName;
        SingleStreamQualities = [ ];
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

        return streamType is StreamType.Mixed 
            ? ValidateDownloadMuxed() 
            : ValidateDownloadSingle();
    }
    bool ValidateDownloadSingle()
    {
        // Invalid Selected Stream Quality
        if (_singleStreamQualities.Contains( _selectedStreamQualityName )) 
            return true;
        
        Message = "Invalid Stream Quality!";
        HasMessage = true;
        return false;
    }
    bool ValidateDownloadMuxed()
    {
        // Invalid Selected Video Quality
        if (!_muxedVideoStreamQualities.Contains( _selectedMuxedVideoStreamQualityName ))
        {
            Message = "Invalid Video Quality!";
            HasMessage = true;
            return false;
        }

        // Invalid Selected Audio Quality
        if (!_muxedAudioStreamQualities.Contains( _selectedMuxedAudioStreamQualityName ))
        {
            Message = "Invalid Audio Quality!";
            HasMessage = true;
            return false;
        }

        return true;
    }
    string ConstructStreamTitleDisplayName()
    {
        if ( _dlService is null )
            return DefaultVideoName;

        StringBuilder builder = new();
        builder.Append( _dlService.VideoName ?? DefaultVideoName );
        builder.Append( $" : Author = {_dlService.VideoAuthor ?? "No Author Found"}" );
        builder.Append( $" : Length = {_dlService.VideoDuration}" );

        return builder.ToString();
    }
    void DisplayCorrectStreamSettings()
    {
        var mixed = Enum.TryParse( _selectedStreamTypeName, out StreamType type )
            && type is StreamType.Mixed;
        
        IsSingleStreamSettingsEnabled = !mixed;
        IsMuxedStreamSettingsEnabled = mixed;
    }
}