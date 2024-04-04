using System;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using YouToMp4Avalonia.Models;
using YouToMp4Avalonia.Services;

namespace YouToMp4Avalonia.ViewModels;

public abstract class BaseViewModel : ReactiveObject, IDisposable
{
    // Services
    protected readonly FileLogger Logger = Program.ServiceProvider.GetService<FileLogger>()!;
    protected readonly SettingsManager SettingsManager = Program.ServiceProvider.GetService<SettingsManager>()!;
    
    // Reactive Property Fields
    bool _isFree;
    bool _hasMessage;
    string _message = string.Empty;
    
    // Commands
    public ReactiveCommand<Unit, Unit> CloseMessageCommand { get; }
    
    // Constructor
    public void Dispose()
    {
        GC.SuppressFinalize( this ); // Rider Suggested Optimization
        SettingsManager.SettingsChanged -= OnAppSettingsChanged;
    }
    protected BaseViewModel()
    {
        CloseMessageCommand = ReactiveCommand.Create( CloseMessage );
    }
    
    // Reactive Properties
    public bool IsFree
    {
        get => _isFree;
        set => this.RaiseAndSetIfChanged( ref _isFree, value );
    }
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged( ref _message, value );
    }
    public bool HasMessage
    {
        get => _hasMessage;
        set => this.RaiseAndSetIfChanged( ref _hasMessage, value );
    }
    
    // Methods
    protected virtual void OnAppSettingsChanged( AppSettingsModel newSettings )
    {
        
    }
    protected static string ExString( Exception e, string? message = null )
    {
        return string.IsNullOrWhiteSpace( message )
            ? $"{e} : {e.Message}"
            : $"{message} : {e} : {e.Message}";
    }
    public void ShowMessage( string message )
    {
        Message = message;
        HasMessage = true;
    }
    void CloseMessage()
    {
        HasMessage = false;
    }
}