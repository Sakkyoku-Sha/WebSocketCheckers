using System.Runtime.InteropServices;
using System.Text;
using WebGameServer.State; // Assuming previous definitions are here

namespace WebGameServer.WebSocketEncoding.ToClientMessages
{
    // Changed from 'ref struct' to 'struct' to allow FromByteSpan implementation
    public readonly struct ActiveGameInfoMessage : IToClientMessage<ActiveGameInfoMessage>
    {
        private const byte PLAYER2_EXISTS = 1;
        private const byte PLAYER2_DOES_NOT_EXIST = 0;

        // --- Fields remain the same ---
        private readonly Guid gameId;
        private readonly GameStatus status;
        private readonly string gameName;
        private readonly Guid player1Id;
        private readonly string player1Name;
        private readonly Guid? player2Id; // Keep nullable Guid for logic
        private readonly string? player2Name;
        private readonly CheckersMove[] gameHistory; // Keep as array

        // --- Constructor remains similar ---
        public ActiveGameInfoMessage(GameInfo gameinfo)
        {
            gameId = gameinfo.GameId;
            status = gameinfo.Status;
            gameName = gameinfo.GameName;

            player1Id = gameinfo.Player1.UserId;
            player1Name = gameinfo.Player1.PlayerName;

            if (gameinfo.Player2 != null)
            {
                player2Id = gameinfo.Player2.UserId;
                player2Name = gameinfo.Player2.PlayerName;
            }
            else
            {
                player2Id = null; // Ensure null if Player2 object is null
                player2Name = null;
            }

            // Ensure gameHistory is never null, use empty array if needed
            gameHistory = gameinfo.GetHistory().ToArray();
        }

        // Private constructor for FromByteSpan
        private ActiveGameInfoMessage(Guid gameId, GameStatus status, string gameName, Guid p1Id, string p1Name, Guid? p2Id, string? p2Name, CheckersMove[] history)
        {
            this.gameId = gameId;
            this.status = status;
            this.gameName = gameName;
            this.player1Id = p1Id;
            this.player1Name = p1Name;
            this.player2Id = p2Id;
            this.player2Name = p2Name;
            this.gameHistory = history;
        }


        /// <summary>
        /// Revised Encoding:
        /// - 16 bytes: GameId
        /// - 1 byte:  Status
        /// - 1 byte:  GameNameLength (Lg)
        /// - Lg bytes: GameName (UTF8)
        /// - 16 bytes: Player1Id
        /// - 1 byte:  Player1NameLength (Lp1)
        /// - Lp1 bytes: Player1Name (UTF8)
        /// - 1 byte:  HasPlayer2 flag (1 if player 2 exists, 0 otherwise)
        /// - If HasPlayer2 == 1:
        ///    - 16 bytes: Player2Id
        ///    - 1 byte:  Player2NameLength (Lp2)
        ///    - Lp2 bytes: Player2Name (UTF8)
        /// - 1 byte:  GameHistoryCount (C)
        /// - C * sizeof(CheckersMove) bytes: GameHistory data
        /// </summary>
        public byte[] ToBytes()
        {
            var gameIdBytes = gameId.ToByteArray(); // 16
            var statusByte = (byte)status; // 1

            var gameNameBytes = Encoding.UTF8.GetBytes(gameName);
            if (gameNameBytes.Length > 255) throw new ArgumentOutOfRangeException(nameof(gameName), "Game Name too long for 1-byte length encoding.");
            var gameNameLengthByte = (byte)gameNameBytes.Length; // 1 + N

            var player1IdBytes = player1Id.ToByteArray(); // 16
            var player1NameBytes = Encoding.UTF8.GetBytes(player1Name);
            if (player1NameBytes.Length > 255) throw new ArgumentOutOfRangeException(nameof(player1Name), "Player 1 Name too long for 1-byte length encoding.");
            var player1NameLengthByte = (byte)player1NameBytes.Length; // 1 + N

            byte hasPlayer2Byte = PLAYER2_DOES_NOT_EXIST; // 1
            byte[]? player2IdBytes = null; // 0 or 16
            byte[]? player2NameBytes = null; // 0 or N
            byte player2NameLengthByte = 0; // 0 or 1
            int player2TotalSize = 0;

            if (player2Id.HasValue && player2Name != null)
            {
                hasPlayer2Byte = PLAYER2_EXISTS;
                player2IdBytes = player2Id.Value.ToByteArray();
                player2NameBytes = Encoding.UTF8.GetBytes(player2Name);
                if (player2NameBytes.Length > 255) throw new ArgumentOutOfRangeException(nameof(player2Name), "Player 2 Name too long for 1-byte length encoding.");
                player2NameLengthByte = (byte)player2NameBytes.Length;
                player2TotalSize = player2IdBytes.Length + 1 + player2NameBytes.Length; // 16 + 1 + N
            }

            // Ensure history array isn't null
            var historyArray = gameHistory ?? Array.Empty<CheckersMove>();
            if (historyArray.Length > 255) throw new ArgumentOutOfRangeException(nameof(gameHistory), "Game History too long for 1-byte count encoding.");
            var gameHistoryCountByte = (byte)historyArray.Length; // 1

            int gameHistoryBytesLength = 0;
            ReadOnlySpan<byte> gameHistoryBytesSpan = ReadOnlySpan<byte>.Empty;
             if (historyArray.Length > 0)
             {
                 // Important: Requires CheckersMove to be an unmanaged struct
                 gameHistoryBytesSpan = MemoryMarshal.AsBytes<CheckersMove>(historyArray);
                 gameHistoryBytesLength = gameHistoryBytesSpan.Length; // C * sizeof(CheckersMove)
             }


            // Calculate total size accurately
            var totalSize = gameIdBytes.Length + 1 +
                            1 + gameNameBytes.Length +
                            player1IdBytes.Length + 1 + player1NameBytes.Length +
                            1 + // hasPlayer2 flag
                            player2TotalSize +
                            1 + // gameHistoryCount
                            gameHistoryBytesLength;

            var buffer = new byte[totalSize];
            var offset = 0;

            // GameId
            Buffer.BlockCopy(gameIdBytes, 0, buffer, offset, gameIdBytes.Length);
            offset += gameIdBytes.Length; // offset = 16

            // Status
            buffer[offset++] = statusByte; // offset = 17

            // GameName
            buffer[offset++] = gameNameLengthByte; // offset = 18
            Buffer.BlockCopy(gameNameBytes, 0, buffer, offset, gameNameBytes.Length);
            offset += gameNameBytes.Length; // offset = 18 + Lg

            // Player1Id
            Buffer.BlockCopy(player1IdBytes, 0, buffer, offset, player1IdBytes.Length);
            offset += player1IdBytes.Length; // offset = 34 + Lg

            // Player1Name
            buffer[offset++] = player1NameLengthByte; // offset = 35 + Lg
            Buffer.BlockCopy(player1NameBytes, 0, buffer, offset, player1NameBytes.Length);
            offset += player1NameBytes.Length; // offset = 35 + Lg + Lp1

            // HasPlayer2 flag
            buffer[offset++] = hasPlayer2Byte; // offset = 36 + Lg + Lp1

            // Player2 Data (if exists)
            if (hasPlayer2Byte == PLAYER2_EXISTS && player2IdBytes != null && player2NameBytes != null)
            {
                // Player2Id
                Buffer.BlockCopy(player2IdBytes, 0, buffer, offset, player2IdBytes.Length);
                offset += player2IdBytes.Length; // offset += 16
                // Player2Name
                buffer[offset++] = player2NameLengthByte; // offset += 1
                Buffer.BlockCopy(player2NameBytes, 0, buffer, offset, player2NameBytes.Length);
                offset += player2NameBytes.Length; // offset += Lp2
            }

            // GameHistory Count
            buffer[offset++] = gameHistoryCountByte;

             // GameHistory Data (if exists)
             if (gameHistoryCountByte > 0 && gameHistoryBytesLength > 0)
             {
                 gameHistoryBytesSpan.CopyTo(buffer.AsSpan(offset));
                 // We don't need Buffer.BlockCopy if we use spans
                 // Buffer.BlockCopy(gameHistoryBytesSpan.ToArray(), 0, buffer, offset, gameHistoryBytesLength);
                 offset += gameHistoryBytesLength;
             }

            // Sanity check - did we fill the buffer exactly?
             if (offset != totalSize)
             {
                 // This indicates an error in the size calculation or writing logic
                 throw new InvalidOperationException($"Serialization error: Final offset {offset} does not match calculated size {totalSize}.");
             }

            return buffer;
        }
        
        /// <summary>
        /// Deserializes the message content from a byte span.
        /// Assumes data contains ONLY the payload, not the message type byte.
        /// </summary>
        public static ActiveGameInfoMessage FromByteSpan(Span<byte> data)
        {
            var offset = 0;

            // GameId (16 bytes)
            if (data.Length < offset + 16) throw new ArgumentException("Data too short for GameId");
            var gameId = new Guid(data.Slice(offset, 16));
            offset += 16;

            // Status (1 byte)
            if (data.Length < offset + 1) throw new ArgumentException("Data too short for Status");
            var status = (GameStatus)data[offset++];

            // GameName (1 byte length + N bytes)
            if (data.Length < offset + 1) throw new ArgumentException("Data too short for GameName length");
            var gameNameLength = data[offset++];
            if (data.Length < offset + gameNameLength) throw new ArgumentException("Data too short for GameName content");
            var gameName = Encoding.UTF8.GetString(data.Slice(offset, gameNameLength));
            offset += gameNameLength;

            // Player1Id (16 bytes)
            if (data.Length < offset + 16) throw new ArgumentException("Data too short for Player1Id");
            var player1Id = new Guid(data.Slice(offset, 16));
            offset += 16;

            // Player1Name (1 byte length + N bytes)
            if (data.Length < offset + 1) throw new ArgumentException("Data too short for Player1Name length");
            var player1NameLength = data[offset++];
            if (data.Length < offset + player1NameLength) throw new ArgumentException("Data too short for Player1Name content");
            var player1Name = Encoding.UTF8.GetString(data.Slice(offset, player1NameLength));
            offset += player1NameLength;

            // HasPlayer2 flag (1 byte)
            if (data.Length < offset + 1) throw new ArgumentException("Data too short for HasPlayer2 flag");
            var hasPlayer2 = data[offset++];

            Guid? player2Id = null;
            string? player2Name = null;
            if (hasPlayer2 == PLAYER2_EXISTS)
            {
                // Player2Id (16 bytes)
                if (data.Length < offset + 16) throw new ArgumentException("Data too short for Player2Id");
                player2Id = new Guid(data.Slice(offset, 16));
                offset += 16;

                // Player2Name (1 byte length + N bytes)
                if (data.Length < offset + 1) throw new ArgumentException("Data too short for Player2Name length");
                var player2NameLength = data[offset++];
                if (data.Length < offset + player2NameLength) throw new ArgumentException("Data too short for Player2Name content");
                player2Name = Encoding.UTF8.GetString(data.Slice(offset, player2NameLength));
                offset += player2NameLength;
            }

            // GameHistory Count (1 byte)
            if (data.Length < offset + 1) throw new ArgumentException("Data too short for GameHistory count");
            var gameHistoryCount = data[offset++];

            CheckersMove[] gameHistory = Array.Empty<CheckersMove>();
            if (gameHistoryCount > 0)
            {
                // Important: Requires CheckersMove to be an unmanaged struct
                int moveSize = Marshal.SizeOf<CheckersMove>(); // Or use const CheckersMove.SizeInBytes
                int expectedHistoryBytesLength = gameHistoryCount * moveSize;

                if (data.Length < offset + expectedHistoryBytesLength) throw new ArgumentException("Data too short for GameHistory content");

                var historyBytes = data.Slice(offset, expectedHistoryBytesLength);
                gameHistory = MemoryMarshal.Cast<byte, CheckersMove>(historyBytes).ToArray();
                offset += expectedHistoryBytesLength;
            }

            // Optional: Check if we consumed the entire span
             if (offset != data.Length)
             {
                  // This might indicate extra unexpected data
                  // Depending on protocol, this could be an error or expected padding/future fields
                  // For strict parsing, uncomment:
                  // throw new ArgumentException($"Extra data remaining ({data.Length - offset} bytes) after parsing.");
             }


            // Construct the message using the private constructor
            return new ActiveGameInfoMessage(gameId, status, gameName, player1Id, player1Name, player2Id, player2Name, gameHistory);
        }


        public static ToClientMessageType GetMessageType()
        {
            return ToClientMessageType.ActiveGameInfoMessage;
        }

        // Optional: Add getters if needed (struct fields are private)
        public Guid GameId => gameId;
        public GameStatus Status => status;
        public string GameName => gameName;
        public Guid Player1Id => player1Id;
        public string Player1Name => player1Name;
        public Guid? Player2Id => player2Id;
        public string? Player2Name => player2Name;
        public ReadOnlyMemory<CheckersMove> GameHistory => gameHistory; // Return as ReadOnlyMemory
    }
}