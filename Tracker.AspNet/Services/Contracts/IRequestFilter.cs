using Microsoft.AspNetCore.Http;

namespace Tracker.AspNet.Services.Contracts;

public interface IRequestFilter
{
    bool ShouldProcessRequest(HttpContext context, Func<HttpContext, bool> filter);
}
