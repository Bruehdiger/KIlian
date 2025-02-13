using System.Text;

namespace KIlian.Features.Irc;

public interface IIrcClient : IAsyncDisposable
{
    Encoding CurrentEncoding { get; }
    
    bool IsConnected { get; }
    
    Task ConnectAsync<T>(IrcConnectionOptions<T> connectionOptions, CancellationToken cancellationToken = default);
    
    Task<string?> ReadLineAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    
    Task WriteLineAsync(string value, CancellationToken cancellationToken = default);
}