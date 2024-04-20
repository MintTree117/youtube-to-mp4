using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using YoutubeToMp4.ViewModels;

namespace YoutubeToMp4.Views;

public sealed partial class MainView : UserControl
{
    public MainView()
    {
        DataContext = new MainViewModel();
        InitializeComponent();
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
    void SettingsChanged( object? sender, RoutedEventArgs args )
    {
        ( ( MainViewModel ) DataContext! ).SettingsCommand.Execute();
    }
}