import React, {useEffect, useRef, useState} from "react";
import GameBoard from "./gameboard";
import GameHistory from "./gameHistory";
import GamesPanel from "./gamesPanel";

import {
    GameInfo,
    GameStatus,
    InitialStateMessage,
    PlayerJoinedMessage,
    TryCreateGameResultMessage,
    TryJoinGameResult,
    NewMoveMessage, GameStatusChangedMessage, DrawRequestRejectedMessage, DrawRequestMessage

} from "@/WebSocket/Decoding";

import WebSocketEvents from "@/WebSocket/WebSocketEvents";
import {WebSocketSend} from "@/WebSocket/WebSocketConnect";


export default function Home() {
    
  const currentGame = useRef<GameInfo | null>(null);
  const [moveNumber, setMoveNumber] = useState<number>(-1);
  
  const onGameInfoMessage = (gameInfo : GameInfo) => {
      currentGame.current = gameInfo;
      setMoveNumber(gameInfo.historyCount-1);
  }
  
  const onInitialStateMessage = (initialStateMessage : InitialStateMessage) => {
      if(initialStateMessage.gameInfo !== null) {
          onGameInfoMessage(initialStateMessage.gameInfo);
      }
  }
  
  const onTryJoinGameResponse = (tryJoinGameResult : TryJoinGameResult) => {
      if(tryJoinGameResult.gameInfo !== null && tryJoinGameResult.didJoinGame) {
          onGameInfoMessage(tryJoinGameResult.gameInfo)
      }
  }
  
  const onNewMoveMessage = (newMoveMessage : NewMoveMessage) => {
      if(currentGame.current === null) return;
      currentGame.current.forcedMoves = newMoveMessage.forcedMovesInPosition;
      currentGame.current.history.push(newMoveMessage.move);
      setMoveNumber(currentGame.current.history.length - 1);
  }
  
  const onGameStatusChanged = (gameStatusMessage : GameStatusChangedMessage) => {
      console.log("Game status changed: ", gameStatusMessage.gameStatus);
  }
  
  const onDrawRequest = (drawRequestMessage : DrawRequestMessage) => {
      console.log("Draw request received");
  }
  
  const onDrawRequestRejected = (drawRequestRejectedMessage : DrawRequestRejectedMessage) => {
      console.log("Draw request rejected");
  }
  
  const onPlayerJoined = (playerJoinedMessage : PlayerJoinedMessage) => {
      console.log("Player joined: " + playerJoinedMessage.playerName);
  }
  
  const onTryCreateGameResult = (tryCreateGameResult : TryCreateGameResultMessage) => {
      if(tryCreateGameResult.gameId >= 0) {
          currentGame.current = { //Probably should update the response here to send an empty GameInfo instead 
              gameId: tryCreateGameResult.gameId,
              gameName : "",
              player1Name: "Me",
              player1Id: "",
              player2Name: "",
              player2Id: "",
              forcedMoves: [],
              history: [],
              historyCount : 0,
              gameStatus : GameStatus.WaitingForPlayers,
          }
      }
  }
  
  useEffect(() => {

      WebSocketEvents.initialStateEvent.subscribe(onInitialStateMessage);
      WebSocketEvents.tryJoinGameResultEvent.subscribe(onTryJoinGameResponse);
      WebSocketEvents.newMoveEvent.subscribe(onNewMoveMessage);
      WebSocketEvents.gameStatusChangedEvent.subscribe(onGameStatusChanged);
      WebSocketEvents.drawRequestEvent.subscribe(onDrawRequest);
      WebSocketEvents.drawRequestRejectedEvent.subscribe(onDrawRequestRejected);
      WebSocketEvents.playerJoinedEvent.subscribe(onPlayerJoined);
      WebSocketEvents.tryCreateGameResultEmitter.subscribe(onTryCreateGameResult);
      
      return () => {
            WebSocketEvents.initialStateEvent.unsubscribe(onInitialStateMessage);
            WebSocketEvents.tryJoinGameResultEvent.unsubscribe(onTryJoinGameResponse);
            WebSocketEvents.newMoveEvent.unsubscribe(onNewMoveMessage);
            WebSocketEvents.gameStatusChangedEvent.unsubscribe(onGameStatusChanged);
            WebSocketEvents.drawRequestEvent.unsubscribe(onDrawRequest);
            WebSocketEvents.drawRequestRejectedEvent.unsubscribe(onDrawRequestRejected);
            WebSocketEvents.playerJoinedEvent.unsubscribe(onPlayerJoined);
      }
       
  }, []);

    const CreateNewGame = ()=> {
        WebSocketSend.tryCreateNewGame();
    }
    
    const RefreshActiveGames = () => {
        WebSocketSend.refreshActiveGames();
    }; 

    const TryMakeMove = (fromIndex : number, toIndex : number) => {
        WebSocketSend.tryMakeMove(fromIndex, toIndex);
    }
    
    const TryJoinGame = (gameId : number) => {
        WebSocketSend.tryJoinGame(gameId);
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
