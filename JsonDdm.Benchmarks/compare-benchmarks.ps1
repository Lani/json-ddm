#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Compare benchmark results between two runs.

.DESCRIPTION
    This script compares benchmark results from baseline and current runs,
    highlighting performance improvements or regressions.

.PARAMETER BaselineDir
    Directory containing baseline benchmark results (JSON format)

.PARAMETER CurrentDir
    Directory containing current benchmark results (JSON format)

.PARAMETER Threshold
    Percentage threshold for reporting changes (default: 5%)

.EXAMPLE
    ./compare-benchmarks.ps1 -BaselineDir ./baseline-results -CurrentDir ./current-results
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BaselineDir,
    
    [Parameter(Mandatory=$true)]
    [string]$CurrentDir,
    
    [Parameter(Mandatory=$false)]
    [double]$Threshold = 5.0
)

function Get-BenchmarkResults {
    param([string]$Directory)
    
    $jsonFiles = Get-ChildItem -Path $Directory -Filter "*.json" -Recurse | 
        Where-Object { $_.Name -notlike "*-report-github.md" }
    
    if ($jsonFiles.Count -eq 0) {
        Write-Error "No JSON result files found in $Directory"
        return $null
    }
    
    # Use the most recent results file
    $latestFile = $jsonFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    Write-Host "Reading results from: $($latestFile.FullName)"
    
    $content = Get-Content $latestFile.FullName -Raw | ConvertFrom-Json
    return $content.Benchmarks
}

function Format-TimeSpan {
    param([double]$Nanoseconds)
    
    if ($Nanoseconds -lt 1000) {
        return "{0:F2} ns" -f $Nanoseconds
    } elseif ($Nanoseconds -lt 1000000) {
        return "{0:F2} μs" -f ($Nanoseconds / 1000)
    } elseif ($Nanoseconds -lt 1000000000) {
        return "{0:F2} ms" -f ($Nanoseconds / 1000000)
    } else {
        return "{0:F2} s" -f ($Nanoseconds / 1000000000)
    }
}

function Format-Bytes {
    param([long]$Bytes)
    
    if ($Bytes -lt 1024) {
        return "$Bytes B"
    } elseif ($Bytes -lt 1048576) {
        return "{0:F2} KB" -f ($Bytes / 1024)
    } else {
        return "{0:F2} MB" -f ($Bytes / 1048576)
    }
}

Write-Host "`n=== Benchmark Comparison ===" -ForegroundColor Cyan
Write-Host "Baseline: $BaselineDir"
Write-Host "Current:  $CurrentDir"
Write-Host "Threshold: $Threshold%`n"

$baseline = Get-BenchmarkResults -Directory $BaselineDir
$current = Get-BenchmarkResults -Directory $CurrentDir

if ($null -eq $baseline -or $null -eq $current) {
    exit 1
}

$improvements = @()
$regressions = @()
$noChange = @()

foreach ($currentBench in $current) {
    $methodName = $currentBench.Method
    $baselineBench = $baseline | Where-Object { $_.Method -eq $methodName }
    
    if ($null -eq $baselineBench) {
        Write-Host "⚠️  New benchmark: $methodName" -ForegroundColor Yellow
        continue
    }
    
    $baselineTime = $baselineBench.Statistics.Mean
    $currentTime = $currentBench.Statistics.Mean
    
    $changePercent = (($currentTime - $baselineTime) / $baselineTime) * 100
    
    $result = [PSCustomObject]@{
        Method = $methodName
        BaselineTime = $baselineTime
        CurrentTime = $currentTime
        ChangePercent = $changePercent
        BaselineMem = if ($baselineBench.Memory.BytesAllocatedPerOperation) { $baselineBench.Memory.BytesAllocatedPerOperation } else { 0 }
        CurrentMem = if ($currentBench.Memory.BytesAllocatedPerOperation) { $currentBench.Memory.BytesAllocatedPerOperation } else { 0 }
    }
    
    if ([Math]::Abs($changePercent) -lt $Threshold) {
        $noChange += $result
    } elseif ($changePercent -lt 0) {
        $improvements += $result
    } else {
        $regressions += $result
    }
}

# Report improvements
if ($improvements.Count -gt 0) {
    Write-Host "`n✅ IMPROVEMENTS ($($improvements.Count))" -ForegroundColor Green
    Write-Host ("{0,-50} {1,15} {1,15} {1,10}" -f "Method", "Baseline", "Current", "Change")
    Write-Host ("-" * 95)
    
    foreach ($item in $improvements | Sort-Object ChangePercent) {
        $baseTime = Format-TimeSpan $item.BaselineTime
        $currTime = Format-TimeSpan $item.CurrentTime
        $change = "{0:F1}%" -f $item.ChangePercent
        
        Write-Host ("{0,-50} {1,15} {2,15} {3,10}" -f `
            $item.Method, $baseTime, $currTime, $change) -ForegroundColor Green
        
        if ($item.CurrentMem -ne $item.BaselineMem) {
            $baseMem = Format-Bytes $item.BaselineMem
            $currMem = Format-Bytes $item.CurrentMem
            $memChange = (($item.CurrentMem - $item.BaselineMem) / $item.BaselineMem) * 100
            Write-Host ("  Memory: {0,15} -> {1,15} ({2:F1}%)" -f `
                $baseMem, $currMem, $memChange) -ForegroundColor DarkGreen
        }
    }
}

# Report regressions
if ($regressions.Count -gt 0) {
    Write-Host "`n❌ REGRESSIONS ($($regressions.Count))" -ForegroundColor Red
    Write-Host ("{0,-50} {1,15} {1,15} {1,10}" -f "Method", "Baseline", "Current", "Change")
    Write-Host ("-" * 95)
    
    foreach ($item in $regressions | Sort-Object ChangePercent -Descending) {
        $baseTime = Format-TimeSpan $item.BaselineTime
        $currTime = Format-TimeSpan $item.CurrentTime
        $change = "+{0:F1}%" -f $item.ChangePercent
        
        Write-Host ("{0,-50} {1,15} {2,15} {3,10}" -f `
            $item.Method, $baseTime, $currTime, $change) -ForegroundColor Red
        
        if ($item.CurrentMem -ne $item.BaselineMem) {
            $baseMem = Format-Bytes $item.BaselineMem
            $currMem = Format-Bytes $item.CurrentMem
            $memChange = (($item.CurrentMem - $item.BaselineMem) / $item.BaselineMem) * 100
            Write-Host ("  Memory: {0,15} -> {1,15} (+{2:F1}%)" -f `
                $baseMem, $currMem, $memChange) -ForegroundColor DarkRed
        }
    }
}

# Summary
Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "Improvements: $($improvements.Count)" -ForegroundColor Green
Write-Host "Regressions:  $($regressions.Count)" -ForegroundColor Red
Write-Host "No change:    $($noChange.Count)" -ForegroundColor Gray

# Exit with error if there are regressions
if ($regressions.Count -gt 0) {
    Write-Host "`n⚠️  Performance regressions detected!" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✅ No performance regressions detected." -ForegroundColor Green
exit 0
