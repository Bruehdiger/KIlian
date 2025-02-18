using KIlian.Shared.Ollama;

namespace KIlian.Shared.Dashboard;

public interface IDashboardClient
{
    Task ReceiveIrcMessage(string message, CancellationToken cancellationToken = default);
    
    Task ReceiveConversationTurn(ConversationTurn conversationTurn, CancellationToken cancellationToken = default);
}