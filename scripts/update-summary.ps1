param(
    [string]$Summary,
    [string]$FilePath
)

if ([string]::IsNullOrEmpty($FilePath)) {
    $FilePath = Join-Path $PSScriptRoot "..\DS4Windows\build_timestamp.txt"
    $FilePath = [System.IO.Path]::GetFullPath($FilePath)
}

if ([string]::IsNullOrEmpty($Summary)) {
    Write-Host "Enter new summary text (single line). Press Enter when done:" -ForegroundColor Yellow
    $Summary = Read-Host
}

try {
    $dir = [System.IO.Path]::GetDirectoryName($FilePath)
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $FilePath -Value $Summary -Encoding UTF8
    Write-Host "Wrote summary to: $FilePath" -ForegroundColor Green
}
catch {
    Write-Error "Failed to write summary: $_"
    exit 1
}
