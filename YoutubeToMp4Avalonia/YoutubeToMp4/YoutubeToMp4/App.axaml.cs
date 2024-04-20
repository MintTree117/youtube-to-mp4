using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using YoutubeToMp4.ViewModels;
using YoutubeToMp4.Views;

namespace YoutubeToMp4;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load( this );
    }
    
    // This is where desktop vs mobile view is decided
    public override void OnFrameworkInitializationCompleted()
    {
        switch ( ApplicationLifetime )
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}