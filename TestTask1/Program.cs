using System.Text;
using Microsoft.Extensions.Logging;
using TestTask1;
using TestTask1.StreamScanner;

DateTime Start = DateTime.Now;
Console.WriteLine("START: " + Start);
var cancellationToken = CancellationToken.None; // Для наглядности

// Пример ввода для стандартных данных: Start: start, end: End, contains: Contains, template: \d+, max: 1000
const string fallbackData = "some data Start some data 10 Contains some data 11 End some data";

const string directoryPath = "TestFiles";
string[] defaultFilePaths =
{
    Path.Combine(directoryPath, "test1.txt"),
    Path.Combine(directoryPath, "test2.txt"),
    Path.Combine(directoryPath, "test3.txt")
};

string outputFilePath = Path.Combine(directoryPath, "MatchLog.txt");

InitFilesWithFallBackDataIfAllEmpty();


StreamScanParams scanParams = HandleInput();

IEnumerable<Stream> streams = defaultFilePaths
    .Where(File.Exists)
    .Select(filePath => new FileStream(filePath, FileMode.Open));

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddFile(outputFilePath));
using var loggerSubscriber = new LoggerSubscriber(loggerFactory.CreateLogger<LoggerSubscriber>());

// await new DefaultStreamScanner(loggerSubscriber.MatchHandler).ScanStreamsInParallelAsync(
//     streams: streams,
//     scanParams,
//     bufferSize: 4096,
//     cancellationToken);

await new DefaultStreamScanner(loggerSubscriber.MatchHandler).ScanStreamsInParallelAsync(
    streams: new List<Stream>(){File.Open(scanParams.Path!, FileMode.Open, FileAccess.Read)},
    scanParams,
    bufferSize: 4096,
    cancellationToken);
DateTime end = DateTime.Now;
Console.WriteLine("END: " + end);
Console.WriteLine("Time: " + (end -Start).TotalMilliseconds + " ms");
Console.WriteLine($"Результат записан в файл: {Path.GetFullPath(outputFilePath)}");


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
    var args = Environment.GetCommandLineArgs();
    string start = args[1];
    string end = args[2];
    string contains = args[3];
    string template = args[4];
    int max = Int32.Parse(args[5]);
    string path = args[6];

    // Console.Write("Введите start: ");
    // string start = Console.ReadLine() ?? string.Empty;
    //
    // Console.Write("Введите end: ");
    // string end = Console.ReadLine() ?? string.Empty;
    //
    // Console.Write("Введите contains: ");
    // string contains = Console.ReadLine() ?? string.Empty;
    //
    // Console.Write("Введите template: ");
    // string template = Console.ReadLine() ?? string.Empty;
    //
    // Console.Write("Введите max: ");
    // int max = int.Parse(Console.ReadLine() ?? string.Empty);

    return new StreamScanParams(start, end, contains, template, max, ignoreCase: false, path: path);
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
            Console.WriteLine($"{Environment.NewLine}Ошибка ввода: {e.Message}{Environment.NewLine}");
        }
    }
}