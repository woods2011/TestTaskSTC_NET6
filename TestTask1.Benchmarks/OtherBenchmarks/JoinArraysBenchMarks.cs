using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace TestTask1.Benchmarks.OtherBenchmarks;

[MemoryDiagnoser]
public class JoinArraysBenchMarks
{
    private readonly List<byte[]> _bytes = new();

    public JoinArraysBenchMarks()
    {
        for (int i = 0; i < 5; i++)
        {
            var bytes = new byte[4096];
            new Random(1).NextBytes(bytes);
            _bytes.Add(bytes);
        }
    }
    
    [Benchmark(Baseline = true)]
    public byte[] JoinByteArrays()
    {
        int totalLength = _bytes.Sum(bytes => bytes.Length);

        var result = new byte[totalLength];
        int currentOffset = 0;
        foreach (byte[] array in _bytes)
        {
            Buffer.BlockCopy(array, 0, result, currentOffset, array.Length);
            currentOffset += array.Length;
        }

        return result;
    }
    
    [Benchmark]
    public byte[] JoinByteArraysUseLinqSum()
    {
        int totalLength = 0;
        foreach (byte[] array in _bytes) totalLength += array.Length;

        var result = new byte[totalLength];
        int currentOffset = 0;
        foreach (byte[] array in _bytes)
        {
            Buffer.BlockCopy(array, 0, result, currentOffset, array.Length);
            currentOffset += array.Length;
        }

        return result;
    }
    
    [Benchmark]
    public byte[] JoinByteArrays_OnSequence()
    {
        ReadOnlySequence<byte> sequence = CreateSequence(_bytes);
        return sequence.ToArray();
    }

    
    private static ReadOnlySequence<T> CreateSequence<T>(IReadOnlyList<T[]> segments)
    {
        if (segments.Count == 0) return ReadOnlySequence<T>.Empty;

        ReadOnlyChunk<T> startSegment = new(segments[0]);
        ReadOnlyChunk<T> currentSegment = startSegment;

        for (int i = 1; i < segments.Count; i++) 
            currentSegment = currentSegment.Append(segments[i]);

        return new ReadOnlySequence<T>(startSegment, 0, currentSegment, currentSegment.Memory.Length);
    }
    
    private sealed class ReadOnlyChunk<T> : ReadOnlySequenceSegment<T>
    {
        public ReadOnlyChunk(ReadOnlyMemory<T> memory) => Memory = memory;

        public ReadOnlyChunk<T> Append(ReadOnlyMemory<T> memory)
        {
            var nextChunk = new ReadOnlyChunk<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = nextChunk;
            return nextChunk;
        }
    }
}