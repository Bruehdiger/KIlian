using OllamaSharp;

namespace KIlian.Features.Ollama;

public interface IKIlianChatService
{
    IOllamaApiClient Client { get; }
    
    Task<string?> ChatAsync(KIlianChatRequest request, CancellationToken cancellationToken = default);
}