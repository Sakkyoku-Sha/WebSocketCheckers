"use client"
import { HtmlContext } from 'next/dist/server/route-modules/pages/vendored/contexts/entrypoints';
import { MouseEventHandler, useEffect, useState, useRef, useMemo, JSX } from 'react';

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

export interface GamePiece{
    row: number
    col: number
    value: GameBoardSquare
}

export default function GameBoard() {

    const [selectedSquare, setSelectedSquare] = useState<{ row: number, col: number } | null>(null);
    const [gameState, setGameState] = useState<Uint8Array>(initialGameState); 
    const wsRef = useRef<WebSocket | null>(null);

    
    const TryMakeMove = (row: number, col: number) => {
        if (selectedSquare === null) return;
        
        const move: Move = {
            fromX: selectedSquare.col,
            fromY: selectedSquare.row,
            toX: col,
            toY: row
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
    
    const onBoardClick: MouseEventHandler<HTMLDivElement> = (event) => {
        const rect = event.currentTarget.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        const col = Math.floor(x / (rect.width / 8));  // Divide by 8 to get 8 columns
        const row = Math.floor(y / (rect.height / 8)); // Divide by 8 to get 8 rows
    
        if (selectedSquare === null) {
            setSelectedSquare({ row, col });
        } else {
            TryMakeMove(row, col);
            setSelectedSquare(null);
        }
    };
    
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

const DetermineBackGroundColor = (row : number, col : number) : string => {
    if ((row + col) % 2 === 0) {
        return "whiteSquare";
    }
    return "blackSquare";
}

// Temp value untill we actually get the server to send game state with the init on websocket connect. 
const initialGameState = new Uint8Array([
    // Player 2 pieces on the top 3 rows (3)
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 0
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 1
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 2
  
    // Empty middle rows (0)
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 3
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 4
  
    // Player 1 pieces on the bottom 3 rows (1)
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 5
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 6
    0, 0, 0, 0, 0, 0, 0, 0,  // Row 7
  
    // Turn flag (index 64)
    1
  ]);


