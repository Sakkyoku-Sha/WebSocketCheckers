"useclient";
import { RefObject } from "react";
import { CheckersMove } from "./page";


export interface GameHistoryProps {
    moveHistory: RefObject<CheckersMove[]>
    moveNumber: number
    onMoveClick: (index : number) => void
}

export default function GameHistory(props : GameHistoryProps){

    const { moveHistory, moveNumber, onMoveClick } = props;

    const handleClick = (index: number) => {
        onMoveClick(index);
    }

    const items = moveHistory.current.map((move, index) => {
        return <GameHistoryItem
                key={index}
                move={move}
                onClick={() => {handleClick(index)}}
                highlight={moveNumber === index}
            />
    });
    
    return <div className="game-history-list">
        {items}
    </div>
}

interface GameHistoryItemProps {
    highlight?: boolean
    move: CheckersMove
    onClick: () => void
}

const letters = "ABCDEFGH";
function toBoardNotation(x : number, y : number) {
    return `${letters[x]}${8 - y}`; 
}

const GameHistoryItem = (props : GameHistoryItemProps) => {
    
      const { highlight, move, onClick } = props;
      const fromNotation = toBoardNotation(move.fromIndex % 8, Math.floor(move.fromIndex / 8))
      const toNotation = toBoardNotation(move.toIndex % 8, Math.floor(move.toIndex / 8));
      
      const active = highlight ? "active" : "";
      
      const capturedSquares = move.capturedPieces;
      let capturedPiecesXY: { x: number; y: number }[] = [];
  
      if (capturedSquares !== BigInt(0)) {
          for (let bitIndex = 0; bitIndex < 64; bitIndex++) {
              if ((capturedSquares & (BigInt(1) << BigInt(bitIndex))) !== BigInt(0)) {
                  capturedPiecesXY.push({ x: bitIndex % 8, y: Math.floor(bitIndex / 8) });
              }
          }
      }
      
      const capturedPieces = capturedPiecesXY.map((xy) => toBoardNotation(xy.x, xy.y)).join(", ");
      const capturedPiecesText = capturedPieces.length > 0 ? ` Captured: ${capturedPieces}` : "";
      
      const moveDescription = `${fromNotation} → ${toNotation}${capturedPiecesText}`;

      return (
          <div className={`game-history-item ${active}`} onClick={() => onClick()}>
              <div className="game-history-move-description">{moveDescription}</div>
          </div>
      );
  };