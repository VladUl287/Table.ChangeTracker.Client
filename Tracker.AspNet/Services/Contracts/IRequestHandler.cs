using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IRequestHandler
{
    Task<bool> IsNotModified(HttpContext context, ImmutableGlobalOptions options, CancellationToken token);
}
