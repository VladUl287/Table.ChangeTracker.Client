namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute(
      string? route = null,
      string[]? tables = null,
      Type[]? entities = null) : Attribute
{
    public string? Route { get; } = route;
    public string[]? Tables { get; } = tables;
    public Type[]? Entities { get; } = entities;
}
