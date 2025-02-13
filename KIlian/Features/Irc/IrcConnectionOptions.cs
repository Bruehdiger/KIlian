namespace KIlian.Features.Irc;

public class IrcConnectionOptions<T>
{
    private const int IrcDefaultPort = 6667;
    private readonly string? _realname;
    
    public required string Server { get; init; }

    public required string Nick { get; init; }

    public string Realname
    {
        get => string.IsNullOrEmpty(_realname) ? Nick : _realname;
        init => _realname = value.Trim();
    }
    
    public int Port { get; init; } = IrcDefaultPort;

    public bool IsSecure { get; init; }

    public int Mode { get; init; }

    public T? Credentials { get; init; }
}