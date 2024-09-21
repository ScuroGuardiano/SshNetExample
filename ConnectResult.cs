namespace SshPlayground;

public enum ConnectResult
{
    CONNECTED = 0,
    ALREADY_CONNECTED = 1,
    AUTHENTICATION_FAILURE = 2,
    SSH_CONNECTION_FAILURE = 3,
}
