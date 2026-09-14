# Phase 5 Step 14: プロファイル適用一本化および保存・Rename時同期 実装計画書

- **Document**: `docs-forDIMG/MadeByAgent/Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md`
- **Target Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Base Branch**: `gwin7ok/DS4Windows-Vader4Pro` (`For-DI-migration-work`)
- **Author**: Agent (Claude-based assistant)
- **Status**: 計画策定完了 (Ready for Implementation)
- **Created Date**: 2026-09-14
- **Last Updated**: 2026-09-14

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