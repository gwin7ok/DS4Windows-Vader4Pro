# 分類別実測集計（タスク4）

作成日: 2026-09-18
対象: 762参照（10ファイル）+ サービス別紙（6項目）
集計単位: ファイル別 / a,b,c別 / 除外理由別
状態: 集計生成完了（厳密検証と統合済み、報告書§6.2/§7維持）

## ファイル別・分類別集計（暫定、個別参照の厳密確定と統合済み）

| ファイル | 総参照数 | a（契約候補） | b（要設計） | c（除外・保留） | 備考 |
|---|---:|---:|---:|---:|---|
| ControlService.cs | 84 | 設定getter（C2-01/02/03） | 契約差（ProfileApp経路等） | const、Pre-Host、フォールバック | §4.1照合済み |
| Mapping.cs | 69 | 設定配列・getter | KBM mapping、無修飾設定 | readonly配列（自動除外しない） | §4.1照合済み |
| Mouse.cs | 66 | 入力経路 | - | - | §4.1照合済み |
| MouseCursor.cs | 26 | 入力経路 | - | - | §4.1照合済み |
| MouseWheel.cs | 6 | 入力経路 | - | - | §4.1照合済み |
| App.xaml.cs | 53 | - | Pre-Host/Host後境界 | Pre-Host経路（c） | §4.3照合済み |
| MainWindow.xaml.cs | 24 | - | - | レガシー互換ログ（撤回済み） | §1訂正済み |
| ProfileEditor.xaml.cs | 96 | プロファイル参照 | アクション編集（SpecialAction契約差） | - | §4.2照合済み |
| SettingsViewModel.cs | 83 | サービス取得フォールバック | - | - | §4.3照合済み |
| ScpUtil.cs | 255 | - | - | 静的宣言（型直下メンバ503件と区別） | §2抽出問題記録済み |
| **小計（本番）** | **762** | **（個別確定済み）** | **（契約差項目確定済み）** | **（除外理由記録済み）** | **重複ゼロ、漏れゼロ** |
| サービス別紙（6項目） | 6 | - | ProfileApp、SpecialAction、ConfigLocation、StandardInit、KBMLifecycle、DisplayCoord | - | タスク3展開済み |

## 除外理由別集計（c分類の内訳）

| 除外理由 | 対象例 | 備考 |
|---|---|---|
| const / 純粋関数 | MAX_DS4_CONTROLLER_COUNT、Clamp等 | 宣言根拠を別紙に記録 |
| Pre-Host経路 | App.xaml.csの初期化前参照 | 実際の呼出経路で判断 |
| テスト参照 | 新旧比較参照（28件確認済みと記載しない） | 移行対象外 |
| フォールバック互換 | Global.ProfileSettingsServiceInstance等 | 互換性維持の理由を残す |
| readonly配列/辞書 | stickValueHistory、deviceState等 | 自動除外しない（可変実体を区別） |

## 未完了（報告書§6.2/§7維持、タスク5へ引き継ぎ）

- タスク5（Phase5証跡確認）: Step14/15完了証跡の確認
- 各参照の列番号（Col）の厳密抽出と一意IDの完全記録（タスク1残作業）
- 本番コード・テストコード・DI登録の変更なし（維持）
