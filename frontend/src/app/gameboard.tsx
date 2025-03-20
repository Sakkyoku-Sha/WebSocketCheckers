"use client"
import { useEffect, useState } from 'react';


export interface GameState {
    Board: number[][]
    Player1Turn: boolean
}
export interface Move {
    fromX: number
    fromY: number
    toX: number
    toY: number
  }
  

export default function GameBoard() {

    const [selectedSquare, setSelectedSquare] = useState<number[] | undefined>(undefined);
    const [gameBoard, setGameBoard] = useState<GameBoardSquare[][]>(generateInitialBoard()); 
    
    const TryMakeMove = (row: number, col: number) => {
        if (selectedSquare === undefined) return;
        
        const move: Move = {
            fromX: selectedSquare[0],
            fromY: selectedSquare[1],
            toX: row,
            toY: col
        };

        const endPoint = 'http://localhost:5041/TryMakeMove';
        fetch(endPoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(move)
        })
    }

    const onSquareClick = (row: number, col: number) => {
        if (selectedSquare === undefined) {
            setSelectedSquare([row, col]);
        } else {
            TryMakeMove(row, col);
            setSelectedSquare(undefined);
        }
    }
    
    useEffect(() => {

        const ws = new WebSocket('ws://localhost:5041/ws');

        ws.onopen = (event) => {
          console.log('Connected to server');
        };
        
        ws.onclose = () => {
          console.log('Disconnected from server');
        };
        
        ws.onerror = (error) => {
          console.error("WebSocket error:", error);
        };

        ws.onmessage = (event) => {
            const gameState = JSON.parse(event.data) as GameState;
    
            setGameBoard(gameState.Board
                .map(row => row.map(square => {
                    switch(square) {
                        case 1:
                            return GameBoardSquare.PLAYER1PAWN;
                        case 2:
                            return GameBoardSquare.PLAYER1KING;
                        case 3:
                            return GameBoardSquare.PLAYER2PAWN;
                        case 4:
                            return GameBoardSquare.PLAYER2KING;
                        default:
                            return GameBoardSquare.EMPTY;
                    }
                }))
            );
        };


        return () => {
            ws.close();
        }
    }, []);
    
       
    return (
        <div className="game-board">
            {gameBoard.map((row, rowIndex) => (
                <div key={rowIndex} className="game-board-row">
                    {row.map((square, colIndex) => (
                        <div 
                           onClick={() => onSquareClick(rowIndex, colIndex)} 
                            key={colIndex} className={`gameBoardSquare ${DetermineBackGroundColor(rowIndex, colIndex)} ${selectedSquare && selectedSquare[0] === rowIndex && selectedSquare[1] === colIndex ? "selected" : ""}`}>
                            {square === GameBoardSquare.PLAYER1PAWN && <div className="player1-piece"></div>}
                            {square === GameBoardSquare.PLAYER1KING && <div className="player1-piece"></div>}
                            {square === GameBoardSquare.PLAYER2PAWN && <div className="player2-piece"></div>}
                            {square === GameBoardSquare.PLAYER2KING && <div className="player2-piece"></div>}
                        </div>
                    ))}
                </div>
            ))}
        </div>
    );
}

const DetermineBackGroundColor = (row : number, col : number) : string => {
    if ((row + col) % 2 === 0) {
        return "whiteSquare";
    }
    return "blackSquare";
}

function generateInitialBoard(): GameBoardSquare[][] {

    const board: GameBoardSquare[][] = Array(8).fill(null).map(() => Array(8).fill(GameBoardSquare.EMPTY));

    for (let row = 0; row < 3; row++) {
        for (let col = 0; col < 8; col++) {
            if ((row + col) % 2 !== 0) {
                board[row][col] = GameBoardSquare.PLAYER2PAWN;
            }
        }
    }

    for (let row = 5; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            if ((row + col) % 2 !== 0) {
                board[row][col] = GameBoardSquare.PLAYER1PAWN;
            }
        }
    }

    return board;
}

enum GameBoardSquare{
    EMPTY = 0,
    PLAYER1PAWN = 1,
    PLAYER1KING = 2,
    PLAYER2PAWN = 3,
    PLAYER2KING = 4    
}
