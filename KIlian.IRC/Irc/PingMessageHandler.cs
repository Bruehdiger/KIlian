using Microsoft.Extensions.Options;

namespace KIlian.IRC.Irc;

public class PingMessageHandler(IKIlianIrcClient ircClient, IOptions<IrcOptions> ircOptions) : IIrcMessageHandler
{
    private readonly IrcOptions _ircOptions = ircOptions.Value;

    public Task HandleMessage(string message, Func<string, Task> next)
        => message == $"PING {_ircOptions.Name}" ? ircClient.WriteLineAsync($"PONG {_ircOptions.Server}") : next(message);
}