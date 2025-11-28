using Npgsql.EFCore.Tracker.AspNet.Models;

namespace Npgsql.EFCore.Tracker.AspNet.Services.Contracts;

public interface IActionsRegistry
{
    ActionDescriptor GetActionDescriptor(string route);
}
