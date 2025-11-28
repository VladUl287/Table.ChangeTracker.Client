using Microsoft.AspNetCore.Http;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using Npgsql.EFCore.Tracker.AspNet.Utils;

namespace Npgsql.EFCore.Tracker.AspNet.Services;

public class DefaultPathResolver : IPathResolver
{
    public virtual string ResolvePath(HttpContext context)
    {
        return context.Request.GetEncodedPath();
    }
}
