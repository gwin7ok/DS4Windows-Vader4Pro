chcp 65001

# 出力先ファイルパスの定数定義
$OUTPUT_PATH = "G:\Cursor_Folder\DS4Windows-Vader4Pro\urls.txt"

# 一時的に浅いクローンを作成（ファイルの実体はダウンロードしないため高速です）
git clone --depth 1 -b For-DI-migration-work --filter=blob:none https://github.com/gwin7ok/DS4Windows-Vader4Pro.git temp_repo
Set-Location temp_repo

# 全ファイルのURL一覧を作成し、Raw URLに変換して出力
$urls = git ls-tree -r --name-only HEAD | ForEach-Object {
    # 1. 通常のblob用URLを作成
    $blobUrl = "https://github.com/gwin7ok/DS4Windows-Vader4Pro/blob/For-DI-migration-work/$_"
    
    # 2. Raw URLへ変換
    # 例: github.com/user/repo/blob/branch/path -> raw.githubusercontent.com/user/repo/refs/heads/branch/path
    $rawUrl = $blobUrl -replace "github\.com/([^/]+)/([^/]+)/blob/([^/]+)/", "raw.githubusercontent.com/`$1/`$2/refs/heads/`$3/"
    
    $rawUrl
}
$urls | Out-File -FilePath $OUTPUT_PATH -Encoding utf8

# 一時フォルダを削除して元の場所に戻る
Set-Location ..
Remove-Item -Recurse -Force temp_repo
Write-Host "完了: $($urls.Count) 件の Raw URL を $OUTPUT_PATH に保存しました。" -ForegroundColor Green