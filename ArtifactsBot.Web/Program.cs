using ArtifactsBot.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;

namespace ArtifactsBot.Web;

public class Program
{
    public static readonly DateTime InitializedAt;

    static Program()
    {
        InitializedAt = DateTime.Now;
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
#if DEBUG
        builder.Configuration.AddUserSecrets<Program>();
#endif

        // Add services to the container.

        builder.Services.AddControllers();

        #region ApplicationInsights

#if DEBUG
        // Disable sending Application Insights telemetry.
        builder.Services.Configure<TelemetryConfiguration>(tc => tc.DisableTelemetry = true);
#endif
        builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions { EnableAdaptiveSampling = false });

        #endregion ApplicationInsights

        builder.Services.AddSingleton(sp => new AppInsightsLogService(sp.GetRequiredService<TelemetryClient>()));
        builder.Services.AddSingleton(sp => new ArtifactsService(sp.GetRequiredService<AppInsightsLogService>()));
        builder.Services.AddSingleton(sp => new DiscordService(sp.GetRequiredService<AppInsightsLogService>(),
            sp.GetRequiredService<ArtifactsService>(),
            builder.Configuration.GetValue<string>(builder.Environment.IsProduction() ? "DiscordToken" : "DiscordTokenDev")!));
        builder.Services.AddHostedService<DiscordBackgroundService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.MapControllers();

        app.Run();
    }
}
