using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class DefaultTrackerHasher : ITrackerHasher
{
    private const int StackAllocThreshold = 64;

    public ulong Hash(ReadOnlySpan<long> versions)
    {
        if (BitConverter.IsLittleEndian)
            return XxHash3.HashToUInt64(MemoryMarshal.AsBytes(versions));

        return HashBigEndian(versions);
    }

    private static ulong HashBigEndian(ReadOnlySpan<long> versions)
    {
        var byteCount = versions.Length * sizeof(long);
        if (byteCount >= StackAllocThreshold)
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            Span<byte> ticks = rented.AsSpan(0, versions.Length);

            for (int i = 0; i < versions.Length; i++)
                BinaryPrimitives.WriteInt64LittleEndian(
                    ticks.Slice(i * sizeof(long), sizeof(long)), versions[i]);

            ArrayPool<byte>.Shared.Return(rented);
            return XxHash3.HashToUInt64(ticks);
        }

        Span<byte> buffer = stackalloc byte[byteCount];
        for (int i = 0; i < versions.Length; i++)
            BinaryPrimitives.WriteInt64LittleEndian(
                buffer.Slice(i * sizeof(long), sizeof(long)), versions[i]);
        return XxHash3.HashToUInt64(buffer);
    }
}
