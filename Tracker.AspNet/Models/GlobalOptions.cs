namespace Tracker.AspNet.Models;

public sealed class GlobalOptions
{
    public bool RouteCaseSensitive { get; init; }
    public TimeSpan CacheLifeTime { get; init; }
}
