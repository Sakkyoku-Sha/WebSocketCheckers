import { MouseEventHandler, RefObject, useState, useRef, useMemo, JSX, Ref, useEffect } from 'react';
import {CheckersMove} from './page';
import {ForcedMove, GameInfo} from "@/app/WebSocket/Decoding";

enum GameBoardSquare{
    EMPTY = 0,
    PLAYER1PAWN = 1,
    PLAYER1KING = 2,
    PLAYER2PAWN = 3,
    PLAYER2KING = 4    
}

export interface SquareToRender {
    row: number
    col: number
    value: GameBoardSquare
    classNameExtension?: string
}

export interface GameBoardProps {
    moveNumber : number
    currentGame : RefObject<GameInfo | null>
    makeMove : (fromIndex : number, toIndex : number) => void;
}

export default function GameBoard(props: GameBoardProps) {

    const forcedMoves = props.currentGame.current?.forcedMoves ?? [];
    const moveHistory = props.currentGame.current?.history ?? [];
    
    const currRenderedMove = useRef<number>(props.moveNumber);
    
    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<number[]>(initialGameState);
    
    const TryMakeMove = (row: number, col: number) => {

        if (selectedSquare === undefined || selectedSquare === null){ return; }
        if (props.currentGame.current === null) { return; }

        const toIndex = row * 8 + col;
        const fromIndex = selectedSquare.row * 8 + selectedSquare.col;

        props.makeMove(fromIndex, toIndex);
    }
    
    useEffect(() => {
        const movedBoard = PlayMoves(currRenderedMove.current, props.moveNumber, moveHistory, gameState);
        currRenderedMove.current = props.moveNumber;
        setGameState(movedBoard);
    }, [props.moveNumber]);

    const onBoardClick: MouseEventHandler<HTMLDivElement> = (event) => {
        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
        
        const clickedPiece = gameState[row * 8 + col] !== GameBoardSquare.EMPTY;
        if (selectedSquare === null && clickedPiece) {
            setSelectedSquare({row, col });
        } else {
            TryMakeMove(row, col);
            setSelectedSquare(null);
        }
    };
    
    const activeState = props.moveNumber === moveHistory.length-1;
    const squaresToRender = ResolveSquaresToRender(gameState, forcedMoves, activeState, selectedSquare);
    
    let deactivatedClass = activeState ? "" : " deactivated-board";
    return (
        <div className={`game-board${deactivatedClass}`} onClick={onBoardClick}>
          {squaresToRender.map((piece) => renderPiece(piece))}
        </div>
      );
}

function ResolveSquaresToRender(gameBoard : number[], forcedMoves : ForcedMove[], activeState : boolean, selectedSquare : { row: number, col: number } | null) : SquareToRender[] {
    
    const squaresList = [] as SquareToRender[];
    
    for(let pos = 0; pos < gameBoard.length; pos++) {
        
        const pieceValue = gameBoard[pos];
        const currRow = Math.floor(pos / 8);
        const currCol = pos % 8;
        
        if(pieceValue !== GameBoardSquare.EMPTY){
            
            const forcedStart = activeState ? IsForcedStart(pos, forcedMoves) : -1;
            const isSelected = selectedSquare?.row === currRow && selectedSquare?.col === currCol;
            
            squaresList.push({
                row : currRow,
                col : currCol,
                value : pieceValue,
                classNameExtension : determineClassNames(pieceValue, forcedStart, isSelected),
            })
        }
        else{
            const forcedEnd = activeState ? IsForcedEnd(pos, forcedMoves) : -1;
            if(forcedEnd >= 0) {
                squaresList.push({
                    row : currRow,
                    col : currCol,
                    value : GameBoardSquare.EMPTY,
                    classNameExtension : " forced-jump forced-jump-" + forcedEnd
                })
            }
        }
    }
    
    return squaresList;
}

function IsForcedStart(pos : number, forcedMoves : ForcedMove[]) : number  {
    for(let i = 0; i < forcedMoves.length; i++) {
        if(forcedMoves[i].initialPosition === pos) {
            return i;
        }
    }
    return -1;
}

function IsForcedEnd(pos : number, forcedMoves : ForcedMove[]) : number  {
    for(let i = 0; i < forcedMoves.length; i++) {
        if(forcedMoves[i].finalPosition === pos) {
            return i;
        }
    }
    return -1;
}

const renderPiece = (gamePiece : SquareToRender) => {

    const left = (12.5 * gamePiece.col) + "%";
    const top = (12.5 * gamePiece.row) + "%";
    const key = gamePiece.row + ":" + gamePiece.col;
    
    return <div key={key} className={gamePiece.classNameExtension} style={{left:left,top:top}}></div>;
};

const determineClassNames = (square: GameBoardSquare, forcedSquareIndex : number, isSelectedSquare : boolean) : string => {
    
    let classNameExtension = "";
    if(isSelectedSquare) {
        classNameExtension += " selected";
    }
    if(forcedSquareIndex >= 0) {
        classNameExtension += " forced-jump-" + forcedSquareIndex;
    }
    switch (square) {
        case GameBoardSquare.PLAYER1PAWN: 
            classNameExtension +=  " player1-pawn"; 
            break;
        case GameBoardSquare.PLAYER1KING: 
            classNameExtension +=  " player1-king"; 
            break;
        case GameBoardSquare.PLAYER2PAWN: 
            classNameExtension +=  " player2-pawn"; 
            break;
        case GameBoardSquare.PLAYER2KING: 
            classNameExtension +=  " player2-king"; 
            break;
        default: 
            classNameExtension +=  "emptySquare";
    }
    
    return classNameExtension;
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


