using Tracker.AspNet.Models;
using Microsoft.AspNetCore.Http;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

public interface IProviderResolver
{
    ISourceProvider ResolveProvider(HttpContext ctx, ImmutableGlobalOptions options, out bool shouldDispose);
}
