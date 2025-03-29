using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public record PlayerJoinedMessage(Guid UserId, string UserName) : IToClientMessage<PlayerJoinedMessage>
{
    public Guid UserId = UserId;
    public readonly string UserName = UserName;

    public PlayerJoinedMessage(PlayerInfo playerInfo) : this(playerInfo.UserId, playerInfo.PlayerName)
    {
    }

    //Encoding is 
    // 1. UserId (16 bytes)
    // 2. UserNameLength (2 bytes)
    // 3. UserName (N bytes)
    public byte[] ToBytes()
    {
        var userIdBytes = UserId.ToByteArray();
        var userNameBytes = System.Text.Encoding.UTF8.GetBytes(UserName);
        var userNameLengthBytes = BitConverter.GetBytes((ushort)userNameBytes.Length);
        
        var totalSize = userIdBytes.Length + userNameBytes.Length + 2;
        var buffer = new byte[totalSize];
        
        Array.Copy(userIdBytes, 0, buffer, 0, userIdBytes.Length);
        Array.Copy(userNameLengthBytes, 0, buffer, userIdBytes.Length, userNameLengthBytes.Length);
        Array.Copy(userNameBytes, 0, buffer, userIdBytes.Length + userNameLengthBytes.Length, userNameBytes.Length);
        
        return buffer;
    }

    public static PlayerJoinedMessage FromBytes(Span<byte> data)
    {
        if (data.Length < 18) // 16 bytes for UserId + 2 bytes for UserNameLength
            throw new ArgumentException("Data length is not sufficient to deserialize PlayerJoinedMessage.");

        var userId = new Guid(data.Slice(0, 16));
        var userNameLength = BitConverter.ToUInt16(data.Slice(16, 2));
        var userName = System.Text.Encoding.UTF8.GetString(data.Slice(18, userNameLength));

        return new PlayerJoinedMessage(userId, userName);
    }

    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.PlayerJoined;
    }
}