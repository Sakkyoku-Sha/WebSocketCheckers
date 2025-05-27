import {ByteReader, CheckersMove} from "./ByteReader";

export enum FromServerMessageType
{
    //Initial Connection Message 
    SessionStartMessage = 0,

    //Initial State for Connected Client 
    InitialStateMessage = 1,

    //Updating All User Messages
    GameCreatedMessage = 2,

    //Querying Responses 
    ActiveGamesResponse = 3,
    TryCreateGameResponse = 4,
    TryJoinGameResponse = 5,

    //Game State Updates 
    NewMove = 7,
    GameStatusChanged = 8,
    DrawRequest = 9,
    DrawRequestRejected = 10,
    PlayerJoined = 11,
}

export enum GameStatus
{
    NoPlayers = 0,
    WaitingForPlayers = 1,
    InProgress = 2,
    Abandoned = 3,
    Player1Win = 4,
    Player2Win = 5,
    Draw = 6,
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
    
    gameTimeStartMs : bigint, 
    player1RemainingTimeMs: number, //casted down since this should never be more than a few hours 
    player2RemainingTimeMs: number,
    
    forcedMoves : ForcedMove[]; 
}

export interface GameCreatedMessage extends WebSocketMessage {
    GameMetaData : GameMetaData
}
export interface GameMetaData{
    gameId : number,
    player1Name : string,
    player2Name : string,
}
export interface ActiveGamesMessage extends WebSocketMessage {
    activeGames : GameMetaData[]
}

export interface InitialStateMessage extends WebSocketMessage {
    activeGamesCount : number,
    activeGames : GameMetaData[]
    gameInfo : GameInfo | null,
}

export type DecodeResult = WebSocketMessage | 
                    SessionStartMessage |
                    PlayerJoinedMessage |
                    InitialStateMessage |
                    TryJoinGameResult |
                    GameCreatedMessage | 
                    ActiveGamesMessage | 
                    TryCreateGameResultMessage |
                    NewMoveMessage | 
                    GameStatusChangedMessage |
                    DrawRequestMessage |
                    DrawRequestRejectedMessage;

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

export function decodeForcedMoves(byteReader: ByteReader) : ForcedMove[] {
    
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

function decodeGameInfo(byteReader: ByteReader) : GameInfo {
    
    const gameId = byteReader.readUint32();
    const gameStatus = byteReader.readUint8() as GameStatus;
    const gameName = byteReader.readLengthPrefixedStringUTF16LE();
    
    const player1Id = byteReader.readGuid();
    const player1Name = byteReader.readLengthPrefixedStringUTF16LE(); 
    
    const player2Id = byteReader.readGuid();
    const player2Name = byteReader.readLengthPrefixedStringUTF16LE();
    
    const historyCount = byteReader.readUint16();
    const history = byteReader.readTimedCheckersMoves(historyCount);
    
    const gameTimeStartMs = byteReader.readInt64();
    const player1RemainingTime = byteReader.readUint32();
    const player2RemainingTime = byteReader.readUint32();
    
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
        
        gameTimeStartMs : gameTimeStartMs,
        player1RemainingTimeMs: player1RemainingTime,
        player2RemainingTimeMs: player2RemainingTime,
        
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
        type : FromServerMessageType.TryJoinGameResponse,
        version : version,
        didJoinGame : Boolean(byteReader.readUint8()),
        gameInfo : gameInfoMessage,
    };
}

function decodeGameCreated(byteReader: ByteReader, version: number) : GameCreatedMessage {

    const gameMetaData = decodeGamesMetaData(byteReader);
    return {
        type : FromServerMessageType.GameCreatedMessage,
        version : version,
        GameMetaData : gameMetaData[0],
    };
}

function decodeActiveGamesMessage(byteReader: ByteReader, version: number) : ActiveGamesMessage {
    return {
        type: FromServerMessageType.ActiveGamesResponse,
        version: version,
        activeGames: decodeGamesMetaData(byteReader),
    }
}
function decodeGamesMetaData(byteReader: ByteReader) : GameMetaData[] {
    
    let activeGamesLength = byteReader.readUint16();
    let activeGames = Array<GameMetaData>(activeGamesLength);
    
    for(let i = 0; i < activeGamesLength; i++){
        activeGames[i] = decodeGameMetaData(byteReader);
    }
    
    return activeGames;
}

function decodeGameMetaData(byteReader: ByteReader) : GameMetaData {
    const gameId = byteReader.readUint32();
    const player1Name = byteReader.readLengthPrefixedStringUTF16LE();
    const player2Name = byteReader.readLengthPrefixedStringUTF16LE();
    return  {
        gameId : gameId,
        player1Name : player1Name,
        player2Name : player2Name
    }
}

function decodeInitialServerMessage(byteReader: ByteReader, version: number): DecodeResult {
    
    const activeGames = decodeGamesMetaData(byteReader);

    let gameInfo : GameInfo | null = null;
    let wasInGame = byteReader.readUint8(); 
    
    if(wasInGame === 1 && byteReader.bytesRemaining > 0) {
        gameInfo = decodeGameInfo(byteReader);
    }
    
    return {
        type : FromServerMessageType.InitialStateMessage,
        version : version,
        activeGamesCount : activeGames.length,
        activeGames : activeGames,
        gameInfo : gameInfo,
    }
}

export interface TryCreateGameResultMessage extends WebSocketMessage {
    gameId : number
}
function decodeTryCreateGameResult(byteReader: ByteReader, version: number) : TryCreateGameResultMessage {
    return {
        type : FromServerMessageType.TryCreateGameResponse,
        version : version,
        gameId : byteReader.readUint32(),
    }
}

export interface NewMoveMessage extends WebSocketMessage {
    move : CheckersMove,
    forcedMovesInPosition : ForcedMove[], //array of bitboard indices
}
function decodeNewMoveMessage(byteReader: ByteReader, version: number) : NewMoveMessage {

    const move = byteReader.readTimedCheckersMove();
    const forcedMovesInPosition = decodeForcedMoves(byteReader);
    return {
        type : FromServerMessageType.NewMove,
        version : version,
        move : move,
        forcedMovesInPosition : forcedMovesInPosition,
    };
}

export interface GameStatusChangedMessage extends WebSocketMessage {
    gameStatus : GameStatus,
}
function decodeGameStatusChanged(byteReader: ByteReader, version: number) : GameStatusChangedMessage  {
    const gameStatus = byteReader.readUint8() as GameStatus;
    return {
        type : FromServerMessageType.GameStatusChanged,
        version : version,
        gameStatus : gameStatus,
    };
}


export interface DrawRequestMessage extends WebSocketMessage {
}
function decodeDrawRequest(byteReader: ByteReader, version: number) : DrawRequestMessage  {
    return {
        type : FromServerMessageType.DrawRequest,
        version : version,
    };
}

export interface DrawRequestRejectedMessage extends WebSocketMessage {
}
function decodeDrawRequestRejected(byteReader: ByteReader, version: number): DrawRequestRejectedMessage {
    return {
        type : FromServerMessageType.DrawRequestRejected,
        version : version,
    };
}

export function decode(arrayBuffer : ArrayBuffer) : DecodeResult {
    
    const byteReader = new ByteReader(arrayBuffer);
    const version = byteReader.readUint16();
    const type = byteReader.readUint16() as FromServerMessageType;
    
    switch (type) {
        case FromServerMessageType.NewMove:
            return decodeNewMoveMessage(byteReader, version);
        case FromServerMessageType.GameStatusChanged:
            return decodeGameStatusChanged(byteReader, version);
        case FromServerMessageType.DrawRequest:
            return decodeDrawRequest(byteReader, version);
        case FromServerMessageType.DrawRequestRejected:
            return decodeDrawRequestRejected(byteReader, version);
        case FromServerMessageType.SessionStartMessage:
            return decodeSessionStartMessage(byteReader , version);
        case FromServerMessageType.PlayerJoined:
            return decodePlayerJoined(byteReader, version);
        case FromServerMessageType.InitialStateMessage:
            return decodeInitialServerMessage(byteReader, version);
        case FromServerMessageType.TryJoinGameResponse:
            return decodeTryJoinGameResult(byteReader, version);
        case FromServerMessageType.GameCreatedMessage:
            return decodeGameCreated(byteReader, version);
        case FromServerMessageType.ActiveGamesResponse: 
            return decodeActiveGamesMessage(byteReader, version);
        case FromServerMessageType.TryCreateGameResponse:    
            return decodeTryCreateGameResult(byteReader, version);
            
        default: throw new Error("Unknown type '" + type + "'");   
    }
}


