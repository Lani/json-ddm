# JsonDdm Performance Benchmarks

This project contains comprehensive performance benchmarks for the JsonDdm library using BenchmarkDotNet.

## Running Benchmarks Locally

### Run All Benchmarks

```bash
cd JsonDdm.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Class

```bash
dotnet run -c Release --filter "*MergeBenchmarks*"
```

### Run Specific Benchmark Method

```bash
dotnet run -c Release --filter "*MergeBenchmarks.MergeSmallDocuments*"
```

### Run with Additional Options

```bash
# Run with memory profiler
dotnet run -c Release -- --memory

# Run with all exporters
dotnet run -c Release -- --exporters json html csv

# Run quick mode (fewer iterations, faster results)
dotnet run -c Release -- --job short
```

## Benchmark Categories

### 1. MergeBenchmarks

Basic merge operations testing overall performance with documents of varying sizes:

- Small documents (simple objects)
- Medium documents (nested objects with arrays)
- Large documents (deep nesting with multiple arrays)

### 2. ArrayReorderingBenchmarks

Tests the O(n²) complexity concern identified in the code review:

- Parameterized by array size (10, 50, 100, 500 items)
- Parameterized by number of reorder operations (1, 5, 10)
- Tests various position types (before, after, start, end)

**This is critical for tracking improvements to the reordering algorithm.**

### 3. DeepNestingBenchmarks

Tests performance and safety with deeply nested structures:

- Parameterized by nesting depth (5, 10, 20, 50 levels)
- Tests both objects and arrays at each level
- Validates that depth limits don't impact performance significantly

### 4. CloneBenchmarks

Tests the performance impact of DeepClone() operations:

- Parameterized by object/array count (100, 500, 1000)
- Tests large objects with many properties
- Tests large arrays with complex items

**This helps measure improvements when reducing unnecessary cloning.**

### 5. ControlKeysBenchmarks

Tests control key operations:

- Delete operations ($patch)
- Value extraction ($value)
- Key escaping ($$key)
- Parameterized by item count (50, 100, 200)

## Understanding Results

BenchmarkDotNet output includes:

- **Mean**: Average execution time
- **Error**: Standard error (half of 99.9% confidence interval)
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: Garbage collection counts
- **Allocated**: Memory allocated per operation

### Example Output

```
| Method              | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|----------:|
| MergeSmallDocuments | 1.234 μs  | 0.015 μs | 0.014 μs | 0.0191 |     320 B |
| MergeLargeDocuments | 45.67 μs  | 0.234 μs | 0.219 μs | 2.5635 |   42968 B |
```

## CI Integration

### Saving Baseline Results

Before making changes, establish a baseline:

```bash
# Run benchmarks and save results
dotnet run -c Release -- --exporters json --artifacts ./baseline-results
```

This creates JSON files in `./baseline-results/` that can be:

1. Committed to the repository
2. Saved as CI artifacts
3. Used for comparison in future runs

### Comparing Against Baseline

After making changes:

```bash
# Run benchmarks again
dotnet run -c Release -- --exporters json --artifacts ./current-results

# Compare using BenchmarkDotNet's built-in comparison or custom tools
```

### GitHub Actions Example

Create `.github/workflows/benchmarks.yml`:

```yaml
name: Performance Benchmarks

on:
  pull_request:
    branches: [main]
  workflow_dispatch:

jobs:
  benchmark:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0 # Need git history for comparison

      # Checkout main branch to get baseline
      - name: Checkout main branch
        run: |
          git fetch origin main
          git checkout main

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "9.0.x"

      # Run baseline benchmarks on main
      - name: Run baseline benchmarks
        run: |
          cd JsonDdm.Benchmarks
          dotnet run -c Release -- --exporters json --artifacts ./baseline-results

      # Checkout PR branch
      - name: Checkout PR branch
        run: git checkout ${{ github.sha }}

      # Run current benchmarks
      - name: Run current benchmarks
        run: |
          cd JsonDdm.Benchmarks
          dotnet run -c Release -- --exporters json --artifacts ./current-results

      # Compare results (using custom script)
      - name: Compare results
        run: |
          # You can use tools like:
          # - BenchmarkDotNet.ResultsComparer
          # - Custom PowerShell/Python scripts
          # - benchmarkdotnet-ci-comparison tool
          echo "Comparison logic here"

      # Upload artifacts for manual review
      - name: Upload baseline results
        uses: actions/upload-artifact@v4
        with:
          name: baseline-results
          path: JsonDdm.Benchmarks/baseline-results/**/*

      - name: Upload current results
        uses: actions/upload-artifact@v4
        with:
          name: current-results
          path: JsonDdm.Benchmarks/current-results/**/*
```

### Using BenchmarkDotNet Baseline Feature

You can also use BenchmarkDotNet's built-in baseline feature in the code:

```csharp
[Benchmark(Baseline = true)]
public JsonNode? MergeSmallDocuments_Old()
{
    // Old implementation
}

[Benchmark]
public JsonNode? MergeSmallDocuments_Optimized()
{
    // New implementation
}
```

The results will show relative performance:

```
| Method                        | Mean     | Ratio |
|------------------------------ |---------:|------:|
| MergeSmallDocuments_Old       | 1.234 μs | 1.00  |
| MergeSmallDocuments_Optimized | 0.567 μs | 0.46  | <- 2.18x faster!
```

## Performance Goals

Based on the code review, target improvements:

1. **Array Reordering**: Reduce from O(n²) to O(n)
   - Current: ~500ms for 500 items with 10 reorders
   - Target: <50ms for same workload

2. **Deep Cloning**: Reduce unnecessary clones
   - Current: High allocation rate
   - Target: 50% reduction in allocations

3. **Deep Nesting**: Add limits without performance penalty
   - Target: <10% overhead with depth checking

## Best Practices

1. **Always run in Release mode** - Debug mode skews results
2. **Close other applications** - Minimize background interference
3. **Run multiple times** - BenchmarkDotNet does this automatically
4. **Use `[MemoryDiagnoser]`** - Track allocations, not just time
5. **Parameterize tests** - Test multiple scales to find complexity
6. **Save baselines** - Track performance over time

## Troubleshooting

### Benchmark runs very slowly

- Reduce parameter combinations
- Use `--job short` for quick verification
- Run specific benchmark instead of all

### High variance in results

- Close background applications
- Run on dedicated hardware
- Increase warmup iterations

### OutOfMemoryException

- Reduce parameterized values
- Run benchmarks individually
- Increase available memory

## Further Reading

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
- [Optimization Guidelines](https://learn.microsoft.com/en-us/dotnet/core/performance/)
