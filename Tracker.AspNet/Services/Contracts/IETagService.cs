using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    Task<bool> TrySetETagAsync(HttpContext context, GlobalOptions options, CancellationToken token = default);
}
