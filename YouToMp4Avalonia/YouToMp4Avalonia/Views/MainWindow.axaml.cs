using System;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using YouToMp4Avalonia.Models;
using YouToMp4Avalonia.Services;
using YouToMp4Avalonia.ViewModels;

namespace YouToMp4Avalonia.Views;

public sealed partial class MainWindow : Window, IDisposable
{
    // Services
    readonly FileLogger _logger = Program.ServiceProvider.GetService<FileLogger>()!;
    readonly SettingsManager SettingsService = Program.ServiceProvider.GetService<SettingsManager>()!;
    
    // Cached Views
    readonly YtDownloaderView _downloadView;
    YtSearchView? _youtubeView;
    ArchiveView? _archiveView;
    
    // Initialization
    public void Dispose()
    {
        SettingsService.SettingsChanged -= OnChangeSettings;
    }
    public MainWindow()
    {
        DataContext = new MainWindowViewModel();
        InitializeComponent();
        
        _downloadView = new YtDownloaderView();
        MainContent.Content = _downloadView;

        SettingsService.SettingsChanged += OnChangeSettings;
        OnChangeSettings( SettingsService.Settings );
    }
    protected override void OnClosed( EventArgs e )
    {
        SettingsService.SettingsChanged -= OnChangeSettings;
        base.OnClosed( e );
    }
    
    // UI Event Methods
    void OnChangeSettings( AppSettingsModel? newSettings )
    {
        string? img = newSettings?.SelectedBackgroundImage;
        
        if ( string.IsNullOrWhiteSpace( img ) )
            return;

        if ( img == AppSettingsModel.TransparentBackgroundKeyword )
        {
            Background = null;
            return;
        }

        Stream? stream;
        
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            stream = assembly.GetManifestResourceStream( img );
        }
        catch ( Exception e )
        {
            _logger.LogWithConsole( $"{e} : {e.Message}" );
            return;
        }
        
        if ( stream is null )
        {
            stream?.Dispose();
            return;
        }

        Background = new ImageBrush( new Bitmap( stream ) )
        {
            Stretch = Stretch.UniformToFill
        };

        stream.Dispose();
    }
    void OnNewPage()
    {
        MainContent.IsEnabled = true;
    }
    void OnClickViewYoutubeDownloader( object? sender, RoutedEventArgs args )
    {
        MainContent.Content = _downloadView;
        OnNewPage();
    }
    void OnClickViewYoutubeSearch( object? sender, RoutedEventArgs args )
    {
        _youtubeView ??= new YtSearchView();
        MainContent.Content = _youtubeView;
        OnNewPage();
    }
    void OnClickViewArchive( object? sender, RoutedEventArgs args )
    {
        _archiveView ??= new ArchiveView();
        MainContent.Content = _archiveView;
        OnNewPage();
    }
    void OnClickSettings( object? sender, RoutedEventArgs args )
    {
        MainContent.Content = new SettingsView();
        OnNewPage();
    }
}