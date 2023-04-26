using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TestTask1.StreamScanner;

namespace TestTask1;

public class LoggerSubscriber
{
    private readonly ILogger<LoggerSubscriber> _logger;

    public LoggerSubscriber(ILogger<LoggerSubscriber> logger) =>
        _logger = logger;

    public void MatchHandler(ScanMatch scanMatch) => _logger.LogInformation(
        "Match found: {MatchValue} at Range index: {MatchIndex}", scanMatch.MatchValue, scanMatch.MatchIndexInRange);
}