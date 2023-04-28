using System.Text;
using System.Text.RegularExpressions;
using TestTask1.Helpers;

namespace TestTask1.StreamScanner;

/// <summary>
/// Структура для хранения информации о совпадениях найденых в процессе сканирования потока.
/// </summary>
public record struct ScanMatch(string MatchValue, int RangeLength, int MatchIndexInRange);

/// <summary>
/// Базовый класс для сканеров потока. Реализует методы для параллельного сканирования потоков.
/// </summary>
public abstract class StreamScannerBase
{
    protected readonly Action<ScanMatch> MatchNotifier;

    /// <param name="matchNotifier">Делегат для уведомления о найденных совпадениях.</param>
    protected StreamScannerBase(Action<ScanMatch>? matchNotifier = null) =>
        MatchNotifier = matchNotifier ?? (_ => { });

    /// <summary>
    /// Сканирует все потоки из коллекции параллельно и асинхронно в соответствии с заданными параметрами сканирования.
    /// </summary>
    /// <param name="streams">Коллекция потоков для сканирования.</param>
    /// <param name="scanParams">Параметры сканирования.</param>
    /// <param name="bufferSize">Размер буфера для чтения потока (по умолчанию равен 1024 байт).</param>
    /// <param name="token">CancellationToken.</param>
    public async Task ScanStreamsInParallelAsync(
        IEnumerable<Stream> streams,
        StreamScanParams scanParams,
        int bufferSize = 4096,
        CancellationToken token = default)
    {
        await Parallel.ForEachAsync(
            streams,
            token,
            async (stream, ct) => await ScanStreamAsync(stream, scanParams, bufferSize, ct));
    }

    /// <inheritdoc cref="ScanStreamsInParallelAsync(IEnumerable{Stream}, StreamScanParams, int, CancellationToken)"/>
    /// <param name="maxDegreeOfParallelism">Максимальное число параллельных задач.</param>
    public async Task ScanStreamsInParallelAsync(
        IEnumerable<Stream> streams,
        StreamScanParams scanParams,
        int maxDegreeOfParallelism,
        int bufferSize = 4096,
        CancellationToken token = default)
    {
        await Parallel.ForEachAsync(
            streams,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = token },
            async (stream, ct) => await ScanStreamAsync(stream, scanParams, bufferSize, ct));
    }

    /// <summary>
    /// Асинхронно сканирует поток в соответствии с заданными параметрами сканирования.
    /// </summary>
    /// <param name="stream">Поток для сканирования.</param>
    /// <param name="scanParams">Параметры сканирования.</param>
    /// <param name="bufferSize">Размер буфера для чтения потока (по умолчанию равен 4096 байт).</param>
    /// <param name="token">CancellationToken.</param>
    public abstract Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int bufferSize = 4096,
        CancellationToken token = default);
}

public class DefaultStreamScanner : StreamScannerBase
{
    /// <inheritdoc/>
    public DefaultStreamScanner(Action<ScanMatch>? matchNotifier = null) : base(matchNotifier) { }

    /// <inheritdoc/>
    public override async Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int bufferSize = 4096,
        CancellationToken token = default)
    {
        Encoding encoding = Encoding.ASCII; // т.к. кодировка 1 байт не нужно хранить encoding.GetDecoder();
        var readBuffer = new byte[bufferSize];
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