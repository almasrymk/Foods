namespace Foods.API.Notifications
{
    using Foods.Application.Customer.Notifications;

    /// <summary>Runs the notification generator shortly after startup and then every hour.</summary>
    public sealed class NotificationWorker(IServiceScopeFactory scopes, ILogger<NotificationWorker> logger) : BackgroundService
    {
        static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { return; }

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var generator = scope.ServiceProvider.GetRequiredService<NotificationGenerator>();
                    var added = await generator.RunAsync(DateTime.UtcNow, stoppingToken);
                    if (added > 0)
                        logger.LogInformation("Notifications generated: {Count}", added);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Notification generator failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
