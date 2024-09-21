namespace SshPlayground.Packets;

public class ResizeShellPacket : PacketBase
{
    public override uint MinimalSize
        => BaseSize
        + 4 // Columns,
        + 4 // Rows,
        + 4 // Width,
        + 4; // Height

    public uint Columns { get; private set; }
    public uint Rows { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    public override byte[] Serialize()
    {
        throw new NotSupportedException();
    }

    public override int Deserialize(byte[] bytes)
    {
        var r = base.Deserialize(bytes);
        if (r < 0) return r;

        Columns = ReadUint32(bytes, r + 4);
        Rows = ReadUint32(bytes, r + 8);
        Width = ReadUint32(bytes, r + 12);
        Height = ReadUint32(bytes, r + 16);

        return (int)MinimalSize;
    }
}
