// Encode.ts - Functions for encoding messages TO the server

// 1. Mirror the C# FromClientMessageType Enum
// (Assuming you have this enum defined on the server)
export enum ToServerMessageType {
    IdentifyUser = 0, // Match the value from C# FromClientMessageType.IdentifyUser
    // Add other message types the client might send
    AnotherMessage = 1,
}

// Define a constant for the version ID the client will send
// Make sure this matches what the server expects or handles
const CLIENT_VERSION_ID = 1;

// --- Helper Functions ---

/**
 * Helper to convert a 2-char hex string part to a byte.
 */
function hexToByte(hex: string): number {
    return parseInt(hex, 16);
}

/**
 * Encodes a standard UUID string (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
 * into a 16-byte Uint8Array following the .NET Guid.ToByteArray() format.
 * C# Guid Bytes Order:
 *   Bytes 0-3:   Data1 (int, reversed from string)
 *   Bytes 4-5:   Data2 (short, reversed from string)
 *   Bytes 6-7:   Data3 (short, reversed from string)
 *   Bytes 8-15:  Data4 (8 bytes, sequential from string)
 * @param uuidString The standard UUID string.
 * @returns A 16-byte Uint8Array in .NET Guid format.
 * @throws Error if the UUID string format is invalid.
 */
export function encodeGuidNet(uuidString: string): Uint8Array {
    const cleanedUuid = uuidString.replace(/-/g, '');
    if (cleanedUuid.length !== 32) {
        throw new Error(`Invalid UUID string format: ${uuidString}`);
    }

    const bytes = new Uint8Array(16);
    let byteIndex = 0;

    try {
        // Part 1 (Data1 - 4 bytes, reversed)
        for (let i = 6; i >= 0; i -= 2) {
            bytes[byteIndex++] = hexToByte(cleanedUuid.substring(i, i + 2));
        }
        // Part 2 (Data2 - 2 bytes, reversed)
        for (let i = 10; i >= 8; i -= 2) {
            bytes[byteIndex++] = hexToByte(cleanedUuid.substring(i, i + 2));
        }
        // Part 3 (Data3 - 2 bytes, reversed)
        for (let i = 14; i >= 12; i -= 2) {
            bytes[byteIndex++] = hexToByte(cleanedUuid.substring(i, i + 2));
        }
        // Part 4 (Data4 - 8 bytes, sequential)
        for (let i = 16; i < 32; i += 2) {
            bytes[byteIndex++] = hexToByte(cleanedUuid.substring(i, i + 2));
        }
    } catch (e : any) {
         throw new Error(`Failed to parse hex in UUID string: ${uuidString}. Error: ${e.message}`);
    }


    if (byteIndex !== 16) {
        // This should not happen with the loops above if input is valid
         throw new Error(`Internal error during GUID encoding. Produced ${byteIndex} bytes, expected 16.`);
    }

    return bytes;
}

// --- Generic Wrapper Encoding ---

/**
 * Encodes the message wrapper (header + payload) into an ArrayBuffer.
 * Uses little-endian for header fields.
 * @param type The message type enum value (ToServerMessageType).
 * @param payload The raw payload bytes (Uint8Array).
 * @param versionId The version ID to include in the header.
 * @returns An ArrayBuffer containing the complete message ready to send.
 */
export function encodeWrapper(type: ToServerMessageType, payload: Uint8Array, versionId: number = CLIENT_VERSION_ID): ArrayBuffer {
    const payloadSize = payload.length;
    const headerSize = 6; // ushort (2) + ushort (2) + ushort (2)
    const totalSize = headerSize + payloadSize;

    const buffer = new ArrayBuffer(totalSize);
    const dataView = new DataView(buffer);
    const littleEndian = true; // Match C# server expectation

    let offset = 0;
    // Write VersionId (ushort)
    dataView.setUint16(offset, versionId, littleEndian);
    offset += 2;

    // Write Type (ushort)
    dataView.setUint16(offset, type, littleEndian);
    offset += 2;

    // Write PayloadSize (ushort)
    dataView.setUint16(offset, payloadSize, littleEndian);
    offset += 2; // offset is now 6

    // Write Payload
    const payloadView = new Uint8Array(buffer, offset, payloadSize); // Create view into the buffer
    payloadView.set(payload); // Copy payload bytes into the buffer view

    return buffer;
}


// --- Specific Message Encoding Functions ---

/**
 * Encodes an IdentifyUser message.
 * @param userId The user's UUID as a standard string.
 * @returns An ArrayBuffer ready to be sent via WebSocket.
 */
export function encodeIdentifyUserMessage(userId: string): ArrayBuffer {
    // 1. Encode the payload (UUID string to .NET Guid bytes)
    const payloadBytes = encodeGuidNet(userId);

    // 2. Encode the wrapper with the payload
    const messageBuffer = encodeWrapper(
        ToServerMessageType.IdentifyUser,
        payloadBytes
        // Optionally pass a specific versionId if needed:
        // CLIENT_VERSION_ID
    );

    return messageBuffer;
}

// --- Usage Example (inside your WebSocket logic) ---

/*
// Assume 'ws' is your connected WebSocket instance
// ws.binaryType = "arraybuffer"; // Ensure binary type is set

function sendIdentifyMessage(userUuid: string, ws: WebSocket) {
    if (ws.readyState === WebSocket.OPEN) {
        try {
            const messageToSend = encodeIdentifyUserMessage(userUuid);
            ws.send(messageToSend);
            console.log(`Sent IdentifyUser message for User ID: ${userUuid}`);
        } catch (error) {
            console.error(`Failed to encode or send IdentifyUser message:`, error);
        }
    } else {
        console.error("WebSocket is not open. Cannot send IdentifyUser message.");
    }
}

// Example call:
// const myUserId = "123e4567-e89b-12d3-a456-426614174000";
// sendIdentifyMessage(myUserId, ws);

*/