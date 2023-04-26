using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Attributes;
using TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;
using TestTask1.StreamScanner;

namespace TestTask1.Benchmarks;

[MemoryDiagnoser]
public class StreamScannerBenchmarks
{
    [Params(256, 1024)]
    public int BufferSize { get; set; }


    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(TestDataSets))]
    [DebuggerStepThrough]
    public async Task ScannerOnSpans(TestDataSet testData)
    {
        testData.Stream.Position = 0;
        await new DefaultStreamScanner().ScanStreamAsync(testData.Stream, testData.ScanParams, BufferSize);
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task ScannerOnSpansWithStreamReader(TestDataSet testData)
    {
        testData.Stream.Position = 0;
        await new StreamScannerWithStreamReader().ScanStreamAsync(testData.Stream, testData.ScanParams, BufferSize);
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task ScannerOnStringBuilder(TestDataSet testData)
    {
        testData.Stream.Position = 0;
        await new StreamScannerOnStringBuilder().ScanStreamAsync(testData.Stream, testData.ScanParams, BufferSize);
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task ScannerOnStringBuilderWithIndexOf(TestDataSet testData)
    {
        testData.Stream.Position = 0;
        await new StreamScannerOnStringBuilderWithIndexOfExtension()
            .ScanStreamAsync(testData.Stream, testData.ScanParams, BufferSize);
    }


    public static IEnumerable<TestDataSet> TestDataSets()
    {
        var random = new Random(1);

        string start = "START";
        string end = "END";
        string contains = "CONTAINS";
        int max = 1000;
        bool ignoreCase = true;
        

        #region TestData для Простого регулярного выражения

        string simpleTemplate = @"\d+"; // Любая последовательность цифр
        string[] simpleTemplateMatchedContent =
            new[] { 10, 11, 557, 1234, 12345, 123456, 1234567, 12345678, 12345678 }.Select(i => i.ToString()).ToArray();

        // (Малый и Часто встречающийся Диапазон)
        var test1Name = "TD1_МалыйЧастыйДиапазон_ПростоеРегВыр";
        string test1String = ShortRangeDataFactory(
            () => GetRandomElement(simpleTemplateMatchedContent, random), start, end, contains).Repeat(16);
        yield return CreateTestData(start, end, contains, simpleTemplate, max, ignoreCase, test1String, test1Name);

        // (Большой Диапазон)
        var test2Name = "TD2_БольшойДиапазон_ПростоеРегВыр";
        string test2String = LongRangeDataFactory(
            () => GetRandomElement(simpleTemplateMatchedContent, random), start, end, contains).Repeat(5);
        yield return CreateTestData(start, end, contains, simpleTemplate, max, ignoreCase, test2String, test2Name);

        #endregion


        #region TestData для Сложного регулярного выражения

        string hardTemplate = @"\b(?:\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?\b"; // Числа с плавающей точкой
        string[] hardTemplateMatchedContent =
            { "1", "1.1", "1,1", "1,111", "1,111,111", "1,111,111.1", "1,111,111.111" };

        // (Малый и Часто встречающийся Диапазон)
        var test3Name = "TD3_МалыйЧастыйДиапазон_СложноеРегВыр";
        string test3String = ShortRangeDataFactory(
            () => GetRandomElement(hardTemplateMatchedContent, random), start, end, contains).Repeat(15);
        yield return CreateTestData(start, end, contains, hardTemplate, max, ignoreCase, test3String, test3Name);

        // (Большой Диапазон)
        var test4Name = "TD4_БольшойДиапазон_СложноеРегВыр";
        string test4String = LongRangeDataFactory(
            () => GetRandomElement(hardTemplateMatchedContent, random), start, end, contains).Repeat(5);
        yield return CreateTestData(start, end, contains, hardTemplate, max, ignoreCase, test4String, test4Name);

        #endregion


        static TestDataSet CreateTestData(
            string start,
            string end,
            string contains,
            string template,
            int max,
            bool ignoreCase,
            string testDataString,
            string testCaseName)
        {
            byte[] testBytes = Encoding.ASCII.GetBytes(testDataString);

            return new TestDataSet(
                new MemoryStream(testBytes),
                new StreamScanParams(start, end, contains, template, max, ignoreCase),
                testCaseName);
        }

        static string ShortRangeDataFactory(Func<string> matchFactory, string start, string end, string contains) =>
            $"someData {start} someData {matchFactory()} someData someData {matchFactory()} {end} someData  " +
            $"someData data someData someData someData someData someData someData someData someData someData" +
            $"someData {start} someData {matchFactory()} {contains} someData {matchFactory()} {end} someData" +
            $"someData {start} someData {matchFactory()} someData someData {matchFactory()} {end}  someData ";

        static string LongRangeDataFactory(Func<string> matchFactory, string start, string end, string contains) =>
            $"someData {start} someData {contains} {matchFactory()} someData {matchFactory()} {end} someData" +
            $"{start}  data someData someData someData someData someData someData someData someData someData" +
            $"someData data someData someData someData someData someData someData someData someData someData" +
            $"someData data someData someData someData someData someData someData someData someData someData" +
            $"someData data someData someData {matchFactory()} someData someData someData someData someData " +
            $"someData data someData someData someData someData someData someData someData someData someData" +
            $"someData data someData someData {matchFactory()} someData someData someData someData someData " +
            $"someData data someData someData someData someData someData someData someData someData someData" +
            $"someData data someData someData {matchFactory()} someData someData someData someData someData " +
            $"someData data someData someData someData someData someData someData someData someData    {end}" +
            $"someData {start} someData someData   {matchFactory()} someData {matchFactory()} {end} s{start}";
        
        static string GetRandomElement(IReadOnlyList<string> data, Random random) => data[random.Next(0, data.Count)];
    }

    public record TestDataSet(MemoryStream Stream, StreamScanParams ScanParams, string BaseName)
    {
        public override string ToString() => $"{BaseName}: ({Stream.ToArray().Length} bytes)";
    }
}

internal static class StringHelpers
{
    public static string Repeat(this string value, int count) =>
        new StringBuilder(count * value.Length).Insert(0, value, count).ToString();
}