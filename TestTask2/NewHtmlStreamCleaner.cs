using System.Text;

namespace TestTask2;

/// <summary>
/// Класс предназначеный для удаления HTML-тегов из потока.
/// </summary>
public static class NewHtmlStreamCleaner
{
    /// <summary>
    /// Асинхронно удаляет HTML-теги из входного потока и записывает результат в выходной поток.
    /// Работает только для кодировок в которых символы не могут перекрываться.
    /// </summary>
    /// <param name="inputStream">Входной поток, содержащий HTML-теги.</param>
    /// <param name="outputStream">Выходной поток для записи результата очищенный от HTML-тегов.</param>
    /// <param name="readBufferSize">Размер буфера для чтения потока (по умолчанию равен 4096 байт).</param>
    /// <param name="writeBufferSize">Размер буфера для записи в поток (по умолчанию равен 4096 байт).</param>
    /// <param name="encoding">Кодировка потока (по умолчанию UTF-8).</param>
    /// <param name="token">CancellationToken.</param>
    public static async Task RemoveHtmlTagsFromStreamAsync(
        Stream inputStream,
        Stream outputStream,
        int readBufferSize = 4096,
        int writeBufferSize = 4096,
        Encoding? encoding = null,
        CancellationToken token = default)
    {
        encoding ??= new UTF8Encoding(false);
        await using var bufferedStream = new BufferedStream(outputStream, writeBufferSize);

        byte[] openTagBytes = encoding.GetBytes(new[] { '<' });
        byte[] closeTagBytes = encoding.GetBytes(new[] { '>' });
        int maxReminderLen = Math.Max(openTagBytes.Length, closeTagBytes.Length) - 1;

        if (maxReminderLen is 0)
        {
            await RemoveHtmlTagsFromStreamAsyncForOneByteEncodings(
                inputStream,
                outputStream: bufferedStream,
                openTagByte: openTagBytes[0],
                closeTagByte: closeTagBytes[0],
                readBufferSize,
                token);

            return;
        }

        var dataReminder = 0;
        var readBuffer = new byte[readBufferSize + maxReminderLen];
        int bytesRead;
        var isInsideTag = false;

        while ((bytesRead = await inputStream.ReadAsync(readBuffer.AsMemory(dataReminder), token)) > 0)
        {
            ReadOnlyMemory<byte> newData = readBuffer.AsMemory(0, dataReminder + bytesRead);

            while (newData.Length > 0)
            {
                if (!isInsideTag)
                {
                    int indexOfOpenTag = newData.Span.IndexOf(openTagBytes);
                    if (indexOfOpenTag is not -1)
                    {
                        await WriteDataToStream(newData[..indexOfOpenTag]);

                        (isInsideTag, dataReminder) = (true, 0);
                        newData = newData[(indexOfOpenTag + openTagBytes.Length)..];
                    }
                    else
                    {
                        dataReminder = Math.Min(openTagBytes.Length - 1, newData.Length);
                        int indexToSlice = newData.Length - dataReminder;

                        await WriteDataToStream(newData[..indexToSlice]);

                        newData[indexToSlice..].CopyTo(readBuffer);
                        break;
                    }
                }
                else
                {
                    int indexOfCloseTag = newData.Span.IndexOf(closeTagBytes);
                    if (indexOfCloseTag is not -1)
                    {
                        (isInsideTag, dataReminder) = (false, 0);
                        newData = newData[(indexOfCloseTag + closeTagBytes.Length)..];
                    }
                    else
                    {
                        dataReminder = Math.Min(closeTagBytes.Length - 1, newData.Length);
                        int indexToSlice = newData.Length - dataReminder;

                        newData[indexToSlice..].CopyTo(readBuffer);
                        break;
                    }
                }
            }
        }

        await WriteDataToStream(readBuffer.AsMemory(0, dataReminder));

        
        // -------------------------------------------------------------------------------------------------------------
        async ValueTask WriteDataToStream(ReadOnlyMemory<byte> dataToWriteLocal)
        {
            if (dataToWriteLocal.Length > 0)
                await bufferedStream.WriteAsync(dataToWriteLocal, token);
        }
    }

    private static async Task RemoveHtmlTagsFromStreamAsyncForOneByteEncodings(
        Stream inputStream,
        Stream outputStream,
        byte openTagByte,
        byte closeTagByte,
        int readBufferSize = 4096,
        CancellationToken token = default)
    {
        var readBuffer = new byte[readBufferSize];
        int bytesRead;
        var isInsideTag = false;

        while ((bytesRead = await inputStream.ReadAsync(readBuffer, token)) > 0)
        {
            ReadOnlyMemory<byte> newData = readBuffer.AsMemory(0, bytesRead);

            while (newData.Length > 0)
            {
                if (!isInsideTag)
                {
                    int indexOfOpenTag = newData.Span.IndexOf(openTagByte);
                    if (indexOfOpenTag is not -1)
                    {
                        await WriteDataToStream(newData[..indexOfOpenTag]);

                        isInsideTag = true;
                        newData = newData[(indexOfOpenTag + 1)..];
                    }
                    else
                    {
                        await WriteDataToStream(newData);
                        break;
                    }
                }
                else
                {
                    int indexOfCloseTag = newData.Span.IndexOf(closeTagByte);
                    if (indexOfCloseTag is not -1)
                    {
                        isInsideTag = false;
                        newData = newData[(indexOfCloseTag + 1)..];
                    }
                    else break;
                }
            }
        }

        async ValueTask WriteDataToStream(ReadOnlyMemory<byte> dataToWriteLocal)
        {
            if (dataToWriteLocal.Length > 0)
                await outputStream.WriteAsync(dataToWriteLocal, token);
        }
    }
}