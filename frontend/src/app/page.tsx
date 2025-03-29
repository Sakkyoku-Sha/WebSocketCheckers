"use client"
import GameBoard from "./gameboard";
import { useEffect, useRef, RefObject, useState } from "react";
import GameHistory from "./gamehistory";
import { encodeIdentifyUserMessage } from "./WebSocket/Encode";
import { decodeMessage, GameHistoryUpdateMessage, PlayerJoinedMessage, SessionStartMessage, ToClientMessageType } from "./WebSocket/Decode";

export interface CheckersMove{
  fromIndex: number;      
  toIndex: number;        
  promoted: boolean;     
  capturedPieces: bigint; 
}

export default function Home() {

  const moveHistory = useRef<CheckersMove[]>([]); 
  const wsRef = useRef<WebSocket | null>(null);
  const sessionIdRef = useRef<string | null>(null);
  const userId = useRef<string | null>(null);
  const gameId = useRef<string | null>(null);

  const [moveNumber, setMoveNumber] = useState<number>(-1);
  
  const HandleWebSocketData = (byteData : ArrayBuffer) => {
        
    const resultingMessage = decodeMessage(byteData)

    switch(resultingMessage?.type) {
      case ToClientMessageType.SessionStartMessage:
        const sessionStartMessage = resultingMessage?.message as SessionStartMessage;
        sessionIdRef.current = sessionStartMessage.sessionId;
        console.log("Session started:", sessionStartMessage.sessionId);
        break;
      case ToClientMessageType.GameHistoryUpdate:
        const gameHistoryUpdate = resultingMessage?.message as GameHistoryUpdateMessage;
        moveHistory.current = moveHistory.current.concat(gameHistoryUpdate.moves);
        setMoveNumber(moveHistory.current.length - 1);
        break;
      case ToClientMessageType.PlayerJoined:
        const playerJoinedMessage = resultingMessage?.message as PlayerJoinedMessage;
        console.log("Player joined:", playerJoinedMessage.userId);
        break;
      default:
        console.error("Unknown message type:", resultingMessage?.type);
    }
  }

  useEffect(() => {

      if (!wsRef.current) {
          wsRef.current = new WebSocket("ws://localhost:5050/ws");
          wsRef.current.binaryType = "arraybuffer";

          wsRef.current.onopen = () => {
              console.log("Connected to server");
              //Send clientId to server 
              const newUserId = crypto.randomUUID(); 
              const message = encodeIdentifyUserMessage(newUserId);
              
              userId.current = newUserId;
              wsRef.current?.send(message);
          };
          wsRef.current.onclose = () => console.log("Disconnected from server");
          wsRef.current.onerror = (error) => console.error("WebSocket error:", error);

          wsRef.current.onmessage = (event) => {
              HandleWebSocketData(event.data as ArrayBuffer);
          };
      }

      return () => {
          wsRef.current?.close();
          wsRef.current = null;
      };
  }, []);

  function createGame(event: any): void {
  
    fetch("http://localhost:5050/CreateGame", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        userId: userId.current,
      })})
      .then((response) => {
        console.log("Response:", response);
        if (!response.ok) {
          throw new Error("Network response was not ok");
        }
        // Parse JSON response
        return response.json(); // This extracts the content from the response body as JSON
      })
      .then((data) => {
        console.log("Response Content:", data); // Now you can use the parsed data
        gameId.current = data.gameId; // Assuming the server returns a gameId
      })
      .catch((error) => {
        console.error("Fetch error:", error);
      });
 
  
  }

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
          <GameBoard moveHistoryRef={moveHistory} moveNumber={moveNumber} gameIdRef={gameId}/>
        </div>
        <div className="player-one-info">

        </div>
      </div>
      <div className="game-search-container">
        <button className="create-game-button" onClick={createGame}>Create Game</button>
      
      </div>
    </div>
  );
}

const moveByteSize = 11;  //FromIndex = 1, ToIndex = 1, Promoted = 1, Captured = 8
