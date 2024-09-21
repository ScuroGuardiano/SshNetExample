namespace SshPlayground.Packets;

public enum PacketType : byte
{
    Unknown = 0,
    // Handled by the server
    // OpenShell = 1,
    // CloseShell = 2,
    ResizeShell = 3,
    SendToShell = 4,

    // Handled by the client
    ShellOpened = 21,
    ShellClosed = 22,
    ShellData = 23,
}
