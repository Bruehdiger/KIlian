using KIlian.Features.Irc.Extensions;
using KIlian.Features.Ollama;
using Microsoft.Extensions.Options;

namespace KIlian.Features.Irc.Messages;

public class ChatMessageHandler(IKIlianChatService chat, IIrcClient ircClient, IOptions<IrcOptions> ircOptions, IOptions<OllamaOptions> ollamaOptions) : IIrcMessageHandler
{
    private readonly OllamaOptions _ollamaOptions = ollamaOptions.Value;
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
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

            if (!await chat.Client.IsRunningAsync(cancellationToken))
            {
                await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg("Ollama API ist nicht online.", _ircOptions.Channel), cancellationToken);
                return;
            }
            
            //if the model is not running at this time, the first generate request will cause ollama to run it
            var response = await chat.ChatAsync(new KIlianChatRequest(input), cancellationToken);

            if (!string.IsNullOrEmpty(response))
            {
                await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg($">>> {input}", _ircOptions.Channel), cancellationToken);
                await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg(response, _ircOptions.Channel), cancellationToken);
            }
        }
    }
}