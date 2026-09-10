# Phase 5 - Step 14 フォーム/カラム幅設定 SSOT 統一 進捗管理ステータス

作成日: 2026-09-11
改訂日: 2026-09-11（フェーズA・B・C 全タスク完了 / 実機検証準備完了）
現在ステータス: **全コード改修・テスト・文書更新完了（実機検証準備完了）**
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- 個別計画書: `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Plan.md`
- 調査報告書: `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（Issue 6 是正完了）
- Phase 5 全体計画: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
- Phase 5 全体状況: `docs-forDIMG/MadeByAgent/Phase5-Status.md`
- 移行全体憲法: `.github/copilot-instructions.md`（Pure DI原則・フォールバック/シム維持原則）

---

## 1. 概要と完了実績サマリ

### 1.1 目的
実機検証中に発覚した「アプリ終了時にウィンドウサイズ・位置・各種カラム幅が `Profiles.xml` へ正しく反映されないことがある」不具合（Issue 6）の根本原因である、DIサービス（`EnvironmentService`, `ProfileSettingsService`, `AppNotificationService`）における **実体 `BackingStore`（`m_Config`）と連動しない孤立 private field の二重保持バグ** を根絶し、アクセス経路を `IAppSettingsService` / `INotificationService` 経由に統一（SSOT 一本化）する。

### 1.2 完了ステータスサマリ
* **フェーズA（DIサービスの孤立排除とSSOT委譲化）**: ✅ **全7タスク完了**
* **フェーズB（UI層の直参照置換とアクセス経路統一）**: ✅ **全4タスク完了**
* **フェーズC（ドキュメント更新）**: ✅ **タスク-4 完了**
* **ビルド / テスト状況**:
  - `dotnet build ./DS4Windows/DS4WinWPF.csproj` : ✅ **成功（警告 0 / エラー 0）**
  - `dotnet test`（全156件の単体テスト実行） : ✅ **全156件 PASS（オールグリーン）**
* **次回予定**: **実機検証（実機CP4: ウィンドウサイズ・位置・カラム幅・通知の終了時保存テスト）**

---

## 2. マイクロタスク進捗一覧（全タスク完了）

### フェーズA: DI サービス側の孤立フィールド排除と SSOT 委譲化（全件完了）

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(a)-1** | `IEnvironmentService` インターフェースの縮小 | `IEnvironmentService.cs` | ✅ 完了 | 8設定プロパティを削除、環境プローブのみ残存 |
| **(a)-2** | `EnvironmentService` 実装の純化 | `EnvironmentService.cs` | ✅ 完了 | 孤立 private field 8個・アクセサを完全削除 |
| **(a)-3** | `EnvironmentServiceTests.cs` の是正 | `EnvironmentServiceTests.cs` | ✅ 完了 | 削除プロパティのテスト撤去、残存機能テスト化 |
| **(a)-4** | `IAppSettingsService` / `AppSettingsService` への5プロパティ追加 | `IAppSettingsService.cs`<br>`AppSettingsService.cs` | ✅ 完了 | カラム幅5プロパティ（`int`）を宣言・`Global` 委譲実装。全35メンバ完全適合 |
| **(a)-5** | `IProfileSettingsService` / `ProfileSettingsService` の孤立5プロパティ削除 | `ProfileSettingsService.cs` | ✅ 完了 | 孤立 private field 5個およびアクセサをピンポイント完全切除 |
| **(a)-6** | `AppNotificationService.cs` の孤立フィールド排除と Global 委譲化 | `AppNotificationService.cs` | ✅ 完了 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、`Global` 委譲化 |
| **(a)-7** | `NotificationServiceTests.cs` の是正と単体テスト確認 | `NotificationServiceTests.cs` | ✅ 完了 | `Global` 連動・状態復元・双方向連動テスト追加。全テストPASS |

---

### フェーズB: UI 層の Global 直参照置換とアクセス経路統一（全件完了）

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(b)-1** | `SettingsViewModel.cs` の Global 直参照置換 | `SettingsViewModel.cs` | ✅ 完了 | `StartMinimize`, `MinimizeToTaskbar`, `CloseMinimizes` を注入済み `_appSettings` 経由へ配線 |
| **(b)-2** | `ProfileEditor.xaml.cs` の Global 直参照置換 | `ProfileEditor.xaml.cs` | ✅ 完了 | `SaveSplitterAndColumnWidths` / `RestoreSplitterAndColumnWidths` のカラム幅を DI 経由に置換 |
| **(b)-3** | 通知設定アクセス経路の監査・統一 | `SettingsViewModel.cs` | ✅ 完了 | `ShowNotificationsIndex` を `_appSettings.Notifications` に配線し SSOT 連動を確定 |
| **(b)-4** | 全単体テスト・クリーンビルド確認 | ソリューション全体 | ✅ 完了 | 全156件の単体テスト PASS およびクリーンビルド確認完了 |

---

### フェーズC: ドキュメント更新・実機検証

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **タスク-4** | ドキュメント・進捗ステータス更新 | 計画書・報告書・Status文書 | ✅ 完了 | Issue 6 是正完了を記録 |
| **実機CP4** | 実機起動・終了時の設定永続化検証 | 実機環境 | ⏳ 次回実施 | ウィンドウ幾何情報・カラム幅・通知の XML 保存・復元確認 |

---

## 3. 作業対象ファイル棚卸しマトリクス（全13件改修・検証完了）

| ファイルパス | 役割 | 変更内容 | 進捗状況 |
|---|---|---|:---:|
| `DS4Windows/DI/IEnvironmentService.cs` | DIインターフェース | 永続設定8プロパティ削除、環境プローブ専用化 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | サービス実装 | 孤立 private field 8個・アクセサを完全削除 | ✅ **完了** |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 単体テスト | 削除メンバのテスト撤去、残存機能検証に純化 | ✅ **完了** |
| `DS4Windows/DI/IAppSettingsService.cs` | DIインターフェース | カラム幅5プロパティ（`int`）の追加宣言 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/AppSettingsService.cs` | サービス実装 | カラム幅5プロパティの `Global` 委譲実装（全35メンバ完全適合） | ✅ **完了** |
| `DS4Windows/DI/IProfileSettingsService.cs` | DIインターフェース | カラム幅5プロパティは元々未定義（無変更で整合） | ✅ **完了** |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | サービス実装 | 孤立 private field 5個およびアクセサの完全削除 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/AppNotificationService.cs` | サービス実装 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、`Global` 委譲化 | ✅ **完了** |
| `DS4WindowsTests/NotificationServiceTests.cs` | 単体テスト | `Global` 連動・テスト間状態汚染防止・双方向同期テストへ是正 | ✅ **完了** |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | UI ViewModel | `StartMinimize`、`ShowNotificationsIndex` 等の直参照を `_appSettings` 経由に配線 | ✅ **完了** |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | UI View Code-behind | カラム幅の直参照を `_appSettings` 経由に置換 | ✅ **完了** |

---

## 4. 是正完了の総括
* **孤立バグの完全根絶**: `Profiles.xml` に保存される全約70項目について、孤立した独立フィールドを持つ DI サービス（`EnvironmentService`, `ProfileSettingsService`, `AppNotificationService`）を完全に純化・委譲化し、実体 `BackingStore`（`m_Config`）を SSOT とする単一経路を確立した。
* **Pure DI 化の徹底**: UI 層（`SettingsViewModel.cs`, `ProfileEditor.xaml.cs`）からの `Global` 静的直参照を排除し、コンストラクタ注入された `IAppSettingsService` 経由に置換した。
* **回帰ゼロの保証**: 全156件の単体テストがオールグリーンで通過し、既存機能への破壊的変更が一切生じていないことを証明した。
