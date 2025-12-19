using Moq;
using Npgsql;
using System.Data.Common;
using Tracker.Npgsql.Extensions;

namespace Tracker.Npgsql.Tests;

public class ReaderExtensionsTests
{
    private const long PostgresTimestampOffsetTicks = 630822816000000000L;

    [Theory]
    [InlineData(0, PostgresTimestampOffsetTicks)] // Postgres epoch should map to .NET DateTime.MinValue
    [InlineData(1, PostgresTimestampOffsetTicks + 10)] // 1 microsecond = 10 ticks
    [InlineData(1000000, PostgresTimestampOffsetTicks + 10000000)] // 1 second = 10,000,000 ticks
    [InlineData(-1, PostgresTimestampOffsetTicks - 10)] // Negative microseconds
    [InlineData(631152000000000L, 630822816000000000L + 6311520000000000L)] // Year 2024 example
    public void GetTimestampTicks_ValidValues_ReturnsCorrectTicks(
        long postgresMicroseconds, long expectedTicks)
    {
        // Arrange
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(It.IsAny<int>())).Returns(postgresMicroseconds);

        var reader = mockReader.Object;
        const int testOrdinal = 0;

        // Act
        var result = reader.GetTimestampTicks(testOrdinal);

        // Assert
        Assert.Equal(expectedTicks, result);
        mockReader.Verify(r => r.GetInt64(testOrdinal), Times.Once);
    }

    [Fact]
    public void GetTimestampTicks_MaxValue_ReturnsCorrectTicks()
    {
        // Arrange
        long postgresMaxValue = 9223372036854775807L; // long.MaxValue
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(0)).Returns(postgresMaxValue);

        var reader = mockReader.Object;

        // Act
        var result = reader.GetTimestampTicks(0);

        // Assert
        long expected = PostgresTimestampOffsetTicks + (postgresMaxValue * 10);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetTimestampTicks_MinValue_ReturnsCorrectTicks()
    {
        // Arrange
        long postgresMinValue = -9223372036854775808L; // long.MinValue
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(0)).Returns(postgresMinValue);

        var reader = mockReader.Object;

        // Act
        var result = reader.GetTimestampTicks(0);

        // Assert
        long expected = PostgresTimestampOffsetTicks + (postgresMinValue * 10);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999)]
    public void GetTimestampTicks_DifferentOrdinals_UsesCorrectOrdinal(int ordinal)
    {
        // Arrange
        long postgresValue = 123456789L;
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(ordinal)).Returns(postgresValue);

        var reader = mockReader.Object;

        // Act
        var result = reader.GetTimestampTicks(ordinal);

        // Assert
        long expected = PostgresTimestampOffsetTicks + (postgresValue * 10);
        Assert.Equal(expected, result);
        mockReader.Verify(r => r.GetInt64(ordinal), Times.Once);
    }

    [Fact]
    public void GetTimestampTicks_EdgeCase_ZeroMicrosecondsAfterEpoch()
    {
        // PostgreSQL timestamp '2000-01-01' should be microseconds = 0
        // This should map to .NET DateTime with ticks = PostgresTimestampOffsetTicks

        // Arrange
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(0)).Returns(0L);

        var reader = mockReader.Object;

        // Act
        var result = reader.GetTimestampTicks(0);

        // Assert
        Assert.Equal(PostgresTimestampOffsetTicks, result);
    }

    [Fact]
    public void GetTimestampTicks_VerifyDateTimeConversion()
    {
        // Test that the ticks produce a valid DateTime
        // PostgreSQL: 2000-01-01 00:00:00 should map to .NET: 2000-01-01 00:00:00

        // Arrange
        long postgresValue = 0; // 2000-01-01 00:00:00
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(0)).Returns(postgresValue);

        var reader = mockReader.Object;

        // Act
        var ticks = reader.GetTimestampTicks(0);
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);

        // Assert
        Assert.Equal(2000, dateTime.Year);
        Assert.Equal(1, dateTime.Month);
        Assert.Equal(1, dateTime.Day);
        Assert.Equal(0, dateTime.Hour);
        Assert.Equal(0, dateTime.Minute);
        Assert.Equal(0, dateTime.Second);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Theory]
    [InlineData(946684800000000L)] // 2000-01-01 00:00:00 to 2000-01-02 00:00:00
    [InlineData(-946684800000000L)] // 2000-01-01 00:00:00 to 1999-12-31 00:00:00
    [InlineData(1577836800000000L)] // 2000-01-01 to 2020-01-01
    public void GetTimestampTicks_RoundTripToDateTime_ShouldBeValid(long postgresMicroseconds)
    {
        // Arrange
        var mockReader = new Mock<DbDataReader>();
        mockReader.Setup(r => r.GetInt64(0)).Returns(postgresMicroseconds);

        var reader = mockReader.Object;

        // Act
        var ticks = reader.GetTimestampTicks(0);
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);

        // Assert - DateTime should be valid
        Assert.NotEqual(DateTime.MinValue, dateTime);
        Assert.NotEqual(DateTime.MaxValue, dateTime);

        // Additional check: convert back to verify consistency
        // The conversion isn't perfectly invertible due to tick/microsecond rounding,
        // but should be within 1 microsecond
        long convertedBackMicroseconds = (ticks - PostgresTimestampOffsetTicks) / 10;
        Assert.True(Math.Abs(postgresMicroseconds - convertedBackMicroseconds) <= 1);
    }
}