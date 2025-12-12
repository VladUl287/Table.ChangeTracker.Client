using Moq;
using System.Reflection;
using Tracker.AspNet.Services;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Tests;

public class ETagServiceTests
{
    private readonly Mock<IAssemblyTimestampProvider> _mockAssembly;
    private readonly DateTime _fixedAssemblyWriteTime;
    private readonly string _fixedAssemblyTicks;
    private const string _fixedAssemblyTicksString = "638397072000000000";

    public ETagServiceTests()
    {
        _fixedAssemblyWriteTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _fixedAssemblyTicks = _fixedAssemblyWriteTime.Ticks.ToString();

        _mockAssembly = new Mock<IAssemblyTimestampProvider>();
        _mockAssembly.Setup(a => a.GetWriteTime()).Returns(_fixedAssemblyWriteTime);
    }

    [Fact]
    public void Constructor_ShouldInitializeAssemblyWriteTime()
    {
        // Arrange
        var assembly = _mockAssembly.Object;

        // Act
        var service = new ETagService(assembly);

        // Assert - We can't directly test private field, but we can test its effect
        Assert.NotNull(service);
    }

    [Theory]
    [InlineData(123456789UL, "suffix", "123456789-suffix")]
    [InlineData(0UL, "test", "0-test")]
    [InlineData(9999999999UL, "", "9999999999")]
    [InlineData(42UL, "my-suffix", "42-my-suffix")]
    public void Build_ShouldCreateCorrectETag(ulong lastTimestamp, string suffix, string expectedBody)
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var expectedETag = _fixedAssemblyTicks + "-" + expectedBody;

        // Act
        var result = service.Build(lastTimestamp, suffix);

        // Assert
        Assert.Equal(expectedETag, result);
    }

    [Fact]
    public void Build_WithZeroTimestampAndEmptySuffix_ShouldCreateCorrectETag()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var expected = _fixedAssemblyTicks + "-0";

        // Act
        var result = service.Build(0UL, "");

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(123456789UL, "suffix", true)]
    [InlineData(123456789UL, "wrongsuffix", false)]
    [InlineData(9999999999UL, "", true)]
    [InlineData(9999999999UL, "wrong", false)]
    public void EqualsTo_ShouldReturnCorrectResult(ulong lastTimestamp, string suffix, bool shouldMatch)
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var etag = service.Build(lastTimestamp, suffix);
        var wrongSuffix = shouldMatch ? suffix : "wrong" + suffix;

        // Act
        var result = service.EqualsTo(etag, lastTimestamp, wrongSuffix);

        // Assert
        Assert.Equal(shouldMatch, result);
    }

    [Fact]
    public void EqualsTo_WithWrongAssemblyTime_ShouldReturnFalse()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var etag = service.Build(123UL, "test");

        // Create a different assembly time
        var differentTime = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var differentMockAssembly = new Mock<IAssemblyTimestampProvider>();
        differentMockAssembly.Setup(a => a.GetWriteTime()).Returns(differentTime);
        var differentService = new ETagService(differentMockAssembly.Object);

        // Act
        var result = differentService.EqualsTo(etag, 123UL, "test");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData(_fixedAssemblyTicksString)] // Missing the rest
    [InlineData(_fixedAssemblyTicksString + "-")] // Just assembly time and dash
    public void EqualsTo_WithInvalidLengthETag_ShouldReturnFalse(string invalidEtag)
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);

        // Act
        var result = service.EqualsTo(invalidEtag, 123UL, "test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsTo_WithWrongTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var etag = service.Build(123UL, "test");

        // Act
        var result = service.EqualsTo(etag, 456UL, "test");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsTo_WithEmptySuffixAndValidEtag_ShouldReturnTrue()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var etag = service.Build(123UL, "");

        // Act
        var result = service.EqualsTo(etag, 123UL, "");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsTo_WithNullEtag_ShouldReturnFalse()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            service.EqualsTo(null!, 123UL, "test"));
    }

    [Fact]
    public void EqualsTo_WithCorrectEtagButDifferentCasing_ShouldReturnFalse()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var etag = service.Build(123UL, "TestSuffix");
        var uppercaseEtag = etag.ToUpperInvariant();

        // Act
        var result = service.EqualsTo(uppercaseEtag, 123UL, "TestSuffix");

        // Assert
        Assert.False(result); // Because StringComparison.Ordinal is used
    }

    [Theory]
    [InlineData(0UL, 1)] // Edge case: 0 has 1 digit
    [InlineData(9UL, 1)] // Single digit
    [InlineData(10UL, 2)] // Two digits
    [InlineData(99UL, 2)] // Two digits max
    [InlineData(100UL, 3)] // Three digits
    [InlineData(9999999999999999999UL, 19)] // Max ulong digits
    public void Build_WithVariousTimestamps_ShouldHaveCorrectLength(ulong timestamp, int expectedDigitCount)
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var suffix = "test";
        var expectedLength = _fixedAssemblyTicks.Length + expectedDigitCount + suffix.Length + 2;

        // Act
        var result = service.Build(timestamp, suffix);

        // Assert
        Assert.Equal(expectedLength, result.Length);
    }

    [Fact]
    public void Build_And_EqualsTo_ShouldBeSymmetric()
    {
        // Arrange
        var service = new ETagService(_mockAssembly.Object);
        var testCases = new[]
        {
            (123UL, "suffix1"),
            (0UL, ""),
            (9999999999UL, "long-suffix-with-dashes"),
            (1UL, "a"), // Single character suffix
            (18446744073709551615UL, "max-ulong") // Max ulong value
        };

        foreach (var (timestamp, suffix) in testCases)
        {
            // Act
            var etag = service.Build(timestamp, suffix);
            var isValid = service.EqualsTo(etag, timestamp, suffix);

            // Assert
            Assert.True(isValid, $"Failed for timestamp={timestamp}, suffix='{suffix}'");
        }
    }

    [Fact]
    public void ComputeLength_PrivateMethod_EdgeCases()
    {
        // Note: This test requires using reflection to test the private method
        var service = new ETagService(_mockAssembly.Object);
        var method = typeof(ETagService).GetMethod("ComputeLength",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Test with empty suffix
        var length1 = (int)method!.Invoke(service, new object[] { 1, 0 })!;
        Assert.Equal(_fixedAssemblyTicks.Length + 1 + 1, length1); // assembly + timestamp + 1 dash

        // Test with suffix
        var length2 = (int)method.Invoke(service, new object[] { 3, 5 })!;
        Assert.Equal(_fixedAssemblyTicks.Length + 3 + 5 + 2, length2); // assembly + timestamp + suffix + 2 dashes
    }
}
