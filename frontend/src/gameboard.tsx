import React, { MouseEventHandler, RefObject, useState, useRef, useEffect } from 'react';
import {FailedMoveMessage, ForcedMove, GameInfo} from "@/WebSocket/Decoding";
import {CheckersMove} from "@/WebSocket/ByteReader";
import WebSocketEvents from "@/WebSocket/WebSocketEvents";

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


const dragThresholdSquared = 64; // Minimum pixels 8 to drag before considering it a drag action

export default function GameBoard(props: GameBoardProps) {

    const forcedMoves = props.currentGame.current?.forcedMoves ?? [];
    const moveHistory = props.currentGame.current?.history ?? [];
    
    const currRenderedMove = useRef<number>(props.moveNumber);
    const mouseDownPosition = useRef<{ x: number, y: number } | null>(null);
    const draggingPiece = useRef<{ row: number, col: number } | null>(null);
    
    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<number[]>(initialGameState);

    const pieceRefs = useRef<Map<number, React.RefObject<HTMLDivElement | null>>>(new Map());
    
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

    const onFailedMove = (failedMove : FailedMoveMessage) => {
        
        //Rollback to the game state before the failed move. 
        const refIndex = failedMove.fromXy; 
        const ref = pieceRefs.current.get(failedMove.fromXy);
      
        const col = refIndex % 8;
        const row = Math.floor(refIndex / 8);
        
        if(ref !== undefined && ref.current !== null) {
            const left = (12.5 * col) + "%";
            const top = (12.5 * row) + "%";
            
            ref.current.style.left = left;
            ref.current.style.top = top;
        }
    }
    
    useEffect(() => {
        WebSocketEvents.failedMoveEvent.subscribe(onFailedMove);
        
        return () => {
            WebSocketEvents.failedMoveEvent.unsubscribe(onFailedMove);
        }
    }, []);
    
    const onBoardMouseDown: MouseEventHandler<HTMLDivElement> = (event) => {
        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
        
        const isPlayerPiece = true;
        if(!isPlayerPiece) { return; }
        
        setSelectedSquare({row, col });
        mouseDownPosition.current = { x: event.clientX, y: event.clientY };
    }
    
    const onBoardMouseUp: MouseEventHandler<HTMLDivElement> = (event) => {
        
        mouseDownPosition.current = null;
     

        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
        
        if(selectedSquare !== null && (selectedSquare.row !== row || selectedSquare.col !== col)) {
            TryMakeMove(row, col);
            setSelectedSquare(null);
            draggingPiece.current = null;
        }
        else if(draggingPiece.current !== null) {
            //Reset position of the piece if it was not moved.
            //Might need to put simple valid move check here later? not sure how to "roll back bad moves" without flicker
            
            const refPos = draggingPiece.current.row * 8 + draggingPiece.current.col;
            const ref = pieceRefs.current.get(refPos);
            
            if(ref !== undefined && ref.current !== null) {
                const left = (12.5 * draggingPiece.current.col) + "%";
                const top = (12.5 * draggingPiece.current.row) + "%";
                
                ref.current.style.left = `${left}%`;
                ref.current.style.top = `${top}px`;
            }
        }
        
    }
    
    const onBoardMouseMove: MouseEventHandler<HTMLDivElement> = (event) => {
        
        if(!selectedSquare || mouseDownPosition.current === null) { return; }
        
        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
        
        if(draggingPiece.current != null){

            const squareWidth = rect.width / 8;
            const squareHeight = rect.height / 8;
            
            const refPos = draggingPiece.current.row * 8 + draggingPiece.current.col;
            const ref = pieceRefs.current.get(refPos);
            
            if(ref !== undefined && ref.current !== null) {
                ref.current.style.backgroundColor = "";
                ref.current.style.border = "";
                ref.current.style.position = "absolute";
                ref.current.style.top = y - (squareHeight / 2) + "px";
                ref.current.style.left = x - (squareWidth / 2) + "px";
            }
            
            return; 
        }
        
        const mouseDownXDelta = Math.abs(mouseDownPosition.current?.x ?? 0 - x);
        const mouseDownYDelta = Math.abs(mouseDownPosition.current?.y ?? 0 - y);

        const mouseDownXDeltaSquared = mouseDownXDelta ** 2;
        const mouseDownYDeltaSquared = mouseDownYDelta ** 2;
        
        if(mouseDownXDeltaSquared + mouseDownYDeltaSquared < dragThresholdSquared) {
            return; // Not enough movement to consider it a drag
        }
       
        draggingPiece.current = {row : row, col : col };
    }
    
    const activeState = props.moveNumber === moveHistory.length-1;
    const squaresToRender = ResolveSquaresToRender(gameState, forcedMoves, activeState, selectedSquare);
    
    let deactivatedClass = activeState ? "" : " deactivated-board";
    return (
        <div 
            className={`game-board${deactivatedClass}`}
            onMouseDown={onBoardMouseDown} 
            onMouseMove={onBoardMouseMove}
            onMouseUp={onBoardMouseUp}>
            
          {squaresToRender.map((squareToRender, i) => {

              const left = (12.5 * squareToRender.col) + "%";
              const top = (12.5 * squareToRender.row) + "%";
              const key = squareToRender.row + ":" + squareToRender.col;
              
              const refPos = squareToRender.row * 8 + squareToRender.col;
              let ref = pieceRefs.current.get(refPos);
              if(ref === undefined) {
                ref = React.createRef<HTMLDivElement>();
                pieceRefs.current.set(refPos, ref);
              }
              
              return <div ref={ref} key={key} className={squareToRender.classNameExtension} style={{left:left,top:top}}></div>;
          })}
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

function IsPlayerPiece(pos : number, gameBoard : number[], isPlayer1 : boolean) : boolean {
    if(isPlayer1) {
        return gameBoard[pos] === GameBoardSquare.PLAYER1PAWN || gameBoard[pos] === GameBoardSquare.PLAYER1KING;
    }
    else{
        return gameBoard[pos] === GameBoardSquare.PLAYER2PAWN || gameBoard[pos] === GameBoardSquare.PLAYER2KING;
    }
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

const isBitSet = (bitField: bigint, bitIndex: number): boolean => {
    return (bitField & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0);
}

const ApplyMove = (move : CheckersMove, gameBoard : number[], moveNumber : number) : void => {
    
    const originalPiece = gameBoard[move.fromIndex];
    gameBoard[move.fromIndex] = GameBoardSquare.EMPTY;
    
    let newPiece: GameBoardSquare;
    if (move.promoted) {
        newPiece = moveNumber % 2 === 0 ? GameBoardSquare.PLAYER1KING : GameBoardSquare.PLAYER2KING;
    } else {
       
        if (originalPiece === GameBoardSquare.PLAYER1KING || originalPiece === GameBoardSquare.PLAYER2KING) {
            newPiece = originalPiece;
        } else {
            newPiece = moveNumber % 2 === 0 ? GameBoardSquare.PLAYER1PAWN : GameBoardSquare.PLAYER2PAWN;
        }
    }
    gameBoard[move.toIndex] = newPiece;
    
    // Remove captured pieces
    const capturedPieces = move.capturedPawns | move.capturedKings;
    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if (isBitSet(capturedPieces, bitIndex)) {
            gameBoard[bitIndex] = GameBoardSquare.EMPTY;
        }
    }
}

const UndoMove = (move : CheckersMove, gameBoard : number[], moveNumber : number) : void => {
   
    const movedPiece = gameBoard[move.toIndex];
    gameBoard[move.toIndex] = GameBoardSquare.EMPTY;
    
    const isPlayer1Move = moveNumber % 2 === 0;
    let restoredPiece: GameBoardSquare;
    
    if (move.promoted) {
        
        restoredPiece = isPlayer1Move ? GameBoardSquare.PLAYER1PAWN : GameBoardSquare.PLAYER2PAWN;
    } else {
        
        if (movedPiece === GameBoardSquare.PLAYER1KING || movedPiece === GameBoardSquare.PLAYER2KING) {
            restoredPiece = movedPiece;
        } else {
            restoredPiece = isPlayer1Move ? GameBoardSquare.PLAYER1PAWN : GameBoardSquare.PLAYER2PAWN;
        }
    }
    gameBoard[move.fromIndex] = restoredPiece;

    // Restore captured pieces
    const toRecoverPawn = isPlayer1Move ? GameBoardSquare.PLAYER2PAWN : GameBoardSquare.PLAYER1PAWN;
    const toRecoverKing = isPlayer1Move ? GameBoardSquare.PLAYER2KING : GameBoardSquare.PLAYER1KING;
    const capturedPawns = move.capturedPawns;
    const capturedKings = move.capturedKings; 
    
    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if (isBitSet(capturedPawns, bitIndex)) {
            gameBoard[bitIndex] = toRecoverPawn;
        }
        else if(isBitSet(capturedKings, bitIndex)) {
            gameBoard[bitIndex] = toRecoverKing;
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
        UndoMove(move, nextState, currentIndex);
        currentIndex--; 
    }
    while(currentIndex < moves.length && currentIndex < targetIndex){
        currentIndex++;
        move = moves[currentIndex];
        ApplyMove(move, nextState, currentIndex);
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

