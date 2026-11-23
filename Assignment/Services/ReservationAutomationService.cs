namespace Assignment.Services;

/// <summary>Runs <see cref="ReservationService.RunAutomationAsync"/> on a schedule.</summary>
public class ReservationAutomationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReservationAutomationService> _logger;
    private readonly TimeSpan _interval;

    public ReservationAutomationService(IServiceScopeFactory scopes, ILogger<ReservationAutomationService> logger, IConfiguration config)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Workflow:AutomationIntervalMinutes", 15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                int changed = await scope.ServiceProvider.GetRequiredService<ReservationService>().RunAutomationAsync();
                if (changed > 0) _logger.LogInformation("Reservation automation updated {Count} reservation(s)", changed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Reservation automation failed");
            }
            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
