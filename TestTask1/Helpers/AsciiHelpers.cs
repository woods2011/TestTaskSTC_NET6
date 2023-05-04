using System.Runtime.CompilerServices;

namespace TestTask1.Helpers;

public static class AsciiHelpers
{
    public static string ConvertToString(ReadOnlySpan<byte> bytes)
    {
        const int safeAllocCount = 1024 * 75 * 2; // 150kb 

        Span<char> chars = bytes.Length <= safeAllocCount ? stackalloc char[bytes.Length] : new char[bytes.Length];

        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char) bytes[i];

        return new string(chars);
    }

    public static char[] ConvertToChars(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length];

        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char) bytes[i];

        return chars;
    }

    public static Span<char> ConvertToChars(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        for (var i = 0; i < destination.Length; i++)
            destination[i] = (char) bytes[i];

        return destination;
    }

    private static class AsciiInvertCaseByteTable
    {
        public static readonly byte[] Table = new byte[256]; // IReadOnlyList<byte>

        static AsciiInvertCaseByteTable()
        {
            for (int i = 0; i < 256; i++)
                if (i is (>= 65 and <= 90) or (>= 97 and <= 122))
                    Table[i] = (byte) (i ^ 0x20);
                else
                    Table[i] = 0;
        }
    }

    public static int IndexOf(this Span<byte> source, ReadOnlySpan<byte> pattern, bool ignoreCase) => 
        IndexOf((ReadOnlySpan<byte>) source, pattern, ignoreCase);
    
    public static int IndexOf(this ReadOnlySpan<byte> source, ReadOnlySpan<byte> pattern, bool ignoreCase)
    {
        if (!ignoreCase) return source.IndexOf(pattern);

        byte invertedFirstByte = AsciiInvertCaseByteTable.Table[pattern[0]];
        bool firstByteIsLetter = invertedFirstByte != 0;
        return firstByteIsLetter
            ? IndexOfCaseInSensitiveFirstByteIsLetter(source, pattern)
            : IndexOfCaseInSensitiveFirstByteIsNotLetter(source, pattern);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int IndexOfCaseInSensitiveFirstByteIsLetter(ReadOnlySpan<byte> source, ReadOnlySpan<byte> pattern)
    {
        byte firstByte = pattern[0];
        byte invertedFirstByte = AsciiInvertCaseByteTable.Table[firstByte];
        var currentOffset = 0;

        while (true)
        {
            int indexOfFirstByte = source.IndexOfAny(firstByte, invertedFirstByte);

            if (indexOfFirstByte is -1)
                return -1;

            source = source.Slice(indexOfFirstByte + 1);
            if (pattern.Length > source.Length + 1)
                return -1;

            currentOffset += indexOfFirstByte;

            var isMatchFound = true;

            for (var i = 1; i < pattern.Length; i++)
            {
                if (source[i - 1] == pattern[i]) continue;

                byte invertedByte = AsciiInvertCaseByteTable.Table[pattern[i]];

                if (source[i - 1] == invertedByte) continue;

                isMatchFound = false;
                break;
            }

            if (isMatchFound) return currentOffset;
            currentOffset++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static int IndexOfCaseInSensitiveFirstByteIsNotLetter(ReadOnlySpan<byte> source, ReadOnlySpan<byte> pattern)
    {
        byte firstByte = pattern[0];
        var currentOffset = 0;

        while (true)
        {
            int indexOfFirstByte = source.IndexOf(firstByte);

            if (indexOfFirstByte is -1)
                return -1;

            source = source.Slice(indexOfFirstByte + 1);
            if (pattern.Length > source.Length + 1)
                return -1;

            currentOffset += indexOfFirstByte;

            var isMatchFound = true;

            for (var i = 1; i < pattern.Length; i++)
            {
                if (source[i - 1] == pattern[i]) continue;

                byte invertedByte = AsciiInvertCaseByteTable.Table[pattern[i]];

                if (source[i - 1] == invertedByte) continue;

                isMatchFound = false;
                break;
            }

            if (isMatchFound) return currentOffset;
            currentOffset++;
        }
    }

    // [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    // public static int IndexOfCaseSensitive(this ReadOnlySpan<byte> source, ReadOnlySpan<byte> pattern)
    // {
    //     byte firstByte = pattern[0];
    //     var currentOffset = 0;
    //
    //     while (true)
    //     {
    //         int indexOfFirstByte = source.IndexOf(firstByte);
    //         if (indexOfFirstByte is -1)
    //             return -1;
    //
    //         source = source.Slice(indexOfFirstByte + 1);
    //         if (pattern.Length > source.Length + 1)
    //             return -1;
    //
    //         currentOffset += indexOfFirstByte;
    //
    //         var isMatchFound = true;
    //
    //         for (var i = 1; i < pattern.Length; i++)
    //         {
    //             if (source[i - 1] == pattern[i]) continue;
    //
    //             isMatchFound = false;
    //             break;
    //         }
    //
    //         if (isMatchFound) return currentOffset;
    //         currentOffset++;
    //     }
    // }
}