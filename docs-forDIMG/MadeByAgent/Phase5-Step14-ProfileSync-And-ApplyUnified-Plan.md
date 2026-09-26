# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期 実装計画書

- **Document**: `docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md`
- **Target Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Base Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Author**: Agent (Claude-based assistant)
- **Status**: 計画策定完了 (Ready for Implementation) ／ フェーズD（契機5統合、新設）・フェーズF（通知機能の完全分離）・フェーズG（通知判定基準の統一・案①採用）が2026-09-14付でユーザー承認済み。**フェーズDから実装着手**
- **Created Date**: 2026-09-14
- **Last Updated**: 2026-09-14（契機5＝HotPlugの共通窓口統合を新規フェーズDとしてフェーズCの直後に挿入。既存フェーズD〜Gを1つずつ繰り下げ（D→E, E→F, F→G, G→H）。§3.1に契機7・契機8を正式統合）

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
   - 4.4 [フェーズD: 契機5（初回接続時／HotPlug）の共通窓口統合](#44-フェーズd-契機5初回接続時hotplugの共通窓口統合2026-09-14新設)
   - 4.5 [フェーズE: Rename（名前変更）契機（契機6）の統合](#45-フェーズe-rename名前変更契機契機6の統合)
   - 4.6 [フェーズF: 通知機能の完全分離（システム通知とプロファイル切替通知の独立実装）](#46-フェーズf-通知機能の完全分離システム通知とプロファイル切替通知の独立実装2026-09-14新設2026-09-14改称旧フェーズe)
   - 4.7 [フェーズG: 通知判定基準の統一（案①実装）およびDI経由契機の整合性確認](#47-フェーズg-通知判定基準の統一案実装およびdi経由契機の整合性確認2026-09-14改訂2026-09-14さらに改称旧フェーズf)
   - 4.8 [フェーズH: 総合テスト・実機検証・完了報告](#48-フェーズh-総合テスト実機検証完了報告)
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
コントローラーへプロファイルを安全に適用する単一の静的窓口を `Global`（`DS4Control/ScpUtil.cs`）に新設し、アプリ全体の 7 つの適用契機をすべてここを通過させる（**注**: 契機8「ホットキー経由」は既存プロファイルの再通知に過ぎずプロファイル適用処理そのものではないため、`ApplyProfileToSlot`の集約対象には含めない。§4.6参照）。（**2026-09-14改訂**: 実地確認により発見された契機7・8を正式に統合。詳細な発見経緯は §7.2 事実3、§4.6 を参照）

```csharp
public static bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source = ProfileChangeSource.Manual)
```

#### 【集約対象となる7つの適用契機、および通知イベントに関わる契機8】
1. **手動選択 (Manual)**: `MainWindow.xaml.cs` (`SelectProfCombo_SelectionChanged`)
2. **編集・保存時 (Save/Apply)**: `MainWindow.xaml.cs` (`Editor_ProfileSaved` 経由)
3. **切換アクション (SpecialAction)**: `DefaultProfileSwitcher.cs` (`SwitchProfile`)
4. **自動プロファイル (AutoProfile)**: `AutoProfileService.cs` (ウィンドウ検知時)
5. **初回接続時 (HotPlug)**: `ControlService.cs` (`PrepareConnectedInputController`)
6. **Rename時 (Rename)**: `MainWindow.xaml.cs` (`RenameProfileBtn_Click` 経由)
7. **トレイアイコン メニューからの選択 (TrayIconMenu)**（**2026-09-14追加**）: `MainWindow.xaml.cs` (`TrayIconVM_ProfileSelected`)
8. **ホットキー経由の切替 (Hotkey)**（**2026-09-14追加**）: `MainWindow.xaml.cs` (`ShowHotkeyNotification` 経由で `AppLogger.LogProfileChanged` を直接呼び出し。§4.6・§7参照)

> 契機7・8は、実地確認（§7・§4.6）により当初の棚卸しに漏れていたことが判明し、2026-09-14付でユーザー承認のうえ正式に契機一覧へ統合した。契機3・4・7は、いずれも既に `IProfileApplicationService`（DIサービス）経由で `Global.ApplyProfileToSlot` を介さずに実装されている点に注意（§4.7改訂版参照）。契機8はプロファイル適用そのものではなく通知イベント発火のみに関わる特殊な契機であり、§4.6のフェーズFで是正する。契機5（初回接続時／HotPlug）は当初 `ApplyProfileToSlot` を経由していなかったが、フェーズD（§4.4）で統合済みである。「共通窓口をすべて通過させる」という設計方針自体は維持しつつ、実装上は §4.7 で述べる通り `Global.ApplyProfileToSlot` を `IProfileApplicationService` への薄い委譲として書き直すことで、静的経路・DI経路のどちらから入っても最終的に同一の実処理・同一の通知判定基準を通るようにする。

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

### 4.4 フェーズD: 契機5（初回接続時／HotPlug）の共通窓口統合 【2026-09-14新設】

> **新設の経緯**: §3.1で契機5として当初から契機一覧に列挙されていたにもかかわらず、実地確認（§7、および本ドキュメント更新の過程での再調査）の結果、`ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`）が共通窓口 `Global.ApplyProfileToSlot` を経由せず、旧来の `Global.ApplyProfile`（8引数の静的メソッド）を直接呼び出し続けていることが判明した。これは本計画の目的（§1.1）に照らして明確な抜け漏れであるため、ユーザー承認のうえ新規フェーズDとしてフェーズC直後に挿入する。既存のフェーズD（Rename統合）以降は全てフェーズを1つずつ繰り下げる。

* **目的**: 契機5（`ControlService.cs` の初回接続時プロファイル適用）を、他の契機と同じく共通窓口 `Global.ApplyProfileToSlot` 経由に統一する。
* **対象ファイル**:
  1. `DS4Windows/DS4Control/ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`。主対象）
  2. `DS4Windows/DS4Control/ScpUtil.cs`（`Global.ApplyProfileToSlot`。付随的な暫定是正、下記参照）
* **現状の実装（変更前）**:
  ```csharp
  // ControlService.cs（PrepareConnectedInputControllerSettingEvents 内）
  string prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile, (index + 1).ToString(), profileToApply, "N/A");
  bool display = _profileSettings.ProfileChangedNotification;

  profileLoaded = Global.ApplyProfile(index, profileToApply, false, false, this,
      DS4Windows.ProfileChangeSource.ControlService, prolog, display);
  ```
* **作業内容**:
  1. 上記の直接呼び出しを、以下のように `Global.ApplyProfileToSlot` 呼び出しへ置き換える。
     ```csharp
     profileLoaded = Global.ApplyProfileToSlot(index, profileToApply, DS4Windows.ProfileChangeSource.ControlService);
     ```
     - `this`（`ControlService` インスタンス）は `Program.rootHub`（`App.xaml.cs` にてDIコンテナから解決された同一のシングルトンインスタンス）と常に一致するため、`ApplyProfileToSlot` 内部で `Program.rootHub` を用いても実行時の挙動に差異は生じない。
     - 通知メッセージ（`prolog`）は `ApplyProfileToSlot` が内部で統一フォーマットにより生成するため、明示的な組み立ては不要になる。バッテリー表示欄が `"N/A"` から空文字列に変わる軽微な表示差分が生じるが、これは契機1・2・6と表示形式を揃える意図した副次効果であり、実害はない。
  2. **付随的な暫定是正（No Feature Drop対応、必須）**: `Global.ApplyProfileToSlot` は現時点で `Global.Notifications`（誤った基準）を用いて通知表示可否を判定している（§7参照、フェーズGで完全是正予定）。契機5は従来 `_profileSettings.ProfileChangedNotification`（正しい基準）を用いていたため、本フェーズの統合をそのまま行うと契機5の通知判定が一時的に誤った基準に巻き込まれ、退行（regression）となる。これは `.github/copilot-instructions.md` §2.2「現在の機能の完全維持 (No Feature Drop)」に反するため、フェーズGでの完全委譲化を待たず、本フェーズの一部として `ApplyProfileToSlot` 内の該当行のみ先行是正する。
     ```csharp
     // 変更前: bool shouldDisplayNotification = Global.Notifications != 0;
     // 変更後（フェーズDによる暫定是正）:
     bool shouldDisplayNotification = Global.ProfileChangedNotification;
     ```
     この一行修正は、フェーズGで計画されている `IProfileApplicationService` への完全委譲によって置き換えられる暫定措置である。フェーズGの作業内容・完了検証はこれを前提に更新済み（§4.7参照）。
* **完了検証**:
  - `dotnet build` が警告・エラー 0 件で成功すること。
  - 全単体テスト（169件）がすべて PASS すること。
  - 実機にて、コントローラー初回接続時・再接続時にプロファイルが正しく適用され、`display`（`ProfileChangedNotification`）の設定通りに独自通知ウィンドウの表示・非表示が切り替わること（暫定是正により退行がないことの確認）。

---

### 4.5 フェーズE: Rename（名前変更）契機（契機6）の統合
* **目的**: 接続中プロファイルの名前変更時に、内部パス・UI ComboBox・動作状態をシームレスに追従させる。
* **対象ファイル**: `DS4Windows/DS4Forms/MainWindow.xaml.cs`
* **作業内容**:
  1. `RenameProfileBtn_Click` において、ダイアログでのファイル名変更成功後に `SyncProfileListAndControllers(oldProfile, newProfile);` を呼び出すように改修。
* **完了検証**:
  - 接続中コントローラーで使用中のプロファイルを Rename した場合、UI の選択肢・選択中項目が新プロファイル名に正しくスライドし、コントローラーの動作が中断されないこと。

---

### 4.6 フェーズF: 通知機能の完全分離（システム通知とプロファイル切替通知の独立実装）【2026-09-14新設・2026-09-14改称（旧フェーズE）・2026-09-15実機検証を経て仕様確定・実装完了】

#### 背景（実地確認による裏付け）

`MainWindow.xaml.cs` の実コードを確認した結果、「通知を表示」（`Global.Notifications`、システム通知の重大度設定）と「Display profile switch notification」（`Global.ProfileChangedNotification`、プロファイル切替通知専用スイッチ）という**本来独立しているべき2つの設定**が、実装上は次のように絡み合っていることを確認した。

| メソッド | 現状の実装 | 問題点 |
|---|---|---|
| `ShowNotification(object sender, DebugEventArgs e)`（L369、`AppLogger.TrayIconLog`購読） | `appSettingsService.Notifications` を正しく判定し、トレイ通知（`notifyIcon.ShowNotification`）のみを出す | 問題なし。実質的に「機能1（システム通知）」として既に独立している |
| `ShowProfileChangeNotification(string message, bool isWarning)`（L401） | **設定値の判定を一切行わず**、無条件に `ProfileNotificationWindow.ShowNotification(message)` を呼ぶ。引数 `isWarning` は未使用 | 「Display profile switch notification」チェックボックスの状態を全く見ていない |
| `OnProfileChanged(object sender, ProfileChangedEventArgs e)`（L755、`AppLogger.ProfileChanged`購読） | メッセージ文字列を組み立てて `ShowProfileChangeNotification` を無条件に呼ぶだけ | 上記と同じ問題がそのまま伝播 |
| `ShowHotkeyNotification(string message)`（L697、**契機8として新規発見**：ホットキー経由のプロファイル切替） | `appSettingsService.Notifications == 2` のときのみ `AppLogger.LogProfileChanged(...)` を呼ぶ（＝これが`true`でなければ`ProfileChanged`イベント自体が発火しない） | 「システム通知」の重大度設定（`Notifications`）でプロファイル切替通知の発火可否を決めてしまっている。`Global.ApplyProfileToSlot`と同種の誤りが、ここにも独立に存在する |
| `AppLogger.LogProfileChanged(...)`（`Log.cs` L84、`displayNotification`引数、既定値`true`） | `displayNotification` が `false` の場合、`ProfileChanged` イベント自体を発火しない（`Log.cs` L93-100） | 「表示可否の最終判定」と「イベントを発火するか否か」が同じ1個のboolに同居しており、呼び出し元（契機）ごとに異なる設定値で計算されてしまう根本原因 |
| `SettingsViewModel.cs` の `IsProfileChangedCheckVisible`（**2026-09-15実機検証で新規発見**） | `ShowNotificationsIndex`（＝`Notifications`）が「すべて」(2) でない限り、`Display profile switch notification` チェックボックス自体を `Visibility.Collapsed` にしてUIから隠す | UIレベルでも2設定が絡み合っており、「通知を表示」が「すべて」以外だとプロファイル切替通知の設定自体を変更できない（実機検証で発覚した問題1） |

この結果、実機検証（2026-09-15）にて以下2点の不具合として顕在化した。

- **問題1**: 「通知を表示」が「すべて」になっていないと、「Display profile switch notification」チェックボックスがUI上から消え、設定変更ができない。
- **問題2**: 「通知を表示」を「すべて」にしていても、コントローラーの接続・切断時にトースト通知が表示されない（接続時のプロファイル適用通知が、常に独自ウィンドウ側の`Log.ProfileChanged`経路のみに固定されており、トースト経路と接続していなかったための仕様上のギャップ）。

#### 確定仕様（2026-09-15、ユーザー指示に基づき確定）

* **チェックボックス（`Display profile switch notification`）**:
  - UI表示は「通知を表示」の設定値に関わらず**常に表示**する（問題1の是正）。
  - ON: プロファイル**適用**時（表示上は「切替」だが、仕様上は手動切替に限らずあらゆる適用契機を対象とする）に、独自ウィンドウによる通知を表示する。
  - OFF: 独自ウィンドウは表示しない。
* **リストボックス（`通知を表示`、`Global.Notifications`）**:
  - 選択レベルにより、通常のシステム通知（トースト）の表示可否を制限する（既存仕様通り）。
  - チェックボックスがONの場合、プロファイル適用イベントの通知は**トーストでは一切表示しない**（独自ウィンドウのみに一本化）。
  - チェックボックスがOFFの場合、プロファイル適用イベントの通知は**通常のシステム通知と同列に扱い**、リストボックスのレベル判定に従ってトースト表示する（プロファイル適用は警告ではないため、実質「すべて」選択時のみ表示される）。

#### 目的

「システム通知」と「プロファイル切替通知」を、**設定値・出力先ともに完全に独立した2つのメソッド**として実装し直し、以後どの契機から呼ばれても常に一貫した判定結果になるようにする。

* **機能1：システム通知**（`ShowSystemNotification`） — 判定基準は `Global.Notifications`（0/1/2）のみ。出力先は Windows トースト（`notifyIcon.ShowNotification`）のみ。プロファイル関連の処理には一切関与しない。
* **機能2：プロファイル切替通知**（`ShowProfileSwitchNotification`） — 出力先は独自デスクトップ通知（`ProfileNotificationWindow.ShowNotification`）のみ。**表示可否の判定（`ProfileChangedNotification`）は呼び出し元（`OnProfileChanged`）が行い**、本メソッド自体は無条件に表示する（機能1・機能2どちらへ振り分けるかの判定を一箇所に集約するため）。

#### 対象ファイル

1. `DS4Windows/DS4Forms/MainWindow.xaml.cs`（主対象）
2. `DS4Windows/DS4Control/Log.cs`（`displayNotification` 引数の意味づけの純化）
3. `DS4Windows/DS4Forms/ProfileNotificationWindow.xaml.cs`（**変更なし**。単一ウィンドウ・タイマー延長ロジックは既に適切なため確認のみ）
4. `DS4Windows/DS4Forms/MainWindow.xaml`（**2026-09-15追加**。チェックボックスの`Visibility`バインディング撤去）
5. `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs`（**2026-09-15追加**。`IsProfileChangedCheckVisible`関連コードの完全削除）

#### 作業内容（2026-09-15、実機検証結果を踏まえ確定・実装済み）

1. **機能1の整理・改名**: `ShowNotification(object sender, DebugEventArgs e)` を `ShowSystemNotification` へ改名。判定ロジック・購読先 `AppLogger.TrayIconLog` は変更しない。あわせて `ShowSystemNotification(string message, bool isWarning)` という文字列ベースのオーバーロードを新設し、`OnProfileChanged`（機能2 OFF時）からも共用できるようにする。
   ```csharp
   private void ShowSystemNotification(object sender, DS4Windows.DebugEventArgs e)
   {
       if (e.Temporary) return;
       if (!string.IsNullOrEmpty(e.Data) && (e.Data.StartsWith("[DI]") || e.Data.StartsWith("[Legacy]"))) return;
       Dispatcher.BeginInvoke((Action)(() => ShowSystemNotification(e.Data, e.Warning)));
   }

   private void ShowSystemNotification(string message, bool isWarning)
   {
       if (appSettingsService.Notifications == 2 || (appSettingsService.Notifications == 1 && isWarning))
       {
           if (notifyIcon.IsCreated)
           {
               try { notifyIcon.ShowNotification(TrayIconViewModel.ballonTitle, message,
                   !isWarning ? H.NotifyIcon.Core.NotificationIcon.Info : H.NotifyIcon.Core.NotificationIcon.Warning); }
               catch (System.InvalidOperationException) { }
           }
       }
   }
   ```
2. **機能2の新設**: `ShowProfileChangeNotification(string message, bool isWarning)` を廃止し、以下のシグネチャで `ShowProfileSwitchNotification` を新設する。判定を持たない点に注意（判定は呼び出し元 `OnProfileChanged` が行う）。
   ```csharp
   private void ShowProfileSwitchNotification(string message)
   {
       try { ProfileNotificationWindow.ShowNotification(message); }
       catch { /* プロファイル通知失敗は無視 */ }
   }
   ```
3. **`OnProfileChanged` を機能1/機能2の振り分けポイントとして書き換え**（確定仕様の中核）:
   ```csharp
   if (profileSettingsService.ProfileChangedNotification)
   {
       ShowProfileSwitchNotification(prolog);      // 機能2: 独自ウィンドウのみ
   }
   else
   {
       ShowSystemNotification(prolog, false);      // 機能1: 通常のトーストと同列（isWarning: false）
   }
   ```
4. **契機8の是正**: `ShowHotkeyNotification` 内の `if (appSettingsService.Notifications == 2)` ガードを撤去し、常に `AppLogger.LogProfileChanged(-1, message, false, ProfileChangeSource.Hotkey)` を呼ぶ（イベントは常に発火させ、表示可否は `OnProfileChanged` に一任）。
5. **`Log.cs` の意味づけ純化**: `LogProfileChanged` の `displayNotification` 引数のXMLドキュメントコメントを、「UIに表示するか否か」ではなく「イベント発火可否（サイレント内部処理を除外するためのフラグ。既定`true`）」に更新。実装は変更なし。
6. **（2026-09-15追加）問題1の是正**: `MainWindow.xaml` のチェックボックスから `Visibility="{Binding IsProfileChangedCheckVisible}"` を削除。`SettingsViewModel.cs` から `IsProfileChangedCheckVisible` プロパティ・バッキングフィールド・イベント、および `ShowNotificationsIndex` セッター内・コンストラクタ内の関連代入コードを完全削除。
7. 契機1・2・5・6・7・8のいずれも、最終的に `AppLogger.ProfileChanged` イベント経由で `OnProfileChanged` に到達し、確定仕様に基づく同一の振り分けロジックを受けることを確認する。

#### 本フェーズがフェーズG（§4.7、通知判定基準の統一）に与える影響

本フェーズの実装により、`displayNotification` パラメータの役割が「表示可否の最終判定結果」から「プロファイル変更イベントを発火してよいか（通常は常に`true`）」という単純な意味に変わる。これにより、フェーズGで計画している `Global.ApplyProfileToSlot` → `IProfileApplicationService.ApplyProfile` への委譲実装がシンプルになる（単純に `true` を渡せばよくなる。フェーズGの該当箇所は本フェーズ完了を前提に更新済み）。**このため、本フェーズはフェーズGより先に実施する。**

#### 完了検証

* `dotnet build` が警告・エラー 0 件で成功すること。
* 全単体テスト（169件）がすべて PASS すること。
* 実機にて、「通知を表示」がどの設定値でも「Display profile switch notification」チェックボックスが常に表示・操作可能であること（問題1の解消確認）。
* 実機にて、チェックボックスON時、「通知を表示」の設定値に関わらずプロファイル適用時に独自デスクトップ通知が表示されること。
* 実機にて、チェックボックスOFF時、「通知を表示」＝「すべて」であればプロファイル適用時にトースト通知が表示され、それ以外のレベルでは表示されないこと（問題2の解消確認）。
* 実機にて、コントローラー接続時・切断時の通知が、上記の確定仕様通りに動作すること。
* 実機にて、ホットキー経由のプロファイル切替（契機8）についても、上記と同様に確定仕様通りに動作すること。

---

### 4.7 フェーズG: 通知判定基準の統一（案①実装）およびDI経由契機の整合性確認 【2026-09-14改訂・2026-09-14さらに改称（旧フェーズF）】

> **改訂の経緯（要約）**: 本フェーズは当初「フェーズE」として計画され、その後の改訂・新設フェーズの挿入により「フェーズF」を経て「フェーズG」に改称された（新設順: 契機5統合フェーズ→§4.4、通知機能の完全分離フェーズ→§4.6、いずれも本フェーズの前に挿入）。当初の計画は「`DefaultProfileSwitcher`/`AutoProfileService` の呼び出しを `Global.ApplyProfileToSlot` に差し替える」内容であったが、§7 の実地確認により、これら2箇所（および契機7 `TrayIconVM_ProfileSelected`）は既に `IProfileApplicationService`（DIサービス、Pure DI準拠）経由で正しく実装されており、かつ通知判定基準（`ProfileChangedNotification`）も正しいことが判明した。一方、`Global.ApplyProfileToSlot` 自身（契機1・2・5・6が経由）は誤った判定基準（`Global.Notifications`。契機5統合時にフェーズDで`ProfileChangedNotification`へ暫定是正済み、§4.4参照）を使用していた。2026-09-14付でユーザーより **案①（`Global.ApplyProfileToSlot` を `IProfileApplicationService.ApplyProfile` への薄い委譲に書き直す）** の採用が承認されたため、本フェーズの目的・対象・作業内容を以下の通り全面的に差し替える。差し替え前の当初計画（`Global.ApplyProfileToSlot` への差し替え）は撤回する。

* **目的**: `Global.ApplyProfileToSlot` を `IProfileApplicationService.ApplyProfile` への薄い委譲（シム）として書き直し、プロファイル切替通知の表示判定基準を `IProfileSettingsService.ProfileChangedNotification` の1箇所に真に一本化する。あわせて、契機3・4・7（DIサービス経由の既存実装）が無改修のまま整合していることを確認する。
* **対象ファイル**:
  1. `DS4Windows/DS4Control/ScpUtil.cs`（`Global.ApplyProfileToSlot` の実装修正。**唯一の変更対象**）
  2. `DS4Windows/Actions/DefaultProfileSwitcher.cs`（**変更なし**。既存の `IProfileApplicationService` 経由実装を維持することを回帰テストで確認するのみ）
  3. `DS4Windows/DS4Control/Services/AutoProfileService.cs`（**変更なし**。同上）
  4. `DS4Windows/DS4Forms/MainWindow.xaml.cs`（`TrayIconVM_ProfileSelected`。**変更なし**。同上）
  5. `DS4Windows/DS4Control/ControlService.cs`（`PrepareConnectedInputControllerSettingEvents`。**変更なし**。フェーズDで既に `Global.ApplyProfileToSlot` 経由に統合済みのため、本フェーズの委譲化がそのまま自動的に波及することを確認するのみ）
* **作業内容**:
  1. `Global.ApplyProfileToSlot` 内の以下の実装（フェーズDで暫定是正済みの版。§4.4参照）を削除する。
     ```csharp
     // ★通知設定の有効・無効を一元反映（フェーズDで ProfileChangedNotification に暫定是正済み）
     bool shouldDisplayNotification = Global.ProfileChangedNotification;
     return ApplyProfile(slotIndex, profileName, false, false, Program.rootHub, source, prolog, shouldDisplayNotification);
     ```
  2. 代わりに、DIコンテナから `IProfileApplicationService` を解決し、これへ委譲する形に書き換える。
     ```csharp
     var appService = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IProfileApplicationService>();
     if (appService != null)
     {
         return appService.ApplyProfile(slotIndex, profileName, isTemp: false, launchProgram: false,
             source: source, prolog: prolog, displayNotification: true);
     }
     // DI未解決時のフォールバック（§2.1 フォールバック/シム維持原則）
     return ApplyProfile(slotIndex, profileName, false, false, Program.rootHub, source, prolog,
         displayNotification: true);
     ```
     - `displayNotification` は §4.6改訂の結果、「表示可否の判定結果」ではなく「イベントを発火してよいか（＝サイレント内部処理でないか）」という単純な意味になるため、通常の手動操作・保存操作である契機1・2・5・6では常に `true` を渡す。実際に画面へ表示するかどうかは、§4.6で新設する `ShowProfileSwitchNotification` が `ProfileChangedNotification` を見て自律的に判定する。
  3. フェーズAで実装済みのスロット境界チェック・空文字列チェック（`Global.ApplyProfileToSlot` 冒頭のガード）は変更せず残置する。
  4. `DefaultProfileSwitcher.cs`・`AutoProfileService.cs`・`MainWindow.xaml.cs`（`TrayIconVM_ProfileSelected`）・`ControlService.cs`（契機5、フェーズD後）は無改修のまま据え置く。
* **完了検証**:
  - `dotnet build` が警告・エラー 0 件で成功すること。
  - 全単体テスト（169件）がすべて PASS すること。
  - `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` §4 の TC-06（メインウィンドウComboBoxとトレイアイコンメニューの双方で、「Display profile switch notification」チェックボックスの状態に応じた通知表示が一致すること）が実機で確認できること。

---



### 4.8 フェーズH: 総合テスト・実機検証・完了報告
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
   全ての適用契機（7契機、§3.1参照。契機8は通知イベントのみに関わるため対象外）が、`Global.ApplyProfileToSlot` を経由するもの・`IProfileApplicationService.ApplyProfile` を直接経由するものの別を問わず、最終的に同一の実処理・同一の通知判定基準（`IProfileSettingsService.ProfileChangedNotification`）を通過していること（**2026-09-14改訂**: §4.7の改訂に伴い、「`Global.ApplyProfileToSlot` の物理的な通過」ではなく「判定ロジックの実質的な一本化」を基準とする）。
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

### 7.3 是正方針（2026-09-14 ユーザー承認済み）

§4.7（フェーズG、当初「フェーズE」）は、現状の計画のまま実行すると（`DefaultProfileSwitcher`/`AutoProfileService` を `IProfileApplicationService` から `Global.ApplyProfileToSlot` 呼び出しへ差し替える）、以下の**2つの後退**を生む恐れがあるため、実装前に方針の見直しが必要と判断した。

1. `.github/copilot-instructions.md` §3.1「Pure DI（純粋コンストラクタ注入）の堅持」に反し、既にDIサービス経由で正しく動作している箇所を、わざわざ新設の static メソッド呼び出しに巻き戻すことになる。
2. 正しく `ProfileChangedNotification` を参照している判定を、誤った `Global.Notifications` 判定に置き換えてしまう回帰を生む。

そこで、以下2案を提示した。

| 案 | 内容 | 長所 | 短所 |
|---|---|---|---|
| **案①（採用）** | `Global.ApplyProfileToSlot` 自体を `IProfileApplicationService.ApplyProfile` への薄い委譲（シム）として書き直す。`shouldDisplayNotification` の独自算出をやめる（フェーズF完了後は `displayNotification: true` を渡すだけでよい。§4.6・§4.7参照）。フェーズG（当初「フェーズE」）は「`DefaultProfileSwitcher`/`AutoProfileService` を `Global.ApplyProfileToSlot` へ差し替える」のではなく、**現状維持（既にDIサービス経由のため対応不要）** に変更する。 | Pure DI原則に合致。判定基準を1箇所（`IProfileApplicationService`）に真に一本化できる。§2.1のフォールバック/シム維持原則（静的経路を薄いラッパーとして残す）にも合致。 | `Global.ApplyProfileToSlot` の実装（フェーズAで新設したばかりのコード）に手を入れる必要がある。 |
| 案②（不採用） | `Global.ApplyProfileToSlot` 内の一行 `Global.Notifications != 0` を `Global.ProfileChangedNotification` に置き換えるのみ。 | 変更行数が最小。 | 「2つの実装が同じ関心事を別々に持つ」という構造的重複自体は解消されない。将来また同種の齟齬が再発するリスクが残る。 |

**2026-09-14、ユーザーより案①の採用が承認された。** あわせて、フェーズG（当初「フェーズE」）の対象・作業内容の見直し（上表の通り）、契機7（トレイアイコン選択）を §3.1 の正式な契機一覧へ統合すること、**「通知機能の完全分離」を新規フェーズF（§4.6）としてフェーズGの前に挿入すること**、および**「契機5（HotPlug）の共通窓口統合」を新規フェーズD（§4.4）としてフェーズCの直後・（Rename統合の）フェーズEの前に挿入すること**も承認された。具体的な実装内容は §4.4・§4.6・§4.7（改訂版）に統合済み。**次段階として §4.4（フェーズD）→§4.5（フェーズE）→§4.6（フェーズF）→§4.7（フェーズG）の順に実装を進める。**

### 7.4 フェーズC実装状況の実地確認結果（ドキュメント記載との齟齬）

`Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` は本追記時点で §4.3 フェーズC のタスク C-1・C-2 を `[ ]` 未着手と記載していたが、実コード確認の結果、**両タスクとも既に実装済み**であることを確認した。

- **C-1**（`Editor_ProfileSaved` を `SyncProfileListAndControllers();` 呼び出しに置き換え）: `MainWindow.xaml.cs` L2002-2006 に実装済み。
- **C-2**（`SelectProfCombo_SelectionChanged` 内の適用処理を `Global.ApplyProfileToSlot` に差し替え）: `MainWindow.xaml.cs` L1059 に実装済み。

一方、**C-3**（実機テストによる保存時クラッシュ解消確認）は未実施のまま残っている。したがって、フェーズCは「コード実装完了・実機確認待ち」という状態であり、Status.md 側の記載を実態に合わせて訂正する（別紙 `Phase5-Step14-ProfileSync-And-ApplyUnified-Status.md` 更新版を参照）。

なお、フェーズD（Rename契機の統合）については、`MainWindow.xaml.cs` の `RenameProfBtn_Click` を実地確認した結果、`SyncProfileListAndControllers(oldProfile, newProfile)` の呼び出しは**まだ追加されておらず**、Status.md の「未着手」という記載と実装状況は一致していることを確認した（齟齬なし）。