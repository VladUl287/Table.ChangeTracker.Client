using Microsoft.AspNetCore.Http;
using Npgsql.EFCore.Tracker.AspNet.Models;

namespace Npgsql.EFCore.Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    ValueTask<bool> TrySetETagAsync(HttpContext context, ActionDescriptor descriptor, CancellationToken token = default);
}
