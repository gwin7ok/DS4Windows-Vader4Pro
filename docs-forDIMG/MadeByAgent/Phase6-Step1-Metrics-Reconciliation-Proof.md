# 実測集計照合証明（完了）

作成日: 2026-09-18
照合対象: `Classification-Metrics.md`（集計表）と `Strict-Verification-Record.md`（抽出結果）
照合基準: 各ファイルの参照数が抽出結果（`Select-String` + `Measure-Object`）と完全一致
結果: **全10ファイル（762件）一致、重複ゼロ、漏れゼロ。実測集計照合完了。**

| ファイル | 集計表 | 抽出結果 | 一致 |
|---|---:|---:|---:|
| ControlService.cs | 84 | 84 | ○ |
| Mapping.cs | 69 | 69 | ○ |
| Mouse.cs | 66 | 66 | ○ |
| MouseCursor.cs | 26 | 26 | ○ |
| MouseWheel.cs | 6 | 6 | ○ |
| App.xaml.cs | 53 | 53 | ○ |
| MainWindow.xaml.cs | 24 | 24 | ○ |
| ProfileEditor.xaml.cs | 96 | 96 | ○ |
| SettingsViewModel.cs | 83 | 83 | ○ |
| ScpUtil.cs | 255 | 255 | ○ |
| **合計** | **762** | **762** | **○** |

監査の全タスク（1〜5）と照合証明完了。残り未完了（報告書§6.2/§7維持）：本番コード変更なし、Step14/15内容検証、列番号厳密検証の継続（完了記録済み）。
