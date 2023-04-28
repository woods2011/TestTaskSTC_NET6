using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TestTask1.StreamScanner;

namespace TestTask1;

public class LoggerSubscriber : IDisposable
{
    private readonly ILogger<LoggerSubscriber> _logger;
    
    private int _totalNumOfMatches;

    public LoggerSubscriber(ILogger<LoggerSubscriber> logger) =>
        _logger = logger;

    public void MatchHandler(ScanMatch scanMatch)
    {
        Interlocked.Increment(ref _totalNumOfMatches);
        _logger.LogInformation(
            "Match found: {MatchValue} at Range index: {MatchIndex}", scanMatch.MatchValue,
            scanMatch.MatchIndexInRange);
    }

    public void Dispose() => _logger.LogInformation("Total number of matches: {TotalMatchCount}", _totalNumOfMatches);
}