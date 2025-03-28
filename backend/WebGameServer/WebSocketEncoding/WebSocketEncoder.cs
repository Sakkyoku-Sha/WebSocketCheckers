using WebGameServer.WebSocketEncoding.ToClientMessages;

namespace WebGameServer.WebSocketEncoding;

public static class WebSocketEncoder
{
    public static byte[] Encode<T>(T value) where T : IToClientMessage<T>
    {
        var payLoad = value.ToBytes();
        var toMessage = new ToClientWrapper()
        {
            Type = T.GetMessageType(),
            VersionId = 0,
            PayLoadSize = (ushort)payLoad.Length,
            Payload = payLoad,
        };

        return toMessage.ToBytes();
    }
}