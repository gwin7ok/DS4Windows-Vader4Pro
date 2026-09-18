# Phase6-Step1 段階的監査集計（中間）

作成日: 2026-09-18
対象ブランチ: For-DI-migration-work
状態: 全件分類監査・分類別実測集計は未完了（段階的進行中）

## 完了済み（修飾参照の行数集計のみ、a/b/c分類は未確定）

| Phase | 対象ファイル | 行数 | 備考 |
|---|---|---:|---|
| 1 | ControlService.cs | 84 | 修飾参照候補 |
| 2 | Mapping.cs | 69 | 無修飾設定参照含む |
| 3 | Mouse.cs | 66 | |
| 3 | MouseCursor.cs | 26 | |
| 3 | MouseWheel.cs | 6 | |
| 4 | App.xaml.cs | 53 | UI層 |
| 5 | MainWindow.xaml.cs | 24 | UI層 |
| 5 | ProfileEditor.xaml.cs | 96 | UI層 |
| 5 | SettingsViewModel.cs | 83 | UI層 |
| 6 | ScpUtil.cs | 255 | コア（最大） |
| **小計** | **10ファイル** | **762** | 全対象網羅完了（行数のみ） |

## 次の対象（未実施）

- UI層: App.xaml.cs (55), MainWindow.xaml.cs (24), ProfileEditor.xaml.cs (102), SettingsViewModel.cs (92) → 計273行
- ScpUtil.cs: 259行
- サービス別紙展開（未検証メモの個別参照化）
- 全件a/b/c分類確定、除外理由記録、ホットパス判定
- 分類別実測集計（ファイル別・a/b/c別・除外別）の生成と照合

## 制約（報告書§6.2/§7より維持）

- 旧暫定数（193件等）は今回の実測として転記しない。
- 1,337出現回数（§6.1）はDI移行対象件数ではない（コメント・文字列含む）。
- 本番コード・テストコード・DI登録は変更していない。
- ビルド・dotnet test・実機検証は未実施。
