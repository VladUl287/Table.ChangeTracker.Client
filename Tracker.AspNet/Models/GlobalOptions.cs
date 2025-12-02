using Microsoft.AspNetCore.Http;

namespace Tracker.AspNet.Models;

public sealed class GlobalOptions
{
    public Func<HttpContext, bool> Filter { get; set; } = (_) => true;

    public string[] Tables { get; set; } = [];
    public Type[] Entities { get; set; } = [];

    public TimeSpan XactCacheLifeTime { get; set; }
    public TimeSpan TablesCacheLifeTime { get; set; }

    public Func<HttpContext, string> Suffix { get; set; } = (_) => string.Empty;

    public GlobalOptions Copy()
    {
        return new GlobalOptions
        {
            Filter = Filter,
            Tables = Tables?.ToArray() ?? [],
            Entities = Entities?.ToArray() ?? [],
            XactCacheLifeTime = XactCacheLifeTime,
            TablesCacheLifeTime = TablesCacheLifeTime,
            Suffix = Suffix
        };
    }
}
