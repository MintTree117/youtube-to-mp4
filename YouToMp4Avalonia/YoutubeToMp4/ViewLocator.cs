using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ReactiveUI;

namespace YoutubeToMp4;

public sealed class ViewLocator : IDataTemplate
{
    public Control? Build( object? data )
    {
        if ( data is null )
            return null;

        string name = data.GetType().FullName!.Replace( "ViewModel", "View", StringComparison.Ordinal );
#pragma warning disable IL2057
        var type = Type.GetType( name );
#pragma warning restore IL2057

        if ( type == null ) 
            return new TextBlock { Text = "Not Found: " + name };
        
        var control = ( Control ) Activator.CreateInstance( type )!;
        control.DataContext = data;
        return control;

    }

    public bool Match( object? data )
    {
        return data is ReactiveObject;
    }
}