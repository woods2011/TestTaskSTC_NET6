using System.Text;
using TestTask1;
using TestTask1.StreamScanner;
using TestTask1.TestFiles;

const string filesDirectoryPath = "TestFiles";
string fallBackFilePath = Path.Combine(filesDirectoryPath, "TestFallBack_30MB_LotsOfValidAndInvalidRanges.txt");


await MainFlow(); // Для того чтобы можно было быстро закомментировать и сгенерировать большие файлы (строка ниже)
// GenerateDataFlow();


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

// ---------------------------------------------------------------------------------------------------------------------
async Task MainFlow()
{
    if (!Directory.Exists(filesDirectoryPath)) Directory.CreateDirectory(filesDirectoryPath);

    string[] defaultFilePaths =
    {
        // fallBackFilePath,
        // Path.Combine(filesDirectoryPath, "TD1_200MB_FromGmail_RangeFrom1000to50000+.txt"),
        Path.Combine(filesDirectoryPath, "TD2_200MB_LotsOfValidAndInvalidRanges.txt")
    };

    bool allFilesDoNotExist = AllFilesAtDefaultFilePathsDoNotExist();

    (StreamScanParams scanParams, bool shouldGenerateFile) = HandleInput();

    if (allFilesDoNotExist || shouldGenerateFile) GenerateFallbackFile();
    FileStream[] streams = defaultFilePaths
        .Distinct()
        .Where(File.Exists)
        // .Select(filePath => new EmulateChunkedNonSeekableStream(File.ReadAllBytes(filePath), 1));
        .Select(filePath => new FileStream(filePath, FileMode.Open, FileAccess.Read))
        .ToArray();

    Console.WriteLine($">>>Всего будет обработано: {streams.Length} файлов");

    string outputFilePath = Path.Combine(filesDirectoryPath, "LastScanLog.txt");
    await using var matchesSubscriber = new MatchesReporterSubscriber(outputFilePath);


    //                         new SimpleGreedyStreamScanner(matchesSubscriber.MatchHandler)
    var defaultStreamScanner = new DefaultStreamScanner(matchesSubscriber.MatchHandler);

    await defaultStreamScanner.ScanStreamsInParallelAsync(
        streams: streams,
        scanParams,
        bufferSize: 4096,
        CancellationToken.None);


    // -----------------------------------------------------------------------------------------------------------------
    bool AllFilesAtDefaultFilePathsDoNotExist()
    {
        defaultFilePaths = defaultFilePaths.Where(File.Exists).ToArray();
        return !defaultFilePaths.Any();
    }

    static (StreamScanParams, bool) HandleInputCore()
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

        Console.WriteLine();

        var streamScanParams = new StreamScanParams(start, end, contains, template, max, ignoreCase: false);
        return (streamScanParams, shouldGenerateFile);
    }

    static (StreamScanParams, bool) HandleInput()
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

    void GenerateFallbackFile()
    {
        if (!shouldGenerateFile)
            Console.WriteLine("Т.к. по стандартным путям файлы отсутствовали, файл будет сгенерирован автоматически!" +
                              Environment.NewLine);

        defaultFilePaths = new[] { fallBackFilePath };

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


// StreamScanParams scanParams = new(
//     "START", 
//     "ENDING",
//     "CONTAINS",
//     @"(\+\d{1,2}\s?)?(\(\d{3}\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}", 60000, ignoreCase: false);