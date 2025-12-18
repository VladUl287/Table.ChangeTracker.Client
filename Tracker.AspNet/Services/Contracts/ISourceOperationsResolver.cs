using System.Diagnostics.CodeAnalysis;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

/// <summary>
/// Resolves and provides access to source-specific operations based on source identifiers.
/// </summary>
/// <remarks>
/// This interface manages a collection of <see cref="ISourceOperations"/> instances,
/// allowing lookup by source ID and providing a default/first source when needed.
/// </remarks>
public interface ISourceOperationsResolver
{
    /// <summary>
    /// Gets the default or first registered source operations instance.
    /// </summary>
    /// <value>The first <see cref="ISourceOperations"/> in the collection.</value>
    /// <remarks>
    /// This property provides a convenient way to access a source operations instance
    /// when the specific source doesn't matter or when a fallback is needed.
    /// </remarks>
    ISourceOperations First { get; }

    /// <summary>
    /// Determines whether a source with the specified identifier is registered.
    /// </summary>
    /// <param name="sourceId">The identifier of the source to check.</param>
    /// <returns><see langword="true"/> if a source with the given ID is registered; otherwise, <see langword="false"/>.</returns>
    bool Registered(string sourceId);

    /// <summary>
    /// Attempts to resolve source operations for the specified source identifier.
    /// </summary>
    /// <param name="sourceId">The identifier of the source to resolve.</param>
    /// <param name="sourceOperations">When this method returns <see langword="true"/>, contains the resolved 
    /// <see cref="ISourceOperations"/> instance; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if source operations were successfully resolved; otherwise, <see langword="false"/>.</returns>
    bool TryResolve(string sourceId, [NotNullWhen(true)] out ISourceOperations? sourceOperations);
}