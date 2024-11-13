namespace ArtifactsBot.Services.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Embed field values cannot be truly empty. If this value would be empty, replace it with something invisible.
    /// </summary>
    public static string ToInvisibleEmbedIfEmpty(this string value) => string.IsNullOrWhiteSpace(value) ? Constants.EmbedEmptyItem : value;

    /// <summary>
    /// Make lowercase and replace spaces with underscores.
    /// </summary>
    public static string ToCodeFormat(this string value) => value.ToLowerInvariant().Replace(' ', '_').Replace("&", "and");
}
