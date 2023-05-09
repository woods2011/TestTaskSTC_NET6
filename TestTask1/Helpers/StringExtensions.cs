namespace TestTask1.Helpers;

public static class StringExtensions
{
    public static bool ContainsAny(this ReadOnlySpan<char> span, params char[] anyOf) =>
        span.IndexOfAny(anyOf) != -1;

    public static bool ContainsAny(this string input, params char[] anyOf) => input.AsSpan().ContainsAny(anyOf);

    public static int IndexOf(
        this ReadOnlySpan<char> span,
        ReadOnlySpan<char> value,
        int startIndex,
        StringComparison comparisonType)
    {
        int indexInSlice = span.Slice(startIndex).IndexOf(value, comparisonType);

        if (indexInSlice == -1) return -1;

        return startIndex + indexInSlice;
    }
    
    public static int IndexOf(
        this ReadOnlySpan<byte> span,
        byte value,
        int startIndex)
    {
        int indexInSlice = span.Slice(startIndex).IndexOf(value);

        if (indexInSlice == -1) return -1;

        return startIndex + indexInSlice;
    }
    
    
    public static Memory<T> CopyToAndFitDest<T>(this ReadOnlySpan<T> source, Memory<T> destination)
    {
        source.CopyTo(destination.Span);
        return destination[..source.Length];
    }

    public static Memory<T> CopyToAndFitDest<T>(this ReadOnlyMemory<T> source, Memory<T> destination) =>
        CopyToAndFitDest(source.Span, destination);
}