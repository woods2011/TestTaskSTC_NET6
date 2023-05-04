namespace TestTask1.TestFiles;

public static class TestDataProvider
{
    public static void GenerateTestDataFromVasheGmailPismo(
        string matchedValue = "1,111,111.1",
        string startMarker = "START",
        string endMarker = "END",
        string containsMarker = "CONTAINS",
        string filePath = "TestFiles/test_200MB_OneRangeFrom1000to50000+_3MatchesAtLeast.txt",
        int fileSizeInMegaBytes = 200,
        int? rndSeed = 5)
    {
        string? directoryName = Path.GetDirectoryName(filePath);
        if (directoryName is not null && !Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        Random random = rndSeed.HasValue ? new Random(rndSeed.Value) : Random.Shared;
        long fileSize = fileSizeInMegaBytes * 1024 * 1024; // 200 MB
        const int bufferSize = 8 * 1024;                   // 8 KB


        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var bufferedStream = new BufferedStream(fileStream, bufferSize);

        for (long index = 0; index < fileSize; index++)
        {
            switch (index)
            {
                case 1000:
                    bufferedStream.Write(GetBytesFromAscii(startMarker));
                    break;

                case 25000:
                    byte[] contains = GetBytesFromAscii(containsMarker);
                    index += contains.Length - 1;

                    bufferedStream.Write(contains);
                    break;

                case 27000 or 37000 or 47000:
                    byte[] matchedValueBytes = GetBytesFromAscii(matchedValue);
                    index += matchedValueBytes.Length - 1;

                    bufferedStream.Write(matchedValueBytes);
                    break;

                case 50000:
                    bufferedStream.Write(GetBytesFromAscii(endMarker));
                    break;

                default:
                    bufferedStream.WriteByte(GenerateRandomPrintableAsciiChar(random));
                    break;
            }
        }

        Console.WriteLine($"Сгенерированный файл находится по адресу:{Environment.NewLine}" +
                          $"\t{Path.GetFullPath(filePath)}{Environment.NewLine}");
    }


    public static void GenerateFullyRandomTestData(
        string matchedValue = "1,111,111.1",
        string startMarker = "START",
        string endMarker = "END",
        string containsMarker = "CONTAINS",
        string filePath = "TestFiles/test_200MB_RandomRangeCount.txt",
        int fileSizeInMegaBytes = 200,
        double dataInsertPerByteProbability = 1e-4,
        int? rndSeed = 5)
    {
        string? directoryName = Path.GetDirectoryName(filePath);
        if (directoryName is not null && !Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        Random random = rndSeed.HasValue ? new Random(rndSeed.Value) : Random.Shared;
        long fileSize = fileSizeInMegaBytes * 1024 * 1024;
        const int bufferSize = 4 * 1024; // 4 KB

        using var fileStream = new FileStream(filePath,
            new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, BufferSize = bufferSize });
        using var bufferedStream = new BufferedStream(fileStream, bufferSize);

        for (long index = 0; index < fileSize; index++)
        {
            var isInsertSomething = random.NextDouble() < dataInsertPerByteProbability;

            if (!isInsertSomething)
            {
                bufferedStream.WriteByte(GenerateRandomPrintableAsciiChar(random));
                continue;
            }

            var whatToInsertProbe = random.NextDouble();

            var bytes = whatToInsertProbe switch
            {
                < 0.2 => GetBytesFromAscii(startMarker),    // 0.2 prob
                < 0.3 => GetBytesFromAscii(endMarker),      // 0.1 prob
                < 0.5 => GetBytesFromAscii(containsMarker), // 0.2 prob
                _ => GetBytesFromAscii(matchedValue)        // 0.5 prob
            };

            index += bytes.Length - 1;
            bufferedStream.Write(bytes);
        }

        Console.WriteLine($"Сгенерированный файл находится по адресу:{Environment.NewLine}" +
                          $"\t{Path.GetFullPath(filePath)}{Environment.NewLine}");
    }


    private static byte[] GetBytesFromAscii(string input) => GetBytesFromAscii(input.ToCharArray());

    private static byte[] GetBytesFromAscii(params char[] chars)
    {
        var bytes = new byte[chars.Length];

        for (int i = 0; i < chars.Length; i++)
            bytes[i] = (byte) chars[i];

        return bytes;
    }


    private static byte GenerateRandomChar(Random random, int minValue, int maxValue, params char[] excludedChars)
    {
        byte randomChar;

        do randomChar = (byte) random.Next(minValue, maxValue + 1);
        while (excludedChars.Contains((char) randomChar));

        return randomChar;
    }

    private static byte GenerateRandomPrintableAsciiChar(Random random, params char[] excludedChars) =>
        GenerateRandomChar(random, 32, 126, excludedChars);

    private static byte GenerateRandomPrintableAsciiChar(Random random, IEnumerable<char> excludedChars) =>
        GenerateRandomChar(random, 32, 126, excludedChars.ToArray());

    private static byte GenerateRandomAsciiChar(Random random, params char[] excludedChars) =>
        GenerateRandomChar(random, 0, 127, excludedChars);


    public static readonly char[] ForbiddenChars = { '\x00', '\x0d', '\x0a' };
}

// char[] excludedChars = Array.Empty<char>()
//     .Concat(startMarker)
//     .Concat(endMarker)
//     .Concat(containsMarker)
//     .ToArray();