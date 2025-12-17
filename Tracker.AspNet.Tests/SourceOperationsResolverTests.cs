using NSubstitute;
using Tracker.AspNet.Services;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Tests;

public class SourceOperationsResolverTests
{
    public interface ITestSourceOperations : ISourceOperations
    {
        // Marker interface for testing
    }

    [Fact]
    public void Constructor_WithSourceOperations_InitializesStore()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("source1");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("source2");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        // Act
        var resolver = new SourceOperationsResolver(sourceOperations);

        // Assert
        Assert.True(resolver.Registered("source1"));
        Assert.True(resolver.Registered("source2"));
    }

    [Fact]
    public void First_Property_ReturnsFirstSourceOperation()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("source1");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("source2");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.First;

        // Assert
        Assert.Same(mockSource1, result);
    }

    [Fact]
    public void First_Property_WithSingleSource_ReturnsThatSource()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("single-source");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.First;

        // Assert
        Assert.Same(mockSource, result);
    }

    [Fact]
    public void Registered_WithFirstSourceId_ReturnsTrue()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("first");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("second");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.Registered("first");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Registered_WithOtherSourceId_ReturnsTrue()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("first");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("second");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.Registered("second");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Registered_WithUnknownSourceId_ReturnsFalse()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("first");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("second");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.Registered("unknown");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryResolve_WithValidSourceId_ReturnsSourceOperations()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("source1");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("source2");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        resolver.TryResolve("source2", out var result);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockSource2, result);
    }

    [Fact]
    public void TryResolve_WithFirstSourceId_ReturnsFirstSource()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("first");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("second");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        resolver.TryResolve("first", out var result);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockSource1, result);
    }

    [Fact]
    public void TryResolve_WithNullSourceId_ReturnsNull()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("source1");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        resolver.TryResolve(null, out var result);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_WithEmptySourceId_ReturnsNull()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("source1");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        resolver.TryResolve("", out var result);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_WithUnknownSourceId_ReturnsNull()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("source1");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        resolver.TryResolve("unknown", out var result);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_ThrowsException()
    {
        // Arrange
        var emptyCollection = new List<ISourceOperations>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SourceOperationsResolver(emptyCollection));
    }

    [Fact]
    public void Constructor_WithDuplicateSourceIds_ThrowsException()
    {
        // Arrange
        var mockSource1 = Substitute.For<ISourceOperations>();
        mockSource1.SourceId.Returns("duplicate");

        var mockSource2 = Substitute.For<ISourceOperations>();
        mockSource2.SourceId.Returns("duplicate");

        var sourceOperations = new List<ISourceOperations> { mockSource1, mockSource2 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new SourceOperationsResolver(sourceOperations));
        Assert.Contains("duplicate", exception.Message);
    }

    [Fact]
    public void Registered_WithNullSourceId_ReturnsFalse()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("source1");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = () => { resolver.Registered(null); };

        // Assert
        Assert.Throws<ArgumentNullException>(result);
    }

    [Fact]
    public void Registered_WithEmptySourceId_ReturnsFalse()
    {
        // Arrange
        var mockSource = Substitute.For<ISourceOperations>();
        mockSource.SourceId.Returns("source1");

        var sourceOperations = new List<ISourceOperations> { mockSource };

        var resolver = new SourceOperationsResolver(sourceOperations);

        // Act
        var result = resolver.Registered("");

        // Assert
        Assert.False(result);
    }
}
