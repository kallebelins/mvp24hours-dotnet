using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace App.Function;

public class HeartbeatFunction(ILogger<HeartbeatFunction> logger)
{
    [Function("Heartbeat")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        logger.LogInformation("Function complex heartbeat at {TimeUtc}", DateTime.UtcNow);
    }
}
