using System.Runtime.CompilerServices;
using KIlian.EfCore;
using KIlian.EfCore.Entities;
using KIlian.Features.Irc.Extensions;
using KIlian.Features.Ollama;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace KIlian.Features.Irc.Messages;

public class ChatMessageHandler(
    IKIlianChatService chat,
    IIrcClient ircClient,
    IDbContextFactory<KIlianSqliteDbContext> dbFactory,
    IIrcMessageHoster hoster,
    IOptions<IrcOptions> ircOptions,
    IOptionsMonitor<OllamaOptions> ollamaOptions) : IIrcMessageHandler
{
    private readonly IrcOptions _ircOptions = ircOptions.Value;
    
    public async Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken)
    {
        if (message.Command != "PRIVMSG")
        {
            return;
        }
        
        var channel = message.Parameters.FirstOrDefault();
        if (!_ircOptions.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dbMessages = new List<Message>();
        try
        {
            await foreach (var (content, from) in ProcessChatMessageAsync(message, cancellationToken))
            {
                dbMessages.Add(new() { Content = content, From = from, Created = DateTimeOffset.Now });
            }
        }
        finally
        {
            _ = SaveMessagesAsync(dbMessages);
        }

        return;

        async Task SaveMessagesAsync(IList<Message> messages)
        {
            if (messages.Count < 1)
            {
                return;
            }
            
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            await db.AddRangeAsync(messages, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            
            //TODO: log exceptions lol
        }
    }

    private async IAsyncEnumerable<(string, string)> ProcessChatMessageAsync(IrcMessage message, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var input = message.Trailing ?? "";
        var user = new string(message.Prefix?.TakeWhile(ch => ch != '!').ToArray());
        
        if (input.StartsWith("@kilian ", StringComparison.OrdinalIgnoreCase))
        {
            input = input["@kilian ".Length..].Trim();
            if (string.IsNullOrEmpty(input))
            {
                yield break;
            }
            
            yield return (input, user);
            
            if (!await chat.Client.IsRunningAsync(cancellationToken))
            {
                await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg("Ollama API ist nicht online.", _ircOptions.Channel), cancellationToken);
                yield break;
            }
            
            string? response;
            var success = false;
            try
            {
                var request = new KIlianChatRequest(input);
                if (ollamaOptions.CurrentValue.RequestParameters is not null)
                {
                    var parameters = ollamaOptions.CurrentValue.RequestParameters;
                    request.Options = new RequestOptions
                    {
                        Temperature = parameters.Temperature,
                        TopK = parameters.TopK,
                        RepeatPenalty = parameters.RepeatPenalty,
                        TopP = parameters.TopP,
                        RepeatLastN = parameters.RepeatLastN,
                    };
                }
                
                //if the model is not running at this time, the first generate request will cause ollama to run it
                response = await chat.ChatAsync(request, cancellationToken);
                success = true;
            }
            catch (TimeoutException)
            {
                response = "Macht euch weg, ihr nervt";
            }
            catch (Exception)
            {
                response = "Ich hab mich grad übel eingeschissen";
            }

            if (string.IsNullOrEmpty(response))
            {
                yield break;
            }

            if (success)
            {
                yield return (response, _ircOptions.Name);
            }
                
            var messages = IrcMessage.PrivMsg(response, _ircOptions.Channel).ToArray();

            if (messages.Length > _ircOptions.MessagesThreshold)
            {
                var (token, newResponse) = await hoster.UploadMessage(response, cancellationToken).ContinueWith(
                    task => task.IsCompletedSuccessfully
                        ? task.Result
                        : ("", "Meine Antwort ist zu lang und der FileHoster will sie auch nicht (du Opfer)."),
                    cancellationToken);
                messages = IrcMessage.PrivMsg($"{newResponse} {token}".Trim(), _ircOptions.Channel).ToArray();
            }
            
            await ircClient.WriteMessagesAsync(IrcMessage.PrivMsg($">>> {input}", _ircOptions.Channel), cancellationToken);
            await ircClient.WriteMessagesAsync(messages, cancellationToken);
        } else if (!string.IsNullOrEmpty(input))
        {
            yield return (input, user);
        }
    }
}

public interface IIrcMessageHoster
{
    Task<(string token, string link)> UploadMessage(string message, CancellationToken cancellationToken = default);
}

public class NullPointerService(HttpClient httpClient) : IIrcMessageHoster
{
    public async Task<(string, string)> UploadMessage(string message, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(message), "file", $"{Guid.NewGuid()}.txt");
        using var response = await httpClient.PostAsync("https://0x0.st", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (response.Headers.GetValues("X-Token").FirstOrDefault() ?? "Bruder wo ist mein Token?", (await response.Content.ReadAsStringAsync(cancellationToken)).Trim());
    }
}