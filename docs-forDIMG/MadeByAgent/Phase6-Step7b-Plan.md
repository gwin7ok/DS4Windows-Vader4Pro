# Phase6-Step7b 計画書: プロファイル編集画面の「編集内容の即時反映」廃止（保存・適用時に一括反映）

作成日: 2026-09-24  
状態: 計画書作成済み、Step7b-0（事前調査）完了（2026-09-24）、Step7b-1 以降は承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
位置づけ: Step7（`App.xaml.cs` Post-Host DI化）と Step8（`ProfileEditor.xaml.cs` の段階的MVVM移設）の間に挿入する独立ステップ。Step4 の完了確定とは無関係に進められる。  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`（§2.2 の機能廃止の例外に該当。§0.2 の承認を参照）  
関連: `Phase6-Status.md` 持ち越し K4-3、`Phase6-Step8-Plan.md`（同じファイルを大きく触るため、本ステップを先に完了させる）

---

## 0. 背景と決定

### 0.1 発見の経緯
Step4 の実機確認中に、次の問題が見つかった。

- 接続中のコントローラーに適用されているプロファイルを、コントローラー横の Edit ボタンで開き、ジャイロタブの Output Mode を Mouse にした瞬間、そのコントローラーのジャイロマウスが有効になってポインタが動き出した。
- 原因は Step4 の変更ではなく、既存の仕様にある（§1）。

### 0.2 ユーザー決定（2026-09-24）
1. **編集中のリアルタイム確認は廃止する（機能削除の承認）**。実機で確認したい場合は、必ず**保存または適用を押してから**行う運用とする。`copilot-instructions.md` §2.2 の「ユーザーが明示的に承認した機能廃止」に当たる。
2. **ライトバー色プレビューとランブルテストは残す**。これらは一時的な確認機能なので、新仕様でも接続中のコントローラー（`targetDevice`）に対して動かす。
3. **独立した Step として実施する**。名称は「Step7b」。Step8 の前に完了させる。

### 0.3 新しい仕様
- プロファイル編集画面は、常に**編集用の作業スロット**（`Global.TEST_PROFILE_INDEX`＝8）に対して読み書きする。編集内容は、保存・適用を押すまで、接続中のどのコントローラーにも影響しない。
- 接続中のコントローラーは、**別の引数 `targetDevice`**（Edit ボタンを押したコントローラーの番号。プロファイル一覧から開いた場合は −1）として編集画面に渡す。ランブルテスト、ライトバーのプレビュー、入力読み取り表示、スティック・ジャイロ・ステアリングホイールの校正など、実際のコントローラーを使う機能だけがこれを使う。
- **適用**: プロファイルを保存し、`ApplyProfileToSlot(targetDevice, …)` で対象コントローラーへ**設定全体を一括で適用**する（既存の処理。適用時に `CheckProfileOptions` 等が走り、タッチパッドのリセットや `PostSetup` も行われる）。
- **保存**: プロファイルを保存し、既存の `SyncProfileListAndControllers` が、そのプロファイルを使っている**全スロット**へ再適用する（既存の処理）。
- **キャンセル**: 作業スロットを破棄するだけ。コントローラーのスロットは一切変更しない（現行の「元のプロファイルを再読み込みして取り消す」処理は不要になる）。

---

## 1. 現状の調査結果

### 1.1 即時反映の原因
- `MainWindow.xaml.cs:1534` は、コントローラー横の Edit ボタンで `ShowProfileEditor(idx, entity)`（`idx`＝コントローラーのスロット番号 0〜7）を呼ぶ。
- `ProfileEditor.Reload(device, entity)`（`ProfileEditor.xaml.cs:1091`）は、そのスロットへプロファイルを読み込み（`profileRepository.LoadProfile(device, profile.Name)`）、以降の編集はすべてそのスロットの `BackingStore` に直接書き込まれる。
- `ProfileSettingsViewModel` の各プロパティのセッターは `profileSettings.XXX[device] = value` の形（例: `GyroOutputMode`、同 857 行）。この配列は、入力ループ（`Mapping`、`Mouse` 等）が毎レポート読む配列と同一である。
- 一方、プロファイル一覧から開く場合（`MainWindow.xaml.cs:1845`、`1889`、`2079`）は、すでに `TEST_PROFILE_INDEX` を使うので即時反映しない。**新仕様は、この既存の経路にコントローラー側の機能を追加する形で実現できる**。

### 1.2 即時反映に関わる箇所（すべて対象）
- **設定値の書き込み（`device` を編集スロットとして使うもの）**: `ProfileSettingsViewModel` のほぼ全プロパティ、`MappingListViewModel`、`BindingWindow`、`SpecialActionsListViewModel`（`Global.ProfileActions[deviceNum]`）、`AxialStickUserControl.UseDevice(Global.LSModInfo[device])`、`OutConTypeCombo_SelectionChanged`（`Global.OutContType[deviceNum]`）、`PresetMenuHelper(device, …)`、`PresetOptionWindow.SetupData(deviceNum)`。編集スロットを 8 にすれば、すべて自動的に作業スロットへ向く。
- **編集操作の即時フック（`device < CURRENT_DS4_CONTROLLER_LIMIT` の判定つき。編集スロットが 8 になれば自動的に無効になる）**:
  - `ProfileSettingsViewModel.cs`: `touchPad[device]?.Reset()`（GyroOutMode／TouchpadOutput の変更時）、`PostSetup()`、`ResetTouchStickAccel`、`Mapping.deltaAccelProcessors[device].*Processor.Reset()`（3025〜3067 行付近）。
  - `SetGyroMouseDeadZone`／`SetGyroMouseToggle`／`SetGyroMouseStickToggle`／`SetGyroControlsToggle`（`controlService` を渡す。2520〜2733 行付近）。内部で `touchPad[index]` を参照するため、編集スロットが 8 のときの動作を 7b-0 で確認する。
  - `ProfileEditor.xaml.cs`: `GyroOutModeCombo_SelectionChanged`（1358 行、`touchPad[deviceNum]?.ResetToggleGyroModes()`）、`ResetTrackAccel`（1661 行付近）。
- **保存前の取り消し**: `CancelBtn_Click`（1256 行）が、`HaltReportingRunAction` の中でスロットへプロファイルを再読み込みして即時反映を取り消している。
- **コントローラーを使う機能（`targetDevice` へ付け替えが必要）**:
  - `ProfileSettingsViewModel.FuncDevNum`（コンストラクタで `device < LIMIT ? device : 0`）を使うもの: ランブルテスト（`RumbleTestBtn_Click`、1512 行）、ステアリングホイール校正（1706 行）、ジャイロ校正（`GyroCalibration_Click`、2339 行）、キャンセル・閉じる時のランブル停止（1258、1444 行）。
  - `device < LIMIT` の判定を使うもの: ライトバーの強制色プレビュー（`StartForcedColor`／`UpdateForcedColor`／`EndForcedColor`、VM 3126〜3156 行）。編集スロットが 8 になるとプレビューが動かなくなるため、判定を `targetDevice` に付け替える。
  - 入力読み取り表示: `ProfileEditor.Reload` の `conReadingsUserCon.UseDevice(...)`（1133〜1144 行）、`UseControllerReadoutCk_Click` の `Device < LIMIT` の判定（2056 行）。`UseDevice(int index, int profileDevIdx)` は、入力を読むコントローラー（`index`）と、設定を読むスロット（`profileDevIdx`）を別に持てる。新仕様では `UseDevice(targetDevice, TEST_PROFILE_INDEX)` にする。
  - スティック再校正: `CalibrateStick_OnClick`（2411 行）が `deviceNum == 8` を「Edit ボタン経由でないので不可」の判定に使っている。`StickCalibrationWindow(stick, device, vm)` は、`device` のコントローラーの現在の入力を読み、`vm` 経由でドリフト補正値を**作業スロット**へ書く。新仕様では `targetDevice < 0` を不可の判定にし、ウィンドウには `targetDevice` を渡す。

### 1.3 既存の適用処理の確認
- `Global.ApplyProfileToSlot` → `ApplyProfile` → `LoadProfile(device, launchProgram, control)`（`ScpUtil.cs:3127〜3203`）。`ScpUtil.cs:10894` の `control.CheckProfileOptions(device, tempDev, true)` により、タッチパッドのリセット・`PostSetup`・`ResetToggleGyroModes`、トリガー効果、Bluetooth ポーリング間隔などが、適用時にまとめて行われる。
- 保存後は `MainWindow.Editor_ProfileSaved` → `SyncProfileListAndControllers` が、そのプロファイルを使っている全スロットに `ApplyProfileToSlot` を実行する。
- したがって、**「保存・適用時に設定全体を適用する」処理は、すでに存在する**。新規に作るのは、編集の書き込み先を作業スロットへ切り替える部分と、コントローラー機能の付け替えである。

---

## 2. 設計

### 2.1 方針
- `ProfileEditor` は、編集スロット（`deviceNum`。常に `TEST_PROFILE_INDEX`）と、コントローラー（`targetDevice`。−1 ならなし）を別々に持つ。
- `ProfileSettingsViewModel` は、コンストラクタに `targetDevice`（省略時 −1）を追加する。
  - `Device`（編集スロット）は 8 のまま。
  - `FuncDevNum` は `targetDevice >= 0 ? targetDevice : 0` にする（一覧から開いた場合の現行動作＝コントローラー 0 を使うランブルテストを維持する）。
  - ライトバーのプレビュー 3 メソッドの判定は `device < LIMIT` から `targetDevice >= 0` に変える。
- 既存の一覧経由の動作は、そのまま維持する（`targetDevice = -1`）。

### 2.2 API の変更
- `MainWindow.ShowProfileEditor(int device, ProfileEntity entity = null)` を、`ShowProfileEditor(int targetDevice, ProfileEntity entity)`（編集スロットは内部で常に `TEST_PROFILE_INDEX`）に変更する。呼び出し元 4 箇所（1534、1544、1845、1889、2079 行付近）を更新する。
- `new ProfileEditor(device)`、`ProfileEditor.Reload(int device, ProfileEntity)` に `targetDevice` を追加する。
- 適用（`ExecuteSaveOrApply(isApply: true)`）の `Global.ApplyProfileToSlot(deviceNum, …)` は `targetDevice >= 0` のときのみ `ApplyProfileToSlot(targetDevice, …)` を実行する（一覧から開いた場合は保存のみ。現行も `slotIndex >= LIMIT` で `false` を返すだけで、実質保存のみ）。

### 2.3 挙動の変更点（ユーザー向け）
- 編集中の設定変更は、保存・適用を押すまで、コントローラーに反映されない。実機で確認するには、適用または保存を押す。
- 編集画面で Output Mode を Mouse にしても、ポインタは動かない。適用した時点で有効になる。
- 入力読み取り表示（Controller readings タブ）は、編集中の設定（作業スロット）で計算した結果を、接続中のコントローラーの入力に対して表示する。従来どおり、画面上での確認は可能。

---

## 3. マイクロステップ

各ステップの後に、ユーザー側で `dotnet build` と `dotnet test` を実行する。コミットもユーザー側で行う。

```text
【Phase6-Step7b マイクロステップ構成】
├─ Step7b-0: 事前調査の確定（実装なし）
├─ Step7b-1: 編集スロットの固定と targetDevice の導入（MainWindow・ProfileEditor・ProfileSettingsViewModel）
├─ Step7b-2: コントローラー機能の付け替え（ランブル・ライトバー・入力読み取り・校正）
├─ Step7b-3: 保存・適用・キャンセルの整理
└─ Step7b-4: テスト・文書・実機確認・完了報告
```

### Step7b-0: 事前調査の確定（実装なし）【完了（2026-09-24）】
調査結果（詳細は各項目の後ろの「結果」）:
- **結果1（`BindingWindow` 等）**: `MappingListViewModel`、`SpecialActionsListViewModel`、`PresetOptionWindow`、`SpecialActionEditor` は、コントローラーへ直接アクセスしない。`RecordBox` は `controlService.recordingMacro`（デバイス番号に依存しない）のみ。**`BindingWindow` だけが例外**で、`TestRumbleBtn_Click`（`BindingWindow.xaml.cs:856〜881`、`DS4Controllers[deviceNum]` へ `setRumble`）と、`BindingWindowViewModel` のライトバー強制色プレビュー（`StartForcedColor`／`EndForcedColor`／`UpdateForcedColor`、同 168〜198 行）が `deviceNum < CURRENT_DS4_CONTROLLER_LIMIT` の判定を使っている。編集スロットが 8 になると、これらのボタンが動かなくなるため、`targetDevice` を渡す必要がある。なお、プロファイル一覧経由（現行でも編集スロット 8）では、これらは既に動いていない（動作の後退ではなく、Edit ボタン経由の機能維持のための対応）。`BindingWindow` の生成箇所は `ProfileEditor.xaml.cs` の 7 箇所（1286、2069、2133、2260、2330、2371、2404 行）と `SpecialActionEditor.xaml.cs:524`（`specialActVM.DeviceNum`）。
- **結果2（`SetGyro*` 4メソッド）**: `BackingStore.SetGyroMouseDZ`／`SetGyroControlsToggle`／`SetGyroMouseToggle`／`SetGyroMouseStickToggle`（`ScpUtil.cs:5040〜5066`）は、`index < CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null` の判定を持つため、編集スロット 8 でも安全（コントローラーへ影響しない）。
- **結果3（`MainWindow.ProfileEditor_Closed`）**: レイアウト（サイズ・タブ）の復元のみで、編集スロットに依存する後処理はない。
- **結果4（`TEST_PROFILE_INDEX` の実行時の使用）**: `ControlService.cs:2537`（`ind >= TEST_PROFILE_INDEX` なら処理をスキップ）と `ScpUtil.cs:4863`（`lightbarSettingInfo[8]` の初期値）のみ。入力ループ等での使用はない。8 のハードコードは `ProfileEditor.xaml.cs:2413`（`deviceNum == 8`。`targetDevice < 0` の判定へ置換予定）のみ。
- **結果5（新規プロファイル）**: `Reload(device, null)` → `PresetOptionWindow.SetupData(TEST)`／`LoadBlankDevProfile(TEST, false, controlService, false)`。プロファイル一覧の New ボタン（`NewProfListBtn_Click`）が既にこの経路（編集スロット 8）を使っており、動作実績がある。
- **結果6（プロファイル名）**: 既存プロファイルの編集では `profileNameTxt.IsEnabled = false`（`ProfileEditor.xaml.cs:1110`）。名前は変わらない。
- **追加の発見（設計への影響）**: `ProfileSettingsViewModel` は `IViewModelFactory.CreateProfileSettingsViewModel(int device)`（`DI/IViewModelFactory.cs:11`、実装 `ViewModelFactory.cs:25`、テスト `PatternCViewModelTests.cs:86`）経由で生成される。`targetDevice` を渡すため、この契約に引数を追加する（`targetDevice` は省略可能な引数で、既存の呼び出しは変更不要にできる）。

確認した項目（元の調査項目）:
1. `BindingWindow`、`SpecialActionEditor`、`PresetOptionWindow`、`MappingListViewModel` が、編集スロットのほかにコントローラー（`DS4Controllers`／`getDS4State` 等）へ直接アクセスしていないこと（アクセスがあれば `targetDevice` へ付け替える対象に追加する）。
2. `SetGyroMouseDeadZone`／`SetGyroMouseToggle`／`SetGyroMouseStickToggle`／`SetGyroControlsToggle`（`BackingStore.SetGyroMouseDZ` 等）が、編集スロット 8 で呼ばれても安全であること（`touchPad[8]` などの範囲外・null 参照が起きないこと）。一覧経由の編集で既に呼ばれているため、通常は安全。
3. `MainWindow.ProfileEditor_Closed` に、編集スロットに依存した後処理がないこと。
4. `TEST_PROFILE_INDEX`（8）を、編集画面以外（ControlService の入力ループ、タッチパッド配列など）が実行時に使っていないこと。
5. 新規プロファイル作成（`NewProfBtn_Click`、Edit ではなく New ボタン）でも、`Global.LoadBlankDevProfile(TEST, …)`、`PresetOptionWindow.SetupData(TEST)` で問題がないこと。
6. 保存時にプロファイル名が変わらないこと（既存プロファイルでは名前入力が無効）。

### Step7b-1: 編集スロットの固定と targetDevice の導入
- `MainWindow.ShowProfileEditor` の呼び出し 5 箇所を新しいシグネチャへ。コントローラー横の Edit／New ボタンは `targetDevice = idx`、プロファイル一覧経由は `−1`。
- `ProfileEditor` のコンストラクタ・`Reload` に `targetDevice` を追加し、`deviceNum` は常に `TEST_PROFILE_INDEX` を使う。`ProfileSettingsViewModel` のコンストラクタに `targetDevice`（省略可能、既定 −1）を追加し、`FuncDevNum` を §2.1 のとおりにする。`IViewModelFactory.CreateProfileSettingsViewModel(int device, int targetDevice = -1)` と実装 `ViewModelFactory` も同様に拡張する（契約の追加はモデル図 03 に反映が必要か確認する）。
- この時点で、Output Mode を Mouse にしてもポインタが動かなくなる（主目的）。

### Step7b-2: コントローラー機能の付け替え
- ランブルテスト、ステアリングホイール校正、ジャイロ校正は `FuncDevNum`（＝`targetDevice`）のままで動く。`targetDevice < 0` のときは、ランブルテスト等を現行どおりコントローラー 0 に対して動かす。
- ライトバー強制色プレビュー（VM の 3 メソッド）の判定を `targetDevice >= 0` に変更。
- 入力読み取り表示は `conReadingsUserCon.UseDevice(targetDevice >= 0 ? targetDevice : 0, TEST_PROFILE_INDEX)`。`UseControllerReadoutCk_Click` の判定は、`targetDevice >= 0` に変更する。
- スティック再校正は、`deviceNum == 8` の判定を `targetDevice < 0` に変更し、`StickCalibrationWindow` には `targetDevice` を渡す。
- `BindingWindow`（`TestRumbleBtn_Click`）と `BindingWindowViewModel`（ライトバー強制色プレビュー）に、省略可能な引数 `targetDevice`（既定 −1）を追加する。`deviceNum < LIMIT` の判定は `targetDevice >= 0` の判定へ置換し、ランブルテストは `DS4Controllers[targetDevice]` を使う。`ProfileEditor.xaml.cs` の 7 箇所と、`SpecialActionEditor.xaml.cs:524` から `targetDevice` を渡す（`SpecialActionEditor` にも `targetDevice` を持たせる）。`Global.InverseRumbleMotors[deviceNum]` は編集中の設定（作業スロット）を読むままにする。

### Step7b-3: 保存・適用・キャンセルの整理
- **適用**: 保存後、`targetDevice >= 0` のとき `ApplyProfileToSlot(targetDevice, …)`。
- **保存**: 既存の `ProfileSaved` → `SyncProfileListAndControllers`（全スロットへ再適用）をそのまま使う。
- **キャンセル**: コントローラーのスロットへ触れる再読み込み（`LoadProfile(deviceNum, …)` の `HaltReportingRunAction` 部分）を削除する。`Global.outDevTypeTemp[deviceNum] = X360` のリセットと、ランブル停止、`Closed` 通知は維持する。
- 閉じる時（保存後の `Close`）のランブル停止は、`FuncDevNum`／`targetDevice` に対して維持する。

### Step7b-4: テスト・文書・実機確認・完了報告
- **テスト**:
  - `ProfileEditorTargetDeviceGuardTests`（ソース走査ガード）: `MainWindow.cs` の `ShowProfileEditor` 呼び出しが編集スロットへ `TEST_PROFILE_INDEX` を使うこと、`ProfileEditor.xaml.cs` の `CancelBtn_Click` がコントローラーのスロットへ `LoadProfile` しないこと。
  - `ProfileSettingsViewModel` の `FuncDevNum`（`targetDevice` の反映と、−1 のときの 0 へのフォールバック）の単体テスト（コントローラー関連の依存が生成できる範囲で）。
  - 既存テストが全件成功を維持すること。
- **文書**: `Phase6-Status.md`、`Phase6-Plan.md`、`Phase6-Step8-Plan.md`（`ProfileEditor.xaml.cs` の参照台帳が本ステップ後の内容に更新される旨）を更新する。`copilot-instructions.md` の変更は不要。
- **完了報告書**: `Phase6-Step7b-Completion-Report.md`。

---

## 4. 実機確認項目（Vader 4 Pro／DS4／DualSense）
1. **主目的**: 接続中のコントローラーに適用されているプロファイルを Edit ボタンで開き、ジャイロタブの Output Mode を Mouse にしても、ポインタが動かない。適用を押すと動き出す。
2. **設定の反映**: 編集して**適用**を押すと、そのコントローラーに反映される。**保存**を押すと、そのプロファイルを使っている全コントローラーに反映される。
3. **キャンセル**: 編集して、保存も適用もせずにキャンセルすると、コントローラーの動作が変わらない。プロファイルファイルも変わらない。
4. **一時確認機能**: ランブルテスト、ライトバー色のプレビュー（色選択ダイアログ中に実機の色が変わり、閉じると元に戻る）、Controller readings タブ（編集中の設定で計算された値が表示される）。
5. **校正**: スティックの再校正（Edit ボタン経由のみ可。一覧経由では従来どおりメッセージが出る）、ジャイロ校正、ステアリングホイール校正。
6. **一覧経由**: プロファイル一覧から開いて編集・保存する従来の動作に変化がない。
7. **新規プロファイル**: コントローラー横の New ボタンでの作成、プリセット選択、保存。
- 環境がなく実施できない項目は、`Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録する。

---

## 5. リスクと対策
- **編集スロット 8 の共有**: 一覧経由の編集で既に使われている（動作実績あり）。同時に開ける編集画面は 1 つ（`editor == null` の判定）。
- **`Global.ProfilePath[8]` の扱い**: `Reload` の `device == TEST_PROFILE_INDEX` の分岐が、そのまま新仕様の全経路で使われる。
- **適用時の副作用**: 従来の即時フック（タッチパッドの `Reset`／`PostSetup`、Delta 加速のリセット等）は、適用時に `LoadProfile` → `CheckProfileOptions` の中で行われることを 7b-0 で確認する。抜けがあれば、適用後に同等の処理を呼ぶ。
- **一覧経由での挙動変化なし**: `targetDevice = −1` の場合、現行動作（`FuncDevNum = 0` へのフォールバック、`UseDevice(0, TEST)`）を維持する。
- **Step8 との衝突**: 本ステップ完了後に Step8 の台帳を更新する（`ProfileEditor.xaml.cs` の行番号が変わるため）。

## 6. ロールバック方針
- ステップ単位で `git revert` する。設定値の書き込み先の切り替え（7b-1）は、`ShowProfileEditor` の呼び出しを元に戻すだけで旧動作に戻る。

## 7. 見積り
- 7b-0: 完了（調査済み。結果は §3 Step7b-0）
- 7b-1: 約 2.5 時間（`IViewModelFactory` の拡張を含む）
- 7b-2: 約 4 時間（校正・入力読み取り・`BindingWindow` の付け替えと確認を含む）
- 7b-3: 約 1.5 時間
- 7b-4: 約 4 時間（テスト、文書、実機確認）
- **合計: 約 1.5〜2 日**（7b-0 の調査で不確実性が解消。`BindingWindow` への `targetDevice` 追加分を反映。残る不確実性は、`StickCalibrationWindow`／ステアリングホイール校正の動作確認）

## 8. 完了判定チェックリスト
- [x] Step7b-0 の調査項目 1〜6 が確認され、本書へ追記されている（2026-09-24）
- [ ] プロファイル編集画面の編集スロットが、常に `TEST_PROFILE_INDEX` である
- [ ] 編集中の変更が、保存・適用まで接続中のコントローラーに反映されない
- [ ] 適用で対象コントローラーに、保存で該当する全スロットに、設定全体が反映される
- [ ] キャンセルでコントローラーのスロットが変化しない
- [ ] ランブルテスト、ライトバー色プレビュー（`BindingWindow` 内のものを含む）、入力読み取り表示、各種校正が `targetDevice` で動く
- [ ] 一覧経由の編集の動作が従来と変わらない
- [ ] `dotnet build`、`dotnet test` 全件成功
- [ ] 実機確認（§4）が完了、または Step11 の先送り台帳に登録済み
- [ ] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step8-Plan.md` を更新、完了報告書を作成
