using System;
using Avalonia;
using Avalonia.ReactiveUI;
using YouToMp4Avalonia.Services;

namespace YouToMp4Avalonia;

sealed class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = default!;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread] public static void Main( string[] args )
    {
        // Register custom singleton services
        FileLogger.Create();
        SettingsManager.Create();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime( args );
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}