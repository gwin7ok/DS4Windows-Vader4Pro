Param(
    [switch]$WithV,
    [string]$Remote = 'origin',
    [switch]$DryRun,
    [switch]$SkipBuild,
    [string]$TokenEnvVar = 'GITHUB_TOKEN'
)

Set-StrictMode -Version Latest

# repo info
$repoOwner = 'gwin7ok'
$repoName = 'DS4Windows-Vader4Pro'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$repoRoot = (Resolve-Path -Path (Join-Path $scriptDir "..")).ProviderPath
Set-Location $repoRoot

$propsPath = Join-Path $repoRoot 'Directory.Build.props'
if (-not (Test-Path $propsPath)) { Write-Error "Directory.Build.props not found at $propsPath"; exit 2 }

$xml = [xml](Get-Content -Path $propsPath -ErrorAction Stop)
$version = $xml.Project.PropertyGroup.Version
if (-not $version) {
    $node = $xml.SelectSingleNode('//Version')
    if ($node) { $version = $node.'#text' }
}
if (-not $version) { Write-Error 'No <Version> element found'; exit 3 }

$tag = if ($WithV) { "v$version" } else { "$version" }
Write-Host "Preparing release for $tag (version $version)"

function Find-ZipsForVersion {
    param([string]$ver)
    Get-ChildItem -Path $repoRoot -Recurse -Filter "DS4Windows_${ver}_*.zip" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
}

if (-not $SkipBuild) {
    Write-Host "Building x64..."
    if ($DryRun) { Write-Host "[DryRun] dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 -o ./DS4Windows/bin/x64/Release/net8.0-windows" }
    else {
        & dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 -o ./DS4Windows/bin/x64/Release/net8.0-windows
        if ($LASTEXITCODE -ne 0) { Write-Error 'dotnet publish x64 failed'; exit 4 }
    }
    Write-Host "Running post-build for x64..."
    if ($DryRun) { Write-Host "[DryRun] pwsh ./scripts/post-build.ps1 ./DS4Windows/bin/x64/Release/net8.0-windows ." }
    else { pwsh -NoProfile -File ./scripts/post-build.ps1 ./DS4Windows/bin/x64/Release/net8.0-windows .; if ($LASTEXITCODE -ne 0) { Write-Error 'post-build x64 failed'; exit 5 } }

    # x86 build removed: repository no longer produces x86 artifacts.
    # Previously this script built/published x86 and ran post-build for x86;
    # that logic has been intentionally removed to keep releases x64-only.
}

# locate zips (ensure array)
$zips = @(Find-ZipsForVersion -ver $version)
if ($zips.Count -eq 0) {
    Write-Warning "No zip artifacts found for version $version. Continuing — you can upload manually later."
} else {
    Write-Host "Found artifact(s):"; $zips | ForEach-Object { Write-Host "  $_" }
}

# Git tag handling (local + remote delete/create/push)
if ($DryRun) {
    Write-Host "\n[DryRun] Would delete local tag if exists, delete remote tag, create annotated tag and push: $tag" 
} else {
    $localTag = git tag -l $tag 2>$null
    $localExists = -not [string]::IsNullOrWhiteSpace(($localTag | Out-String).Trim())
    if ($localExists) { Write-Host "Deleting local tag $tag"; git tag -d $tag }
    Write-Host "Deleting remote tag $tag (ignore errors)"; git push $Remote --delete $tag 2>$null
    Write-Host "Creating annotated tag $tag"; git tag -a $tag -m "Release $tag" HEAD; if ($LASTEXITCODE -ne 0) { Write-Error 'git tag creation failed'; exit 8 }
    Write-Host "Pushing tag $tag to $Remote"; git push $Remote $tag; if ($LASTEXITCODE -ne 0) { Write-Error 'git push failed'; exit 9 }
}

## Prefer using gh CLI (uses user's authenticated session) when available.
$useGh = $false
try {
    $ghCmd = Get-Command gh -ErrorAction Stop
    $useGh = $true
} catch {
    $useGh = $false
}

if ($useGh) {
    # Check authentication
    $ghAuthOk = $false
    try {
        gh auth status --hostname github.com 2>$null | Out-Null
        $ghAuthOk = $true
    } catch {
        $ghAuthOk = $false
    }

    if ($DryRun) {
        Write-Host "[DryRun] gh release view $tag (if exists)"
        Write-Host "[DryRun] gh release delete $tag --yes"
        if ($zips.Count -gt 0) {
            $zipArgs = $zips -join ' '
            Write-Host "[DryRun] gh release create $tag $zipArgs --title $tag --notes-file <tempfile>"
        } else {
            Write-Host "[DryRun] gh release create $tag --title $tag --notes-file <tempfile>"
        }
    } elseif ($ghAuthOk) {
        Write-Host "Using gh CLI (authenticated) to manage releases"
        # delete existing release if present
        $viewResult = & gh release view $tag 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Deleting existing release $tag..."
            & gh release delete $tag --yes
        } else { Write-Host "No existing gh release for $tag" }

        # write changelog to tempfile if present
        $changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
        $bodyFile = $null
        if (Test-Path $changelogPath) {
            $raw = Get-Content -Raw -Path $changelogPath
            $pattern = "(?ms)^## \[${version}\].*?(?=^## \[|\z)"
            $m = [regex]::Match($raw, $pattern)
            if ($m.Success) {
                $bodyFile = [System.IO.Path]::GetTempFileName()
                Set-Content -Path $bodyFile -Value $m.Value -Encoding UTF8
            } else { Write-Warning "CHANGELOG.md section for $version not found; release will have empty notes." }
        }

        # Create release with assets (gh can upload multiple assets)
        if ($zips.Count -gt 0) {
            $args = @('release','create',$tag) + $zips + @('--title',$tag)
            if ($bodyFile) { $args += @('--notes-file',$bodyFile) }
            Write-Host "gh $($args -join ' ')"
            & gh @args
            $exit = $LASTEXITCODE
        } else {
            $args = @('release','create',$tag,'--title',$tag)
            if ($bodyFile) { $args += @('--notes-file',$bodyFile) }
            Write-Host "gh $($args -join ' ')"
            & gh @args
            $exit = $LASTEXITCODE
        }

        if ($bodyFile -and (Test-Path $bodyFile)) { Remove-Item $bodyFile -ErrorAction SilentlyContinue }
        if ($exit -ne 0) { Write-Warning "gh release create exited with $exit" }
    } else {
        Write-Warning "gh CLI found but not authenticated. Please run 'gh auth login' to enable gh-based release operations, or provide a GITHUB_TOKEN environment variable. Falling back to API token mode."
        $useGh = $false
    }
}

if (-not $useGh) {
    # Fallback to token-based REST API
    $token = [Environment]::GetEnvironmentVariable($TokenEnvVar)
    if (-not $token) {
        Write-Warning "Environment variable '$TokenEnvVar' not set. You must set it to a token with repo permissions to create/delete releases and upload assets. Aborting upload steps."
        Write-Warning "Skipping GitHub release creation/upload because token not provided. If you only wanted tag recreation this completed above."
    } else {
        $authHeaders = @{ Authorization = "token $token"; 'User-Agent' = 'release-and-publish-script' }

        # Delete existing release for tag
        $getUri = "https://api.github.com/repos/$repoOwner/$repoName/releases/tags/$tag"
        try {
            if ($DryRun) { Write-Host "[DryRun] GET $getUri" } else { $resp = Invoke-RestMethod -Headers $authHeaders -Uri $getUri -Method Get -ErrorAction Stop }
        } catch {
            Write-Host ("No existing release for {0} or could not query releases: " -f $tag) -NoNewline; Write-Host $_.Exception.Message }

        if (-not $DryRun -and $resp) {
            $delUri = "https://api.github.com/repos/$repoOwner/$repoName/releases/$($resp.id)"
            Write-Host "Deleting existing release id $($resp.id) ..."
            try { Invoke-RestMethod -Headers $authHeaders -Uri $delUri -Method Delete -ErrorAction Stop; Write-Host 'Deleted previous release' } catch { Write-Warning ("Failed to delete release: " + $_.Exception.Message) }
        }

        # Build release body from CHANGELOG.md
        $changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
        $body = ""
        if (Test-Path $changelogPath) {
            $raw = Get-Content -Raw -Path $changelogPath
            $pattern = "(?ms)^## \[${version}\].*?(?=^## \[|\z)"
            $m = [regex]::Match($raw, $pattern)
            if ($m.Success) { $body = $m.Value } else { Write-Warning "CHANGELOG.md section for $version not found; using empty body." }
        } else { Write-Warning "CHANGELOG.md not found" }

        # Create release
        $createUri = "https://api.github.com/repos/$repoOwner/$repoName/releases"
        $payload = @{ tag_name = $tag; name = "$tag"; body = $body; draft = $false; prerelease = $false } | ConvertTo-Json
        if ($DryRun) { Write-Host "[DryRun] POST $createUri`nPayload: $payload" } else {
            try {
                $createResp = Invoke-RestMethod -Headers $authHeaders -Uri $createUri -Method Post -Body $payload -ContentType 'application/json' -ErrorAction Stop
                Write-Host "Created release id $($createResp.id)"
            } catch {
                Write-Error ("Failed to create release: " + $_.Exception.Message); exit 11
            }
        }

        # Upload assets
        if (-not $DryRun -and $createResp) {
            foreach ($zip in $zips) {
                $name = [System.IO.Path]::GetFileName($zip)
                $uploadUri = "https://uploads.github.com/repos/$repoOwner/$repoName/releases/$($createResp.id)/assets?name=$name"
                Write-Host "Uploading $name to $uploadUri"
                try {
                    Invoke-RestMethod -Headers $authHeaders -Uri $uploadUri -Method Post -InFile $zip -ContentType 'application/zip' -ErrorAction Stop
                    Write-Host "Uploaded $name"
                } catch {
                    Write-Warning ("Failed to upload " + $name + ": " + $_.Exception.Message)
                }
            }
        }
    }
}

Write-Host "Done"
