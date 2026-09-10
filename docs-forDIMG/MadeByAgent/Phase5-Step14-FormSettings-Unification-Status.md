# Phase 5 - Step 14 フォーム/カラム幅設定 SSOT 統一 進捗管理ステータス

作成日: 2026-09-11
現在ステータス: **進行中（タスク(a)-4 完了 / 次回: タスク(a)-5）**
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
* **完了タスク**: タスク(a)-1, (a)-2, (a)-3, **(a)-4**
* **ビルド / テスト状況**:
  - `dotnet build ./DS4Windows/DS4WinWPF.csproj` : ✅ **成功（警告 0 / エラー 0）**
  - `dotnet test`（全単体テスト実行） : ✅ **全件 PASS**
* **次回着手予定**: **タスク(a)-5: `IProfileSettingsService` / `ProfileSettingsService` の孤立5プロパティ削除**

---

## 2. マイクロタスク進捗一覧

### フェーズA: DI サービス側の孤立フィールド排除と SSOT 委譲化

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(a)-1** | `IEnvironmentService` インターフェースの縮小 | `IEnvironmentService.cs` | ✅ 完了 | 8設定プロパティを削除、環境プローブのみ残存 |
| **(a)-2** | `EnvironmentService` 実装の純化 | `EnvironmentService.cs` | ✅ 完了 | 孤立 private field 8個・アクセサを完全削除 |
| **(a)-3** | `EnvironmentServiceTests.cs` の是正 | `EnvironmentServiceTests.cs` | ✅ 完了 | 削除プロパティのテスト撤去、残存機能テスト化 |
| **(a)-4** | `IAppSettingsService` / `AppSettingsService` への5プロパティ追加 | `IAppSettingsService.cs`<br>`AppSettingsService.cs` | ✅ 完了 | カラム幅5プロパティ（`int`）を宣言・`Global` 委譲実装。ビルド・全テスト成功 |
| **(a)-5** | `IProfileSettingsService` / `ProfileSettingsService` の孤立5プロパティ削除 | `IProfileSettingsService.cs`<br>`ProfileSettingsService.cs` | ⏳ **次回着手** | カラム幅5プロパティの孤立フィールド・アクセサ完全削除 |
| **(a)-6** | `AppNotificationService.cs` の孤立フィールド排除と Global 委譲化 | `AppNotificationService.cs` | ⏳ 未着手 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、`Global` 委譲化 |
| **(a)-7** | `NotificationServiceTests.cs` の是正と単体テスト確認 | `NotificationServiceTests.cs` | ⏳ 未着手 | 孤立前提テストの是正、`Global` 連動検証追加 |

---

### フェーズB: UI 層の Global 直参照置換とアクセス経路統一

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **(b)-1** | `SettingsViewModel.cs` の Global 直参照置換 | `SettingsViewModel.cs` | ⏳ 未着手 | `StartMinimized`, `CloseMinimizes` 等を注入済み `_appSettings` 経由へ置換 |
| **(b)-2** | `ProfileEditor.xaml.cs` の Global 直参照置換 | `ProfileEditor.xaml.cs` | ⏳ 未着手 | カラム幅5プロパティの直参照を `appSettingsService.Xxx` に置換 |
| **(b)-3** | 通知設定アクセス経路の監査・統一 | `SettingsViewModel.cs` 等 | ⏳ 未着手 | `IAppSettingsService` / `INotificationService` の SSOT 連動確認 |
| **(b)-4** | 全単体テスト・クリーンビルド確認 | ソリューション全体 | ⏳ 未着手 | 全テスト（169件超）PASS 維持を確認 |

---

### フェーズC: ドキュメント更新・実機検証

| タスクID | タスク概要 | 対象ファイル | 状態 | 備考 |
|---|---|---|:---:|---|
| **タスク-4** | ドキュメント・進捗ステータス更新 | `Phase5-Status.md`<br>`Phase5-Plan.md`<br>調査レポート §3.6 | ⏳ 未着手 | Issue 6 是正完了を記録 |
| **実機CP4** | 実機起動・終了時の設定永続化検証 | 実機環境 | ⏳ 未着手 | ウィンドウ幾何情報・カラム幅・通知の XML 保存・復元確認 |

---

## 3. 作業対象ファイル棚卸しマトリクス

| ファイルパス | 役割 | 変更内容 | 進捗状況 |
|---|---|---|:---:|
| `DS4Windows/DI/IEnvironmentService.cs` | DIインターフェース | 永続設定8プロパティ削除、環境プローブ専用化 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | サービス実装 | 孤立 private field 8個・アクセサを完全削除 | ✅ **完了** |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 単体テスト | 削除メンバのテスト撤去、残存機能検証に純化 | ✅ **完了** |
| `DS4Windows/DI/IAppSettingsService.cs` | DIインターフェース | カラム幅5プロパティ（`int`）の追加宣言 | ✅ **完了** |
| `DS4Windows/DS4Control/Services/AppSettingsService.cs` | サービス実装 | カラム幅5プロパティの `Global` 委譲実装（全35メンバ完全適合） | ✅ **完了** |
| `DS4Windows/DI/IProfileSettingsService.cs` | DIインターフェース | 孤立していたカラム幅5プロパティ宣言の削除 | ⏳ **次回 (a)-5** |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | サービス実装 | 孤立 private field 5個およびアクセサの完全削除 | ⏳ **次回 (a)-5** |
| `DS4Windows/DS4Control/Services/AppNotificationService.cs` | サービス実装 | `_notificationsEnabled`, `_flashTaskbar` 独自フィールド撤去、委譲化 | ⏳ 未着手 (a)-6 |
| `DS4WindowsTests/NotificationServiceTests.cs` | 単体テスト | `Global` 連動を検証するテストへの是正・拡充 | ⏳ 未着手 (a)-7 |
| `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs` | UI ViewModel | `Global.StartMinimized` 等の直参照を `appSettingsService` 経由に置換 | ⏳ 未着手 (b)-1 |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | UI View Code-behind | `Global.ProfileEditorLeftWidth` 等の直参照を `appSettingsService` 経由に置換 | ⏳ 未着手 (b)-2 |

---

## 4. (a)-4 完了実績の詳細記録

### 4.1 変更内容
1. **`IAppSettingsService.cs`**:
   - `ProfileEditorLeftWidth` (int)
   - `ProfileEditorRightWidth` (int)
   - `SpecialActionNameColWidth` (int)
   - `SpecialActionTriggerColWidth` (int)
   - `SpecialActionDetailColWidth` (int)
   上記5プロパティをインターフェースに追加宣言。
2. **`AppSettingsService.cs`**:
   - コミット `7e5ed24`（ビルド・テスト成功実績コミット）を原本として正確に復元した上で、上記 5 プロパティを `Global` への正規委譲（getter / setter ＋ `NotifyChanged(nameof(...))`）として追加。
   - 他の全 35 メンバ（`Save()`, `Load()`, `SettingChanged`, 各種設定プロパティ）との型整合性を 100% 確保。

### 4.2 検証エビデンス
- **ビルド結果**: `dotnet build ./DS4Windows/DS4WinWPF.csproj -c Debug /p:platform=x64` → 成功（警告 0 / エラー 0）
- **単体テスト結果**: 全単体テスト実行 → 全件 PASS
- **リモート反映**: リモートリポジトリ（`For-DI-migration-work`）に正常反映済み。

---

## 5. 次回着手タスク（タスク(a)-5）の作業概要

* **目的**:
  `IAppSettingsService` への委譲実装が完了したため、従来 `ProfileSettingsService` 側に存在していた「`Global` と連動しない孤立した 5 プロパティ（死コード）」を完全撤去する。
* **改修対象**:
  1. `DS4Windows/DI/IProfileSettingsService.cs`:
     - `ProfileEditorLeftWidth`, `ProfileEditorRightWidth`, `ControllerSelectProfileColWidth`, `ControllerLinkedProfileColWidth`, `ControllerLinkProfIdColWidth` の宣言を削除。
  2. `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
     - 上記 5 プロパティの独自 private field（`_profileEditorLeftWidth` 等）および getter/setter アクセサを完全削除。
