using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using YoutubeToMp4Blazor.Data;
using YoutubeToMp4Blazor.Models;

namespace YoutubeToMp4Blazor.Features.Authentication;

public sealed class AuthRepository( ILogger<AuthRepository> logger, DatabaseContext context )
{
    // Fields
    const int MaxRetries = 3;
    const int RetryDelayMilliseconds = 3000;
    readonly ILogger<AuthRepository> _logger = logger;
    readonly DatabaseContext _context = context;
    
    // Methods
    public async Task<IEnumerable<DlKey>> TryGetKeys()
    {
        await using SqlConnection connection = await _context.GetOpenConnection();
        
        if ( connection.State is not ConnectionState.Open ) {
            _logger.LogError( "Failed to open connection to database!" );
            return new List<DlKey>();
        }

        int currentRetry = 0;
        while ( currentRetry < MaxRetries )
        {
            try
            {
                return await connection.QueryAsync<DlKey>( "SELECT * FROM AccessKeys", commandType: CommandType.Text );
            }
            catch ( TimeoutException timeout )
            {
                currentRetry++;

                if ( currentRetry >= MaxRetries ) {
                    throw new Exception( timeout.Message, timeout );
                }
                
                await Task.Delay( RetryDelayMilliseconds );
            }
            catch ( Exception e )
            {
                _logger.LogError( e, e.Message );
                break;
            }
            finally
            {
                currentRetry++;
            }
        }

        return new List<DlKey>();
    }
    public async Task<bool> TryAddRecords( List<DlKeyRecord> records )
    {
        await using SqlConnection connection = await _context.GetOpenConnection();

        if ( connection.State != ConnectionState.Open )
        {
            _logger.LogError( "Failed to open connection to database!" );
            return false;
        }

        await using SqlTransaction? transaction = connection.BeginTransaction();
        await transaction.SaveAsync( "start" );

        foreach ( DlKeyRecord r in records )
        {
            if ( await TryAddRecord( connection, transaction, r ) )
                continue;
            
            _logger.LogError( "Failed to insert a record. Breaking operation." );
            await transaction.RollbackAsync( "start" );
            return false;
        }
        await transaction.CommitAsync();
        return false;
    }
    async Task<bool> TryAddRecord( SqlConnection connection, SqlTransaction transaction, DlKeyRecord record )
    {
        int currentTry = 0;
        while ( currentTry < MaxRetries )
        {
            try
            {
                currentTry++;
                StringBuilder sqlBuilder = new();
                sqlBuilder.Append( "INSERT INTO AccessKeyRecords (AccessKeyId, IpAddress, RequestType, DateCreated)" );
                sqlBuilder.Append( $"VALUES ({record.KeyId}, {record.KeyId}, {record.KeyId}, {record.KeyId})" );

                int rowsAffected = await connection.ExecuteAsync( sqlBuilder.ToString(), transaction, commandType: CommandType.Text );
                return true;
            }
            catch ( TimeoutException timeout )
            {
                if ( currentTry >= MaxRetries )
                {
                    _logger.LogError( timeout, "Timeout retry limit reached!" );
                    return false;
                }
                
                await Task.Delay( RetryDelayMilliseconds );
            }
            catch ( Exception e )
            {
                _logger.LogError( e, e.Message );
                return false;
            }
            finally
            {
                currentTry++;
            }
        }

        return false;
    }
}