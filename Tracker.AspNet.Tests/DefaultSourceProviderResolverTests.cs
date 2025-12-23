//using Moq;
//using Tracker.AspNet.Services;
//using Microsoft.Extensions.Logging;
//using Tracker.Core.Services.Contracts;
//using Tracker.AspNet.Models;
//using Tracker.AspNet.Tests.AttirubtesTests;

//namespace Tracker.AspNet.Tests;

//public class DefaultProviderResolverTests
//{
//    private readonly Mock<ILogger<DefaultProviderResolver>> _loggerMock;
//    private readonly Mock<IProviderIdGenerator> _idGeneratorMock;
//    private readonly List<Mock<ISourceProvider>> _providerMocks;

//    public DefaultProviderResolverTests()
//    {
//        _loggerMock = new Mock<ILogger<DefaultProviderResolver>>();
//        _idGeneratorMock = new Mock<IProviderIdGenerator>();

//        _providerMocks = new List<Mock<ISourceProvider>>
//        {
//            CreateProviderMock("provider1"),
//            CreateProviderMock("provider2"),
//            CreateProviderMock("provider3")
//        };
//    }

//    private Mock<ISourceProvider> CreateProviderMock(string id)
//    {
//        var mock = new Mock<ISourceProvider>();
//        mock.Setup(p => p.Id).Returns(id);
//        return mock;
//    }

//    private DefaultProviderResolver CreateResolver(
//        IEnumerable<ISourceProvider>? providers = null,
//        IProviderIdGenerator? idGenerator = null,
//        ILogger<DefaultProviderResolver>? logger = null)
//    {
//        return new DefaultProviderResolver(
//            providers ?? _providerMocks.Select(m => m.Object),
//            idGenerator ?? _idGeneratorMock.Object,
//            logger ?? _loggerMock.Object);
//    }

//    [Fact]
//    public void Constructor_ShouldCreateStoreAndDefaultProvider()
//    {
//        // Act
//        var resolver = CreateResolver();

//        // Assert
//        Assert.NotNull(resolver);

//        // Verify that all providers are accessible
//        var provider = resolver.SelectProvider("provider1", new ImmutableGlobalOptions());
//        Assert.Equal("provider1", provider?.Id);
//    }

//    public class SelectProviderWithProviderIdAndImmutableOptionsTests
//    {
//        private readonly DefaultProviderResolverTests _fixture;

//        public SelectProviderWithProviderIdAndImmutableOptionsTests()
//        {
//            _fixture = new DefaultProviderResolverTests();
//        }

//        [Fact]
//        public void SelectProvider_WithValidProviderId_ShouldReturnProvider()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions();

//            // Act
//            var result = resolver.SelectProvider("provider2", options);

//            // Assert
//            Assert.Equal("provider2", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithInvalidProviderId_ShouldThrowException()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions();

//            // Act & Assert
//            var exception = Assert.Throws<InvalidOperationException>(
//                () => resolver.SelectProvider("invalid-provider", options));

//            Assert.Contains("invalid-provider", exception.Message);
//        }

//        [Fact]
//        public void SelectProvider_WithNullProviderIdAndNoSourceOps_ShouldReturnDefault()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions
//            {
//                SourceProvider = null,
//                SourceProviderFactory = null
//            };

//            // Act
//            var result = resolver.SelectProvider(null, options);

//            // Assert
//            Assert.Equal("provider1", result?.Id); // First provider is default
//        }

//        [Fact]
//        public void SelectProvider_WithNullProviderIdAndSourceProviderInOptions_ShouldReturnFromOptions()
//        {
//            // Arrange
//            var customProvider = _fixture.CreateProviderMock("custom-provider").Object;
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions
//            {
//                SourceProvider = customProvider
//            };

//            // Act
//            var result = resolver.SelectProvider(null, options);

//            // Assert
//            Assert.Equal(customProvider, result);
//        }
//    }

//    public class SelectProviderWithGlobalOptionsTests
//    {
//        private readonly DefaultProviderResolverTests _fixture;

//        public SelectProviderWithGlobalOptionsTests()
//        {
//            _fixture = new DefaultProviderResolverTests();
//        }

//        [Fact]
//        public void SelectProvider_WithValidSourceIdInGlobalOptions_ShouldReturnProvider()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions
//            {
//                Source = "provider3"
//            };

//            // Act
//            var result = resolver.SelectProvider(options);

//            // Assert
//            Assert.Equal("provider3", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithInvalidSourceIdInGlobalOptions_ShouldThrowException()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions
//            {
//                Source = "invalid-id"
//            };

//            // Act & Assert
//            var exception = Assert.Throws<InvalidOperationException>(
//                () => resolver.SelectProvider(options));

//            Assert.Contains("invalid-id", exception.Message);
//            Assert.Contains("Available provider IDs:", exception.Message);
//        }

//        [Fact]
//        public void SelectProvider_WithNoSourceIdAndNoOps_ShouldReturnDefault()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions
//            {
//                Source = null,
//                SourceProvider = null,
//                SourceProviderFactory = null
//            };

//            // Act
//            var result = resolver.SelectProvider(options);

//            // Assert
//            Assert.Equal("provider1", result?.Id);
//        }
//    }

//    public class SelectProviderWithContextAndProviderIdTests
//    {
//        private readonly DefaultProviderResolverTests _fixture;

//        public SelectProviderWithContextAndProviderIdTests()
//        {
//            _fixture = new DefaultProviderResolverTests();
//        }

//        [Fact]
//        public void SelectProvider_WithValidProviderIdAndContext_ShouldReturnProvider()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions();
//            _fixture._idGeneratorMock.Setup(g => g.GenerateId<TestDbContext>())
//                .Returns("generated-id");

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>("provider2", options);

//            // Assert
//            Assert.Equal("provider2", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithInvalidProviderIdAndContext_ShouldThrowException()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions();

//            // Act & Assert
//            var exception = Assert.Throws<InvalidOperationException>(
//                () => resolver.SelectProvider<TestDbContext>("invalid-id", options));

//            Assert.Contains(nameof(TestDbContext), exception.Message);
//            Assert.Contains("invalid-id", exception.Message);
//        }

//        [Fact]
//        public void SelectProvider_WithNullProviderIdAndGeneratedIdFound_ShouldReturnGenerated()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions();
//            _fixture._idGeneratorMock.Setup(g => g.GenerateId<TestDbContext>())
//                .Returns("provider2");

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>(null, options);

//            // Assert
//            Assert.Equal("provider2", result?.Id);
//            _fixture._idGeneratorMock.Verify(g => g.GenerateId<TestDbContext>(), Times.Once);
//        }

//        [Fact]
//        public void SelectProvider_WithNullProviderIdGeneratedIdNotFoundNoOps_ShouldReturnDefault()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new ImmutableGlobalOptions
//            {
//                SourceProvider = null,
//                SourceProviderFactory = null
//            };
//            _fixture._idGeneratorMock.Setup(g => g.GenerateId<TestDbContext>())
//                .Returns("non-existent-id");

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>(null, options);

//            // Assert
//            Assert.Equal("provider1", result?.Id);
//        }
//    }

//    public class SelectProviderWithContextAndGlobalOptionsTests
//    {
//        private readonly DefaultProviderResolverTests _fixture;

//        public SelectProviderWithContextAndGlobalOptionsTests()
//        {
//            _fixture = new DefaultProviderResolverTests();
//        }

//        [Fact]
//        public void SelectProvider_WithValidSourceIdInGlobalOptionsAndContext_ShouldReturnProvider()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions
//            {
//                Source = "provider3"
//            };

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>(options);

//            // Assert
//            Assert.Equal("provider3", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithNoSourceIdGeneratedIdFound_ShouldReturnGeneratedProvider()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions { Source = null };
//            _fixture._idGeneratorMock.Setup(g => g.GenerateId<TestDbContext>())
//                .Returns("provider2");

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>(options);

//            // Assert
//            Assert.Equal("provider2", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithNoSourceIdGeneratedIdNotFoundNoOps_ShouldReturnDefault()
//        {
//            // Arrange
//            var resolver = _fixture.CreateResolver();
//            var options = new GlobalOptions
//            {
//                Source = null,
//                SourceProvider = null,
//                SourceProviderFactory = null
//            };
//            _fixture._idGeneratorMock.Setup(g => g.GenerateId<TestDbContext>())
//                .Returns("non-existent-id");

//            // Act
//            var result = resolver.SelectProvider<TestDbContext>(options);

//            // Assert
//            Assert.Equal("provider1", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_WithEmptyProviderList_ShouldWork()
//        {
//            // Arrange
//            var singleProviderMock = _fixture.CreateProviderMock("single-provider");
//            var resolver = _fixture.CreateResolver(
//                providers: new[] { singleProviderMock.Object });
//            var options = new GlobalOptions();

//            // Act
//            var result = resolver.SelectProvider(options);

//            // Assert
//            Assert.Equal("single-provider", result?.Id);
//        }

//        [Fact]
//        public void SelectProvider_ProviderIdCaseSensitive_ShouldRespectCase()
//        {
//            // Arrange
//            var caseSensitiveProvider = _fixture.CreateProviderMock("PROVIDER1").Object;
//            var resolver = _fixture.CreateResolver(providers: [caseSensitiveProvider]);
//            var options = new ImmutableGlobalOptions();

//            // Act
//            var result1 = resolver.SelectProvider("PROVIDER1", options);
//            var exception = Assert.Throws<InvalidOperationException>(
//                () => resolver.SelectProvider("provider1", options));

//            // Assert
//            Assert.Equal("PROVIDER1", result1?.Id);
//            Assert.Contains("provider1", exception.Message);
//        }
//    }
//}
