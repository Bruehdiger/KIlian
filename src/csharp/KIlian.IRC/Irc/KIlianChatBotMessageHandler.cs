using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace KIlian.IRC.Irc;

public class KIlianChatBotMessageHandler(Chat chat, IKIlianIrcClient ircClient, IOptions<IrcOptions> ircOptions, IOptions<OllamaOptions> ollamaOptions) : IIrcMessageHandler
{
    private readonly OllamaOptions _ollamaOptions = ollamaOptions.Value;
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
    public async Task HandleMessage(string message, Func<string, Task> next)
    {
        var match = Regex.Match(message, @$".*\sPRIVMSG\s{_ircOptions.Channel}\s:@KIlian\s(?<message>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var chatMessage = match.Groups["message"].Value;
        if (match.Success && !string.IsNullOrWhiteSpace(chatMessage))
        {
            if (!await chat.Client.IsRunningAsync())
            {
                await ircClient.WriteChannelMessageAsync("Ollama API ist nicht online.");
                return;
            }

            var runningModels = await chat.Client.ListRunningModelsAsync();
            if (runningModels.All(model => model.Name != chat.Model))
            {
                await ircClient.WriteChannelMessageAsync("KIlian LLM ist nicht online.");
                return;
            }
            
            var responseBuilder = new StringBuilder();
            await foreach (var token in chat.SendAsync(chatMessage))
            {
                responseBuilder.Append(token);
            }

            await ircClient.WriteChannelMessageAsync(responseBuilder.ToString());

            chat.Messages.RemoveRange(0, Math.Max(0, chat.Messages.Count - _ollamaOptions.MaxMessageHistory));
        }
        else
        {
            await next(message);
        }
    }
}