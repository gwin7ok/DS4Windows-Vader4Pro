$ErrorActionPreference = 'Stop'
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
foreach ($r in 'Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll','System.Collections.Immutable.dll') {
    [void][System.Reflection.Assembly]::LoadFrom("$PSHOME\$r")
}
$refDir = (Get-ChildItem 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref' | Sort-Object Name -Descending | Select-Object -First 1).FullName
$refDir = Join-Path $refDir 'ref\net9.0'
if (-not (Test-Path $refDir)) { $refDir = Get-ChildItem -Directory (Join-Path (Split-Path $refDir) '*') | Select-Object -First 1 -ExpandProperty FullName }
$src = [System.IO.File]::ReadAllText("$sp\TypeSplitter.cs")
$tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($src)
$refs = New-Object System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]
foreach ($f in (Get-ChildItem $refDir -Filter *.dll)) {
    $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($f.FullName))
}
$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile("$PSHOME\Microsoft.CodeAnalysis.dll"))
$refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile("$PSHOME\Microsoft.CodeAnalysis.CSharp.dll"))
$opts = New-Object Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('TypeSplitterAsm', [Microsoft.CodeAnalysis.SyntaxTree[]]@($tree), [Microsoft.CodeAnalysis.MetadataReference[]]$refs.ToArray(), $opts)
$out = "$sp\TypeSplitterAsm.dll"
$fs = [System.IO.File]::Create($out)
try { $res = $comp.Emit($fs) } finally { $fs.Dispose() }
if (-not $res.Success) {
    $res.Diagnostics | Where-Object { $_.Severity -eq 'Error' } | ForEach-Object { $_.ToString() }
    throw 'compile failed'
}
"built: $out (refs: $refDir)"
