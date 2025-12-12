using Tracker.Core.Extensions;

namespace Tracker.Core.Tests;

public class UlongExtensionsTests
{
    [Theory]
    [InlineData(0UL, 1)] // Edge case: 0 has 1 digit
    [InlineData(9UL, 1)] // Single digit
    [InlineData(10UL, 2)] // Two digits
    [InlineData(99UL, 2)] // Two digits max
    [InlineData(100UL, 3)] // Three digits
    [InlineData(9999999999999999999UL, 19)] // Max ulong digits
    [InlineData(10000000000000000000UL, 20)] // 20 digits
    [InlineData(999999999999999999UL, 18)] // 18 digits
    [InlineData(ulong.MaxValue, 20)] // Max ulong digits
    public void Count_Digits(ulong a, int req_count)
    {
        // Act
        var count = a.CountDigits();

        // Assert
        Assert.Equal(count, req_count);
    }
}
