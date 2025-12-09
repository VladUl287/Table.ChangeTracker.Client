using BenchmarkDotNet.Attributes;
using Tracker.Core.Services;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
public class HashersBenchamrk
{
    private static readonly DateTimeOffset[] _dateTimestamps = [.. Enumerable.Range(0, 5).Select(i => DateTimeOffset.UtcNow.AddDays(i))];

    public static MurmurHash3FinilizerHasher MurmurHash3Finilizer = new();
    public static Fnv1aTimstampsHasher Fnv1ATimstamps = new();
    public static XxHash64Hasher XxHash64Hasher = new();

    [Benchmark]
    public ulong Murmur_Hasher()
    {
        return MurmurHash3Finilizer.Hash(_dateTimestamps);
    }

    [Benchmark]
    public ulong Fnv1a_Hasher()
    {
        return Fnv1ATimstamps.Hash(_dateTimestamps);
    }

    [Benchmark]
    public ulong XxHash64_Hasher()
    {
        return XxHash64Hasher.Hash(_dateTimestamps);
    }
}
