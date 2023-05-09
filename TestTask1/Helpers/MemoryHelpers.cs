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

        return JoinMemory(first, parts, last, resultArray);
    }

    public static Memory<byte> JoinMemory(
        ReadOnlyMemory<byte> first,
        IReadOnlyCollection<ReadOnlyMemory<byte>> parts,
        ReadOnlyMemory<byte> last,
        Memory<byte> destination)
    {
        int currentOffset = first.Length;

        first.CopyTo(destination[..first.Length]);

        foreach (ReadOnlyMemory<byte> memory in parts)
        {
            memory.CopyTo(destination.Slice(currentOffset, memory.Length));
            currentOffset += memory.Length;
        }

        last.CopyTo(destination.Slice(currentOffset, last.Length));
        currentOffset += last.Length;

        return destination[..currentOffset];
    }

    public static Memory<byte> JoinMemory(ReadOnlyMemory<byte> part1, ReadOnlyMemory<byte> part2)
    {
        var resultArray = new byte[part1.Length + part2.Length];
        return JoinMemory(part1, part2, resultArray);
    }

    public static Memory<byte> JoinMemory(
        ReadOnlyMemory<byte> part1,
        ReadOnlyMemory<byte> part2,
        Memory<byte> destination)
    {
        part1.CopyTo(destination[..part1.Length]);
        part2.CopyTo(destination.Slice(part1.Length, part2.Length));

        return destination[(part1.Length + part2.Length)..];
    }


    public static Memory<byte> JoinMemory(IReadOnlyCollection<ReadOnlyMemory<byte>> parts)
    {
        int totalLength = 0;
        foreach (var part in parts) totalLength += part.Length;

        var resultArray = new byte[totalLength];

        return JoinMemory(parts, resultArray);
    }
    
    public static Memory<byte> JoinMemory(IReadOnlyCollection<ReadOnlyMemory<byte>> parts, Memory<byte> destination)
    {
        int currentOffset = 0;

        foreach (ReadOnlyMemory<byte> memory in parts)
        {
            memory.CopyTo(destination.Slice(currentOffset, memory.Length));
            currentOffset += memory.Length;
        }
        
        return destination[..currentOffset];
    }

}