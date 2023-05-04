using System.Collections.Concurrent;
using System.Text;
using TestTask1.StreamScanner;
using static System.Environment;

namespace TestTask1;

public sealed class MatchesReporterSubscriber : IAsyncDisposable
{
    private bool _disposed = false;

    private readonly ConcurrentQueue<RangeMatch> _rangeMatchesQueue = new();
    private int _totalNumberOfRangeMatches = 0;
    private int _totalNumberOfScanMatches = 0;

    private readonly StreamWriter _streamWriter;
    private readonly string _filePath;

    private readonly PeriodicTimer _timer;
    private readonly Task _timerTask;

    private readonly int _flushThreshold = 50;


    public MatchesReporterSubscriber(string filePath)
    {
        _filePath = filePath;

        string? directoryPath = Path.GetDirectoryName(_filePath);
        if (directoryPath is not null && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        _streamWriter = new StreamWriter(
            _filePath,
            Encoding.UTF8,
            new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write });

        _timer = new PeriodicTimer(TimeSpan.FromMicroseconds(1000));
        _timerTask = Task.Run(TimerFlushHandler);
    }

    public void MatchHandler(RangeMatch rangeMatch) => _rangeMatchesQueue.Enqueue(rangeMatch);

    private async Task TimerFlushHandler()
    {
        while (await _timer.WaitForNextTickAsync())
            if (_rangeMatchesQueue.Count >= _flushThreshold && !_disposed)
                await WriteResults();
    }

    private async Task WriteResults()
    {
        var rangeMatches = new List<RangeMatch>();
        while (_rangeMatchesQueue.TryDequeue(out RangeMatch rangeMatch))
            rangeMatches.Add(rangeMatch);

        // Console.WriteLine($"Найдено новых совпадений: {rangeMatches.Sum(rangeMatch => rangeMatch.ScanMatches.Count)}" +
        //                   $"В {rangeMatches.Count} валидных диапазонах");

        foreach (var (scanMatches, rangeLength) in rangeMatches)
        {
            Interlocked.Increment(ref _totalNumberOfRangeMatches);
            Interlocked.Add(ref _totalNumberOfScanMatches, scanMatches.Count);

            await _streamWriter.WriteLineAsync(
                $"Найден подходящий по критериям диапазон!{NewLine}" +
                $"\tДлина диапазона: {rangeLength}{NewLine}" +
                $"\tЧисло совпадений: {scanMatches.Count}");

            for (var i = 0; i < scanMatches.Count; i++)
            {
                ScanMatch match = scanMatches[i];

                await _streamWriter.WriteLineAsync(
                    $"\t\tСовпадение {i + 1}: {match.MatchValue}; На позиции диапазона: {match.MatchIndexInRange}");
            }
            
            await _streamWriter.WriteLineAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _timer.Dispose();
        await _timerTask;

        await WriteResults();

        string totalNumOfMatches = $"{NewLine}" +
                                   $"Суммарное число валидных интервалов: {_totalNumberOfRangeMatches}{NewLine}" +
                                   $"Суммарное число совпадений во всех интервалах: {_totalNumberOfScanMatches}";

        await _streamWriter.WriteAsync(totalNumOfMatches);
        Console.WriteLine(totalNumOfMatches);

        Console.WriteLine($"{NewLine}" +
                          $"Подробный результат сканирования записан в файл:{NewLine}" +
                          $"\t{Path.GetFullPath(_filePath)}");

        await _streamWriter.DisposeAsync();
        
        _disposed = true;
    }

    private readonly string NewLine = Environment.NewLine;
}