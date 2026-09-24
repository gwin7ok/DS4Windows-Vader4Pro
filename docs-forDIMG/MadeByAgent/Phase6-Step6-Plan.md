# Phase6-Step6 計画書: ProfileEditor のマッピング一覧の機種表示追従と、IOutputSlotService の未使用 API 整理

作成日: 2026-09-11  
改訂日: 2026-09-24（Step5 完了後に現行コードと突き合わせ、台帳を再作成。旧改訂: 2026-09-18）  
状態: **Step6-0〜6-3 実装完了（2026-09-24）。Step6-1・6-2 はビルド・テストビルド・テスト実行成功。Step6-3 の追加テストと実機確認（§5）は確認待ち。決定1は案R（未接続 API として温存し、Step10 で接続）。詳細は `Phase6-Step6-Completion-Report.md`**  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.5, §5.5  
参照エビデンス:  
  - `Phase6-Step5-Plan.md`（出力デバイス三態 SSOT 台帳）、`Phase6-Step5-Completion-Report.md`  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（Issue 7 タスク5・6）  

---

## 0. 旧計画書と現行コードの乖離（2026-09-24 に確認）

### 0.1 マッピング一覧の機種表示（旧 C6-01）
- **旧計画の想定**: 「エディタを開いたまま別プロファイルを `Reload()` で読み込むと、マッピング一覧のボタン名（A/B/X/Y ⇄ Cross/Circle）が追従しない」。
- **実際の構造**:
  - `ProfileEditor.Reload` の呼び出し元は `MainWindow.ShowProfileEditor`（`MainWindow.xaml.cs:1922`）の 1 箇所だけで、`editor == null` のときだけ呼ばれる。**開いたままのエディタで別プロファイルを読み込む操作は存在しない**。
  - 旧計画のコード例（`Reload(int device, bool reloadProfile)`、`profileSettingsVM.LoadProfile`）は現行コードと一致しない。現行は `Reload(int device, ProfileEntity profile = null)` で、`profileRepository.LoadProfile(device, profile.Name)` を呼ぶ（`ProfileEditor.xaml.cs:1091〜1210`）。
- **実在する問題（本書で再定義）**:
  - `mappingListVM` は、エディタのコンストラクタ（`ProfileEditor.xaml.cs:299`）で `new MappingListViewModel(deviceNum, profileSettingsVM.ContType)` として作られる。この時点では、まだ `Reload` でプロファイルを読み込んでいないため、**その編集スロットに直前まで入っていたプロファイルの出力種別**でボタン名が決まる。
  - その後の追従は、`outConTypeCombo`（出力種別のコンボボックス）の `SelectionChanged` イベント（`OutConTypeCombo_SelectionChanged`、1783〜1806 行）に頼っている。`Reload` で `DataContext` を付け直したとき、コンボボックスの選択番号が**変化した場合だけ**イベントが発生し、`mappingListVM.UpdateMappingDevType` が呼ばれる。
  - このため、選択番号が変化しない組み合わせ（例: 直前の値と `FallbackValue=0` の関係による）では、マッピング一覧のボタン名が、読み込んだプロファイルの出力種別と食い違う可能性がある。プリセット適用（`PresetBtn_Click` → `StopEditorBindings`／`RefreshEditorBindings`、2300〜2316 行）も同じ仕組みに依存している。
  - **是正**: `Reload` と `RefreshEditorBindings` の中で、プロファイル読み込み（または `UpdateLateProperties`）の直後に、`mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` を明示的に呼ぶ。イベントの発生有無に依存せず、常に正しい表示にする（呼び出しが重複しても、値を設定し直すだけで副作用はない）。

### 0.2 `PluginSlot`／`UnplugSlot`（旧 C6-02〜C6-04）
- **旧計画の記述**: 「呼出元が 0 件のダミーメソッド（常に `false` を返却）」。
- **実際**: 実装は、`ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を呼んで ViGEm の仮想デバイスを抜き差しする実体を持つ（`OutputSlotService.cs:107〜163`）。アプリ本体からの呼出元は 0 件で、テスト（範囲外の安全性、`UnplugSlot` の永続設定非汚染）からのみ呼ばれる。
- **同じ機能の実際の経路**: 出力スロット管理画面（`CurrentOutDeviceViewModel.cs:151、165`）と UDP コマンド（`MainWindow.xaml.cs:1403〜1407`）は、`ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を**直接**呼んでいる。
- Step5 で `SetOutputDeviceType` 呼び出しは `RaiseOutputSlotChanged` に置き換え済み。

### 0.3 ホットスワップ経路（変更なし・温存）
- プロファイル適用時の仮想コントローラーの抜き差しは、`ScpUtil.cs` の `PostLoadSnippet`（呼び出し 11 箇所、8307 行ほか）と `ControlService.UnplugOutDev`／`PluginOutDev` で動いている。本 Step では触れない（全体計画書 §4.5）。

### 0.4 Step7b との関係
- Step7b で、`ProfileEditor` の編集スロットは常に `TEST_PROFILE_INDEX` になり、`Reload` のシグネチャが変わる。本 Step の C6-01 は `Reload` 内への 1 行追加なので、Step7b とは競合しない（Step7b で `Reload` を書き換える際に、この行を維持する）。

---

## 1. 目的
1. プロファイル編集画面を開いたとき、およびプリセットを適用したとき、マッピング一覧のボタン名表記が、読み込んだプロファイルの出力種別（Xbox 360／DS4）と常に一致するようにする。
2. `IOutputSlotService` の未使用 API（`PluginSlot`／`UnplugSlot`）を、決定1に従って整理する。

---

## 2. 修正対象台帳（現行コードから再作成）
- **C6-01**: `DS4Forms/ProfileEditor.xaml.cs` の `Reload`（1150〜1151 行付近、`mappingListVM.UpdateMappings()`／`profileSettingsVM.UpdateLateProperties()` の直後）に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加する。
- **C6-01b**（新規）: 同 `RefreshEditorBindings`（1232〜1233 行付近）にも、同じ 1 行を追加する（プリセット適用時の追従）。
- **C6-02**: `DI/IOutputSlotService.cs` の `PluginSlot`（60 行）→ 決定1に従う。
- **C6-03**: 同 `UnplugSlot`（66 行）→ 決定1に従う。
- **C6-04**: `DS4Control/Services/OutputSlotService.cs` の `PluginSlot`／`UnplugSlot` 実装（107〜163 行）と、`RaiseOutputSlotChanged` の扱い → 決定1に従う。
- **影響を受けるテスト**: `OutputSlotServiceTests.OutOfBounds_ShouldBeHandledSafely`（`PluginSlot`／`UnplugSlot` の範囲外呼び出し）、`OutputSlotServiceSsotTests.UnplugSlot_DoesNotWriteNoneToPersistentSetting`。

---

## 3. 決定事項

### 決定1: `PluginSlot`／`UnplugSlot` の扱い
- **案P（旧計画どおり）**: `[Obsolete]` 属性と技術的負債コメントを付けて温存する。将来の仮想バス刷新（`IVirtualControllerBackend`）への引き継ぎ事項とする。
  - 注意: テストが呼んでいるため、テスト側でコンパイラ警告（CS0618）が出る。テストには `#pragma warning disable CS0618` を付ける。ビルド設定は警告をエラーにしないため、ビルドは通る。
- **案Q**: Step5 の決定1（案B）と同じ考え方で、`PluginSlot`／`UnplugSlot` を削除する。同じ機能は `ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` として実際に使われており、将来の刷新でもそちらが起点になるため、呼出元 0 件の並行実装を残す理由は薄い。`OutputSlotChanged` イベントの発行元は `SetOutputDevice` だけになる。テスト 2 件を修正する。
- **案R（採用）**: 削除も `[Obsolete]` 化もせず、「Phase5-Step12 で意図された UI 接続が未実施の API」として TODO を書き換えて温存し、実際の接続を Step10 で行う。
- **決定の経緯（2026-09-24）**: 当初は案Q（削除）で決定したが、ユーザーの指摘を受けて経緯を再調査した結果、案R に変更した。
  - `PluginSlot`／`UnplugSlot` は、Phase5-Step12（2026-09-05、コミット `c4fbc260`）で DI 化のために追加された。`Phase5-Step12-Plan.md` §1.5・タスク Step12-5 は、「UI（`OutputSlotManagerControl`／`CurrentOutDeviceViewModel`）と `ControlService` からの `outputslotMan` 直接参照を、`IOutputSlotService` 経由に置き換える」ことを意図していた。
  - しかし `Phase5-Step12-Completion-Report.md` では、タスク Step12-5 の中身が「ViGEm ドライバ保護」に差し替わって「完了」とされ、UI の切り替えは行われなかった。このため、サービス側の API だけが存在し、呼び出し側は `ControlService` を直接呼び続けている。
  - Step5 の孤立配列（常に誤った値を返す）とは異なり、`PluginSlot`／`UnplugSlot` は正しい中継処理を持つ「未接続の DI 契約」である。
  - Pure DI の観点では、第4層の UI は、具体クラス（`ControlService`、`OutputSlotManager`）ではなく抽象（`IOutputSlotService`）経由で仮想スロットを操作するのが妥当である（全体4層モデルの依存方向、テスト容易性、仮想バス刷新時の差し替え点の集約）。
- **決定（ユーザー、2026-09-24）: 案R**。

### 3.1 現在の `PluginSlot`／`UnplugSlot` と実際の呼び出し側との差分（Step10 での接続時に是正する）
1. **スレッド**: 画面側（`CurrentOutDeviceViewModel.cs:149〜153、163〜167`）は `controlService.EventDispatcher.BeginInvoke` で `ControlService` のスレッドへ処理を回す。`PluginSlot`／`UnplugSlot` は呼び出し元のスレッドで直接 `AttachUnboundOutDev`／`DetachUnboundOutDev` を呼ぶ。ViGEm ドライバ保護（ガードレール §5.5）のため、サービス側で同じディスパッチを行う必要がある。
2. **事前条件と種別**: 画面側は「`CurrentAttachedStatus` が未接続（または接続中）かつ `CurrentInputBound` が未割り当て」を確認してから呼び、種別には `OutSlotDevice.CurrentType` を使う。`PluginSlot` は事前条件を確認せず、種別を引数で受け取る。
3. **依存の取り方**: `OutputSlotService` は `ControlService` を `control ?? Program.rootHub`（Service Locator 的なフォールバック）で取得している。`ControlService` は既に `Func<IOutputSlotService>` で `OutputSlotService` へ依存しているため（Step2 決定D1）、`ControlService` を直接注入すると循環する。`Func<ControlService>` による遅延解決、または仮想スロット操作用の小さな出力ポートの導入が必要。
4. **イベント**: `PluginSlot` は Step5 以降、`RaiseOutputSlotChanged` で `OutputSlotChanged` を発行する。画面側は `OutputSlotManager.SlotAssigned`／`SlotUnassigned` を購読しており、`OutputSlotChanged` の購読者はテストのみ。接続時に、どちらのイベントを画面更新の正とするかを決める。

---

## 4. マイクロステップ

```text
【Phase6-Step6 マイクロステップ構成】
├─ Step6-0: 台帳の再作成・計画書改訂（本書）  ← 完了
├─ Step6-1: ProfileEditor の Reload／RefreshEditorBindings に UpdateMappingDevType を追加（C6-01、C6-01b）
├─ Step6-2: PluginSlot／UnplugSlot の TODO 書き換え（C6-02〜C6-04、決定1＝案R。コードの実行内容は変えない）
└─ Step6-3: テスト・文書・実機確認・完了報告
```

各ステップの後に、ユーザー側で `dotnet build` と `dotnet test` を実行する。コミットもユーザー側で行う。

### Step6-1
- `Reload` と `RefreshEditorBindings` に 1 行ずつ追加する。既存のコメント・処理順は変えない。

### Step6-2（案R）
- `IOutputSlotService.cs`: `PluginSlot`／`UnplugSlot` の既存 TODO（「呼出元0件。削除／実配線の判断は Phase6-Step6 で行う」）を、次の内容に書き換える: Phase5-Step12 で意図された UI 接続が未実施であること、正式な入口として維持すること（新規コードは `ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を直接呼ばず、本 API を使う方針）、接続は Phase6-Step10 で行うこと、§3.1 の差分 1〜4 の要約。`[Obsolete]` は付けない。
- `OutputSlotService.cs`: 実装側の TODO も同様に書き換える。`control ?? Program.rootHub` の行には、差分3（循環依存の解消方法）の TODO を付ける。
- 実行コード・テストは変更しない。

### Step6-3
- **テスト**: `ProfileEditorMappingDevTypeGuardTests`（ソース走査ガード）: `Reload` と `RefreshEditorBindings` が `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` を呼ぶこと。
- **文書**: `Phase6-Status.md`、`Phase6-Plan.md` を更新し、完了報告書 `Phase6-Step6-Completion-Report.md` を作成する。`Phase6-Step7b-Plan.md` に「`Reload` の `UpdateMappingDevType` 呼び出しを維持する」旨を追記する。`Phase6-Step10-Plan.md` への接続作業の追加は、Step6-0 の決定時に実施済み（同書 §8）。

---

## 5. 実機確認項目
1. 出力種別が DS4 のプロファイルを使っているコントローラーの Edit ボタンで編集画面を開き、マッピング一覧のボタン名が DS4 表記（Cross／Circle 等）であること。続けて、出力種別が Xbox 360 のプロファイルを開き、Xbox 表記（A／B 等）であること。逆の順でも確認する。
2. プロファイル一覧から開いた場合も同様であること。
3. 編集画面で出力種別のコンボボックスを切り替えると、従来どおりボタン名が即座に切り替わること。
4. プリセットを適用した後も、ボタン名がプロファイルの出力種別と一致していること。
5. （念のため）コントローラーのプロファイルを出力種別の異なるものへ切り替えたとき、仮想コントローラーの種別が切り替わること（ホットスワップ経路は未変更）。

## 6. ロールバック方針
- ステップ単位で `git revert` する。Step6-1 は 2 行の追加なので、単独で戻せる。

## 7. 完了判定チェックリスト
- [x] `Reload` と `RefreshEditorBindings` で `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)` が呼ばれる（Step6-1、ビルド・テスト成功）
- [ ] 編集画面を開いたとき・プリセット適用後に、マッピング一覧のボタン名がプロファイルの出力種別と一致する（実機確認）
- [x] `PluginSlot`／`UnplugSlot` の TODO が案R の内容に書き換えられ、接続作業が `Phase6-Step10-Plan.md` に登録されている（Step6-2、ビルド・テスト成功）
- [x] `ScpUtil.cs` の `PostLoadSnippet` によるホットスワップ機構に手を加えていない
- [ ] `dotnet build`、`dotnet test` 全件成功（Step6-3 の追加テスト `ProfileEditorMappingDevTypeGuardTests` は確認待ち）
- [ ] 実機確認（§5）が完了、または Step11 の先送り台帳に登録済み
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step7b-Plan.md` を更新、完了報告書を作成
