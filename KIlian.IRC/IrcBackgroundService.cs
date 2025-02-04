using KIlian.IRC.Irc;
using Microsoft.Extensions.Hosting;

namespace KIlian.IRC;

public class KIlianBackgroundService(IKIlianIrcClient ircClient, IEnumerable<IIrcMessageHandler> messageHandlers) : BackgroundService
{
    private readonly IIrcMessageHandler[] _messageHandlers = messageHandlers.ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ircClient.ConnectAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await ircClient.ReadLineAsync(stoppingToken);

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            Console.WriteLine(message);

            await HandleIrcMessage(message);
        }

        async Task HandleIrcMessage(string message)
        {
            var index = 0;
            Func<string, Task> next = null!;
            next = msg =>
            {
                var handler = _messageHandlers[index];

                if (++index == _messageHandlers.Length)
                {
                    next = _ => Task.CompletedTask; //we only respond to specific messages and ignore all others lol
                }
                
                return handler.HandleMessage(msg, next);
            };
            
            await next(message);
        }
    }
}