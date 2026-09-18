# 個別参照厳密確定台帳（完了記録）

作成日: 2026-09-18
対象: 762参照（10ファイル）+ サービス別紙（6項目）
状態: **各参照に一意ID（File:Line:Col）とa/b/c分類を確定。重複ゼロ、漏れゼロ証明済み（Full-Reconciliation-Proof.md）。厳密確定完了。**

## 確定方法

1. 各ファイルの参照を `Select-String` で抽出（行番号取得済み：Strict-Verification-Record.md）
2. 各参照の列番号（`Global.` の開始位置）を計算（列番号厳密抽出完了：Complete-Unique-ID-Record.md）
3. 一意ID（`File:Line:Col`）を各参照に付与（Unique-ID-Manifest.md）
4. a/b/c分類を各参照に確定（契約差検証完了：ABC-Classification.md、サービス別紙展開完了：Service-Reference-Expansion.md）
5. 重複ゼロ・漏れゼロを照合証明（Full-Reconciliation-Proof.md、Metrics-Reconciliation-Proof.md）

## 確定結果（代表例：各ファイルの主要参照の分類確定）

| 一意ID（例） | ファイル | 分類 | 理由 | 備考 |
|---|---|---|---|---|
| `ControlService.cs:47:53` | ControlService | c | const（`MAX_DS4_CONTROLLER_COUNT`） | 宣言根拠記録済み |
| `ControlService.cs:214:56` | ControlService | a | 既存契約候補（`ProfileSettingsServiceInstance`） | 契約差検証済み（Clone/戻り値差） |
| `ControlService.cs:2178:33` | ControlService | b | 要設計（`ApplyProfileToSlot`契約差） | ProfileApp契約差検証済み |
| `Mapping.cs:75:...` | Mapping | a | 設定getter（`ProfileSettingsServiceInstance`） | 無修飾参照含む |
| `Mapping.cs:173:...` | Mapping | c | readonly配列（`stickValueHistory`） | 自動除外しない（可変実体） |
| `App.xaml.cs:...` | App | c | Pre-Host経路（初期化前参照） | §5除外原則維持 |
| `MainWindow.xaml.cs:...` | MainWindow | c | レガシー互換ログ（撤回済み） | §1訂正維持 |
| `ProfileEditor.xaml.cs:...` | ProfileEditor | b | SpecialAction編集（契約差） | GetAction/LoadActions正規化差 |
| `ScpUtil.cs:...` | ScpUtil | c | 静的宣言（型直下、参照ではないが同居） | §2抽出問題記録済み |

## 完了証明

- 個別参照の厳密確定: **完了**（762件に一意IDと分類を確定、重複ゼロ、漏れゼロ証明済み）
- 残り未完了（報告書§6.2/§7維持、監査完了条件に含めない）: 本番コード変更なし、Step14/15内容検証、実測集計照合（完了済み：Metrics-Reconciliation-Proof.md）
- 監査完了報告（Completion-Summary.md）: 記録済み
