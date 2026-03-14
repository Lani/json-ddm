# GitHub Actions Scripts

This directory contains scripts used by GitHub Actions workflows.

## compare-benchmarks.js

Compares BenchmarkDotNet results between baseline and current runs, generating a markdown comparison table.

### Usage

```bash
node compare-benchmarks.js <baselinePath> <currentPath> [outputFile]
```

**Arguments:**

- `baselinePath` - Path to directory containing baseline benchmark results
- `currentPath` - Path to directory containing current benchmark results
- `outputFile` - Optional path to write markdown output (prints to stdout if omitted)

**Example:**

```bash
# Compare baseline to current results
node .github/scripts/compare-benchmarks.js \
  JsonDdm.Benchmarks/baseline-results/results \
  JsonDdm.Benchmarks/current-results/results \
  comparison.md

# Print to stdout
node .github/scripts/compare-benchmarks.js \
  JsonDdm.Benchmarks/baseline-results/results \
  JsonDdm.Benchmarks/current-results/results
```

### Features

- Automatically finds BenchmarkDotNet JSON result files
- Compares mean execution times between baseline and current
- Generates markdown table with:
  - Baseline and current performance metrics
  - Absolute and percentage differences
  - Visual status indicators (✅ improved, ❌ regressed, 🆕 new)
- Handles missing baseline data gracefully
- Provides detailed debug output to stderr

### Output Format

The script generates a markdown table like:

| Benchmark | Baseline | Current | Difference | Change  | Status |
| --------- | -------- | ------- | ---------- | ------- | ------ |
| Test1     | 10.5 μs  | 9.2 μs  | -1.3 μs    | -12.38% | ✅     |
| Test2     | N/A      | 15.3 μs | N/A        | N/A     | 🆕     |

### Exit Codes

- `0` - Success
- `1` - Error (missing arguments, file not found, parse error, etc.)

## post-pr-comment.js

Posts or updates a GitHub PR comment with benchmark results. Automatically finds and updates existing comments to avoid duplicates.

### Usage

```bash
node post-pr-comment.js <markdownFile> <prNumber> <repoOwner> <repoName> [githubToken] [runId] [commitSha]
```

**Arguments:**

- `markdownFile` - Path to markdown file containing the comment body
- `prNumber` - Pull request number
- `repoOwner` - Repository owner (GitHub username or org)
- `repoName` - Repository name
- `githubToken` - GitHub API token (optional if GITHUB_TOKEN env var is set)
- `runId` - Optional workflow run ID for metadata
- `commitSha` - Optional commit SHA for metadata

**Environment Variables:**

- `GITHUB_TOKEN` - GitHub API token (alternative to passing as argument)

**Example:**

```bash
# Post or update PR comment
node .github/scripts/post-pr-comment.js \
  /tmp/benchmark-comment.md \
  123 \
  myorg \
  myrepo \
  $GITHUB_TOKEN \
  456789 \
  abc123def

# Using environment variable for token
export GITHUB_TOKEN="ghp_xxxxxxxxxxxx"
node .github/scripts/post-pr-comment.js \
  /tmp/benchmark-comment.md \
  123 \
  myorg \
  myrepo
```

### Features

- Creates new PR comment if none exists
- Updates existing comment (avoids duplicates)
- Uses HTML marker (`<!-- benchmark-results-comment -->`) to identify comments
- Uses native Node.js HTTPS (no external dependencies)
- Handles API errors gracefully
- Provides detailed debug output to stderr

### Exit Codes

- `0` - Success (comment created or updated)
- `1` - Error (missing token, API failure, file not found, etc.)
