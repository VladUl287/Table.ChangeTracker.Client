namespace Tracker.Core.Services.Contracts;

public interface ITimestampsHasher
{
    ulong Hash(ReadOnlySpan<DateTimeOffset> timestamps);
}
