using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class MurmurHash3FinilizerHasher : ITimestampsHasher
{
    public ulong Hash(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        const ulong C1 = 0x87c37b91114253d5UL;
        const ulong C2 = 0x4cf5ad432745937fUL;

        ulong h1 = 0UL;

        for (int i = 0; i < timestamps.Length; i++)
        {
            ulong k1 = (ulong)timestamps[i].Ticks;

            k1 *= C1;
            k1 = (k1 << 31) | (k1 >> 33);
            k1 *= C2;
            h1 ^= k1;

            h1 = (h1 << 27) | (h1 >> 37);
            h1 = h1 * 5 + 0x52dce729;
        }

        h1 ^= (ulong)timestamps.Length;
        h1 ^= h1 >> 33;
        h1 *= 0xff51afd7ed558ccdUL;
        h1 ^= h1 >> 33;
        h1 *= 0xc4ceb9fe1a85ec53UL;
        h1 ^= h1 >> 33;

        return h1;
    }
}
