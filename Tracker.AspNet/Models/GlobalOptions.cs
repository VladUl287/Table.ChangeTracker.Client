namespace Tracker.AspNet.Models;

public sealed class GlobalOptions
{
    public bool RouteCaseSensitive { get; init; }
    public TimeSpan XactCacheLifeTime { get; init; }
    public TimeSpan TablesCacheLifeTime { get; init; }
}
