namespace KIlian.IRC.Irc;

public static class IrcClientExtensions
{
    public static async Task WriteChannelMessageAsync(this IKIlianIrcClient ircClient, string message, CancellationToken cancellationToken = default)
    {
        var lines = message.Split('\n').Select(l => l.Trim()).ToArray();

        var messages = lines.SelectMany(line => SplitLine(line, ircClient.Channel));

        foreach (var msg in messages)
        {
            await ircClient.WriteLineAsync(msg, cancellationToken);
        }
    }
    
    public static void WriteChannelMessage(this IKIlianIrcClient ircClient, string message)
    {
        var lines = message.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();

        var messages = lines.SelectMany(line => SplitLine(line, ircClient.Channel));

        foreach (var msg in messages)
        {
            ircClient.WriteLine(msg);
        }
    }
    
    private static IEnumerable<string> SplitLine(string line, string channel)
    {
        var prefix = $"PRIVMSG {channel} :";
        
        while (true)
        {
            var message = $"{prefix}{line}";
            if (message.Length <= 510)
            {
                yield return message;
            }
            else
            {
                yield return message[..510];
                line = message[510..];
                continue;
            }

            break;
        }
    }
}