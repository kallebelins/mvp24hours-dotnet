using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace App.Function;

public class HeartbeatFunction(ILogger<HeartbeatFunction> logger)
{
    [Function("Heartbeat")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        logger.LogInformation("Heartbeat at {TimeUtc}", DateTime.UtcNow);

        if (timer.ScheduleStatus is not null)
        {
            logger.LogDebug("Next heartbeat at {Next}", timer.ScheduleStatus.Next);
        }
    }
}
