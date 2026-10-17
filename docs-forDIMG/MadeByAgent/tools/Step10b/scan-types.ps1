param([string]$Root, [string]$Out)

# トップレベル型を数える簡易スキャナ（コメント・文字列・文字リテラルを除去し、波括弧の深さで判定）
function Remove-CodeNoise([string]$t) {
    $sb = New-Object System.Text.StringBuilder
    $i = 0; $n = $t.Length
    while ($i -lt $n) {
        $c = $t[$i]
        $d = if ($i + 1 -lt $n) { $t[$i + 1] } else { [char]0 }
        if ($c -eq '/' -and $d -eq '/') {
            while ($i -lt $n -and $t[$i] -ne "`n") { $i++ }
            continue
        }
        if ($c -eq '/' -and $d -eq '*') {
            $i += 2
            while ($i + 1 -lt $n -and -not ($t[$i] -eq '*' -and $t[$i + 1] -eq '/')) {
                if ($t[$i] -eq "`n") { [void]$sb.Append("`n") }
                $i++
            }
            $i += 2
            continue
        }
        if ($c -eq '@' -and $d -eq '"') {
            $i += 2
            while ($i -lt $n) {
                if ($t[$i] -eq '"') {
                    if ($i + 1 -lt $n -and $t[$i + 1] -eq '"') { $i += 2; continue }
                    break
                }
                if ($t[$i] -eq "`n") { [void]$sb.Append("`n") }
                $i++
            }
            $i++
            [void]$sb.Append('""')
            continue
        }
        if ($c -eq '$' -and $d -eq '"') { $i++; continue }
        if ($c -eq '"') {
            $i++
            while ($i -lt $n) {
                if ($t[$i] -eq '\') { $i += 2; continue }
                if ($t[$i] -eq '"') { break }
                $i++
            }
            $i++
            [void]$sb.Append('""')
            continue
        }
        if ($c -eq "'") {
            $i++
            while ($i -lt $n) {
                if ($t[$i] -eq '\') { $i += 2; continue }
                if ($t[$i] -eq "'") { break }
                $i++
            }
            $i++
            [void]$sb.Append("' '")
            continue
        }
        [void]$sb.Append($c)
        $i++
    }
    return $sb.ToString()
}


$results = @()
$files = Get-ChildItem -Path $Root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' -and $_.FullName -notmatch '\\externals\\' -and $_.Name -notmatch '\.(g|g\.i|Designer)\.cs$' }

foreach ($f in $files) {
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $code = Remove-CodeNoise $text
    $fileScoped = $code -match '(?m)^\s*namespace\s+[\w\.]+\s*;'
    $types = New-Object System.Collections.Generic.List[string]
    $depth = 0
    $lines = $code -split "`n"
    $hasNs = $code -match '(?m)^\s*namespace\s+[\w\.]+'
    $nsDepth = if ($fileScoped -or -not $hasNs) { 0 } else { 1 }
    foreach ($line in $lines) {
        $trim = $line.Trim()
        if ($depth -eq $nsDepth -and $trim.Length -gt 0) {
            # 属性の行は読み飛ばし、宣言の行を判定
            $m = [regex]::Match($trim, '^(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|unsafe|readonly|new)\s+)*(class|struct|interface|enum|record(?:\s+struct)?|delegate)\s+(.*)$')
            if ($m.Success) {
                $kind = $m.Groups[1].Value
                $rest = $m.Groups[2].Value
                if ($kind -eq 'delegate') {
                    $dm = [regex]::Match($rest, '(\w+)\s*(?:<[^>]*>)?\s*\(')
                    if ($dm.Success) { $types.Add("delegate:" + $dm.Groups[1].Value) }
                } else {
                    $nm = [regex]::Match($rest, '^(\w+)')
                    if ($nm.Success) { $types.Add($kind + ":" + $nm.Groups[1].Value) }
                }
            }
        }
        foreach ($ch in $line.ToCharArray()) {
            if ($ch -eq '{') { $depth++ } elseif ($ch -eq '}') { $depth-- }
        }
    }
    $rel = $f.FullName.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
    if ($stem.EndsWith('.xaml')) { $stem = $stem.Substring(0, $stem.Length - 5) }
    $results += [pscustomobject]@{
        File  = $rel
        Lines = $lines.Count
        Count = $types.Count
        Types = ($types -join ', ')
        Stem  = $stem
    }
}

$results | ConvertTo-Json -Depth 3 | Set-Content -Path $Out -Encoding UTF8
"files=" + $results.Count
