using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using YouToMp4Avalonia.Views;

namespace YouToMp4Avalonia;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load( this );
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
        {
            desktop.MainWindow = new MainWindow();
        }
        else
        {
            Console.WriteLine( "Failed to create desktop.MainWindow because if clause failed in App.axaml.cs!" );
        }

        base.OnFrameworkInitializationCompleted();
    }
}