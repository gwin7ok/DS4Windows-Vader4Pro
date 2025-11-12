param(
    [Parameter(Mandatory=$true)][string]$PublishDir,
    [Parameter(Mandatory=$false)][string]$OutDir = '.'
)

# Ensure working directory is repository root (task runs with cwd set)
Write-Host "post-build: PublishDir=$PublishDir OutDir=$OutDir"

$propsPath = Join-Path (Get-Location) 'Directory.Build.props'
if (-not (Test-Path $propsPath)) {
    Write-Error "Directory.Build.props not found at $propsPath"
    exit 1
}

$text = Get-Content -Raw -Path $propsPath
if ($text -match '<Version>(.*?)</Version>') {
    $v = $matches[1]
} else {
    Write-Error 'Version not found in Directory.Build.props'
    exit 1
}

Write-Host "Detected version: $v"

# Call python post-build script via Start-Process to avoid invocation quoting issues
$python = 'python'
$script = Join-Path (Get-Location) 'utils\post-build.py'
if (-not (Test-Path $script)) {
    Write-Error "Post-build script not found at $script"
    exit 1
}

Start-Process -FilePath $python -ArgumentList @($script, $PublishDir, $OutDir, $v) -NoNewWindow -Wait
exit $LASTEXITCODE
