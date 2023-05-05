using System.Text.RegularExpressions;

namespace TestTask1.StreamScanner;

public class StreamScanParams
{
    public string Start { get; }
    public string End { get; }
    public string Contains { get; }
    public int Max { get; }
    public bool IgnoreCase { get; }
    public string? Path { get; set; }
    public Regex Regex { get; }

    public StreamScanParams(string start,
        string end,
        string contains,
        string template,
        int max,
        bool ignoreCase = false,
        string? path = null)
    {
        ValidateArgumentForLengthFrom2To63(start, nameof(start));
        ValidateArgumentForLengthFrom2To63(end, nameof(end));
        ValidateArgumentForLengthFrom2To63(contains, nameof(contains));
        ValidateArgumentForEmpty(template, nameof(template));
        if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), "Значение параметра должно быть больше 0");
        
        Start = start;
        End = end;
        Contains = contains;
        Max = max;
        IgnoreCase = ignoreCase;
        Path = path;

        RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (IgnoreCase) regexOptions |= RegexOptions.IgnoreCase;
        Regex = new Regex(template, regexOptions);

        static void ValidateArgumentForEmpty(string param, string paramName)
        {
            if (string.IsNullOrEmpty(param))
                throw new ArgumentException("Параметр не может быть пустым", paramName);
        }

        static void ValidateArgumentForLengthFrom2To63(string param, string paramName)
        {
            if (param.Length is < 2 or > 63)
                throw new ArgumentOutOfRangeException(paramName, "Длина параметра должна быть от 2 до 63 символов");
        }
    }
}