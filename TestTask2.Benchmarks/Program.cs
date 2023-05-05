﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using TestTask2.Benchmarks;

IConfig defaultConfig = DefaultConfig.Instance;
// defaultConfig = new DebugInProcessConfig(); // Uncomment to enable debug

IConfig config = defaultConfig
    .WithSummaryStyle(DefaultConfig.Instance.SummaryStyle.WithMaxParameterColumnWidth(75))
    // .AddJob(Job.ShortRun)
    .AddJob(Job.MediumRun)
    .AddExporter(MarkdownExporter.GitHub);

BenchmarkRunner.Run<HtmlStreamCleanerBenchmarks>(config);