using Microsoft.Data.SqlClient;

namespace YouToMp4Blazor.Data;

public sealed class DatabaseContext
{
    readonly ILogger<DatabaseContext> Logger;
    readonly string _connectionString;

    public DatabaseContext( ILogger<DatabaseContext> logger, IConfiguration config )
    {
        Logger = logger;
        _connectionString = config.GetConnectionString( "DefaultConnection" ) ?? string.Empty;
    }

    public async Task<SqlConnection> GetOpenConnection()
    {
        try
        {
            var connection = new SqlConnection( _connectionString );
            await connection.OpenAsync();
            return connection;
        }
        catch ( Exception e )
        {
            Logger.LogError( e.Message, e );
            return new SqlConnection();
        }
    }
}