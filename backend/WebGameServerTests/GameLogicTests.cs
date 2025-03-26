using WebGameServer.GameLogic;

namespace WebGameServerTests;

public class GameLogicTests
{
    // Helper function to create CheckersBitboardState from string array representation (same as before)
    // Note: The boardString is assumed to be an 8-element array where index 0 is the top row.
    public static GameState CreateBoardFromStringArray(string[] boardString)
    {
        var state = new GameState();
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                switch (boardString[y][x])
                {
                    case 'p':
                        state.Player1Pawns = GameState.SetBit(state.Player1Pawns, GameState.GetBitIndex(x, y));
                        break;
                    case 'k':
                        state.Player1Kings = GameState.SetBit(state.Player1Kings, GameState.GetBitIndex(x, y));
                        break;
                    case 'e':
                        state.Player2Pawns = GameState.SetBit(state.Player2Pawns, GameState.GetBitIndex(x, y));
                        break;
                    case 'K':
                        state.Player2Kings = GameState.SetBit(state.Player2Kings, GameState.GetBitIndex(x, y));
                        break;
                    case '.':
                    default:
                        break; // Empty square
                }
            }
        }
        return state;
    }

    [Test]
    public void ValidPawnMoves()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            "........", // row 2: 
            "........", // row 3: 
            "........", // row 4
            "..p.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((1, 4), true), // valid move
            ((3, 4), true), // valid move
            
            ((0, 0), false), // invalid move
            ((4, 3), false), // invalid move
            ((2, 4), false), // invalid move
            ((1, 6), false), // invalid move
            ((3, 6), false), // invalid move
        }, true);
    }
    
    [Test]
    public void ValidPlayer2PawnMoves()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            "........", // row 2: 
            "........", // row 3: 
            "........", // row 4
            "..e.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((1, 6), true), // invalid move
            ((3, 6), true), // invalid move
            ((1, 4), false), // valid move
            ((3, 4), false), // valid move`
            ((0, 0), false), // invalid move
            ((4, 3), false), // invalid move
            ((2, 4), false), // invalid move
            
        }, false);
    }
    
    [Test]
    public void SimpleValidKingMoves()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            "........", // row 2: 
            "........", // row 3: 
            "........", // row 4
            "..k.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((1, 4), true), // valid move
            ((3, 4), true), // valid move
            ((1, 6), true), 
            ((3, 6), true), 
            
            ((0, 0), false), 
            ((4, 3), false), 
            ((2, 4), false)
        }, true);
    }
    
    [Test]
    public void SimpleValidPawnJumpMoves()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "..k.K...", // row 1
            "...p....", // row 2: 
            "........", // row 3: 
            ".p.e....", // row 4
            "..p.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        var a = GameState.GetBitIndex(2, 5);
        var b = GameState.GetBitIndex(4, 3);
        var x = Math.Abs(a - b) / 2; 
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((4, 3), true), // Jump over enemy Pawn 
            ((0, 3), false), // Jump over own Pawn 
        }, true);
        
        
        //Jumping Kings 
        ExecuteAndVerify(state, (3, 2), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((5, 0), true), // Jump over enemy King 
            ((1, 0), false), // Jump over own King 
        }, true);
    }
    
    [Test]
    public void SimpleValidKingJumpMoves()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "..k.K...", // row 1
            "...k....", // row 2: 
            "........", // row 3: 
            ".p.e....", // row 4
            "..k.....", // row 5
            ".K.e....", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((4, 3), true), // Jump over enemy Pawn 
            ((0, 4), false), // Jump over own Pawn 
            ((0, 7), true), // Jump back over Pawn 
            ((4, 7), true), // Jump back over King 
        }, true);
        
        //Jumping Kings 
        ExecuteAndVerify(state, (3, 2), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((5, 0), true), // Jump over enemy King 
            ((1, 0), false), // Jump over own King 
        }, true);
    }
    
    [Test]
    public void ForcedJumpTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            "........", // row 2: 
            "........", // row 3: 
            "...e....", // row 4
            "..p.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((4, 3), true), // Jump over enemy Pawn 
            ((1, 4), false), // Must jump over pawn so this is not valid 
        }, true);
    }
    
    [Test]
    public void DoubleForcedJumpTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            ".....e..", // row 2: 
            "........", // row 3: 
            "...e....", // row 4
            "..p.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((6, 1),  true), //Jump over both enemy pawns
            ((4, 3), false), //Must Jump to Final Jump Point 
            ((1, 4), false), //  
        }, true);
    }
    
    [Test]
    public void JumpsWithPromotionForcedJumpTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "....e...", // row 1
            "........", // row 2: 
            "..e.....", // row 3: 
            ".p......", // row 4
            "........", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (1, 4), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((2, 3),  false), //Jump over both enemy pawns
            ((3, 2), false), //Must Jump to Final Jump Point 
            ((4, 1), false), //  
            ((5, 0), true), 
        }, true);
    }
    [Test]
    public void ComplexJumpTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            ".e.e....", // row 2: 
            "........", // row 3: 
            ".e.e....", // row 4
            "..k.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        ExecuteAndVerify(state, (2, 5), new ((byte x, byte y) to, bool expectedSuccess)[] {
            ((2, 5),  true), //Player is forced to go all the way back to their starting position 
        }, true);
    }
    
    [Test]
    public void ComplexJumpGameStateTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "........", // row 1
            ".e.e....", // row 2: 
            "........", // row 3: 
            ".e.e....", // row 4
            "..k.....", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        var move = new CheckersMove(2, 5, 2, 5); 
        var isValidMove = GameLogic.TryApplyMove(ref state, move);
        
        Assert.IsTrue(isValidMove);
        Assert.IsTrue(GameState.IsBitSet(state.Player1Kings, GameState.GetBitIndex(2,5))); //King still exists
        
        Assert.IsFalse(GameState.IsBitSet(state.Player1Pawns, GameState.GetBitIndex(1,4))); //Removed all jumped pieces 
        Assert.IsFalse(GameState.IsBitSet(state.Player2Kings, GameState.GetBitIndex(3,4)));
        Assert.IsFalse(GameState.IsBitSet(state.Player2Kings, GameState.GetBitIndex(1,2)));
        Assert.IsFalse(GameState.IsBitSet(state.Player2Kings, GameState.GetBitIndex(3,2)));
    }
    
    [Test]
    public void PromotionMidJumpTest()
    {
        var boardString = new string[]
        {
            "........", // row 0
            "..e.e...", // row 1
            ".p......", // row 2: 
            "........", // row 3: 
            "........", // row 4
            "........", // row 5
            "........", // row 6
            "........", // row 7
        };
        var state = CreateBoardFromStringArray(boardString);
        
        //Jumping Pawns 
        var move = new CheckersMove(1, 2, 5, 2); 
        var isValidMove = GameLogic.TryApplyMove(ref state, move);
        
        Assert.IsTrue(isValidMove);
        Assert.IsTrue(GameState.IsBitSet(state.Player1Kings, GameState.GetBitIndex(5,2))); //was promoted to king and jumped
        
        Assert.IsFalse(GameState.IsBitSet(state.GetPlayer1Pieces(), GameState.GetBitIndex(1,2)));//removed original piece 
        
        Assert.IsFalse(GameState.IsBitSet(state.Player1Pawns, GameState.GetBitIndex(2,1))); //Removed all jumped pieces 
        Assert.IsFalse(GameState.IsBitSet(state.Player1Pawns, GameState.GetBitIndex(4,1)));
    }

    private void ExecuteAndVerify(GameState state, (byte x, byte y) start,
        ((byte x, byte y) to, bool expectedSuccess)[] moves, bool? forceTurnAs)
    {
        foreach (var curr in moves)
        {
            TestMoveValidity(state, new CheckersMove(start.x, start.y, curr.to.x, curr.to.y), curr.expectedSuccess, forceTurnAs);
        }
    }
    private void TestMoveValidity(GameState state, CheckersMove move, bool expectedSuccess, bool? forceTurnAs)
    {
        if (forceTurnAs.HasValue)
        {
            state.IsPlayer1Turn = forceTurnAs.Value;
        }
        var isValidMove = GameLogic.TryApplyMove(ref state, move);
        
        Assert.AreEqual(expectedSuccess, isValidMove, $"Move validity check failed for move {move.FromX},{move.FromY} -> {move.ToX},{move.ToY}");
    }
}