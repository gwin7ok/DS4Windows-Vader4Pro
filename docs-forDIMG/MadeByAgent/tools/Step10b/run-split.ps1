param(
    [Parameter(Mandatory = $true)][string]$ListFile,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$repo = 'G:\Cursor_Folder\DS4Windows-Vader4Pro'
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
foreach ($r in 'Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll','System.Collections.Immutable.dll') {
    try { [void][System.Reflection.Assembly]::LoadFrom("$PSHOME\$r") } catch {}
}
if (-not ('TypeSplitter' -as [type])) {
    [void][System.Reflection.Assembly]::LoadFrom("$sp\TypeSplitterAsm.dll")
}
$files = Get-Content $ListFile | Where-Object { $_.Trim() -ne '' -and -not $_.StartsWith('#') }
$totalNew = 0; $problems = 0
$report = New-Object System.Collections.Generic.List[string]
foreach ($rel in $files) {
    $path = Join-Path $repo $rel
    if (-not (Test-Path $path)) { $report.Add("MISSING  $rel"); $problems++; continue }
    $plan = [TypeSplitter]::MakePlan($path)
    if ($plan.Issues.Count -gt 0) {
        $problems++
        $report.Add("ISSUE    $rel")
        foreach ($i in $plan.Issues) { $report.Add("            - $i") }
        continue
    }
    $kept = $plan.KeptType
    $mode = if ($plan.DeleteSource) { "元ファイルを削除し全型を別名へ（最大の型: $kept）" } else { "元ファイルに残す型: $kept" }
    $report.Add("PLAN     $rel : $($plan.Outputs.Count) 型。$mode")
    foreach ($o in $plan.Outputs) {
        $outRel = $o.Path.Substring($repo.Length + 1).Replace('\', '/')
        $tag = if ($o.Path -ceq $plan.Source) { 'keep' } else { 'new ' }
        $report.Add("            $tag $outRel")
        if ($tag -eq 'new ') { $totalNew++ }
    }
    if ($Apply) {
        $errs = [TypeSplitter]::Apply($plan)
        if ($errs.Count -gt 0) {
            $problems++
            $report.Add("VERIFY-NG $rel")
            foreach ($e in $errs) { $report.Add("            - $e") }
        } else {
            $report.Add("VERIFY-OK $rel")
        }
    }
}
$report | ForEach-Object { $_ }
"----"
"files=$($files.Count) newOrWritten=$totalNew problems=$problems apply=$($Apply.IsPresent)"
