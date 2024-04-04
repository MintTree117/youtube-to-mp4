using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using YouToMp4Avalonia.ViewModels;

namespace YouToMp4Avalonia.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView()
    {
        DataContext = new SettingsViewModel();
        InitializeComponent();
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
}