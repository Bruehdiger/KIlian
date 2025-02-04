namespace KIlian.IRC.Irc;

public interface IKIlianIrcClient : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<string?> ReadLineAsync(CancellationToken cancellationToken = default);
    Task WriteLineAsync(string message, CancellationToken cancellationToken = default);
    void WriteLine(string message);

    string Channel { get; }
}