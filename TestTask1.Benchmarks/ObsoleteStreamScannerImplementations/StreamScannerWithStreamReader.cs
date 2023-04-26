using System.Text;
using System.Text.RegularExpressions;
using TestTask1.Helpers;
using TestTask1.StreamScanner;

namespace TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

public class StreamScannerWithStreamReader : StreamScannerBase
{
    public StreamScannerWithStreamReader(Action<ScanMatch>? matchNotifier = null) : base(matchNotifier) { }
    
    public override async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int bufferSize = 1024 * 4,
        CancellationToken token = default)
    {
        var readBuffer = new char[Encoding.ASCII.GetMaxCharCount(bufferSize)];
        int charsRead;
        var streamReader = new StreamReader(
            stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize);

        StringComparison comparisonType = scanParams.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var partBuffer = ReadOnlyMemory<char>.Empty;

        while ((charsRead = await streamReader.ReadAsync(readBuffer, token)) > 0)
        {
            partBuffer = String.Concat(partBuffer.Span, readBuffer.AsSpan(0, charsRead)).AsMemory();
            partBuffer =  ScanPart(scanParams, partBuffer, comparisonType, MatchNotifier);
        }

        static ReadOnlyMemory<char> ScanPart(
            StreamScanParams scanParams,
            ReadOnlyMemory<char> partBuffer,
            StringComparison comparisonType,
            Action<ScanMatch> matchNotifier)
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

                foreach (Match match in scanParams.Regex.Matches(rangeData.ToString()))
                    matchNotifier(new ScanMatch(match.Value, rangeData.Length, match.Index));

                return true;
            }
        }
    }
}