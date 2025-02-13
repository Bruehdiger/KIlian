namespace KIlian.Features.Irc.Authentication;

public class IrcAuthenticationFacade(IServiceProvider serviceProvider) : IIrcAuthenticationFacade
{
    public Task AuthenticateAsync<T>(IrcConnectionOptions<T> connectionOptions, IIrcClient irc,
        CancellationToken cancellationToken = default)
    {
        var authenticator = serviceProvider.GetRequiredService<IIrcAuthenticator<T>>();
        return authenticator.AuthenticateAsync(connectionOptions, irc, cancellationToken);
    }
}