using WebGameServer.WebSocketEncoding.FromClientMessages;
using WebGameServer.WebSocketEncoding.ToClientMessages;

namespace WebGameServer.WebSocketEncoding;

public interface IByteSerializable<out T> where T : allows ref struct
{
    public byte[] ToBytes();
    public static abstract T FromBytes(Span<byte> data);
}

public interface IToClientMessage<out T> : IByteSerializable<T>
{
    static abstract ToClientMessageType GetMessageType(); 
}

public interface IFromClientMessage<out T> : IByteSerializable<T>
{
    static abstract FromClientMessageType GetMessageType(); 
}