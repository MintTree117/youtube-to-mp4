using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using YouToMp4Avalonia.ViewModels;

namespace YouToMp4Avalonia.Views;

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