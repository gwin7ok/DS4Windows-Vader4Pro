# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期 実装計画書

- **Document**: `docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md`
- **Target Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Base Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Author**: Agent (Claude-based assistant)
- **Status**: 計画策定完了 (Ready for Implementation) ／ フェーズC実地確認・追加発見事項あり（§7、承認待ち）
- **Created Date**: 2026-09-14
- **Last Updated**: 2026-09-14（実コード grep による追加調査：通知表示判定基準の二重化を発見、§7に追記）

---

## 目次
1. [概要と目的](#1-概要と目的)
2. [真因分析と課題整理](#2-真因分析と課題整理)
3. [アーキテクチャ設計](#3-アーキテクチャ設計)
   - 3.1 [共通適用窓口 Global.ApplyProfileToSlot の設計](#31-共通適用窓口-globalapplyprofiletoslot-の設計)
   - 3.2 [4段階トランザクション同期メソッドの設計](#32-4段階トランザクション同期メソッドの設計)
4. [マイクロステップ実施計画（フェーズ構成）](#4-マイクロステップ実施計画フェーズ構成)
   - 4.1 [フェーズA: 共通適用メソッド Global.ApplyProfileToSlot の新設](#41-フェーズa-共通適用メソッド-globalapplyprofiletoslot-の新設)
   - 4.2 [フェーズB: 4段階トランザクション同期メソッドおよびガードの実装](#42-フェーズb-4段階トランザクション同期メソッドおよびガードの実装)
   - 4.3 [フェーズC: プロファイル保存契機（契機2）および手動選択（契機1）の統合](#43-フェーズc-プロファイル保存契機契機2および手動選択契機1の統合)
   - 4.4 [フェーズD: Rename（名前変更）契機（契機6）の統合](#44-フェーズd-rename名前変更契機契機6の統合)
   - 4.5 [フェーズE: 切換アクション（契機3）および自動プロファイル（契機4）の統合](#45-フェーズe-切換アクション契機3および自動プロファイル契機4の統合)
   - 4.6 [フェーズF: 総合テスト・実機検証・完了報告](#46-フェーズf-総合テスト実機検証完了報告)
5. [完了基準 (Definition of Done)](#5-完了基準-definition-of-done)
6. [リスク分析と対策](#6-リスク分析と対策)
7. [（2026-09-14追記）実地確認による追加発見事項と是正方針](#7-2026-09-14追記実地確認による追加発見事項と是正方針)

---

## 1. 概要と目的

### 1.1 目的
本計画は、Phase 5 Step 14 の一環として判明した**プロファイル保存時における WPF バインディング起因の強制クラッシュ（TargetException）を根本解消**するとともに、**接続中コントローラーのプロファイルに対する Rename（名前変更）時の参照破綻を防止**し、散在している**全プロファイル適用処理を共通窓口 `ApplyProfileToSlot` に完全一本化**することを目的とする。

---

## 2. 真因分析と課題整理

### 2.1 課題1: プロファイル保存時のクラッシュ (TargetException)
* **現象**: プロファイル編集画面で設定を変更して「保存」をクリックした直後、DS4Windows が強制終了する。
* **真因**:
  - プロファイル保存完了後、ディスク上の全 XML ファイルを正本（SSOT）としてプロファイル一覧を同期するため `ProfileListHolder.Refresh()` を呼び出していた。
  - `Refresh()` 内部で `profileListCol.Clear()` が実行されるが、コントローラー一覧画面（Controllersタブ）の ComboBox が特定アイテム（`ProfileEntity`）を選択した状態のままコレクションが空になる。
  - WPF のバインディングエンジン（`PropertyPathWorker`）が選択中アイテムを見失い、`System.Reflection.TargetException: Object does not match target type.` を投げてメインスレッドがクラッシュした。

### 2.2 課題2: Rename 時の参照・UI破綻
* **現象**: コントローラーに適用中のプロファイルを一覧画面の「Rename」ボタンで改名した際、ComboBox の選択項目が空白（未選択）になったり、内部パス（`Global.ProfilePath`）が旧名のまま取り残されて次回起動・再読込時に FileNotFound や初期化エラーが発生する。
* **真因**: ファイル名変更（`File.Move`）のみが行われ、接続中コントローラーの内部スロット情報、退避配列、および ComboBox の選択状態を一括更新・再同期するトランザクション処理が存在しない。

### 2.3 課題3: プロファイル適用経路の散在
* **現状**: アプリケーション全体でプロファイルを適用するコードが各機能（手動 ComboBox、編集保存、スペシャルアクション、自動プロファイル、ホットプラグ接続等）で個別に記述されており、コードの二重化と挙動不整合のリスクが存在する。

---

## 3. アーキテクチャ設計

### 3.1 共通適用窓口 Global.ApplyProfileToSlot の設計
コントローラーへプロファイルを安全に適用する単一の静的窓口を `Global`（`DS4Control/ScpUtil.cs`）に新設し、アプリ全体の 6 つの適用契機をすべてここを通過させる。

```csharp
public static bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source = ProfileChangeSource.Manual)
```

#### 【集約対象となる6つの契機】
1. **手動選択 (Manual)**: `MainWindow.xaml.cs` (`SelectProfCombo_SelectionChanged`)
2. **編集・保存時 (Save/Apply)**: `MainWindow.xaml.cs` (`Editor_ProfileSaved` 経由)
3. **切換アクション (SpecialAction)**: `DefaultProfileSwitcher.cs` (`SwitchProfile`)
4. **自動プロファイル (AutoProfile)**: `AutoProfileService.cs` (ウィンドウ検知時)
5. **初回接続時 (HotPlug)**: `ControlService.cs` (`PrepareConnectedInputController`)
6. **Rename時 (Rename)**: `MainWindow.xaml.cs` (`RenameProfileBtn_Click` 経由)

> **（2026-09-14追記）契機7の追加発見**: 上記6契機の実地確認（§7）の過程で、`MainWindow.xaml.cs` の `TrayIconVM_ProfileSelected`（トレイアイコンのコンテキストメニューからの選択）が独立した第7の手動選択契機として存在することが判明した。詳細は §7.2 事実3 を参照。

---

### 3.2 4段階トランザクション同期メソッドの設計
`MainWindow.xaml.cs` に配置し、保存時・更新時・リネーム時の UI およびオブジェクト整合性を完全に保証する。

```text
[SyncProfileListAndControllers(string oldProfile = null, string newProfile = null)]
  │
  ├─ ① 【退避 & ガード (Detach)】
  │     ・フラグ isProfileSyncing = true を設定（ComboBox の SelectionChanged 暴発を遮断）
  │     ・接続中コントローラー各スロット（0〜3）の現在のプロファイル名を string[] activeProfiles に退避
  │
  ├─ ② 【SSOT 再構築 (Rebuild)】
  │     ・もし Rename 時（oldProfile != null）なら：
  │         - activeProfiles 内の oldProfile を newProfile に置換
  │         - Global.ProfilePath 内の oldProfile を newProfile に置換
  │         - Global.OlderProfilePath 内の oldProfile を newProfile に置換
  │     ・ProfileListHolder.Refresh() を実行してディスク全XMLからリストを再生成
  │       （※isProfileSyncing ガード中のため、Clear() されても TargetException クラッシュは一切発生しない）
  │
  ├─ ③ 【復元・再選択 (Re-attach)】
  │     ・コントローラー画面の各スロット ComboBox の選択状態を activeProfiles に基づいて復元
  │     ・フラグ isProfileSyncing = false に戻す
  │
  └─ ④ 【再適用・ホットリロード (Re-apply)】
        ・保存またはリネームの影響を受けたスロットに対し、
          共通メソッド Global.ApplyProfileToSlot(i, activeProfiles[i], source) を実行して即座に最新設定を反映！
```

---

## 4. マイクロステップ実施計画（フェーズ構成）

### 4.1 フェーズA: 共通適用メソッド Global.ApplyProfileToSlot の新設
* **目的**: プロファイル適用の共通基盤を静的メソッドとして構築する。
* **対象ファイル**: `DS4Windows/DS4Control/ScpUtil.cs`
* **作業内容**:
  1. `Global` クラス内に `ApplyProfileToSlot` メソッドを追加。
  2. スロット境界チェック（`0 <= slotIndex < 4`）、空文字列チェック、およびコントローラー接続チェック（`Program.rootHub?.controllers[slotIndex] != null`）を実装。
  3. `Global.ApplyProfile`（8引数）へ安全に委譲。
* **完了検証**: `dotnet build` が警告・エラー 0 件で成功すること。

---

### 4.2 フェーズB: 4段階トランザクション同期メソッドおよびガードの実装
* **目的**: `MainWindow.xaml.cs` にデータバインディング保護フラグと同期メソッドを新設する。
* **対象ファイル**: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
* **作業内容**:
  1. イベント同期ガードフラグ `private bool isProfileSyncing = false;` を追加。
  2. `SelectProfCombo_SelectionChanged` の冒頭に `if (isProfileSyncing) return;` ガードを追加。
  3. 4段階同期メソッド `private void SyncProfileListAndControllers(string oldProfile = null, string newProfile = null)` を実装。
* **完了検証**: `dotnet build` が正常に通ること。

---

### 4.3 フェーズC: プロファイル保存契機（契機2）および手動選択（契機1）の統合
* **目的**: 保存時のクラッシュを根本解消し、手動選択を共通メソッドに繋ぎ込む。
* **対象ファイル**: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
* **作業内容**:
  1. `Editor_ProfileSaved` の中身を `SyncProfileListAndControllers();` の呼び出しに置き換え。
  2. `SelectProfCombo_SelectionChanged` 内のプロファイル適用処理を `Global.ApplyProfileToSlot` に差し替え。
* **完了検証**:
  - 実機にてプロファイル編集画面を開き、「保存」または「適用」を押しても **TargetException が発生せず正常に完了すること**。

---

### 4.4 フェーズD: Rename（名前変更）契機（契機6）の統合
* **目的**: 接続中プロファイルの名前変更時に、内部パス・UI ComboBox・動作状態をシームレスに追従させる。
* **対象ファイル**: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
* **作業内容**:
  1. `RenameProfileBtn_Click` において、ダイアログでのファイル名変更成功後に `SyncProfileListAndControllers(oldProfile, newProfile);` を呼び出すように改修。
* **完了検証**:
  - 接続中コントローラーで使用中のプロファイルを Rename した場合、UI の選択肢・選択中項目が新プロファイル名に正しくスライドし、コントローラーの動作が中断されないこと。

---

### 4.5 フェーズE: 切換アクション（契機3）および自動プロファイル（契機4）の統合
* **目的**: アプリ全体の残りすべてのプロファイル適用経路を共通窓口に集約する。
* **対象ファイル**:
  - `DS4Windows/Actions/DefaultProfileSwitcher.cs`
  - `DS4Windows/DS4Control/Services/AutoProfileService.cs`
* **作業内容**:
  1. `DefaultProfileSwitcher.SwitchProfile` を `Global.ApplyProfileToSlot(device, targetProfile, ProfileChangeSource.SpecialAction)` 呼び出しに統一。
  2. `AutoProfileService` の自動切り替え実行箇所を `Global.ApplyProfileToSlot(deviceIndex, autoProfile, ProfileChangeSource.AutoProfile)` 呼び出しに統一。
* **完了検証**:
  - 全単体テスト（169件）がすべて PASS すること。

---

### 4.6 フェーズF: 総合テスト・実機検証・完了報告
* **作業内容**:
  1. 保存クラッシュ防止、Rename 追従、ホットリロード、手動切り替えの全シナリオを実機検証。
  2. `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` の更新と完了報告。

---

## 5. 完了基準 (Definition of Done)

1. **クラッシュゼロ**:
   プロファイル編集画面で [保存] または [適用] を何度連続で行っても、`TargetException` やアプリクラッシュが一切発生しないこと。
2. **Rename完全追従**:
   接続中のプロファイルをリネームしても、コントローラーの動作・ComboBox表示・内部パス（`Global.ProfilePath`）の整合性が 100% 維持されること。
3. **一本化の達成**:
   アプリケーション内の全プロファイル適用処理が `Global.ApplyProfileToSlot` を通過していること。
4. **全テスト合格**:
   `dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj` が 169 passed / 0 failed であること。
5. **警告ゼロ**:
   ビルド時のコンパイラ警告が 0 件であること。

---

## 6. リスク分析と対策

| リスク | 影響度 | 対策 |
| :--- | :---: | :--- |
| ComboBox 再選択時のイベント暴発 | 高 | `isProfileSyncing` フラグにより、同期中は `SelectionChanged` を完全にスキップする。 |
| 未接続スロットへの誤適用 | 中 | `ApplyProfileToSlot` 冒頭で `Program.rootHub?.controllers[slotIndex]` の接続状態を必ず検証する。 |
| 大量プロファイル環境での遅延 | 低 | `ProfileListHolder.Refresh()` はメモリ上の高速な XML パースであり、UI フリーズは生じない。 |

---

## 7. （2026-09-14追記）実地確認による追加発見事項と是正方針

### 7.1 経緯

ユーザーより、別エージェントが作成した「通知関連メソッド・設定値の全体棚卸し」文書（プロファイル適用通知とシステム通知が2系統絡み合っている、という指摘）の提供を受けた。当該文書は実コードに基づかない一般論としての棚卸しであったため、`.github/copilot-instructions.md` §6.11「実地確認の原則」に従い、`git`（`raw.githubusercontent.com` 経由）から本ブランチの実ファイル（`MainWindow.xaml.cs`, `ProfileNotificationWindow.xaml.cs`, `Log.cs`, `ScpUtil.cs`, `ProfileEditor.xaml.cs`, `ProfileApplicationService.cs`, `IProfileApplicationService.cs`, `DefaultProfileSwitcher.cs`, `AutoProfileService.cs`, `SettingsViewModel.cs`）を取得し、実地 grep 調査を実施した。

調査の結果、ユーザー提供文書が懸念していた「通知の2系統混線」とは**別の、より具体的かつ重大な構造的不整合**が、本計画書がまさに新設した `Global.ApplyProfileToSlot`（§3.1・§4.1）を起点に発生していることが判明した。以下に確定事実として記録する。

### 7.2 確認された事実（実コード引用ベース）

#### 事実1: プロファイル切替通知の「表示可否」判定に、設定基準が異なる2つの実装が並存している

- **`Global.ApplyProfileToSlot`**（`ScpUtil.cs`、本計画のフェーズAで新設）は、以下の実装になっている。

  ```csharp
  // ★通知設定の有効・無効を一元反映（Notifications != 0 であれば通知を表示）
  bool shouldDisplayNotification = Global.Notifications != 0;
  return ApplyProfile(slotIndex, profileName, false, false, Program.rootHub, source, prolog, shouldDisplayNotification);
  ```

  すなわち、**「通知を表示」コンボボックス（`Global.Notifications`、値0/1/2。本来はトレイバルーン通知の重大度設定）** を基準に、プロファイル切替の独自通知ウィンドウ（`ProfileNotificationWindow`）を出すか否かを決定している。

- 一方、既存の DI サービス **`IProfileApplicationService.ApplyProfile`**（`ProfileApplicationService.cs`、Phase5の先行Stepで構築済み・Pure DI準拠）は、以下の実装になっている。

  ```csharp
  bool shouldDisplay = displayNotification ?? _profileSettings?.ProfileChangedNotification ?? false;
  ```

  すなわち、**「Display profile switch notification」チェックボックス（`IProfileSettingsService.ProfileChangedNotification`）** を基準にしている。これはユーザー提供の棚卸し文書が「機能2（プロファイル適用通知）の判定基準はこのチェックボックスのみであるべき」と指摘していた、設計意図に合致する正しい実装である。

  → **同一の関心事（プロファイル切替時に独自通知ウィンドウを出すか否か）に対し、参照する設定値が異なる2つの実装がリポジトリ内に共存している。**

#### 事実2: 契機ごとに、どちらの実装を通るかが不統一である（実地確認）

実際のコード追跡の結果、6つ（後述の通り実際は7つ）の適用契機は、以下のように**2つの経路に分裂**していることを確認した。

| 契機 | 呼び出し元 | 経由する実装 | 通知判定基準 |
|---|---|---|---|
| 手動選択（メインウィンドウComboBox） | `MainWindow.xaml.cs` `SelectProfCombo_SelectionChanged`（L1059） | `Global.ApplyProfileToSlot` | `Global.Notifications`（誤り） |
| 編集・保存時 | `MainWindow.xaml.cs` `Editor_ProfileSaved` → `SyncProfileListAndControllers`（L1991） | `Global.ApplyProfileToSlot` | `Global.Notifications`（誤り） |
| 切換アクション（SpecialAction） | `DefaultProfileSwitcher.cs`（L68/L107/L132、フォールバック時のみL74/L112/L137で`Global.ApplyProfile`） | `IProfileApplicationService` | `ProfileChangedNotification`（正） |
| 自動プロファイル | `AutoProfileService.cs`（L116/L168） | `IProfileApplicationService` | `ProfileChangedNotification`（正） |
| **（未列挙）トレイアイコン メニューからの選択** | `MainWindow.xaml.cs` `TrayIconVM_ProfileSelected`（L364） | `IProfileApplicationService` | `ProfileChangedNotification`（正） |

（初回接続時／Rename時の2契機は、本計画書執筆時点でまだ `ApplyProfileToSlot` に未接続、または未実装のため対象外。§7.4参照）

この結果、**同じ「ユーザーによる手動プロファイル切り替え」という操作であっても、メインウィンドウのComboBoxから選んだ場合とトレイアイコンのメニューから選んだ場合とで、「Display profile switch notification」チェックボックスをOFFにしても一方は通知が出続ける、という体感できる不整合が既に発生し得る状態にある。**

#### 事実3: §3.1「集約対象となる6つの契機」の列挙に漏れがあった

`MainWindow.xaml.cs` の `TrayIconVM_ProfileSelected`（トレイアイコンのコンテキストメニューからのプロファイル選択）は、独立した手動選択の契機でありながら、本計画書 §3.1 の「集約対象となる6つの契機」には列挙されていなかった。これも契機7として追加登録する必要がある。

> **契機7（追加）**: トレイアイコン メニューからの選択 (TrayIconMenu): `MainWindow.xaml.cs` (`TrayIconVM_ProfileSelected`)

### 7.3 是正方針（要ユーザー承認・未実装）

§4.5「フェーズE」は、現状の計画のまま実行すると（`DefaultProfileSwitcher`/`AutoProfileService` を `IProfileApplicationService` から `Global.ApplyProfileToSlot` 呼び出しへ差し替える）、以下の**2つの後退**を生む恐れがあるため、実装前に方針の見直しが必要と判断する。

1. `.github/copilot-instructions.md` §3.1「Pure DI（純粋コンストラクタ注入）の堅持」に反し、既にDIサービス経由で正しく動作している箇所を、わざわざ新設の static メソッド呼び出しに巻き戻すことになる。
2. 正しく `ProfileChangedNotification` を参照している判定を、誤った `Global.Notifications` 判定に置き換えてしまう回帰を生む。

そこで、以下2案のいずれかを提案する。**いずれも本計画書の目的（§1.1「散在している全プロファイル適用処理を共通窓口に完全一本化する」）に資するが、影響範囲・工数が異なるため、着手前にユーザーの承認を得たい。**

| 案 | 内容 | 長所 | 短所 |
|---|---|---|---|
| **案①（推奨）** | `Global.ApplyProfileToSlot` 自体を `IProfileApplicationService.ApplyProfile` への薄い委譲（シム）として書き直す。`shouldDisplayNotification` の独自算出をやめ、`displayNotification` 引数を渡さない（`null`のまま）ことで `IProfileApplicationService` 側の `ProfileChangedNotification` 解決に委ねる。フェーズEは「`DefaultProfileSwitcher`/`AutoProfileService` を `Global.ApplyProfileToSlot` へ差し替える」のではなく、**現状維持（既にDIサービス経由のため対応不要）** に変更する。 | Pure DI原則に合致。判定基準を1箇所（`IProfileApplicationService`）に真に一本化できる。§2.1のフォールバック/シム維持原則（静的経路を薄いラッパーとして残す）にも合致。 | `Global.ApplyProfileToSlot` の実装（フェーズAで新設したばかりのコード）に手を入れる必要がある。`Program.rootHub` 依存箇所の扱いを`IProfileApplicationService`側の実装と整合させる要確認事項が残る（後述リスク）。 |
| 案②（最小変更） | `Global.ApplyProfileToSlot` 内の一行 `Global.Notifications != 0` を `Global.ProfileChangedNotification` に置き換えるのみ。 | 変更行数が最小。 | 「2つの実装が同じ関心事を別々に持つ」という構造的重複自体は解消されない。将来また同種の齟齬が再発するリスクが残る。 |

**本追記時点ではコードは一切変更していない。** 案①・案②のどちらを採用するか、またはフェーズE自体の対象ファイル・作業内容（上表の見直し）について、ユーザーの指示を得てから実装に着手する。

### 7.4 フェーズC実装状況の実地確認結果（ドキュメント記載との齟齬）

`Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` は本追記時点で §4.3 フェーズC のタスク C-1・C-2 を `[ ]` 未着手と記載していたが、実コード確認の結果、**両タスクとも既に実装済み**であることを確認した。

- **C-1**（`Editor_ProfileSaved` を `SyncProfileListAndControllers();` 呼び出しに置き換え）: `MainWindow.xaml.cs` L2002-2006 に実装済み。
- **C-2**（`SelectProfCombo_SelectionChanged` 内の適用処理を `Global.ApplyProfileToSlot` に差し替え）: `MainWindow.xaml.cs` L1059 に実装済み。

一方、**C-3**（実機テストによる保存時クラッシュ解消確認）は未実施のまま残っている。したがって、フェーズCは「コード実装完了・実機確認待ち」という状態であり、Status.md 側の記載を実態に合わせて訂正する（別紙 `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` 更新版を参照）。

なお、フェーズD（Rename契機の統合）については、`MainWindow.xaml.cs` の `RenameProfBtn_Click` を実地確認した結果、`SyncProfileListAndControllers(oldProfile, newProfile)` の呼び出しは**まだ追加されておらず**、Status.md の「未着手」という記載と実装状況は一致していることを確認した（齟齬なし）。