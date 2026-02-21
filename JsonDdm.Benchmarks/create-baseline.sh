#!/bin/bash
# Run benchmarks and save baseline results for future comparison

set -e

echo "======================================"
echo "Creating Performance Baseline"
echo "======================================"
echo ""

# Ensure we're in Release mode
echo "Building in Release mode..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo ""
echo "Running benchmarks..."
echo "This may take several minutes..."
echo ""

# Create baseline directory
BASELINE_DIR="./baseline-results"
mkdir -p $BASELINE_DIR

# Run benchmarks and export to baseline directory
# Note: BenchmarkDotNet expects space-separated exporters, not comma-separated
dotnet run -c Release -- \
    --exporters json markdown \
    --artifacts $BASELINE_DIR

if [ $? -ne 0 ]; then
    echo ""
    echo "======================================"
    echo "Benchmark run failed!"
    echo "======================================"
    exit 1
fi

echo ""
echo "======================================"
echo "Baseline created successfully!"
echo "======================================"
echo ""
echo "Results saved to: $BASELINE_DIR"
echo ""
echo "To compare future runs against this baseline:"
echo "  1. Run: dotnet run -c Release -- --exporters json --artifacts ./current-results"
echo "  2. Run: pwsh ./compare-benchmarks.ps1 -BaselineDir $BASELINE_DIR -CurrentDir ./current-results"
echo ""
echo "Or commit the baseline to git for CI comparison."
