// See https://aka.ms/new-console-template for more information

using System.Text;
using TestTask2;

var fallbackData = "<html><body><h1>Test</h1><p>Some text</p></body></html>";
var defaultEncoding = Encoding.UTF8;

var inputFileDirectory = "Files";
if (!Directory.Exists(inputFileDirectory)) Directory.CreateDirectory(inputFileDirectory);

var inputFilePath = Path.Combine(inputFileDirectory, "index.html");
if (!File.Exists(inputFilePath)) File.WriteAllText(inputFilePath, fallbackData, defaultEncoding);

var outputFilePath = Path.Combine(inputFileDirectory, "indexResult.txt");

using var inputStream = new FileStream(inputFilePath, FileMode.Open);
using var outputStream = new FileStream(outputFilePath, FileMode.Create);

await HtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(inputStream, outputStream, encoding: defaultEncoding);