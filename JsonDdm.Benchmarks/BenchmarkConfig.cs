using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Custom configuration for benchmarks.
/// Can be applied to individual benchmark classes with [Config(typeof(CustomBenchmarkConfig))].
/// </summary>
public class CustomBenchmarkConfig : ManualConfig
{
  public CustomBenchmarkConfig()
  {
    // Add memory diagnoser to track allocations
    AddDiagnoser(MemoryDiagnoser.Default);

    // Export results in multiple formats
    AddExporter(MarkdownExporter.GitHub);
    AddExporter(HtmlExporter.Default);
    AddExporter(CsvExporter.Default);
    AddExporter(JsonExporter.FullCompressed);
    // Add statistical columns
    AddColumn(StatisticColumn.Mean);
    AddColumn(StatisticColumn.Error);
    AddColumn(StatisticColumn.StdDev);
    AddColumn(StatisticColumn.Median);
    AddColumn(StatisticColumn.Min);
    AddColumn(StatisticColumn.Max);
    AddColumn(BaselineRatioColumn.RatioMean);

    // Order benchmarks by name
    Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);

    // Job configuration
    AddJob(Job.Default
        .WithWarmupCount(3)      // Number of warmup iterations
        .WithIterationCount(10)   // Number of measured iterations
        .WithMaxIterationCount(20)); // Maximum iterations
  }
}

/// <summary>
/// Quick configuration for rapid iteration during development.
/// Fewer iterations, faster results, less accuracy.
/// </summary>
public class QuickBenchmarkConfig : ManualConfig
{
  public QuickBenchmarkConfig()
  {
    AddDiagnoser(MemoryDiagnoser.Default);
    AddExporter(MarkdownExporter.Console);
    AddLogger(ConsoleLogger.Default);

    // Fast job for quick feedback
    AddJob(Job.Dry  // Very fast, minimal iterations
        .WithWarmupCount(1)
        .WithIterationCount(1));
  }
}
