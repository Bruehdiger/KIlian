using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using KIlian.Features.Irc.Authentication;
using KIlian.Features.Irc.Extensions;
using KIlian.Features.Irc.Messages;

namespace KIlian.Features.Irc;

public class IrcClient(IIrcAuthenticationFacade authenticationFacade) : IIrcClient
{
    private static readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    
    private readonly TcpClient _client = new();
    private StreamReader _reader = StreamReader.Null;
    private StreamWriter _writer = StreamWriter.Null;
    private bool _disposed;
    
    public Encoding CurrentEncoding => _encoding;
    
    public bool IsConnected => _client.Connected;

    public async Task ConnectAsync<T>(IrcConnectionOptions<T> connectionOptions, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (IsConnected)
        {
            return;
        }
        
        await _client.ConnectAsync(connectionOptions.Server, connectionOptions.Port, cancellationToken);
        Stream stream = _client.GetStream();
        if (connectionOptions.IsSecure)
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: true);
            await sslStream.AuthenticateAsClientAsync(connectionOptions.Server);
            stream = sslStream;
        }
        _reader = new StreamReader(stream, _encoding, leaveOpen: true);
        _writer = new StreamWriter(stream, _encoding, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };

        if (connectionOptions.Credentials is not null)
        {
            await authenticationFacade.AuthenticateAsync(connectionOptions, this, cancellationToken);
        }
        
        await this.WriteMessageAsync(IrcMessage.Nick(connectionOptions.Nick), cancellationToken);
        await this.WriteMessageAsync(IrcMessage.User(connectionOptions.Nick, connectionOptions.Mode, connectionOptions.Realname), cancellationToken);
    }
    
    public async Task<string?> ReadLineAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!IsConnected)
        {
            return null;
        }
        
        return await _reader.ReadLineAsync(cancellationToken);
    }

    public async Task WriteLineAsync(string value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (!IsConnected)
        {
            return;
        }
        
        await _writer.WriteLineAsync(value.ToCharArray(), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        
        await CastAndDispose(_client);
        await CastAndDispose(_reader);
        await _writer.DisposeAsync();
        GC.SuppressFinalize(this);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}