using System.Diagnostics;
using System.Text;
using TestTask1;
using TestTask1.StreamScanner;
using TestTask1.TestFiles;

const string filesDirectoryPath = "TestFiles";
string fallBackFilePath = Path.Combine(filesDirectoryPath, "TestFallBack_30MB_LotsOfValidAndInvalidRanges.txt");
if (!Directory.Exists(filesDirectoryPath)) Directory.CreateDirectory(filesDirectoryPath);


await MainFlow(); // Для того чтобы можно было быстро закомментировать и сгенерировать большие файлы (строка ниже)
// GenerateDataFlow();


async Task MainFlow()
{
    List<string> defaultFilePaths = new()
    {
        // fallBackFilePath,
        // Path.Combine(filesDirectoryPath, "TD1_200MB_FromGmail_RangeFrom1000to50000+.txt"),
        // Path.Combine(filesDirectoryPath, "TD2_200MB_LotsOfValidAndInvalidRanges.txt")
    };
    bool allFilesDoNotExist = AllFilesAtDefaultFilePathsDoNotExist();

    // 1. Ввод с консоли
    // (StreamScanParams scanParams, bool shouldGenerateFile) = HandleConsoleInput();
    // if (allFilesDoNotExist || shouldGenerateFile) GenerateFallbackFile(!shouldGenerateFile);

    // 2. Захардкоженные данные
    // StreamScanParams scanParams = HandleManualInput();

    // 3. Из коммандной строки
    StreamScanParams scanParams = HandleCmdArgsInput();

    if (scanParams.Path != null) defaultFilePaths.Add(scanParams.Path);

    List<FileStream> streams = defaultFilePaths
        .Distinct()
        .Where(File.Exists)
        // .Select(filePath => new EmulateChunkedNonSeekableStream(File.ReadAllBytes(filePath), 1));
        .Select(filePath => new FileStream(filePath, FileMode.Open, FileAccess.Read))
        .ToList();

    // ???
    // Stream to = File.OpenWrite("/Volumes/SONY ALPHA/test/test2.xml");
    // var w = Stopwatch.StartNew(); 
    // streams[0].CopyTo(to);
    // w.Stop();


    string outputFilePath = Path.Combine(filesDirectoryPath, "LastScanLog.txt");
    await using var matchesSubscriber = new MatchesReporterSubscriber(outputFilePath);

    //                         new SimpleGreedyStreamScanner(matchesSubscriber.MatchHandler)
    var defaultStreamScanner = new DefaultStreamScanner(matchesSubscriber.MatchHandler);

    var watch = Stopwatch.StartNew();
    Console.WriteLine($">>>Всего будет обработано: {streams.Count} файлов{Environment.NewLine}");
    Console.WriteLine($"START: {DateTime.Now:hh:mm:ss.fff}");

    await defaultStreamScanner.ScanStreamsInParallelAsync(
        streams: streams,
        scanParams,
        bufferSize: 4096 * 4,
        CancellationToken.None);

    watch.Stop();
    Console.WriteLine($"END: {DateTime.Now:hh:mm:ss.fff}");
    Console.WriteLine($"Time: {watch.Elapsed.TotalMilliseconds} ms");
    Console.WriteLine($"Результат записан в файл: {Path.GetFullPath(outputFilePath)}");

    await matchesSubscriber.DisposeAsync();
    streams.ForEach(stream => stream.Dispose());
    Console.WriteLine($"{Environment.NewLine}Нажмите любую кнопку для выхода...");
    Console.ReadKey();

    // -----------------------------------------------------------------------------------------------------------------
    bool AllFilesAtDefaultFilePathsDoNotExist() => !defaultFilePaths.Where(File.Exists).Any();


    StreamScanParams HandleManualInput() => new(
        start: "<h0ost endtime=\"",
        end: "></port></ports></hos0t>",
        contains: "<port protocol=\"tcp\"",
        template: "open",
        max: 70000,
        ignoreCase: false,
        path: "data.xml");

    StreamScanParams HandleCmdArgsInput()
    {
        string filePath = args[5];
        if (!File.Exists(filePath)) 
            Console.WriteLine($"!!!Указанный файл не существует: {Path.GetFullPath(filePath)}{Environment.NewLine}");

        return new(
            start: args[0],
            end: args[1],
            contains: args[2],
            template: args[3],
            max: Int32.Parse(args[4]),
            ignoreCase: false,
            path: filePath);
    }

    static (StreamScanParams, bool) HandleConsoleInput()
    {
        static (StreamScanParams, bool) HandleConsoleInputCore()
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

            Console.Write("Сгенерировать файл автоматически? (y/n): ");
            string generateFileStr = Console.ReadLine() ?? string.Empty;
            bool shouldGenerateFile = generateFileStr.Equals("y", StringComparison.InvariantCultureIgnoreCase);

            var streamScanParams = new StreamScanParams(start, end, contains, template, max, ignoreCase: false);
            return (streamScanParams, shouldGenerateFile);
        }

        while (true)
        {
            try
            {
                return HandleConsoleInputCore();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка ввода: {e.Message}{Environment.NewLine}");
            }
        }
    }

    void GenerateFallbackFile(bool shouldNotify)
    {
        if (shouldNotify)
            Console.WriteLine("Т.к. по стандартным путям файлы отсутствовали, файл будет сгенерирован автоматически!" +
                              Environment.NewLine);

        defaultFilePaths = new List<string> { fallBackFilePath };

        var template = @"(\+\d{1,2}\s?)?(\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}";
        Console.WriteLine(
            $"При генерации используются значения соответствующие шаблону:{Environment.NewLine}" +
            $">>>Regex для номера телефона (US):{Environment.NewLine}" +
            $"{template}{Environment.NewLine}");

        TestDataProvider.GenerateFullyRandomTestData(
            matchedValue: "+1 (555) 123-4567",
            startMarker: scanParams.Start,
            endMarker: scanParams.End,
            containsMarker: scanParams.Contains,
            filePath: fallBackFilePath,
            fileSizeInMegaBytes: 30);
    }
}

// ---------------------------------------------------------------------------------------------------------------------
void GenerateDataFlow()
{
    // Template: (\+\d{1,2}\s?)?(\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}               | Regex для номера телефона (US)

    TestDataProvider.GenerateTestDataFromVasheGmailPismo(
        matchedValue: "+1 (555) 123-4567",
        startMarker: "START",
        endMarker: "ENDING",
        containsMarker: "CONTAINS",
        filePath: Path.Combine(filesDirectoryPath, "TD1_200MB_FromGmail_RangeFrom1000to50000+.txt"),
        fileSizeInMegaBytes: 200);

    // TestDataProvider.GenerateFullyRandomTestData(
    //     matchedValue: "+1 (555) 123-4567",
    //     startMarker: "START",
    //     endMarker: "ENDING",
    //     containsMarker: "CONTAINS",
    //     filePath: Path.Combine(filesDirectoryPath, "TD2_200MB_LotsOfValidAndInvalidRanges.txt"),
    //     fileSizeInMegaBytes: 200);
}


// StreamScanParams scanParams = new(
//     "START", 
//     "ENDING",
//     "CONTAINS",
//     @"(\+\d{1,2}\s?)?(\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}", 60000, ignoreCase: false);