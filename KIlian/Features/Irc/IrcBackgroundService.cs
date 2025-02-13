using KIlian.Features.Irc.Authentication;
using KIlian.Features.Irc.Extensions;
using KIlian.Features.Irc.Messages;
using Microsoft.Extensions.Options;

namespace KIlian.Features.Irc;

public class IrcBackgroundService(IIrcClient ircClient, IEnumerable<IIrcMessageHandler> messageHandlers, IOptions<IrcOptions> ircOptions) : BackgroundService, IHostedLifecycleService
{
    private readonly IIrcMessageHandler[] _messageHandlers = messageHandlers.ToArray();
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!ircClient.IsConnected)
            {
                await ircClient.ConnectAsync<PlainSaslCredentials>(new()
                {
                    Server = _ircOptions.Server,
                    Port = _ircOptions.Port,
                    Nick = _ircOptions.Name,
                    IsSecure = true,
                    Credentials = new PlainSaslCredentials
                    {
                        Password = _ircOptions.KIlianPassword,
                        ServerPass = _ircOptions.ServerPass,
                    }
                }, stoppingToken);
                
                await ircClient.WriteMessageAsync(IrcMessage.Join(_ircOptions.Channel), stoppingToken);
            }
            
            var message = await ircClient.ReadMessageAsync(cancellationToken: stoppingToken);
            if (message is null)
            {
                continue;
            }

            foreach (var handler in _messageHandlers)
            {
                //TODO: handle potential errors using ContinueWith or something idk
                _ = handler.InvokeAsync(message, stoppingToken);
            }
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => ircClient.WriteMessageAsync(IrcMessage.Quit("Ich gehe jetzt in deine mamer nussen"), cancellationToken: cancellationToken);
}