using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using YouToMp4Avalonia.Enums;
using YouToMp4Avalonia.Models;
using YouToMp4Avalonia.Services;

namespace YouToMp4Avalonia.ViewModels;

public sealed class YtSearchViewModel : BaseViewModel
{
    // Services Definitions
    readonly YoutubeBrowser _youtubeSearchService = Program.ServiceProvider.GetService<YoutubeBrowser>()!;
    
    // Property Field List Values
    readonly List<YoutubeSortType> _sortTypesDefinition = Enum.GetValues<YoutubeSortType>().ToList();
    readonly List<int> _resultCounts = [ 10, 20, 30, 50, 100, 200 ];
    
    // Property Fields
    IReadOnlyList<YoutubeSearchResult> _searchResults = [ ];
    List<string> _sortTypes = [ ];
    List<string> _resultCountNames = [ ];
    string _selectedSortType = string.Empty;
    string _selectedResultCountName = string.Empty;
    string _searchText = string.Empty;
    
    // Commands
    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    public ReactiveCommand<string, Unit> CopyUrlCommand { get; }
    
    // Constructor
    public YtSearchViewModel()
    {
        SortTypes = GetSortTypeNames();
        ResultCountNames = GetResultsPerPageNames( _resultCounts );
        SelectedSortType = _sortTypes[ 0 ];
        SelectedResultCountName = _resultCountNames[ 0 ];
        SearchCommand = ReactiveCommand.CreateFromTask( Search );
        CopyUrlCommand = ReactiveCommand.CreateFromTask<string>( async ( url ) => { await CopyUrlToClipboard( url ); } );

        IsFree = true;
    }
    
    // Reactive Properties
    public IReadOnlyList<YoutubeSearchResult> SearchResults
    {
        get => _searchResults;
        set => this.RaiseAndSetIfChanged( ref _searchResults, value );
    }
    public List<string> SortTypes
    {
        get => _sortTypes;
        set => this.RaiseAndSetIfChanged( ref _sortTypes, value );
    }
    public List<string> ResultCountNames
    {
        get => _resultCountNames;
        set => this.RaiseAndSetIfChanged( ref _resultCountNames, value );
    }
    public string SelectedSortType
    {
        get => _selectedSortType;
        set
        {
            this.RaiseAndSetIfChanged( ref _selectedSortType, value );
            OnChangeSortDropdown();
        }
    }
    public string SelectedResultCountName
    {
        get => _selectedResultCountName;
        set
        {
            this.RaiseAndSetIfChanged( ref _selectedResultCountName, value );
            OnChangeResultsDropdown();
        }
    }
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged( ref _searchText, value );
    }
    
    // Command Delegates
    async Task Search()
    {
        if ( !ValidateSearchParams( out int resultCountIndex ) )
        {
            ShowMessage( "Invalid search results count selected!" );
            return;
        }
        
        IsFree = false;

        ServiceReply<IReadOnlyList<YoutubeSearchResult>> searchReply = await _youtubeSearchService.GetStreams( _searchText, _resultCounts[ resultCountIndex ] );

        if ( searchReply is { Success: true, Data: not null } )
        {
            SearchResults = searchReply.Data;
            IsFree = true;
            return;
        }

        SearchResults = new List<YoutubeSearchResult>();
        ShowMessage( $"Failed to search : {searchReply.PrintDetails()}" );
        
        IsFree = true;
    }
    async Task CopyUrlToClipboard( string? url )
    {
        if ( string.IsNullOrWhiteSpace( url ) )
        {
            ShowMessage( "Tried to copy invalid url!" );
            return;
        }

        try
        {
            // TODO: Make this mobile accessible
            Window? mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if ( mainWindow?.Clipboard is null )
            {
                Logger.LogWithConsole( "Failed to obtain clipboard from main window!" );
                ShowMessage( "Failed to copy youtube link to clipboard!" );
                return;
            }

            await mainWindow.Clipboard.SetTextAsync( url );
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            ShowMessage( $"{ServiceErrorType.AppError.ToString()} : Failed to copy youtube link to clipboard!" );
        }
    }
    
    // Private Methods
    static List<string> GetSortTypeNames()
    {
        List<string> types = Enum.GetNames<YoutubeSortType>().ToList();

        for ( int i = 0; i < types.Count; i++ )
            types[ i ] = $"Sort: {types[ i ]}";

        return types;
    }
    static List<string> GetResultsPerPageNames( IEnumerable<int> values )
    {
        List<string> names = [ ];
        names.AddRange( from value in values select $"Show: {value}" );
        return names;
    }
    bool ValidateSearchParams( out int resultCountIndex )
    {
        resultCountIndex = -1;

        if ( string.IsNullOrWhiteSpace( _searchText ) )
        {
            Logger.LogWithConsole( "Search text is null!" );
            ShowMessage( "Search text is null!" );
            return false;
        }
        
        if ( !_resultCountNames.Contains( _selectedResultCountName ) )
        {
            Logger.LogWithConsole( "_resultCountNames out of bounds!" );
            ShowMessage( "Invalid _selectedResultsPerPage" );
            return false;
        }

        resultCountIndex = _resultCountNames.IndexOf( _selectedResultCountName );

        if ( resultCountIndex < 0 || resultCountIndex > _resultCounts.Count )
        {
            Logger.LogWithConsole( "resultCountIndex out of bounds!" );
            ShowMessage( "Invalid _selectedResultsPerPage" );
            return false;
        }

        return true;
    }
    void OnChangeSortDropdown()
    {
        IsFree = false;

        try
        {
            int index = _sortTypes.IndexOf( _selectedSortType );

            if ( index < 0 || index > _sortTypesDefinition.Count )
            {
                HasMessage = true;
                ShowMessage( "Invalid _selectedSortType!" );
                return;
            }

            SearchResults = _sortTypesDefinition[ index ] switch
            {
                YoutubeSortType.Default => SearchResults,
                YoutubeSortType.Alphabetical => _searchResults.OrderBy( r => r.Title ).ToList(),
                YoutubeSortType.Duration => _searchResults.OrderBy( r => r.Duration ).ToList(),
                YoutubeSortType.AlphabeticalReverse => _searchResults.OrderBy( r => r.Duration ).Reverse().ToList(),
                YoutubeSortType.DurationReverse => _searchResults.OrderBy( r => r.Duration ).Reverse().ToList(),
                _ => SearchResults
            };
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            ShowMessage( $"{ServiceErrorType.AppError} : An error occured while updating sort dropdown." );
        }

        IsFree = true;
    }
    void OnChangeResultsDropdown()
    {
        if ( string.IsNullOrWhiteSpace( _searchText ) )
            return;   
        
        SearchCommand.Execute();
    }
}