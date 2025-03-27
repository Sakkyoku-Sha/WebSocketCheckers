"use client"
import { HtmlContext } from 'next/dist/server/route-modules/pages/vendored/contexts/entrypoints';
import { unescape } from 'querystring';
import { MouseEventHandler, useEffect, useState, useRef, useMemo, JSX } from 'react';

enum GameBoardSquare{
    EMPTY = 0,
    PLAYER1PAWN = 1,
    PLAYER1KING = 2,
    PLAYER2PAWN = 3,
    PLAYER2KING = 4    
}
export interface Move {
    FromIndex: number
    ToIndex :number
}

export interface CheckersMove extends Move{
    promoted: boolean,
    captured: bigint //Bitboard representation
}

export interface GamePiece{
    row: number
    col: number
    value: GameBoardSquare
}

export default function GameBoard() {

    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<number[]>(initialGameState);
    
    const moveHistory = useRef<CheckersMove[]>([]); 
    const currMove = useRef<number>(-1);

    const IsPlayer1Turn = () => {
        return moveHistory.current.length % 2 === 0;
    }

    const TryMakeMove = (row: number, col: number) => {

        if (selectedSquare === undefined || selectedSquare === null){ return; }
        
        const move: Move = {
            FromIndex: selectedSquare.row * 8 + selectedSquare.col,
            ToIndex: row * 8 + col
        };

        const endPoint = 'http://localhost:5050/TryMakeMove';
        fetch(endPoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(move)
        })
    }
    
    const PlayBoardTo = (targetMove : number) => {

        let curr = currMove.current;
        let moves = moveHistory.current;

        if (curr === targetMove) {
            return;
        }

        let move : CheckersMove;
        while(curr >= 0 && curr > targetMove){
            curr--; 
            move = moves[curr];
            UndoMove(move, curr%2 == 0, gameState);
        }
        while(curr < moves.length && curr < targetMove){
            curr++;
            move = moves[curr];
            ApplyMove(move, gameState);
        }
        setGameState(() => {return [...gameState]})
        currMove.current = targetMove; 
    }

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
    
    const moveByteSize = 11;  //FromIndex = 1, ToIndex = 1, Promoted = 1, Captured = 8
    const HandleWebSocketData = (byteData : Uint8Array) => {
        
        const moveCount = byteData.length / moveByteSize; 
           
        for(let i = 0; i < moveCount; i++){

            let bitboard =  BigInt(0);
            for (let j = 0; j < 8; j++) {
                bitboard += BigInt(byteData[(i * moveByteSize + 3) + j]) << BigInt(8 * j);
            }

            const move : CheckersMove = {
                FromIndex: byteData[i * moveByteSize],
                ToIndex: byteData[i * moveByteSize + 1],
                promoted : byteData[i * moveByteSize + 2] === 1,
                captured : bitboard
            }

            moveHistory.current.push(move);
        }
        if(currMove.current !== moveHistory.current.length - 1){
            PlayBoardTo(moveHistory.current.length-1);
        } 
    }

    const wsRef = useRef<WebSocket | null>(null);
    useEffect(() => {

        if (!wsRef.current) {
            wsRef.current = new WebSocket("ws://localhost:5050/ws");
            wsRef.current.binaryType = "arraybuffer";

            wsRef.current.onopen = () => {
                console.log("Connected to server");
            };
            wsRef.current.onclose = () => console.log("Disconnected from server");
            wsRef.current.onerror = (error) => console.error("WebSocket error:", error);

            wsRef.current.onmessage = (event) => {
                const byteData = new Uint8Array(event.data);
                HandleWebSocketData(byteData)
            };
        }

        return () => {
            wsRef.current?.close();
            wsRef.current = null;
        };


    }, []);

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

    return (
        <div className="game-board" onClick={onBoardClick}>
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
    gameBoard[move.ToIndex] = gameBoard[move.FromIndex];
    gameBoard[move.FromIndex] = GameBoardSquare.EMPTY;

    const capturedSquares = move.captured; 

    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if ((capturedSquares & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0)) {
            gameBoard[bitIndex] = GameBoardSquare.EMPTY;
        }
    }
}
const UndoMove = (move : CheckersMove, isPlayer1Turn : boolean, gameBoard : number[]) : void => {
    gameBoard[move.FromIndex] = gameBoard[move.ToIndex];
    gameBoard[move.ToIndex] = GameBoardSquare.EMPTY;

    const toRecover = isPlayer1Turn ? GameBoardSquare.PLAYER2PAWN : GameBoardSquare.PLAYER1PAWN;
    const capturedSquares = move.captured; 

    for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
        if ((capturedSquares & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0)) {
            gameBoard[bitIndex] = toRecover;
        }
    }
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


