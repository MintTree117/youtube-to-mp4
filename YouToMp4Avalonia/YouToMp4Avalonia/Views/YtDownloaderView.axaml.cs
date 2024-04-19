using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using YouToMp4Avalonia.ViewModels;

namespace YouToMp4Avalonia.Views;

public sealed partial class YtDownloaderView : UserControl
{
    public YtDownloaderView()
    {
        DataContext = new YtDownloaderViewModel();
        InitializeComponent();
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
    void SettingsChanged( object? sender, RoutedEventArgs args )
    {
        ( ( YtDownloaderViewModel ) DataContext! ).SettingsCommand.Execute();
    }
}