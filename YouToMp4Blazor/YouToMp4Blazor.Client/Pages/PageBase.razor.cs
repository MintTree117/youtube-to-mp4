using Microsoft.AspNetCore.Components;

namespace YouToMp4Blazor.Client.Pages;

public abstract class PageBase : ComponentBase
{
    // Properties
    [Inject] protected ILogger<Home> Logger { get; init; } = default!;
    
    public string LoadingMessage => _loadingMessage;
    public string LoaderCss => _loaderCss;
    public string AlertMessage => _alertMessage;
    public string AlertCss => _alertCss;
    public string AlertButtonCss => _alertButtonCss;
    
    // Fields
    protected const string CssBlock = "d-block";
    const string CssHide = "d-none";
    const string CssFlex = "d-flex";
    
    protected bool _isLoading;
    string _loadingMessage = string.Empty;
    string _loaderCss = CssHide;
    string _alertMessage = string.Empty;
    string _alertCss = CssHide;
    string _alertButtonCss = string.Empty;
    
    // Methods
    public void CloseAlert()
    {
        _alertMessage = string.Empty;
        _alertCss = CssHide;
        StateHasChanged();
    }
    protected void ShowAlert( AlertType type, string message )
    {
        _alertMessage = message;
        SetAlertCss( type );
        // TODO: start fade animation
        StateHasChanged();
    }
    protected void SetAlertCss( AlertType type )
    {
        _alertCss = type switch
        {
            AlertType.Success => $"alert-success {CssFlex}",
            AlertType.Warning => $"alert-warning {CssFlex}",
            AlertType.Danger => $"alert-danger {CssFlex}",
            _ => $"alert-danger {CssFlex}"
        };
        _alertButtonCss = type switch
        {
            AlertType.Success => "btn-success",
            AlertType.Warning => "btn-warning",
            AlertType.Danger => "btn-danger",
            _ => "btn-danger"
        };
    }
    protected void ToggleLoading( bool value, string? loadingMessage = null )
    {
        _isLoading = value;
        _loaderCss = value ? CssFlex : CssHide;
        _loadingMessage = loadingMessage ?? string.Empty;
        StateHasChanged();
    }
}