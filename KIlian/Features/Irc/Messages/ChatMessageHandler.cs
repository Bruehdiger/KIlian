using KIlian.Features.Irc.Extensions;
using KIlian.Features.Ollama;
using Microsoft.Extensions.Options;

namespace KIlian.Features.Irc.Messages;

public class ChatMessageHandler(IKIlianChatService chat, IIrcClient ircClient, IOptions<IrcOptions> ircOptions) : IIrcMessageHandler
{
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
            
            string? response;
            try
            {
                //if the model is not running at this time, the first generate request will cause ollama to run it
                response = await chat.ChatAsync(new KIlianChatRequest(input), cancellationToken);
            }
            catch (TimeoutException)
            {
                response = "Macht euch weg, ihr nervt";
            }
            catch (Exception)
            {
                response = "Ich hab mich grad übel eingeschissen";
            }

            if (!string.IsNullOrEmpty(response))
            {
                var messages = IrcMessage.PrivMsg(response, _ircOptions.Channel).ToArray();

                if (messages.Length > 10)
                {
                    var user = new string(message.Prefix?.TakeWhile(ch => ch != '!').ToArray());
                    await ircClient.WriteMessageAsync(IrcMessage.Kick([_ircOptions.Channel], [user], "huan"), cancellationToken);
                }
                else
                {
                    await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg($">>> {input}", _ircOptions.Channel), cancellationToken);
                    await ircClient.WriteMessagesAsync(messages, cancellationToken);   
                }
            }
        }
    }
}