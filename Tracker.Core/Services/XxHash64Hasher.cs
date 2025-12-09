using System.Buffers;
using System.IO.Hashing;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class XxHash64Hasher : ITimestampsHasher
{
    public ulong Hash(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        const int BufferSizeThreshold = 128;

        var bufferSize = timestamps.Length * sizeof(long);
        if (bufferSize >= BufferSizeThreshold)
        {
            var arr = ArrayPool<byte>.Shared.Rent(bufferSize);

            Span<byte> bufferArr = arr.AsSpan();

            for (int i = 0; i < timestamps.Length; i++)
                BitConverter.TryWriteBytes(bufferArr.Slice(i * sizeof(long), sizeof(long)), timestamps[i].Ticks);

            ArrayPool<byte>.Shared.Return(arr);

            return XxHash64.HashToUInt64(bufferArr);
        }

        Span<byte> buffer = stackalloc byte[bufferSize];
        for (int i = 0; i < timestamps.Length; i++)
            BitConverter.TryWriteBytes(buffer.Slice(i * sizeof(long), sizeof(long)), timestamps[i].Ticks);
        return XxHash64.HashToUInt64(buffer);
    }
}
