using System.Text;
using System.Text.RegularExpressions;
using TestTask1.Helpers;
using TestTask1.StreamScanner;

namespace TestTask1.Benchmarks.ObsoleteStreamScannerImplementations;

public class StreamScannerOnStringBuilderWithIndexOfExtension : StreamScannerBase
{
    public StreamScannerOnStringBuilderWithIndexOfExtension(Action<ScanMatch>? matchNotifier = null) 
        : base(matchNotifier) { }
    
    public override async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int bufferSize = 1024 * 4,
        CancellationToken token = default)
    {
        StringBuilder stringBuffer = new();
        var buffer = new byte[bufferSize];
        int bytesRead;

        StringComparison comparisonType = scanParams.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        while ((bytesRead = await stream.ReadAsync(buffer, token)) > 0)
        {
            stringBuffer.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            ScanPart(scanParams, stringBuffer, comparisonType, MatchNotifier);
        }

        static void ScanPart(
            StreamScanParams scanParams,
            StringBuilder stringBuffer,
            StringComparison comparisonType,
            Action<ScanMatch> matchNotifier)
        {
            int startIndex = stringBuffer.IndexOf(scanParams.Start, scanParams.IgnoreCase);
            if (startIndex == -1)
            {
                int numOfSymbolsToRemove = stringBuffer.Length - scanParams.Start.Length; // на самом деле еще +1
                stringBuffer.Remove(0, Math.Max(0, numOfSymbolsToRemove));
                return;
            }

            stringBuffer.Remove(0, startIndex);
            
            int endIndex = stringBuffer.IndexOf(scanParams.End, scanParams.Start.Length, scanParams.IgnoreCase); //
            if (endIndex == -1) return;

            
            int rangeLength = endIndex + scanParams.End.Length;
            bool isRangeValid = CheckRangeAgainstAllCriteria(stringBuffer.ToString(0, rangeLength));

            var remainingBuffer = stringBuffer.Remove(0, isRangeValid ? rangeLength : 1); //
            ScanPart(scanParams, remainingBuffer, comparisonType, matchNotifier);
            
            
            // ---------------------------------------------------------------------------------------------------------
            bool CheckRangeAgainstAllCriteria(string rangeData)
            {
                if (rangeData.Length > scanParams.Max) return false;

                bool areForbiddenCharsPresent = rangeData.ContainsAny('\x00', '\x0d', '\x0a'); // Возможно стоит вынести
                if (areForbiddenCharsPresent) return false;

                bool isContainsPresent = rangeData.Contains(scanParams.Contains, comparisonType);
                if (!isContainsPresent) return false;

                foreach (Match match in scanParams.Regex.Matches(rangeData))
                    matchNotifier(new ScanMatch(match.Value, rangeData.Length, match.Index));

                return true;
            }
        }
    }
}