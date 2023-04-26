using System.Text;
using FluentAssertions;
using TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

namespace TestTask1.UnitTests;

public class StringBuilderExtensionsTest
{
    [Theory]
    [InlineData("", "not present", -1)]
    [InlineData("Test 123!", "", 0)]
    [InlineData("Test 123!", "Test", 0)]
    [InlineData("Test 123!", "Test 123!", 0)]
    [InlineData("Test 123!a", "Test 123!", 0)]
    [InlineData("aTest 123!", "Test 123!", 1)]
    [InlineData("Test 123!", "123!", 5)]
    [InlineData("Test 123!", "!", 8)]
    [InlineData("Test 123!", "not present", -1)]
    public void IndexOf_ReturnsCorrectIndex(string input, string search, int expectedIndex)
    {
        // Arrange
        var sb = new StringBuilder(input);

        // Act
        int index = sb.IndexOf(search);

        // Assert
        index.Should().Be(expectedIndex);
    }

    [Theory]
    [InlineData("", "not present", 1, -1)]
    [InlineData("Test 123!", "", 5, 5)]
    [InlineData("Test 123!", "Test", 1, -1)]
    [InlineData("Test 123!", "est 123!", 1, 1)]
    [InlineData("aTest 123!", "Test 123!", 1, 1)]
    [InlineData("Test 123!", "123!", 6, -1)]
    [InlineData("Test 123!", "!", 8, 8)]
    [InlineData("Test 123!", "not present", 5, -1)]
    [InlineData("", "not present", 0, -1)]
    [InlineData("Test 123!", "", 0, 0)]
    [InlineData("Test 123!", "Test", 0, 0)]
    [InlineData("Test 123!", "Test 123!", 0, 0)]
    [InlineData("Test 123!a", "Test 123!", 0, 0)]
    [InlineData("aTest 123!", "Test 123!", 0, 1)]
    [InlineData("Test 123!", "123!", 0, 5)]
    [InlineData("Test 123!", "!", 0, 8)]
    [InlineData("Test 123!", "not present", 0, -1)]
    public void IndexOf_ReturnsCorrectIndex_WhenOffsetIsSet(string input, string search, int start, int expectedIndex)
    {
        // Arrange
        var sb = new StringBuilder(input);

        // Act
        int index = sb.IndexOf(search, start);

        // Assert
        index.Should().Be(expectedIndex);
    }

    [Theory]
    [InlineData("Case insensitive", "Case ", 0)]
    [InlineData("Case insensitive", "CASE ", -1)]
    [InlineData("Case insen1!sitive", "insen1!sitive", 5)]
    [InlineData("Case insen1!sitive", "InSeN1!sItIvE", -1)]
    public void IndexOf_ShouldBeCaseSensitive_WhenIgnoreCaseSetToFalse(string input, string search, int expectedIndex)
    {
        // Arrange
        var sb = new StringBuilder(input);

        // Act
        int index = sb.IndexOf(search, ignoreCase: false);

        // Assert
        index.Should().Be(expectedIndex);
    }


    [Theory]
    [InlineData("Case insensitive", "Case ", 0)]
    [InlineData("Case insensitive", "CASE ", 0)]
    [InlineData("Case insen1!sitive", "insen1!sitive", 5)]
    [InlineData("Case insen1!sitive", "InSeN1!sItIvE", 5)]
    public void IndexOf_ShouldBeCaseInsensitive_WhenIgnoreCaseSetToTrue(string input, string search, int expectedIndex)
    {
        // Arrange
        var sb = new StringBuilder(input);

        // Act
        int index = sb.IndexOf(search, ignoreCase: true);

        // Assert
        index.Should().Be(expectedIndex);
    }
}