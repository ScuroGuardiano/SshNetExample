namespace SshPlayground.Packets;

public class ShellClosedPacket : PacketBase
{
    public ShellClosedPacket()
    {
        Type = (byte)PacketType.ShellClosed;
    }

    public override uint MinimalSize => BaseSize;


    public override byte[] Serialize()
    {
        var buff = new byte[MinimalSize];
        buff[0] = Type;
        return buff;
    }

    public override int Deserialize(byte[] bytes)
    {
        throw new NotSupportedException();
    }
}
