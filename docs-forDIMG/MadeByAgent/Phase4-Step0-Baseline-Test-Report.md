# Step0 基準ビルド・テスト・起動確認レポート

## 実行環境

- リポジトリ: `G:\Cursor_Folder\DS4Windows-Vader4Pro`
- 対象ブランチ: `For-DI-migration-work`（計画書記載値）
- 実施日: 2026-08-31
- コード変更: なし（Step0の調査資料のみ追加）

## ビルド結果

実行コマンド:

```text
dotnet build DS4WindowsWPF.sln --nologo
```

結果: 成功

- 警告: 0
- エラー: 0

初回はビルドとテストを並列実行したため、`DS4Windows.dll`への同時書込みでCS2012が発生した。その後、ビルドを単独で再実行して成功した。CS2012はソース起因ではなく、同一`obj`出力への並列アクセスによるファイルロックである。

## 自動テスト結果

### DS4WindowsTests

実行コマンド:

```text
dotnet test DS4WindowsTests/DS4Windows.Actions.Tests.csproj --nologo --no-restore
```

結果: 成功、31/31、失敗0、スキップ0

### StandaloneTests

実行コマンド:

```text
dotnet test StandaloneTests/StandaloneTests.csproj --nologo --no-restore
```

結果: 成功、13/13、失敗0、スキップ0

## 主要画面起動確認

本環境ではWPF実機GUI操作を自動確認していないため、以下は未実施とする。後続のローカル実機確認で記録する。

| 対象 | 結果 | 確認内容 |
|---|---|---|
| MainWindow | 未実施 | 起動、コントローラ一覧、ログ表示 |
| ProfileEditor | 未実施 | プロファイル読込、設定表示、保存 |
| Controller関連タブ | 未実施 | 接続表示、出力設定、デバイス操作 |
| UAC／外部プログラム | 未実施 | 昇格、LaunchProgram、多重起動防止 |

## 既存ログ確認

Step0ではログ機構を変更していない。起動ログの代表例はソース上、`App.xaml.cs`の以下の既存出力で確認できる。

- `Startup culture: CurrentCulture=..., CurrentUICulture=...`
- `DS4Windows version ...`
- `Logger created`

実機起動時の実出力値と時刻は、GUI起動確認を行う環境で追記する。

## 基準値

| 項目 | 基準値 |
|---|---|
| ソリューションビルド | 成功、警告0、エラー0 |
| `DS4Windows.Actions.Tests` | 31成功、0失敗 |
| `StandaloneTests` | 13成功、0失敗 |
| 主要画面・実機 | 未実施 |
| Step0コード変更 | なし |
