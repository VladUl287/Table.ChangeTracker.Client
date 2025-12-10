using Microsoft.Extensions.Logging;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Logging;

public static partial class TrackContextAttributeLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Options builder for Action '{ActionName}'. Options '{Options}'")]
    public static partial void LogOptionsBuilded(this ILogger logger, string actionName, ImmutableGlobalOptions options);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SourceId generated from TContext for Action '{ActionName}'")]
    public static partial void LogSourceIdGenerateFromTContext(this ILogger logger, string actionName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SourceId taked from global options for Action '{ActionName}'")]
    public static partial void LogSourceIdTakedFromOptions(this ILogger logger, string actionName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Table name {TableName} duplicated for Action '{ActionName}'")]
    public static partial void LogTableNameDuplicated(this ILogger logger, string tableName, string actionName);
}
