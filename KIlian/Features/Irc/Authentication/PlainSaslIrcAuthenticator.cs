using KIlian.Features.Irc.Extensions;
using KIlian.Features.Irc.Messages;
using Microsoft.AspNetCore.Authentication;

namespace KIlian.Features.Irc.Authentication;

public class PlainSaslIrcAuthenticator : IIrcAuthenticator<PlainSaslCredentials>
{
    public async Task AuthenticateAsync(IrcConnectionOptions<PlainSaslCredentials> connectionOptions, IIrcClient irc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionOptions.Credentials);
        
        if (!string.IsNullOrEmpty(connectionOptions.Credentials.ServerPass))
        {
            await irc.WriteMessageAsync(IrcMessage.Pass(connectionOptions.Credentials.ServerPass), cancellationToken);
        }

        await irc.WriteMessageAsync(IrcMessage.AuthenticatePlain(), cancellationToken);
        var response = await irc.ReadLineAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
        if (response != "AUTHENTICATE +")
        {
            throw new AuthenticationFailureException("Unable to authenticate with server.");
        }
        
        await irc.WriteMessageAsync(IrcMessage.AuthenticatePlain(connectionOptions.Nick, connectionOptions.Credentials.Password, irc.CurrentEncoding), cancellationToken);
        var rplLoggedIn = await irc.ReadMessageAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
        var rplSaslSuccess = await irc.ReadMessageAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
        if (rplLoggedIn?.Command != "900" || rplSaslSuccess?.Command != "903")
        {
            throw new AuthenticationFailureException("Unable to authenticate with server.");
        }
    }
}