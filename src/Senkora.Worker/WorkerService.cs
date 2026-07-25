using Hangfire;

namespace Senkora.Worker;

public sealed class WorkerService(ILogger<WorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Senkora Worker started at {Time}", DateTimeOffset.UtcNow);

        // Recurring jobs are registered here
        // Full job registration in Faz 6 (Scheduler phase)
        RecurringJob.AddOrUpdate("health-check", () => Console.WriteLine("Health OK"), Cron.Minutely);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
