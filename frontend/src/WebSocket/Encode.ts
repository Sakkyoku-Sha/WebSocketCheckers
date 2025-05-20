enum ToServerMessageType
{
    //Initial Message From User 
    IdentifyUser = 0,

    //Server Status Queries 
    GetActiveGamesRequest = 1,

    //Try Act on Game State, Either Game State is Updated or Fail Response is Returned
    TryJoinGameRequest = 2,
    TryCreateGameRequest = 3,
    TryMakeMoveRequest = 4,

    //Draw Request / Responses  
    DrawRequest = 5,
    DrawRequestResponse = 6,

    //Game Status Changes
    Surrender = 7,
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

export function encodeCreateGameMessage(): ArrayBuffer {
    return encodeWrapper(
        1,
        ToServerMessageType.TryCreateGameRequest,
        new Uint8Array(),
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
export function encodeDrawRequestMessage() {
    const payloadBytes = new Uint8Array();
    return encodeWrapper(
        1,
        ToServerMessageType.DrawRequest,
        payloadBytes
    )
}

export function encodeDrawRequestResponse(accept: boolean) {
    const buffer = new ArrayBuffer(1);
    new DataView(buffer).setUint8(0, accept ? 1 : 0);

    return encodeWrapper(
        1,
        ToServerMessageType.DrawRequestResponse,
        new Uint8Array(buffer)
    );
}

export function encodeSurrenderMessage() {
    const payloadBytes = new Uint8Array();
    return encodeWrapper(
        1,
        ToServerMessageType.Surrender,
        payloadBytes
    )
}

export function encodeTryMakeMoveMessage(fromIndex : number, toIndex : number): ArrayBuffer {
    
    const totalLength = 1 + 1; 
    const buffer = new ArrayBuffer(totalLength);
    const view = new Uint8Array(buffer);
    
    const dataView = new DataView(buffer);
    dataView.setUint8(0, fromIndex);
    dataView.setUint8(1, toIndex);
    
    return encodeWrapper(
        1,
        ToServerMessageType.TryMakeMoveRequest,
        view,
    );
}

export function encodeTryJoinGameMessage(gameId : number): ArrayBuffer {
    
    const totalLength = 4; 
    const buffer = new ArrayBuffer(totalLength);
    const view = new Uint8Array(buffer);
    
    const dataView = new DataView(buffer);
    dataView.setInt32(0, gameId,true);
    
    return encodeWrapper(
        1,
        ToServerMessageType.TryJoinGameRequest,
        view,
    )
}