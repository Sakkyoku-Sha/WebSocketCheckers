"use client"
import React, {useEffect, useRef, useState} from "react";
import {
    encodeActiveGamesMessage,
    encodeCreateGameMessage,
    encodeIdentifyUserMessage,
    encodeTryJoinGameMessage,
    encodeTryMakeMoveMessage
} from "./WebSocket/Encode";
import GameBoard from "./gameboard";
import GameHistory from "./gameHistory";
import GamesPanel from "./gamesPanel";
import {
    ActiveGamesMessage,
    CreateGameResultMessage,
    decode,
    FromServerMessageType, GameInfoMessage,
    NewMoveMessage,
    PlayerJoinedMessage,
    SessionStartMessage,
    TryJoinGameResult
} from "@/app/WebSocket/Decoding";
import Subscriptions from "@/app/Events/Events";

export interface CheckersMove{
  fromIndex: number;      
  toIndex: number;        
  promoted: boolean;     
  capturedPieces: bigint; 
}

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
  const gameId = useRef<number | null>(null);
  const wsRef = useRef<WebSocket | null>(null);
  const moveHistory = useRef<CheckersMove[]>([]); 
  const sessionIdRef = useRef<string | null>(null);

  const [moveNumber, setMoveNumber] = useState<number>(-1);
  
  const onGameInfoMessage = (gameInfo : GameInfoMessage) => {
      gameId.current = gameInfo.gameId;
      moveHistory.current = gameInfo.history; 
      setMoveNumber(gameInfo.historyCount-1);
  }
  
  const HandleWebSocketData = (byteData : ArrayBuffer) => {
        
    const resultingMessage = decode(byteData)

    switch(resultingMessage.type) {
        
        case FromServerMessageType.SessionStartMessage:
            sessionIdRef.current = (resultingMessage as SessionStartMessage).sessionId;
            
            //Send User Id to server.
            const newUserId = ResolveUserId();
            const toSend = encodeIdentifyUserMessage(newUserId);

            userId.current = newUserId;
            wsRef.current?.send(toSend);
            
            break;
        
        case FromServerMessageType.NewMoveMessage:
            moveHistory.current.push((resultingMessage as NewMoveMessage).move);
            setMoveNumber(moveHistory.current.length - 1);
            break;
            
        case FromServerMessageType.GameInfoMessage:
            const gameInfoMessage = (resultingMessage as GameInfoMessage);
            onGameInfoMessage(gameInfoMessage);
            break;
            
        case FromServerMessageType.TryJoinGameResultMessage:
            const tryJoinGameResult = (resultingMessage as TryJoinGameResult);
            if(tryJoinGameResult.gameInfo !== null) {
                onGameInfoMessage(tryJoinGameResult.gameInfo!)
            }
            
            //Clear Games since we are in one. 
            Subscriptions.activeGamesMessageEvent.emit(
                {
                    activeGames : [], 
                    type : FromServerMessageType.TryJoinGameResultMessage, 
                    version : 1
                });
            
            break;
    
        case FromServerMessageType.PlayerJoined:
            const playerName = (resultingMessage as PlayerJoinedMessage).playerName;
            console.log("Player joined:", playerName);
            break;
        
        case FromServerMessageType.CreateGameResultMessage:
            const resultId = (resultingMessage as CreateGameResultMessage).gameId;
            if(resultId !== null && resultId >= 0) {
                gameId.current = resultId;
            }
            break;
            
        case FromServerMessageType.ActiveGamesMessage: 
            const activeGames = (resultingMessage as ActiveGamesMessage); 
            Subscriptions.activeGamesMessageEvent.emit(activeGames); 
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

    const CreateNewGame = ()=> {
        if(!userId.current || !wsRef.current) return; 
        
        const createNewGameMessage = encodeCreateGameMessage(userId.current);
        wsRef.current.send(createNewGameMessage); 
    }
    
    const RefreshActiveGames = () => {
        if(!wsRef.current) return;

        const createNewGameMessage = encodeActiveGamesMessage();
        wsRef.current.send(createNewGameMessage);
    }; 

    const TryMakeMove = (fromIndex : number, toIndex : number) => {
        if(!userId.current || gameId.current === null || gameId.current < 0 || !wsRef.current) return;
        
        const tryMakeMoveMessage = encodeTryMakeMoveMessage(userId.current, gameId.current, fromIndex, toIndex);
        wsRef.current.send(tryMakeMoveMessage);
    }
    
    const TryJoinGame = (gameId : number) => {
        if(!userId.current || !wsRef.current) return;
        
        const tryJoinGameMessage = encodeTryJoinGameMessage(userId.current, gameId);
        wsRef.current.send(tryJoinGameMessage);
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
            <GameBoard makeMove={TryMakeMove} moveHistoryRef={moveHistory} moveNumber={moveNumber} gameIdRef={gameId}/>
          </div>
          <div className="player-one-info">

          </div>
        </div>
          <div className="games-panel-container">
            <GamesPanel 
                onCreateGameClick={CreateNewGame} 
                refreshClicked={RefreshActiveGames} 
                onGameClicked={TryJoinGame} />
          </div>
      </div>
  );
}

const moveByteSize = 11;  //FromIndex = 1, ToIndex = 1, Promoted = 1, Captured = 8
