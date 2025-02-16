using Microsoft.AspNetCore.SignalR.Client;

namespace KIlian.Dashboard.Features.SignalR;

public class IndefiniteRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount < 6)
        {
            return TimeSpan.FromSeconds(retryContext.PreviousRetryCount * 10);
        }

        retryContext.PreviousRetryCount = 6;
        return TimeSpan.FromSeconds(60);
    }
}