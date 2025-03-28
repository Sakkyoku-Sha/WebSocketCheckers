using WebGameServer.State;

namespace WebGameServer.GameStateManagement.KeyValueStore;

public class InMemoryKeyGameInfoStore : IKeyGameInfoStore
{
    private readonly Dictionary<Guid, GameInfo> _gameInfos = new();
    public bool TryGetState(Guid gameId, out GameInfo? state)
    {
        return _gameInfos.TryGetValue(gameId, out state);
    }
    public void SetGameInfo(Guid gameId, GameInfo gameInfo)
    {
        //Game States CAN be updated without retrival! 
        _gameInfos.TryAdd(gameId, gameInfo);
    }
    public void RemoveGameInfo(Guid gameId)
    {
        _gameInfos.Remove(gameId);
    }
    public Task<bool> TryGetStateAsync(Guid gameId, out GameInfo? state)
    {
        return Task.FromResult(TryGetState(gameId, out state));
    }
    public Task SetGameInfoAsync(Guid gameId, GameInfo gameInfo)
    {
        SetGameInfo(gameId, gameInfo);
        return Task.CompletedTask;
    }

    public void ClearStore()
    {
        _gameInfos.Clear();
    }
}