// 1. Mirror the C# Enum
export enum ToClientMessageType {
    SessionStartMessage = 0,
    PlayerJoined = 1,
    GameHistoryUpdate = 2,
    // Add other message types here if needed
}

// 2. Interface for the decoded wrapper
export interface DecodedWrapper {
    versionId: number; // ushort
    type: ToClientMessageType; // Mapped enum value
    payloadSize: number; // ushort
    payload: Uint8Array; // The raw payload bytes
}

// 3. Interface for the decoded SessionStartMessage
export interface SessionStartMessage {
    sessionId: string; // GUID represented as a string
}

// --- Decoding Functions ---

/**
 * Decodes the wrapper structure from an ArrayBuffer.
 * Assumes little-endian byte order for header fields.
 * @param arrayBuffer The raw ArrayBuffer received from the WebSocket.
 * @returns The decoded wrapper structure.
 * @throws Error if buffer is too small for the header.
 */
export function decodeWrapper(arrayBuffer: ArrayBuffer): DecodedWrapper {
    if (arrayBuffer.byteLength < 6) {
        throw new Error(`Buffer too small for header. Need 6 bytes, got ${arrayBuffer.byteLength}.`);
    }

    const dataView = new DataView(arrayBuffer);
    const littleEndian = true; // C# MemoryMarshal usually uses native (little) endian

    const versionId = dataView.getUint16(0, littleEndian);
    const typeValue = dataView.getUint16(2, littleEndian);
    const payloadSize = dataView.getUint16(4, littleEndian);

    // Basic validation: Does the declared payload size match the remaining buffer?
    if (arrayBuffer.byteLength < 6 + payloadSize) {
         throw new Error(`Buffer too small for declared payload size. Header says ${payloadSize}, remaining buffer is ${arrayBuffer.byteLength - 6}.`);
    }

    // Extract payload as a Uint8Array
    // new Uint8Array(buffer, byteOffset, length)
    const payload = new Uint8Array(arrayBuffer, 6, payloadSize);

     // Check if the numeric type value exists in our enum
     if (!(typeValue in ToClientMessageType)) {
        console.warn(`Received unknown message type value: ${typeValue}`);
        // You might want to throw an error or handle this differently
        // For now, let's assign a specific 'unknown' value or keep the number
     }
     const type = typeValue as ToClientMessageType; // Cast, potentially unsafe if value is invalid

    return {
        versionId,
        type,
        payloadSize,
        payload,
    };
}

/**
 * Helper function to convert a byte to a 2-digit hex string.
 */
function byteToHex(byte: number): string {
    return byte.toString(16).padStart(2, '0');
}

/**
 * Decodes a .NET Guid from its 16-byte representation into a standard string format.
 * Handles the specific byte order produced by C# Guid.ToByteArray().
 * Format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
 * C# Guid Bytes Order:
 *   Bytes 0-3:   Data1 (int, needs reversal for standard string format)
 *   Bytes 4-5:   Data2 (short, needs reversal)
 *   Bytes 6-7:   Data3 (short, needs reversal)
 *   Bytes 8-15:  Data4 (8 bytes, sequential)
 * @param guidBytes The 16-byte Uint8Array representing the Guid.
 * @returns The GUID string.
 * @throws Error if input is not 16 bytes.
 */
export function decodeGuid(guidBytes: Uint8Array): string {
    if (guidBytes.length !== 16) {
        throw new Error(`Invalid GUID byte array length. Expected 16, got ${guidBytes.length}.`);
    }

    const hex = (bytes: Uint8Array): string => Array.from(bytes).map(byteToHex).join('');

    // Extract and reverse parts as needed for standard string representation
    const part1 = hex(guidBytes.slice(0, 4).reverse()); // int
    const part2 = hex(guidBytes.slice(4, 6).reverse()); // short
    const part3 = hex(guidBytes.slice(6, 8).reverse()); // short
    const part4 = hex(guidBytes.slice(8, 10));          // 2 bytes sequential
    const part5 = hex(guidBytes.slice(10, 16));         // 6 bytes sequential

    return `${part1}-${part2}-${part3}-${part4}-${part5}`;
}


/**
 * Decodes the payload for a SessionStartMessage.
 * @param payload The raw payload bytes from the wrapper (should be 16 bytes for a Guid).
 * @returns The decoded SessionStartMessage object.
 */
export function decodeSessionStartMessage(payload: Uint8Array): SessionStartMessage {
     try {
        const sessionId = decodeGuid(payload);
        return { sessionId };
     } catch (e) {
        throw new Error(`Failed to decode SessionStartMessage payload: ${e.message}`);
     }
}


// --- Main Message Dispatcher ---

/**
 * Decodes a full WebSocket message (wrapper + typed payload).
 * @param arrayBuffer The raw ArrayBuffer received from the WebSocket.
 * @returns An object containing the decoded message type and the specific decoded message data,
 *          or null/throws error if decoding fails or type is unknown.
 */
export function decodeMessage(arrayBuffer: ArrayBuffer): { type: ToClientMessageType; versionId: number; message: SessionStartMessage | PlayerJoinedMessage | GameHistoryUpdateMessage } | null {
    try {
        const wrapper = decodeWrapper(arrayBuffer);

        let messageData: any = null; // Use 'any' or create a union type of all possible messages

        switch (wrapper.type) {
            case ToClientMessageType.SessionStartMessage:
                messageData = decodeSessionStartMessage(wrapper.payload);
                break;

            case ToClientMessageType.PlayerJoined:
                messageData = decodePlayerJoined(wrapper.payload);
                break;

            case ToClientMessageType.GameHistoryUpdate:
                 // Example:
                 // messageData = decodeGameHistoryUpdate(wrapper.payload); // Implement this decoder
                messageData = decodeGameHistoryUpdate(wrapper.payload);
                break;

            default:
                // Handle unknown message types if decodeWrapper didn't already
                 console.error(`Unknown or unsupported message type received: ${wrapper.type}`);
                 // Return null, throw an error, or return a generic object
                 return null; // Or potentially: { type: wrapper.type, versionId: wrapper.versionId, message: { error: 'Unknown type', payload: wrapper.payload } };
        }

        return {
            type: wrapper.type,
            versionId: wrapper.versionId,
            message: messageData,
        };

    } catch (error) {
        console.error("Failed to decode WebSocket message:", error);
        // Depending on requirements, you might return null, throw, or return an error object
        return null;
    }
}

export interface DecodedCheckersMove {
    fromIndex: number;      // byte -> number (0-255)
    toIndex: number;        // byte -> number (0-255)
    promoted: boolean;      // bool (stored as byte 1/0) -> boolean
    capturedPieces: bigint; // ulong -> bigint
}

/**
 * Represents the decoded payload of a GameHistoryUpdate message.
 */
export interface GameHistoryUpdateMessage {
    moves: DecodedCheckersMove[];
}

// --- Constants ---

const CHECKERS_MOVE_SERIALIZED_SIZE = 11; // Matches C# constant (1+1+1+8)

// --- Decoding Function ---

/**
 * Decodes the payload of a GameHistoryUpdate message.
 * The payload is expected to be a sequence of serialized CheckersMove objects.
 * Assumes little-endian byte order for the ulong (capturedPieces).
 * @param payload The raw payload bytes (Uint8Array) from the ToClientWrapper.
 * @returns A GameHistoryUpdateMessage object containing the decoded moves.
 * @throws Error if the payload length is not a multiple of the move size.
 */
export function decodeGameHistoryUpdate(payload: Uint8Array): GameHistoryUpdateMessage {
    if (payload.byteLength % CHECKERS_MOVE_SERIALIZED_SIZE !== 0) {
        throw new Error(`Invalid GameHistoryUpdate payload length. Expected multiple of ${CHECKERS_MOVE_SERIALIZED_SIZE}, but got ${payload.byteLength}.`);
    }

    const moves: DecodedCheckersMove[] = [];
    const moveCount = payload.byteLength / CHECKERS_MOVE_SERIALIZED_SIZE;

    // Use DataView for fine-grained control over reading bytes and endianness
    // Access the underlying ArrayBuffer of the Uint8Array
    const dataView = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    const littleEndian = true; // C# BitConverter usually defaults to little-endian

    for (let i = 0; i < moveCount; i++) {
        const offset = i * CHECKERS_MOVE_SERIALIZED_SIZE;

        // Read data for one move according to C# serialization:
        const fromIndex = dataView.getUint8(offset + 0);
        const toIndex = dataView.getUint8(offset + 1);
        const promotedByte = dataView.getUint8(offset + 2);
        // Read the ulong (8 bytes) as BigInt, specifying little-endian
        const capturedPieces = dataView.getBigUint64(offset + 3, littleEndian);

        const move: DecodedCheckersMove = {
            fromIndex,
            toIndex,
            promoted: promotedByte !== 0, // Convert the byte back to boolean
            capturedPieces,
        };
        moves.push(move);
    }

    return { moves };
}
export interface PlayerJoinedMessage {
    userId: string;   // Decoded GUID string
    userName: string; // Decoded UTF-8 user name
}

export function decodePlayerJoined(payload: Uint8Array): PlayerJoinedMessage {
    const GUID_LENGTH = 16;
    const LENGTH_PREFIX_LENGTH = 2; // ushort
    const MIN_EXPECTED_LENGTH = GUID_LENGTH + LENGTH_PREFIX_LENGTH; // 18

    if (payload.byteLength < MIN_EXPECTED_LENGTH) {
        throw new Error(`Invalid PlayerJoined payload length. Expected at least ${MIN_EXPECTED_LENGTH} bytes, but got ${payload.byteLength}.`);
    }

    // Use DataView for reading multi-byte numbers and controlling endianness
    const dataView = new DataView(payload.buffer, payload.byteOffset, payload.byteLength);
    const littleEndian = true; // Match C# BitConverter default

    // 1. Decode UserId (16 bytes)
    // Slice the first 16 bytes for the Guid
    const userIdBytes = payload.slice(0, GUID_LENGTH);
    // Use the previously defined Guid decoder
    const userId = decodeGuid(userIdBytes); // Assuming decodeGuid exists and handles .NET format

    // 2. Decode UserNameLength (ushort, 2 bytes)
    // Read the length prefix starting at offset 16
    const userNameLength = dataView.getUint16(GUID_LENGTH, littleEndian);

    // 3. Validate total length based on UserNameLength
    const expectedTotalLength = MIN_EXPECTED_LENGTH + userNameLength;
    if (payload.byteLength < expectedTotalLength) {
        throw new Error(`Invalid PlayerJoined payload length. Header indicates ${userNameLength} bytes for username, requires total ${expectedTotalLength} bytes, but got ${payload.byteLength}.`);
    }
    // Optional strict check: if (payload.byteLength !== expectedTotalLength) { ... }

    // 4. Decode UserName (N bytes, UTF-8)
    // Slice the username bytes starting after the length prefix (offset 18)
    const userNameBytes = payload.slice(MIN_EXPECTED_LENGTH, expectedTotalLength);
    // Use TextDecoder for UTF-8
    const decoder = new TextDecoder('utf-8');
    const userName = decoder.decode(userNameBytes);

    return {
        userId,
        userName,
    };
}
