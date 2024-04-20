using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YouToMp4Avalonia.Models;

namespace YouToMp4Avalonia.Services;

public sealed class SettingsManager : SingletonService<SettingsManager>
{
    readonly FileLogger Logger = FileLogger.Instance;
    
    // Constants
    const string UserSettingsDirectory = "./UserSettings";
    const string UserSettingsFilepath = UserSettingsDirectory + "/UserSettings.txt";
    const string FailLoadMessage = "Failed to load settings file! You can still make changes, but they might not be saved once you close the app.";
    const string FailedSaveMessage = "Failed to save settings to disk! Changes will still persist until you close the app.";
    
    // Settings Model
    public AppSettingsModel Settings { get; private set; }
    string? _loadError;

    // Constructor
    public SettingsManager()
    {
        Settings = LoadSettings();
    }
    AppSettingsModel LoadSettings()
    {
        try
        {
            if ( !File.Exists( UserSettingsFilepath ) )
            {
                _loadError = $"{ServiceErrorType.IoError} : Settings file doesn't exist";
                Logger.LogWithConsole( _loadError );
                return new AppSettingsModel();
            }
            
            string json = File.ReadAllText( UserSettingsFilepath );
            AppSettingsModel? loadedSettings = JsonSerializer.Deserialize( json, AppSettingsModelContext.Default.AppSettingsModel );
            bool loaded = loadedSettings is not null;
            
            if ( loaded )
                Settings = loadedSettings!;

            return Settings;
        }
        catch ( Exception e )
        {
            _loadError = e.ToString();
            Logger.LogWithConsole( e );
            return new AppSettingsModel();
        }
    }
    
    // Public Methods
    public async Task<Reply<AppSettingsModel>> LoadSettingsAsync()
    {
        try
        {
            if ( !File.Exists( UserSettingsFilepath ) )
                return new Reply<AppSettingsModel>( Settings, ServiceErrorType.NotFound, FailLoadMessage );
            
            string json = await File.ReadAllTextAsync( UserSettingsFilepath );
            AppSettingsModel? loadedSettings = JsonSerializer.Deserialize( json, AppSettingsModelContext.Default.AppSettingsModel );
            
            bool loaded = loadedSettings is not null;

            if ( loaded )
                Settings = loadedSettings!;

            return loaded
                ? new Reply<AppSettingsModel>( Settings )
                : new Reply<AppSettingsModel>( Settings, ServiceErrorType.NotFound, FailLoadMessage );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( e );
            return new Reply<AppSettingsModel>( ServiceErrorType.IoError, FailLoadMessage );
        }
    }
    public async Task<Reply<bool>> SaveSettings( AppSettingsModel newSettings )
    {
        try
        {
            if ( !Directory.Exists( UserSettingsDirectory ) )
                Directory.CreateDirectory( UserSettingsDirectory );
            
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes( newSettings, AppSettingsModelContext.Default.AppSettingsModel );

            await using FileStream fs = new( UserSettingsFilepath, FileMode.Create );
            await fs.WriteAsync( jsonBytes );
            return new Reply<bool>( true );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( e );
            return new Reply<bool>( ServiceErrorType.IoError, FailedSaveMessage );
        }
        finally
        {
            Settings = newSettings;
        }
    }
}