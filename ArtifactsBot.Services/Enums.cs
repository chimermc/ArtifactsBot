namespace ArtifactsBot.Services;

public static class Enums
{
    public enum ControlReason
    {
        Unspecified = 0,
        OutOfRetries,
        InvalidResource,
        CommandResponse
    }
}
