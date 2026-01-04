namespace Tracker.FastEndpoints.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TrackPreProcessorAttribute(
    string[]? tables = null,
    string? providerId = null,
    string? cacheControl = null) : Attribute
{
    public IReadOnlyList<string>? Tables => tables;
    public string? ProviderId => providerId;
    public string? CacheControl => cacheControl;
}
