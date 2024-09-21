using System.Net.WebSockets;
using System.Threading.Channels;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshPlayground.Packets;

namespace SshPlayground;

public class SshWsShellHandler : IDisposable
{
    private SshClient? _client;
    private WebSocket? _ws;
    private ShellStream? _shell;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private PacketParser _parser;
    private Channel<PacketBase> _outputQueue = Channel.CreateBounded<PacketBase>(10);

    public SshWsShellHandler(PacketParser parser)
    {
        _parser = parser;
    }

    public async Task<ConnectResult> Connect(string sshHost, string sshUsername, string sshPassword, CancellationToken ct)
    {
        if (_client is not null && _client.IsConnected)
        {
            return ConnectResult.ALREADY_CONNECTED;
        }

        _client = new SshClient(sshHost, sshUsername, sshPassword);

        try
        {
            await _client.ConnectAsync(ct);
            return ConnectResult.CONNECTED;
        }
        catch (SshAuthenticationException ex)
        {
            Console.Error.WriteLine($"SSH Authentication exception occured: {ex.Message}");
            return ConnectResult.AUTHENTICATION_FAILURE;
        }
        catch (SshConnectionException ex)
        {
            Console.Error.WriteLine($"SSH Connection exception occured: {ex.Message}");
            return ConnectResult.SSH_CONNECTION_FAILURE;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Some random exception occured while connecting to ssh: {ex.Message}");
            throw;
        }
    }

    public async Task HandleWebsocketAsync(WebSocket ws, uint cols, uint rows, uint width, uint height, CancellationToken ct)
    {
        ct.Register(async () => await Close());
        _ws = ws;

        if (_client is null || !_client.IsConnected)
        {
            await Close();
            throw new Exception("You must connect first to launch shell!");
        }

        try
        {
            Console.WriteLine("Starting shell...");
            _shell = _client.CreateShellStream("xterm", cols, rows, width, height, 4096);

            _shell.Closed += async (x, y) => await Close();

            // This should never fail, channel at this point should be as empty as me.
            _outputQueue.Writer.TryWrite(new ShellOpenedPacket());

            Console.WriteLine("Started shell.");
        }
        catch (Exception ex)
        {
            // Idk maybe send some error to client, so he knows that something went wrong
            // instead of closing connection silently?
            // No?
            // Fine.
            Console.Error.WriteLine($"Some error occured :( {ex.Message}");
        }
        finally
        {
            await Close();
        }
    }

    private async Task WebSocketReceiveLoop()
    {
        ArgumentNullException.ThrowIfNull(_ws, nameof(_ws));
        ArgumentNullException.ThrowIfNull(_shell, nameof(_shell));
        ArgumentNullException.ThrowIfNull(_client, nameof(_client));

        Console.WriteLine("Recv loop started.");

        var recvBuff = new byte[4096];
        bool streamingToShell = false;
        bool ignoring = false;
        var res = await _ws.ReceiveAsync(new ArraySegment<byte>(recvBuff), _cts.Token);

        while (!res.CloseStatus.HasValue)
        {
            if (streamingToShell)
            {
                // I found that there's no async implementation for this :(
                _shell.Write(recvBuff[0..res.Count]);
            }
            else if (!ignoring)
            {
                // Alright, now, we're parsing packet here that could not be complete
                // Because we do not check for EndOfMessage but only SendToShellPacket should so large that it would be a problem.
                // It won't a problem tho because SendToShellPacket can work with partial message.
                // Resize packet size for example is 17 bytes.
                // Technically websocket would allow for things very slowly, byte after byte
                // But this is only a Playground + it looks like something unrealistic, so I don't care.
                var packet = _parser.ParseFromBytes(recvBuff[0..res.Count]);

                if (packet is ResizeShellPacket rp)
                {
                    _shell.ResizeWindow(rp.Columns, rp.Rows, rp.Width, rp.Height);
                }
                else if (packet is SendToShellPacket stsp)
                {
                    _shell.Write(stsp.Data);
                    streamingToShell = true;
                }
                else
                {
                    // Unknown packet, I will ignore it for now. We should send some error message to client tbh.
                    ignoring = true;
                }
            }

            if (res.EndOfMessage)
            {
                streamingToShell = false;
            }

            res = await _ws.ReceiveAsync(new ArraySegment<byte>(recvBuff), _cts.Token);
        }

        Console.WriteLine("Recv loop finished.");
    }

    private async Task WebSocketSendLoop()
    {
        ArgumentNullException.ThrowIfNull(_ws, nameof(_ws));

        Console.WriteLine("Send loop started");
        while (!_ws.CloseStatus.HasValue)
        {
            if (_outputQueue.Reader.Completion.Status == TaskStatus.RanToCompletion)
            {
                break;
            }

            var packet = await _outputQueue.Reader.ReadAsync(_cts.Token);
            var bytes = packet.Serialize();
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, _cts.Token);
        }
        Console.WriteLine("Send loop finished.");
    }

    private async Task ShellReadLoop()
    {
        ArgumentNullException.ThrowIfNull(_shell, nameof(_shell));

        var recv = 0;
        var buff = new byte[4096];

        Console.WriteLine("Shell read loop started");
        while ((recv = await _shell.ReadAsync(buff, 0, 4096, _cts.Token)) > 0 && !_cts.Token.IsCancellationRequested)
        {
            var packet = new ShellDataPacket(buff[0..recv]);
            await _outputQueue.Writer.WriteAsync(packet);
        }
        Console.WriteLine("Shell read loop finished");
    }

    private async Task Close()
    {
        await _cts.CancelAsync();

        if (_ws is not null && !_ws.CloseStatus.HasValue)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
        }

        Dispose();
    }

    public void Dispose()
    {
        _shell?.Dispose();
        _shell = null;

        _client?.Dispose();
        _client = null;
    }
}
