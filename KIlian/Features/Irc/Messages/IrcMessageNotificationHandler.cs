using KIlian.Features.Dashboard;
using Microsoft.AspNetCore.SignalR;

namespace KIlian.Features.Irc.Messages;

public class IrcMessageNotificationHandler(IHubContext<DashboardHub> dashboard) : IIrcMessageHandler
{
    public Task InvokeAsync(IrcMessage message, CancellationToken cancellationToken = default) => dashboard.Clients.All.SendAsync("ReceiveIrcMessage", message.ToString(), cancellationToken);
}