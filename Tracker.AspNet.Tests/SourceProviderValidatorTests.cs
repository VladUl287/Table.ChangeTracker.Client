//using Microsoft.AspNetCore.Builder;
//using NSubstitute;
//using Tracker.AspNet.Services;
//using Tracker.Core.Services.Contracts;

//namespace Tracker.AspNet.Tests;

//public class SourceProviderValidatorTests
//{
//    public interface ITestSourceOperations : ISourceProvider
//    {
//        // Marker interface for testing
//    }

//    [Fact]
//    public void Configure_WithValidOperations_ReturnsNextDelegate()
//    {
//        // Arrange
//        var mockSource1 = Substitute.For<ISourceProvider>();
//        mockSource1.Id.Returns("source1");

//        var mockSource2 = Substitute.For<ISourceProvider>();
//        mockSource2.Id.Returns("source2");

//        var operations = new List<ISourceProvider> { mockSource1, mockSource2 };

//        var validator = new DefaultProvidersValidator(operations);
//        var mockAppBuilder = Substitute.For<IApplicationBuilder>();

//        Action<IApplicationBuilder> next = builder =>
//        {
//            // Simulate next action
//        };

//        // Act
//        var result = validator.Configure(next);

//        // Assert
//        Assert.NotNull(result);
//        Assert.IsType<Action<IApplicationBuilder>>(result);

//        // Verify no exception was thrown
//        result.Invoke(mockAppBuilder);
//        Assert.True(true); // Just to show the test passed without exception
//    }

//    [Fact]
//    public void Configure_WithEmptyOperations_ThrowsInvalidOperationException()
//    {
//        // Arrange
//        var emptyOperations = new List<ISourceProvider>();
//        var validator = new DefaultProvidersValidator(emptyOperations);

//        Action<IApplicationBuilder> next = builder => { };

//        // Act & Assert
//        var exception = Assert.Throws<InvalidOperationException>(() => validator.Configure(next));

//        Assert.Contains($"At least one {nameof(ISourceProvider)} implementation is required", exception.Message);
//    }

//    [Fact]
//    public void Configure_WithSingleOperation_DoesNotThrow()
//    {
//        // Arrange
//        var mockSource = Substitute.For<ISourceProvider>();
//        mockSource.Id.Returns("single-source");

//        var operations = new List<ISourceProvider> { mockSource };
//        var validator = new DefaultProvidersValidator(operations);

//        Action<IApplicationBuilder> next = builder => { };
//        var mockAppBuilder = Substitute.For<IApplicationBuilder>();

//        // Act
//        var result = validator.Configure(next);

//        // Assert - Should not throw
//        result.Invoke(mockAppBuilder);
//        Assert.True(true); // Just to show the test passed without exception
//    }

//    [Fact]
//    public void Configure_WithDuplicateSourceIds_ThrowsInvalidOperationException()
//    {
//        // Arrange
//        var mockSource1 = Substitute.For<ISourceProvider>();
//        mockSource1.Id.Returns("duplicate-id");

//        var mockSource2 = Substitute.For<ISourceProvider>();
//        mockSource2.Id.Returns("duplicate-id");

//        var mockSource3 = Substitute.For<ISourceProvider>();
//        mockSource3.Id.Returns("unique-id");

//        var operations = new List<ISourceProvider> { mockSource1, mockSource2, mockSource3 };
//        var validator = new DefaultProvidersValidator(operations);

//        Action<IApplicationBuilder> next = builder => { };

//        // Act & Assert
//        var exception = Assert.Throws<InvalidOperationException>(() => validator.Configure(next));

//        Assert.Contains($"Duplicate {nameof(ISourceProvider.Id)} values found", exception.Message);
//        Assert.Contains("duplicate-id", exception.Message);
//    }

//    [Fact]
//    public void Configure_WithMultipleDuplicateSourceIds_IncludesAllInExceptionMessage()
//    {
//        // Arrange
//        var mockSource1 = Substitute.For<ISourceProvider>();
//        mockSource1.Id.Returns("duplicate1");

//        var mockSource2 = Substitute.For<ISourceProvider>();
//        mockSource2.Id.Returns("duplicate1");

//        var mockSource3 = Substitute.For<ISourceProvider>();
//        mockSource3.Id.Returns("duplicate2");

//        var mockSource4 = Substitute.For<ISourceProvider>();
//        mockSource4.Id.Returns("duplicate2");

//        var mockSource5 = Substitute.For<ISourceProvider>();
//        mockSource5.Id.Returns("unique");

//        var operations = new List<ISourceProvider> { mockSource1, mockSource2, mockSource3, mockSource4, mockSource5 };
//        var validator = new DefaultProvidersValidator(operations);

//        Action<IApplicationBuilder> next = builder => { };

//        // Act & Assert
//        var exception = Assert.Throws<InvalidOperationException>(() => validator.Configure(next));

//        Assert.Contains($"Duplicate {nameof(ISourceProvider.Id)} values found", exception.Message);
//        Assert.Contains("duplicate1", exception.Message);
//        Assert.Contains("duplicate2", exception.Message);
//    }

//    [Fact]
//    public void Configure_WithValidOperations_CallsNextDelegateWithBuilder()
//    {
//        // Arrange
//        var mockSource = Substitute.For<ISourceProvider>();
//        mockSource.Id.Returns("source1");

//        var operations = new List<ISourceProvider> { mockSource };
//        var validator = new DefaultProvidersValidator(operations);

//        var mockAppBuilder = Substitute.For<IApplicationBuilder>();
//        var nextWasCalled = false;
//        IApplicationBuilder passedBuilder = null;

//        Action<IApplicationBuilder> next = builder =>
//        {
//            nextWasCalled = true;
//            passedBuilder = builder;
//        };

//        // Act
//        var result = validator.Configure(next);
//        result.Invoke(mockAppBuilder);

//        // Assert
//        Assert.True(nextWasCalled);
//        Assert.Same(mockAppBuilder, passedBuilder);
//    }

//    [Fact]
//    public void Configure_ReturnsSameDelegate_WhenNoExceptions()
//    {
//        // Arrange
//        var mockSource = Substitute.For<ISourceProvider>();
//        mockSource.Id.Returns("source1");

//        var operations = new List<ISourceProvider> { mockSource };
//        var validator = new DefaultProvidersValidator(operations);

//        var mockAppBuilder = Substitute.For<IApplicationBuilder>();
//        Action<IApplicationBuilder> originalNext = builder => { };

//        // Act
//        var returnedDelegate = validator.Configure(originalNext);

//        // Assert
//        Assert.Same(originalNext, returnedDelegate);
//    }

//    [Fact]
//    public void Configure_WithEmptySourceId_DoesNotThrow()
//    {
//        // Arrange
//        var mockSource1 = Substitute.For<ISourceProvider>();
//        mockSource1.Id.Returns("");

//        var mockSource2 = Substitute.For<ISourceProvider>();
//        mockSource2.Id.Returns("source2");

//        var operations = new List<ISourceProvider> { mockSource1, mockSource2 };
//        var validator = new DefaultProvidersValidator(operations);

//        Action<IApplicationBuilder> next = builder => { };
//        var mockAppBuilder = Substitute.For<IApplicationBuilder>();

//        // Act
//        var result = validator.Configure(next);

//        // Assert - Should not throw
//        result.Invoke(mockAppBuilder);
//        Assert.True(true); // Just to show the test passed without exception
//    }

//    [Fact]
//    public void Configure_WithDuplicateEmptySourceIds_ThrowsInvalidOperationException()
//    {
//        // Arrange
//        var mockSource1 = Substitute.For<ISourceProvider>();
//        mockSource1.Id.Returns("");

//        var mockSource2 = Substitute.For<ISourceProvider>();
//        mockSource2.Id.Returns("");

//        var operations = new List<ISourceProvider> { mockSource1, mockSource2 };
//        var validator = new DefaultProvidersValidator(operations);

//        Action<IApplicationBuilder> next = builder => { };

//        // Act & Assert
//        var exception = Assert.Throws<InvalidOperationException>(() => validator.Configure(next));

//        Assert.Contains($"Duplicate {nameof(ISourceProvider.Id)} values found", exception.Message);
//    }
//}
