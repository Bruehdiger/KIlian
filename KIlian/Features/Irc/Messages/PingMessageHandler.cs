using KIlian.Features.Irc.Extensions;
using Microsoft.Extensions.Options;

namespace KIlian.Features.Irc.Messages;

public class PingMessageHandler(IIrcClient ircClient, IOptions<IrcOptions> ircOptions) : IIrcMessageHandler
{
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
    public async Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        if (message.Command == "PING" && message.Parameters.FirstOrDefault() == _ircOptions.Name)
        {
            await ircClient.WriteMessageAsync(IrcMessage.Pong(_ircOptions.Server), cancellationToken);
        }
    }
}