import {
    ActiveGamesMessage,
    CreateGameResultMessage,
    GameInfoMessage,
    PlayerJoinedMessage,
    SessionStartMessage,
    TryJoinGameResult
} from "@/app/WebSocket/Decoding";

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

const sessionStartEmitter = new EventEmitter<SessionStartMessage>();
const createGameResultEmitter = new EventEmitter<CreateGameResultMessage>();
const gameInfoEmitter = new EventEmitter<GameInfoMessage>();
const playerJoinedEmitter = new EventEmitter<PlayerJoinedMessage>();
const tryJoinGameResultEmitter = new EventEmitter<TryJoinGameResult>();
const activeGamesMessageEmitter = new EventEmitter<ActiveGamesMessage>();

const Subscriptions = {
    sessionStartMessageEvent : sessionStartEmitter,
    createGameResultEvent : createGameResultEmitter,
    gameInfoEvent : gameInfoEmitter,
    playerJoinedEvent : playerJoinedEmitter,
    tryJoinGameResultEvent : tryJoinGameResultEmitter,
    activeGamesMessageEvent : activeGamesMessageEmitter,
} as const;

export default Subscriptions;
