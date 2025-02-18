using KIlian.Features.Ollama;
using KIlian.Shared.Dashboard;
using KIlian.Shared.Ollama;
using Microsoft.AspNetCore.SignalR;

namespace KIlian.Features.Dashboard;

public class DashboardHub : Hub<IDashboardClient>
{
    public Task<IEnumerable<ConversationTurn>> GetCurrentConversation(IKIlianChatService chat) => Task.FromResult<IEnumerable<ConversationTurn>>(chat.Conversation);
}