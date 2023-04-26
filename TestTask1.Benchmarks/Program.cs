using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using TestTask1.Benchmarks;

ManualConfig config = DefaultConfig.Instance
    .WithSummaryStyle(DefaultConfig.Instance.SummaryStyle.WithMaxParameterColumnWidth(75))
    .AddJob(Job.ShortRun)
    .AddExporter(MarkdownExporter.GitHub);

BenchmarkRunner.Run<StreamScannerBenchmarks>(config);

// BenchmarkRunner.Run<StreamScannerBenchmarks>(new DebugInProcessConfig());

// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
