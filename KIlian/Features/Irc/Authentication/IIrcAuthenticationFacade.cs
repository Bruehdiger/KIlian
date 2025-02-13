namespace KIlian.Features.Irc.Authentication;

public interface IIrcAuthenticationFacade
{
    Task AuthenticateAsync<T>(IrcConnectionOptions<T> connectionOptions, IIrcClient irc, CancellationToken cancellationToken = default);
}