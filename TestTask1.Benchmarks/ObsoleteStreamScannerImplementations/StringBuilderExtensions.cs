using System.Text;

namespace TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

public static class StringBuilderExtensions
{
    public static int IndexOf(this StringBuilder sb, string value, bool ignoreCase = false)
        => IndexOf(sb, value, 0, ignoreCase);
    
    public static int IndexOf(this StringBuilder sb, string value, int start, bool ignoreCase = false)
    {
        if (value == string.Empty) return start;
        
        for (int index = start; index < sb.Length - (value.Length - 1); index++)
        {
            if (!CharsAreEqual(sb[index], value[0], ignoreCase)) continue;

            var isFound = true;
            for (int offset = 1; offset < value.Length; offset++)
                if (!CharsAreEqual(sb[index + offset], value[offset], ignoreCase))
                {
                    isFound = false;
                    break;
                }

            if (isFound) return index;
        }

        return -1;

        static bool CharsAreEqual(char a, char b, bool ignoreCase) =>
            ignoreCase ? char.ToUpperInvariant(a) == char.ToUpperInvariant(b) : a == b;
    }
}