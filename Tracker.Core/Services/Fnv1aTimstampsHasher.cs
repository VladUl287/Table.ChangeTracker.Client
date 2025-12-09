using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class Fnv1aTimstampsHasher : ITimestampsHasher
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public ulong Hash(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        ulong hash = FnvOffsetBasis;

        foreach (var timestamp in timestamps)
        {
            long ticks = timestamp.Ticks;

            for (int i = 0; i < 8; i++)
            {
                byte b = (byte)(ticks >> (i * 8));
                hash ^= b;
                hash *= FnvPrime;
            }
        }

        return hash;
    }
}
