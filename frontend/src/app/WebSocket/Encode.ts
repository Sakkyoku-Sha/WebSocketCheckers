// 1. Mirror the C# FromClientMessageType Enum
export enum ToServerMessageType
{
    IdentifyUser = 0,
    TryMakeMove = 1,
    TryJoinGame = 2,
    TryCreateGame = 3,
    GetActiveGamesRequest = 4,
}
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

export function encodeWrapper(version : number, type: ToServerMessageType, payload: Uint8Array): ArrayBuffer {
    
    const totalSize = 2 + 2 + payload.length; //version / type
    const buffer = new ArrayBuffer(totalSize);
    const dataView = new DataView(buffer);
    const littleEndian = true; // Match C# server expectation

    let offset = 0;
    // Write VersionId (ushort)
    dataView.setUint16(offset, version, littleEndian);
    offset += 2;

    // Write Type (ushort)
    dataView.setUint16(offset, type, littleEndian);
    offset += 2;
    
    
    // Write Payload
    if(payload.length === 0) {return buffer;}
    
    const payloadView = new Uint8Array(buffer, offset, payload.length); // Create view into the buffer
    payloadView.set(payload); // Copy payload bytes into the buffer view
    
    return buffer;
}

export function encodeIdentifyUserMessage(userId: string): ArrayBuffer {
    
    const payloadBytes = encodeGuidNet(userId);
    return encodeWrapper(
        1,
        ToServerMessageType.IdentifyUser,
        payloadBytes
    );
}

export function encodeCreateGameMessage(userId: string): ArrayBuffer {

    const payloadBytes = encodeGuidNet(userId);
    return encodeWrapper(
        1,
        ToServerMessageType.TryCreateGame,
        payloadBytes
    );
}

export function encodeActiveGamesMessage(){
 
    const payloadBytes = new Uint8Array();
    return encodeWrapper(
        1,
        ToServerMessageType.GetActiveGamesRequest,
        payloadBytes
    )
}

export function encodeTryMakeMoveMessage(userId : string, gameId : number, fromIndex : number, toIndex : number): ArrayBuffer {
    
    const totalLength = 16 + 4 + 1 + 1; 
    const buffer = new ArrayBuffer(totalLength);
    const view = new Uint8Array(buffer);

    const userIdBytes = encodeGuidNet(userId);
    view.set(userIdBytes, 0);

    const dataView = new DataView(buffer);
    dataView.setInt32(16, gameId,true);
    dataView.setUint8(20, fromIndex);
    dataView.setUint8(21, toIndex);

    return encodeWrapper(
        1,
        ToServerMessageType.TryMakeMove,
        view,
    );
}

export function encodeTryJoinGameMessage(userId : string, gameId : number): ArrayBuffer {
    
    const totalLength = 16 + 4; 
    const buffer = new ArrayBuffer(totalLength);
    const view = new Uint8Array(buffer);
    
    const userIdBytes = encodeGuidNet(userId);
    view.set(userIdBytes, 0);
    
    const dataView = new DataView(buffer);
    dataView.setInt32(16, gameId,true);
    
    return encodeWrapper(
        1,
        ToServerMessageType.TryJoinGame,
        view,
    )
}