using System.Collections.Concurrent;
using KIlian.Features.Dashboard;
using KIlian.Shared.Dashboard;
using KIlian.Shared.Ollama;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace KIlian.Features.Ollama;

public class KIlianChatService(IOllamaApiClient ollama, IOptions<OllamaOptions> ollamaOptions, IHubContext<DashboardHub, IDashboardClient> dashboard) : BackgroundService, IKIlianChatService
{
    private record KIlianRequest(
        TaskCompletionSource<string?> RequestCompletionSource,
        Message Message,
        ChatRequest ChatRequest,
        CancellationToken CancellationToken);
    
    private readonly OllamaOptions _ollamaOptions = ollamaOptions.Value;
    private readonly List<(DateTimeOffset start, Message input, DateTimeOffset end, Message output)> _conversation = [];
    private readonly ConcurrentQueue<KIlianRequest> _requests = [];
    private readonly SemaphoreSlim _requestSignal = new(0);
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
        
        _requests.Enqueue(new(requestCompletionSource, message, chatRequest, cancellationToken));
        
        _requestSignal.Release();
        
        return requestCompletionSource.Task.WaitAsync(cancellationToken);
    }

    private async Task ProcessRequest(TaskCompletionSource<string?> requestCompletionSource, Message message, ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            requestCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }

        try
        {
            var messageBuilder = new MessageBuilder();
            var start = DateTimeOffset.Now;

            ChatResponseStream? finalChunk = null;
            await foreach (var chunk in ollama.ChatAsync(chatRequest, cancellationToken))
            {
                if (chunk is null)
                {
                    continue;
                }

                messageBuilder.Append(chunk);
                finalChunk = chunk;
            }

            if (finalChunk is not ChatDoneResponseStream { Done: true })
            {
                throw new InvalidOperationException("Invalid final chunk");
            }

            var end = DateTimeOffset.Now;
            var responseMessage = messageBuilder.ToMessage();
            requestCompletionSource.SetResult(responseMessage.Content);

            if (!string.IsNullOrEmpty(responseMessage.Content))
            {
                lock (_conversation)
                {
                    _conversation.Add((start, message, end, responseMessage));
                    _conversation.RemoveRange(0,
                        Math.Max(0, _conversation.Count - _ollamaOptions.MaxConversationTurns));
                    _ = dashboard.Clients.All.ReceiveConversationTurn(
                        new(start, message.Content!, end, responseMessage.Content), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            requestCompletionSource.TrySetCanceled(cancellationToken);
        }
        catch (Exception e)
        {
            requestCompletionSource.TrySetException(e);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _requestSignal.WaitAsync(stoppingToken);
                
            if (!_requests.TryDequeue(out var request))
            {
                continue;
            }

            if (await _maxConcurrency.WaitAsync(_ollamaOptions.MaxConcurrencyMillisecondsTimeout, stoppingToken))
            {
                _ = ProcessRequest(request.RequestCompletionSource,
                        request.Message,
                        request.ChatRequest,
                        CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken, stoppingToken).Token)
                    .ContinueWith(_ => _maxConcurrency.Release(), stoppingToken);
            }
            else
            {
                request.RequestCompletionSource.TrySetException(new TimeoutException());
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _requestSignal.Dispose();
        _maxConcurrency.Dispose();
        GC.SuppressFinalize(this);
    }
}