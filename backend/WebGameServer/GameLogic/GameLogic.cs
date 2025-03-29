using WebGameServer.State;

namespace WebGameServer.GameLogic;
public static class GameLogic
{
    private static bool IsOnBoard(int x, int y) => x >= 0 && x < GameState.BoardSize && y >= 0 && y < GameState.BoardSize;
    private static bool IsDarkSquare(int x, int y) => (x + y) % 2 == 1;
    
    public static bool TryApplyMove(GameState state, int fromBitIndex, int toBitIndex)
    {
        var (fromX, fromY) = GameState.GetXY(fromBitIndex);
        var (toX, toY) = GameState.GetXY(toBitIndex);
        
        //Move must be on the board and on a dark square 
        if (
            !IsOnBoard(fromX, fromY) || 
            !IsOnBoard(toX, toY) || 
            !IsDarkSquare(fromX, fromY) || 
            !IsDarkSquare(toX, toY))
        {
            return false; 
        }
        
        //Can't move to a space with a piece, (unless it's the same square (as cycles jumps are supported)) 
        var allPieces = state.GetAllPieces();
        if (GameState.IsBitSet(allPieces, toBitIndex) && fromBitIndex != toBitIndex)
        {
            return false; 
        }
        
        //Only Can move the current turn pieces. 
        var playerPieces = state.IsPlayer1Turn ? state.GetPlayer1Pieces() : state.GetPlayer2Pieces();
        if (!GameState.IsBitSet(playerPieces, fromBitIndex))
        {
            return false;
        }
        
        //Determine if we are working with a king or a pawn 
        var playerKings = state.IsPlayer1Turn ? state.Player1Kings : state.Player2Kings;
        var isKing = GameState.IsBitSet(playerKings, fromBitIndex);
        
        //Determine offsets. 
        var dx = toX- fromX;
        var dy = toY - fromY;
        
        //Simple Move Case Valid moves are 
        //todo redo logic so that we don't call this method for single jumps (the majority of moves) 
        var possibleJumps = DeterminePossibleJumpEndPoints(state, playerPieces, playerKings, allPieces);
        if (possibleJumps.Count > 0 && possibleJumps.All(x => x.finalJump != toBitIndex) ||
            (fromBitIndex == toBitIndex && possibleJumps.Count == 0))
        {
            return false; 
        }
        //If it's not a jump then it must be a single movement
        if (possibleJumps.Count == 0 && (Math.Abs(dy) != 1 || Math.Abs(dx) != 1)) 
        {
            return false; 
        }
        //Pawn movement must be forward 
        if (!isKing && (state.IsPlayer1Turn && dy > 0) || !isKing && (!state.IsPlayer1Turn && dy < 0))
        {
            return false;
        }
        
        var shouldPromote = ShouldPromote(toBitIndex, state.IsPlayer1Turn); 
        ulong playerPieceBoard; 
        if (state.IsPlayer1Turn)
        {
            playerPieceBoard = isKing ? state.Player1Kings : state.Player1Pawns;
        }
        else
        {
            playerPieceBoard = isKing ? state.Player2Kings : state.Player2Pawns;
        }

        // Clear the bit and set the new bit on the selected board.
        playerPieceBoard = GameState.ClearBit(playerPieceBoard, fromBitIndex);
        playerPieceBoard = GameState.SetBit(playerPieceBoard, toBitIndex);

        // Update the original state board after the modifications.
        if (state.IsPlayer1Turn)
        {
            if (isKing)
                state.Player1Kings = playerPieceBoard;
            else
                state.Player1Pawns = playerPieceBoard;
        }
        else
        {
            if (isKing)
                state.Player2Kings = playerPieceBoard;
            else
                state.Player2Pawns = playerPieceBoard;
        }
        
        var moveToStore = new CheckersMove()
        {
            FromIndex = (byte)fromBitIndex, //Valid Indexes will cast fine 
            ToIndex = (byte)toBitIndex,
        };
        //Remove opponent pieces 
        if (possibleJumps.Count > 0)
        {
            var opponentKings = state.IsPlayer1Turn ? state.Player2Kings : state.Player1Kings;
            var opponentsPawns = state.IsPlayer1Turn ? state.Player2Pawns : state.Player2Kings;
            
            ulong capturedPiecesBoard = 0ul;
            (int finalJump, List<int> jumpedOver, bool becameKing) jumpedIndexes = possibleJumps.Find(x => x.finalJump == toBitIndex);
            foreach (var jumped in jumpedIndexes.jumpedOver)
            {
                opponentKings = GameState.ClearBit(opponentKings, jumped);
                opponentsPawns = GameState.ClearBit(opponentsPawns, jumped);
                capturedPiecesBoard = GameState.SetBit(capturedPiecesBoard, jumped); 
            }

            if (state.IsPlayer1Turn)
            {
                state.Player2Kings = opponentKings;
                state.Player2Pawns = opponentsPawns;
            }
            else
            {
                state.Player1Kings = opponentKings;
                state.Player1Pawns = opponentsPawns;
            }

            moveToStore.CapturedPieces = capturedPiecesBoard;
            shouldPromote |= jumpedIndexes.becameKing;
        }

        moveToStore.Promoted = shouldPromote;
        //Promotion Logic. 
        if (shouldPromote)
        {
            if (state.IsPlayer1Turn)
            {
                state.Player1Kings = GameState.SetBit(state.Player1Kings, toBitIndex);
                state.Player1Pawns = GameState.ClearBit(state.Player1Pawns, toBitIndex);
            }
            else
            {
                state.Player2Kings = GameState.SetBit(state.Player2Kings, toBitIndex);
                state.Player2Pawns = GameState.ClearBit(state.Player2Pawns, toBitIndex);
            }
        }
        
        //Flip Turns, no partial moves are possible. 
        state.IsPlayer1Turn = !state.IsPlayer1Turn;
        state.AddHistory(moveToStore);
        return true; 
    }
    
    private static List<(int finalJump, List<int> jumpedOver, bool king)> DeterminePossibleJumpEndPoints(GameState state, ulong playerPieces, ulong playerKings, ulong allPieces)
    {
        var opponentPieces = playerPieces ^ allPieces; //xor works due to playerPieces being in allPieces. 
        var possibleJumps = new List<(int finalJump, List<int> jumpedOver, bool king)>();
        
        //todo This could be made more optimized with move history later (just check the most recent moves) 
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