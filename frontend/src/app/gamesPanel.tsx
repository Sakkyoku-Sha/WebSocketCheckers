"use client";
import React, {useEffect, useState} from 'react';
import Subscriptions from "@/app/Events/Events";
import {ActiveGamesMessage, GameCreatedMessage, GameMetaData} from "@/app/WebSocket/Decoding";

export default function GamesPanel(props : {
    onCreateGameClick : () => void;
    refreshClicked : () => void;
    onGameClicked : (gameId : number) => void;
}){
    
    const [activeGames, setActiveGames] = useState<GameMetaData[]>();
    
    const onUpdateActiveGames = (activeGamesMessage: ActiveGamesMessage) => {
        setActiveGames(activeGamesMessage.activeGames);
    };
    
    const onGameCreated = (gameCreatedMessage: GameCreatedMessage) => {
        setActiveGames((prevActiveGames) => {
            if (prevActiveGames === undefined) {
                return [gameCreatedMessage.GameMetaData];
            } else {
                return [...prevActiveGames, gameCreatedMessage.GameMetaData];
            }
        });
    }
    
    useEffect(() => {
        
      Subscriptions.activeGamesMessageEvent.subscribe(onUpdateActiveGames);
      Subscriptions.gameCreatedEvent.subscribe(onGameCreated);
      
      return () => {
          Subscriptions.activeGamesMessageEvent.unsubscribe(onUpdateActiveGames);
          Subscriptions.gameCreatedEvent.unsubscribe(onGameCreated);
      }
      
    }, []);
    
    const createGame = () => { 
        props.onCreateGameClick();
    }

    const toDisplay = activeGames?.map((game: GameMetaData) => {
        return (
            <div key={game.gameId} className="game-card" onClick={() => props.onGameClicked(game.gameId)}>
                <div className="game-id">Game ID: {game.gameId}</div>
                <div className="players">
                    <div className="player">
                        <span className="label">Player 1:</span> {game.player1Name}
                    </div>
                    <div className="player">
                        <span className="label">Player 2:</span> {game.player2Name}
                    </div>
                </div>
            </div>
        );
    });

    return (
        <div className="games-panel">
            <div className="games-panel-header">
                <button className="create-game-button" onClick={createGame}>
                    + Create Game
                </button>
                <button className="refresh-button" onClick={props.refreshClicked}>
                    ⟳ Get Active Games
                </button>
            </div>
            <div className="games-list">
                {toDisplay}
            </div>
        </div>
    );
}