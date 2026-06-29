using Microsoft.Extensions.Logging;

namespace Resto.Infrastructure.Events;

internal static class EventDispatchRetry
{
    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000),
    ];

    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        ILogger logger,
        string eventType,
        CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < BackoffDelays.Length)
            {
                attempt++;
                logger.LogWarning(
                    ex,
                    "Reintento {Attempt}/{MaxAttempts} al despachar evento {EventType}",
                    attempt,
                    BackoffDelays.Length,
                    eventType);

                await Task.Delay(BackoffDelays[attempt - 1], cancellationToken);
            }
        }
    }
}
