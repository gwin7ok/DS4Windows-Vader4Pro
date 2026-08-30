$ErrorActionPreference = 'Stop'
$OutputEncoding = [System.Text.UTF8Encoding]::new()
[Console]::OutputEncoding = $OutputEncoding

dotnet publish ./DS4Windows/DS4WinWPF.csproj `
    -c Release `
    /p:platform=x64 `
    /p:AppendTargetFrameworkToOutputPath=false `
    -o ./DS4Windows/bin/x64/Release

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'ビルドに成功しました' -ForegroundColor Green
