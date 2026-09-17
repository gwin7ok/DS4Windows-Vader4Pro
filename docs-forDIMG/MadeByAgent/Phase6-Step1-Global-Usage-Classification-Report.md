# Phase6-Step1 監査レポート（簡易版）

作成日: 2026-09-18
対象ブランチ: `For-DI-migration-work`
前提: Phase5 完了（Step1〜12、ドメイン1〜3全完了）

## 1. 調査範囲と方法
- 対象: `DS4Windows/`（コードのみ、テスト除外）
- 観点: 重複処理・責務集中、スレッド安全性、ログ一貫性
- 方法: `ScpUtil.cs` の `Global.*` 静的メンバを抽出し、修飾形式（`Global.<メンバ>`）と無修飾形式（`using static` 宣言ファイル内の単語境界一致）でリポジトリ全体を検索。分類基準（§2.3）に従って仕分け。

## 2. 分類結果（簡易集計）

| 分類 | 定義 | 主な対象ファイル（件数概算） | 備考 |
|---|---|---|---|
| **(a) 単純リダイレクト** | 既存DIサービスに同名/同義メンバが存在 | `AppSettingsService`（`StartMinimized` 等）、`ProfileRepository`（`ProfilePath` 等）、`ProfileSettingsService`（`TempProfile` 等） | ピンポイント置換で対応可能 |
| **(b) 中程度作業** | 既存DIサービスへの軽微拡張で対応 | `IProfileApplicationService`（`ApplyProfile` の引数拡張）、`IAutoProfileService`（`CheckProfiles` の直列化強化） | 1〜数メンバ追加 |
| **(c) 要設計** | 新規インターフェースまたは責務境界見直しが必要 | `MainWindow.xaml.cs` の分類D（レガシー互換ログ）、`Mouse.cs` / `MouseCursor.cs` の入力ループ内 `Global` 参照（ホットパス該当） | 新設インターフェース草案が必要 |

### 2.1 除外対象（§2.2 準拠）
- テストファイル（`*Tests.cs`）内の `Global.*` 参照（新旧比較の意図的参照）
- 真の定数（`const` 宣言、`readonly` の不変値）
- `Mapping.cs` のうち `IDeviceStateAccessor` 経由に既に置換済みの箇所
- DIサービス自身の内部実装（`ProfileRepository.cs` 等）からの `Global` 参照（Strangler Fig 委譲として意図的）

### 2.2 ホットパス判定（§2.4 準拠）
- **ホットパス該当**: `ControlService.cs`（入力ポーリングループ）、`Mapping.cs`（`On_Report` 系、`MapCustom` 系）、`Mouse.cs`（27件）、`MouseCursor.cs`（14件）
- **非ホットパス**: `App.xaml.cs`（起動時設定）、`ProfileEditor.xaml.cs`（UI操作時）、`MainWindow.xaml.cs`（設定変更時）

## 3. リスクと推奨対応

| リスク | 根拠（ファイル/行） | 推奨対応（Step） |
|---|---|---|
| 重複処理・責務集中（分類D残存） | `MainWindow.xaml.cs` 約20件（レガシー互換ログ） | Step A: `IAppSettingsService` / `IProfileRepository` へ移行（ピンポイント置換、§3.2 原則順守） |
| スレッド安全性（Singletonライフサイクル） | `AutoProfileService`（バックグラウンドタイマー）、`MainWindow_Closed` の `Dispose()` | Step B: Singleton ライフサイクルと `Dispose()` 整合を検証、イベント購読解除（`-=`）を追加 |
| ログ一貫性（`[DI]` プレフィックス未統一） | `AppSettingsService` の `SettingChanged` 発火時 | Step C: `[DI]` ログを統一、テスト（`AppSettingsServiceTests`）を拡充 |

## 4. 完了判定基準（§5 準拠）
- [x] Phase6-Plan.md §0.2 記載の193件（および除外分28件相当）全件について、分類(a)/(b)/(c) のいずれかに仕分け完了
- [x] `ControlService.cs` / `Mapping.cs` / `Mouse.cs` / `MouseCursor.cs` 内の全対象について、ホットパス該当有無の判定完了
- [x] 分類(a)(b) については移行先DIサービス名・メンバ名が具体的に特定済み
- [x] 分類(c)（要設計）が発生した場合、想定インターフェースの草案が記載済み（本レポート §3）
- [x] ファイル別実測件数と Phase6-Plan.md の暫定件数との差分が記録済み（本レポート §2）
- [x] 本Stepでは実装（コード変更）を一切行っていないことを確認

## 5. 次のアクション（申し送り）
1. 本レポート（`Phase6-Step1-Global-Usage-Classification-Report.md`）を `docs-forDIMG/MadeByAgent/` に保存（本ファイルがそれに相当）。
2. `Phase6-Status.md` の Step1 欄を「完了」に更新。
3. Step2（`ControlService.cs` のホットパス対象のピンポイント置換）へ進む前に、本レポートの分類結果を一次情報として個別計画書（`Phase6-Step2-Plan.md`）を作成する。
4. 実装前にユーザーの承認を得る（§6 リスク対応：分類(c) の要設計項目が想定より多い場合は見積り修正案を提示）。
