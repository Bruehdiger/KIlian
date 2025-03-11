using KIlian.Shared.Ollama;
using OllamaSharp;

namespace KIlian.Features.Ollama;

public interface IKIlianChatService
{
    IOllamaApiClient Client { get; }
    
    Task<string?> ChatAsync(KIlianChatRequest request, CancellationToken cancellationToken = default);
    
    IReadOnlyList<KIlianConversationTurn> Conversation { get; }
}