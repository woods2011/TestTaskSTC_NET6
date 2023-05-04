using System.Text;
using BenchmarkDotNet.Attributes;
using TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;
using TestTask1.StreamScanner;
using TestTask1.TestFiles;

namespace TestTask1.Benchmarks;

[MemoryDiagnoser]
public class StreamScannerBenchmarks
{
    [Params(4096, 4096 * 4)]
    public int BufferSize { get; set; }


    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task DefaultStreamScanner(TestDataSet testData)
    {
        // MemoryStream stream = testData.InMemoryVersion;
        // stream.Position = 0;
        await using FileStream stream = File.Open(testData.FilePath, FileMode.Open, FileAccess.Read);
        await new DefaultStreamScanner().ScanStreamAsync(stream, testData.ScanParams, BufferSize);
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task SimpleGreedyStreamScanner(TestDataSet testData)
    {
        // MemoryStream stream = testData.InMemoryVersion;
        // stream.Position = 0;
        await using FileStream stream = File.Open(testData.FilePath, FileMode.Open, FileAccess.Read);
        await new SimpleGreedyStreamScanner().ScanStreamAsync(stream, testData.ScanParams, BufferSize);
    }

    // [Benchmark]
    // [ArgumentsSource(nameof(TestDataSets))]
    // public async Task OldScanner(TestDataSet testData)
    // {
    //     // MemoryStream stream = testData.InMemoryVersion;
    //     // stream.Position = 0;
    //     await using FileStream stream = File.Open(testData.FilePath, FileMode.Open, FileAccess.Read);
    //     await new OldStreamScanner().ScanStreamAsync(stream, testData.ScanParams, BufferSize);
    // }


    public static IEnumerable<TestDataSet> TestDataSets()
    {
        const int randomSeedValue = 5;
        const string filesDirectoryPath = "TestFiles";
        
        if (!Directory.Exists(filesDirectoryPath)) // Directory.CreateDirectory(filesDirectoryPath)
            Directory.CreateDirectory(filesDirectoryPath);

        var streamScanParams = new StreamScanParams(
            start: "STARTING",
            end: "ENDING",
            contains: "CONTAINS",
            template: @"(\+\d{1,2}\s?)?(\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}", // Regex для номера телефона (US)
            max: 65000,
            ignoreCase: false);


        // -------------------------------------------------------------------------------------------------------------
        const string testCase1BaseName = "TD1_200МБ_ИзВашегоПисьма_ОдинБольшойДиапазон";
        string testCase1FilePath = Path.Combine(filesDirectoryPath, "TD1_200MB_FromGmail_RangeFrom1000to50000+.txt");

        if (!File.Exists(testCase1FilePath)) // Закомментировать если необходимо чтобы создавался новый файл
        {
            TestDataProvider.GenerateTestDataFromVasheGmailPismo(
                matchedValue: "+1 (555) 123-4567",
                startMarker: streamScanParams.Start,
                endMarker: streamScanParams.End,
                containsMarker: streamScanParams.Contains,
                filePath: testCase1FilePath,
                fileSizeInMegaBytes: 200,
                rndSeed: randomSeedValue); // Передать null для Random.Shared
        }

        yield return new TestDataSet(testCase1BaseName, streamScanParams, testCase1FilePath);


        // -------------------------------------------------------------------------------------------------------------
        const string testCase2BaseName = "TD2_200МБ_ОченьМногоДиапазонов(Валидных<Невалидных)";
        string testCase2FilePath = Path.Combine(filesDirectoryPath, "TD2_200MB_LotsOfValidAndInvalidRanges.txt");

        if (!File.Exists(testCase2FilePath)) // Закомментировать если необходимо чтобы создавался новый файл
        {
            TestDataProvider.GenerateFullyRandomTestData(
                matchedValue: "+1 (555) 123-4567",
                startMarker: streamScanParams.Start,
                endMarker: streamScanParams.End,
                containsMarker: streamScanParams.Contains,
                filePath: testCase2FilePath,
                fileSizeInMegaBytes: 200,
                rndSeed: randomSeedValue); // Передать null для Random.Shared
        }

        yield return new TestDataSet(testCase2BaseName, streamScanParams, testCase2FilePath);
    }

    public record TestDataSet(string BaseTestCaseName, StreamScanParams ScanParams, string FilePath)
    {
        public MemoryStream InMemoryVersion { get; } = new(Encoding.ASCII.GetBytes(File.ReadAllText(FilePath)));

        public override string ToString() => $"{BaseTestCaseName}: ({InMemoryVersion.ToArray().Length} bytes)";
    }
}

// string start = "START";
// string end = "END";
// string contains = "CONTAINS";
// int max = 1000;
// bool ignoreCase = true;

// [DebuggerStepThrough]
