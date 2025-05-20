import {
    ActiveGamesMessage, DrawRequestMessage, DrawRequestRejectedMessage,
    GameCreatedMessage, GameStatusChangedMessage,
    InitialStateMessage, NewMoveMessage,
    PlayerJoinedMessage,
    SessionStartMessage, TryCreateGameResultMessage,
    TryJoinGameResult
} from "@/WebSocket/Decoding";

type Handler<T> = (payload: T) => void;

class EventEmitter<T> {
    private handlers: Set<Handler<T>> = new Set();

    subscribe(handler: Handler<T>) {
        this.handlers.add(handler);
    }

    unsubscribe(handler: Handler<T>) {
        this.handlers.delete(handler);
    }

    emit(payload: T) {
        for (const handler of this.handlers) {
            handler(payload);
        }
    }
}

const gameCreatedEmitter = new EventEmitter<GameCreatedMessage>();
const initialStateEmitter = new EventEmitter<InitialStateMessage>();
const playerJoinedEmitter = new EventEmitter<PlayerJoinedMessage>();
const tryJoinGameResultEmitter = new EventEmitter<TryJoinGameResult>();
const activeGamesMessageEmitter = new EventEmitter<ActiveGamesMessage>();
const gameStatusChangedEmitter = new EventEmitter<GameStatusChangedMessage>();
const newMoveEmitter = new EventEmitter<NewMoveMessage>();
const drawRequestEmitter = new EventEmitter<DrawRequestMessage>();
const drawRequestRejectedEmitter = new EventEmitter<DrawRequestRejectedMessage>();
const tryCreateGameResultEmitter = new EventEmitter<TryCreateGameResultMessage>();

const WebSocketEvents = {
    initialStateEvent : initialStateEmitter,
    gameCreatedEvent : gameCreatedEmitter,
    playerJoinedEvent : playerJoinedEmitter,
    tryCreateGameResultEmitter : tryCreateGameResultEmitter,
    tryJoinGameResultEvent : tryJoinGameResultEmitter,
    activeGamesMessageEvent : activeGamesMessageEmitter,
    gameStatusChangedEvent : gameStatusChangedEmitter,
    newMoveEvent : newMoveEmitter,
    drawRequestEvent : drawRequestEmitter,
    drawRequestRejectedEvent : drawRequestRejectedEmitter,
} as const;

export default WebSocketEvents;
