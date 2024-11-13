using ArtifactsBot.Services;

namespace ArtifactsBot.Web;

public class DiscordBackgroundService : BackgroundService
{
    private readonly AppInsightsLogService _logService;
    private readonly DiscordService _discordService;

    public DiscordBackgroundService(AppInsightsLogService logService, DiscordService discordService)
    {
        _logService = logService;
        _discordService = discordService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logService.LogInfo("Service is starting.");

#if !DEBUG
        await Task.Delay(60000, cancellationToken); // Wait some time to ensure the previous instance is finished after deployments or app restarts
#endif

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _discordService.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logService.LogCritical($"Unhandled exception in Discord client loop: {ex}");
            }
        }

        _logService.LogInfo("Service is stopping.");
        await _logService.FlushAsync();
    }
}
