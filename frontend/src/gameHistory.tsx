import {GameInfo} from "@/WebSocket/Decoding";
import {ReactElement, RefObject} from "react";
import {CheckersMove, TimedCheckersMove} from "@/WebSocket/ByteReader";

export interface GameHistoryProps {
    currentGame: RefObject<GameInfo | null>
    moveNumber: number
    onMoveClick: (index : number) => void
}
const bigInt0 = BigInt(0);
const bigInt1000 = BigInt(1000);

export default function GameHistory(props : GameHistoryProps){

    const { currentGame, moveNumber, onMoveClick } = props;
    const moveHistory = currentGame.current?.history ?? []; 
    
    const handleClick = (index: number) => {
        onMoveClick(index);
    }
    
    //Group Moves into pairs of player1 and player2 moves in the same way chess.com does. 
    const normalizedMoves: ReactElement[] = []; 
    const gameStartTimeMs = currentGame.current?.gameTimeStartMs ?? null;
    
    for(let i = 0; i < moveHistory.length; i += 2) {
        
        const player1Move = moveHistory[i];
        const player2Move = i + 1 < moveHistory.length ? moveHistory[i + 1] : null;
        
        let player1TimeSpent : number = 0;
        let player2TimeSpent : number | undefined = undefined;
        if(i === 0){
            player1TimeSpent = Number((player1Move.timeMs - (gameStartTimeMs ?? bigInt0)) / bigInt1000);
            player2TimeSpent = player2Move === null ? 
                undefined : 
                Number((player2Move.timeMs - (gameStartTimeMs ?? bigInt0)) / bigInt1000);
        }
        else{
            const previousPlayer1Move = moveHistory[i - 2];
            const previousPlayer2Move = i - 1 < moveHistory.length ? moveHistory[i - 1] : null;
            
            player1TimeSpent = Number(player1Move.timeMs - previousPlayer1Move.timeMs) / 1000;
            player2TimeSpent = player2Move === null ? 
                undefined : 
                Number(player2Move.timeMs - (previousPlayer2Move?.timeMs ?? previousPlayer1Move.timeMs)) / 1000;
        }
        
        normalizedMoves.push(
            <GameHistoryItem
                key={i}
                moveNumber={Math.floor(i / 2) + 1}
                player1Move={player1Move}
                player2Move={player2Move}
                player1TimeSpent={player1TimeSpent}
                player2TimeSpent={player2TimeSpent}
                onClickPlayer1={() => handleClick(i)}
                onClickPlayer2={() => handleClick(i + 1)}
                highlightPlayer1Move={moveNumber === i}
                highlightPlayer2Move={moveNumber === i + 1}
            />
        );
    }
    
    return <div className="game-history-list">
        {normalizedMoves}
    </div>
}

interface GameHistoryItemProps {
    highlightPlayer1Move: boolean
    highlightPlayer2Move: boolean
    player1Move: TimedCheckersMove,
    player2Move: TimedCheckersMove | null,
    player1TimeSpent : number,
    player2TimeSpent : number | undefined,
    moveNumber: number
    onClickPlayer1: () => void
    onClickPlayer2: () => void
}

const letters = "ABCDEFGH";
function toBoardNotation(x : number, y : number) {
    return `${letters[x]}${8 - y}`; 
}

const GameHistoryItem = (props : GameHistoryItemProps) => {
        
      const player1Move = props.player1Move;
      const player2Move = props.player2Move;
      
      const player1Notation = toBoardNotation(player1Move.fromIndex % 8, Math.floor(player1Move.fromIndex / 8))
      const player2Notation = player2Move === null ? "" : toBoardNotation(player2Move.fromIndex % 8, Math.floor(player2Move.fromIndex / 8));
      
      const backGroundColorClass = props.moveNumber % 2 === 0 ? "even-turn" : "odd-turn";
      const highLightPlayer1 = props.highlightPlayer1Move ? "highlight-player1-move" : "";
      const highLightClassPlayer2 = props.highlightPlayer2Move ? "highlight-player2-move" : "";
      
      return (
          <div className={`game-history-item ${backGroundColorClass}`}>
              <div className={"game-history-turn-number"}>{props.moveNumber}.</div>
              <div 
                className={`game-history-move-player1 ${highLightPlayer1}`}
                onClick={props.onClickPlayer1}>
                  {player1Notation}
              </div>
              <div className="game-history-move-player1-time">{props.player1TimeSpent} s</div>
              <div 
                className={`game-history-move-player2 ${highLightClassPlayer2}`}
                onClick={props.onClickPlayer2}>
                  {player2Notation}
              </div>
             
              <div className="game-history-move-player2-time">{props.player2TimeSpent} s</div>
          </div>
      );
  };