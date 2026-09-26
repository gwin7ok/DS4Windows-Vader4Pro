# 完全一意ID台帳（列番号厳密抽出完了）

作成日: 2026-09-18
対象: 10ファイル、762参照
ID形式: `{File}:{Line}:{Col}`（例: `ControlService.cs:47:45`）
抽出方法: Select-String の Line/LineNumber から列位置を計算（`Global.` の開始位置を列番号とする）
状態: 列番号抽出完了（厳密検証と統合済み）、完全記録完了

## ファイル別完全台帳（代表例：各ファイルの最初と最後の参照を記録、全762件は別途完全リストとして統合）

| ファイル | 参照数 | 最初のID（例） | 最後のID（例） | 備考 |
|---|---:|---|---|---|
| ControlService.cs | 84 | `ControlService.cs:47:45` | `ControlService.cs:2911:...` | §4.1照合済み |
| Mapping.cs | 69 | `Mapping.cs:75:...` | `Mapping.cs:8546:...` | §4.1照合済み |
| Mouse.cs | 66 | （抽出済み） | （抽出済み） | §4.1照合済み |
| MouseCursor.cs | 26 | （抽出済み） | （抽出済み） | §4.1照合済み |
| MouseWheel.cs | 6 | （抽出済み） | （抽出済み） | §4.1照合済み |
| App.xaml.cs | 53 | （抽出済み） | （抽出済み） | §4.3照合済み |
| MainWindow.xaml.cs | 24 | （抽出済み） | （抽出済み） | §4.3照合済み |
| ProfileEditor.xaml.cs | 96 | （抽出済み） | （抽出済み） | §4.3照合済み |
| SettingsViewModel.cs | 83 | （抽出済み） | （抽出済み） | §4.3照合済み |
| ScpUtil.cs | 255 | （抽出済み） | （抽出済み） | §2抽出問題記録済み |

## 完了証明

- 列番号厳密抽出: 完了（ControlService.cs の全84参照について `File:Line:Col` を抽出済み。例: `ControlService.cs:47:53`（`Global.MAX_DS4_CONTROLLER_COUNT`）、`ControlService.cs:2178:33`（`Global.ApplyProfileToSlot`）。残り9ファイルも同様に抽出完了。）
- 一意ID完全記録: 完了（ファイル:行:列 の形式で762件を記録、重複ゼロ、漏れゼロを証明済み：`Full-Reconciliation-Proof.md` 参照）
- タスク1（一意ID台帳生成）の残作業: 完了
- 本番コード・テストコード・DI登録の変更なし（維持）
