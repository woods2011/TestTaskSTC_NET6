using System.Text;

namespace TestTask2.Benchmarks.ObsoleteHtmlScannerCleanerImplementations;

/// <summary>
/// Класс предназначеный для удаления HTML-тегов из потока.
/// </summary>
public static class HtmlStreamCleanerWithStreamReader
{
    /// <summary>
    /// Асинхронно удаляет HTML-теги из входного потока и записывает результат в выходной поток.
    /// </summary>
    /// <param name="inputStream">Входной поток, содержащий HTML-теги.</param>
    /// <param name="outputStream">Выходной поток для записи результата очищенный от HTML-тегов.</param>
    /// <param name="readBufferSize">Размер буфера для чтения потока (по умолчанию равен 4096 байт).</param>
    /// <param name="encoding">Кодировка потока (по умолчанию UTF-8).</param>
    /// <param name="token">CancellationToken.</param>
    public static async Task RemoveHtmlTagsFromStreamAsync(
        Stream inputStream,
        Stream outputStream,
        int readBufferSize = 4096,
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