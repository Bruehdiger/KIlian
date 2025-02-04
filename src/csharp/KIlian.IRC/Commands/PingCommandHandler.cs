using CommandLineExtensions;
using KIlian.IRC.Irc;

namespace KIlian.IRC.Commands;

public class PingCommandHandler(IKIlianIrcClient ircClient) : ICommandOptionsHandler<EmptyCommandOptions>
{
    public async Task<int> HandleAsync(EmptyCommandOptions options, CancellationToken cancellationToken)
    {
        await ircClient.WriteChannelMessageAsync("halt's maul du huan", cancellationToken);
        return 0;
    }
}