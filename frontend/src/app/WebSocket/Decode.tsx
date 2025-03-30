// ==========================================================
// ========== WebSocket Message Decoding Utilities ==========
// ==========================================================

// --- Message Type Enum ---
// Mirrors the C# ToClientMessageType enum
export enum ToClientMessageType {
    // Make sure these values EXACTLY match your C# enum integer values
    SessionStartMessage = 0, // Assuming 0 from your example
    PlayerJoined = 1,
    GameHistoryUpdate = 2,
    GameInfoMessage = 3, // Added just in case it's needed standalone (adjust value if necessary)
    // Add other message types here
}

// --- Wrapper Interface ---
// Represents the decoded header/wrapper structure
export interface DecodedWrapper {
    versionId: number; // ushort
    type: ToClientMessageType; // Mapped enum value
    payloadSize: number; // ushort
    payload: Uint8Array; // The raw payload bytes
}

// --- Helper Functions ---

/** Helper to convert byte to hex */
function byteToHex(byte: number): string {
    return byte.toString(16).padStart(2, '0');
}

/**
 * Decodes a .NET Guid from its 16-byte representation (as produced by Guid.ToByteArray())
 * into a standard UUID string format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
 */
export function decodeGuid(guidBytes: Uint8Array): string {
    if (guidBytes.length !== 16) {
        throw new Error(`Invalid GUID byte array length. Expected 16, got ${guidBytes.length}.`);
    }
    const hex = (bytes: Uint8Array): string => Array.from(bytes).map(byteToHex).join('');
    const part1 = hex(guidBytes.slice(0, 4).reverse());
    const part2 = hex(guidBytes.slice(4, 6).reverse());
    const part3 = hex(guidBytes.slice(6, 8).reverse());
    const part4 = hex(guidBytes.slice(8, 10));
    const part5 = hex(guidBytes.slice(10, 16));
    return `${part1}-${part2}-${part3}-${part4}-${part5}`;
}

/**
 * Decodes the wrapper structure from an ArrayBuffer.
 */
export function decodeWrapper(arrayBuffer: ArrayBuffer): DecodedWrapper {
    const headerSize = 6; // versionId(2) + type(2) + payloadSize(2)
    if (arrayBuffer.byteLength < headerSize) {
        throw new Error(`Buffer too small for header. Need ${headerSize} bytes, got ${arrayBuffer.byteLength}.`);
    }

    const dataView = new DataView(arrayBuffer);
    const littleEndian = true; // Assume little-endian matching C# defaults

    const versionId = dataView.getUint16(0, littleEndian);
    const typeValue = dataView.getUint16(2, littleEndian);
    const payloadSize = dataView.getUint16(4, littleEndian);

    if (arrayBuffer.byteLength < headerSize + payloadSize) {
         throw new Error(`Buffer too small for declared payload size. Header says ${payloadSize}, remaining buffer is ${arrayBuffer.byteLength - headerSize}.`);
    }
     if (arrayBuffer.byteLength > headerSize + payloadSize) {
         // This might happen if messages are concatenated; only process the expected size
          console.warn(`Buffer larger than declared payload size. Header says ${payloadSize}, actual payload available is ${arrayBuffer.byteLength - headerSize}. Processing only declared size.`);
     }


    const payload = new Uint8Array(arrayBuffer, headerSize, payloadSize);

     if (!(typeValue in ToClientMessageType)) {
        console.warn(`Received unknown message type value: ${typeValue}`);
        // Assign the raw value but keep it typed as the enum for structure
     }
     const type = typeValue as ToClientMessageType;

    return { versionId, type, payloadSize, payload };
}


// --- Game Info Message Related Interfaces and Decoder ---

export enum GameStatus {
    WaitingForPlayer = 0, InProgress = 1, Player1Win = 2, Player2Win = 3, Draw = 4, Abandoned = 5
}
export interface CheckersMove { fromRow: number; fromCol: number; toRow: number; toCol: number; flags: number; }
const CHECKERS_MOVE_SIZE_BYTES = 5;
export interface PlayerInfo { userId: string; playerName: string; }
export interface GameInfoDecoded {
    gameId: string; status: GameStatus; gameName: string;
    player1: PlayerInfo; player2: PlayerInfo | null; gameHistory: CheckersMove[];
}

function readCheckersMove(view: DataView, offset: number): CheckersMove {
     if (offset + CHECKERS_MOVE_SIZE_BYTES > view.byteLength) {
        throw new Error("Buffer too short to read CheckersMove");
    }
    return {
        fromRow: view.getUint8(offset), fromCol: view.getUint8(offset + 1),
        toRow: view.getUint8(offset + 2), toCol: view.getUint8(offset + 3),
        flags: view.getUint8(offset + 4),
    };
}

// UPDATED decodeGameInfoMessage (now uses decodeGuid and takes Uint8Array)
function decodeGameInfoMessage(payload: Uint8Array): GameInfoDecoded {
    const view = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    const textDecoder = new TextDecoder('utf-8');
    let offset = 0;

    try {
        // GameId (16 bytes)
        if (payload.byteLength < offset + 16) throw new Error("Buffer too short for GameInfo.GameId");
        const gameId = decodeGuid(payload.slice(offset, offset + 16));
        offset += 16;

        // Status (1 byte)
        if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.Status");
        const status: GameStatus = view.getUint8(offset++);

        // GameName (1 byte length + N bytes)
        if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.GameName length");
        const gameNameLength = view.getUint8(offset++);
        if (payload.byteLength < offset + gameNameLength) throw new Error("Buffer too short for GameInfo.GameName content");
        const gameName = textDecoder.decode(new Uint8Array(payload.buffer, payload.byteOffset + offset, gameNameLength));
        offset += gameNameLength;

        // Player1Id (16 bytes)
        if (payload.byteLength < offset + 16) throw new Error("Buffer too short for GameInfo.Player1Id");
        const player1Id = decodeGuid(payload.slice(offset, offset + 16));
        offset += 16;

        // Player1Name (1 byte length + N bytes)
        if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.Player1Name length");
        const player1NameLength = view.getUint8(offset++);
        if (payload.byteLength < offset + player1NameLength) throw new Error("Buffer too short for GameInfo.Player1Name content");
        const player1Name = textDecoder.decode(new Uint8Array(payload.buffer, payload.byteOffset + offset, player1NameLength));
        offset += player1NameLength;
        const player1: PlayerInfo = { userId: player1Id, playerName: player1Name };

        // HasPlayer2 flag (1 byte)
        if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.HasPlayer2 flag");
        const hasPlayer2 = view.getUint8(offset++);

        let player2: PlayerInfo | null = null;
        if (hasPlayer2 === 1) {
            // Player2Id (16 bytes)
            if (payload.byteLength < offset + 16) throw new Error("Buffer too short for GameInfo.Player2Id");
            const player2Id = decodeGuid(payload.slice(offset, offset + 16));
            offset += 16;

            // Player2Name (1 byte length + N bytes)
            if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.Player2Name length");
            const player2NameLength = view.getUint8(offset++);
            if (payload.byteLength < offset + player2NameLength) throw new Error("Buffer too short for GameInfo.Player2Name content");
            const player2Name = textDecoder.decode(new Uint8Array(payload.buffer, payload.byteOffset + offset, player2NameLength));
            offset += player2NameLength;
            player2 = { userId: player2Id, playerName: player2Name };
        }

        // GameHistory Count (1 byte)
        if (payload.byteLength < offset + 1) throw new Error("Buffer too short for GameInfo.GameHistory count");
        const gameHistoryCount = view.getUint8(offset++);

        const gameHistory: CheckersMove[] = [];
        if (gameHistoryCount > 0) {
            const expectedHistoryBytesLength = gameHistoryCount * CHECKERS_MOVE_SIZE_BYTES;
            if (payload.byteLength < offset + expectedHistoryBytesLength) {
                throw new Error("Buffer too short for GameInfo.GameHistory content");
            }
             const historyView = new DataView(payload.buffer, payload.byteOffset + offset, expectedHistoryBytesLength);
             let historyOffset = 0;
            for (let i = 0; i < gameHistoryCount; i++) {
                gameHistory.push(readCheckersMove(historyView, historyOffset));
                historyOffset += CHECKERS_MOVE_SIZE_BYTES;
            }
            offset += expectedHistoryBytesLength;
        }

        if (offset !== payload.byteLength) {
             console.warn(`Decoding GameInfoMessage finished with ${payload.byteLength - offset} bytes remaining within its payload.`);
        }

        return { gameId, status, gameName, player1, player2, gameHistory };

    } catch (e) {
        console.error("Failed to decode GameInfoMessage:", e);
        throw new Error(`Failed to decode GameInfoMessage payload: ${e instanceof Error ? e.message : String(e)}`); // Rethrow with context
    }
}


// --- Session Start Message Related Interfaces and Decoder ---

// Interface for the decoded SessionStartMessage payload
export interface SessionStartMessageDecoded {
    sessionId: string; // GUID represented as a string
    isInGame: boolean;
    gameInfo: GameInfoDecoded | null; // Decoded GameInfo or null
}

/**
 * Decodes the payload for a SessionStartMessage.
 * Assumes payload structure: Guid (16) + isInGame (1) + [Optional GameInfo]
 * @param payload The raw payload bytes (Uint8Array) from the wrapper.
 * @returns The decoded SessionStartMessage object.
 */
export function decodeSessionStartMessage(payload: Uint8Array): SessionStartMessageDecoded {
    const minLength = 17; // 16 (Guid) + 1 (bool)
    if (payload.byteLength < minLength) {
        throw new Error(`Invalid SessionStartMessage payload length. Expected at least ${minLength} bytes, got ${payload.byteLength}.`);
    }

    const view = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    let offset = 0;

    try {
        // 1. Decode SessionId (16 bytes)
        const sessionIdBytes = payload.slice(offset, offset + 16);
        const sessionId = decodeGuid(sessionIdBytes);
        offset += 16;

        // 2. Decode IsInGame (1 byte)
        const isInGameByte = view.getUint8(offset++);
        const isInGame = isInGameByte === 1;

        // 3. Decode optional GameInfo
        let gameInfo: GameInfoDecoded | null = null;
        const remainingBytes = payload.byteLength - offset;

        if (isInGame && remainingBytes > 0) {
            // If flag is true and there are bytes left, they *must* be the GameInfo
            const gameInfoPayload = payload.slice(offset); // Get the rest of the payload
            gameInfo = decodeGameInfoMessage(gameInfoPayload); // Decode it
            // We don't need to advance offset here as slice created a new view,
            // and decodeGameInfoMessage consumes its entire payload.
            // We *could* add a check: offset + gameInfoPayload.byteLength === payload.byteLength
        } else if (isInGame && remainingBytes === 0) {
            // isInGame is true, but server sent no GameInfo bytes (e.g., GameInfo was null)
             console.warn("SessionStartMessage indicates 'isInGame' but contains no GameInfo payload.");
             gameInfo = null; // Explicitly null
        } else if (!isInGame && remainingBytes > 0) {
             // isInGame is false, but there's extra data. Log a warning.
             console.warn(`SessionStartMessage indicates 'not in game' but has ${remainingBytes} unexpected extra bytes.`);
             // We ignore the extra bytes according to the implied format
        }
        // else (!isInGame && remainingBytes === 0) -> Correct case, do nothing

        return { sessionId, isInGame, gameInfo };

     } catch (e) {
         console.error("Failed to decode SessionStartMessage:", e);
        throw new Error(`Failed to decode SessionStartMessage payload: ${e instanceof Error ? e.message : String(e)}`);
     }
}


// --- Other Message Decoders (Placeholders/Your Implementations) ---

export interface DecodedCheckersMove { fromIndex: number; toIndex: number; promoted: boolean; capturedPieces: bigint; }
export interface GameHistoryUpdateMessage { moves: DecodedCheckersMove[]; }
const CHECKERS_MOVE_SERIALIZED_SIZE = 11; // 1+1+1+8

export function decodeGameHistoryUpdate(payload: Uint8Array): GameHistoryUpdateMessage {
     if (payload.byteLength % CHECKERS_MOVE_SERIALIZED_SIZE !== 0) {
        throw new Error(`Invalid GameHistoryUpdate payload length. Expected multiple of ${CHECKERS_MOVE_SERIALIZED_SIZE}, but got ${payload.byteLength}.`);
    }
    const moves: DecodedCheckersMove[] = [];
    const moveCount = payload.byteLength / CHECKERS_MOVE_SERIALIZED_SIZE;
    const dataView = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    const littleEndian = true;

    for (let i = 0; i < moveCount; i++) {
        const offset = i * CHECKERS_MOVE_SERIALIZED_SIZE;
        const fromIndex = dataView.getUint8(offset + 0);
        const toIndex = dataView.getUint8(offset + 1);
        const promotedByte = dataView.getUint8(offset + 2);
        const capturedPieces = dataView.getBigUint64(offset + 3, littleEndian);
        moves.push({ fromIndex, toIndex, promoted: promotedByte !== 0, capturedPieces });
    }
    return { moves };
}

export interface PlayerJoinedMessage { userId: string; userName: string; }

export function decodePlayerJoined(payload: Uint8Array): PlayerJoinedMessage {
    const GUID_LENGTH = 16;
    const LENGTH_PREFIX_LENGTH = 2; // ushort for name length
    const MIN_EXPECTED_LENGTH = GUID_LENGTH + LENGTH_PREFIX_LENGTH;

    if (payload.byteLength < MIN_EXPECTED_LENGTH) {
        throw new Error(`Invalid PlayerJoined payload length. Expected >= ${MIN_EXPECTED_LENGTH}, got ${payload.byteLength}.`);
    }

    const dataView = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    const littleEndian = true;
    const textDecoder = new TextDecoder('utf-8');

    const userId = decodeGuid(payload.slice(0, GUID_LENGTH));
    const userNameLength = dataView.getUint16(GUID_LENGTH, littleEndian);
    const expectedTotalLength = MIN_EXPECTED_LENGTH + userNameLength;

    if (payload.byteLength < expectedTotalLength) {
        throw new Error(`Invalid PlayerJoined payload. Declared username length ${userNameLength} exceeds buffer size.`);
    }

    const userNameBytes = payload.slice(MIN_EXPECTED_LENGTH, expectedTotalLength);
    const userName = textDecoder.decode(userNameBytes);

     if (payload.byteLength > expectedTotalLength) {
         console.warn(`PlayerJoined payload has ${payload.byteLength - expectedTotalLength} extra bytes.`);
     }


    return { userId, userName };
}

// --- Union Type for Decoded Messages ---
// Add all specific decoded message types here
export type DecodedMessagePayload =
    | SessionStartMessageDecoded
    | PlayerJoinedMessage
    | GameHistoryUpdateMessage
    | GameInfoDecoded // If GameInfo can be sent standalone
    | { error: string; rawPayload: Uint8Array } // Generic error object
    | null; // Or represent unknown types differently


// --- Main Message Dispatcher ---

/**
 * Decodes a full WebSocket message (wrapper + typed payload).
 * @param arrayBuffer The raw ArrayBuffer received from the WebSocket.
 * @returns An object containing the decoded message type and the specific decoded message data,
 *          or an error object/null if decoding fails or type is unknown.
 */
export function decodeMessage(arrayBuffer: ArrayBuffer): { versionId: number; type: ToClientMessageType; message: DecodedMessagePayload } {
    let wrapper: DecodedWrapper;
    try {
        wrapper = decodeWrapper(arrayBuffer);
    } catch (error) {
        console.error("Failed to decode message wrapper:", error);
        // Return a structured error maybe?
        return { versionId: -1, type: -1 as ToClientMessageType, message: { error: `Wrapper decode failed: ${error instanceof Error ? error.message : String(error)}`, rawPayload: new Uint8Array(arrayBuffer) } };
    }

    let messageData: DecodedMessagePayload = null;

    try {
        switch (wrapper.type) {
            case ToClientMessageType.SessionStartMessage:
                messageData = decodeSessionStartMessage(wrapper.payload);
                break;

            case ToClientMessageType.PlayerJoined:
                messageData = decodePlayerJoined(wrapper.payload);
                break;

            case ToClientMessageType.GameHistoryUpdate:
                messageData = decodeGameHistoryUpdate(wrapper.payload);
                break;

            // Example if GameInfoMessage could be sent standalone:
            // case ToClientMessageType.GameInfoMessage:
            //     messageData = decodeGameInfoMessage(wrapper.payload);
            //     break;

            default:
                console.error(`Unknown or unsupported message type received: ${wrapper.type}`);
                // Return an error payload containing the raw data for inspection
                messageData = { error: `Unknown message type: ${wrapper.type}`, rawPayload: wrapper.payload };
                break;
        }

        return {
            versionId: wrapper.versionId,
            type: wrapper.type,
            message: messageData,
        };

    } catch (error) {
        console.error(`Failed to decode payload for message type ${wrapper.type}:`, error);
        // Return specific error related to payload decoding
        return {
             versionId: wrapper.versionId,
             type: wrapper.type,
             message: { error: `Payload decode failed for type ${wrapper.type}: ${error instanceof Error ? error.message : String(error)}`, rawPayload: wrapper.payload }
        };
    }
}

// --- Example Usage ---
/*
const ws = new WebSocket("ws://your_server_address");
ws.binaryType = "arraybuffer"; // Important!

ws.onmessage = (event) => {
    if (event.data instanceof ArrayBuffer) {
        const decoded = decodeMessage(event.data);

        console.log("Received message:", decoded);

        if (decoded.message && !(decoded.message as any).error) { // Check if it's not an error object
            switch (decoded.type) {
                case ToClientMessageType.SessionStartMessage:
                    const sessionMsg = decoded.message as SessionStartMessageDecoded;
                    console.log("Session Started. ID:", sessionMsg.sessionId);
                    if (sessionMsg.isInGame && sessionMsg.gameInfo) {
                        console.log("Currently in game:", sessionMsg.gameInfo.gameName);
                        console.log("Player 1:", sessionMsg.gameInfo.player1.playerName);
                         console.log("Player 2:", sessionMsg.gameInfo.player2?.playerName ?? "N/A");
                    } else if (sessionMsg.isInGame) {
                         console.log("Currently in game, but waiting for game details.");
                    }
                    else {
                         console.log("Not currently in a game.");
                    }
                    break;

                case ToClientMessageType.PlayerJoined:
                    const joinedMsg = decoded.message as PlayerJoinedMessage;
                    console.log(`Player Joined: ${joinedMsg.userName} (ID: ${joinedMsg.userId})`);
                    // Update UI, etc.
                    break;

                case ToClientMessageType.GameHistoryUpdate:
                    const historyMsg = decoded.message as GameHistoryUpdateMessage;
                    console.log("Game History Updated:", historyMsg.moves);
                    // Update game board, etc.
                    break;

                // Add cases for other message types
            }
        } else if (decoded.message) {
             // Handle decoding errors reported in the message object
             console.error("Decoding error:", (decoded.message as any).error);
        }


    } else {
        console.log("Received non-binary message:", event.data);
    }
};

ws.onerror = (error) => {
    console.error("WebSocket Error:", error);
};

ws.onopen = () => {
    console.log("WebSocket connection established.");
    // Send login message, etc.
};

ws.onclose = (event) => {
    console.log("WebSocket connection closed:", event.code, event.reason);
};
*/