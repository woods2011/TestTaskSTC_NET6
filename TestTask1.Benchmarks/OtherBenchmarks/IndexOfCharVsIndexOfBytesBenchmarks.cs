using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using TestTask1.Helpers;

namespace TestTask1.Benchmarks.OtherBenchmarks;

[MemoryDiagnoser]
public class IndexOfCharVsIndexOfBytesBenchmarks
{
    //[Params(25)]
    [Params(5, 25, 100, 200)]
    public int MatchSize { get; set; }

    private byte[] _bytes = null!;
    private char[] _chars = null!;

    private byte[] _bytesToFind = null!;
    private char[] _charsToFind = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _bytes = new byte[1024];
        new Random(1).NextBytes(_bytes);

        _bytesToFind = _bytes.AsSpan(_bytes.Length / 2, MatchSize).ToArray();

        _chars = Encoding.ASCII.GetChars(_bytes);
        _charsToFind = Encoding.ASCII.GetChars(_bytesToFind);
    }


    [Benchmark]
    public int SpanIndexOf_OnBytes()
    {
        return _bytes.AsSpan().IndexOf(_bytesToFind);
    }

    // [Benchmark]
    // public int SpanIndexOf_OnChars()
    // {
    //     return _chars.AsSpan().IndexOf(_charsToFind);
    // }
    //
    // [Benchmark]
    // public int SpanIndexOfOne_OnBytes()
    // {
    //     return _bytes.AsSpan().IndexOf(_bytesToFind[0]);
    // }


    [Benchmark]
    public int MyIndexOfIgnoreCase_OnBytes()
    {
        return _bytes.AsSpan().IndexOf(_bytesToFind, ignoreCase: true);
    }

    [Benchmark]
    public int MyIndexOf_OnBytes()
    {
        return _bytes.AsSpan().IndexOf(_bytesToFind, ignoreCase: false);
    }


    [Benchmark]
    public int CompareInfoIndexOf_OnChars()
    {
        return MemoryExtensions.IndexOf(_chars, _charsToFind, StringComparison.Ordinal);
    }

    //
    // [Benchmark]
    // public int CompareInfoIndexOf_OnChars_IgnoreCase()
    // {
    //     return MemoryExtensions.IndexOf(_chars, _charsToFind, StringComparison.OrdinalIgnoreCase);
    // }


    // [Benchmark]
    // public int ArrayIndexOfOne_OnBytes()
    // {
    //     return Array.IndexOf(_bytes, _bytesToFind[0]);
    // }

    // [Benchmark]
    // public int ArrayIndexOfOne_OnChars()
    // {
    //     return Array.IndexOf(_chars, _charsToFind[0]);
    // }
}