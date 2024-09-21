using SshPlayground;
using SshPlayground.Packets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            });
});

builder.Services.AddSingleton<PacketParser>();
builder.Services.AddScoped<SshWsShellHandler>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "Usage: /open-shell?host=<host>&username=<username>&password=<password>&cols=<columns>&rows=<rows>&width=<width>&height=<height>");

app.MapGet("/open-shell", async (
            HttpContext context,
            SshWsShellHandler ssh,
            CancellationToken cancellationToken
            ) =>
{
    var host = context.Request.Query["host"][0];
    var username = context.Request.Query["username"][0];
    var password = context.Request.Query["password"][0];
    var colsStr = context.Request.Query["cols"][0];
    var rowsStr = context.Request.Query["rows"][0];
    var widthStr = context.Request.Query["width"][0];
    var heightStr = context.Request.Query["height"][0];

    var queryValid = host is not null && username is not null && password is not null;

    _ = uint.TryParse(colsStr, out var cols);
    cols = cols == 0 ? 80 : cols;
    _ = uint.TryParse(rowsStr, out var rows);
    rows = rows == 0 ? 24 : rows;
    _ =uint.TryParse(widthStr, out var width);
    width = width == 0 ? 800 : width;
    _ = uint.TryParse(heightStr, out var height);
    height = height == 0 ? 600 : height;

    if (context.WebSockets.IsWebSocketRequest && queryValid)
    {
        var result = await ssh.Connect(host!, username!, password!, cancellationToken);

        if (result == ConnectResult.AUTHENTICATION_FAILURE)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Message = "SSH Authentication failed." });
        }

        if (result == ConnectResult.SSH_CONNECTION_FAILURE)
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            await context.Response.WriteAsJsonAsync(new { Message = "Connection to SSH failed. It maybe due to server configuration or client failure, idk, I am a teapot." });
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await ssh.HandleWebsocketAsync(webSocket, cols, rows, width, height, cancellationToken);

        return;
    }

    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(new { Message = "Spadaj, frajerze" }, cancellationToken: cancellationToken);
});

app.Run();
