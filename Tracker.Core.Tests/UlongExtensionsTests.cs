using Tracker.Core.Extensions;

namespace Tracker.Core.Tests;

public class UlongExtensionsTests
{
    [Fact]
    public void Count_Default()
    {
        // Arrange
        ulong a = 123;
        int req_count = 3;

        // Act
        var count = a.CountDigits();

        // Assert
        Assert.Equal(count, req_count);
    }

    [Fact]
    public void Count_Zero()
    {
        // Arrange
        ulong a = 0;
        int req_count = 1;

        // Act
        var count = a.CountDigits();

        // Assert
        Assert.Equal(count, req_count);
    }

    [Fact]
    public void Count_Max()
    {
        // Arrange
        ulong a = ulong.MaxValue;
        int req_count = 20;

        // Act
        var count = a.CountDigits();

        // Assert
        Assert.Equal(count, req_count);
    }
}
