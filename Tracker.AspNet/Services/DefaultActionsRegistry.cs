using System.Collections.Frozen;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class DefaultActionsRegistry : IActionsRegistry
{
    private readonly FrozenDictionary<string, ActionDescriptor> _store;

    public DefaultActionsRegistry(IEnumerable<ActionDescriptor> descriptors)
    {
        var store = new Dictionary<string, ActionDescriptor>();
        foreach (var descriptor in descriptors)
            store[descriptor.Route] = descriptor;
        _store = store.ToFrozenDictionary();
    }

    public virtual ActionDescriptor? GetActionDescriptor(string route)
    {
        return _store.TryGetValue(route, out var value) ? value : null;
    }
}
