using KIlian.Features.Dashboard;
using KIlian.Shared.Dashboard;
using Microsoft.AspNetCore.SignalR;

namespace KIlian.Features.Irc.Messages;

public class IrcMessageNotificationHandler(IHubContext<DashboardHub, IDashboardClient> dashboard) : IIrcMessageHandler
{
    public Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken = default) => dashboard.Clients.All.ReceiveIrcMessage(message.ToString(), cancellationToken);
}