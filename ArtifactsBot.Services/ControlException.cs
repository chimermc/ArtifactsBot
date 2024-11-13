namespace ArtifactsBot.Services;

/// <summary>
/// An exception easily identifiable as being thrown by this application for control flow reasons.
/// </summary>
public class ControlException : Exception
{
    public readonly Enums.ControlReason Reason;

    public ControlException(Enums.ControlReason reason, string message) : base(message)
    {
        Reason = reason;
    }

    public override string ToString()
    {
        return $"({Reason}) {Message}";
    }
}
