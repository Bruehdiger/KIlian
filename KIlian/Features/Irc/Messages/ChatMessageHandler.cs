using System.Text;
using KIlian.Features.Irc.Extensions;
using KIlian.Features.Ollama;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace KIlian.Features.Irc.Messages;

public class ChatMessageHandler(Chat chat, IIrcClient ircClient, IOptions<IrcOptions> ircOptions, IOptions<OllamaOptions> ollamaOptions) : IIrcMessageHandler
{
    private readonly OllamaOptions _ollamaOptions = ollamaOptions.Value;
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    //Chat uses a simple list for tracking messages, which causes problems, when being accessed by multiple threads at once
    //Need to create a wrapper around Chat to make it thread safe, so I can handle multiple requests at the same time
    //So for now rate limiter is just a lock replacement (can't use async-await inside of a lock statement)
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    
    public async Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        var channel = message.Parameters.FirstOrDefault();
        var input = message.Trailing ?? "";
        if (message.Command == "PRIVMSG" && channel == _ircOptions.Channel && input.StartsWith("@kilian ", StringComparison.OrdinalIgnoreCase))
        {
            input = input["@kilian ".Length..].Trim();
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            try
            {
                await _rateLimiter.WaitAsync(cancellationToken);
                
                if (!await chat.Client.IsRunningAsync(cancellationToken))
                {
                    await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg("Ollama API ist nicht online.", _ircOptions.Channel), cancellationToken);
                    return;
                }
            
                //if the model is not running at this time, the first generate request will cause ollama to run it
                var responseBuilder = new StringBuilder();
                await foreach (var token in chat.SendAsync(input, cancellationToken))
                {
                    responseBuilder.Append(token);
                }

                await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg(responseBuilder.ToString(), _ircOptions.Channel), cancellationToken);

                chat.Messages.RemoveRange(0, Math.Max(0, chat.Messages.Count - _ollamaOptions.MaxMessageHistory));
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}