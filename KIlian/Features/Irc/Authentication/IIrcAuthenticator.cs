namespace KIlian.Features.Irc.Authentication;

public interface IIrcAuthenticator<T>
{
    Task AuthenticateAsync(IrcConnectionOptions<T> connectionOptions, IIrcClient irc, CancellationToken cancellationToken = default);
}