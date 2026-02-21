# Performance Testing Setup

Comprehensive performance testing infrastructure has been created for JsonDdm using BenchmarkDotNet.

## 📁 What Was Created

### Benchmark Project

- **JsonDdm.Benchmarks/** - New project with comprehensive benchmarks
  - `MergeBenchmarks.cs` - Basic merge operations (small, medium, large documents)
  - `ArrayReorderingBenchmarks.cs` - Tests O(n²) complexity concern with parameterized sizes
  - `DeepNestingBenchmarks.cs` - Tests deep nesting and stack overflow scenarios
  - `CloneBenchmarks.cs` - Tests excessive DeepClone() performance impact
  - `ControlKeysBenchmarks.cs` - Tests control key operations ($patch, $value, etc.)
  - `BenchmarkConfig.cs` - Custom configurations (standard and quick modes)

### CI/CD Integration

- **.github/workflows/benchmarks.yml** - GitHub Actions workflow for automated benchmarking on PRs

### Helper Scripts

- **compare-benchmarks.ps1** - PowerShell script to compare baseline vs current results
- **create-baseline.sh** - Bash script to create performance baseline

## 🚀 Quick Start

### 1. Create a Baseline (Before Making Changes)

```bash
cd JsonDdm.Benchmarks
./create-baseline.sh
```

This will:

- Build in Release mode
- Run all benchmarks
- Save results to `baseline-results/`
- Take 5-10 minutes to complete

### 2. Run Quick Benchmarks During Development

```bash
cd JsonDdm.Benchmarks
dotnet run -c Release -- --job short --filter "*MergeBenchmarks*"
```

### 3. After Making Changes, Compare Results

```bash
# Run current benchmarks
dotnet run -c Release -- --exporters json --artifacts ./current-results

# Compare with baseline
pwsh ./compare-benchmarks.ps1 -BaselineDir ./baseline-results -CurrentDir ./current-results
```

The script will show:

- ✅ **Improvements** (faster/less memory)
- ❌ **Regressions** (slower/more memory)
- Summary with percentage changes

## 📊 Understanding Results

### Sample Output

```
| Method              | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|----------:|
| MergeSmallDocuments | 1.234 μs  | 0.015 μs | 0.014 μs | 0.0191 |     320 B |
| MergeLargeDocuments | 45.67 μs  | 0.234 μs | 0.219 μs | 2.5635 |   42968 B |
```

**Key Metrics:**

- **Mean** - Average time per operation
- **Error** - 99.9% confidence interval margin
- **StdDev** - Consistency of measurements
- **Gen0/1/2** - Garbage collection frequency
- **Allocated** - Memory allocated per operation

### What to Track

Based on code review priorities:

1. **ArrayReorderingBenchmarks** - Should improve significantly after O(n²) fix
   - Current: Quadratic growth with array size
   - Target: Linear growth

2. **CloneBenchmarks** - Should improve after reducing unnecessary clones
   - Watch both time and allocated memory

3. **DeepNestingBenchmarks** - Should stay stable after adding depth limits
   - Ensure limits don't add significant overhead

## 🔄 CI Integration

### Automatic PR Checks

The GitHub Actions workflow runs on every PR that changes library code:

1. Checks out `main` branch
2. Runs baseline benchmarks
3. Checks out PR branch
4. Runs current benchmarks
5. Uploads both as artifacts
6. Posts results as PR comment (optional)

### Viewing Results

After CI runs:

1. Go to PR → "Checks" tab
2. Click on "Performance Benchmarks" workflow
3. Download artifacts:
   - `benchmark-baseline-NNN`
   - `benchmark-current-NNN`
4. Compare locally or review in PR comment

### Manual CI Trigger

For comprehensive benchmarks (all test cases):

1. Go to Actions → "Performance Benchmarks"
2. Click "Run workflow"
3. Results saved for 90 days

## 📈 Performance Goals

### Based on Code Review

| Issue             | Current                | Target                       | Benchmark                 |
| ----------------- | ---------------------- | ---------------------------- | ------------------------- |
| O(n²) reordering  | ~500ms for 500 items   | <50ms                        | ArrayReorderingBenchmarks |
| Excessive cloning | High allocation        | 50% reduction                | CloneBenchmarks           |
| No depth limits   | Risk of stack overflow | Add limit with <10% overhead | DeepNestingBenchmarks     |

## 🛠️ Common Commands

```bash
# Run all benchmarks (full suite)
dotnet run -c Release

# Run specific benchmark class
dotnet run -c Release --filter "*ArrayReorderingBenchmarks*"

# Run specific method
dotnet run -c Release --filter "*MergeSmallDocuments*"

# Quick mode (fast feedback, less accurate)
dotnet run -c Release -- --job short

# Export multiple formats
dotnet run -c Release -- --exporters json html csv markdown

# List available benchmarks without running
dotnet run -c Release -- --list tree
```

## 📝 Best Practices

1. **Always run in Release mode** - Debug mode results are meaningless
2. **Close background apps** - Minimize interference
3. **Run multiple times** - BenchmarkDotNet handles this automatically
4. **Commit baselines** - Track performance over time
5. **Review before merging** - Check for regressions in PRs

## 🎯 Next Steps

1. **Create initial baseline:**

   ```bash
   cd JsonDdm.Benchmarks
   ./create-baseline.sh
   git add baseline-results/
   git commit -m "Add performance baseline"
   ```

2. **Implement optimizations** (per REVIEW.md priorities)

3. **Run comparison after each optimization:**

   ```bash
   dotnet run -c Release -- --exporters json --artifacts ./current-results
   pwsh ./compare-benchmarks.ps1 -BaselineDir ./baseline-results -CurrentDir ./current-results
   ```

4. **Update baseline** after verified improvements:
   ```bash
   rm -rf baseline-results/
   ./create-baseline.sh
   git commit -am "Update performance baseline after optimizations"
   ```

## 📚 Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/core/performance/)
- See [JsonDdm.Benchmarks/README.md](JsonDdm.Benchmarks/README.md) for detailed information

## ⚠️ Troubleshooting

**Issue:** Benchmark takes forever

- **Fix:** Use `--job short` for quick tests or filter to specific benchmarks

**Issue:** High variance in results

- **Fix:** Close background apps, run on dedicated hardware

**Issue:** OutOfMemoryException

- **Fix:** Reduce parameter values in benchmark attributes

**Issue:** Can't compare results

- **Fix:** Ensure both runs exported JSON format (`--exporters json`)
