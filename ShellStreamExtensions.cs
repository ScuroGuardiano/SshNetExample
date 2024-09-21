using System.Reflection;
using Renci.SshNet;

public static class ShellStreamExtensions
{
  public static bool ResizeWindow(this ShellStream shell, uint cols, uint rows, uint width, uint height)
  {
    FieldInfo field = shell.GetType().GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var channel = field.GetValue(shell)!;
    var sendWindowChangeRequest = channel.GetType().GetMethod("SendWindowChangeRequest")!;
    bool res = (bool)sendWindowChangeRequest.Invoke(channel, [ cols, rows, width, height ])!;
    return res;
  }
}
