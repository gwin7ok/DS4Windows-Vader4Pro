# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期 進捗管理文書

- **Document**: `docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md`
- **Target Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Base Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Author**: Agent (Claude-based assistant)
- **Status**: フェーズA・B 完了 / フェーズC 実装済み・実機確認待ち / **フェーズD コード実装完了・ビルド確認/実機確認待ち** / フェーズF・G 承認済み・未着手
- **Created Date**: 2026-09-14
- **Last Updated**: 2026-09-14（フェーズD（契機5統合）のコード実装完了。`ControlService.cs`・`ScpUtil.cs` を修正。ビルド・単体テスト・実機確認はgwin7ok氏の実施待ち）

---

## 目次
1. [全体サマリ](#1-全体サマリ)
2. [フェーズ別進捗詳細](#2-フェーズ別進捗詳細)
   - 2.1 [フェーズA: 共通適用窓口 ApplyProfileToSlot の新設](#21-フェーズa-共通適用窓口-applyprofiletoslot-の新設)
   - 2.2 [フェーズB: 4段階同期メソッドおよびガードの実装](#22-フェーズb-4段階同期メソッドおよびガードの実装)
   - 2.3 [フェーズC: プロファイル保存契機および手動選択の統合](#23-フェーズc-プロファイル保存契機および手動選択の統合)
   - 2.4 [フェーズD: 契機5（HotPlug）の共通窓口統合](#24-フェーズd-契機5hotplugの共通窓口統合)
   - 2.5 [フェーズE: Rename 契機の統合](#25-フェーズe-rename-契機の統合)
   - 2.6 [フェーズF: 通知機能の完全分離](#26-フェーズf-通知機能の完全分離)
   - 2.7 [フェーズG: 通知判定基準の統一（案①実装）](#27-フェーズg-通知判定基準の統一案実装)
   - 2.8 [フェーズH: 総合検証および完了報告](#28-フェーズh-総合検証および完了報告)
3. [ビルドおよび自動テスト結果ログ](#3-ビルドおよび自動テスト結果ログ)
4. [実機テスト検証マトリクス](#4-実機テスト検証マトリクス)
5. [課題・ブロッカー・技術的負債トラッキング](#5-課題ブロッカー技術的負債トラッキング)
6. [更新履歴 (Change Log)](#6-更新履歴-change-log)
7. [（2026-09-14追記）実地確認による追加発見事項](#7-2026-09-14追記実地確認による追加発見事項)

---

## 1. 全体サマリ

### 1.1 進捗状況
* **総合進捗率**: **33% (2 / 8 フェーズ完了) ＋ フェーズC実装は完了済み（実機確認C-3のみ残）。フェーズD・F・G（承認済み）の実装は未着手**
* **現在ステータス**: フェーズA・B 完了。フェーズCはコード実装（C-1・C-2）が完了済み（実機確認C-3待ち）。実地確認で判明した以下の3点がユーザーより承認され、フェーズ構成を改訂した。
  1. **フェーズD（新設）**: 契機5（`ControlService.cs`、初回接続時／HotPlug）を共通窓口 `Global.ApplyProfileToSlot` に統合する。フェーズCの直後・（Rename統合の）フェーズEの前に挿入。
  2. **フェーズF（新設）**: 通知機能の完全分離（`ShowSystemNotification`/`ShowProfileSwitchNotification`への分離実装）を、フェーズGより先に実施する。
  3. **フェーズG（旧「フェーズE」）**: 案①（`Global.ApplyProfileToSlot`を`IProfileApplicationService`への委譲に書き直す）を採用。

  **実装は新設フェーズDから再開し、以降フェーズE→F→G→Hの順に進める。**

### 1.2 フェーズ別ステータスサマリ

| フェーズ | 対象領域 | 状態 | 完了予定 | 備考 |
| :--- | :--- | :---: | :---: | :--- |
| **フェーズA** | `ScpUtil.cs` 共通適用窓口新設 | [x] 完了 | 2026-09-14 | `Global.ApplyProfileToSlot` 実装完了 |
| **フェーズB** | `MainWindow.xaml.cs` 同期メソッド・ガード新設 | [x] 完了 | 2026-09-14 | `isProfileSyncing` および `SyncProfileListAndControllers` 実装完了 |
| **フェーズC** | 保存時ホットリロード＆手動 ComboBox 統合 | [~] 実装済み・実機確認待ち | 2026-09-14 | **保存時クラッシュの完全解消**。C-1/C-2はコード実装済み（2026-09-14実地確認、§2.3・§7参照）。C-3（実機確認）と通知判定基準の是正（§7）が残課題 |
| **フェーズD** | 契機5（HotPlug）の共通窓口統合（新設） | [~] コード実装完了・ビルド確認/実機確認待ち | 2026-09-14 | `ControlService.cs` を `Global.ApplyProfileToSlot` 経由に統一。付随的な通知判定の暫定是正含む |
| **フェーズE** | Rename（名前変更）契機の統合 | [ ] 未着手 | 2026-09-14 | 接続中プロファイルの Rename 破綻解消 |
| **フェーズF** | 通知機能の完全分離（新設） | [ ] 未着手（承認済み・実装待ち） | 2026-09-14 | `ShowSystemNotification`/`ShowProfileSwitchNotification`への分離。契機8（ホットキー）の是正含む |
| **フェーズG** | 通知判定基準の統一（案①実装） | [ ] 未着手（承認済み・実装待ち） | 2026-09-14 | `Global.ApplyProfileToSlot` を `IProfileApplicationService` への委譲に書き換え。`DefaultProfileSwitcher`/`AutoProfileService`/`TrayIconVM_ProfileSelected`/`ControlService.cs` は無改修据え置き |
| **フェーズH** | 総合検証・DoD 判定・ドキュメント完了 | [ ] 未着手 | 2026-09-14 | 実機テスト網羅・完了報告書作成 |

---

## 2. フェーズ別進捗詳細

### 2.1 フェーズA: 共通適用窓口 ApplyProfileToSlot の新設
- [x] **タスク A-1**: `DS4Windows/DS4Control/ScpUtil.cs` の `Global` クラスに `ApplyProfileToSlot` 静的メソッドを追加
- [x] **タスク A-2**: `dotnet build` によるコンパイル整合性検証（警告・エラー 0 件）

### 2.2 フェーズB: 4段階同期メソッドおよびガードの実装
- [x] **タスク B-1**: `DS4Windows/DS4Forms/MainWindow.xaml.cs` に `private bool isProfileSyncing = false;` フラグを追加
- [x] **タスク B-2**: `SelectProfCombo_SelectionChanged` の先頭に `if (isProfileSyncing) return;` ガードを追加
- [x] **タスク B-3**: `SyncProfileListAndControllers(string oldProfile = null, string newProfile = null)` メソッドを実装（全 `CURRENT_DS4_CONTROLLER_LIMIT` スロット追従対応）
- [x] **タスク B-4**: `dotnet build` によるコンパイル整合性検証（警告・エラー 0 件）

### 2.3 フェーズC: プロファイル保存契機および手動選択の統合
- [x] **タスク C-1**: `MainWindow.xaml.cs` の `Editor_ProfileSaved` を `SyncProfileListAndControllers();` 呼び出しに置き換え（**2026-09-14実地確認: `MainWindow.xaml.cs` L2002-2006に実装済みと判明。本Status.mdの旧記載［未着手］は誤りだったため訂正**）
- [x] **タスク C-2**: `MainWindow.xaml.cs` の `SelectProfCombo_SelectionChanged` 内の適用処理を `Global.ApplyProfileToSlot` に差し替え（**2026-09-14実地確認: `MainWindow.xaml.cs` L1059に実装済みと判明。本Status.mdの旧記載［未着手］は誤りだったため訂正**）
- [ ] **タスク C-3**: 実機テストによるプロファイル保存時のクラッシュ解消確認（未実施のまま。C-1/C-2のコードは既にビルド済みブランチに存在するため、次回実機確認時にあわせて実施可能）

> ⚠️ **（2026-09-14追記）新規判明事項**: C-1・C-2が経由する `Global.ApplyProfileToSlot` は、プロファイル切替通知の表示可否判定に `Global.Notifications`（本来はトレイバルーン通知の重大度設定）を使用しており、正しい設定である `Global.ProfileChangedNotification`（「Display profile switch notification」チェックボックス）を参照していない。この基準がフェーズC実装によって既に本流のコードに組み込まれてしまっている。詳細と是正方針は §7 を参照し、対応方針の承認を得てから修正する。

### 2.4 フェーズD: 契機5（HotPlug）の共通窓口統合 【2026-09-14新設・実装着手中】

> §3.1で契機5として当初から契機一覧に列挙されていたにもかかわらず、`ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`）が共通窓口 `Global.ApplyProfileToSlot` を経由せず `Global.ApplyProfile`（8引数）を直接呼び出し続けていたことが実地確認で判明。詳細は `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §4.4を参照。

- [x] **タスク D-1**: `ControlService.cs` の `PrepareConnectedInputControllerSettingEvents` 内の `Global.ApplyProfile` 直接呼び出しを `Global.ApplyProfileToSlot(index, profileToApply, ProfileChangeSource.ControlService)` に置き換え。**実装完了（2026-09-14）**
- [x] **タスク D-2（No Feature Drop対応・付随的な暫定是正）**: `Global.ApplyProfileToSlot` 内の `Global.Notifications != 0` を `Global.ProfileChangedNotification` に暫定修正（契機5が従来正しく参照していた基準を維持するため。フェーズGで `IProfileApplicationService` への完全委譲に置き換えられる暫定措置）。**実装完了（2026-09-14）**
- [ ] **タスク D-3**: 全単体テスト（169件）実行・検証。**gwin7ok氏の実施待ち**（本セッションはLinuxサンドボックスのため `dotnet build`/`dotnet test` を実行できず、コード編集のみ）
- [ ] **タスク D-4**: 実機にて、コントローラー初回接続時・再接続時のプロファイル適用と、`ProfileChangedNotification` 設定に応じた通知表示・非表示が退行なく動作することを確認。**gwin7ok氏の実施待ち**（TC-09参照）

### 2.5 フェーズE: Rename 契機の統合
- [ ] **タスク E-1**: `MainWindow.xaml.cs` の `RenameProfileBtn_Click` 後続処理を `SyncProfileListAndControllers(oldProfile, newProfile);` に統合
- [ ] **タスク E-2**: 接続中コントローラー適用プロファイルの Rename 実機追従検証

### 2.6 フェーズF: 通知機能の完全分離 【2026-09-14新設】

> 実地確認の結果、`MainWindow.xaml.cs` の `ShowNotification`/`ShowProfileChangeNotification`/`OnProfileChanged`/`ShowHotkeyNotification` が絡み合い、「通知を表示」（システム通知レベル）と「Display profile switch notification」（プロファイル切替通知）の判定基準が混線していることが判明した。詳細は `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §4.6・§7を参照。

- [ ] **タスク F-1**: `MainWindow.xaml.cs` の `ShowNotification(object sender, DebugEventArgs e)` を `ShowSystemNotification` へ改名（判定ロジック・購読先は変更なし）。
- [ ] **タスク F-2**: `ShowProfileChangeNotification(string, bool)` を廃止し、`ShowProfileSwitchNotification(string message)` を新設。内部で `profileSettingsService.ProfileChangedNotification` を直接判定してから `ProfileNotificationWindow.ShowNotification` を呼ぶ。
- [ ] **タスク F-3**: `OnProfileChanged` の呼び出し先を `ShowProfileSwitchNotification` に置き換え。
- [ ] **タスク F-4**: `ShowHotkeyNotification`（契機8）内の `appSettingsService.Notifications == 2` ガードを撤去し、常に `AppLogger.LogProfileChanged(...)` を呼ぶよう修正。
- [ ] **タスク F-5**: `Log.cs` の `LogProfileChanged` の `displayNotification` 引数のXMLドキュメントコメントを、「表示可否」ではなく「イベント発火可否（既定`true`）」に純化。
- [ ] **タスク F-6**: 全単体テスト（169件）実行・検証。
- [ ] **タスク F-7**: 実機にて、「通知を表示」＝なし／「Display profile switch notification」＝ONの組み合わせ、およびその逆の組み合わせで、各通知が自身の設定のみに従って独立して表示・非表示になることを確認（TC-06・TC-07、§4参照）。

### 2.7 フェーズG: 通知判定基準の統一（案①実装） 【2026-09-14改訂・タスク全面差し替え、当初「フェーズE」から改称】

> 旧タスク（`DefaultProfileSwitcher`/`AutoProfileService` を `Global.ApplyProfileToSlot` へ差し替え）は撤回。実地確認の結果これらは無改修が正しいと判明したため。詳細は `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §4.7（改訂版）・§7を参照。

- [ ] **タスク G-1**: `DS4Windows/DS4Control/ScpUtil.cs` の `Global.ApplyProfileToSlot` を、フェーズDで暫定是正した `Global.ProfileChangedNotification` 判定・`Global.ApplyProfile` 直接呼び出しから、`DS4WinWPF.AppHost.GetService<IProfileApplicationService>()` 経由の委譲に書き換え。`displayNotification: true` を渡すだけでよい。DI未解決時のフォールバックも同様に `displayNotification: true` を用いる。
- [ ] **タスク G-2**: `DefaultProfileSwitcher.cs` / `AutoProfileService.cs` / `MainWindow.xaml.cs`（`TrayIconVM_ProfileSelected`）/ `ControlService.cs`（契機5、フェーズD後）が無改修のまま整合していることを確認（コード変更なし、レビューのみ）。
- [ ] **タスク G-3**: 全単体テスト（169件）実行・検証（変更なし）
- [ ] **タスク G-4**: 実機にて TC-06（§4）を実施し、メインウィンドウComboBoxとトレイアイコンメニューの通知挙動が一致することを確認。

### 2.8 フェーズH: 総合検証および完了報告
- [ ] **タスク H-1**: 実機検証マトリクスの全項目 PASS 確認
- [ ] **タスク H-2**: 完了報告書の作成

---

## 3. ビルドおよび自動テスト結果ログ

### 3.1 フェーズB ビルド結果
* **日時**: 2026-09-14
* **結果**: ビルド成功 (警告 0件, エラー 0件)
* **テスト結果**: 169 成功, 0 失敗 (DS4Windows.Actions.Tests)

---

## 4. 実機テスト検証マトリクス

| テストケース ID | 検証項目・シナリオ | 期待結果 | 判定 | 備考 |
| :--- | :--- | :--- | :---: | :--- |
| **TC-01** | プロファイル編集画面での「保存」実行 | クラッシュせず画面が閉じ、変更が保存される | Pending | 課題1の解消確認（フェーズC検証対象） |
| **TC-02** | プロファイル編集画面での「適用」実行 | 画面が開いたまま即時にホットリロードされる | Pending | 共通化の確認（フェーズC検証対象） |
| **TC-03** | 接続中プロファイルの Rename 実行 | UI の ComboBox が新名にスライドし動作継続 | Pending | 課題2の解消確認（フェーズD検証対象） |
| **TC-04** | 手動 ComboBox によるプロファイル切り替え | 選択したプロファイルに即時切り替わる | Pending | 契機1の確認（フェーズC検証対象） |
| **TC-05** | Emulated Controller の双方向切り替え | X360 ⇄ DS4 の変更が破綻なく追従する | Pending | Issue 7 整合性確認 |
| **TC-06** | 「Display profile switch notification」チェックボックスをOFFにした状態で、(a)メインウィンドウComboBox、(b)トレイアイコン メニューの双方からプロファイルを切り替える | (a)(b)いずれの経路でも独自通知ウィンドウが表示されないこと | Pending | フェーズG検証対象。フェーズF完了後に実施 |
| **TC-07** | 「通知を表示」を「なし」に設定した状態で、「Display profile switch notification」をONにしてプロファイルを切り替える | システム通知（トースト）は出ないが、プロファイル切替の独自デスクトップ通知は正しく表示されること | Pending | フェーズF検証対象。§4.6の機能分離が正しく機能しているかの確認 |
| **TC-08** | 「Display profile switch notification」をOFFにした状態で、ホットキー経由でプロファイルを切り替える（契機8） | 独自デスクトップ通知が表示されないこと（「通知を表示」の設定値に関わらず） | Pending | フェーズF検証対象。契機8の是正確認 |
| **TC-09**（新規） | コントローラーの初回接続・再接続時（契機5）に、「Display profile switch notification」の設定通りに通知が表示・非表示されること | フェーズD統合後も退行がないこと | Pending | フェーズD検証対象。契機5統合に伴う暫定是正の確認 |

---

## 5. 課題・ブロッカー・技術的負債トラッキング

* **既知の課題**:
  - `ProfileListCol.Clear()` 実行時の ComboBox バインディング破壊（フェーズBにて同期メソッド・ガード新設完了、フェーズCの呼び出し差し替えで完全解消予定）。
* **ブロッカー**: なし。
* **（2026-09-14追記）新規の技術的負債**: `Global.ApplyProfileToSlot`（フェーズA新設）と既存 `IProfileApplicationService.ApplyProfile` とで、プロファイル切替通知の表示判定基準（`Global.Notifications` vs `Global.ProfileChangedNotification`）が食い違っている。さらに `MainWindow.xaml.cs` の通知関連メソッド（`ShowNotification`/`ShowProfileChangeNotification`/`OnProfileChanged`/`ShowHotkeyNotification`）がシステム通知とプロファイル切替通知の判定を混同しており、`ControlService.cs`（契機5）は共通窓口を経由せず旧来の `Global.ApplyProfile` を直接呼び出している。**2026-09-14、是正方針（フェーズD：契機5統合、フェーズF：通知機能の完全分離、フェーズG：案①）がユーザーより承認され、実装タスク化済み（§2.4・§2.6・§2.7）。実装はフェーズDから着手中。**

---

## 6. 更新履歴 (Change Log)

* **2026-09-14**:
  - フェーズB 完了: `MainWindow.xaml.cs` に `isProfileSyncing` ガードおよび 4段階同期メソッド `SyncProfileListAndControllers` を新設。スロット数（8スロット上限）追従を保証。
  - フェーズA 完了: `Global.ApplyProfileToSlot` 共通静的窓口を `ScpUtil.cs` に新設。
  - 実装計画書（`Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md`）策定。
  - 進捗管理文書（本ドキュメント）新規作成。
* **2026-09-14（追記・実地確認）**:
  - ユーザー提供の通知関連棚卸し文書をきっかけに、`git`ベースの実コード grep 調査を実施。
  - フェーズC（C-1・C-2）が本Status.mdの記載（未着手）に反し、実際には既にコード実装済みであることを発見・訂正。
  - `Global.ApplyProfileToSlot`（フェーズA）と `IProfileApplicationService.ApplyProfile`（既存）とで、プロファイル切替通知の表示判定基準が `Global.Notifications` と `Global.ProfileChangedNotification` に分裂している重大な不整合を新規発見。是正方針（案①・案②）を `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §7 に記載し、ユーザー承認待ちとした。
  - `MainWindow.xaml.cs` の `TrayIconVM_ProfileSelected`（トレイアイコン メニュー経由の選択）が、Plan.md §3.1の「6つの契機」に未列挙だった第7の適用契機であることを発見・追記。
  - 実機テスト検証マトリクスに TC-06（通知判定基準の不整合確認用）を追加。
  - コードの変更は本追記では一切行っていない（ドキュメントのみ更新）。
* **2026-09-14（追記・ユーザー承認反映、第2回）**:
  - ユーザーより、通知関連メソッドの「完全分離」（`ShowSystemNotification`/`ShowProfileSwitchNotification`への分離実装）を新規フェーズEとして、フェーズFの前に挿入することが承認された。
  - これに伴い、従来「フェーズE」と呼んでいた「通知判定基準の統一（案①実装）」はフェーズFへ改称した（総合検証フェーズも旧フェーズF→フェーズGへ改称）。
  - `MainWindow.xaml.cs` の実コード再確認により、契機8（ホットキー経由のプロファイル切替、`ShowHotkeyNotification`）が「通知を表示」設定でプロファイル切替通知イベントの発火可否を誤って判定していることを新規発見し、契機一覧・フェーズEのタスクに追加。
  - `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §3.1（契機8を追加）・§4.5（新設）・§4.6（旧§4.5から改称・内部の`displayNotification`受け渡しをフェーズE完了後提としてシンプル化）・§4.7（旧§4.6から改称）・§5・§7.3を改訂。
  - 本ドキュメント（Status.md）のフェーズ別ステータスサマリ・§2.5（新設）・§2.6（改称）・§2.7（改称）・§4実機テストマトリクス（TC-07・TC-08追加）を更新。
  - **コードの実装はまだ行っていない。ドキュメント更新のみ完了。**
* **2026-09-14（追記・ユーザー承認反映、第3回）**:
  - ユーザーより、契機5（`ControlService.cs`、初回接続時／HotPlug）の共通窓口統合を新規フェーズDとして、フェーズC直後・（Rename統合の）フェーズDの前に挿入することが承認された。
  - これに伴い、既存フェーズD（Rename）〜G（総合検証）を1つずつ繰り下げ（D→E, E→F, F→G, G→H）。
  - 実地確認（`ControlService.cs` L2171）により、契機5が共通窓口を経由せず `Global.ApplyProfile`（8引数）を直接呼び出していることを確認。
  - フェーズDの実装に伴い、契機5が従来正しく参照していた `ProfileChangedNotification` 基準が `ApplyProfileToSlot` 経由で `Global.Notifications`（誤り）に巻き込まれ退行することを防ぐため、`ApplyProfileToSlot` 内の判定式を暫定是正するタスク（D-2）を追加。No Feature Drop原則（copilot-instructions §2.2）に基づく対応。
  - `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §3.1・§4.4（新設）・§4.5〜§4.7（改称・改訂）・§5・§7.3を改訂。
  - 本ドキュメント（Status.md）の全体構成・フェーズ別ステータスサマリ・§2.4（新設）〜§2.8（改称）・§4実機テストマトリクス（TC-09追加）を更新。
  - **実装をフェーズD（タスクD-1〜D-4）から再開する。**
* **2026-09-14（フェーズD コード実装）**:
  - タスクD-1: `ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`）の `Global.ApplyProfile` 直接呼び出しを `Global.ApplyProfileToSlot(index, profileToApply, ProfileChangeSource.ControlService)` に置き換え。
  - タスクD-2: `ScpUtil.cs` の `Global.ApplyProfileToSlot` 内の通知判定基準を `Global.Notifications != 0` から `Global.ProfileChangedNotification` に暫定是正（No Feature Drop対応）。あわせてXMLドキュメントコメントを実態に合わせて更新。
  - 軽微な仕様差: プロファイル適用時のログ／通知メッセージのバッテリー表示欄が `"N/A"` から空文字列に変化（契機1・2・6と表示形式を統一する意図した副次効果）。
  - **本セッションはLinuxサンドボックスのため `dotnet build`/`dotnet test` を実行できていない。タスクD-3（ビルド・単体テスト）およびD-4（実機確認）はgwin7ok氏の実施が必要。**

---

## 7. （2026-09-14追記）実地確認による追加発見事項

本セクションは要約のみを記載する。詳細な事実確認（コード引用・契機ごとの経路一覧）および是正方針の比較検討は `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §7 を正本として参照すること。

### 7.1 発見の要約

1. **通知表示判定基準の二重化（重大）**: フェーズAで新設した `Global.ApplyProfileToSlot`（`ScpUtil.cs`）は `Global.Notifications`（「通知を表示」0/1/2設定）を基準に独自通知ウィンドウの表示可否を決めているが、既存の `IProfileApplicationService.ApplyProfile`（`DefaultProfileSwitcher`/`AutoProfileService`/`TrayIconVM_ProfileSelected` が使用）は `Global.ProfileChangedNotification`（「Display profile switch notification」チェックボックス）を基準にしており、設定基準が食い違っている。
2. **契機の不統一**: 手動選択（メインウィンドウComboBox）・編集保存時は `Global.ApplyProfileToSlot` 経由（誤った基準）、切換アクション・自動プロファイル・トレイアイコン選択は `IProfileApplicationService` 経由（正しい基準）と、契機によって経路が分裂している。
3. **未列挙の契機7**: `TrayIconVM_ProfileSelected`（トレイアイコン メニュー経由の選択）が、本ドキュメント群が前提とする「6つの契機」に含まれていなかった。
4. **本Status.mdの記載誤り**: フェーズC（C-1・C-2）は「未着手」と記載されていたが、実コード確認の結果、既に実装済みであることが判明した（§2.3で訂正済み）。
5. **（2026-09-14第2回追記）通知メソッド自体の混線**: `MainWindow.xaml.cs` の `ShowNotification`/`ShowProfileChangeNotification`/`OnProfileChanged` を実コード確認した結果、「プロファイル切替通知」を出す `ShowProfileChangeNotification` は `ProfileChangedNotification` 設定を一切参照せず無条件に表示していることが判明した。表示可否は呼び出し元（`ApplyProfile`系）が計算した `displayNotification` フラグに完全依存しており、判定ロジックがメソッド自身に存在しない構造的欠陥である。
6. **（2026-09-14第2回追記）未列挙の契機8**: `ShowHotkeyNotification`（ホットキー経由のプロファイル切替）が、`appSettingsService.Notifications == 2` を条件に `AppLogger.LogProfileChanged` の呼び出し自体を抑制しており、システム通知の設定でプロファイル切替通知の発火を左右する、契機1・2と同種の誤りを独立に持っていた。
7. **（2026-09-14第3回追記）契機5が共通窓口を未経由**: `ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`）が、`Global.ApplyProfileToSlot` を経由せず `Global.ApplyProfile`（8引数の静的メソッド）を直接呼び出し続けていた。§3.1に契機5として列挙されていたにもかかわらず、いずれのフェーズにも統合作業が割り当てられていなかった計画上の抜け漏れ。

### 7.2 是正方針の状態

- 通知判定基準の二重化（1・2）に対しては、案①（`Global.ApplyProfileToSlot` を `IProfileApplicationService.ApplyProfile` への薄い委譲に書き直す）と、案②（判定式の一行修正のみ）の2案を提示し、**案①が承認され、フェーズGとして実装タスク化済み**。
- 通知メソッド自体の混線（5・6）に対しては、**「通知機能の完全分離」を新規フェーズFとして、フェーズGより前に実施することが承認された**（`ShowSystemNotification`/`ShowProfileSwitchNotification`への分離、契機8の是正を含む。§2.6参照）。
- 契機5の未統合（7）に対しては、**「契機5の共通窓口統合」を新規フェーズDとして、フェーズCの直後に実施することが承認された**（§2.4参照）。契機5統合に伴う通知判定の暫定是正も同フェーズに含む。
- 実装順序: **フェーズD（契機5統合）→ フェーズE（Rename統合）→ フェーズF（通知機能の完全分離）→ フェーズG（判定基準の統一・案①）→ フェーズH（総合検証）**。
- **いずれもユーザー承認済みであり、フェーズDの実装から着手する。**