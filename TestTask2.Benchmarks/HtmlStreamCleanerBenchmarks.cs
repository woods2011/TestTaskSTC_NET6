using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Attributes;
using TestTask2.Benchmarks.ObsoleteHtmlScannerCleanerImplementations;

namespace TestTask2.Benchmarks;

[MemoryDiagnoser]
public class HtmlStreamCleanerBenchmarks
{
    [Params(256, 1024)]
    public int BufferSize { get; set; }


    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(TestDataSets))]
    [DebuggerStepThrough]
    public async Task StreamCleanerWithoutStreamReader(TestDataSet testData)
    {
        testData.InputStream.Position = 0;
        var outputStream = new MemoryStream();

        await HtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(testData.InputStream, outputStream, BufferSize);
    }

    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task StreamCleanerWithStreamReader(TestDataSet testData)
    {
        testData.InputStream.Position = 0;
        var outputStream = new MemoryStream();

        await HtmlStreamCleanerWithStreamReader.RemoveHtmlTagsFromStreamAsync(
            testData.InputStream, outputStream, BufferSize);
    }


    public static IEnumerable<TestDataSet> TestDataSets()
    {
        byte[] htmlContentBytes = File.ReadAllBytes("Files/test.html");
        var memoryStream = new MemoryStream(htmlContentBytes);

        yield return new TestDataSet(memoryStream, "test.html");
    }

    public record TestDataSet(MemoryStream InputStream, string BaseName)
    {
        public override string ToString() => $"{BaseName}: ({InputStream.ToArray().Length} bytes)";
    }
}

internal static class StringHelpers
{
    public static string Repeat(this string value, int count) =>
        new StringBuilder(count * value.Length).Insert(0, value, count).ToString();
}