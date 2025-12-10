using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class GlobalOptionsBuilderLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Table name {TableName} duplicated in global options")]
    public static partial void LogOptionsTableNameDuplicated(this ILogger logger, string tableName);
}
