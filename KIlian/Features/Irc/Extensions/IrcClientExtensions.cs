using KIlian.Features.Irc.Messages;

namespace KIlian.Features.Irc.Extensions;

public static class IrcClientExtensions
{
    public static async Task<IrcMessage?> ReadMessageAsync(this IIrcClient reader, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var message = await reader.ReadLineAsync(timeout, cancellationToken);
        return string.IsNullOrEmpty(message) ? null : new IrcMessage(message);
    }
    
    public static Task WriteMessageAsync(this IIrcClient writer, IrcMessage message,
        CancellationToken cancellationToken = default) => writer.WriteLineAsync(message.ToString(), cancellationToken);

    public static async Task WriteMessagesAsync(this IIrcClient writer, IEnumerable<IrcMessage> messages,
        CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            await WriteMessageAsync(writer, message, cancellationToken);
        }
    }
}