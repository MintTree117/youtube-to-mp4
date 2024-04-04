using AngleSharp.Browser;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YouToMp4Avalonia.Views;

public partial class ResultMessage : UserControl
{   
    // Define the Text property using Avalonia's property system
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<ResultMessage, string>( 
        nameof( Text ), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay );
    
    // ^ Bound to ViewModel through axaml declaration and to local static property ^
    public string Text
    {
        get => GetValue( TextProperty );
        set => SetValue( TextProperty, value );
    }
    
    // Close command property registration so I can call this command from my button
    public static readonly StyledProperty<ICommand> CloseCommandProperty = AvaloniaProperty.Register<ResultMessage, ICommand>( nameof( CloseCommand ) );
    // ^^
    public ICommand CloseCommand
    {
        get => GetValue( CloseCommandProperty );
        set => SetValue( CloseCommandProperty, value );
    }
    
    // Constructor
    public ResultMessage()
    {
        InitializeComponent();
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load( this );
    }
}