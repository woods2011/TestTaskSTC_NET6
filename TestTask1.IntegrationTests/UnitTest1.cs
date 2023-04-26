using System.Text;

namespace TestTask1.IntegrationTests;

public class DefaultStreamScannerTests
{
    [Fact]
    public void TestWithDifferentStreamTypes()
    {
        string start = "START";
        string end = "END";
        string contains = "CONTAINS";
        string template = @"v\d+";
        int max = 1000;

        string[] filePaths = { "test.txt" };

        string testData = "some data START some data v10 CONTAINS some data v11 END some data";
        byte[] testDataBytes = Encoding.ASCII.GetBytes(testData);
        var steamsCount = 10;
        var memoryStreams = Enumerable.Range(0, steamsCount).Select(_ => new MemoryStream(testDataBytes));


        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFile("MatchLog.txt"));
        var loggerSubscriber = new LoggerSubscriber(loggerFactory.CreateLogger<LoggerSubscriber>());

        await new DefaultStreamScanner(loggerSubscriber.MatchHandler).ScanStreamsInParallelAsync(
            streams: memoryStreams,
            new StreamScanParams(start, end, contains, template, max, ignoreCase: false),
            bufferSize: 1024 * 4,
            cancellationToken);
    }
}