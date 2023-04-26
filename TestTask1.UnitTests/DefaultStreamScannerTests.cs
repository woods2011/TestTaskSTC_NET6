using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using TestTask1.StreamScanner;

namespace TestTask1.UnitTests;

public class DefaultStreamScannerTests
{
    private readonly ConcurrentBag<ScanMatch> _matches = new();
    private void MatchHandler(ScanMatch match) => _matches.Add(match);

    private static readonly Random Rnd = Random.Shared;


    public static IEnumerable<object[]> GenericTestCases()
    {
        var templateAndMatchedValue1 = (template: @"v\d+", value: "v10");
        var templateAndMatchedValue2 = (template: @"\b(?:\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?\b", value: "1,111,111.1");

        yield return CreateGenericTestCase("StArT", "EnD", "CoNTaInS", templateAndMatchedValue1);
        yield return CreateGenericTestCase("StArT", "EnD", "CoNTaInS", templateAndMatchedValue2);

        static object[] CreateGenericTestCase(
            string start,
            string end,
            string contains,
            (string Template, string Value) templateAndValue)
        {
            const int matchedCount = 2;
            string rangeOfInterest = CreateGenericRange(start, end, contains, templateAndValue.Value);

            return new object[]
            {
                start, end, contains, rangeOfInterest, templateAndValue.Template, templateAndValue.Value, matchedCount
            };
            //  start, end, contains, rangeOfInterest, template,                  matchedValue,           countMatchesInRanges
        }
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldCorrectlyDetectMatchingRanges(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        int max = rangeOfInterest.Length;
        int expectedMatchCount = countMatchesInRanges;

        var testData = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testData);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
        if (expectedMatchCount == 0) return;
        _matches.Should().OnlyContain(match => match.MatchValue.Equals(matchedValue, StringComparison.Ordinal));
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldNotDetectMatchingRanges_WhenMaxIsLessThanRangeLength(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        int max = rangeOfInterest.Length - 1;
        int expectedMatchCount = 0;

        var testData = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testData);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldCorrectlyDetectMatchingRanges_WhenBufferSizeIsEqualToOne(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        const int bufferSize = 1;
        int max = rangeOfInterest.Length;
        int expectedMatchCount = countMatchesInRanges;

        var testData = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testData);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max),
            bufferSize);

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
        if (expectedMatchCount == 0) return;
        _matches.Should().OnlyContain(match => match.MatchValue.Equals(matchedValue, StringComparison.Ordinal));
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldCorrectlyDetectMatchingRanges_WhenOuterRangeNotValidButInnerRangeIsValid(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        int max = rangeOfInterest.Length;
        int expectedMatchCount = countMatchesInRanges;

        var testDataAddOuterInterval = $"some data {start} {matchedValue} some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testDataAddOuterInterval);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
        if (expectedMatchCount == 0) return;
        _matches.Should().OnlyContain(match => match.MatchValue.Equals(matchedValue, StringComparison.Ordinal));
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldNotDetectMatchingRanges_WhenRangeContainsForbiddenSymbols(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        var forbiddenSymbol = GetRandomElement('\x00', '\x0d', '\x0a');
        int max = rangeOfInterest.Length;
        int expectedMatchCount = 0;

        rangeOfInterest = rangeOfInterest.Insert(Rnd.Next(0, rangeOfInterest.Length), forbiddenSymbol.ToString());
        var testDataAddOuterInterval = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testDataAddOuterInterval);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldNotDetectMatchingRanges_WhenRangeNotContainsContains(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        contains = $"not{contains}";
        int max = rangeOfInterest.Length;
        int expectedMatchCount = 0;

        var testDataAddOuterInterval = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testDataAddOuterInterval);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
    }


    [Theory]
    [MemberData(nameof(TestCaseWithInversedCharacterCase))]
    public async Task ScanStreamAsync_ShouldNotDetectMatchingRanges_WhenCaseNotMatchedButScanningIsCaseSensitive(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        bool ignoreCase = false;
        int max = rangeOfInterest.Length;
        int expectedMatchCount = 0;

        var testDataAddOuterInterval = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testDataAddOuterInterval);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max, ignoreCase));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
    }


    [Theory]
    [MemberData(nameof(TestCaseWithInversedCharacterCase))]
    public async Task ScanStreamAsync_ShouldDetectMatchingRanges_WhenCaseNotMatchedButScanningIsNotCaseSensitive(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        bool ignoreCase = true;
        int max = rangeOfInterest.Length;
        int expectedMatchCount = countMatchesInRanges;

        var testDataAddOuterInterval = $"some data {rangeOfInterest} some data";
        MemoryStream memoryStream = ConvertStringToMemoryStreamAscii(testDataAddOuterInterval);

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamAsync(
            memoryStream,
            new StreamScanParams(start, end, contains, template, max, ignoreCase));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
        if (expectedMatchCount == 0) return;
        _matches.Should().OnlyContain(match => match.MatchValue.Equals(matchedValue, StringComparison.OrdinalIgnoreCase));
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamsInParallelAsync_ShouldWorkCorrectly(
        string start,
        string end,
        string contains,
        string rangeOfInterest,
        string template,
        string matchedValue,
        int countMatchesInRanges)
    {
        // Arrange
        var streamsCount = Rnd.Next(1, 7);
        int max = rangeOfInterest.Length;
        var expectedMatchCount = countMatchesInRanges * streamsCount;

        var testDataAddOuterInterval = $"some data {start} someData {rangeOfInterest} some data";
        var memoryStreams = Enumerable.Range(0, streamsCount)
            .Select(_ => ConvertStringToMemoryStreamAscii(testDataAddOuterInterval));

        var streamScanner = new DefaultStreamScanner(MatchHandler);

        // Act
        await streamScanner.ScanStreamsInParallelAsync(
            memoryStreams,
            new StreamScanParams(start, end, contains, template, max, ignoreCase: false));

        // Assert
        _matches.Should().HaveCount(expectedMatchCount);
        if (expectedMatchCount == 0) return;
        _matches.Should().OnlyContain(match => match.MatchValue.Equals(matchedValue, StringComparison.Ordinal));
    }


    private static string CreateGenericRange(
        string start,
        string end,
        string contains,
        string matchedValue,
        char? forbiddenSymbol = null,
        string dataSeparator = " some data some data ")
    {
        var innerRange =
            $"{dataSeparator}{contains}{dataSeparator}{matchedValue}{dataSeparator}{matchedValue}{dataSeparator}";
        var rangeStr = $"{start}{innerRange}{end}";

        if (forbiddenSymbol.HasValue)
            rangeStr = rangeStr.Insert(Rnd.Next(0, rangeStr.Length), forbiddenSymbol.Value.ToString());

        return rangeStr;
    }

    private static T GetRandomElement<T>(params T[] array) => array[Rnd.Next(array.Length)];

    private static MemoryStream ConvertStringToMemoryStreamAscii(string testData)
    {
        byte[] testDataBytes = Encoding.ASCII.GetBytes(testData);
        return new MemoryStream(testDataBytes);
    }


    public static IEnumerable<object[]> TestCaseWithInversedCharacterCase()
    {
        var templateAndMatchedValue1 = (template: @"v\d+", value: "v10");
        var templateAndMatchedValue2 = (template: @"\b(?:\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?\b", value: "1,111,111.1");

        foreach (object[] val in CreateInversedTestCases("START", "END", "CONTAINS", templateAndMatchedValue1))
            yield return val;

        foreach (object[] val in CreateInversedTestCases("START", "END", "CONTAINS", templateAndMatchedValue2))
            yield return val;

        static IEnumerable<object[]> CreateInversedTestCases(
            string start,
            string end,
            string contains,
            (string Template, string Value) templateAndValue)
        {
            const int matchedCount = 2;
            string rangeOfInterest = CreateGenericRange(start, end, contains, templateAndValue.Value);

            string reversedStart = ReverseCase(start);
            if (!reversedStart.Equals(start, StringComparison.Ordinal))
                yield return CreateTestCase(reversedStart, end, contains, rangeOfInterest, templateAndValue);

            string reversedEnd = ReverseCase(end);
            if (!reversedEnd.Equals(end, StringComparison.Ordinal))
                yield return CreateTestCase(start, reversedEnd, contains, rangeOfInterest, templateAndValue);

            string reversedContains = ReverseCase(contains);
            if (!reversedContains.Equals(contains, StringComparison.Ordinal))
                yield return CreateTestCase(start, end, reversedContains, rangeOfInterest, templateAndValue);

            static object[] CreateTestCase(
                string start,
                string end,
                string contains,
                string rangeOfInterest,
                (string Template, string Value) templateAndVal)
            {
                return new object[]
                {
                    start, end, contains, rangeOfInterest, templateAndVal.Template, templateAndVal.Value, matchedCount
                };
                //  start, end, contains, rangeOfInterest, template,                matchedValue,         countMatchesInRanges
            }
        }

        static string ReverseCase(string str)
        {
            char[] charArray = str.ToCharArray();

            for (int i = 0; i < charArray.Length; i++)
            {
                if (char.IsLower(charArray[i]))
                    charArray[i] = char.ToUpperInvariant(charArray[i]);

                else
                {
                    if (char.IsUpper(charArray[i]))
                        charArray[i] = char.ToLowerInvariant(charArray[i]);
                }
            }

            return new string(charArray);
        }
    }
}