using WebGameServer.GameLogic;

namespace WebGameServer.API;

public class ActiveGameInfo
{
    public Guid GameId { get; set; }
    public string GameName { get; set; }
    public string Player1Name { get; set; }
    public string Player2Name { get; set; }
    public Byte[] GameStateSnapShot { get; set; }
}