"use client"
import { useEffect, useState, useRef, useMemo, JSX } from 'react';

enum GameBoardSquare{
    EMPTY = 0,
    PLAYER1PAWN = 1,
    PLAYER1KING = 2,
    PLAYER2PAWN = 3,
    PLAYER2KING = 4    
}
export interface Move {
    fromX: number
    fromY: number
    toX: number
    toY: number
}


export default function GameBoard() {

    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<Uint8Array>(initialGameState); 
    const wsRef = useRef<WebSocket | null>(null);

    
    const TryMakeMove = (row: number, col: number) => {
        if (selectedSquare === null) return;
        
        const move: Move = {
            fromX: selectedSquare.row,
            fromY: selectedSquare.col,
            toX: row,
            toY: col
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
    
    const onSquareClick = (row: number, col: number) => {
        if (selectedSquare === null) {
            setSelectedSquare({row, col});
        } else {
            TryMakeMove(row, col);
            setSelectedSquare(null);
        }
    }
    
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
                const gameStateBytes = new Uint8Array(event.data);
                if (gameStateBytes.length === 65) {
                    setGameState(gameStateBytes);
                }
            };
        }

        return () => {
            wsRef.current?.close();
            wsRef.current = null;
        };


    }, []);

    const rows = useMemo<JSX.Element[]>(() => {
        return Array.from({ length: 8 }, (_, rowIndex) => (
            <div key={rowIndex} className="game-board-row">
                {Array.from({ length: 8 }, (_, colIndex) => {
                    const squareIndex = rowIndex * 8 + colIndex;
                    return (
                        <div
                            key={colIndex}
                            onClick={() => onSquareClick(rowIndex, colIndex)}
                            className={`gameBoardSquare ${DetermineBackGroundColor(rowIndex, colIndex)} ${selectedSquare?.row === rowIndex && selectedSquare?.col === colIndex ? "selected" : ""}`}
                        >
                            {renderPiece(gameState[squareIndex])}
                        </div>
                    );
                })}
            </div>
        ));
    }, [gameState, selectedSquare]);
  
    return <div className="game-board">{rows}</div>;
}

const renderPiece = (squareValue: number) => {
    switch (squareValue) {
        case GameBoardSquare.PLAYER1PAWN: return <div className="player1-piece"></div>;
        case GameBoardSquare.PLAYER1KING: return <div className="player1-piece king"></div>;
        case GameBoardSquare.PLAYER2PAWN: return <div className="player2-piece"></div>;
        case GameBoardSquare.PLAYER2KING: return <div className="player2-piece king"></div>;
        default: return null;
    }
};

const DetermineBackGroundColor = (row : number, col : number) : string => {
    if ((row + col) % 2 === 0) {
        return "whiteSquare";
    }
    return "blackSquare";
}

// Temp value untill we actually get the server to send game state with the init on websocket connect. 
const initialGameState = new Uint8Array([
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
  
    // Turn flag (index 64)
    1
  ]);


