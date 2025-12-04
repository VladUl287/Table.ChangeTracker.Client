using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    Task<bool> TrySetETagAsync(HttpContext context, ImmutableGlobalOptions options, CancellationToken token);

    Task<bool> TrySetETagAsync<TContext>(HttpContext context, ImmutableGlobalOptions options, CancellationToken token)
        where TContext : DbContext;
}
