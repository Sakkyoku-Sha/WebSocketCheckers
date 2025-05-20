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
    decode,
    GameCreatedOrUpdatedMessage,
    GameInfo,
    GameStatus,
    InitialStateMessage,
    PlayerJoinedMessage,
    SessionStartMessage,
    TryCreateGameResultMessage,
    TryJoinGameResult,
    FromServerMessageType, NewMoveMessage, GameStatusChangedMessage

} from "@/WebSocket/Decoding";

import Subscriptions from "@/Events/Events";

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
    
  const currentGame = useRef<GameInfo | null>(null);
  
  const wsRef = useRef<WebSocket | null>(null);
  const sessionIdRef = useRef<string | null>(null);

  const userId = useRef<string | null>(null);
  const [moveNumber, setMoveNumber] = useState<number>(-1);
  
  const onGameInfoMessage = (gameInfo : GameInfo) => {
      currentGame.current = gameInfo;
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
            
        case FromServerMessageType.InitialStateMessage:
            const gameInfoMessage = (resultingMessage as InitialStateMessage);
            if(gameInfoMessage.gameInfo !== null) {
                onGameInfoMessage(gameInfoMessage.gameInfo);
            }
            if(gameInfoMessage.activeGames.length > 0){
                Subscriptions.activeGamesMessageEvent.emit({
                    version : 1,
                    type : FromServerMessageType.InitialStateMessage,
                    activeGames: gameInfoMessage.activeGames
                })
            }
          
            break;
            
        case FromServerMessageType.TryJoinGameResponse:
            const tryJoinGameResult = (resultingMessage as TryJoinGameResult);
            if(tryJoinGameResult.gameInfo !== null) {
                onGameInfoMessage(tryJoinGameResult.gameInfo)
            }
            break;
        case FromServerMessageType.NewMove:
            if(currentGame.current === null) return;
            const newMoveMessage = resultingMessage as NewMoveMessage;
            
            currentGame.current.forcedMoves = newMoveMessage.forcedMovesInPosition;
            currentGame.current.history.push(newMoveMessage.move);
            setMoveNumber(currentGame.current.history.length - 1);
            
            break;
        case FromServerMessageType.GameStatusChanged:
            const gameStatusMessage = (resultingMessage as GameStatusChangedMessage); 
            console.log("Game status changed: ", gameStatusMessage.gameStatus);
            break;
        case FromServerMessageType.DrawRequest:
            console.log("Draw request received");
            break;
        case FromServerMessageType.DrawRequestRejected:
            console.log("Sent draw request rejected");
            break;

        case FromServerMessageType.PlayerJoined:
            const playerName = (resultingMessage as PlayerJoinedMessage).playerName;
            console.log("Player joined:", playerName);
            break;
        
        case FromServerMessageType.GameCreatedMessage:
            const gameCreatedMessage = (resultingMessage as GameCreatedOrUpdatedMessage);
            Subscriptions.gameCreatedOrUpdatedEvent.emit(gameCreatedMessage);
            break;
            
        case FromServerMessageType.ActiveGamesResponse: 
            const activeGames = (resultingMessage as ActiveGamesMessage); 
            Subscriptions.activeGamesMessageEvent.emit(activeGames); 
            break;
            
        case FromServerMessageType.TryCreateGameResponse:    
            const tryCreateGameResult = (resultingMessage as TryCreateGameResultMessage);
            if(tryCreateGameResult.gameId >= 0) {
                currentGame.current = { //Probably should update the response here to send an empty GameInfo instead 
                    gameId: tryCreateGameResult.gameId,
                    gameName : "", 
                    player1Name: "Me",
                    player1Id: userId.current ?? "",
                    player2Name: "",
                    player2Id: "",
                    forcedMoves: [],
                    history: [],
                    historyCount : 0,
                    gameStatus : GameStatus.WaitingForPlayers,
                }
            }
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
        
        const createNewGameMessage = encodeCreateGameMessage();
        wsRef.current.send(createNewGameMessage); 
    }
    
    const RefreshActiveGames = () => {
        if(!wsRef.current) return;

        const createNewGameMessage = encodeActiveGamesMessage();
        wsRef.current.send(createNewGameMessage);
    }; 

    const TryMakeMove = (fromIndex : number, toIndex : number) => {
        if(!userId.current || currentGame.current === null || !wsRef.current) return;
        
        const tryMakeMoveMessage = encodeTryMakeMoveMessage(fromIndex, toIndex);
        wsRef.current.send(tryMakeMoveMessage);
    }
    
    const TryJoinGame = (gameId : number) => {
        if(!userId.current || !wsRef.current) return;
        
        const tryJoinGameMessage = encodeTryJoinGameMessage(gameId);
        wsRef.current.send(tryJoinGameMessage);
    }
    
    return (
      <div className="page">
        <div className="game-history-container">
          <div className="game-history-title">Game History</div> 
          <GameHistory currentGame={currentGame} moveNumber={moveNumber} onMoveClick={(index) => setMoveNumber(index)}/>
        </div>
        <div className="game-container">
          <div className="player-two-info player-display-card">
              <PlayerCard playerName={currentGame.current?.player2Name ?? "Waiting For Player"}/>
          </div>
          <div className="game-area">
            <GameBoard makeMove={TryMakeMove} 
                       currentGame={currentGame}
                       moveNumber={moveNumber}/>
          </div>
           
          <div className="player-one-info player-display-card">
              <PlayerCard playerName={currentGame.current?.player1Name ?? "Waiting For Player"}/>
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

interface PlayerCardProps {
    playerName : string,
}
function PlayerCard(props: PlayerCardProps) {
    return (
        <div
            className={`player-card`}
            tabIndex={0}
        >
            <div className="avatar" style={{ backgroundImage: `url()` }} />
            <div className="info">
                <span className="name">{props.playerName}</span>
                <div className="status-bar">
                    <div className="bar" style={{ width: `5%` }} />
                </div>
            </div>
        </div>
    );
}
