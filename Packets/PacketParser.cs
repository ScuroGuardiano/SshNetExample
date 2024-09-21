namespace SshPlayground.Packets;

public class PacketParser
{
    public PacketBase ParseFromBytes(byte[] bytes)
    {
        if (bytes.Length < 1)
        {
            throw new InvalidDataException("Byte array is empty.");
        }

        PacketType type = (PacketType)bytes[0];

        PacketBase? packet = type switch {
            PacketType.ResizeShell => CreatePacketFromBytes<ResizeShellPacket>(bytes),
            PacketType.SendToShell => CreatePacketFromBytes<SendToShellPacket>(bytes),
            _ => throw new InvalidDataException("Unknown packet type")
        };

        if (packet is null)
        {
            throw new InvalidDataException($"Packet {type.ToString()} is invalid.");
        }

        return packet;
    }

    public PacketType GetPacketType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 1)
        {
            throw new InvalidDataException("Byte array is empty.");
        }

        PacketType type = (PacketType)bytes[0];

        return Enum.IsDefined(type) ? type : PacketType.Unknown;
    }
    
    private T? CreatePacketFromBytes<T>(byte[] bytes)
        where T : PacketBase, new()
    {
        var packet = new T();
        var r = packet.Deserialize(bytes);
        return r > 0 ? packet : null;
    }
}
