using KIlian.Shared.Ollama;

namespace KIlian.Shared.Dashboard;

public interface IDashboardClient
{
    Task ReceiveIrcMessage(string message, CancellationToken cancellationToken = default);
    
    Task ReceiveConversationTurn(KIlianConversationTurn conversationTurn, CancellationToken cancellationToken = default);
}