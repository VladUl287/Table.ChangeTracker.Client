using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class ProviderResolverLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Table name {TableName} duplicated in global options")]
    public static partial void LogSourceIdResolvedFromStore(this ILogger logger, string tableName);
}
