using System.Text;
using BenchmarkDotNet.Attributes;

namespace TestTask1.Benchmarks.OtherBenchmarks;

[MemoryDiagnoser]
public class AsciiCharArrayToByteArrayBenchmarks
{
    private readonly byte[] _bytes;

    public AsciiCharArrayToByteArrayBenchmarks()
    {
        _bytes = new byte[1024];
        new Random(1).NextBytes(_bytes);
    }

    
    [Benchmark(Baseline = true)]
    public char[] ConvertWithEncoding()
    {
        char[] chars = Encoding.ASCII.GetChars(_bytes);
        return chars;
    }

    [Benchmark]
    public char[] ConvertManual()
    {
        char[] chars = new char[_bytes.Length];

        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char) _bytes[i];

        return chars;
    }
    
    [Benchmark]
    public string ConvertToStringWithEncoding()
    {
        return Encoding.ASCII.GetString(_bytes);
    }
    
    [Benchmark]
    public string ConvertToStringManual()
    {
        char[] chars = new char[_bytes.Length];

        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char) _bytes[i];

        return new string(chars);
    }

    [Benchmark]
    public string ConvertToStringManualWithStackAlloc()
    {
        const int safeAllocCount = 1024 * 2 * 100;
        Span<char> chars = _bytes.Length <= safeAllocCount ? stackalloc char[_bytes.Length] : new char[_bytes.Length];

        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char) _bytes[i];

        return new string(chars);
    }
}