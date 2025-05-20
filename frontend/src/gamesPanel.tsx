import React, {useEffect, useRef, useState} from 'react';
import Subscriptions from "@/Events/Events";
import {ActiveGamesMessage, GameCreatedOrUpdatedMessage, GameMetaData} from "@/WebSocket/Decoding";

export default function GamesPanel(props : {
    onCreateGameClick : () => void;
    refreshClicked : () => void;
    onGameClicked : (gameId : number) => void;
}){
    
    const cardUpdaters = useRef<Map<number, (gameMetaData: GameMetaData) => void>>(new Map());
    const activeGames= useRef<GameMetaData[]>([]);
    const [, forceUpdate] = useState(0);
    
    const onUpdateActiveGames = (activeGamesMessage: ActiveGamesMessage) => {
        //Entire overwrite of the active games.
        activeGames.current = activeGamesMessage.activeGames;
        forceUpdate((prev) => prev + 1);
    };
    
    const onGameCreatedOrUpdated = (gameCreatedMessage: GameCreatedOrUpdatedMessage) => {
        
        const gameMetaData = gameCreatedMessage.GameMetaData;
        if(cardUpdaters.current.has(gameMetaData.gameId)) {
            
            //Only update the card if it already exists.
            const updater = cardUpdaters.current.get(gameMetaData.gameId);
            if(updater !== undefined) {
                updater(gameMetaData);
            }
        }
        else{
            //Only re-render the entire list if a new element is added. 
            activeGames.current.push(gameMetaData);
            forceUpdate((prev) => prev + 1);
        }
    }
    
    useEffect(() => {
        
      Subscriptions.activeGamesMessageEvent.subscribe(onUpdateActiveGames);
      Subscriptions.gameCreatedOrUpdatedEvent.subscribe(onGameCreatedOrUpdated);
      
      return () => {
          Subscriptions.activeGamesMessageEvent.unsubscribe(onUpdateActiveGames);
          Subscriptions.gameCreatedOrUpdatedEvent.unsubscribe(onGameCreatedOrUpdated);
      }
      
    }, []);
    
    const createGame = () => { 
        props.onCreateGameClick();
    }
    
    const registerUpdater = (gameId: number, updateFn: (gameMetaData: GameMetaData) => void) => {
        if (cardUpdaters.current.has(gameId)) {
            cardUpdaters.current.delete(gameId);
        }
        cardUpdaters.current.set(gameId, updateFn);
    }
    
    const toDisplay = activeGames.current.map(gameMetaData => {
        return <GameCard key = {"game" + gameMetaData.gameId}
            initialGameMetaData={gameMetaData} 
            onClick={() => props.onGameClicked(gameMetaData.gameId)} 
            registerUpdater={registerUpdater}/>
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

interface GameCardProps {
    initialGameMetaData : GameMetaData;
    onClick: (gameId : number) => void;
    registerUpdater: (gameId : number, updateFn : (gameMetaData : GameMetaData) => void) => void,
}

function GameCard(props: GameCardProps) {
    
    const gameId = props.initialGameMetaData.gameId;
    const [player1Name, setPlayer1Name] = useState(props.initialGameMetaData.player1Name);
    const [player2Name, setPlayer2Name] = useState(props.initialGameMetaData.player2Name);
    
    const onGameUpdated = (gameMetaData: GameMetaData) => {
        setPlayer1Name(gameMetaData.player1Name);
        setPlayer2Name(gameMetaData.player2Name);
    }
    
    useEffect(() => {
        props.registerUpdater(gameId, onGameUpdated);
    }, [gameId]);
    
    return (
        <div key={gameId} className="game-card" onClick={() => props.onClick(gameId)}>
            <div className="game-id">Game ID: {gameId}</div>
            <div className="players">
                <div className="player">
                    <span className="label">Player 1:</span> {player1Name}
                </div>
                <div className="player">
                    <span className="label">Player 2:</span> {player2Name}
                </div>
            </div>
        </div>
    );
}