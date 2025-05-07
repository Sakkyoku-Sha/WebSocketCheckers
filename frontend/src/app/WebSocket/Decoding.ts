import {ByteReader, CheckersMove} from "./ByteReader";

export enum FromServerMessageType
{
    SessionStartMessage = 0,
    PlayerJoined = 1,
    NewMoveMessage = 2,
    GameInfoMessage = 3,
    TryJoinGameResultMessage = 4,
    CreateGameResultMessage = 5,
    ActiveGamesMessage = 6,
}

export enum GameStatus
{
    NoPlayers, 
    WaitingForPlayers,
    InProgress,
    Finished,
    Abandoned
}

export interface WebSocketMessage {
    version : number,
    type: FromServerMessageType
}
export interface SessionStartMessage extends WebSocketMessage {
    sessionId : string; 
}
export interface PlayerJoinedMessage extends WebSocketMessage { 
    playerId : string,
    playerName : string,
}
export interface NewMoveMessage extends WebSocketMessage {
   move : CheckersMove
}
export interface TryJoinGameResult extends WebSocketMessage {
    didJoinGame : boolean
    gameInfo : GameInfoMessage | null,
}
export interface GameInfoMessage extends WebSocketMessage {
    gameId : number,
    gameStatus : GameStatus,
    gameName : string,
    
    player1Id : string,
    player1Name : string,
    
    player2Id : string,
    player2Name : string,
    
    historyCount : number,
    history : Array<CheckersMove>
}
export interface CreateGameResultMessage extends WebSocketMessage {
    gameId : number
}

export interface GameMetaData{
    gameId : number,
    player1Name : string,
    player2Name : string,
}
export interface ActiveGamesMessage extends WebSocketMessage {
    activeGames : GameMetaData[]
}

type DecodeResult = WebSocketMessage | 
                    SessionStartMessage |
                    PlayerJoinedMessage | 
                    NewMoveMessage |    
                    GameInfoMessage |
                    TryJoinGameResult |
                    CreateGameResultMessage | 
                    ActiveGamesMessage;

function decodeSessionStartMessage(byteReader: ByteReader, version: number) : SessionStartMessage {
    return {
        type : FromServerMessageType.SessionStartMessage,
        version : version,
        sessionId : byteReader.readGuid(),
    };
}

function decodePlayerJoined(byteReader: ByteReader, version: number): PlayerJoinedMessage {
    
    const playerId = byteReader.readGuid();
    const playerNameLength = byteReader.readUint8();
    const playerName = byteReader.readStringUTF16LE(playerNameLength);
    
    return {
        type : FromServerMessageType.PlayerJoined,
        version : version,
        playerId : playerId,
        playerName : playerName
    }
}

function decodeNewMoveMessage(byteReader: ByteReader, version: number) :  NewMoveMessage {
    return {
        type : FromServerMessageType.NewMoveMessage,
        version : version,
        move : byteReader.readCheckersMove(),
    };
}

function decodeGameInfoMessage(byteReader: ByteReader, version: number) : GameInfoMessage {
    
    const gameId = byteReader.readInt32();
    const gameStatus = byteReader.readUint8() as GameStatus;
    const gameNameLength = byteReader.readUint8();
    const gameName = byteReader.readStringUTF16LE(gameNameLength);
    
    const player1Id = byteReader.readGuid();
    const player1NameLength = byteReader.readUint8();
    const player1Name = byteReader.readStringUTF16LE(player1NameLength); 
    
    const player2Id = byteReader.readGuid();
    const player2NameLength = byteReader.readUint8();
    const player2Name = byteReader.readStringUTF16LE(player2NameLength);
    
    const historyCount = byteReader.readUint16();
    const history = byteReader.readCheckersMoves(historyCount);
    
    return {
        type: FromServerMessageType.GameInfoMessage,
        version : version,
        
        gameId : gameId,
        gameStatus : gameStatus,
        gameName : gameName,
        
        player1Id : player1Id,
        player1Name : player1Name,
        
        player2Id : player2Id,
        player2Name : player2Name,
        
        historyCount : historyCount,
        history : history
    }
}

function decodeTryJoinGameResult(byteReader: ByteReader, version: number) : TryJoinGameResult {
    
    const didJoinGame = byteReader.readUint8();
    let gameInfoMessage : GameInfoMessage | null = null;
    if(didJoinGame) {
        gameInfoMessage = decodeGameInfoMessage(byteReader, version);
    }
    
    return {
        type : FromServerMessageType.TryJoinGameResultMessage,
        version : version,
        didJoinGame : Boolean(byteReader.readUint8()),
        gameInfo : gameInfoMessage,
    };
}


function decodeCreateGameResult(byteReader: ByteReader, version: number) : CreateGameResultMessage {
    return {
        type : FromServerMessageType.CreateGameResultMessage,
        version : version,
        gameId : byteReader.readInt32(),
    };
}

function decodeActiveGamesMessage(byteReader: ByteReader, version: number) : ActiveGamesMessage {
    
    const activeGames : GameMetaData[] = [];
    
    while(byteReader.bytesRemaining > 0){
        
        const gameId = byteReader.readInt32();
        
        const player1NameLength = byteReader.readUint8();
        let player1Name = "";
        if(player1NameLength > 0){
            player1Name = byteReader.readStringUTF16LE(player1NameLength);
        }
        
        const player2NameLength = byteReader.readUint8();
        let player2Name = ""; 
        if(player2NameLength > 0){
            player2Name = byteReader.readStringUTF16LE(player2NameLength);
        }
        
        activeGames.push({
            gameId : gameId,
            player1Name : player1Name,
            player2Name : player2Name,
        })
    }
    
    return {
        type: FromServerMessageType.ActiveGamesMessage,
        version: version,
        activeGames: activeGames,
    }
    
    
}

export function decode(arrayBuffer : ArrayBuffer) : DecodeResult {
    
    const byteReader = new ByteReader(arrayBuffer);
    const version = byteReader.readUint16();
    const type = byteReader.readUint16() as FromServerMessageType;
    
    switch (type) {

        case FromServerMessageType.SessionStartMessage:
            return decodeSessionStartMessage(byteReader , version);
        case FromServerMessageType.PlayerJoined:
            return decodePlayerJoined(byteReader, version); 
        case FromServerMessageType.NewMoveMessage:
            return decodeNewMoveMessage(byteReader, version); 
        case FromServerMessageType.GameInfoMessage:
            return decodeGameInfoMessage(byteReader, version);
        case FromServerMessageType.TryJoinGameResultMessage:
            return decodeTryJoinGameResult(byteReader, version);
        case FromServerMessageType.CreateGameResultMessage:
            return decodeCreateGameResult(byteReader, version);
        case FromServerMessageType.ActiveGamesMessage: 
            return decodeActiveGamesMessage(byteReader, version);
            
            
        default: throw new Error("Unknown type '" + type + "'");   
    }
}