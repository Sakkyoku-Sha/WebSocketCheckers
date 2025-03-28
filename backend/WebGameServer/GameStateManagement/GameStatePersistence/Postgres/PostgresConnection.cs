using Npgsql;
using Path = System.IO.Path;

namespace WebGameServer.GameStateManagement.GameStatePersistence.Postgres;

public class PostgresConnection : IDisposable, IAsyncDisposable
{
    //Too lazy to make environment variables at the moment. 
    private const string ConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=mysecretpassword;";
    public readonly NpgsqlConnection Connection;
    
    public PostgresConnection()
    {
        //Todo resilience on this connection / retries / etc... 
        Connection = new NpgsqlConnection(ConnectionString);
    }
    public async Task Connect()
    {
        await Connection.OpenAsync();
        await RunSetup();
    }

    private async Task RunSetup()
    {
        //Run setup script which will create the DB if and only if it does not exist. 
        var setupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameStateManagement/GameStatePersistence/Postgres/setup.sql");
        var sqlScript  = await File.ReadAllTextAsync(setupFile);

        await using var command = new NpgsqlCommand(sqlScript, Connection);
        await command.ExecuteNonQueryAsync();
    }
    
    public void Dispose()
    {
        Connection.Dispose();
    }
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}