# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期 進捗管理文書

- **Document**: `docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md`
- **Target Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Base Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Author**: Agent (Claude-based assistant)
- **Status**: フェーズA・B 完了 / フェーズC 実装済み・実機確認待ち（実地確認により判明） / 通知判定基準の是正方針は承認待ち（§7参照）
- **Created Date**: 2026-09-14
- **Last Updated**: 2026-09-14（実コード grep によるフェーズC実装状況の訂正、および通知表示判定基準の二重化を追記）

---

## 目次
1. [全体サマリ](#1-全体サマリ)
2. [フェーズ別進捗詳細](#2-フェーズ別進捗詳細)
   - 2.1 [フェーズA: 共通適用窓口 ApplyProfileToSlot の新設](#21-フェーズa-共通適用窓口-applyprofiletoslot-の新設)
   - 2.2 [フェーズB: 4段階同期メソッドおよびガードの実装](#22-フェーズb-4段階同期メソッドおよびガードの実装)
   - 2.3 [フェーズC: プロファイル保存契機および手動選択の統合](#23-フェーズc-プロファイル保存契機および手動選択の統合)
   - 2.4 [フェーズD: Rename 契機の統合](#24-フェーズd-rename-契機の統合)
   - 2.5 [フェーズE: 切換アクションおよび自動プロファイルの統合](#25-フェーズe-切換アクションおよび自動プロファイルの統合)
   - 2.6 [フェーズF: 総合検証および完了報告](#26-フェーズf-総合検証および完了報告)
3. [ビルドおよび自動テスト結果ログ](#3-ビルドおよび自動テスト結果ログ)
4. [実機テスト検証マトリクス](#4-実機テスト検証マトリクス)
5. [課題・ブロッカー・技術的負債トラッキング](#5-課題ブロッカー技術的負債トラッキング)
6. [更新履歴 (Change Log)](#6-更新履歴-change-log)
7. [（2026-09-14追記）実地確認による追加発見事項](#7-2026-09-14追記実地確認による追加発見事項)

---

## 1. 全体サマリ

### 1.1 進捗状況
* **総合進捗率**: **33% (2 / 6 フェーズ完了) ＋ フェーズC実装は完了済み（実地確認で判明、実機確認C-3のみ残）**
* **現在ステータス**: フェーズA・B 完了。フェーズCはコード実装（C-1・C-2）が既に完了していることを2026-09-14の実地確認（`git`ベースのソース照合）で確認した。加えて、フェーズEの計画内容に影響する重大な追加発見（プロファイル切替通知の表示判定基準が2種類並存している）があり、ユーザー承認待ち（§7参照）。

### 1.2 フェーズ別ステータスサマリ

| フェーズ | 対象領域 | 状態 | 完了予定 | 備考 |
| :--- | :--- | :---: | :---: | :--- |
| **フェーズA** | `ScpUtil.cs` 共通適用窓口新設 | [x] 完了 | 2026-09-14 | `Global.ApplyProfileToSlot` 実装完了 |
| **フェーズB** | `MainWindow.xaml.cs` 同期メソッド・ガード新設 | [x] 完了 | 2026-09-14 | `isProfileSyncing` および `SyncProfileListAndControllers` 実装完了 |
| **フェーズC** | 保存時ホットリロード＆手動 ComboBox 統合 | [~] 実装済み・実機確認待ち | 2026-09-14 | **保存時クラッシュの完全解消**。C-1/C-2はコード実装済み（2026-09-14実地確認、§2.3・§7参照）。C-3（実機確認）と通知判定基準の是正（§7）が残課題 |
| **フェーズD** | Rename（名前変更）契機の統合 | [ ] 未着手 | 2026-09-14 | 接続中プロファイルの Rename 破綻解消 |
| **フェーズE** | スペシャルアクション＆自動プロファイル統合 | [ ] 未着手 | 2026-09-14 | 全適用経路の一本化完了 |
| **フェーズF** | 総合検証・DoD 判定・ドキュメント完了 | [ ] 未着手 | 2026-09-14 | 実機テスト網羅・完了報告書作成 |

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

### 2.4 フェーズD: Rename 契機の統合
- [ ] **タスク D-1**: `MainWindow.xaml.cs` の `RenameProfileBtn_Click` 後続処理を `SyncProfileListAndControllers(oldProfile, newProfile);` に統合
- [ ] **タスク D-2**: 接続中コントローラー適用プロファイルの Rename 実機追従検証

### 2.5 フェーズE: 切換アクションおよび自動プロファイルの統合
- [ ] **タスク E-1**: `DS4Windows/Actions/DefaultProfileSwitcher.cs` の `ApplyProfileToSlot` 差し替え
- [ ] **タスク E-2**: `DS4Windows/DS4Control/Services/AutoProfileService.cs` の `ApplyProfileToSlot` 差し替え
- [ ] **タスク E-3**: 全自動テスト（169件）実行・検証

### 2.6 フェーズF: 総合検証および完了報告
- [ ] **タスク F-1**: 実機検証マトリクスの全項目 PASS 確認
- [ ] **タスク F-2**: 完了報告書の作成

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
| **TC-06**（新規） | 「Display profile switch notification」チェックボックスをOFFにした状態で、(a)メインウィンドウComboBox、(b)トレイアイコン メニューの双方からプロファイルを切り替える | (a)(b)いずれの経路でも独自通知ウィンドウが表示されないこと（現状は(a)側が`Global.Notifications`基準のため、条件によって表示されてしまう不整合を検証） | Pending | §7で判明した通知判定基準の二重化の実機確認用（是正実装後に実施） |

---

## 5. 課題・ブロッカー・技術的負債トラッキング

* **既知の課題**:
  - `ProfileListCol.Clear()` 実行時の ComboBox バインディング破壊（フェーズBにて同期メソッド・ガード新設完了、フェーズCの呼び出し差し替えで完全解消予定）。
* **ブロッカー**: なし。
* **（2026-09-14追記）新規の技術的負債**: `Global.ApplyProfileToSlot`（フェーズA新設）と既存 `IProfileApplicationService.ApplyProfile` とで、プロファイル切替通知の表示判定基準（`Global.Notifications` vs `Global.ProfileChangedNotification`）が食い違っている。詳細・是正方針は §7 参照。ユーザー承認待ちのため現時点で未着手。

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

---

## 7. （2026-09-14追記）実地確認による追加発見事項

本セクションは要約のみを記載する。詳細な事実確認（コード引用・契機ごとの経路一覧）および是正方針の比較検討は `Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md` §7 を正本として参照すること。

### 7.1 発見の要約

1. **通知表示判定基準の二重化（重大）**: フェーズAで新設した `Global.ApplyProfileToSlot`（`ScpUtil.cs`）は `Global.Notifications`（「通知を表示」0/1/2設定）を基準に独自通知ウィンドウの表示可否を決めているが、既存の `IProfileApplicationService.ApplyProfile`（`DefaultProfileSwitcher`/`AutoProfileService`/`TrayIconVM_ProfileSelected` が使用）は `Global.ProfileChangedNotification`（「Display profile switch notification」チェックボックス）を基準にしており、設定基準が食い違っている。
2. **契機の不統一**: 手動選択（メインウィンドウComboBox）・編集保存時は `Global.ApplyProfileToSlot` 経由（誤った基準）、切換アクション・自動プロファイル・トレイアイコン選択は `IProfileApplicationService` 経由（正しい基準）と、契機によって経路が分裂している。
3. **未列挙の契機7**: `TrayIconVM_ProfileSelected`（トレイアイコン メニュー経由の選択）が、本ドキュメント群が前提とする「6つの契機」に含まれていなかった。
4. **本Status.mdの記載誤り**: フェーズC（C-1・C-2）は「未着手」と記載されていたが、実コード確認の結果、既に実装済みであることが判明した（§2.3で訂正済み）。

### 7.2 是正方針の状態

- 案①（`Global.ApplyProfileToSlot` を `IProfileApplicationService.ApplyProfile` への薄い委譲に書き直す。フェーズEは対応不要に変更）と、案②（判定式の一行修正のみ）の2案を提示済み。
- **いずれもユーザー承認待ちであり、本追記時点でコードは一切変更していない。**
- 承認が得られ次第、フェーズC-4（新設想定）として本ドキュメントにタスクを追加し、あわせてフェーズEの対象・作業内容（§2.5）を見直す。