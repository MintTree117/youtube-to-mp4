using System;
using Microsoft.Extensions.DependencyInjection;
using YouToMp4Avalonia.Services;
using Avalonia;
using Avalonia.ReactiveUI;

namespace YouToMp4Avalonia;

sealed class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = default!;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread] public static void Main( string[] args )
    {
        ServiceCollection serviceCollection = [ ];
        ConfigureServices( serviceCollection );

        ServiceProvider = serviceCollection.BuildServiceProvider();

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

    static void ConfigureServices( IServiceCollection services )
    {
        services.AddSingleton<FileLogger>();
        services.AddSingleton<SettingsManager>();
        services.AddSingleton<YoutubeClientHolder>();
        services.AddSingleton<HttpController>();
        services.AddSingleton<YoutubeDownloader>();
        services.AddSingleton<YoutubeBrowser>();
        services.AddSingleton<FFmpegChecker>();
        services.AddSingleton<ArchiveService>();
    }
}