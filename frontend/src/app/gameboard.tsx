"use client"
import { MouseEventHandler, RefObject, useState, useRef, useMemo, JSX, Ref, useEffect } from 'react';
import {CheckersMove} from './page';
import { fetchTryMakeMove } from './Fetch/Fetch';

enum GameBoardSquare{
    EMPTY = 0,
    PLAYER1PAWN = 1,
    PLAYER1KING = 2,
    PLAYER2PAWN = 3,
    PLAYER2KING = 4    
}

export interface GamePiece{
    row: number
    col: number
    value: GameBoardSquare
}

export interface GameBoardProps {
    moveHistoryRef : RefObject<CheckersMove[]>
    moveNumber : number
    gameIdRef : RefObject<string | null>
}

export default function GameBoard(props: GameBoardProps) {

    const moveHistory = props.moveHistoryRef;
    const currRenderedMove = useRef<number>(props.moveNumber);
    
    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<number[]>(initialGameState);
    
    const TryMakeMove = (row: number, col: number) => {

        if (selectedSquare === undefined || selectedSquare === null){ return; }
        if (props.gameIdRef.current === null) { return; }

        const toIndex = row * 8 + col;
        const fromIndex = selectedSquare.row * 8 + selectedSquare.col;

        fetchTryMakeMove(props.gameIdRef.current, fromIndex, toIndex);
    }
    
    useEffect(() => {
        const movedBoard = PlayMoves(currRenderedMove.current, props.moveNumber, moveHistory.current, gameState);
        currRenderedMove.current = props.moveNumber;
        setGameState(movedBoard);
    }, [props.moveNumber]);

    const onBoardClick: MouseEventHandler<HTMLDivElement> = (event) => {
        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
        
        if (selectedSquare === null) {
            setSelectedSquare({row, col });
        } else {
            TryMakeMove(row, col);
            setSelectedSquare(null);
        }
    };
    
    const nonEmptySquares : GamePiece[] = useMemo(() => {
        const squaresList = new Array<GamePiece>();    
        for (let row = 0; row < 8; row++) {
            for (let col = 0; col < 8; col++) {
                const squareValue = gameState[row * 8 + col];
                if (squareValue !== GameBoardSquare.EMPTY) {
                    squaresList.push({row, col, value: squareValue});
                }
            }
        }
        return squaresList;
    }, [gameState]);

    let deactivated = props.moveNumber === props.moveHistoryRef.current.length-1 ? "" : " deactivated-board";
    return (
        <div className={`game-board${deactivated}`} onClick={onBoardClick}>
          {nonEmptySquares.map((piece) => renderPiece(piece, selectedSquare))}
        </div>
      );
}

const renderPiece = (gamePiece : GamePiece, selectedSquare : { row: number, col: number } | null) => {

    const left = (12.5 * gamePiece.col) + "%";
    const top = (12.5 * gamePiece.row) + "%";
    const key = gamePiece.row + ":" + gamePiece.col;
    const isSelected = selectedSquare !== null && selectedSquare.row === gamePiece.row && selectedSquare.col === gamePiece.col;
    
    return <div key={key} className={determineClassName(gamePiece.value) + (isSelected ? " selected" : "")} style={{left:left,top:top}}></div>;
};

const determineClassName = (square: GameBoardSquare) : string => {
    switch (square) {
        case GameBoardSquare.PLAYER1PAWN: return "player1-pawn";
        case GameBoardSquare.PLAYER1KING: return "player1-king";
        case GameBoardSquare.PLAYER2PAWN: return "player2-pawn";
        case GameBoardSquare.PLAYER2KING: return "player2-king";
        default: return "emptySquare";
    }
}

const ApplyMove = (move : CheckersMove, gameBoard : number[]) : void => {
    gameBoard[move.toIndex] = gameBoard[move.fromIndex];
    gameBoard[move.fromIndex] = GameBoardSquare.EMPTY;

    const capturedSquares = move.capturedPieces; 

    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if ((capturedSquares & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0)) {
            gameBoard[bitIndex] = GameBoardSquare.EMPTY;
        }
    }
}
const UndoMove = (move : CheckersMove, isPlayer1Turn : boolean, gameBoard : number[]) : void => {
    gameBoard[move.fromIndex] = gameBoard[move.toIndex];
    gameBoard[move.toIndex] = GameBoardSquare.EMPTY;

    //Todo send captured kings back to front end to know if we should recover a king or pawn.
    const toRecover = isPlayer1Turn ? GameBoardSquare.PLAYER2PAWN : GameBoardSquare.PLAYER1PAWN;
    const capturedSquares = move.capturedPieces; 

    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if ((capturedSquares & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0)) {
            gameBoard[bitIndex] = toRecover;
        }
    }
}

const PlayMoves = (currentIndex : number, targetIndex : number, moves : CheckersMove[], currentState : number[]) : number[] => {

    if (currentIndex === targetIndex) {
        return currentState;
    }

    const nextState = [...currentState];
    let move : CheckersMove; 

    while(currentIndex >= 0 && currentIndex > targetIndex){
        move = moves[currentIndex];
        UndoMove(move, currentIndex%2 == 0, nextState);
        currentIndex--; 
    }
    while(currentIndex < moves.length && currentIndex < targetIndex){
        currentIndex++;
        move = moves[currentIndex];
        ApplyMove(move, nextState);
    }
    
    return nextState;
}

// Temp value untill we actually get the server to send game state with the init on websocket connect. 
const initialGameState = ([
    // Player 2 pieces on the top 3 rows (3)
    0, 3, 0, 3, 0, 3, 0, 3,  // Row 0
    3, 0, 3, 0, 3, 0, 3, 0,  // Row 1
    0, 3, 0, 3, 0, 3, 0, 3,  // Row 2
  
    // Empty middle rows (0)
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 3
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 4
  
    // Player 1 pieces on the bottom 3 rows (1)
    1, 0, 1, 0, 1, 0, 1, 0,  // Row 5
    0, 1, 0, 1, 0, 1, 0, 1,  // Row 6
    1, 0, 1, 0, 1, 0, 1, 0,  // Row 7
]);


