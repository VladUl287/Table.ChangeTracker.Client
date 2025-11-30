namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute(
      string? route = null,
      string[]? tables = null,
      Type[]? entities = null,
      TimeSpan? cacheLifeTime = default) : Attribute
{
    public string? Route { get; } = route;
    public string[]? Tables { get; } = tables;
    public Type[]? Entities { get; } = entities;
    public TimeSpan? CacheLifeTime { get; } = cacheLifeTime;

    internal bool IsGlobal => Tables is null or { Length: 0 } && Entities is null or { Length: 0 };
}
