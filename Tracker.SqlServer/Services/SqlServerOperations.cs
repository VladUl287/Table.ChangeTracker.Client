using System.Data.Common;
using Tracker.Core.Services.Contracts;

namespace Tracker.SqlServer.Services;

public sealed class SqlServerOperations(string sourceId, DbDataSource dataSource) : ISourceOperations
{
    public string SourceId => sourceId;

    public async Task<DateTimeOffset?> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        return DateTimeOffset.Parse("12.12.2001");
    }

    public Task<IEnumerable<DateTimeOffset>> GetLastTimestamp(string[] keys, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<DateTimeOffset?> GetLastTimestamp(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
