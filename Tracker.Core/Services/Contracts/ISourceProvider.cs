using System.Collections.Immutable;

namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Defines operations for managing source data tracking and versions management.
/// </summary>
public interface ISourceProvider : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for the source.
    /// </summary>
    /// <value>
    /// A string representing the source identifier.
    /// </value>
    string Id { get; }

    /// <summary>
    /// Gets the last version number for a specific key.
    /// </summary>
    /// <param name="key">The key to retrieve the last version for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing the last version number as a 64-bit integer.
    /// </returns>
    ValueTask<long> GetLastVersion(string key, CancellationToken token = default);

    /// <summary>
    /// Retrieves the last versions for multiple keys and populates the provided versions array.
    /// </summary>
    /// <param name="keys">An immutable array of keys to retrieve versions for.</param>
    /// <param name="versions">An array to be populated with the corresponding versiosn for each key. The array length must match the keys array length.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default);

    /// <summary>
    /// Gets the overall last version across all tracked data for the source.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing the last version number as a 64-bit integer.
    /// </returns>
    ValueTask<long> GetLastVersion(CancellationToken token = default);

    /// <summary>
    /// Enables tracking for a specific key.
    /// </summary>
    /// <param name="key">The key to enable tracking for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a boolean value indicating whether tracking was successfully enabled.
    /// </returns>
    ValueTask<bool> EnableTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Disables tracking for a specific key.
    /// </summary>
    /// <param name="key">The key to disable tracking for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a boolean value indicating whether tracking was successfully disabled.
    /// </returns>
    ValueTask<bool> DisableTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Checks whether tracking is enabled for a specific key.
    /// </summary>
    /// <param name="key">The key to check tracking status for.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a boolean value indicating whether tracking is enabled for the key.
    /// </returns>
    ValueTask<bool> IsTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Sets the last version for a specific key.
    /// </summary>
    /// <param name="key">The key to set the version for.</param>
    /// <param name="value">The version value to set.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a boolean value indicating whether the version was successfully set.
    /// </returns>
    ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default);
}
