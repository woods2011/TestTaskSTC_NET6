using System.Text;
using System.Text.RegularExpressions;
using TestTask1.Helpers;
using TestTask1.StreamScanner;

namespace TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

public class AncientDefaultStreamScanner : StreamScannerBase
{
    /// <inheritdoc/>
    public AncientDefaultStreamScanner(Action<RangeMatch>? matchNotifier = null) : base(matchNotifier) { }

    /// <inheritdoc/>
    public override async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int readBufferSize = 4096,
        CancellationToken token = default)
    {
        Encoding encoding = Encoding.ASCII; // т.к. кодировка 1 байт не нужно хранить encoding.GetDecoder();
        var readBuffer = new byte[readBufferSize];
        int bytesRead;

        StringComparison comparisonType = scanParams.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var partBuffer = ReadOnlyMemory<char>.Empty;

        while ((bytesRead = await stream.ReadAsync(readBuffer, token)) > 0)
        {
            partBuffer = String.Concat(partBuffer.Span, encoding.GetString(readBuffer, 0, bytesRead)).AsMemory();
            partBuffer = ScanPart(scanParams, partBuffer, comparisonType, MatchNotifier);
        }

        static ReadOnlyMemory<char> ScanPart(
            StreamScanParams scanParams,
            ReadOnlyMemory<char> partBuffer,
            StringComparison comparisonType,
            Action<RangeMatch>? matchNotifier)
        {
            int startIndex = partBuffer.Span.IndexOf(scanParams.Start, comparisonType);
            if (startIndex == -1)
            {
                int numOfSymbolsToRemove = partBuffer.Length - scanParams.Start.Length; // на самом деле еще +1
                return partBuffer.Slice(Math.Max(0, numOfSymbolsToRemove));
            }


            partBuffer = partBuffer.Slice(startIndex);

            int endIndex = partBuffer.Span.IndexOf(scanParams.End, scanParams.Start.Length, comparisonType);
            if (endIndex == -1) return partBuffer;


            int rangeLength = endIndex + scanParams.End.Length;
            bool isRangeValid = CheckRangeAgainstAllCriteria(partBuffer.Slice(0, rangeLength).Span);

            var remainingBuffer = partBuffer.Slice(isRangeValid ? rangeLength : 1); //
            return ScanPart(scanParams, remainingBuffer, comparisonType, matchNotifier);


            // ---------------------------------------------------------------------------------------------------------
            bool CheckRangeAgainstAllCriteria(ReadOnlySpan<char> rangeData)
            {
                if (rangeData.Length > scanParams.Max) return false;

                bool areForbiddenCharsPresent = rangeData.ContainsAny('\x00', '\x0d', '\x0a'); // Возможно стоит вынести
                if (areForbiddenCharsPresent) return false;

                bool isContainsPresent = rangeData.Contains(scanParams.Contains, comparisonType);
                if (!isContainsPresent) return false;
                
                var rangeAsString = rangeData.ToString();
                
                var matches = new List<ScanMatch>();
                foreach (ValueMatch match in scanParams.Regex.EnumerateMatches(rangeAsString))
                    matches.Add(new ScanMatch(rangeAsString.AsMemory(match.Index, match.Length), match.Index));

                matchNotifier?.Invoke(new RangeMatch(matches, rangeAsString.Length));

                return true;
            }
        }
    }
}