namespace TestTask1.StreamScanner;

/// <summary>
/// Базовый класс для сканеров потока. Реализует методы для параллельного сканирования потоков.
/// </summary>
public abstract class StreamScannerBase
{
    protected readonly Action<RangeMatch>? MatchNotifier;

    /// <param name="matchNotifier">Делегат для уведомления о найденных совпадениях.</param>
    protected StreamScannerBase(Action<RangeMatch>? matchNotifier = null) =>
        MatchNotifier = matchNotifier;

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
        int readBufferSize = 4096,
        CancellationToken token = default)
    {
        await Parallel.ForEachAsync(
            streams,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = token },
            async (stream, ct) => await ScanStreamAsync(stream, scanParams, readBufferSize, ct));
    }

    /// <summary>
    /// Асинхронно сканирует поток в соответствии с заданными параметрами сканирования.
    /// </summary>
    /// <param name="stream">Поток для сканирования.</param>
    /// <param name="scanParams">Параметры сканирования.</param>
    /// <param name="readBufferSize">Размер буфера для чтения потока (по умолчанию равен 4096 байт).</param>
    /// <param name="token">CancellationToken.</param>
    public abstract Task ScanStreamAsync(
        Stream stream,
        StreamScanParams scanParams,
        int readBufferSize = 4096,
        CancellationToken token = default);
}

/// <summary>
/// Структура для хранения информации о совпадениях найденых в процессе сканирования потока.
/// </summary>
public readonly record struct RangeMatch(IReadOnlyList<ScanMatch> ScanMatches, int RangeLength);

public readonly record struct ScanMatch(ReadOnlyMemory<char> MatchValue, int MatchIndexInRange);