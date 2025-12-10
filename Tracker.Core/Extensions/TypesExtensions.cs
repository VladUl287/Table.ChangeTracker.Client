using System.Text;
using System.Buffers;
using System.IO.Hashing;

namespace Tracker.Core.Extensions;

public static class TypesExtensions
{
    public static string GetTypeHashId(this Type type)
    {
        var typeName = type.FullName ?? throw new NullReferenceException();

        var maximumBytes = Encoding.UTF8.GetMaxByteCount(typeName.Length);

        const int MaxBytesThreshold = 256;
        if (maximumBytes > MaxBytesThreshold)
        {
            byte[] data = ArrayPool<byte>.Shared.Rent(maximumBytes);

            var count = Encoding.UTF8.GetBytes(typeName, data);
            var bytes = data.AsSpan()[..count];

            var hash = XxHash64.HashToUInt64(bytes);

            ArrayPool<byte>.Shared.Return(data);

            return hash.ToString();
        }
        else
        {
            Span<byte> data = stackalloc byte[maximumBytes];

            var count = Encoding.UTF8.GetBytes(typeName, data);
            data = data[..count];

            return XxHash64.HashToUInt64(data).ToString();
        }
    }
}
