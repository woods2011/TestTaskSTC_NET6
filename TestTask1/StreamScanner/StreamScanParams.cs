using System.Text.RegularExpressions;

namespace TestTask1.StreamScanner;

public class StreamScanParams
{
    public string Start { get; }
    public string End { get; }
    public string Contains { get; }
    public int Max { get; }
    public bool IgnoreCase { get; }
    public Regex Regex { get; }

    public StreamScanParams(
        string start,
        string end,
        string contains,
        string template,
        int max,
        bool ignoreCase = false)
    {
        ValidateArgumentForEmpty(start, nameof(start));
        ValidateArgumentForEmpty(end, nameof(end));
        ValidateArgumentForEmpty(template, nameof(template));

        if (max <= 0)
            throw new ArgumentOutOfRangeException(nameof(max), "Значение параметра должно быть больше 0");

        Start = start;
        End = end;
        Contains = contains;
        Max = max;
        IgnoreCase = ignoreCase;

        RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (IgnoreCase) regexOptions |= RegexOptions.IgnoreCase;
        Regex = new Regex(template, regexOptions);
        
        static void ValidateArgumentForEmpty(string param, string paramName)
        {
            if (string.IsNullOrEmpty(param))
                throw new ArgumentException("Параметр не может быть пустым", paramName);
        }
    }
}