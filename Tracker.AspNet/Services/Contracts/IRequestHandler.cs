using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

/// <summary>
/// Determines if the requested data has not been modified, allowing a 304 Not Modified status code to be returned.
/// </summary>
public interface IRequestHandler
{
    /// <summary>
    /// Determines whether the request content has not been modified since the last request.
    /// When content is unchanged: sets HTTP 304 Not Modified status code if ETag is present.
    /// When content has changed: generates ETag, sets it in the response headers, 
    /// and applies Cache-Control configuration from options.
    /// </summary>
    /// <param name="context">The HTTP context containing request and response information.</param>
    /// <param name="options">Global configuration options for the application.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <c>true</c>
    /// if the request content has not been modified and a 304 status was set; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> IsNotModified(HttpContext context, ImmutableGlobalOptions options, CancellationToken token = default);
}
