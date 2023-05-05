using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Attributes;
using TestTask2.Benchmarks.ObsoleteHtmlScannerCleanerImplementations;

namespace TestTask2.Benchmarks;

[MemoryDiagnoser]
public class HtmlStreamCleanerBenchmarks
{
    [Params(256, 1024, 4096)]
    public int BufferSize { get; set; }

    
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task NewStreamCleaner(TestDataSet testData)
    {
        testData.InputStream.Position = 0;
        await NewHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(testData.InputStream, new MemoryStream(), BufferSize);
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(TestDataSets))]
    public async Task OldStreamCleaner(TestDataSet testData)
    {
        testData.InputStream.Position = 0;
        await OldHtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(testData.InputStream, new MemoryStream(), BufferSize);
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