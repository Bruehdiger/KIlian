using System.Collections.Concurrent;
using KIlian.Features.Dashboard;
using KIlian.Shared.Dashboard;
using KIlian.Shared.Ollama;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace KIlian.Features.Ollama;

//This service makes it so that the model gets hit with multiple messages at the same time, because KIlian is part of a group chat/"asynchronous" chats.
//So the situation is kinda cringe, because multiple people can ask multiple questions at the same time.
//Usually it's a strictly sequential conversation for a chat bot (input -> response -> input -> response).
//Which is probably the reason, the chat api of OllamaSharp doesn't care about bullshit like parallelism or thread safety.
//But I can't stop people from sending messages in IRC during response generation.
//So now I have this abomination with concurrency limits and shit.
//The previous option was to sequentially process inputs as they come in one after another.
//But that would be easy and boring lol.
public class KIlianChatService(IOllamaApiClient ollama, IOptions<OllamaOptions> ollamaOptions, IHubContext<DashboardHub, IDashboardClient> dashboard) : BackgroundService, IKIlianChatService
{
    private readonly List<(DateTimeOffset start, Message input, DateTimeOffset end, Message output)> _conversation = [];
    private readonly ConcurrentQueue<(ChatRequest chatReq, Func<ChatRequest, Task<(DateTimeOffset start, Message input, DateTimeOffset end, Message output)?>> requestFunc)> _requests = [];
    private readonly SemaphoreSlim _requestSignal = new(0);
    private readonly ConcurrentQueue<Task<(DateTimeOffset start, Message input, DateTimeOffset end, Message output)?>> _responses = [];
    private readonly SemaphoreSlim _responseSignal = new(0);
    private readonly SemaphoreSlim _maxConcurrency = new(ollamaOptions.Value.MaxConcurrentRequests, ollamaOptions.Value.MaxConcurrentRequests);
    
    public IOllamaApiClient Client => ollama;

    public IReadOnlyList<ConversationTurn> Conversation
    {
        get
        {
            lock (_conversation)
            {
                return _conversation.Select(c => new ConversationTurn(c.start, c.input.Content!, c.end, c.output.Content!)).ToArray();
            }
        }
    }
    
    public Task<string?> ChatAsync(KIlianChatRequest request, CancellationToken cancellationToken = default)
    {
        var chatRequest = new ChatRequest
        {
            Model = ollama.SelectedModel,
            Stream = !request.Tools.Any(),
            Tools = request.Tools.ToArray(),
            Format = request.Format,
            Options = request.Options
        };
        
        var message = new Message
        {
            Content = request.Message,
            Role = request.Role,
            Images = request.Base64Images.ToArray()
        };
        
        lock (_conversation)
        {
            chatRequest.Messages = _conversation.SelectMany(c => new[] { c.input, c.output }).Append(message).ToArray();
        }
        
        var requestCompletionSource = new TaskCompletionSource<string?>();
        
        _requests.Enqueue((chatRequest, async chatReq =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            
            try
            {
                var messageBuilder = new MessageBuilder();
                var start = DateTimeOffset.Now;
                
                await foreach (var chunk in ollama.ChatAsync(chatReq, cancellationToken))
                {
                    if (chunk != null)
                    {
                        messageBuilder.Append(chunk);
                    }
                }
            
                var end = DateTimeOffset.Now;
                var responseMessage = messageBuilder.ToMessage();
                requestCompletionSource.SetResult(responseMessage.Content);
                return string.IsNullOrEmpty(responseMessage.Content) ? null : (start, message, end, responseMessage);
            }
            catch (Exception e)
            {
                requestCompletionSource.SetException(e);
                return null;
            }
        }));
        
        _requestSignal.Release();
        
        return requestCompletionSource.Task.WaitAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var requestTask = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _requestSignal.WaitAsync(stoppingToken);
                
                if (!_requests.TryDequeue(out var request))
                {
                    continue;
                }
            
                await _maxConcurrency.WaitAsync(stoppingToken);
            
                _responses.Enqueue(request.requestFunc(request.chatReq));
                _responseSignal.Release();
            }
        }, stoppingToken);

        var responseTask = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _responseSignal.WaitAsync(stoppingToken);
            
                if (!_responses.TryDequeue(out var response))
                {
                    continue;
                }

                var conversationTurn = await response.ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, stoppingToken);
                
                _maxConcurrency.Release();
                
                if (!conversationTurn.HasValue)
                {
                    continue;
                }

                lock (_conversation)
                {
                    _conversation.Add(conversationTurn.Value);
                    _conversation.RemoveRange(0, Math.Max(0, _conversation.Count - ollamaOptions.Value.MaxConversationTurns));
                    _ = dashboard.Clients.All.ReceiveConversationTurn(new(conversationTurn.Value.start, conversationTurn.Value.input.Content!, conversationTurn.Value.end, conversationTurn.Value.output.Content!), stoppingToken);
                }
            }
        }, stoppingToken);
        
        return Task.WhenAll(requestTask, responseTask);
    }

    public override void Dispose()
    {
        base.Dispose();
        _requestSignal.Dispose();
        _responseSignal.Dispose();
        _maxConcurrency.Dispose();
        GC.SuppressFinalize(this);
    }
}