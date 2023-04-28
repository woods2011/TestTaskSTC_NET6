using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using TestTask2.Benchmarks;

ManualConfig config = DefaultConfig.Instance
    .WithSummaryStyle(DefaultConfig.Instance.SummaryStyle.WithMaxParameterColumnWidth(75))
    .AddJob(Job.ShortRun)
    .AddExporter(MarkdownExporter.GitHub);
    
BenchmarkRunner.Run<HtmlStreamCleanerBenchmarks>(config);