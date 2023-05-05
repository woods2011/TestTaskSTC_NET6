namespace TestTask1.Helpers;

public static class MemoryHelpers
{
    public static Memory<byte> JoinMemory(
        ReadOnlyMemory<byte> first,
        IReadOnlyCollection<ReadOnlyMemory<byte>> parts,
        ReadOnlyMemory<byte> last)
    {
        int totalLength = first.Length + last.Length;
        foreach (var part in parts) totalLength += part.Length;

        var resultArray = new byte[totalLength];
        int currentOffset = first.Length;

        first.CopyTo(resultArray.AsMemory(0, first.Length));

        foreach (ReadOnlyMemory<byte> memory in parts)
        {
            memory.Span.CopyTo(resultArray.AsSpan(currentOffset, memory.Length));
            currentOffset += memory.Length;
        }

        last.CopyTo(resultArray.AsMemory(currentOffset, last.Length));

        return resultArray;
    }

    
    public static Memory<byte> JoinMemory(ReadOnlyMemory<byte> part1, ReadOnlyMemory<byte> part2)
    {
        var resultArray = new byte[part1.Length + part2.Length];

        part1.CopyTo(resultArray.AsMemory(0, part1.Length));
        part2.CopyTo(resultArray.AsMemory(part1.Length, part2.Length));

        return resultArray;
    }
    

    public static Memory<byte> JoinMemory(IReadOnlyCollection<ReadOnlyMemory<byte>> parts)
    {
        int totalLength = 0;
        foreach (var part in parts) totalLength += part.Length;

        var resultArray = new byte[totalLength];
        int currentOffset = 0;

        foreach (ReadOnlyMemory<byte> memory in parts)
        {
            memory.Span.CopyTo(resultArray.AsSpan(currentOffset, memory.Length));
            currentOffset += memory.Length;
        }

        return resultArray;
    }
}