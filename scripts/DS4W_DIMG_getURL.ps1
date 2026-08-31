# 出力先ファイルパスの定数定義
$OUTPUT_PATH = "G:\Cursor_Folder\DS4Windows-Vader4Pro\urls.txt"

# 一時的に浅いクローンを作成（ファイルの実体はダウンロードしないため高速です）
git clone --depth 1 -b For-DI-migration-work --filter=blob:none https://github.com/gwin7ok/DS4Windows-Vader4Pro.git temp_repo
Set-Location temp_repo

# 全ファイルのURL一覧を作成して出力
$urls = git ls-tree -r --name-only HEAD | ForEach-Object {
    "https://github.com/gwin7ok/DS4Windows-Vader4Pro/blob/For-DI-migration-work/$_"
}
$urls | Out-File -FilePath $OUTPUT_PATH -Encoding utf8

# 一時フォルダを削除して元の場所に戻る
Set-Location ..
Remove-Item -Recurse -Force temp_repo
Write-Host "完了: $($urls.Count) 件のURLを $OUTPUT_PATH に保存しました。" -ForegroundColor Green