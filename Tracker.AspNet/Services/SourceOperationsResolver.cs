using System.Collections.Frozen;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class SourceOperationsResolver(
    IEnumerable<ISourceOperations> sourceOperations) : ISourceOperationsResolver
{
    private readonly FrozenDictionary<string, ISourceOperations> _store
        = sourceOperations.ToFrozenDictionary(c => c.SourceId);

    public ISourceOperations Resolve(string? sourceId)
    {
        if (sourceId is not null && _store.TryGetValue(sourceId, out var value))
            return value;

        return _store.Values[0];
    }
}
