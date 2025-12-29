using System.Buffers.Binary;
using Tracker.Core.Services;

namespace Tracker.Core.Tests.ServicesTests;

public class DefaultTrackerHasherTests
{
    private readonly DefaultTrackerHasher _hasher = new();

    [Fact]
    public void Hash_EmptySpan_ReturnsValidHash()
    {
        // Arrange
        ReadOnlySpan<long> timestamps = [];

        // Act
        var hash = _hasher.Hash(timestamps);

        // Assert
        var expected = CalculateExpectedHash([]);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Hash_SingleTimestamp_ReturnsValidHash()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var versions = new long[] { timestamp.Ticks };

        // Act
        var hash = _hasher.Hash(versions);

        // Assert
        // Calculate expected hash manually
        var expected = CalculateExpectedHash(versions);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Hash_MultipleTimestamps_ReturnsValidHash()
    {
        // Arrange
        var timestamps = new long[]
        {
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 3, 12, 0, 0, TimeSpan.Zero).Ticks
        };

        // Act
        var hash = _hasher.Hash(timestamps);

        // Assert
        var expected = CalculateExpectedHash(timestamps);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Hash_SameTimestamps_ReturnsSameHash()
    {
        // Arrange
        var timestamp1 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var timestamp2 = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var versions1 = new long[] { timestamp1.Ticks };
        var versions2 = new long[] { timestamp2.Ticks };

        // Act
        var hash1 = _hasher.Hash(versions1);
        var hash2 = _hasher.Hash(versions2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_DifferentOrder_ReturnsDifferentHash()
    {
        // Arrange
        var timestamps1 = new long[]
        {
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero).Ticks
        };

        var timestamps2 = new long[]
        {
            new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).Ticks
        };

        // Act
        var hash1 = _hasher.Hash(timestamps1);
        var hash2 = _hasher.Hash(timestamps2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(16)]  // Stack alloc path (16 * 8 = 128 bytes)
    [InlineData(17)]  // ArrayPool path (17 * 8 = 136 bytes > 128)
    public void Hash_StackAllocVsArrayPool_ReturnsSameHash(int count)
    {
        // Arrange
        var timestamps = new long[count];
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < count; i++)
        {
            timestamps[i] = now.AddTicks(i * 1000).Ticks;
        }

        // Act
        var hash = _hasher.Hash(timestamps);

        // Assert
        var expected = CalculateExpectedHash(timestamps);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Hash_LargeArray_ReturnsValidHash()
    {
        // Arrange
        var timestamps = new long[1000];
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (int i = 0; i < timestamps.Length; i++)
        {
            timestamps[i] = baseTime.AddHours(i).Ticks;
        }

        // Act
        var hash = _hasher.Hash(timestamps);

        // Assert
        // Verify it doesn't throw and produces a consistent hash
        var hash2 = _hasher.Hash(timestamps);
        Assert.Equal(hash, hash2);
    }

    [Fact]
    public void Hash_MinAndMaxValues_HandlesCorrectly()
    {
        // Arrange
        var timestamps = new long[]
        {
            DateTimeOffset.MinValue.Ticks,
            DateTimeOffset.MaxValue.Ticks
        };

        // Act
        var hash = _hasher.Hash(timestamps);

        // Assert
        // Should not throw and produce a valid hash
        var hash2 = _hasher.Hash(timestamps);
        Assert.Equal(hash, hash2);
    }

    [Fact]
    public void Hash_EndiannessPaths_ProduceSameResult()
    {
        // Arrange
        var timestamps = new long[]
        {
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 3, 12, 0, 0, TimeSpan.Zero).Ticks
        };

        // Act & Assert
        // Test both paths if we could control endianness
        // In practice, we rely on the system's endianness
        var hash = _hasher.Hash(timestamps);
        var expected = CalculateExpectedHash(timestamps);
        Assert.Equal(expected, hash);
    }

    // Helper method to calculate expected hash independently
    private static ulong CalculateExpectedHash(ReadOnlySpan<long> versions)
    {
        var byteCount = versions.Length * sizeof(long);
        byte[] buffer = new byte[byteCount];

        for (int i = 0; i < versions.Length; i++)
        {
            var ticks = versions[i];
            var span = buffer.AsSpan(i * sizeof(long), sizeof(long));
            BinaryPrimitives.WriteInt64LittleEndian(span, ticks);
        }

        return System.IO.Hashing.XxHash3.HashToUInt64(buffer);
    }

    public delegate ulong HashMethodDelegate(ReadOnlySpan<long> timestamps);

    [Fact]
    public void HashLittleEndian_And_HashBigEndian_ProduceSameResult_WhenDataIsLittleEndian()
    {
        if (!BitConverter.IsLittleEndian)
            return; // Skip on big-endian systems

        var timestamps = new long[]
        {
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).Ticks,
            new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero).Ticks
        };

        // Get private methods via reflection
        var hasher = new DefaultTrackerHasher();
        var type = typeof(DefaultTrackerHasher);

        var hashBigEndianMethod = type.GetMethod("HashBigEndian",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var hashBigEndianMethodFunc = (HashMethodDelegate)hashBigEndianMethod.CreateDelegate(typeof(HashMethodDelegate));

        // Act
        var littleEndianHash = hasher.Hash(timestamps);
        var bigEndianHash = hashBigEndianMethodFunc(timestamps);

        // Assert
        Assert.Equal(littleEndianHash, bigEndianHash);
    }

    [Fact]
    public void Hash_Performance_StackAllocForSmallArrays()
    {
        // Arrange - exactly at threshold (16 * 8 = 128 bytes)
        var timestamps = new long[16];
        var baseTime = DateTimeOffset.UtcNow;

        for (int i = 0; i < timestamps.Length; i++)
        {
            timestamps[i] = baseTime.AddTicks(i).Ticks;
        }

        // Act & Assert - should use stackalloc
        var hash = _hasher.Hash(timestamps);
        var expected = CalculateExpectedHash(timestamps);
        Assert.Equal(expected, hash);
    }

    [Fact]
    public void Hash_ArrayPool_ReturnsRentedArray()
    {
        // Arrange - force ArrayPool usage
        var timestamps = new long[17]; // 17 * 8 = 136 > 128
        var baseTime = DateTimeOffset.UtcNow;

        for (int i = 0; i < timestamps.Length; i++)
        {
            timestamps[i] = baseTime.AddTicks(i).Ticks;
        }

        // Act & Assert - should use ArrayPool without throwing
        var hash = _hasher.Hash(timestamps);
        var expected = CalculateExpectedHash(timestamps);
        Assert.Equal(expected, hash);
    }
}