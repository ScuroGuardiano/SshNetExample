using System.Net;

namespace SshPlayground.Packets;

public abstract class PacketBase
{
    public byte Type { get; protected set; }
    // We don't need packet size encoded in the packet, we're using WebSockets!
    // Which sends whole messages, so I will know their size.
    // public uint Size { get; protected set; } = 5; // Type + Size
    public abstract uint MinimalSize { get; }
    protected uint BaseSize => 1; // Type

    public abstract byte[] Serialize();
    
    /// <summary>
    ///   Deseralizes packet returning bytes read. If packet is invalid returns -1;
    /// <summary>
    /// <returns>
    ///   Bytes read if packet was valid or -1 is packet was invalid
    /// </returns>
    public virtual int Deserialize(byte[] bytes)
    {
        if (bytes.Length < MinimalSize) {
            Console.Error.WriteLine("Packet length was less than minimum required packed length.");
            return -1;
        }

        Type = bytes[0];
        
        return (int)BaseSize;
    }

    protected uint ReadUint32(byte[] bytes, int offset)
    {
        return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(bytes, offset));
    }

    protected void WriteUint32(uint val, byte[] target, int offset)
    {
        var targetLocation = target.AsSpan().Slice(offset);
        var networkEncoded = (uint)IPAddress.HostToNetworkOrder((int)val);
        var res = BitConverter.TryWriteBytes(targetLocation, networkEncoded);
        if (!res)
        {
            throw new Exception("Couldn't write bytes to span.");
        }
    }
}
