Param(
    [switch]$WithV,
    [string]$Remote = 'origin',
    [switch]$DryRun,
    [switch]$NoPush
)

# Resolve repository root relative to this script
$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$repoRoot = (Resolve-Path -Path (Join-Path $scriptDir "..")).ProviderPath
$propsPath = Join-Path $repoRoot 'Directory.Build.props'

if (-not (Test-Path $propsPath)) {
    Write-Error "Directory.Build.props not found at: $propsPath"
    exit 2
}

try {
    [xml]$xml = Get-Content -Path $propsPath -ErrorAction Stop
} catch {
    Write-Error "Failed reading $propsPath : $_"
    exit 2
}

$version = $null
try {
    # Try common paths for Version element
    $version = $xml.Project.PropertyGroup.Version
    if (-not $version) {
        # fallback: search for any Version element
        $versionNode = $xml.SelectSingleNode('//Version')
        if ($versionNode) { $version = $versionNode.'#text' }
    }
} catch {
    Write-Error "Failed parsing version from $propsPath : $_"
    exit 2
}

if (-not $version) {
    Write-Error "No <Version> element found in $propsPath"
    exit 3
}

$tag = if ($WithV) { "v$version" } else { "$version" }

Write-Host "Repository root: $repoRoot"
Write-Host "Using version: $version"
Write-Host "Tag to (re)create: $tag"

# Prepare planned commands for display
$planned = @()

# Check for local tag
$localTagList = & git tag -l $tag 2>$null
$localTagExists = ($localTagList -ne $null) -and (($localTagList | Out-String).Trim() -ne '')
if ($localTagExists) {
    $planned += "git tag -d $tag"
} else {
    Write-Host "Local tag '$tag' not present."
}

if (-not $NoPush) {
    # remote delete
    $planned += "git push $Remote --delete refs/tags/$tag  # delete remote tag if exists"
}

$planned += "git tag -a $tag -m 'Release $tag' HEAD"

if (-not $NoPush) {
    $planned += "git push $Remote refs/tags/$tag"
}

if ($DryRun) {
    Write-Host "\nDry run: the following commands would be executed (no changes will be made):"
    $planned | ForEach-Object { Write-Host "  $_" }
    exit 0
}

try {
    if ($localTagExists) {
        Write-Host "Deleting local tag $tag..."
        & git tag -d $tag
        if ($LASTEXITCODE -ne 0) { throw "git tag -d failed with exit $LASTEXITCODE" }
    }

    if (-not $NoPush) {
        Write-Host "Deleting remote tag $tag (if exists) on $Remote..."
        # git push --delete may fail if tag doesn't exist; ignore non-zero safely
        & git push $Remote --delete refs/tags/$tag 2>$null
    }

    Write-Host "Creating annotated tag $tag..."
    & git tag -a $tag -m "Release $tag" HEAD
    if ($LASTEXITCODE -ne 0) { throw "git tag creation failed with exit $LASTEXITCODE" }

    if (-not $NoPush) {
        Write-Host "Pushing tag $tag to $Remote..."
        & git push $Remote refs/tags/$tag
        if ($LASTEXITCODE -ne 0) { throw "git push failed with exit $LASTEXITCODE" }
    }

    Write-Host "Success: tag '$tag' created" -ForegroundColor Green
} catch {
    Write-Error "Operation failed: $_"
    exit 10
}
