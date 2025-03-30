"use client"
import React, { useEffect, useRef, RefObject, useState } from "react";
import { encodeIdentifyUserMessage } from "./WebSocket/Encode";
import { decodeMessage, GameHistoryUpdateMessage, PlayerJoinedMessage, SessionStartMessageDecoded, ToClientMessageType } from "./WebSocket/Decode";
import GameBoard from "./gameboard";
import GameHistory from "./gameHistory";
import GamesPanel from "./gamesPanel";
import { ApolloClient, ApolloProvider, InMemoryCache } from "@apollo/client";

export interface CheckersMove{
  fromIndex: number;      
  toIndex: number;        
  promoted: boolean;     
  capturedPieces: bigint; 
}

const graphqlEndPoint = "http://localhost:5050/graphql";
const client = new ApolloClient({
    uri: graphqlEndPoint,
    cache: new InMemoryCache(),
}); 

const ResolveUserId = () => {
    const userId = localStorage.getItem("userId");
    if (userId === null) {
        const newUserId = crypto.randomUUID();
        localStorage.setItem("userId", newUserId);
        return newUserId;
    }
    return userId;
}

export default function Home() {

  const userId = useRef<string | null>(null);
  const gameId = useRef<string | null>(null);
  const wsRef = useRef<WebSocket | null>(null);
  const moveHistory = useRef<CheckersMove[]>([]); 
  const sessionIdRef = useRef<string | null>(null);

  const [moveNumber, setMoveNumber] = useState<number>(-1);
  
  const HandleWebSocketData = (byteData : ArrayBuffer) => {
        
    const resultingMessage = decodeMessage(byteData)

    switch(resultingMessage?.type) {
      case ToClientMessageType.SessionStartMessage:
        const sessionStartMessage = resultingMessage?.message as SessionStartMessageDecoded;
        sessionIdRef.current = sessionStartMessage.sessionId;
        console.log("Session started:", sessionStartMessage.sessionId);
        if(sessionStartMessage.isInGame && sessionStartMessage.gameInfo) {
          gameId.current = sessionStartMessage.gameInfo.gameId;
          console.log("Rejoinin gameID:", sessionStartMessage.gameInfo.gameId);
        }
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
              const newUserId = ResolveUserId();
              
              
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
        <ApolloProvider client={client}>
          <div className="games-panel-container">
            <GamesPanel userIdRef={userId} gameIdRef={gameId}/>
          </div>
        </ApolloProvider>
      </div>
  );
}

const moveByteSize = 11;  //FromIndex = 1, ToIndex = 1, Promoted = 1, Captured = 8
