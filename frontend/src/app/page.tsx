"use client"
import GameBoard from "./gameboard";
import { useEffect, useRef, RefObject, useState } from "react";
import GameHistory from "./gamehistory";

export interface Move {
  FromIndex: number
  ToIndex :number
}

export interface CheckersMove extends Move{
  promoted: boolean,
  captured: bigint //Bitboard representation
}


export default function Home() {

  const moveHistory = useRef<CheckersMove[]>([]); 
  const wsRef = useRef<WebSocket | null>(null);
  const [moveNumber, setMoveNumber] = useState<number>(-1);

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
              HandleWebSocketData(byteData, moveHistory);
              
              const latestMove = moveHistory.current.length - 1;
              setMoveNumber(latestMove);
          };
      }

      return () => {
          wsRef.current?.close();
          wsRef.current = null;
      };
  }, []);


  
  return (
    <div className="page">
      <div className="game-history-container">
        <div className="game-history-title">Game History</div> 
        <GameHistory moveHistory={moveHistory} moveNumber={moveNumber} onMoveClick={(index) => setMoveNumber(index)}/>
      </div>
      <div className="game-container">
        <div className="player-two-info">
          
        </div>
        <div className="game-area">
          <GameBoard moveHistoryRef={moveHistory} moveNumber={moveNumber}/>
        </div>
        <div className="player-one-info">

        </div>
      </div>
      <div className="game-search-container">
      </div>
    </div>
  );
}

const moveByteSize = 11;  //FromIndex = 1, ToIndex = 1, Promoted = 1, Captured = 8
const HandleWebSocketData = (byteData : Uint8Array, currMoveHistoryRef : RefObject<CheckersMove[]>) => {
          
  const moveCount = byteData.length / moveByteSize; 
     
  for(let i = 0; i < moveCount; i++){

      let capturedBitboard =  BigInt(0);
      for (let j = 0; j < 8; j++) {
        capturedBitboard += BigInt(byteData[(i * moveByteSize + 3) + j]) << BigInt(8 * j);
      }

      const move : CheckersMove = {
          FromIndex: byteData[i * moveByteSize],
          ToIndex: byteData[i * moveByteSize + 1],
          promoted : byteData[i * moveByteSize + 2] === 1,
          captured : capturedBitboard
      }

      currMoveHistoryRef.current.push(move);
  }
}