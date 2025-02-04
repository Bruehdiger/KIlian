using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace KIlian.IRC.Irc;

public class KIlianIrcClient(IPasswordManager passwordManager, IOptions<IrcOptions> ircOptions) : IKIlianIrcClient
{
    private static readonly Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IrcOptions _ircOptions = ircOptions.Value;
    private TcpClient _client = new();
    private StreamReader _reader = StreamReader.Null;
    private StreamWriter _writer = StreamWriter.Null;

    public async Task ConnectAsync(CancellationToken stoppingToken)
    {
        await _client.ConnectAsync(_ircOptions.Server, _ircOptions.Port, stoppingToken);
        var stream = new SslStream(_client.GetStream(), leaveInnerStreamOpen: true);
        await stream.AuthenticateAsClientAsync(_ircOptions.Server);
        _reader = new StreamReader(stream, _encoding, leaveOpen: true);
        _writer = new StreamWriter(stream, _encoding, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        
        await _writer.WriteLineAsync($"PASS {passwordManager.GetPassword("ServerPass")}");
        await _writer.WriteLineAsync("AUTHENTICATE PLAIN");
        var response = await ReadExpectedServerResponseAsync();
        if (!response.Contains("AUTHENTICATE +"))
        {
            throw new InvalidOperationException("Unable to connect to server");
        }
        
        await _writer.WriteLineAsync($"AUTHENTICATE {Convert.ToBase64String(_encoding.GetBytes($"\0{_ircOptions.Name}\0{passwordManager.GetPassword("KIlian")}"))}");
        response = await ReadExpectedServerResponseAsync();
        var response2 = await ReadExpectedServerResponseAsync();
        if (!response.StartsWith($":{_ircOptions.Server} 900") || !response2.StartsWith($":{_ircOptions.Server} 903"))
        {
            throw new InvalidOperationException("Login failed");
        }
        
        await _writer.WriteLineAsync($"NICK {_ircOptions.Name}");
        await _writer.WriteLineAsync($"USER {_ircOptions.Name} * 1 :{_ircOptions.Name}");

        await _writer.WriteLineAsync($"JOIN {_ircOptions.Channel}");

        _ = ReconnectAsync(stoppingToken);
        return;
        
        async Task<string> ReadExpectedServerResponseAsync() => await _reader.ReadLineAsync(stoppingToken).AsTask().WaitAsync(TimeSpan.FromSeconds(1), stoppingToken) ?? "";
    }

    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        await _reader.ReadLineAsync(cancellationToken);
    
    public async Task WriteLineAsync(string message, CancellationToken cancellationToken) =>
        await _writer.WriteLineAsync(message.ToCharArray(), cancellationToken);

    public void WriteLine(string message) => _writer.WriteLine(message);
    
    public string Channel => _ircOptions.Channel;

    public async ValueTask DisposeAsync()
    {
        if (_client.Connected)
        {
            await _writer.WriteLineAsync("QUIT :ich gehe jetzt in deine mamer nussen");
        }
        
        _reader.Dispose();
        await _writer.DisposeAsync();
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_client.Connected)
            {
                await Task.Delay(200, cancellationToken);
                continue;
            }
            
            _client.Dispose();
            _client = new();
            await ConnectAsync(cancellationToken);
            break;
        }
    }
}