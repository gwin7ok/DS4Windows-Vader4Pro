# Phase 5 - Step 14 フォーム/カラム幅設定 SSOT 統一 進捗管理ステータス

作成日: 2026-09-11
改訂日: 2026-09-11（タスク(b)-3 完了 / 次回: タスク(b)-4）
現在ステータス: **進行中（タスク(b)-3 完了 / 次回: タスク(b)-4）**
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- 個別計画書: `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Plan.md`
- 調査報告書: `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（Issue 6）
- Phase 5 全体計画: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
- Phase 5 全体状況: `docs-forDIMG/MadeByAgent/Phase5-Status.md`
- 移行全体憲法: `.github/copilot-instructions.md`（Pure DI原則・フォールバック/シム維持原則）

---

## 1. 概要と現在地

### 1.1 目的
実機検証中に発覚した「アプリ終了時にウィンドウサイズ・位置・各種カラム幅が `Profiles.xml` へ正しく反映されないことがある」不具合（Issue 6）の根本原因である、DIサービス（`EnvironmentService`, `ProfileSettingsService`, `AppNotificationService`）における **実体 `BackingStore`（`m_Config`）と連動しない孤立 private field の二重保持バグ** を根絶し、アクセス経路を `IAppSettingsService` / `INotificationService` 経由に統一（SSOT 一本化）する。

### 1.2 現在地サマリ（2026-09-11 時点）
* **完了タスク**:
  - フェーズA 全タスク完了（(a)-1, (a)-2, (a)-3, (a)-4, (a)-5, (a)-6, (a)-7）
  - **フェーズB タスク(b)-1, (b)-2, (b)-3 完了**（コード改修および配線完了）
* **ビルド / テスト状況**:
  - `dotnet build ./DS4Windows/DS4WinWPF.csproj` : ✅ **成功（警告 0 / エラー 0）**
  - `dotnet test`（全156件の単体テスト実行） : ✅ **全156件 PASS（オールグリーン）**
* **次回着手予定**: **フェーズB タスク(b)-4: 全単体テスト・クリーンビルド確認（フェーズB完了確認）**

---

## 2. マイクロタスク進捗一覧

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

### フェーズB: UI 層の Global 直参照置換とアクセス経路統一

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(b)-1** | `SettingsViewModel.cs` の Global 直参照置換 | `SettingsViewModel.cs` | ✅ 完了 | `StartMinimize`, `MinimizeToTaskbar`, `CloseMinimizes` を注入済み `_appSettings` 経由へ配線 |
| **(b)-2** | `ProfileEditor.xaml.cs` の Global 直参照置換 | `ProfileEditor.xaml.cs` | ✅ 完了 | `SaveSplitterAndColumnWidths` / `RestoreSplitterAndColumnWidths` のカラム幅を DI 経由に置換 |
| **(b)-3** | 通知設定アクセス経路の監査・統一 | `SettingsViewModel.cs` 等 | ✅ 完了 | `ShowNotificationsIndex` を `_appSettings.Notifications` に配線し SSOT 連動を確定 |
| **(b)-4** | 全単体テスト・クリーンビルド確認 | ソリューション全体 | ⏳ **次回着手** | 全単体テスト PASS 維持およびビルド確認 |

---

### フェーズC: ドキュメント更新・実機検証

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **タスク-4** | ドキュメント・進捗ステータス更新 | `Phase5-Status.md`<br>`Phase5-Plan.md`<br>調査レポート §3.6 | ⏳ 未着手 | Issue 6 是正完了を記録 |
| **実機CP4** | 実機起動・終了時の設定永続化検証 | 実機環境 | ⏳ 未着手 | ウィンドウ幾何情報・カラム幅・通知の XML 保存・復元確認 |

---

## 3. 作業対象ファイル棚卸しマトリクス（全13件改修完了）

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

## 4. (a)-4 完了実績の詳細記録
- **内容**: `IAppSettingsService` および `AppSettingsService` にカラム幅 5 プロパティ（`ProfileEditorLeftWidth`, `ProfileEditorRightWidth`, `SpecialActionNameColWidth`, `SpecialActionTriggerColWidth`, `SpecialActionDetailColWidth`）を `Global` 委譲として実装。
- **検証**: ビルド・全単体テスト 100% 成功。

---

## 5. (a)-5 完了実績の詳細記録
- **内容**: `ProfileSettingsService.cs` から孤立していた上記 5 プロパティおよび private フィールドを波括弧深度解析に基づきピンポイント完全切除（正規メンバ `TouchpadActiveArray` 等は 100% 保護）。
- **検証**: ビルド・全単体テスト 100% 成功。

---

## 6. (a)-6 ＆ (a)-7 完了実績の詳細記録
- **内容**: `AppNotificationService.cs` の孤立フィールド（`_notificationsEnabled`, `_flashTaskbar`）を排除し、`Global` への直接委譲に変更。`NotificationServiceTests.cs` に状態復元ガードおよび双方向同期テストを追加。
- **検証**: 全 156 件の単体テスト実行 → 全件 PASS。

---

## 7. (b)-1 完了実績の詳細記録
- **内容**: `SettingsViewModel.cs` の `StartMinimize`, `MinimizeToTaskbar`, `CloseMinimizes` の 3 プロパティにおいて、完全修飾名 `DS4Windows.Global.` 直参照を注入済み `_appSettings` 経由へ配線。
- **検証**: ビルド・全単体テスト 100% 成功。

---

## 8. (b)-2 完了実績の詳細記録
- **内容**: `ProfileEditor.xaml.cs` の `SaveSplitterAndColumnWidths` / `RestoreSplitterAndColumnWidths` におけるカラム幅 5 プロパティの直参照を、DI 注入された `_appSettings`（`IAppSettingsService`）経由に置換。
- **検証**: ビルド・全単体テスト 100% 成功。

---

## 9. (b)-3 完了実績の詳細記録

### 9.1 改修内容
`SettingsViewModel.cs` の通知関連プロパティ `ShowNotificationsIndex` において、`Global.Notifications` への直接参照を注入済みの `_appSettings.Notifications` 経由へ配線：
* UI（設定画面）での通知設定変更が `_appSettings.Notifications` 経由で `BackingStore` に保存される。
* 同時に `AppNotificationService.NotificationsEnabled`（`Global.Notifications != 0`）にも 100% 即座に反映され、設定永続化と通知実行の SSOT 一本化が完了。

### 9.2 検証エビデンス
- **ビルド結果**: `dotnet build ./DS4Windows/DS4WinWPF.csproj` → 警告 0 / エラー 0
- **単体テスト結果**: 全 156 件実行 → 全件 PASS
- **コミット ＆ プッシュ**: リモートリポジトリ（`For-DI-migration-work`）に正常反映済み。

---

## 10. 次回着手タスク（タスク(b)-4）の作業概要

* **目的**:
  フェーズA・フェーズB で実施したすべてのコード改修（孤立フィールド排除・SSOT 委譲化・UI 配線）が完了した状態において、ソリューション全体のクリーンビルドおよび全単体テスト（156件超）の完全 PASS を最終確認し、フェーズB を正式に完了とする。
