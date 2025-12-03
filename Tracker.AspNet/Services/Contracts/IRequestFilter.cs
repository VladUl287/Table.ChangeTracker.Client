using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IRequestFilter
{
    bool ShouldProcessRequest(HttpContext context, ImmutableGlobalOptions options);
    bool ShouldProcessRequest<TState>(HttpContext context, Func<TState, ImmutableGlobalOptions> options, TState state);
}
