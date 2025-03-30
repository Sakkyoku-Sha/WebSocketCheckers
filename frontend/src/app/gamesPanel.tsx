"use client";
import React from 'react';
import { fetchCreateGame, fetchJoinGame } from './Fetch/Fetch';
import { gql, useQuery } from '@apollo/client';

const openGamesQuery = 
gql`
    query OpenGames{
        openGames{
            gameId,
            gameName,
            player1{
                userId
                playerName
            }
            player2{
                userId
                playerName
            }
        }
    }
`; 

interface GamePanelProps {
    userIdRef : React.RefObject<string | null>;
    gameIdRef : React.RefObject<string | null>;
}

export default function GamesPanel(props : GamePanelProps){

    const createGame = () => {
        
        if(props.userIdRef.current === null) {
            console.error("User ID is null, cannot create game.");
            return;
        }

        const newGameId = fetchCreateGame(props.userIdRef.current as string);
        if(newGameId === "-1") {
            console.error("Failed to create game");
            return;
        }
        
        props.gameIdRef.current = newGameId;
        console.log("Game created with ID:", newGameId);
    }

    const joinGame = (gameToJoinId : string) => {
        if(gameToJoinId === null) {
            console.error("Game ID is null, cannot join game.");
            return;
        }
        if(props.userIdRef.current === null) {
            console.error("User ID is null, cannot join game.");
            return;
        }
        fetchJoinGame(gameToJoinId, props.userIdRef.current as string);
    }


    const {loading ,error, data} = useQuery(openGamesQuery, {
        fetchPolicy: 'network-only',
        onCompleted: (data) => {
            const openGames = data.openGames;
            console.log("Open games:", openGames);
        },
        onError: (error) => {
            console.error("Error fetching open games:", error);
        },
    });

    const matchestoRender = data?.openGames.map((game : { gameId: string, gameName: string, player1: { userId: string, playerName: string }, player2: { userId: string, playerName: string } }) => {
        
        const gameId = game.gameId;
        const gameName = game.gameName;

        return <div key={gameId} className="game-item" onClick={() => joinGame(gameId)}>
            {gameName}
        </div>
    })

    return (
        <div className="games-panel">
            <button className="create-game-button" onClick={createGame}>Create Game</button>
            {matchestoRender}
        </div>
    );
}