using System.Collections.Concurrent;
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
public class KIlianChatService(IOllamaApiClient ollama, IOptions<OllamaOptions> ollamaOptions) : BackgroundService, IKIlianChatService
{
    private readonly List<Message> _messages = [];
    private readonly ConcurrentQueue<(ChatRequest chatReq, Func<ChatRequest, Task<Message?>> requestFunc)> _requests = [];
    private readonly SemaphoreSlim _requestSignal = new(0);
    private readonly ConcurrentQueue<(ChatRequest chatReq, Task<Message?> responseTask)> _responses = [];
    private readonly SemaphoreSlim _responseSignal = new(0);
    private readonly SemaphoreSlim _maxConcurrency = new(ollamaOptions.Value.MaxConcurrentRequests, ollamaOptions.Value.MaxConcurrentRequests);
    
    public IOllamaApiClient Client => ollama;

    public IReadOnlyList<Message> Messages
    {
        get
        {
            lock (_messages)
            {
                return _messages.ToArray();
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
        
        lock (_messages)
        {
            chatRequest.Messages = _messages.Append(message).ToArray();
        }
        
        var requestCompletionSource = new TaskCompletionSource<string?>();
        
        _requests.Enqueue((chatRequest, chatReq => Task.Run(async () =>
        {
            try
            {
                var messageBuilder = new MessageBuilder();
                await foreach (var chunk in ollama.ChatAsync(chatReq, cancellationToken))
                {
                    if (chunk != null)
                    {
                        messageBuilder.Append(chunk);
                    }
                }
            
                var responseMessage = messageBuilder.ToMessage();
                requestCompletionSource.SetResult(responseMessage.Content);
                return responseMessage;
            }
            catch (Exception e)
            {
                requestCompletionSource.SetException(e);
                return null;
            }
        }, cancellationToken)));
        
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
            
                _responses.Enqueue((request.chatReq, request.requestFunc(request.chatReq)));
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

                var responseMessage = await response.responseTask.ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, stoppingToken);
                
                _maxConcurrency.Release();
                
                if (string.IsNullOrEmpty(responseMessage?.Content))
                {
                    continue;
                }

                lock (_messages)
                {
                    _messages.Add(response.chatReq.Messages!.Last());
                    _messages.Add(responseMessage);
                    _messages.RemoveRange(0, Math.Max(0, _messages.Count - ollamaOptions.Value.MaxMessageHistory));
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
    }
}