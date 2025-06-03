import {
    ActiveGamesMessage,
    decode,
    DecodeResult, FailedMoveMessage,
    FromServerMessageType,
    GameCreatedMessage,
    GameStatusChangedMessage,
    InitialStateMessage,
    NewMoveMessage,
    PlayerJoinedMessage,
    TryCreateGameResultMessage,
    TryJoinGameResult
} from "@/WebSocket/Decoding";
import {
    encodeActiveGamesMessage,
    encodeCreateGameMessage,
    encodeDrawRequestMessage,
    encodeDrawRequestResponse,
    encodeIdentifyUserMessage,
    encodeSurrenderMessage,
    encodeTryJoinGameMessage,
    encodeTryMakeMoveMessage
} from "@/WebSocket/Encode";
import WebSocketEvents from "@/WebSocket/WebSocketEvents";
import {unstable_batchedUpdates} from "react-dom";

let socket: WebSocket | null = null;
let reconnectAttempts = 0;

export function connectWebSocket(url: string) {
    
    socket = new WebSocket(url);
    socket.binaryType = "arraybuffer";
    
    socket.onopen = () => {
        console.log('WebSocket connected');
        reconnectAttempts = 0; // Reset attempts on successful connection
    };

    socket.onmessage = (event) => {
        HandleWebSocketData(event.data as ArrayBuffer);
    };

    socket.onclose = () => {
        console.log('WebSocket disconnected');
        attemptReconnect(url);
    };

    socket.onerror = (error) => {
        console.error('WebSocket error:', error);
        socket?.close(); // Ensure the connection is closed on error
    };
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

function attemptReconnect(url: string) {
    if (reconnectAttempts < 10) { // Limit the number of attempts
        const delay = Math.min(1000 * 2 ** reconnectAttempts, 30000); // Exponential backoff
        reconnectAttempts++;
        console.log(`Reconnecting in ${delay / 1000} seconds...`);
        setTimeout(() => connectWebSocket(url), delay);
    } else {
        console.error('Max reconnect attempts reached');
    }
}

const HandleWebSocketData = (byteData : ArrayBuffer) => {

    const resultingMessage = decode(byteData)
    
    //Avoid unnecessary re-renders caused by multiple subscribers to the same events
    unstable_batchedUpdates(() => {
        ExecuteMessageEmit(resultingMessage);
    })
}

function ExecuteMessageEmit(resultingMessage : DecodeResult) {
    
    switch(resultingMessage.type) {

        //Special case for session start message
        case FromServerMessageType.SessionStartMessage:

            //Send User Id to server for it coorelate this Id to it's socket 
            const newUserId = ResolveUserId();
            const toSend = encodeIdentifyUserMessage(newUserId);

            if(socket !== null) {
                socket.send(toSend);
            }

            break;

        case FromServerMessageType.InitialStateMessage:
            WebSocketEvents.initialStateEvent.emit(resultingMessage as InitialStateMessage);
            break;

        case FromServerMessageType.TryJoinGameResponse:
            WebSocketEvents.tryJoinGameResultEvent.emit(resultingMessage as TryJoinGameResult);
            break;

        case FromServerMessageType.NewMove:
            WebSocketEvents.newMoveEvent.emit(resultingMessage as NewMoveMessage);
            break;

        case FromServerMessageType.GameStatusChanged:
            WebSocketEvents.gameStatusChangedEvent.emit(resultingMessage as GameStatusChangedMessage);
            break;

        case FromServerMessageType.DrawRequest:
            WebSocketEvents.drawRequestEvent.emit(resultingMessage as GameStatusChangedMessage);
            break;

        case FromServerMessageType.DrawRequestRejected:
            WebSocketEvents.drawRequestRejectedEvent.emit(resultingMessage as GameStatusChangedMessage);
            break;

        case FromServerMessageType.PlayerJoined:
            WebSocketEvents.playerJoinedEvent.emit(resultingMessage as PlayerJoinedMessage);
            break;

        case FromServerMessageType.GameCreatedMessage:
            WebSocketEvents.gameCreatedEvent.emit(resultingMessage as GameCreatedMessage);
            break;

        case FromServerMessageType.ActiveGamesResponse:
            WebSocketEvents.activeGamesMessageEvent.emit(resultingMessage as ActiveGamesMessage);
            break;

        case FromServerMessageType.TryCreateGameResponse:
            WebSocketEvents.tryCreateGameResultEmitter.emit(resultingMessage as TryCreateGameResultMessage);
            break;
            
        case FromServerMessageType.FailedMove:
            WebSocketEvents.failedMoveEvent.emit(resultingMessage as FailedMoveMessage);
            break;
        default:
            console.error("Unknown message type:", resultingMessage?.type);
    }
}

export const WebSocketSend ={
    tryCreateNewGame : () => socket?.send(encodeCreateGameMessage()),
    refreshActiveGames : () => socket?.send(encodeActiveGamesMessage()),
    tryMakeMove : (fromIndex : number, toIndex : number) => {
        socket?.send(encodeTryMakeMoveMessage(fromIndex, toIndex))
    },
    tryJoinGame : (gameId : number) => {
        socket?.send(encodeTryJoinGameMessage(gameId))
    },
    drawRequest : () => socket?.send(encodeDrawRequestMessage()),
    drawRequestResponse : (accept : boolean) => socket?.send(encodeDrawRequestResponse(accept)),
    surrender : () => socket?.send(encodeSurrenderMessage()),
}



