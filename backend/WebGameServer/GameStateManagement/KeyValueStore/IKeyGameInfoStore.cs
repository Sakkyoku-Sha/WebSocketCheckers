using WebGameServer.State;

namespace WebGameServer.GameStateManagement.KeyValueStore;

public interface IKeyGameInfoStore
{
    public bool TryGetState(Guid gameId, out GameInfo? state); //Blocking Calls 
    public void SetGameInfo(Guid gameId, GameInfo gameInfo);
    public void RemoveGameInfo(Guid gameId); 
    
    public Task<bool> TryGetStateAsync(Guid gameId, out GameInfo? state); //Non Blocking Calls 
    public Task SetGameInfoAsync(Guid gameId, GameInfo gameInfo);

    public void ClearStore(); 
}