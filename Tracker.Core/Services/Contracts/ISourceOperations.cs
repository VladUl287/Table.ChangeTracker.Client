using System.Collections.Immutable;

namespace Tracker.Core.Services.Contracts;

public interface ISourceOperations
{
    string SourceId { get; }

    ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default);

    ValueTask GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token = default);

    ValueTask<DateTimeOffset> GetLastTimestamp(CancellationToken token = default);

    ValueTask<bool> EnableTracking(string key, CancellationToken token = default);

    ValueTask<bool> DisableTracking(string key, CancellationToken token = default);

    ValueTask<bool> IsTracking(string key, CancellationToken token = default);

    ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default);
}
