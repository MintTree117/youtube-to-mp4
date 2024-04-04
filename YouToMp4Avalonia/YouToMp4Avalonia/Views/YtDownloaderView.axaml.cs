using Avalonia.Controls;
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
}