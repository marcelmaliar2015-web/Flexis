using Flexis.Application.MailCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Flexis.Infrastructure.MailCheck;

internal sealed class MailCheckAutoCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MailCheckAutoCheckWorker> _logger;

    public MailCheckAutoCheckWorker(
        IServiceScopeFactory scopes,
        ILogger<MailCheckAutoCheckWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MailCheck server auto-check worker started. IntervalSeconds={IntervalSeconds}",
            MailCheckAutoCheck.IntervalSeconds);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(MailCheckAutoCheck.StartupDelaySeconds), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MailCheck server auto-check tick failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(MailCheckAutoCheck.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("MailCheck server auto-check worker stopped");
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var listScope = _scopes.CreateScope();
        var settings = listScope.ServiceProvider.GetRequiredService<IMailCheckSettingsRepository>();
        var userIds = await settings.ListAutoCheckEligibleUserIdsAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        foreach (var userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await RunUserAsync(userId, cancellationToken);
        }
    }

    private async Task RunUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var mailCheck = scope.ServiceProvider.GetRequiredService<MailCheckService>();
            var request = new MailCheckRunRequest(false, null, false);
            var result = await mailCheck.RunAsync(userId, request, cancellationToken);
            if (result.Busy)
            {
                return;
            }

            var followUps = 0;
            while (result.HasMore && followUps < MailCheckAutoCheck.MaxFollowUpRuns)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                result = await mailCheck.RunAsync(userId, request, cancellationToken);
                if (result.Busy)
                {
                    break;
                }

                followUps++;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "MailCheck server auto-check failed user={UserId}",
                userId);
        }
    }
}
