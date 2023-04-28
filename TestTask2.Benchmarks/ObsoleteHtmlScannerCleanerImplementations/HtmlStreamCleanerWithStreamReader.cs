using System.Text;

namespace TestTask2.Benchmarks.ObsoleteHtmlScannerCleanerImplementations;

public static class HtmlStreamCleanerWithStreamReader
{
    public static async Task RemoveHtmlTagsFromStreamAsync(
        Stream inputStream,
        Stream outputStream,
        int readBufferSize = 1024,
        Encoding? encoding = null,
        CancellationToken token = default)
    {
        encoding ??= Encoding.UTF8;
        
        var charReadBuffer = new char[encoding.GetMaxCharCount(readBufferSize)];
        int charsRead;
        await using StreamWriter writer = new(outputStream, encoding, leaveOpen: true); // только flush, без закрытия потока
        StreamReader reader = new(inputStream, encoding, bufferSize: readBufferSize);

        var isInsideTag = false;

        while ((charsRead = await reader.ReadAsync(charReadBuffer, token)) > 0)
        {
            for (var index = 0; index < charsRead; index++)
            {
                char curChar = charReadBuffer[index];

                switch (curChar)
                {
                    case '<':
                        isInsideTag = true;
                        break;

                    case '>':
                        isInsideTag = false;
                        break;

                    default:
                        if (!isInsideTag) 
                            await writer.WriteAsync(curChar);
                        break;
                }
            }
        }
    }
}