using System.Numerics;
using System.Runtime.CompilerServices;

namespace WebGameServer.GameLogic;

public static class GameLogic
{
    private static bool IsOnBoard(int x, int y) => x >= 0 && x < GameState.BoardSize && y >= 0 && y < GameState.BoardSize;
    private static bool IsDarkSquare(int x, int y) => (x + y) % 2 == 1;
    
    public static bool TryApplyMove(ref GameState state, CheckersMove move)
    {
        var (fromX, fromY, toX, toY) = move;
        
        //Move must be on the board and on a dark square 
        if (
            !IsOnBoard(fromX, fromY) || 
            !IsOnBoard(toX, toY) || 
            !IsDarkSquare(fromX, fromY) || 
            !IsDarkSquare(toX, toY))
        {
            return false; 
        }

        var fromBit = GameState.GetBitIndex(fromX, fromY);
        var toBit = GameState.GetBitIndex(toX, toY);

        //Can't move to a space with a piece, (unless it's the same square (as cycles jumps are supported)) 
        var allPieces = state.GetAllPieces();
        if (GameState.IsBitSet(allPieces, toBit) && fromBit != toBit)
        {
            return false; 
        }
        
        //Only Can move the current turn pieces. 
        var playerPieces = state.IsPlayer1Turn ? state.GetPlayer1Pieces() : state.GetPlayer2Pieces();
        if (!GameState.IsBitSet(playerPieces, fromBit))
        {
            return false;
        }
        
        //Determine if we are working with a king or a pawn 
        var playerKings = state.IsPlayer1Turn ? state.Player1Kings : state.Player2Kings;
        var isKing = GameState.IsBitSet(playerKings, fromBit);
        
        //Determine offsets. 
        var dx = toX- fromX;
        var dy = toY - fromY;
        
        //Simple Move Case Valid moves are 
        //todo redo logic so that we don't call this method for single jumps (the majority of moves) 
        var possibleJumps = DeterminePossibleJumpEndPoints(ref state, playerPieces, playerKings, allPieces);
        if (possibleJumps.Count > 0 && possibleJumps.All(x => x.finalJump != toBit) ||
            (fromBit == toBit && possibleJumps.Count == 0))
        {
            return false; 
        }
        //If it's not a jump then it must be a single movement
        if (possibleJumps.Count == 0 && (Math.Abs(dy) != 1 || Math.Abs(dx) != 1)) 
        {
            return false; 
        }
        //Pawn movement must be forward 
        if (!isKing && (state.IsPlayer1Turn && dy > 0) || (!state.IsPlayer1Turn && dy < 0))
        {
            return false;
        }
        
        //Move is now assume to be valid
        //Remove From Location and Set Current Position.  
        ref var playerPieceBoard = ref state.Player1Pawns; // Default assignment to avoid uninitialized ref
        //Determine if you should promote. 
        var shouldPromote = ShouldPromote(toBit, state.IsPlayer1Turn); 
        
        if (state.IsPlayer1Turn)
        {
            if (isKing)
                playerPieceBoard = ref state.Player1Kings;
            else
                playerPieceBoard = ref state.Player1Pawns;
        }
        else
        {
            if (isKing)
                playerPieceBoard = ref state.Player2Kings;
            else
                playerPieceBoard = ref state.Player2Pawns;
        }
        //MAKE SURE TO CLEAR AND THEN SET 
        playerPieceBoard = GameState.ClearBit(playerPieceBoard, fromBit);
        playerPieceBoard = GameState.SetBit(playerPieceBoard, toBit);
        
        //Remove opponent pieces 
        if (possibleJumps.Count > 0)
        {
            ref var opponentKings = ref state.Player2Kings;
            ref var opponentsPawns = ref state.Player2Pawns;
            if (!state.IsPlayer1Turn)
            {
                opponentKings = ref state.Player1Kings;
                opponentsPawns = ref state.Player1Pawns;
            }
            
            (int finalJump, List<int> jumpedOver, bool becameKing) jumpedIndexes = possibleJumps.Find(x => x.finalJump == toBit);
            foreach (var jumped in jumpedIndexes.jumpedOver)
            {
                GameState.ClearBit(opponentKings, jumped);
                GameState.ClearBit(opponentsPawns, jumped);
            }

            shouldPromote |= jumpedIndexes.becameKing;
        }

        //Promotion Logic. 
        if (shouldPromote)
        {
            if (state.IsPlayer1Turn)
            {
                state.Player1Kings = GameState.SetBit(state.Player1Kings, toBit);
                state.Player1Pawns = GameState.ClearBit(state.Player1Pawns, toBit);
            }
            else
            {
                state.Player2Kings = GameState.SetBit(state.Player2Kings, toBit);
                state.Player2Pawns = GameState.ClearBit(state.Player2Pawns, toBit);
            }
        }
        
        //Flip Turns, no partial moves are possible. 
        state.IsPlayer1Turn = !state.IsPlayer1Turn;
        
        return true; 
    }
    
    private static List<(int finalJump, List<int> jumpedOver, bool king)> DeterminePossibleJumpEndPoints(ref GameState state, ulong playerPieces, ulong playerKings, ulong allPieces)
    {
        var opponentPieces = playerPieces ^ allPieces; //xor works due to playerPieces being in allPieces. 
        var possibleJumps = new List<(int finalJump, List<int> jumpedOver, bool king)>();
        
        //This could be made more optimized with move history later (just check the most recent moves) 
        for (var i = 0; i < 64; i++)
        {
            if (!GameState.IsBitSet(playerPieces, i)) continue;
            
            var isKing = GameState.IsBitSet(playerKings, i);
            var possiblePositions = DeterminePossibleDirectionsToJump(i, allPieces, opponentPieces, state.IsPlayer1Turn, isKing, new List<int>(6));
            possibleJumps.AddRange(possiblePositions);
        }
        
        return possibleJumps;
    }
    
    private const int ForwardLeft = -9;
    private const int JumpForwardLeft = ForwardLeft + ForwardLeft;
    
    private const int ForwardRight = -7;
    private const int JumpForwardRight = ForwardRight + ForwardRight;
    
    private const int BackLeft = +7;
    private const int JumpBackLeft = BackLeft + BackLeft;
    
    private const int BackRight = +9;
    private const int JumpBackRight = BackRight + BackRight;

    private static readonly (int jumpOver, int jumpTo, Func<bool, bool, bool, bool, bool> canJumpFunc)[] DirectionChecks =
    [
        (ForwardLeft, JumpForwardLeft, (_, canJumpLeft, canJumpUp, _) => canJumpUp && canJumpLeft),
        (ForwardRight, JumpForwardRight, (canJumpRight, _, canJumpUp, _) => canJumpUp && canJumpRight),
        (BackLeft, JumpBackLeft, (_, canJumpLeft, _, canJumpDown) => canJumpDown && canJumpLeft),
        (BackRight, JumpBackRight, (canJumpRight, _, _, canJumpDown) => canJumpDown && canJumpRight)
    ];  
    
    //todo rewrite this to use a stack frame and itterative instead of the current solution. 
    private static IEnumerable<(int, List<int> jumpedOver, bool isKing)> DeterminePossibleDirectionsToJump(int index, ulong allPieces, ulong opponentPieces,
        bool player1Turn, bool isKing, List<int> jumpedOver)
    {
        var canJumpRight = index % 8 < 6;
        var canJumpLeft = index % 8 > 1;
        var canJumpDown = index < 48 && (isKing || !player1Turn); 
        var canJumpUp = index > 15 && (isKing || player1Turn);

        foreach (var direction in DirectionChecks)
        {
            var jumpOver = index + direction.jumpOver;
            var jumpTo = index + direction.jumpTo;
            
            if (!direction.canJumpFunc(canJumpRight, canJumpLeft, canJumpUp, canJumpDown))
            {
                continue;
            }
            if (!GameState.IsBitSet(opponentPieces, jumpOver) ||
                GameState.IsBitSet(allPieces, jumpTo))
            {
                continue;
            }

            //Using Backtracking to avoid cycles, e.g jumping to a position in a circle (which is possible!) 
            opponentPieces = GameState.ClearBit(opponentPieces, jumpOver);
            allPieces = GameState.ClearBit(allPieces, jumpOver);
            allPieces = GameState.ClearBit(allPieces, index);
            
            jumpedOver.Add(jumpOver);
            
            //Recurse
            isKing = isKing || ShouldPromote(jumpTo, player1Turn);
            var jumps = DeterminePossibleDirectionsToJump(jumpTo, allPieces, opponentPieces, player1Turn, isKing, jumpedOver);
            var jumped = false;
            
            foreach (var jump in jumps)
            {
                jumped = true;
                yield return jump;
            }
            if (jumped == false) //We jumped and no other jumps are possible
            {
                yield return (jumpTo, [..jumpedOver], isKing);
            }
            
            //Undo Jump in this closure 
            jumpedOver.RemoveAt(jumpedOver.Count - 1);
            opponentPieces = GameState.SetBit(opponentPieces, jumpOver);
            allPieces = GameState.SetBit(allPieces, jumpOver);
            allPieces = GameState.SetBit(allPieces, index);
        }
    }
    
    private static bool ShouldPromote(int jumpIndex, bool player1Turn)
    {
        return player1Turn ? jumpIndex < 8 : jumpIndex > 55;
    }
}