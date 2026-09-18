# 一意ID台帳（タスク1）

作成日: 2026-09-18
対象: 10ファイル、762参照
ID形式: `{File}:{Line}:{Col}`（例: `ControlService.cs:47:45`）
状態: 台帳生成開始（全件網羅、厳密検証と並行）

## ファイル別参照数（再確認）

| ファイル | 参照数 | ID範囲（開始行） |
|---|---:|---|
| ControlService.cs | 84 | 47〜2911 |
| Mapping.cs | 69 | 75〜8546 |
| Mouse.cs | 66 | （抽出済み） |
| MouseCursor.cs | 26 | （抽出済み） |
| MouseWheel.cs | 6 | （抽出済み） |
| App.xaml.cs | 53 | （抽出済み） |
| MainWindow.xaml.cs | 24 | （抽出済み） |
| ProfileEditor.xaml.cs | 96 | （抽出済み） |
| SettingsViewModel.cs | 83 | （抽出済み） |
| ScpUtil.cs | 255 | （抽出済み） |

## 未完了

- 各参照の列番号（Col）の厳密抽出
- IDとa/b/c分類の対応付け（タスク2と統合）
- 重複・漏れのゼロ照合（別紙との突き合わせ完了済み、IDレベルは未完了）
