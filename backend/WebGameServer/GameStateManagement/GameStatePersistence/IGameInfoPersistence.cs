using WebGameServer.State;

namespace WebGameServer.GameStateManagement.GameStatePersistence;

public interface IGameInfoPersistence
{
    public Task SaveGameInfoAsync(GameInfo gameInfo);
    public Task<GameInfo?> TryGetGameInfoAsync(Guid gameId);
    public GameInfo? TryGetInfoState(Guid gameId);
}