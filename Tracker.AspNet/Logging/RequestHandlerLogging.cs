using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class RequestHandlerLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler started. TraceId: {TraceId}. Path - {Path}")]
    public static partial void LogRequestHandleStarted(this ILogger logger, string traceId, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler finished - TraceId: {TraceId}")]
    public static partial void LogRequestHandleFinished(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resource not modified. TraceId: '{TraceId}'. ETag: {ETag}")]
    public static partial void LogNotModified(this ILogger logger, string traceId, string etag);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ETag added to response: {ETag}. TraceId: '{TraceId}'")]
    public static partial void LogETagAdded(this ILogger logger, string etag, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Source operations provider resolved for request: TraceId - {TraceId}, SourceId - {SourceId}")]
    public static partial void LogSourceProviderResolved(this ILogger logger, string traceId, string sourceId);
}
