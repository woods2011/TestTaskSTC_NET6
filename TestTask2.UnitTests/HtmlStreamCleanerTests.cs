using System.Text;
using FluentAssertions;

namespace TestTask2.UnitTests;

public class HtmlStreamCleaner2Tests
{
    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task RemoveHtmlTagsFromStreamAsync_ShouldRemoveHtmlTagsCorrectly(
        string htmlContent,
        string expectedResult)
    {
        // Arrange
        var encoding = Encoding.UTF8;

        var inputStream = new MemoryStream(encoding.GetBytes(htmlContent));
        var outputStream = new MemoryStream();

        // Act
        await NewHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(inputStream, outputStream, encoding: encoding);

        // Assert
        var result = encoding.GetString(outputStream.ToArray());
        result.Should().BeEquivalentTo(expectedResult);
    }
    
    
    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task RemoveHtmlTagsFromStreamAsync_ShouldRemoveHtmlTagsCorrectly_WhenEncodingIsTwoBytesAtLeast(
        string htmlContent,
        string expectedResult)
    {
        // Arrange
        var bufferSize = 3;
        var encoding = Encoding.UTF32;

        var inputStream = new MemoryStream(encoding.GetBytes(htmlContent));
        var outputStream = new MemoryStream();

        // Act
        await NewHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(inputStream, outputStream, bufferSize, encoding: encoding);

        // Assert
        var result = encoding.GetString(outputStream.ToArray());
        result.Should().BeEquivalentTo(expectedResult);
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task RemoveHtmlTagsFromStreamAsync_ShouldRemoveHtmlTagsCorrectly_WhenBufferSizeIsEqualToOne(
        string htmlContent,
        string expectedResult)
    {
        // Arrange
        var bufferSize = 1;
        var encoding = new UTF8Encoding(false);

        var inputStream = new MemoryStream(encoding.GetBytes(htmlContent));
        var outputStream = new MemoryStream();

        // Act
        await NewHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(
            inputStream, outputStream, bufferSize, encoding: encoding);

        // Assert
        var result = encoding.GetString(outputStream.ToArray());
        result.Should().BeEquivalentTo(expectedResult);
    }


    // На самом деле это интеграционный тест, но я не стал выносить его в отдельный проект
    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task RemoveHtmlTagsFromStreamAsync_ShouldRemoveHtmlTagsCorrectly_WhenInputStreamIsChunkedAndNoSeekable(
            string htmlContent,
            string expectedResult)
    {
        // Arrange
        var chunkSize = 1;
        var encoding = new UTF8Encoding(false);

        var inputStream = new EmulateChunkedNonSeekableStream(encoding.GetBytes(htmlContent), chunkSize);
        var outputStream = new MemoryStream();

        // Act
        await NewHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(inputStream, outputStream, encoding: encoding);

        // Assert
        var result = encoding.GetString(outputStream.ToArray());
        result.Should().BeEquivalentTo(expectedResult);
    }

    public static IEnumerable<object[]> GenericTestCases()
    {
        yield return new object[] { File.ReadAllText("Files/test.html"), File.ReadAllText("Files/testResult.txt") };
        yield return new object[] { "", "" };
        yield return new object[] { "тестTest", "тестTest" };
        yield return new object[] { "<html><body><p></p></body></html>", "" };
        yield return new object[] { "<html><body><p>test</p></body></html>", "test" };
        yield return new object[] { "test<html><body><p></p></body></html>", "test" };
        yield return new object[] { "<html><body><p></p></body></html>test", "test" };
        yield return new object[]
            { "<html><body><p>тест</p></body></html><html><body><p>test</p></body></html>", "тестtest" };
    }
}