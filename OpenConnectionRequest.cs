namespace SshPlayground;

public class OpenConnectionRequest
{
    public required string Host { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
