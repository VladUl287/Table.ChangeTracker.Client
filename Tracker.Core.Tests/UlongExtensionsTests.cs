using Tracker.Core.Extensions;

namespace Tracker.Core.Tests;

public class ULongExtensionsTests
{
    public class GetDigitCountTests
    {
        [Theory]
        [InlineData(0UL, 1)]
        [InlineData(1UL, 1)]
        [InlineData(9UL, 1)]
        public void GetDigitCount_WhenSingleDigit_ReturnsOne(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10UL, 2)]
        [InlineData(99UL, 2)]
        [InlineData(100UL, 3)]
        [InlineData(999UL, 3)]
        [InlineData(1000UL, 4)]
        [InlineData(9999UL, 4)]
        [InlineData(10000UL, 5)]
        [InlineData(99999UL, 5)]
        public void GetDigitCount_AtDigitBoundaries_ReturnsCorrectDigitCount(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(55UL, 2)]
        [InlineData(555UL, 3)]
        [InlineData(5555UL, 4)]
        [InlineData(55555UL, 5)]
        [InlineData(555555UL, 6)]
        [InlineData(5555555UL, 7)]
        [InlineData(55555555UL, 8)]
        [InlineData(555555555UL, 9)]
        public void GetDigitCount_WithMiddleValues_ReturnsCorrectDigitCount(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10000000000UL, 11)]
        [InlineData(99999999999UL, 11)]
        [InlineData(100000000000UL, 12)]
        [InlineData(999999999999UL, 12)]
        [InlineData(1000000000000UL, 13)]
        [InlineData(9999999999999UL, 13)]
        public void GetDigitCount_WithLargeNumbers_ReturnsCorrectDigitCount(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1000000000000000000UL, 19)]
        [InlineData(9999999999999999999UL, 19)]
        [InlineData(18446744073709551615UL, 20)]
        public void GetDigitCount_WithVeryLargeNumbers_ReturnsCorrectDigitCount(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1UL, 1)]
        [InlineData(10UL, 2)]
        [InlineData(100UL, 3)]
        [InlineData(1000UL, 4)]
        [InlineData(10000UL, 5)]
        [InlineData(100000UL, 6)]
        [InlineData(1000000UL, 7)]
        [InlineData(10000000UL, 8)]
        [InlineData(100000000UL, 9)]
        [InlineData(1000000000UL, 10)]
        [InlineData(10000000000UL, 11)]
        public void GetDigitCount_PowersOfTen_ReturnsCorrectDigitCount(ulong number, int expected)
        {
            // Act
            var result = number.GetDigitCount();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetDigitCount_ForAllDigitLengths_CoversAllRanges()
        {
            // Arrange & Act & Assert for each digit length
            for (int digits = 1; digits <= 20; digits++)
            {
                ulong min = digits == 1 ? 0UL : (ulong)Math.Pow(10, digits - 1);
                ulong max = digits == 20 ? ulong.MaxValue : (ulong)Math.Pow(10, digits) - 1;

                // Test minimum value in range
                Assert.Equal(digits, min.GetDigitCount());

                // Test maximum value in range (except for ulong.MaxValue which is handled separately)
                if (digits < 20)
                {
                    Assert.Equal(digits, max.GetDigitCount());
                }

                // Test a value in the middle of the range
                if (digits < 20)
                {
                    ulong middle = min + (max - min) / 2;
                    Assert.Equal(digits, middle.GetDigitCount());
                }
            }
        }

        [Fact]
        public void GetDigitCount_RandomNumbers_ReturnsCorrectDigitCount()
        {
            // Arrange
            var random = new Random();
            var testCases = Enumerable.Range(0, 100)
                .Select(_ => GenerateRandomNumber(random))
                .ToList();

            foreach (var number in testCases)
            {
                // Act
                var result = number.GetDigitCount();
                var expected = number.ToString().Length;

                // Assert
                Assert.Equal(expected, result);
            }
        }

        private static ulong GenerateRandomNumber(Random random)
        {
            byte[] buffer = new byte[8];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}
