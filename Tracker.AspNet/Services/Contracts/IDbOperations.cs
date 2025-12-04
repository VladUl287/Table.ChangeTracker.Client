namespace Tracker.AspNet.Services.Contracts;

public interface ISourceOperations
{
    string SourceId { get; }
    Task<DateTimeOffset?> GetLastTimestamp(string key, CancellationToken token);
    Task<IEnumerable<DateTimeOffset>> GetLastTimestamp(string[] keys, CancellationToken token);
    Task<DateTimeOffset?> GetLastTimestamp(CancellationToken token);
}

public interface IDbOperations
{
    //string ConnectionId { get; init; }
    string Provider { get; init; }
    Task<DateTimeOffset?> GetLastTimestamp(string table, CancellationToken token);
    Task<uint?> GetLastCommittedXact(CancellationToken token);
}

public interface IDbOperations<TContext> : IDbOperations
{
    //Type Context { get; init; }
}
