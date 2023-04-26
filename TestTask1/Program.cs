using System.Text;
using Microsoft.Extensions.Logging;
using TestTask1;
using TestTask1.StreamScanner;

var cancellationToken = CancellationToken.None; // Для наглядности

// Пример ввода для стандартных данных: Start: start, end: End, contains: Contains, template: \d+, max: 1000
var fallbackData = "some data Start some data 10 Contains some data 11 End some data";

string directoryPath = "TestFiles";
string[] defaultFilePaths =
    { $"{directoryPath}/test1.txt", $"{directoryPath}/test2.txt", $"{directoryPath}/test3.txt" };

InitFilesWithFallBackDataIfAllEmpty();

StreamScanParams scanParams = HandleInput();

IEnumerable<Stream> streams = defaultFilePaths
    .Where(File.Exists)
    .Select(filePath => new FileStream(filePath, FileMode.Open));

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFile("MatchLog.txt"));
var loggerSubscriber = new LoggerSubscriber(loggerFactory.CreateLogger<LoggerSubscriber>());

await new DefaultStreamScanner(loggerSubscriber.MatchHandler).ScanStreamsInParallelAsync(
    streams: streams,
    scanParams,
    bufferSize: 1024 * 4,
    cancellationToken);


void InitFilesWithFallBackDataIfAllEmpty()
{
    if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

    bool anyFileExists = defaultFilePaths.Any(File.Exists);
    if (anyFileExists) return;

    foreach (string filePath in defaultFilePaths)
        File.WriteAllText(filePath, fallbackData, Encoding.ASCII);
}

static StreamScanParams HandleInputCore()
{
    Console.Write("Введите start: ");
    string start = Console.ReadLine() ?? string.Empty;

    Console.Write("Введите end: ");
    string end = Console.ReadLine() ?? string.Empty;

    Console.Write("Введите contains: ");
    string contains = Console.ReadLine() ?? string.Empty;

    Console.Write("Введите template: ");
    string template = Console.ReadLine() ?? string.Empty;

    Console.Write("Введите max: ");
    int max = int.Parse(Console.ReadLine() ?? string.Empty);

    return new StreamScanParams(start, end, contains, template, max, ignoreCase: false);
}

static StreamScanParams HandleInput()
{
    while (true)
    {
        try
        {
            return HandleInputCore();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ошибка ввода: {e.Message}{Environment.NewLine}");
        }
    }
}