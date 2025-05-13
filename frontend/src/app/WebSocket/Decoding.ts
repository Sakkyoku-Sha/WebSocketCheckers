import {ByteReader, CheckersMove} from "./ByteReader";

export enum FromServerMessageType
{
    SessionStartMessage = 0,
    PlayerJoined = 1,
    NewMoveMessage = 2,
    InitialServerMessage = 3,
    TryJoinGameResultMessage = 4,
    CreateGameResultMessage = 5,
    ActiveGamesMessage = 6,
}

export enum GameStatus
{
    NoPlayers = 0, 
    WaitingForPlayers = 2,
    InProgress = 3,
    Finished = 4,
    Abandoned = 5
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
   move : CheckersMove,
   forcedMovesInPosition : ForcedMove[], //array of bitboard indices 
}
export interface TryJoinGameResult extends WebSocketMessage {
    didJoinGame : boolean
    gameInfo : GameInfo | null,
}
export interface GameInfo {
    gameId : number,
    gameStatus : GameStatus,
    gameName : string,
    
    player1Id : string,
    player1Name : string,
    
    player2Id : string,
    player2Name : string,
    
    historyCount : number,
    history : Array<CheckersMove>
    
    forcedMoves : ForcedMove[]; 
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

export interface InitialServerMessage extends WebSocketMessage {
    activeGamesCount : number,
    activeGames : GameMetaData[]
    gameInfo : GameInfo | null,
}

type DecodeResult = WebSocketMessage | 
                    SessionStartMessage |
                    PlayerJoinedMessage | 
                    NewMoveMessage |    
                    InitialServerMessage |
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
    const playerName = byteReader.readLengthPrefixedStringUTF16LE();
    
    return {
        type : FromServerMessageType.PlayerJoined,
        version : version,
        playerId : playerId,
        playerName : playerName
    }
}

function decodeForcedMoves(byteReader: ByteReader) : ForcedMove[] {
    
    const forcedMovesInPositionCount = byteReader.readUint8();
    const forcedMovesInPosition = new Array<ForcedMove>(forcedMovesInPositionCount);
    for (let i = 0; i < forcedMovesInPositionCount; i++) {
        forcedMovesInPosition[i] = {
            initialPosition : byteReader.readInt32(),
            finalPosition : byteReader.readInt32()
        }
    }
    return forcedMovesInPosition;
}
export interface ForcedMove {
    initialPosition : number,
    finalPosition: number,
}


function decodeNewMoveMessage(byteReader: ByteReader, version: number) :  NewMoveMessage {
    
    const move = byteReader.readCheckersMove();
    const forcedMovesInPosition = decodeForcedMoves(byteReader);

    return {
        type : FromServerMessageType.NewMoveMessage,
        version : version,
        move : move,
        forcedMovesInPosition : forcedMovesInPosition,
    };
}

function decodeGameInfo(byteReader: ByteReader) : GameInfo {
    
    const gameId = byteReader.readInt32();
    const gameStatus = byteReader.readUint8() as GameStatus;
    const gameName = byteReader.readLengthPrefixedStringUTF16LE();
    
    const player1Id = byteReader.readGuid();
    const player1Name = byteReader.readLengthPrefixedStringUTF16LE(); 
    
    const player2Id = byteReader.readGuid();
    const player2Name = byteReader.readLengthPrefixedStringUTF16LE();
    
    const historyCount = byteReader.readUint16();
    const history = byteReader.readCheckersMoves(historyCount);
    
    const forcedMoves = decodeForcedMoves(byteReader);
    
    return {
        
        gameId : gameId,
        gameStatus : gameStatus,
        gameName : gameName,
        
        player1Id : player1Id,
        player1Name : player1Name,
        
        player2Id : player2Id,
        player2Name : player2Name,
        
        historyCount : historyCount,
        history : history,
        
        forcedMoves : forcedMoves, 
    }
}

function decodeTryJoinGameResult(byteReader: ByteReader, version: number) : TryJoinGameResult {
    
    const didJoinGame = byteReader.readUint8();
    let gameInfoMessage : GameInfo | null = null;
    if(didJoinGame) {
        gameInfoMessage = decodeGameInfo(byteReader);
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
    return {
        type: FromServerMessageType.ActiveGamesMessage,
        version: version,
        activeGames: decodeGameMetaData(byteReader),
    }
}
function decodeGameMetaData(byteReader: ByteReader) : GameMetaData[] {
    
    let activeGamesLength = byteReader.readUint16();
    let activeGames = Array<GameMetaData>(activeGamesLength);
    
    for(let i = 0; i < activeGamesLength; i++){
        const gameId = byteReader.readInt32();
        const player1Name = byteReader.readLengthPrefixedStringUTF16LE();
        const player2Name = byteReader.readLengthPrefixedStringUTF16LE();
        activeGames[i] = {
            gameId : gameId,
            player1Name : player1Name,
            player2Name : player2Name
        }
    }
    
    return activeGames;
}

function decodeInitialServerMessage(byteReader: ByteReader, version: number): DecodeResult {
    
    const activeGames = decodeGameMetaData(byteReader);

    let gameInfo : GameInfo | null = null;
    let wasInGame = byteReader.readUint8(); 
    
    if(wasInGame === 1 && byteReader.bytesRemaining > 0) {
        gameInfo = decodeGameInfo(byteReader);
    }
    
    return {
        type : FromServerMessageType.InitialServerMessage,
        version : version,
        activeGamesCount : activeGames.length,
        activeGames : activeGames,
        gameInfo : gameInfo,
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
        case FromServerMessageType.InitialServerMessage:
            return decodeInitialServerMessage(byteReader, version);
        case FromServerMessageType.TryJoinGameResultMessage:
            return decodeTryJoinGameResult(byteReader, version);
        case FromServerMessageType.CreateGameResultMessage:
            return decodeCreateGameResult(byteReader, version);
        case FromServerMessageType.ActiveGamesMessage: 
            return decodeActiveGamesMessage(byteReader, version);
            
            
        default: throw new Error("Unknown type '" + type + "'");   
    }
}


