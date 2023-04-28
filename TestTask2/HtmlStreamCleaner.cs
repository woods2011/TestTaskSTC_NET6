using System.Text;

namespace TestTask2;

public static class HtmlStreamCleaner
{
    public static async Task RemoveHtmlTagsFromStreamAsync(
        Stream inputStream,
        Stream outputStream,
        int readBufferSize = 1024,
        Encoding? encoding = null,
        CancellationToken token = default)
    {
        encoding ??= Encoding.UTF8;
        Decoder decoder = encoding.GetDecoder(); // Необходимо при работе например с networstream и кодировками >=2 байт

        var readBuffer = new byte[readBufferSize];
        var charReadBuffer = new char[encoding.GetMaxCharCount(readBufferSize)];
        int bytesRead;
        await using StreamWriter writer = new(outputStream, encoding, leaveOpen: true); // только flush, без закрытия потока

        var isInsideTag = false;

        while ((bytesRead = await inputStream.ReadAsync(readBuffer, token)) > 0)
        {
            int dataLenght = decoder.GetChars(readBuffer, 0, bytesRead, charReadBuffer, 0);

            for (var index = 0; index < dataLenght; index++)
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