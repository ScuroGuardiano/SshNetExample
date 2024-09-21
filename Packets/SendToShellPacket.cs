namespace SshPlayground.Packets;

public class SendToShellPacket : PacketBase
{
    public override uint MinimalSize
        => BaseSize;

    public byte[] Data { get; private set; } = [];

    public override byte[] Serialize()
    {
        throw new NotSupportedException();
    }

    public override int Deserialize(byte[] bytes)
    {
        var r = base.Deserialize(bytes);
        if (r < 0) return r;

        var dataSize = bytes.Length - r;

        Data = new byte[dataSize];
        Array.Copy(bytes, r, Data, 0, dataSize);

        return r + dataSize;
    }
}
