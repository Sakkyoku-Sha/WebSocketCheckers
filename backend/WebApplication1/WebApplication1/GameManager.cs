namespace WebApplication1;

public interface IGameLogic
{
    //Execute a move in Checkers from
    bool TryMove((int x, int y) from, (int x, int y) to, out CheckersGameState state);
    
}


public class GameApi :
{
    
}