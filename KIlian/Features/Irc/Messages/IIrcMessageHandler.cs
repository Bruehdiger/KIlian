namespace KIlian.Features.Irc.Messages;

public interface IIrcMessageHandler
{
    Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken = default);
}