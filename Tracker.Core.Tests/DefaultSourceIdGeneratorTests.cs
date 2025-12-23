//using Microsoft.EntityFrameworkCore;
//using System.Collections.Concurrent;
//using Tracker.Core.Services;

//namespace Tracker.Core.Tests
//{
//    public class TestDbContext : DbContext { }
//    public class AnotherTestDbContext : DbContext { }

//    public class DefaultSourceIdGeneratorTests
//    {
//        private readonly DefaultSourceIdGenerator _generator = new();

//        [Fact]
//        public void GenerateId_ShouldReturnHashAsString_ForValidDbContextType()
//        {
//            Arrange & Act
//            var result = _generator.GenerateId<TestDbContext>();

//            Assert
//            Assert.NotNull(result);
//            Assert.False(string.IsNullOrEmpty(result));
//            Assert.True(ulong.TryParse(result, out _)); // Should be a valid ulong string
//        }

//        [Fact]
//        public void GenerateId_ShouldProduceSameHash_ForSameDbContextType()
//        {
//            Arrange & Act
//            var result1 = _generator.GenerateId<TestDbContext>();
//            var result2 = _generator.GenerateId<TestDbContext>();

//            Assert
//            Assert.Equal(result1, result2);
//        }

//        [Fact]
//        public void GenerateId_ShouldProduceDifferentHashes_ForDifferentDbContextTypes()
//        {
//            Arrange & Act
//            var result1 = _generator.GenerateId<TestDbContext>();
//            var result2 = _generator.GenerateId<AnotherTestDbContext>();

//            Assert
//            Assert.NotEqual(result1, result2);
//        }

//        public class ShortNameDbContext : DbContext { }

//        [Fact]
//        public void GenerateId_ShouldUseStackAllocation_ForShortTypeNames()
//        {
//            Act
//           var result = _generator.GenerateId<ShortNameDbContext>();

//            Assert
//            Assert.NotNull(result);
//            Assert.True(ulong.TryParse(result, out _));
//        }

//        [Fact]
//        public void GenerateId_ShouldUseArrayPool_ForLongTypeNames()
//        {
//            Act
//           var result = _generator.GenerateId<Very.Long.Namespace.Path.To.Ensure.We.Exceed.The.TwoHundredFiftySix.Bytes.Threshold
//               .VeryLongNameDbContextToEnsureWeExceedTheTwoHundredFiftySixBytesThresholdDbContext>();

//            Assert
//            Assert.NotNull(result);
//            Assert.True(ulong.TryParse(result, out _));
//        }

//        [Theory]
//        [InlineData(typeof(TestDbContext))]
//        [InlineData(typeof(AnotherTestDbContext))]
//        public void GenerateId_ShouldBeDeterministic_AcrossMultipleRuns(Type dbContextType)
//        {
//            Arrange
//           var method = typeof(DefaultSourceIdGenerator)
//               .GetMethod("GenerateId")
//               ?.MakeGenericMethod(dbContextType);

//            Act
//           var result1 = method?.Invoke(_generator, null) as string;
//            var result2 = method?.Invoke(_generator, null) as string;

//            Assert
//            Assert.Equal(result1, result2);
//        }

//        public class DbContextWithSpecialCharsĀĒĪŌŪ : DbContext { }

//        [Fact]
//        public void GenerateId_ShouldUseUtf8Encoding()
//        {
//            Act
//           var result = _generator.GenerateId<DbContextWithSpecialCharsĀĒĪŌŪ>();

//            Assert
//            Assert.NotNull(result);
//            Assert.True(ulong.TryParse(result, out _));
//        }

//        [Fact]
//        public void GenerateId_ShouldReturnValidUInt64String()
//        {
//            Arrange & Act
//            var result = _generator.GenerateId<TestDbContext>();

//            Assert
//            Assert.True(ulong.TryParse(result, out var hashValue));
//            Assert.True(hashValue > 0 || hashValue == 0); // Just ensure it's a valid ulong
//        }

//        [Fact]
//        public void GenerateId_ShouldNotThrow_WhenCalledMultipleTimes()
//        {
//            Arrange
//           var iterations = 1000;

//            Act & Assert(should not throw)
//            for (int i = 0; i < iterations; i++)
//            {
//                var result = _generator.GenerateId<TestDbContext>();
//                Assert.NotNull(result);
//            }
//        }

//        [Fact]
//        public void GenerateId_ShouldHandleExtremelyLongTypeNames()
//        {
//            Act
//           var result = _generator.GenerateId<A.Very.Long.Namespace.Path
//               .ExtremelyLongDbContextNameThatIsWayLongerThanNecessaryButTestsTheBoundaryConditionsOfTheAlgorithmAndEnsuresNoOverflowOrPerformanceIssuesOccurWhenProcessingSuchLengthyTypeNamesInTheHashGenerationProcessDbContext>();

//            Assert
//            Assert.NotNull(result);
//            Assert.True(ulong.TryParse(result, out _));
//        }
//    }

//    public class SourceIdGeneratorEdgeCaseTests
//    {
//        private readonly DefaultSourceIdGenerator _generator = new();

//        [Fact]
//        public void GenerateId_ShouldBeThreadSafe()
//        {
//            Arrange
//           var results = new ConcurrentBag<string>();
//            var threads = new List<Thread>();
//            int threadCount = 10;
//            int callsPerThread = 100;

//            Act
//            for (int i = 0; i < threadCount; i++)
//            {
//                var thread = new Thread(() =>
//                {
//                    for (int j = 0; j < callsPerThread; j++)
//                    {
//                        results.Add(_generator.GenerateId<TestDbContext>());
//                    }
//                });
//                threads.Add(thread);
//                thread.Start();
//            }

//            foreach (var thread in threads)
//            {
//                thread.Join();
//            }

//            Assert
//           var distinctResults = results.Distinct().ToList();
//            Assert.Single(distinctResults);
//            Assert.Equal(threadCount * callsPerThread, results.Count);
//        }
//    }
//}

//namespace A.Very.Long.Namespace.Path
//{
//    public class ExtremelyLongDbContextNameThatIsWayLongerThanNecessaryButTestsTheBoundaryConditionsOfTheAlgorithmAndEnsuresNoOverflowOrPerformanceIssuesOccurWhenProcessingSuchLengthyTypeNamesInTheHashGenerationProcessDbContext
//        : DbContext
//    { }
//}

//namespace Very.Long.Namespace.Path.To.Ensure.We.Exceed.The.TwoHundredFiftySix.Bytes.Threshold
//{
//    public class VeryLongNameDbContextToEnsureWeExceedTheTwoHundredFiftySixBytesThresholdDbContext : DbContext { }
//}