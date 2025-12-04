namespace Tracker.AspNet.Services.Contracts;

public interface ISourceOperationsResolver
{
    ISourceOperations Resolve(string? sourceId);
}
