#!/usr/bin/env node
/**
 * Compare benchmark results between baseline and current runs
 * Usage: node compare-benchmarks.js <baselinePath> <currentPath> [outputFile]
 *
 * Arguments:
 *   baselinePath - Path to baseline results directory
 *   currentPath - Path to current results directory
 *   outputFile - Optional markdown output file (defaults to stdout)
 */

const fs = require("fs");
const path = require("path");

function findJsonFile(dir) {
  try {
    if (!fs.existsSync(dir)) {
      console.error(`Directory not found: ${dir}`);
      return null;
    }
    const files = fs.readdirSync(dir);
    // Look for BenchmarkDotNet JSON result files
    const jsonFile = files.find(
      (f) =>
        f.endsWith("-full-compressed.json") ||
        (f.includes("Benchmarks") && f.endsWith(".json")),
    );
    return jsonFile ? path.join(dir, jsonFile) : null;
  } catch (e) {
    console.error(`Error reading directory ${dir}:`, e.message);
    return null;
  }
}

function loadBenchmarkData(filePath) {
  if (!filePath) return null;

  try {
    const content = fs.readFileSync(filePath, "utf8");
    const data = JSON.parse(content);
    console.error(`Loaded benchmark data from: ${filePath}`);
    console.error(`Found ${data.Benchmarks?.length || 0} benchmarks`);
    return data;
  } catch (e) {
    console.error(`Error loading ${filePath}:`, e.message);
    return null;
  }
}

function formatTime(ns) {
  if (ns >= 1e9) return `${(ns / 1e9).toFixed(2)} s`;
  if (ns >= 1e6) return `${(ns / 1e6).toFixed(2)} ms`;
  if (ns >= 1e3) return `${(ns / 1e3).toFixed(2)} μs`;
  return `${ns.toFixed(2)} ns`;
}

function generateComparisonMarkdown(baselineData, currentData) {
  let markdown = "## 📊 Benchmark Results\n\n";

  if (!currentData || !currentData.Benchmarks) {
    markdown += "⚠️ No current benchmark data found.\n";
    return markdown;
  }

  console.error(
    `\nGenerating comparison for ${currentData.Benchmarks.length} benchmarks`,
  );

  // Create comparison table
  markdown +=
    "| Benchmark | Baseline | Current | Difference | Change | Status |\n";
  markdown +=
    "|-----------|----------|---------|------------|--------|--------|\n";

  currentData.Benchmarks.forEach((current) => {
    const name = current.MethodTitle || current.Method || "Unknown";
    const currentMean = current.Statistics?.Mean || current.Mean || 0;

    console.error(`\nProcessing benchmark: ${name}`);
    console.error(`  Current mean: ${currentMean}`);

    let baseline = null;
    if (baselineData && baselineData.Benchmarks) {
      baseline = baselineData.Benchmarks.find(
        (b) => (b.MethodTitle || b.Method) === name,
      );
    }

    if (baseline) {
      const baselineMean = baseline.Statistics?.Mean || baseline.Mean || 0;
      const diff = currentMean - baselineMean;
      const percentChange =
        baselineMean > 0 ? ((diff / baselineMean) * 100).toFixed(2) : 0;

      console.error(`  Baseline mean: ${baselineMean}`);
      console.error(`  Difference: ${diff} (${percentChange}%)`);

      const status = diff <= 0 ? "✅" : "❌";
      const diffSign = diff > 0 ? "+" : "";
      const changeStr = `${diffSign}${percentChange}%`;

      markdown += `| ${name} | ${formatTime(baselineMean)} | ${formatTime(currentMean)} | ${diffSign}${formatTime(Math.abs(diff))} | ${changeStr} | ${status} |\n`;
    } else {
      console.error(`  No baseline found - new benchmark`);
      markdown += `| ${name} | N/A | ${formatTime(currentMean)} | N/A | N/A | 🆕 |\n`;
    }
  });

  // Add legend
  markdown += "\n### Legend\n\n";
  markdown += "- **Benchmark**: Name of the benchmark test\n";
  markdown +=
    "- **Baseline**: Performance on `main` branch (mean execution time)\n";
  markdown +=
    "- **Current**: Performance of current PR (mean execution time)\n";
  markdown +=
    "- **Difference**: Absolute time difference (Current - Baseline)\n";
  markdown += "- **Change**: Percentage change from baseline\n";
  markdown +=
    "- **Status**: ✅ Improved (faster) | ❌ Regressed (slower) | 🆕 New benchmark\n";
  markdown += "\n💡 _Lower execution time is better_\n";

  return markdown;
}

function main() {
  const args = process.argv.slice(2);

  if (args.length < 2) {
    console.error(
      "Usage: node compare-benchmarks.js <baselinePath> <currentPath> [outputFile]",
    );
    console.error("");
    console.error("Example:");
    console.error(
      "  node compare-benchmarks.js JsonDdm.Benchmarks/baseline-results JsonDdm.Benchmarks/current-results",
    );
    process.exit(1);
  }

  const [baselinePath, currentPath, outputFile] = args;

  console.error("=== Benchmark Comparison Tool ===");
  console.error(`Baseline path: ${baselinePath}`);
  console.error(`Current path: ${currentPath}`);
  console.error("");

  // Find JSON result files
  const baselineFile = findJsonFile(baselinePath);
  const currentFile = findJsonFile(currentPath);

  if (!currentFile) {
    console.error("❌ ERROR: No current benchmark results found!");
    console.error(`Searched in: ${currentPath}`);
    process.exit(1);
  }

  // Load benchmark data
  const baselineData = baselineFile ? loadBenchmarkData(baselineFile) : null;
  const currentData = loadBenchmarkData(currentFile);

  if (!currentData) {
    console.error("❌ ERROR: Could not load current benchmark data!");
    process.exit(1);
  }

  // Generate comparison markdown
  const markdown = generateComparisonMarkdown(baselineData, currentData);

  // Output result
  if (outputFile) {
    fs.writeFileSync(outputFile, markdown, "utf8");
    console.error(`\n✅ Comparison written to: ${outputFile}`);
  } else {
    console.log(markdown);
  }

  console.error("\n✅ Comparison complete!");
}

// Run if called directly
if (require.main === module) {
  try {
    main();
  } catch (error) {
    console.error("❌ Fatal error:", error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

module.exports = {
  generateComparisonMarkdown,
  loadBenchmarkData,
  findJsonFile,
};
