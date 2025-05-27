export class ByteReader { 
    
    private dataView : DataView; 
    private offset: number; 
    private textDecoder : TextDecoder
    
    constructor(arrayBuffer: ArrayBuffer, initialOffset = 0) {
        if (initialOffset < 0 || initialOffset > arrayBuffer.byteLength) {
            throw new Error("Initial offset is out of buffer bounds.");
        }
        this.dataView = new DataView(arrayBuffer);
        this.offset = initialOffset;
        // Use 'utf-16le' for UTF16 Little Endian
        this.textDecoder = new TextDecoder('utf-16le');
    }
    
    get bytesRemaining(): number {
        return this.dataView.byteLength - this.offset;
    }
    
    hasEnoughBytes(byteCount: number): boolean {
        if (byteCount < 0) throw new Error("byteCount cannot be negative.");
        return this.offset + byteCount <= this.dataView.byteLength;
    }
    
    readGuid(): string {
        
        const guidByteLength = 16;
        if (!this.hasEnoughBytes(guidByteLength)) {
            throw new Error(`Buffer underflow reading GUID. Need ${guidByteLength}, have ${this.bytesRemaining}.`);
        }

        const bytes = new Uint8Array(this.dataView.buffer, this.dataView.byteOffset + this.offset, guidByteLength);
        this.offset += guidByteLength;

        // Format UUID bytes according to .NET Guid structure:
        // The first 3 components (Data1, Data2, Data3) are little-endian (read backwards).
        // The last 2 components (Data4) are big-endian (read forwards).
        const hex = (byte: number) => byte.toString(16).padStart(2, '0');

        // Data1 (4 bytes, LE)
        const d1 = hex(bytes[3]) + hex(bytes[2]) + hex(bytes[1]) + hex(bytes[0]);
        
        // Data2 (2 bytes, LE)
        const d2 = hex(bytes[5]) + hex(bytes[4]);
        
        // Data3 (2 bytes, LE)
        const d3 = hex(bytes[7]) + hex(bytes[6]);
        
        // Data4 (2 bytes + 6 bytes, BE - read sequentially)
        const d4_1 = hex(bytes[8]) + hex(bytes[9]);
        const d4_2 = hex(bytes[10]) + hex(bytes[11]) + hex(bytes[12]) + hex(bytes[13]) + hex(bytes[14]) + hex(bytes[15]);

        return `${d1}-${d2}-${d3}-${d4_1}-${d4_2}`;
    }

    readInt32(): number {
        const size = 4; // sizeof(int)
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading Int32. Need ${size}, have ${this.bytesRemaining}.`);
        }
        // true for littleEndian
        const value = this.dataView.getInt32(this.offset, true);
        this.offset += size;
        return value;
    }
    readInt64(): bigint {
        const size = 8; // sizeof(long)
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading Int64. Need ${size}, have ${this.bytesRemaining}.`);
        }
        // true for littleEndian
        const value = this.dataView.getBigInt64(this.offset, true);
        this.offset += size;
        return value;
    }

    readLengthPrefixedStringUTF16LE(): string {
        
        //Encoding standard for all strings a prefixed with length
        const lengthInBytes = this.readUint8();
        
        if (lengthInBytes < 0) throw new Error("lengthInBytes cannot be negative.");
        if (lengthInBytes === 0) return ""; // Empty string takes 0 bytes

        if (!this.hasEnoughBytes(lengthInBytes)) {
            throw new Error(`Buffer underflow reading StringUTF16LE. Need ${lengthInBytes}, have ${this.bytesRemaining}.`);
        }
        // Ensure length is even for UTF-16
        if (lengthInBytes % 2 !== 0) {
            console.warn(`Reading UTF16LE string with odd byte length (${lengthInBytes}). This might indicate data corruption.`);
            // Decide how to handle: throw an error, or proceed cautiously?
            // Let's proceed but the result might be unexpected.
        }

        const stringBytes = new Uint8Array(this.dataView.buffer, this.dataView.byteOffset + this.offset, lengthInBytes);
        this.offset += lengthInBytes;
        return this.textDecoder.decode(stringBytes);
    }

    readTimedCheckersMove(): TimedCheckersMove {
        // Use the globally defined constant for clarity and consistency
        const expectedSize = TIMED_CHECKERS_MOVE_BYTE_SIZE;
        if (!this.hasEnoughBytes(expectedSize)) {
            throw new Error(`Buffer underflow reading CheckersMove. Need ${expectedSize}, have ${this.bytesRemaining}.`);
        }
        
        const timeMs = this.readInt64();
        const fromIndex = this.readUint8();         
        const toIndex = this.readUint8();
        const promoted = this.readUint8() !== 0;
        const capturedPawns = this.readBigUint64(); 
        const capturedKings = this.readBigUint64(); 
        
        return {
            fromIndex: fromIndex,
            toIndex: toIndex,
            promoted: promoted,
            capturedPawns: capturedPawns,
            capturedKings: capturedKings,
            timeMs: timeMs
        } as TimedCheckersMove;
    }

    readTimedCheckersMoves(numberOfMoves: number): TimedCheckersMove[] {
        if (numberOfMoves < 0) throw new Error("numberOfMoves cannot be negative.");
        if (numberOfMoves === 0) return [];

        // Use the constant for size calculation
        const totalBytesNeeded = numberOfMoves * TIMED_CHECKERS_MOVE_BYTE_SIZE;
        if (!this.hasEnoughBytes(totalBytesNeeded)) {
            throw new Error(`Buffer underflow reading CheckersMoves array. Need ${totalBytesNeeded} bytes for ${numberOfMoves} moves, have ${this.bytesRemaining}.`);
        }

        const moves: TimedCheckersMove[] = new Array<TimedCheckersMove>(numberOfMoves);
        for (let i = 0; i < numberOfMoves; i++) {
            // This now calls the correctly implemented readCheckersMove
            moves[i] = (this.readTimedCheckersMove());
        }
        return moves;
    }

    skipBytes(byteCount: number): void {
        if (byteCount < 0) throw new Error("byteCount cannot be negative.");
        if (!this.hasEnoughBytes(byteCount)) {
            throw new Error(`Buffer underflow attempting to skip ${byteCount} bytes. Only ${this.bytesRemaining} available.`);
        }
        this.offset += byteCount;
    }
    readUint32(): number {
        const size = 4;
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading UInt32. Need ${size}, have ${this.bytesRemaining}.`);
        }
        const value = this.dataView.getUint32(this.offset, true);
        this.offset += size;
        return value;
    }

    readInt8(): number {
        const size = 1; // sizeof(sbyte)
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading Int8. Need ${size}, have ${this.bytesRemaining}.`);
        }
        const value = this.dataView.getInt8(this.offset);
        this.offset += size;
        return value;
    }

    readUint8(): number {
        const size = 1; 
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading Uint8. Need ${size}, have ${this.bytesRemaining}.`);
        }
        
        const value = this.dataView.getUint8(this.offset);
        this.offset += size;
        return value;
    }

    readUint16(): number {
        const size = 2;
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading UInt16. Need ${size}, have ${this.bytesRemaining}.`);
        }
        const value = this.dataView.getUint16(this.offset, true);
        this.offset += size;
        return value;
    }

    readBigUint64(): bigint {
        const size = 8;
        if (!this.hasEnoughBytes(size)) {
            throw new Error(`Buffer underflow reading UInt64. Need ${size}, have ${this.bytesRemaining}.`);
        }
        const value = this.dataView.getBigUint64(this.offset, true);
        this.offset += size;
        return value;
    }
}

export const TIMED_CHECKERS_MOVE_BYTE_SIZE = 27;
export interface CheckersMove{
    fromIndex: number;
    toIndex: number;
    promoted: boolean;
    capturedPawns : bigint;
    capturedKings : bigint;
}
export interface TimedCheckersMove extends CheckersMove {
    timeMs: bigint; // Time in milliseconds
}