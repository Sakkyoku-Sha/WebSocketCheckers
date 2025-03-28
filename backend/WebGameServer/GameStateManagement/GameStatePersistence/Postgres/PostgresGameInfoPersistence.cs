using System.Text.Json;
using Npgsql;
using WebGameServer.State;

namespace WebGameServer.GameStateManagement.GameStatePersistence.Postgres;

public class PostgresGameInfoPersistence : IGameInfoPersistence
{
    private readonly NpgsqlConnection _npgsqlConnection;
    
    public PostgresGameInfoPersistence(PostgresConnection connection)
    {
        _npgsqlConnection = connection.Connection;
    }
    
    private const string SaveCommandText = "INSERT INTO GAME_INFO (game_id, game_info_json) VALUES (@gameId, @gameInfoJson) ON CONFLICT (game_id) DO UPDATE SET game_info_json = @gameInfoJson";
    public async Task SaveGameInfoAsync(GameInfo gameInfo)
    {
        var command = new NpgsqlCommand(SaveCommandText, _npgsqlConnection);
        command.Parameters.AddWithValue("gameId", gameInfo.GameId);
        
        var jsonString = JsonSerializer.Serialize(gameInfo);
        command.Parameters.AddWithValue("gameInfoJson", NpgsqlTypes.NpgsqlDbType.Jsonb, jsonString);
    
        await command.ExecuteNonQueryAsync();
    }
    
    private const string GetCommandText = "SELECT * FROM GAME_INFO WHERE game_id = @gameId";
    public async Task<GameInfo?> TryGetGameInfoAsync(Guid gameId)
    {
        var command = new NpgsqlCommand(GetCommandText, _npgsqlConnection);
        command.Parameters.AddWithValue("gameId", gameId);
        
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var jsonData = reader.GetString(0);
            var gameInfo = JsonSerializer.Deserialize<GameInfo>(jsonData);
            return gameInfo;
        }
        
        return null;
    }

    public GameInfo? TryGetInfoState(Guid gameId)
    {
        var command = new NpgsqlCommand(GetCommandText, _npgsqlConnection);
        command.Parameters.AddWithValue("gameId", gameId);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var jsonData = reader.GetString(0);
            var gameInfo = JsonSerializer.Deserialize<GameInfo>(jsonData);
            return gameInfo;
        }
        
        return null;
    }
}