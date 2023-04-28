﻿// See https://aka.ms/new-console-template for more information

using System.Text;
using TestTask2;

const string fallbackData = "<html><body><h1>Test</h1><p>Some text</p></body></html>";
var defaultEncoding = Encoding.UTF8;

const string inputFileDirectory = "Files";
if (!Directory.Exists(inputFileDirectory)) Directory.CreateDirectory(inputFileDirectory);

string inputFilePath = Path.Combine(inputFileDirectory, "index.html");
if (!File.Exists(inputFilePath)) File.WriteAllText(inputFilePath, fallbackData, defaultEncoding);

string outputFilePath = Path.Combine(inputFileDirectory, "indexResult.txt");

await using var inputStream = new FileStream(inputFilePath, FileMode.Open);
await using var outputStream = new FileStream(outputFilePath, FileMode.Create);

await HtmlStreamCleaner.RemoveHtmlTagsFromStreamAsync(inputStream, outputStream, encoding: defaultEncoding);

Console.WriteLine($"Результат записан в файл: {Path.GetFullPath(outputFilePath)}");