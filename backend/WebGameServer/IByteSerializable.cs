namespace WebGameServer;

public interface IByteSerializable<out T>
{
    byte[] ToByteArray();
    static abstract T FromBytes(byte[] data);
}