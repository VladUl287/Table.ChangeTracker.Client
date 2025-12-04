using System.Data.Common;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class SqlServerOperations(DbDataSource dataSource) : ISourceOperations
{
    public string SourceId => string.Empty;

    public async Task<DateTimeOffset?> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        return null;
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
