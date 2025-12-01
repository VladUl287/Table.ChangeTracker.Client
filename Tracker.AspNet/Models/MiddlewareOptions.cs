using Microsoft.AspNetCore.Http;

namespace Tracker.AspNet.Models;

public sealed class MiddlewareOptions
{
    public Func<HttpContext, bool> Filter { get; init; } = (_) => true;
    public string[] Tables { get; init; } = [];
}
