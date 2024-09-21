namespace SshPlayground.Packets;

public class ShellDataPacket : PacketBase
{
    public ShellDataPacket(byte[] data)
    {
        Data = data;
        Type = (byte)PacketType.ShellData;
    }

    public override uint MinimalSize => BaseSize; // ShellId

    public byte[] Data { get; private set; }

    public override byte[] Serialize()
    {
        var buff = new byte[MinimalSize + Data.Length];
        buff[0] = Type;
        Data.CopyTo(buff, 1);
        return buff;
    }

    public override int Deserialize(byte[] bytes)
    {
        throw new NotSupportedException();
    }
}
