using System.Text;
using FluentAssertions;
using TestTask1.Benchmarks.OtherBenchmarks;
using TestTask1.Helpers;

namespace TestTask1.UnitTests;

public class ManualIndexOfCaseSensitiveTests
{
    [Theory]
    [InlineData("abcde", "a", false)]
    [InlineData("abcde", "e", false)]
    [InlineData("abcde", "c", false)]
    [InlineData("abcde", "ab", false)]
    [InlineData("abcde", "bc", false)]
    [InlineData("abcde", "de", false)]
    [InlineData("abcde", "abcde", false)]
    [InlineData("abcde", "f", false)]
    [InlineData("abcde", "abcdef", false)]
    [InlineData("abcde", "A", true)]
    [InlineData("abcde", "E", true)]
    [InlineData("abcde", "C", true)]
    [InlineData("abcde", "AB", true)]
    [InlineData("abcde", "BC", true)]
    [InlineData("abcde", "DE", true)]
    [InlineData("abcde", "ABCDE", true)]
    [InlineData("abcde", "F", true)]
    [InlineData("abcde", "ABCDEF", true)]
    [InlineData("ABCabc", "a", true)]
    [InlineData("ABCabc", "A", true)]
    [InlineData("ABCaBC", "abc", true)]
    [InlineData("ABCaBC", "ABC", true)]
    [InlineData("123456789", "567", false)]
    [InlineData("123456789", "123", false)]
    [InlineData("123456789", "789", false)]
    [InlineData("Some words with spaces", "words", false)]
    [InlineData("Some words with spaces", "spaces", false)]
    [InlineData("Some words with spaces", "with", false)]
    [InlineData("Some words with spaces", "WORDS", true)]
    [InlineData("Some words with spaces", "SPACES", true)]
    [InlineData("Some words with spaces", "WITH", true)]
    [InlineData("abc!@#123", "!@#", false)]
    [InlineData("abc!@#123", "!@#1", false)]
    [InlineData("abc!@#123", "23", false)]
    [InlineData("abc!@#123", "bc!@", false)]
    [InlineData("abc!@#123", "c!@#", false)]
    [InlineData("\tabc\t123", "\t", false)]
    [InlineData("\tabc\t123", "c\t", false)]
    [InlineData("\tabc\t123", "\t123", false)]
    [InlineData("\tabc\t123", "abc", false)]
    [InlineData("\tabc\t123", "bc\t", false)]
    [InlineData("\x01\x02\x03\x04\x05", "\x02\x03", false)]
    [InlineData("\x01\x02\x03\x04\x05", "\x01\x02\x03", false)]
    [InlineData("\x01\x02\x03\x04\x05", "\x03\x04\x05", false)]
    [InlineData("\x01\x02\x03\x04\x05", "\x02\x03\x04", false)]
    [InlineData("a\x01b\x02c", "b\x02c", false)]
    [InlineData("a\x01b\x02c", "a\x01b", false)]
    [InlineData("a\x01b\x02c", "\x01b\x02", false)]
    public void TestManualIndexOf2(string source, string pattern, bool ignoreCase)
    {
        // Arrange
        byte[] sourceBytes = Encoding.ASCII.GetBytes(source);
        byte[] patternBytes = Encoding.ASCII.GetBytes(pattern);
        int expectedResult = MemoryExtensions.IndexOf(
            source, pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    
        // Act
        int actual = sourceBytes.AsSpan().IndexOf(patternBytes, ignoreCase);
    
        // Assert
        actual.Should().Be(expectedResult);
    }
}