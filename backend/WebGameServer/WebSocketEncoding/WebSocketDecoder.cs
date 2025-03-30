using System.Runtime.InteropServices;
using WebGameServer.WebSocketEncoding.FromClientMessages;

namespace WebGameServer.WebSocketEncoding;

public static class WebSocketDecoder
{
    public static T Decode<T>(byte[] webSocketBuffer) where T : IFromClientMessage<T>
    {
        if (webSocketBuffer.Length != SessionSocketHandler.WebSocketBufferSize)
        {
            throw new Exception("This code assumes the entire message from clients exists within the buffer, if this changes review this CODE!!!!!"); 
        }
        
        var span = new Span<byte>(webSocketBuffer);
        var messageType = MemoryMarshal.Cast<byte, FromClientMessageType>(span.Slice(2, 2))[0];
        var payloadSize = MemoryMarshal.Cast<byte, ushort>(span.Slice(4, 2))[0];

        if (6 + payloadSize > webSocketBuffer.Length)
        {
            throw new Exception("Payload size exceeds the buffer length.");
        }
        var payLoad = span.Slice(6, payloadSize);
      
        if (T.GetMessageType() != messageType)
        {
            throw new Exception("Attempting to decode a message to a type for which it does not apply"); 
        }

        return T.FromByteSpan(payLoad);
    }
    
    public static FromClientWrapper DecodeToWrapper(byte[] webSocketBuffer)
    {
        if (webSocketBuffer.Length != SessionSocketHandler.WebSocketBufferSize)
        {
            throw new Exception("This code assumes the entire message from clients exists within the buffer, if this changes review this CODE!!!!!"); 
        }
        
        var span = new Span<byte>(webSocketBuffer);
        var versionId = MemoryMarshal.Cast<byte, ushort>(span.Slice(0, 2))[0]; 
        var messageType = MemoryMarshal.Cast<byte, FromClientMessageType>(span.Slice(2, 2))[0];
        var payloadSize = MemoryMarshal.Cast<byte, ushort>(span.Slice(4, 2))[0];
        if (6 + payloadSize > webSocketBuffer.Length)
        {
            throw new Exception("Payload size exceeds the buffer length.");
        }
        var payLoad = span.Slice(6, payloadSize);

        return new FromClientWrapper()
        {
            VersionId = versionId,
            Type = messageType,
            PayLoadSize = payloadSize,
            Payload = payLoad
        };
    }
}