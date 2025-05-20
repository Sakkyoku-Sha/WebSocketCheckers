using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;
using WebGameServer.WebSockets.Writers.MessageWriters;

namespace WebGameServerTests;

[TestFixture]
public class MessageWriterTests
{
    [Test]
    public void GameCreatedWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var player1 = new PlayerInfo(Guid.NewGuid(), "Player1", true);
        var writer = new GameCreatedWriter(new GameMetaData(1, player1, PlayerInfo.Empty));
        writer.WriteBytes(ref byteWriter);
        
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));     
    }

    [Test]
    public void GameMetaDataWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);

        var player1 = new PlayerInfo();
        var player2 = new PlayerInfo();

        GameMetaData[] activeGames =
        [
            new(0, player1, player2),
            new(1, player1, player2),
        ];

        var writer = new GameMetaDataWriter(activeGames);
        writer.WriteBytes(ref byteWriter);
        
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));  
    }

    [Test]
    public void InitialGameMessageWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var player1 = new PlayerInfo();
        var player2 = new PlayerInfo();
        GameMetaData[] activeGames =
        [
            new(0, player1, player2),
            new(1, player1, player2),
        ];
        var gameInfo = new GameInfo(1);
        gameInfo.Reset();
        gameInfo.GameState.CurrentForcedJumps = new JumpPath[]
        {
            new(1, false, 3, 4),
            new(5,false,7,8),
        };
        
        var writer = new InitialMessageWriter(activeGames, gameInfo);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void GameInfoWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var gameInfo = new GameInfo(1);
        gameInfo.Reset();
        gameInfo.GameState.CurrentForcedJumps =
        [
            new JumpPath(1, false, 3, 4)
        ];

        var writer = new GameInfoWriter(gameInfo);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void JoinGameResultWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);

        var gameInfo = new GameInfo(1);
        gameInfo.Reset();
        var writer = new JoinGameResultWriter(true, gameInfo);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }

    [Test]
    public void NewMoveWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        var checkersMove = new CheckersMove();
        var jumpPaths = new JumpPath[]
        {
            new(1, false, 3, 4),
            new(5,false,7,8),
        };
        var writer = new NewMoveWriter(checkersMove, jumpPaths);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void OtherPlayerJoinedWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var playerInfo = new PlayerInfo();
        var writer = new PlayerJoinedWriter(playerInfo);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void SessionStartWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var sessionId = Guid.NewGuid();
        var writer = new SessionStartWriter(sessionId);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void GameStatusWriter()
    {
        var buffer = new byte[1024];
        var byteWriter = new ByteWriter(buffer);
        
        var writer = new GameStatusWriter(GameStatus.Player1Win);
        
        writer.WriteBytes(ref byteWriter);
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
}