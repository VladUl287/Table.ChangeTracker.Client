using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IActionsRegistry
{
    ActionDescriptor? GetActionDescriptor(string route);
}
