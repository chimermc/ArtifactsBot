using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ArtifactsBot.Services;

public class AppInsightsLogService
{
    private readonly TelemetryClient _telemetryClient;

    public AppInsightsLogService(TelemetryClient telemetryClientClient)
    {
        _telemetryClient = telemetryClientClient;
    }

    public void LogInfo(string message, [CallerMemberName] string actionName = "", Dictionary<string, string>? properties = null)
    {
        Log(message, actionName, properties, SeverityLevel.Information);
    }

    public void LogWarning(string message, [CallerMemberName] string actionName = "", Dictionary<string, string>? properties = null)
    {
        Log(message, actionName, properties, SeverityLevel.Warning);
    }

    public void LogError(string message, [CallerMemberName] string actionName = "", Dictionary<string, string>? properties = null)
    {
        Log(message, actionName, properties, SeverityLevel.Error);
    }

    public void LogCritical(string message, [CallerMemberName] string actionName = "", Dictionary<string, string>? properties = null)
    {
        Log(message, actionName, properties, SeverityLevel.Critical);
    }

    private void Log(string message, string actionName, Dictionary<string, string>? properties, SeverityLevel severity)
    {
        if (properties == null)
        {
            properties = new Dictionary<string, string> { { "ActionName", actionName } };
        }
        else
        {
            properties["ActionName"] = actionName;
        }

        _telemetryClient.TrackTrace(message, severity, properties);
        Debug.WriteLine($"[{severity}] {actionName}: {message}");
    }

    public async Task FlushAsync()
    {
        _telemetryClient.Flush();
        await Task.Delay(5000);
    }
}
