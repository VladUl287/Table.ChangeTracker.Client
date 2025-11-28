using Npgsql.EFCore.Tracker.AspNet.Models;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using System.Collections.Frozen;

namespace Npgsql.EFCore.Tracker.AspNet.Services;

public class DefaultActionsRegistry : IActionsRegistry
{
    private readonly FrozenDictionary<string, ActionDescriptor> _store;

    public DefaultActionsRegistry(params ActionDescriptor[] descriptors)
    {
        var store = new Dictionary<string, ActionDescriptor>(descriptors.Length);
        foreach (var descriptor in descriptors)
            store[descriptor.Route] = descriptor;
        _store = store.ToFrozenDictionary();
    }

    public ActionDescriptor GetActionDescriptor(string route)
    {
        return _store.GetValueOrDefault(route, null);
    }
}
