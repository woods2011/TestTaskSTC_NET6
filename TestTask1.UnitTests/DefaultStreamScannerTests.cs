using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using TestTask1.StreamScanner;
using TestTask1.UnitTests.Helpers;
using Xunit.Abstractions;

namespace TestTask1.UnitTests;

public class DefaultStreamScannerTests
{
    private static readonly Random Rnd = Random.Shared;
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly ConcurrentQueue<RangeMatch> _rangeMatches = new();

    private void MatchHandler(RangeMatch rangeMatch) => _rangeMatches.Enqueue(rangeMatch);

    public DefaultStreamScannerTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;


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
        const int expectedNumberOfValidRanges = 4;
        int expectedMatchPerRangeCount = countMatchesInRanges;

        var testData =
            $"{rangeOfInterest}\r sme {start}ada{rangeOfInterest}{rangeOfInterest}\r{start}\r{end} \na {rangeOfInterest}";

        int chunkSize = Rnd.Next(1, testData.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testData, chunkSize);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);

        _rangeMatches.Should().AllSatisfy(match => match.RangeLength.Should().Be(rangeOfInterest.Length));
        _rangeMatches.Should().AllSatisfy(match => match.ScanMatches.Should().HaveCount(expectedMatchPerRangeCount));

        IEnumerable<ScanMatch> allScanMatches = _rangeMatches.SelectMany(rangeMatch => rangeMatch.ScanMatches);
        var uniqueScanMatchesIndexes = _rangeMatches.First().ScanMatches.Select(match => match.MatchIndexInRange);

        allScanMatches
            .Should()
            .OnlyContain(scanMatch => scanMatch.MatchValue.ToString().Equals(matchedValue, StringComparison.Ordinal))
            .And
            .AllSatisfy(scanMatch => scanMatch.MatchIndexInRange.Should().BeOneOf(uniqueScanMatchesIndexes));
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
        const int expectedNumberOfValidRanges = 0;

        var testData =
            $"{rangeOfInterest} sme {start}ada{rangeOfInterest}{rangeOfInterest}a{start}d{end} \na {rangeOfInterest}";

        int chunkSize = Rnd.Next(1, testData.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testData, chunkSize);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);
    }


    [Theory]
    [MemberData(nameof(GenericTestCases))]
    public async Task ScanStreamAsync_ShouldCorrectlyDetectMatchingRanges_WhenStreamChunkSizeIsEqualToOne(
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
        const int expectedNumberOfValidRanges = 4;
        int expectedMatchPerRangeCount = countMatchesInRanges;

        var testData =
            $"{rangeOfInterest}\r sme {start}ada{rangeOfInterest}{rangeOfInterest}\r{start}\r{end} \na {rangeOfInterest}";

        Stream chunkedStream = ConvertStringToStreamAscii(testData, 1);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);

        _rangeMatches.Should().AllSatisfy(match => match.RangeLength.Should().Be(rangeOfInterest.Length));
        _rangeMatches.Should().AllSatisfy(match => match.ScanMatches.Should().HaveCount(expectedMatchPerRangeCount));

        IEnumerable<ScanMatch> allScanMatches = _rangeMatches.SelectMany(rangeMatch => rangeMatch.ScanMatches);
        var uniqueScanMatchesIndexes = _rangeMatches.First().ScanMatches.Select(match => match.MatchIndexInRange);

        allScanMatches
            .Should()
            .OnlyContain(scanMatch => scanMatch.MatchValue.ToString().Equals(matchedValue, StringComparison.Ordinal))
            .And
            .AllSatisfy(scanMatch => scanMatch.MatchIndexInRange.Should().BeOneOf(uniqueScanMatchesIndexes));
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
        const int expectedNumberOfValidRanges = 2;
        int expectedMatchPerRangeCount = countMatchesInRanges;

        var testDataAddOuterInterval =
            $"{start} {matchedValue} some {rangeOfInterest} data {start} {matchedValue} {rangeOfInterest} {end} some data";

        int chunkSize = Rnd.Next(1, testDataAddOuterInterval.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testDataAddOuterInterval, chunkSize);
        int readBufferSize = Rnd.Next(1, testDataAddOuterInterval.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);


        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);

        _rangeMatches.Should().AllSatisfy(match => match.RangeLength.Should().Be(rangeOfInterest.Length));
        _rangeMatches.Should().AllSatisfy(match => match.ScanMatches.Should().HaveCount(expectedMatchPerRangeCount));

        IEnumerable<ScanMatch> allScanMatches = _rangeMatches.SelectMany(rangeMatch => rangeMatch.ScanMatches);
        var uniqueScanMatchesIndexes = _rangeMatches.First().ScanMatches.Select(match => match.MatchIndexInRange);

        allScanMatches
            .Should()
            .OnlyContain(scanMatch => scanMatch.MatchValue.ToString().Equals(matchedValue, StringComparison.Ordinal))
            .And
            .AllSatisfy(scanMatch => scanMatch.MatchIndexInRange.Should().BeOneOf(uniqueScanMatchesIndexes));
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
        int max = rangeOfInterest.Length;
        const int expectedNumberOfValidRanges = 0;

        char forbiddenSymbol = Rnd.NextOneOf('\x00', '\x0d', '\x0a');
        int insertIndex = Rnd.Next(1, rangeOfInterest.Length);
        rangeOfInterest = rangeOfInterest.Insert(insertIndex, forbiddenSymbol.ToString());

        var testDataWithForbidden = $"some data {rangeOfInterest} some data{start}{matchedValue}{rangeOfInterest}";

        int chunkSize = Rnd.Next(1, testDataWithForbidden.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testDataWithForbidden, chunkSize);
        int readBufferSize = Rnd.Next(1, testDataWithForbidden.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);
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
        const int expectedNumberOfValidRanges = 0;

        var testData = $"some data {rangeOfInterest} some data{start}{matchedValue}{rangeOfInterest}";

        int chunkSize = Rnd.Next(1, testData.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testData, chunkSize);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);
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
        const int expectedNumberOfValidRanges = 0;

        var testData = $"some data {contains}{rangeOfInterest} some {rangeOfInterest}{end}data";

        int chunkSize = Rnd.Next(1, testData.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testData, chunkSize);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");

        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max, ignoreCase),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);
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
        const int expectedNumberOfValidRanges = 2;
        int expectedMatchPerRangeCount = countMatchesInRanges;

        var testData = $"some data {contains}{rangeOfInterest} some {rangeOfInterest}{end}data";

        int chunkSize = Rnd.Next(1, testData.Length + 1);
        Stream chunkedStream = ConvertStringToStreamAscii(testData, chunkSize);
        int readBufferSize = Rnd.Next(1, testData.Length + 10);


        _testOutputHelper.WriteLine($"{nameof(chunkSize)}: {chunkSize}");
        _testOutputHelper.WriteLine($"{nameof(readBufferSize)}: {readBufferSize}");
        
        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamAsync(
            chunkedStream,
            new StreamScanParams(start, end, contains, template, max, ignoreCase),
            readBufferSize: readBufferSize);

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);

        _rangeMatches.Should().AllSatisfy(match => match.RangeLength.Should().Be(rangeOfInterest.Length));
        _rangeMatches.Should().AllSatisfy(match => match.ScanMatches.Should().HaveCount(expectedMatchPerRangeCount));

        IEnumerable<ScanMatch> allScanMatches = _rangeMatches.SelectMany(rangeMatch => rangeMatch.ScanMatches);
        var uniqueScanMatchesIndexes = _rangeMatches.First().ScanMatches.Select(match => match.MatchIndexInRange);

        allScanMatches
            .Should()
            .OnlyContain(scanMatch => scanMatch.MatchValue.ToString().Equals(matchedValue, StringComparison.Ordinal))
            .And
            .AllSatisfy(scanMatch => scanMatch.MatchIndexInRange.Should().BeOneOf(uniqueScanMatchesIndexes));
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
        int max = rangeOfInterest.Length;

        var streamsCount = Rnd.Next(1, 7);
        const int expectedNumberOfValidRangesInOneStream = 2;
        var expectedNumberOfValidRanges = streamsCount * expectedNumberOfValidRangesInOneStream;

        var testData = $"some data {start} someData {rangeOfInterest} some {rangeOfInterest} data";

        var memoryStreams = Enumerable.Range(0, streamsCount).Select(_ => ConvertStringToStreamAscii(testData));


        var sut = new DefaultStreamScanner(MatchHandler);

        // Act
        await sut.ScanStreamsInParallelAsync(
            memoryStreams,
            new StreamScanParams(start, end, contains, template, max));

        // Assert
        _rangeMatches.Should().HaveCount(expectedNumberOfValidRanges);
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

    private static Stream ConvertStringToStreamAscii(string testData, int chunkSize = -1)
    {
        byte[] testDataBytes = Encoding.ASCII.GetBytes(testData);
        return chunkSize is -1
            ? new MemoryStream(testDataBytes)
            : new EmulateChunkedNonSeekableStream(testDataBytes, chunkSize);
    }
}

// $"{rangeOfInterest}\r sme {start}ada{rangeOfInterest}{rangeOfInterest}\r{start}\r{end} \na {rangeOfInterest}";
// $"{rangeOfInterest}\r sme {start}ada{rangeOfInterest.Replace("v1", "v2")}{rangeOfInterest.Replace("v1", "v3")}\r{start}\r{end} \na {rangeOfInterest.Replace("v1", "v4")}";
// var testData = $"some data {start}{contains}{rangeOfInterest} some {rangeOfInterest}{end}data";
