using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YoutubeToMp4.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
}