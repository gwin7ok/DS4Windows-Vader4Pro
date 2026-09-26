# Phase6-Step7b 計画書: プロファイル編集画面の「編集内容の即時反映」廃止（保存・適用時に一括反映）

作成日: 2026-09-24  
改訂日: 2026-09-26（着手前再確認。現行コード［HEAD `360cb75b`］との突き合わせ、台帳の再作成、決定事項の追加、マイクロステップと実機確認項目の改訂。同日、決定1〜7 を確定）  
状態: 計画書作成済み、Step7b-0（事前調査）完了（2026-09-24）、着手前再確認完了（2026-09-26）、決定1〜7 確定（2026-09-26、すべて推奨案）、決定8（Save／Apply の仕様）確定（2026-09-26）、Step7b-1・7b-2 完了（2026-09-26）、**Step7b-3 実装済み（2026-09-26、決定8 の修正を含む。ユーザーのビルド・テスト・実機確認待ち）**  
対象ブランチ: `For-DI-migration-work`  
位置づけ: Step7（`App.xaml.cs` Post-Host DI化）と Step8（`ProfileEditor.xaml.cs` の段階的MVVM移設）の間に挿入する独立ステップ。  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`（§2.2 の機能廃止の例外に該当。§0.2 の承認を参照。§2.4 は §1A.4 で適用）  
関連: `Phase6-Status.md` 持ち越し K4-3（発端）・K7-1（起動時の DI ホストの前提）、`Phase6-Step8-Plan.md`（同じファイルを大きく触るため、本ステップを先に完了させる）

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
- **保存（Save）と適用（Apply）**（2026-09-26 決定8 で改訂。旧記載は「適用は `ApplyProfileToSlot(targetDevice, …)` で対象コントローラーへ適用」だった）:
  1. プロファイル編集画面の設定値の内容を、すべてプロファイル設定ファイルに保存する（作業スロットから保存）。
  2. その後、編集したプロファイルを使っている（`Global.ProfilePath[i]` がそのプロファイル名の）コントローラーのスロットに、プロファイル設定ファイルの内容で再適用する（既存の `ProfileSaved` → `MainWindow.SyncProfileListAndControllers`。適用時に `PreLoadReset`・`CheckProfileOptions` 等が走り、タッチパッドのリセットや `PostSetup` も行われる。§1A.3 で確認済み）。そのプロファイルを使っていないコントローラー（New Profile の作成元を含む）には適用しない。
  3. Save はプロファイル編集画面を閉じ、Apply は閉じない。違いはこれだけ。
- **キャンセル**: 作業スロットを破棄するだけ。コントローラーのスロットは一切変更しない（現行の「元のプロファイルを再読み込みして取り消す」処理は不要になる）。

---

## 1. 現状の調査結果（2026-09-24、Step7b-0 時点の記録）

> **注意（2026-09-26）**: 本節の行番号・一部の記述は旧調査時点のもの。現行コードとの乖離は §1A.1、正とする台帳は §1A.2 を参照すること。本節は経緯の記録として残す。

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

## 1A. 着手前再確認（2026-09-26、HEAD `360cb75b`）

### 1A.1 旧調査との乖離
1. **行番号のずれ**: Step5〜7 の変更で、`MainWindow.xaml.cs` は約 +2 行、`ProfileEditor.xaml.cs` は +2〜+12 行ずれている（例: `CancelBtn_Click` 1256→1261、`CalibrateStick_OnClick` 2411→2416、`deviceNum == 8` 2413→2418、`BindingWindow` の生成 1286→1291）。VM・`BindingWindow`・`BindingWindowViewModel`・`ScpUtil.cs` の該当行は変わっていない。正は §1A.2。
2. **旧調査で漏れていた「実機に触れる機能」が2系統ある**（旧 Step7b-0 結果1の「`SpecialActionEditor` はコントローラーへ直接アクセスしない」「`RecordBox` は `recordingMacro` のみ」は不正確だった）。
   - **マクロ記録画面（`RecordBox`／`RecordBoxViewModel`）**: `BindingWindow` の「Record A Macro」と、`SpecialActionEditor` のマクロ・キー記録から開く。受け取った `deviceNum` に対して、(a) マクロのライトバー色ステップを選ぶときの強制色プレビュー（`RecordBox.xaml.cs:429〜435` → VM 364〜393 行、`deviceNum < LIMIT` 判定）、(b) 記録中にタッチパッドを一時的に Passthru にし、閉じると戻す処理（VM 167〜168 行、`RevertControlsSettings` 437 行）を行う。Edit ボタン経由では現在これらがそのコントローラーに効いており、編集スロットを 8 にすると効かなくなる。
   - **スペシャルアクション編集画面（`SpecialActionEditor`）のバッテリー確認アクション**: 「空」「満充電」の色選択中に、`CheckBatteryViewModel.StartForcedColor(color, specialActVM.DeviceNum)`（`SpecialActionEditor.xaml.cs:431〜459`、VM 側 `device < LIMIT` 判定）で実機のライトバーをプレビューしている。
   - なお、マクロ記録でコントローラーの入力を読むのは常にコントローラー0（`RecordBoxViewModel.ProcessDS4Tick` 396 行の固定値）で、Edit ボタンのコントローラーとは無関係。これは既存の動作で、本 Step では変えない（§1A.5 観察1）。
3. **`UseControllerReadoutCk_Click` の機能の誤認**: 旧 §1.2 は「入力読み取り表示」の一部としていたが、実際はマッピング一覧の「for readout」チェック（`profileSettings.DS4Mapping`、アプリ全体の設定）で、コントローラーのボタンを押すとそのボタンの割り当てを選択・ハイライトする機能（`InputDS4` 1966 行、`GetActiveInputControl(FuncDevNum)`）。Controller Readings タブ（`conReadingsUserCon`）とは別機能。付け替えの方針（`targetDevice` を使う）は変わらない。
4. **`MainWindow.ShowProfileEditor` の `EmitMissingActionLogsForDevice(device, true, …)`（1935 行）**: 旧調査に記載なし。読み込んだプロファイルのスペシャルアクション名を調べてログを出す処理なので、`targetDevice` ではなく**編集スロット（8）**を渡す必要がある（一覧経由では既に 8 を渡しており動作実績あり）。
5. **`ProfileEditor.DeviceNum`（公開プロパティ）の呼出元が 0 件**。§1A.4 で §2.4 の手順を適用。
6. **ランブルテストの「モーター左右反転」の読み先**: `RumbleTestBtn_Click` はローカル変数 `deviceNum = FuncDevNum` で `Global.InverseRumbleMotors[deviceNum]` を読んでいる（1517〜1597 行）。付け替え後は実機スロットの適用中の値を読むことになり、編集中のチェックがテストに反映されなくなる。旧計画は `BindingWindow` 側（編集スロットを読む）しか扱っていなかった。決定4。
7. **レガシーのフォールバック生成**: `ProfileEditor` のコンストラクタは、ファクトリが解決できない場合に `new ProfileSettingsViewModel(device)`（294 行）を使う。ここにも `targetDevice` を渡す必要がある。
8. **旧 §2.2 の件数表記**: 「呼び出し元 4 箇所」とあるが、列挙されている行は 5 つで、実際も 5 箇所（§1A.2 A1〜A5）。
9. **旧 §5「適用時の副作用」の確認が済んだ**: §1A.3。即時フックの処理は、すべて適用時の処理に含まれている。
10. **モデル図 03 の `IViewModelFactory`**: 理想形として `CreateProfileSettingsVM(string profileName)` と書かれており、現行コードの `CreateProfileSettingsViewModel(int device)` とは既に異なる。契約に `targetDevice` を足すときの図の扱いは決定3。
11. **`Phase6-Plan.md` に Step7b の記載がない**（§0.2 のスコープ表、§1 の一覧、§2 の各ステップ）。Step7b-4 で追記する。

### 1A.2 再確認した台帳（HEAD `360cb75b`）
各項目の矢印の右は、本 Step での扱い。「編集スロット」は常に 8 になることで自動的に作業スロットへ向く箇所（コード上の変更は不要か、引数の出どころの変更のみ）。

#### A. `DS4Windows/DS4Forms/MainWindow.xaml.cs`
- A1 `ProfEditSBtn_Click`（1526 行、呼び出し 1536 行）: `ShowProfileEditor(idx, entity)` → `targetDevice = idx`
- A2 `NewProfBtn_Click`（1541 行、呼び出し 1546 行）: `ShowProfileEditor(idx, null)` → `targetDevice = idx`
- A3 `EditProfBtn_Click`（1842 行、呼び出し 1847 行）: `ShowProfileEditor(Global.TEST_PROFILE_INDEX, entity)` → `targetDevice = −1`
- A4 `NewProfListBtn_Click`（1889 行、呼び出し 1891 行）: `ShowProfileEditor(Global.TEST_PROFILE_INDEX, null)` → `targetDevice = −1`
- A5 `ProfilesListBox_MouseDoubleClick`（2076 行、呼び出し 2081 行）: `ShowProfileEditor(Global.TEST_PROFILE_INDEX, entity)` → `targetDevice = −1`
- A6 `ShowProfileEditor`（1894 行）: `new ProfileEditor(device)`（1920）、`editor.Reload(device, entity)`（1924）→ `targetDevice` を渡す。`EmitMissingActionLogsForDevice(device, …)`（1935）→ 編集スロット（8）
- A7 `Editor_ProfileSaved`（2019 行）→ `SyncProfileListAndControllers`（1955 行）: 変更なし
- A8 `ProfileEditor_Closed`（1851 行）: 変更なし（レイアウト・タブの復元のみ）

#### B. `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`
- B1 フィールド `deviceNum`（151 行）→ 常に `TEST_PROFILE_INDEX`。`targetDevice` フィールドを追加
- B2 公開プロパティ `DeviceNum`（193〜196 行）→ 呼出元 0 件。決定5
- B3 コンストラクタ（267 行）: ログ（269）、`deviceNum = device`（286）、ファクトリ生成（290）、レガシー生成（294）→ `targetDevice` を追加。`MappingListViewModel(deviceNum, …)`（299）、`SpecialActionsListViewModel(device)`（300）、`TouchButtonUserControl(device)`（302）→ 編集スロット
- B4 `Reload`（1091 行）: `deviceNum = device`（1098）、`TEST_PROFILE_INDEX` の分岐（1103〜1106、`ProfilePath[8]` の設定）、`LoadProfile(device, …)`（1108）、`presetWin.SetupData(deviceNum)`（1116）、`LoadBlankDevProfile(device, …)`（1120）、`ProfileActions[deviceNum]`（1125）→ 編集スロット。入力読み取り表示の分岐（1133〜1144、`useControllerUD` と `conReadingsUserCon.UseDevice`）→ `UseDevice(targetDevice >= 0 ? targetDevice : 0, TEST_PROFILE_INDEX)`。`LSModInfo`／`RSModInfo[device]`（1147〜1148、1164、1177）→ 編集スロット。**`mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)`（1154、`UpdateLateProperties()` の直後）は維持**。`inputTimer.Start()`（1209）→ 変更なし
- B5 `RefreshEditorBindings`（1225 行付近、`UpdateMappingDevType` は 1238）→ 変更なし
- B6 `CancelBtn_Click`（1261 行）: ランブル停止（1263〜1266、`FuncDevNum`）→ 維持。`Global.outDevTypeTemp[deviceNum] = X360`（1268）→ 維持（編集スロット）。`Task.Run` 内の再読み込み（1270〜1283、`HaltReportingRunAction` と `LoadProfile(deviceNum, …)`）→ 削除
- B7 `GyroOutModeCombo_SelectionChanged`（1358 行）: `deviceNum < LIMIT` のとき `touchPad[deviceNum]?.ResetToggleGyroModes()`（1363〜1366）→ 常に偽になる。決定7
- B8 `ExecuteSaveOrApply`（1378 行）: `ProfileActions[deviceNum]`（1392）、`SaveProfile(deviceNum, …)`（1400、1404）→ 編集スロットから保存（正しい）。適用の `ApplyProfileToSlot(deviceNum, …)`（1416）→ `targetDevice >= 0` のときだけ `ApplyProfileToSlot(targetDevice, …)`（2026-09-26 決定8 で変更: Save と同じく `ProfileSaved` を通知し、`ApplyProfileToSlot` は削除）
- B9 `Close`（1447 行）: ランブル停止（1449〜1452、`FuncDevNum`）→ 維持
- B10 `RumbleTestBtn_Click`（1515 行）: `FuncDevNum`（1517）→ 自動的に `targetDevice`（または 0）。`InverseRumbleMotors[deviceNum]`（1525、1569、1576、1588、1597）→ 決定4
- B11 ライトバー色の選択（`profileSettingsVM.StartForcedColor` 1489、1502、1697、1956）→ VM 側で判定（C4）
- B12 `FrictionUD_ValueChanged`（1661 行）: `deviceNum < LIMIT` のとき `touchPad[deviceNum]?.ResetTrackAccel(...)`（1666〜1669）→ 常に偽になる。決定7
- B13 `SteeringWheelEmulationCalibrateBtn_Click`（1707 行）: `DS4Controllers[FuncDevNum]`（1711）→ 自動的に `targetDevice`（一覧経由は従来どおりコントローラー0）
- B14 `OutConTypeCombo_SelectionChanged`（1788 行）: `OutContType[deviceNum]`（1794、1797）→ 編集スロット
- B15 `NewActionBtn_Click`（1812 行）／`EditActionBtn_Click`（1842 行）: `new SpecialActionEditor(deviceNum, …)`（1816、1853）→ `targetDevice` を追加（決定1）。`ProfileActions`・`CacheExtraProfileInfo(deviceNum)`（1870〜1881）、1937 行の `ProfileActions[deviceNum]` → 編集スロット
- B16 `InputDS4`（1966 行）: `tempDeviceNum = FuncDevNum`（1975）→ 自動的に `targetDevice`（または 0）
- B17 `ProfileEditor_Closed`（2038 行）: `CacheExtraProfileInfo(profileSettingsVM.Device)`（2053）→ 編集スロット
- B18 `UseControllerReadoutCk_Click`（2059 行）: `profileSettingsVM.Device < LIMIT`（2061）→ `targetDevice >= 0`（一覧経由でクリックしても開始しない現行動作を維持）
- B19 `BindingWindow` の生成 7 箇所 → `targetDevice` を追加: `HoverConBtn_Click`（1291）、`ShowControlBindingWindow`（2074）、`TiltControlsButton_Click`（2138）、`SwipeControlsButton_Click`（2265）、`TriggerFullPullBtn_Click`（2335）、`GyroSwipeControlsBtn_Click`（2376）、`StickOuterBindButton_Click`（2409）
- B20 `PresetBtn_Click`（2305 行）: `SetupData(deviceNum)`（2310）→ 編集スロット
- B21 `GyroCalibration_Click`（2342 行）: `FuncDevNum`（2344）→ 自動的に `targetDevice`（または 0）
- B22 `CalibrateStick_OnClick`（2416 行）: `deviceNum == 8`（2418）→ `targetDevice < 0`。`new StickCalibrationWindow(stick, deviceNum, vm)`（2436）→ `targetDevice` を渡す
- B23 `Global.CacheProfileCustomsFlags(profileSettingsVM.Device)`（`BindingWindow` を閉じた後の各所）→ 編集スロット

#### C. `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`
- C1 `funcDevNum`／`FuncDevNum`（60〜61 行）、コンストラクタ（2877 行、`funcDevNum = …` 2887）→ `targetDevice` を追加し、`FuncDevNum = targetDevice >= 0 ? targetDevice : 0`。`OutDevTypeTemp[device] = X360`（2891）→ 編集スロット
- C2 `SetGyroMouseDeadZone` 等 4 件（2521、2531、2555、2734）→ `BackingStore` 側（`ScpUtil.cs:5040〜5066`）で `index < LIMIT` を判定するため、編集スロット 8 では何もしない（一覧経由と同じ）。変更なし
- C3 即時フック 6 件（3024〜3070 行）: `TouchMouseStickTrackballFrictionChanged`（3026）、`GyroOutModeIndexChanging`（3034）、`TouchpadOutputIndexChanging`（3042）、`TouchpadOutputIndexChanged`（3050）、`LSDeltaAccelEnabledChanged`（3058）、`RSDeltaAccelEnabledChanged`（3066）→ 常に偽になる。決定7
- C4 ライトバー強制色 3 メソッド（`UpdateForcedColor` 3127、`StartForcedColor` 3138、`EndForcedColor` 3149）→ 判定と添字を `targetDevice` に変更
- C5 `UpdateLateProperties`（3615 行）: `OutDevTypeTemp[device]`（3618）→ 編集スロット

#### D. DI 契約
- D1 `DS4Windows/DI/IViewModelFactory.cs:11` `CreateProfileSettingsViewModel(int device)`、実装 `DS4Windows/DS4Control/Services/ViewModelFactory.cs:25〜30`（`[DI]` Trace ログあり）→ `targetDevice` を追加（決定3）
- D2 `IViewModelFactory.cs:12` `CreateRecordBoxViewModel(int device, …)`、実装 `ViewModelFactory.cs:32〜37` → `targetDevice` を追加（決定1＝A）
- D3 テスト用のモック実装は存在しない（`IViewModelFactory` の実装は `ViewModelFactory` のみ。§6.4-4 確認済み）

#### E. 編集画面から開くサブ画面
- E1 `BindingWindow.xaml.cs`: コンストラクタ（62 行、`new BindingWindowViewModel(deviceNum, settings)` 69）、`TestRumbleBtn_Click`（856 行、`bindingVM.DeviceNum` 858、`< LIMIT` 859、`DS4Controllers[deviceNum]` 861、`InverseRumbleMotors[deviceNum]` 867）、`ExtrasColorChoosebtn_Click`（886 行、強制色 893〜899）、`RecordMacroBtn_Click`（928 行、`new RecordBox(bindingVM.DeviceNum, …)` 930）
- E2 `ViewModels/BindingWindowViewModel.cs`: コンストラクタ（54 行）、`outDevTypeTemp[deviceNum]`（57、ボタン名表記用。編集スロットのまま）、強制色（168〜198、`< LIMIT` 170、181、191）
- E3 `RecordBox.xaml.cs`: コンストラクタ（61 行、ファクトリ 67／レガシー 71）、強制色（429〜435）、`RevertControlsSettings`（110、117）
- E4 `ViewModels/RecordBoxViewModel.cs`: コンストラクタ（114 行）、タッチパッドの一時 Passthru（167〜168）、強制色（364〜393）、`ProcessDS4Tick`（396、コントローラー0固定）、`RevertControlsSettings`（437〜441）
- E5 `RecordBoxWindow.xaml.cs`: コンストラクタ（42 行、`new RecordBox(deviceNum, …)` 46）
- E6 `SpecialActionEditor.xaml.cs`: コンストラクタ（54 行、ファクトリ 90／レガシー 94、`pressKeyVM.SetDeviceNum`（100、ボタン名表記用。編集スロットのまま））、`RecordMacroBtn_Click`（411 行、`new RecordBoxWindow` 414）、`BatteryEmptyColorBtn_Click`（431）・`BatteryFullColorBtn_Click`（447）の強制色（437〜459）、`RecordBoxWindow` 生成（466、480、494）、`PressKeySelectBtn_Click`（521 行、`new BindingWindow` 524、`ReadSettings` 528 は表記用）
- E7 `ViewModels/SpecialActions/CheckBatteryViewModel.cs`: 強制色 3 メソッド（66、77、88。引数 `device` と `< LIMIT` 判定）→ 呼び出し側で `targetDevice` を渡す（ファイル変更なし）
- E8 `StickCalibrationWindow.xaml.cs`: コンストラクタ（15 行）、`getDS4State(_device)`（31）→ 呼び出し側で `targetDevice` を渡す（ファイル変更なし）
- E9 `ControllerReadingsControl.xaml.cs`: `UseDevice(int index, int profileDevIdx)`（326 行）、`deviceNum != profileDeviceNum` のとき `SetCurveAndDeadzone(profileDeviceNum, …)` で編集中の設定から再計算（394〜395）→ 変更なし

#### F. テスト
- F1 `DS4WindowsTests/ProfileEditorMappingDevTypeGuardTests.cs`: マーカー `public void Reload(` と `private void RefreshEditorBindings(`。`Reload` の引数を変えてもマーカーは一致する。維持
- F2 `DS4WindowsTests/PatternCViewModelTests.cs`: `CreateProfileSettingsViewModel(0)`（86）、`CreateRecordBoxViewModel(0, settings, true, false)`（99）→ 決定2・3 の結果に合わせる。なお 99 行のテストは、現状 `TouchOutMode[0]` を Passthru にしたまま戻していない（既存の後始末漏れ、§6.4-1）。決定1＝A で Passthru を `targetDevice >= 0` のときだけ行うようにすれば、この副作用も消える
- F3 `DS4WindowsTests/ProfileSettingsViewModelTests.cs`: `new ProfileSettingsViewModel(device, service, …)`（50、81、112）→ 引数を末尾に足すなら影響なし

#### G. 実行時に `TEST_PROFILE_INDEX` を使う箇所（旧 Step7b-0 結果4 の再確認）
- `ControlService.cs:2537`（`PreLoadReset` の `ind >= TEST_PROFILE_INDEX` なら何もしない）、`MainWindow.xaml.cs` の 3 箇所（A3〜A5）、`ProfileEditor.xaml.cs` の 4 箇所（1103、1105、1133、1142）のみ。入力ループでの使用はない。

### 1A.3 適用時の副作用の確認（旧 §5 のリスク項目）
編集スロットを 8 にすると、編集操作の即時フック（B7、B12、C3）は動かなくなる。これらの処理は、適用・保存時の `ApplyProfile` → `LoadProfile` の中で、すべて行われることを確認した。
- `touchPad.Reset()`、`deltaAccelProcessors` の LS／RS のリセット → `ControlService.PreLoadReset`（2529 行。`ScpUtil.cs:6066` から呼ばれる）
- `ResetTrackAccel`、`ResetToggleGyroModes`、`PostSetup` → `ControlService.CheckProfileOptions`（2354 行。`ScpUtil.cs:10894` から呼ばれる）
- `ResetTouchStickAccel`（タッチパッドのスティック出力のトラックボール摩擦）→ `Mouse.PostSetup`（412 行）の中の `touchStickTrackball.ResetAccel(msinfo.trackballFriction)` で同等の処理
- したがって、適用後に追加で呼ぶ処理は不要。

### 1A.4 呼出元 0 件のコード（copilot-instructions §2.4）
- **対象**: `ProfileEditor.DeviceNum`（`public int DeviceNum { get => deviceNum; }`、193〜196 行）
- **経緯**: 2019-12-24 の上流コミット `1e148ba1`（Switch active profile when new profile is created）で、Edit ボタンの New Profile から作ったプロファイルを、そのコントローラーの選択プロファイルに切り替える `MainWindow.Editor_CreatedProfile` のために追加された。2026-09-14 の `0da35884`（ProfileEditor 保存処理の統合、`ProfileSaved` イベントの追加）で、`CreatedProfile` イベントと `Editor_CreatedProfile` が削除され、以後は呼出元がない。
- **Step7b 後の状態**: `deviceNum` が常に 8 になるため、このプロパティは常に 8 を返し、意味を失う。
- **判断**: 決定5 で確認する。
- **付随の観察**: 「New Profile で作成・保存したら、そのコントローラーの選択を新しいプロファイルに切り替える」上流の機能は、2026-09-14 の保存処理の統合で外れている。§2.2 に照らして承認済みの廃止かどうか、文書に記録がない。本 Step では扱わない（§1A.5 観察2）。

### 1A.5 観察事項（本 Step では変えない）
1. **マクロ記録の入力元**: `RecordBoxViewModel.ProcessDS4Tick` は常にコントローラー0の入力を記録する。Edit ボタンでコントローラー2を開いても、記録されるのはコントローラー0の入力で、一時的に Passthru になるのはコントローラー2のタッチパッド、という既存の不整合がある。`targetDevice` を記録の入力元に使えば解消できるが、機能の変更になるため、必要なら別途提案・承認を経て扱う。
2. **新規プロファイル作成時のコントローラー切り替え**: §1A.4 のとおり 2026-09-14 から動いていない。現状、New Profile → 保存 では、コントローラーは元のプロファイル名のまま残る（Apply の場合は、2026-09-16 以降、そのコントローラーが新しいプロファイルに切り替わっていたが、決定8 で Save と同じ動作に揃えた）。ただし即時反映のため、そのコントローラーには新しいプロファイルの設定がメモリ上で効いていた。Step7b 後は、保存しても元のプロファイルの設定のまま（名前と中身が一致する）で、新しいプロファイルを使うには、適用を押すか、一覧で選び直す。上流の切り替え機能を戻すかは、別件としてユーザー判断。
3. **UI の Edit ボタン**: コントローラー一覧の Edit はスプリットボタン（`ProfEditSBtn`）で、押すと Edit、右の▼を開くと New Profile（`newProfBtn`）が出る。
4. **一時プロファイルが有効なスロットへの再適用（2026-09-26 追加、決定8 の検討時に確認）**: 保存・適用後の再適用は `Global.ProfilePath[i]` とプロファイル名の一致で判定する。一時プロファイルとして使われているプロファイルを保存しても、そのスロットには再適用されない。逆に `ProfilePath` がそのプロファイル名で、一時プロファイルが有効なスロットでは、保存で通常のプロファイルに戻る。Step7b 前からの動作で、本 Step では変えない。

---

## 2. 設計（2026-09-26 改訂。決定1〜7 の確定内容［§2A.0］に基づく）

### 2.1 方針
- `ProfileEditor` は、編集スロット（`deviceNum`。常に `TEST_PROFILE_INDEX`）と、コントローラー（`targetDevice`。−1 ならなし）を別々に持つ。
- `ProfileSettingsViewModel` は、コンストラクタに `targetDevice`（省略時 −1）を追加する。
  - `Device`（編集スロット）は 8 のまま。
  - `FuncDevNum` は `targetDevice >= 0 ? targetDevice : 0` にする（一覧から開いた場合の現行動作＝コントローラー 0 を使うランブルテスト等を維持する）。
  - 読み取り専用プロパティ `TargetDevice` を追加する（`ProfileEditor` の判定に使う）。
  - ライトバーのプレビュー 3 メソッドは、`targetDevice >= 0` のときだけ `DS4LightBar.*[targetDevice]` を操作する。
- 編集画面から開くサブ画面（`BindingWindow`、`RecordBox`、`RecordBoxWindow`、`SpecialActionEditor`）にも `targetDevice` を渡し、実機に触れる処理だけがそれを使う（決定1）。設定の読み書きとボタン名の表記は、これまでどおり編集スロットを使う。
- 既存の一覧経由の動作は、そのまま維持する（`targetDevice = −1`）。

### 2.2 API の変更（決定2＝P1・決定3＝M1）
- `MainWindow.ShowProfileEditor(int device, ProfileEntity entity = null)` → `ShowProfileEditor(int targetDevice, ProfileEntity entity)`。編集スロットは `ProfileEditor` の内部で常に `TEST_PROFILE_INDEX`。呼び出し元 5 箇所（A1〜A5）を更新する。
- `new ProfileEditor(int device)` → `new ProfileEditor(int targetDevice)`、`Reload(int device, ProfileEntity)` → `Reload(int targetDevice, ProfileEntity)`（どちらも必須引数）。`MainWindow` から編集スロットを渡す経路をなくし、誤ってコントローラーのスロットを編集スロットにできないようにする。
- `BindingWindow`、`RecordBox`、`RecordBoxWindow`、`SpecialActionEditor` のコンストラクタ: `int targetDevice` を必須引数として追加する（呼び出し元の渡し忘れをコンパイラで検出するため）。
- `ProfileSettingsViewModel`、`BindingWindowViewModel`、`RecordBoxViewModel` のコンストラクタと、`IViewModelFactory.CreateProfileSettingsViewModel`／`CreateRecordBoxViewModel`: 末尾に省略可能な `int targetDevice = -1` を追加する（既存の省略可能な DI 引数の後ろにしか置けないため）。
- **Step6 との関係**: `Reload` と `RefreshEditorBindings` の `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType)`（`UpdateLateProperties()` の直後）を、書き換え後も維持する。ソース走査ガード `ProfileEditorMappingDevTypeGuardTests` が、この呼び出しと順序を検査する。
- 適用（`ExecuteSaveOrApply(isApply: true)`）: 決定8 により、Save と同じく保存後に `ProfileSaved` を通知する（再適用は `MainWindow.SyncProfileListAndControllers` が、そのプロファイルを使っているスロットにだけ行う）。`targetDevice` は適用先の決定に使わない。（旧記載: `targetDevice >= 0` のときだけ `ApplyProfileToSlot(targetDevice, …)`。）
- `Reload` は `Reload(ProfileEntity profile = null)` とし、`targetDevice` はコンストラクタでだけ受け取る（Step7b-3 の実装時に変更。§3 Step7b-3 の実装結果）。

### 2.3 挙動の変更点（ユーザー向け）
- 編集中の設定変更は、保存・適用を押すまで、コントローラーに反映されない。実機で確認するには、適用または保存を押す。
- 編集画面で Output Mode を Mouse にしても、ポインタは動かない。適用した時点で有効になる。
- Controller Readings タブは、編集中の設定（作業スロット）で計算した結果を、接続中のコントローラーの入力に対して表示する（スティック・トリガー・ジャイロのカーブとデッドゾーン。一覧経由と同じ方式）。画面上での確認は従来どおり可能。
- キャンセルは、コントローラーの動作に何もしない（編集中の変更がもともと届いていないため）。
- New Profile → 保存・適用のどちらでも、コントローラーは元のプロファイルのまま（§1A.5 観察2、決定8）。従来の New Profile → 適用は、そのコントローラーを新しいプロファイルに切り替えていたが、これは Save／Apply の仕様（決定8）に反するため改めた。
- Apply でも、そのプロファイルを使っている全コントローラーへ再適用され、プロファイル一覧も更新される（決定8。従来の Apply は Edit ボタンのコントローラーだけに適用し、一覧経由では保存のみで、一覧も更新しなかった）。

---

## 2A. 決定事項（2026-09-26 提示・同日確定）

### 2A.0 確定内容（ユーザー決定 2026-09-26: 決定1〜7 はすべて推奨案）
- **決定1＝案A**: 実機に触れる処理は、サブ画面まですべて `targetDevice` へ付け替える（`BindingWindow` のランブルテスト・ライトバー、マクロ記録のライトバー色プレビューと記録中のタッチパッド Passthru、`SpecialActionEditor` のバッテリー確認の色プレビュー、そこから開く `BindingWindow`・マクロ記録）。機能削減なし。
- **決定2＝案P1**: 画面（`ProfileEditor`、`BindingWindow`、`RecordBox`、`RecordBoxWindow`、`SpecialActionEditor`）のコンストラクタは `targetDevice` を必須引数にする。ViewModel（`ProfileSettingsViewModel`、`BindingWindowViewModel`、`RecordBoxViewModel`）とファクトリは、末尾に省略可能な `int targetDevice = -1` を追加し、渡し忘れはソース走査ガードで検出する。
- **決定3＝案M1**: `IViewModelFactory.CreateProfileSettingsViewModel`／`CreateRecordBoxViewModel` に `targetDevice` を追加し、モデル図 03（理想形のシグネチャと注釈）・04（§1-3 の説明）に「編集対象と、プレビュー・校正に使う実機（`targetDevice`）を分けて渡す」ことを追記する（Step7b-4 で実施）。
- **決定4＝案I1**: ランブルテストの左右反転（`InverseRumbleMotors`）は、編集中の値（作業スロット）を読む。`BindingWindow` 側と揃える。
- **決定5＝案D1**: `ProfileEditor.DeviceNum`（呼出元 0 件）は削除する（§2.4 の「使うべきでないもの」）。新規作成時のコントローラー切り替え機能の復活は本 Step では扱わない（§1A.5 観察2）。
- **決定6＝案L1**: 既存の 2 行のログ（`[ProfileEditor] Opened profile editor …`、`[DI] ViewModelFactory: Created ProfileSettingsViewModel …`）の末尾に `targetDevice` を付け足し、`Reload`・適用・キャンセルに `LogDebug` を 1 行ずつ追加する（文言は決定6 の本文のとおり）。
- **決定7＝案H1**: 到達しなくなる即時フック 8 件（VM の C3 の 6 件と `SetupEvents` での登録、`ProfileEditor` の B7・B12 と `ProfileEditor.xaml` のイベント属性）を、Step7b-3 で削除する（承認済みの機能廃止［§0.2-1］の本体）。
- **決定8（2026-09-26 追加・確定）**: Save と Apply は、どちらも (1) 編集画面の設定値をすべてプロファイル設定ファイルに保存し、(2) 編集したプロファイルを使っているコントローラーにだけ、ファイルの内容で再適用する。違いは (3) Save は画面を閉じ、Apply は閉じないことだけ。Apply も `ProfileSaved` を通知して `MainWindow.SyncProfileListAndControllers` に再適用を任せ、編集画面から `ApplyProfileToSlot` を直接呼ばない。詳細は下記「決定8」。

以下は、提示時の前提・選択肢・推奨理由の記録。

### 決定1: 実機に触れる処理をどこまで `targetDevice` へ付け替えるか
- **前提**: 編集スロットを 8 にすると、`device < LIMIT` で守られた「実機に触れる処理」は、すべて動かなくなる。ユーザー決定（§0.2-2）で、ライトバー色プレビューとランブルテストは残すことになっている。旧計画は、編集画面本体と `BindingWindow` までを付け替え対象としていたが、再確認で、マクロ記録画面とバッテリー確認アクションの色プレビューも実機に触れていることが分かった（§1A.1-2）。
- **案A（推奨）: サブ画面まですべて付け替える**
  - 対象: `BindingWindow`（ランブルテスト・ライトバー）、マクロ記録（ライトバー色プレビュー・記録中のタッチパッド Passthru）、`SpecialActionEditor`（バッテリー確認の色プレビュー、そこから開く `BindingWindow`・マクロ記録）。
  - 利点: Edit ボタン経由で今できる一時確認が、すべて残る（§2.2 の機能削減を起こさない）。
  - 欠点: 変更ファイルが増える（`RecordBox`、`RecordBoxWindow`、`RecordBoxViewModel`、`SpecialActionEditor`、`CreateRecordBoxViewModel` の契約）。
- **案B: 旧計画どおり `BindingWindow` まで**
  - 利点: 変更が小さい。
  - 欠点: マクロ記録のライトバー色プレビューと記録中の Passthru、バッテリー確認の色プレビューが、Edit ボタン経由でも実機に効かなくなる（一覧経由の現状と同じになる）。§0.2 の承認範囲外の機能削減になるため、採用するなら追加の承認が必要。
- **推奨理由**: §0.2 の決定（一時確認機能は残す）と §2.2（エージェント判断での機能削減の禁止）に沿うのは案A。

### 決定2: `targetDevice` の渡し方
- **前提**: 画面と ViewModel のコンストラクタに `targetDevice` を足す。省略可能にすると、渡し忘れてもコンパイルが通り、実機に効かないだけになる（Step7-4 の「テストは通るが実機で効かない」と同種の危険）。
- **案P1（推奨）: 画面（View）側は必須、ViewModel・ファクトリ側は省略可能（既定 −1）**
  - `ProfileEditor`、`BindingWindow`、`RecordBox`、`RecordBoxWindow`、`SpecialActionEditor` は必須引数。呼び出し元はすべて本 Step の対象ファイル内にあり、コンパイラが全箇所を列挙してくれる。
  - ViewModel とファクトリは、既存の省略可能な DI 引数の後ろに置くため、省略可能にする。渡し忘れはソース走査ガードで検出する（§3 の各テスト）。
- **案P2: すべて省略可能（既定 −1）**
  - 利点: 差分が最小。欠点: 渡し忘れがコンパイルで分からない。
- **案P3: 「編集対象」を表す新しい型（例: 編集スロットと `targetDevice` の組）を作って渡す**
  - 利点: 意味が明確。欠点: 新規ファイルが増え、Step8 の MVVM 移設で作り直す可能性が高い。

### 決定3: `IViewModelFactory` の契約とモデル図の扱い
- **前提**: `CreateProfileSettingsViewModel` に `targetDevice` を追加する（決定1＝A なら `CreateRecordBoxViewModel` にも）。モデル図 03 の `IViewModelFactory` は、理想形として `CreateProfileSettingsVM(string profileName)`（編集対象をプロファイル名で指定）と書かれており、現行コードの `int device` とは既に異なる。モデル図 04 §1-3 は「パラメータ（プロファイル名）を伴う複合 ViewModel」と説明している。
- **案M1（推奨）: モデル図 03・04 に `targetDevice` を追記する**
  - 03: 理想形のシグネチャを `CreateProfileSettingsVM(string profileName, int targetDevice)` にし、注釈で「`targetDevice` はプレビュー・校正に使う実機（−1 はなし）。編集内容は作業領域に保持し、保存・適用時にだけ実機へ反映する（Phase6-Step7b）」と書く。
  - 04 §1-3: 「編集対象（プロファイル）と、プレビュー・校正に使う実機（`targetDevice`）を分けて渡す」と追記する。
  - 理由: 「編集は作業領域、実機は別引数」は Step7b で確定する設計上の決まりで、理想形でも必要になる（ライトバー・ランブル・校正は実機がないとできない）。図に書いておかないと、Step8 以降で再び編集スロットと実機を混同する恐れがある。
- **案M2: モデル図は変えない**
  - 理由付け: 現行コードの引数（`int device`）自体が図と異なる過渡期の形なので、過渡期の引数の追加は図に載せない。

### 決定4: ランブルテストの「モーター左右反転（Inverse Rumble Motors）」をどちらの値で判定するか
- **前提**: 編集画面の Rumble タブのテストボタン（`RumbleTestBtn_Click`）は、左右のモーターを入れ替える設定 `InverseRumbleMotors` を見て、どちらのモーターを回すか決める。現在は `FuncDevNum`（Edit ボタン経由では編集中のスロットと同じ）の値を読んでいる。付け替え後にそのままにすると、実機スロットの「適用中の値」を読むことになる。`BindingWindow` のランブルテストは、編集スロットの値を読む（旧計画で決定済み）。
- **案I1（推奨）: 編集中の値（作業スロット）を読む**
  - チェックを変えてすぐテストに反映される、今の Edit ボタン経由の体験を保てる。`BindingWindow` 側とも揃う。
  - 一覧経由では、これまで「コントローラー0の適用中の値」を読んでいたのが「編集中の値」に変わる（小さな改善方向の変更）。
- **案I2: 実機の適用中の値（`targetDevice`）を読む**
  - 保存・適用するまで、チェックの変更がテストに反映されない。

### 決定5: `ProfileEditor.DeviceNum`（呼出元 0 件）の扱い
- **前提**: §1A.4。2019 年に、新規作成したプロファイルへコントローラーを切り替える機能のために作られ、2026-09-14 にその利用箇所が削除された。Step7b 後は常に 8 を返す。
- **案D1（推奨）: 削除する**
  - 理由: 正規の利用箇所がなく、Step7b 後は誤解を招く値（常に 8）を返すだけになる（§2.4 の「使うべきでないもの」）。将来、切り替え機能を戻す場合は、その時点で `TargetDevice` を公開すればよい。
- **案D2: `TargetDevice`（`targetDevice` を返す）に置き換えて温存し、§3.3 原則4 の TODO を付ける**
  - §1A.5 観察2 の切り替え機能を近いうちに戻す予定がある場合に向く。

### 決定6: 実機確認のためのログを追加するか
- **前提**: Step7 の教訓（K7-1）として、実機での挙動をログで判定できるようにしたい。既存のログだけでは、編集画面がどのスロットで開いたか、キャンセル時に何もしなかったかが分からない。`copilot-instructions` §2.3 により、既存ログの文言とレベルは維持する。
- **案L1（推奨）: `LogDebug` を追加し、既存の 2 行に `targetDevice` を付け足す**
  - 既存: `[ProfileEditor] Opened profile editor for device={device}` の末尾に `, targetDevice={targetDevice}` を付け足す（`device` は 8 になる）。
  - 既存: `[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device {device}`（Trace）の末尾に `, targetDevice {targetDevice}` を付け足す。
  - 追加: `Reload` の冒頭で `[ProfileEditor] Reload: editSlot=8, targetDevice=N, profile=名前（新規は (new profile)）`。
  - 追加: 適用時に `[ProfileEditor] Apply: profile '名前' applied to controller slot N`、一覧経由（`targetDevice < 0`）なら `[ProfileEditor] Apply: no target controller (opened from profile list); saved only`。
  - 追加: キャンセル時に `[ProfileEditor] Cancel: edit slot 8 discarded; controller slots were not modified (targetDevice=N)`。
- **案L2: 追加しない**
  - 既存の `ApplyProfile CALLED: device=…`（DEBUG）の有無だけで判定する。編集画面の開き方はログから分からない。

### 決定7: 動かなくなる即時フック（8 箇所）の扱い
- **前提**: 編集スロットが常に 8 になると、`device < LIMIT`（または `deviceNum < LIMIT`）で守られた即時フックは、本番では二度と実行されない。対象は、VM の 6 件（C3）と `ProfileEditor` の 2 件（B7 `GyroOutModeCombo_SelectionChanged`、B12 `FrictionUD_ValueChanged`。どちらも XAML でイベントに結び付いている）。同じ処理は適用時に行われる（§1A.3）。
- **案H1（推奨）: 削除する**
  - VM の 6 件はハンドラーと `SetupEvents` での登録を削除する。`ProfileEditor` の 2 件は、メソッドと XAML のイベント属性（`SelectionChanged`／`ValueChanged`）を削除する。
  - 理由: 承認済みの機能廃止（§0.2-1「編集中のリアルタイム確認の廃止」）の本体であり、残しても到達しないコードになる。Step8 の MVVM 移設の対象も減る。
  - 注意: XAML のイベント属性を消すため、`ProfileEditor.xaml` も変更対象になる。
- **案H2: 残し、§3.3 原則4 の TODO（Step7b 以降は到達しない旨と理由）を付ける**
  - 利点: 差分が最小。欠点: 到達しないコードが Step8 以降も残る。

### 決定8: Save／Apply の仕様と、Apply の再適用先（2026-09-26 追加・確定）
- **発端**: Step7b-3 の実機確認項目（「New Profile から Apply すると、そのコントローラーが新しいプロファイルに切り替わること」）について、ユーザーから Save／Apply の仕様が示された（§0.3 の改訂後の記載）。
- **仕様と現行コードの照合結果**:
  - Save は仕様どおり（保存 → `ProfileSaved` → `SyncProfileListAndControllers` が、プロファイル名が一致するスロットにだけ再適用 → 画面を閉じる）。
  - Apply は仕様から外れていた。`ProfileSaved` を通知せず、`Global.ApplyProfileToSlot(targetDevice, …)`（Step7b 前は `deviceNum`）を直接呼んでいたため、次の 4 点が仕様と異なる: (1) New Profile から Apply すると、そのコントローラーの `ProfilePath` が新しいプロファイルに書き換わり切り替わる、(2) 同じプロファイルを使う他のコントローラーへ再適用されない、(3) 一覧経由では再適用が一切ない、(4) プロファイル一覧が更新されない。
- **経緯**: 2026-09-14（`0da35884`）の時点では Save・Apply ともに `ProfileSaved` を通知しており、仕様どおりだった。2026-09-16（`36d124a6`、Phase5-Step14）で Apply だけが `ApplyProfileToSlot(deviceNum, …)` に分岐した。分岐の理由は計画書・完了報告書に記録がない。Step7b の着手前確認では、この分岐を既存仕様とみなして計画に取り込んでいた（確認不足）。
- **決定（ユーザー承認 2026-09-26）**: Apply も Save と同じく `ProfileSaved` を通知し、違いは画面を閉じるかどうかだけにする。`targetDevice` はプレビュー・校正・ランブルテスト専用とし、適用先の決定には使わない。2026-09-14 時点の動作に戻す形で、仕様どおりの変更（§2.2 の機能削減には当たらない）。
- **変更しないこと（観察事項）**: 再適用の判定は `Global.ProfilePath[i]` とプロファイル名の一致で行う。自動プロファイル等で一時プロファイルが有効なスロットについては、(a) 一時プロファイルとして使われているプロファイルを保存しても再適用されない、(b) `ProfilePath` がそのプロファイル名のスロットでは、保存で一時プロファイルから通常のプロファイルに戻る。Step7b 前からの動作で、本 Step では変えない（§1A.5 観察4）。

---

## 3. マイクロステップ（2026-09-26 改訂）

各ステップの後に、ユーザー側で `dotnet build` と `dotnet test` を実行する。コミットもユーザー側で行う。エージェントはファイルの編集だけを行う。

改訂の考え方: 旧計画は「7b-1 で編集スロットを固定 → 7b-2 で付け替え → 7b-3 で保存・適用」の順だったが、この順では 7b-1 の直後に、適用（`ApplyProfileToSlot(8, …)` が何もしない）やプレビューが一時的に動かない状態のビルドができてしまう。改訂後は、**挙動を変えない下準備（7b-1・7b-2）を先に行い、編集スロットの固定と保存・適用・キャンセルの整理を 7b-3 で同時に行う**。どの時点のビルドでも、機能が欠けた状態にならない。

```text
【Phase6-Step7b マイクロステップ構成（2026-09-26 改訂）】
├─ Step7b-0: 事前調査（2026-09-24 完了）・着手前再確認（2026-09-26 完了）
├─ Step7b-1: ProfileSettingsViewModel と IViewModelFactory の拡張（挙動の変化なし）
├─ Step7b-2: サブ画面への targetDevice の受け渡し（挙動の変化なし）
├─ Step7b-3: 編集スロットの固定と保存・適用・キャンセルの整理（主目的。挙動の変化あり）
└─ Step7b-4: 文書・実機確認・完了報告
```

### Step7b-0: 事前調査の確定（実装なし）【完了（2026-09-24）、着手前再確認完了（2026-09-26）】
2026-09-24 の調査結果（行番号は旧値。§1A で再確認・訂正済み）:
- **結果1（`BindingWindow` 等）**: `MappingListViewModel`、`SpecialActionsListViewModel`、`PresetOptionWindow`、`SpecialActionEditor` は、コントローラーへ直接アクセスしない。`RecordBox` は `controlService.recordingMacro`（デバイス番号に依存しない）のみ。**`BindingWindow` だけが例外**で、`TestRumbleBtn_Click`（`BindingWindow.xaml.cs:856〜881`、`DS4Controllers[deviceNum]` へ `setRumble`）と、`BindingWindowViewModel` のライトバー強制色プレビュー（`StartForcedColor`／`EndForcedColor`／`UpdateForcedColor`、同 168〜198 行）が `deviceNum < CURRENT_DS4_CONTROLLER_LIMIT` の判定を使っている。編集スロットが 8 になると、これらのボタンが動かなくなるため、`targetDevice` を渡す必要がある。なお、プロファイル一覧経由（現行でも編集スロット 8）では、これらは既に動いていない（動作の後退ではなく、Edit ボタン経由の機能維持のための対応）。`BindingWindow` の生成箇所は `ProfileEditor.xaml.cs` の 7 箇所（1286、2069、2133、2260、2330、2371、2404 行）と `SpecialActionEditor.xaml.cs:524`（`specialActVM.DeviceNum`）。
  - **2026-09-26 訂正**: `SpecialActionEditor`（バッテリー確認の色プレビュー）と `RecordBox`（ライトバー色プレビュー、記録中のタッチパッド Passthru）も実機に触れている（§1A.1-2）。`BindingWindow` の生成箇所の行番号は §1A.2 B19。
- **結果2（`SetGyro*` 4メソッド）**: `BackingStore.SetGyroMouseDZ`／`SetGyroControlsToggle`／`SetGyroMouseToggle`／`SetGyroMouseStickToggle`（`ScpUtil.cs:5040〜5066`）は、`index < CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null` の判定を持つため、編集スロット 8 でも安全（コントローラーへ影響しない）。（2026-09-26 再確認: 行番号変化なし）
- **結果3（`MainWindow.ProfileEditor_Closed`）**: レイアウト（サイズ・タブ）の復元のみで、編集スロットに依存する後処理はない。（2026-09-26 再確認: 1851 行、変化なし）
- **結果4（`TEST_PROFILE_INDEX` の実行時の使用）**: `ControlService.cs:2537`（`ind >= TEST_PROFILE_INDEX` なら処理をスキップ）と `ScpUtil.cs:4863`（`lightbarSettingInfo[8]` の初期値）のみ。入力ループ等での使用はない。8 のハードコードは `ProfileEditor.xaml.cs:2413`（`deviceNum == 8`。`targetDevice < 0` の判定へ置換予定）のみ。（2026-09-26 再確認: `deviceNum == 8` は 2418 行。§1A.2 G）
- **結果5（新規プロファイル）**: `Reload(device, null)` → `PresetOptionWindow.SetupData(TEST)`／`LoadBlankDevProfile(TEST, false, controlService, false)`。プロファイル一覧の New ボタン（`NewProfListBtn_Click`）が既にこの経路（編集スロット 8）を使っており、動作実績がある。
- **結果6（プロファイル名）**: 既存プロファイルの編集では `profileNameTxt.IsEnabled = false`（`ProfileEditor.xaml.cs:1110`）。名前は変わらない。
- **追加の発見（設計への影響）**: `ProfileSettingsViewModel` は `IViewModelFactory.CreateProfileSettingsViewModel(int device)`（`DI/IViewModelFactory.cs:11`、実装 `ViewModelFactory.cs:25`、テスト `PatternCViewModelTests.cs:86`）経由で生成される。`targetDevice` を渡すため、この契約に引数を追加する（決定2・3）。

### Step7b-1: `ProfileSettingsViewModel` と `IViewModelFactory` の拡張（挙動の変化なし）【完了（2026-09-26、ビルド・テストビルド・テスト実行成功、コミット・リモート反映済み）】
- **変更**:
  - `ProfileSettingsViewModel`: コンストラクタの末尾に `int targetDevice = -1` を追加。`TargetDevice` プロパティを追加。`FuncDevNum = targetDevice >= 0 ? targetDevice : 0`。ライトバー強制色 3 メソッド（C4）を `targetDevice` に付け替え。
  - `IViewModelFactory.CreateProfileSettingsViewModel(int device, int targetDevice = -1)` と `ViewModelFactory`（`[DI]` Trace ログに `targetDevice` を付け足す。決定6）。
  - `ProfileEditor` のコンストラクタ（ファクトリ生成とレガシー生成の 2 箇所）から、暫定値 `device < CURRENT_DS4_CONTROLLER_LIMIT ? device : -1` を渡す。この時点では編集スロットはまだ `device` のままなので、Edit ボタン経由（`device`＝コントローラー番号）でも一覧経由（`device`＝8 → −1）でも、`FuncDevNum` とライトバーの判定結果は現行と同じになる。
  - 決定7＝H1 により削除する VM の即時フック 6 件（C3）はこの時点では削除しない（Edit ボタン経由ではまだ編集スロットが実機スロットのため）。7b-3 で削除する。
- **テスト**:
  - `ProfileSettingsViewModelTargetDeviceTests`（新規）: `targetDevice` を渡したとき `FuncDevNum`・`TargetDevice` がその値になること、−1 のとき `FuncDevNum` が 0 になること。ライトバー強制色が、`targetDevice >= 0` のとき `DS4LightBar.forcedColor／forcedFlash／forcelight[targetDevice]` だけを変え、−1 のときは何も変えないこと（`DS4LightBar` の静的配列はテストの前後で保存・復元する。§6.4-1）。編集スロット（`device`）に 8 を渡しても `targetDevice` の添字が使われること。
  - `PatternCViewModelTests`（更新）: ファクトリに `targetDevice` を渡したとき、生成された VM の `TargetDevice` に届くこと（実際の DI ホストから解決したファクトリで確認する。K7-1 の教訓）。
- **実機確認**: 不要（挙動の変化なし）。念のため、Edit ボタン経由でライトバー色のプレビューとランブルテストが従来どおり動くことを見てもよい。
- **実装結果（2026-09-26）**:
  - `ProfileSettingsViewModel.cs`: 読み取り専用フィールド `targetDevice` と `TargetDevice` プロパティ、private の判定 `HasTargetDevice`（`targetDevice >= 0 && targetDevice < ControlService.CURRENT_DS4_CONTROLLER_LIMIT`。範囲外の添字で `DS4LightBar` の配列（長さ 8）に触れないよう、上限も判定する）を追加。コンストラクタの末尾に `int targetDevice = -1` を追加し、`FuncDevNum = HasTargetDevice ? targetDevice : 0` に変更。`UpdateForcedColor`／`StartForcedColor`／`EndForcedColor` の判定と添字を `targetDevice` に変更。即時フック（C3）は未変更（7b-3 で削除）。
  - `IViewModelFactory.CreateProfileSettingsViewModel(int device, int targetDevice = -1)`（XML コメントで両引数の意味を記載）、`ViewModelFactory` は `targetDevice` を VM へ渡し、`[DI]` Trace ログの末尾に `, targetDevice {targetDevice}` を付け足した（決定6）。
  - `ProfileEditor.xaml.cs` のコンストラクタ: ファクトリ生成とレガシー生成の 2 箇所へ、暫定値 `interimTargetDevice = device < CURRENT_DS4_CONTROLLER_LIMIT ? device : -1` を渡す（7b-3 で置き換える旨の TODO 付き）。
  - **挙動の同一性**: Edit ボタン経由（`device`＝コントローラー番号）は `targetDevice = device` となり、`FuncDevNum` とライトバーの対象は従来と同じ。一覧経由（`device`＝8）は `targetDevice = −1` となり、`FuncDevNum = 0`、ライトバーのプレビューなしで従来と同じ。
  - **テスト**: `ProfileSettingsViewModelTargetDeviceTests.cs`（新規、6 件。編集スロットと `targetDevice` の分離、−1 と省略時のコントローラー0へのフォールバック、範囲外の値を「実機なし」として扱うこと、強制色が `targetDevice` にだけ効くこと、−1 では何もしないこと。`DS4LightBar` の静的配列と `Global.outDevTypeTemp[8]` を保存・復元し、`Global.ProfileSettingsServiceInstance` は差し替えない）。`PatternCViewModelTests.cs`（既存テストに省略時 −1 の確認を追加し、実際の DI ホストから解決したファクトリで `targetDevice` が届くことを確認するテストを 1 件追加）。
  - モデル図・`ServiceRegistration.cs` は未変更（モデル図は 7b-4 で更新）。テスト用の `IViewModelFactory` のモックは存在しない（§1A.2 D3）。
  - **検証結果（2026-09-26）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。挙動の変化がないため実機確認は省略。

### Step7b-2: サブ画面への `targetDevice` の受け渡し（挙動の変化なし）【完了（2026-09-26、ビルド・テストビルド・テスト実行成功、コミット・リモート反映済み）】
- **前提**: 決定1＝A（サブ画面まですべて付け替え）、決定2＝P1（画面は必須引数、VM・ファクトリは省略可能）。
- **変更**:
  - `BindingWindowViewModel`: コンストラクタ末尾に `int targetDevice = -1`。ライトバー強制色 3 メソッドを `targetDevice` に付け替え。`TargetDevice` プロパティを追加。
  - `BindingWindow`: コンストラクタに必須の `int targetDevice` を追加し VM へ渡す。`TestRumbleBtn_Click` は `targetDevice >= 0` のとき `DS4Controllers[targetDevice]` を鳴らす。左右反転は `Global.InverseRumbleMotors[bindingVM.DeviceNum]`（編集スロット）を読むまま（決定4 と同じ考え方）。`RecordMacroBtn_Click` は `targetDevice` を `RecordBox` へ渡す。
  - `RecordBoxViewModel`: コンストラクタ末尾に `int targetDevice = -1`。ライトバー強制色を `targetDevice` に付け替え。タッチパッドの一時 Passthru と `RevertControlsSettings` は、`targetDevice >= 0` のときだけ `TouchOutMode[targetDevice]` に対して行う。
  - `IViewModelFactory.CreateRecordBoxViewModel(…, int targetDevice = -1)` と `ViewModelFactory`。
  - `RecordBox`、`RecordBoxWindow`: コンストラクタに必須の `int targetDevice` を追加し、下位へ渡す。
  - `SpecialActionEditor`: コンストラクタに必須の `int targetDevice` を追加。バッテリー確認の色プレビューは `checkBatteryVM.*ForcedColor(…, targetDevice)` にする（`targetDevice < 0` なら何もしない分岐を呼び出し側に置く。`CheckBatteryViewModel` は変更しない）。`RecordBoxWindow`・`BindingWindow` の生成へ `targetDevice` を渡す。
  - `ProfileEditor`: `BindingWindow` 生成 7 箇所（B19）と `SpecialActionEditor` 生成 2 箇所（B15）へ、7b-1 と同じ暫定値（`deviceNum < LIMIT ? deviceNum : -1`）を渡す。これで、Edit ボタン経由ではこれまでと同じコントローラー、一覧経由では「なし」になり、挙動は変わらない。
- **テスト**:
  - `BindingWindowViewModelTargetDeviceTests`（新規）: ライトバー強制色が `targetDevice` にだけ効くこと、−1 なら何もしないこと、編集スロット（8）を渡しても `targetDevice` が使われること。
  - `RecordBoxViewModelTargetDeviceTests`（新規）: `targetDevice >= 0` のとき `TouchOutMode[targetDevice]` が Passthru になり、`RevertControlsSettings` で元に戻ること。編集スロットの `TouchOutMode` は変えないこと。−1 のときはどのスロットも変えないこと。ライトバー強制色も同様。
  - `PatternCViewModelTests`（更新）: `CreateRecordBoxViewModel` へ `targetDevice` が届くこと。既存テストの `TouchOutMode[0]` の後始末漏れは、`targetDevice` を渡さない（−1）ことで解消される旨をコメントに残す。
- **実機確認**: 不要（挙動の変化なし）。
- **実装結果（2026-09-26）**:
  - `BindingWindowViewModel.cs`: 読み取り専用 `targetDevice`・`TargetDevice`、公開の判定 `HasTargetDevice`（`targetDevice >= 0 && < CURRENT_DS4_CONTROLLER_LIMIT`）を追加。コンストラクタ末尾に `int targetDevice = -1`。ライトバー強制色 3 メソッドを `targetDevice` に付け替え。ボタン名表記用の `outDevTypeTemp[deviceNum]` は編集スロットのまま。
  - `BindingWindow.xaml.cs`: コンストラクタを `(int deviceNum, DS4ControlSettings settings, int targetDevice, ExposeMode expose = Full)` に変更（必須引数。旧シグネチャの呼び出しは `ExposeMode`／`SpecialAction` 等が `int` に変換できずコンパイルエラーになる）。`TestRumbleBtn_Click` は `bindingVM.HasTargetDevice` のとき `DS4Controllers[bindingVM.TargetDevice]` を鳴らし、左右反転は `Global.InverseRumbleMotors[bindingVM.DeviceNum]`（編集スロット）を読む。`RecordMacroBtn_Click` は `targetDevice` を `RecordBox` へ渡す。
  - `RecordBoxViewModel.cs`: `targetDevice`・`TargetDevice`・private の `HasTargetDevice`、フラグ `touchpadModeOverridden` を追加。コンストラクタ末尾に `int targetDevice = -1`。記録中のタッチパッド Passthru は、実機があるときだけ `TouchOutMode[targetDevice]` に設定し、`RevertControlsSettings` は設定した場合だけ元に戻す（2 回目以降は何もしない。従来は 2 回呼ぶと `TouchOutMode` が `None` になる潜在的な問題があった）。ライトバー強制色 3 メソッドを `targetDevice` に付け替え。`ProcessDS4Tick` のコントローラー0固定は未変更（§1A.5 観察1）。
  - `IViewModelFactory.CreateRecordBoxViewModel(…, int targetDevice = -1)` と `ViewModelFactory`（`[DI]` Trace ログに `targetDevice` を付け足し）。
  - `RecordBox.xaml.cs`: コンストラクタを `(int deviceNum, DS4ControlSettings, bool shift, int targetDevice, bool showscan = true, bool repeatable = true)` に変更（必須）。ファクトリ・レガシー生成の両方へ渡す。`RecordBoxWindow.xaml.cs`: `(int deviceNum, DS4ControlSettings, int targetDevice, bool repeatable = true)` に変更（必須）。
  - `SpecialActionEditor.xaml.cs`: コンストラクタを `(int deviceNum, ProfileList, int targetDevice, SpecialAction specialAction = null)` に変更（必須）。`targetDevice` と private の `HasTargetDevice` を追加。バッテリー確認の色プレビューは、実機があるときだけ `checkBatteryVM.*ForcedColor(…, targetDevice)` を呼ぶ（`CheckBatteryViewModel` は変更なし。−1 をそのまま渡すと添字 −1 で例外になるため呼び出し側で判定）。`RecordBoxWindow` 4 箇所・`BindingWindow` 1 箇所へ `targetDevice` を渡す。
  - `ProfileEditor.xaml.cs`: フィールド `targetDevice`（既定 −1）と、暫定値を返す `InterimTargetDeviceFor(int device)`（7b-3 で削除する旨の TODO 付き）を追加し、コンストラクタと `Reload` で設定。7b-1 のコンストラクタ内の暫定変数はこのフィールドに置き換えた。`BindingWindow` 生成 7 箇所・`SpecialActionEditor` 生成 2 箇所へ `targetDevice` を渡す。7b-3 では、このフィールドの設定元を `MainWindow` から渡される値に変えるだけで済む。
  - **挙動の同一性**: Edit ボタン経由は `targetDevice`＝編集スロット＝コントローラー番号で、実機に触れる処理の対象は従来と同じ。一覧経由は `targetDevice = −1` で、従来も `deviceNum`（8）が範囲外のため動いていなかった処理が、同じく動かない。唯一の違いは、一覧経由のマクロ記録で、従来は編集スロット 8 の `TouchOutMode` を一時的に Passthru にして戻していた処理がなくなること（編集スロット 8 のタッチパッドは実機がなく、記録にも使われないため、見た目の変化はない）。
  - **テスト**: `BindingWindowViewModelTargetDeviceTests.cs`（新規、5 件）、`RecordBoxViewModelTargetDeviceTests.cs`（新規、5 件。独立した `BackingStore` を使い、Passthru が実機のスロットにだけ設定されて元に戻ること、`RevertControlsSettings` を 2 回呼んでも値を壊さないこと、実機なしでは何も変えないこと、強制色の対象）、`PatternCViewModelTests.cs`（`CreateRecordBoxViewModel` の既存テストに省略時 −1 の確認を追加し、実際の DI ホストから解決したファクトリで `targetDevice` が届き Passthru が実機のスロットに設定・復元されることを確認するテストを 1 件追加）。既存テストの `TouchOutMode[0]` の後始末漏れ（§1A.2 F2）は、省略時に Passthru を設定しなくなったことで解消。
  - **検証結果（2026-09-26）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。挙動の変化がないため実機確認は省略。

### Step7b-3: 編集スロットの固定と保存・適用・キャンセルの整理（主目的）【実装済み（2026-09-26）、ビルド・テスト・実機確認待ち】
- **変更**:
  - `MainWindow`: `ShowProfileEditor(int targetDevice, ProfileEntity entity)` に変更し、A1〜A5 を更新（Edit／New Profile は `idx`、一覧経由は −1）。`new ProfileEditor(targetDevice)`、`editor.Reload(targetDevice, entity)`。`EmitMissingActionLogsForDevice` には `Global.TEST_PROFILE_INDEX` を渡す（A6）。
  - `ProfileEditor` のコンストラクタと `Reload`: 引数を `targetDevice` にし、`deviceNum = Global.TEST_PROFILE_INDEX` に固定。`Reload` の `TEST_PROFILE_INDEX` 分岐（1103）は常に通る形に整理する（`ProfilePath[8]` の設定は維持）。`useControllerUD` と `conReadingsUserCon.UseDevice(targetDevice >= 0 ? targetDevice : 0, Global.TEST_PROFILE_INDEX)`。**`UpdateMappingDevType` の呼び出しと位置は変えない**。
  - 7b-1・7b-2 で入れた暫定値（`deviceNum < LIMIT ? deviceNum : -1`）を、すべて `targetDevice` に置き換える（ファクトリ・レガシー生成、`BindingWindow` 7 箇所、`SpecialActionEditor` 2 箇所）。
  - `UseControllerReadoutCk_Click`: `profileSettingsVM.TargetDevice >= 0` の判定に変更（B18）。
  - `CalibrateStick_OnClick`: `targetDevice < 0` を不可の判定にし（メッセージの文言は維持）、`StickCalibrationWindow` へ `targetDevice` を渡す（B22）。
  - `RumbleTestBtn_Click`: 決定4＝I1 により、`InverseRumbleMotors` を編集スロット（`deviceNum` フィールド）から読むよう、ローカル変数名を分ける（例: 鳴らす実機は `rumbleDevice`、設定は `deviceNum`）。
  - **適用**: 決定8 により、Save と同じく `ProfileSaved` を通知し、`MainWindow.SyncProfileListAndControllers` がそのプロファイルを使っているスロットにだけ再適用する（当初計画の `ApplyProfileToSlot(targetDevice, …)` は取りやめ）。決定6＝L1 のログ。
  - **キャンセル**: `CancelBtn_Click` の `Task.Run` による再読み込み（1270〜1283）を削除。ランブル停止・`outDevTypeTemp` のリセット・`Closed` 通知は維持。決定6＝L1 のログ。
  - 決定5＝D1 により `DeviceNum` を削除。
  - 決定7＝H1 により、即時フック 8 件（VM の C3 と `SetupEvents` の登録、`ProfileEditor` の B7・B12 と XAML のイベント属性）を削除。
  - 決定6＝L1 のログ（コンストラクタのログへの付け足し、`Reload` のログ）。
- **テスト**:
  - `ProfileEditorTargetDeviceGuardTests`（新規、ソース走査ガード。`ProfileEditor` は WPF の UserControl で単体駆動が困難なため）:
    - `ProfileEditor.xaml.cs` のコンストラクタと `Reload` が `deviceNum = Global.TEST_PROFILE_INDEX` を代入し、`deviceNum = device`／`deviceNum = targetDevice` の形の代入がないこと。
    - `CancelBtn_Click` に `LoadProfile(` と `HaltReportingRunAction` がないこと。
    - `ExecuteSaveOrApply` の `ApplyProfileToSlot(` の第1引数が `targetDevice` であり、`ApplyProfileToSlot(deviceNum` がないこと。
    - `deviceNum == 8` と、`deviceNum < ControlService.CURRENT_DS4_CONTROLLER_LIMIT` の形の判定が残っていないこと。
    - ファクトリ・レガシー生成、`new BindingWindow(`、`new SpecialActionEditor(`、`new StickCalibrationWindow(` に `targetDevice` が渡されていること（渡し忘れの検出。決定2 の補完）。
    - `MainWindow.xaml.cs` の `ShowProfileEditor(` 呼び出しに `Global.TEST_PROFILE_INDEX` が渡されていないこと（編集スロットを呼び出し元が決めない）。
  - `ProfileEditorMappingDevTypeGuardTests`（既存）: 変更なしで合格すること。
- **実機確認**: §4 の全項目。
- **実装結果（2026-09-26）**:
  - **計画からの変更点（2 件）**:
    - `Reload` は `Reload(int targetDevice, ProfileEntity)` ではなく `Reload(ProfileEntity profile = null)` にした。理由: `ProfileSettingsViewModel` はコンストラクタで `targetDevice` を使って生成されるため、`Reload` でも受け取ると両者が食い違う余地が生まれる。`targetDevice` はコンストラクタでだけ受け取り、読み取り専用フィールドにした。既存ガード `ProfileEditorMappingDevTypeGuardTests` のマーカー `public void Reload(` は一致する。
    - 一覧経由を表す定数 `ProfileEditor.NoTargetDevice`（＝ −1）を追加し、`MainWindow` の 3 箇所で使う（リテラル −1 より意図が読みやすいため。新しい型ではなく既存クラスの定数）。
  - `ProfileEditor.xaml.cs`:
    - `deviceNum` を `private readonly int deviceNum = Global.TEST_PROFILE_INDEX;` に固定。`targetDevice` を読み取り専用にし、コンストラクタ `ProfileEditor(int targetDevice)` で設定。private の判定 `HasTargetDevice` を追加。7b-1・7b-2 の暫定値 `InterimTargetDeviceFor` を削除。
    - コンストラクタ: VM（ファクトリ・レガシー）、`SpecialActionsListViewModel`、`TouchButtonUserControl` へ `deviceNum`（作業スロット）を渡す。ログを `[ProfileEditor] Opened profile editor for device=8, targetDevice=N` に（決定6）。
    - `Reload`: 冒頭に `[ProfileEditor] Reload: editSlot=8, targetDevice=N, profile=名前` のログ。`ProfilePath[8]` の設定を無条件に。`LoadProfile`・`LoadBlankDevProfile`・`LSModInfo`／`RSModInfo` は作業スロット。Controller Readings は `UseDevice(profileSettingsVM.FuncDevNum, Global.TEST_PROFILE_INDEX)`（実機、なければコントローラー0）。**`UpdateMappingDevType` の呼び出しと位置は変更なし**。
    - `CancelBtn_Click`: `Task.Run` 内の `HaltReportingRunAction`／`LoadProfile` を削除し、`[ProfileEditor] Cancel: edit slot 8 discarded; controller slots were not modified (targetDevice=N)` のログを追加。ランブル停止・`outDevTypeTemp` のリセット・`Closed` 通知は維持。
    - `ExecuteSaveOrApply`: （当初）適用は `HasTargetDevice` のとき `ApplyProfileToSlot(targetDevice, …)` を実行していた。→ 決定8 により修正（下記「決定8 による追加修正」）。保存は従来どおり作業スロットから。
    - `RumbleTestBtn_Click`: ローカル変数を `rumbleDevice`（`FuncDevNum`）に改名し、左右反転は作業スロット（フィールド `deviceNum`）を読む（決定4＝I1）。`GyroCalibration_Click` も同様にローカル変数を `calibrationDevice` に改名（動作は同じ）。
    - `UseControllerReadoutCk_Click`: `HasTargetDevice` で判定。`CalibrateStick_OnClick`: `!HasTargetDevice` で不可（メッセージ文言は維持）、`StickCalibrationWindow` へ `targetDevice`。
    - 決定5＝D1: `DeviceNum` プロパティを削除。決定7＝H1: `GyroOutModeCombo_SelectionChanged`（`SetupEvents`／`UnregisterEvents` の登録・解除を含む）と `FrictionUD_ValueChanged` を削除し、経緯のコメントを残した。
  - `ProfileEditor.xaml`: `frictionUD` の `ValueChanged="FrictionUD_ValueChanged"` を削除（1 行）。
  - `ProfileSettingsViewModel.cs`（決定7＝H1）: 即時フック 6 件のハンドラーと `SetupEvents` での登録を削除し、経緯のコメントを残した。イベント自体（`GyroOutModeIndexChanging` 等）は他の購読者があるため残す。
  - `MainWindow.xaml.cs`: `ShowProfileEditor(int targetDevice, ProfileEntity entity)`（XML コメント付き）。一覧経由の 3 箇所は `ProfileEditor.NoTargetDevice`。`new ProfileEditor(targetDevice)`、`editor.Reload(entity)`、`EmitMissingActionLogsForDevice(Global.TEST_PROFILE_INDEX, …)`。
  - **テスト**: `ProfileEditorTargetDeviceGuardTests.cs`（新規、8 件のソース走査ガード）: 作業スロットの固定と代入が初期化子の 1 箇所だけであること、`deviceNum == 8`／`deviceNum < CURRENT_DS4_CONTROLLER_LIMIT`／`InterimTargetDeviceFor` がないこと、キャンセルに `LoadProfile(`／`HaltReportingRunAction` がないこと、保存が `SaveProfile(deviceNum,` であること（適用の検査は下記「決定8 による追加修正」で置き換え）、`BindingWindow`（7 件）・`SpecialActionEditor`（2 件）・VM 生成へ `targetDevice` を渡すこと、スティック再校正の判定と引数、`DeviceNum`・削除した即時フックの再混入がないこと（XAML を含む）、VM に `touchPad[device]`／`deltaAccelProcessors[device]` がないこと、`MainWindow` の `ShowProfileEditor(` に `TEST_PROFILE_INDEX` を渡さないこと、サブ画面の下位への受け渡し（7 種の呼び出し）。
  - **未検証事項**: この環境では `dotnet build`／`dotnet test` を実行していない。ユーザー側でビルド・テストビルド・テスト実行と、§4 の実機確認を行う。
- **決定8 による追加修正（2026-09-26、7b-3 のビルド前に実施）**:
  - 実機確認の前に、ユーザーから Save／Apply の仕様（§2A 決定8）が示され、上記の「適用は `ApplyProfileToSlot(targetDevice, …)`」が仕様に反することが分かったため、修正した。
  - `ExecuteSaveOrApply`: `isApply` による分岐と `ApplyProfileToSlot(targetDevice, …)`・その 3 種のログを削除。Save・Apply 共通で `[ProfileEditor] Save|Apply: profile '名前' saved; re-applying to controller slot(s) that use it (targetDevice=N)` をログに出し、`ProfileSaved` を通知する。`ProfileEditor` からコントローラーのスロットへ直接適用する箇所はなくなった。
  - `ProfileEditorTargetDeviceGuardTests.cs`: 適用の検査を `ProfileEditor_SaveAndApplyBothDelegateReapplyToMainWindow`（`ExecuteSaveOrApply`・`ProfileEditor` 全体に `ApplyProfileToSlot(` がないこと、`ProfileSaved` の通知が 1 箇所で分岐なしであること、Save は閉じて Apply は閉じないこと）に置き換え、`MainWindow_ReappliesOnlyToSlotsUsingTheSavedProfile`（`Editor_ProfileSaved` → `SyncProfileListAndControllers` が、プロファイル名が一致するスロットにだけ `ApplyProfileToSlot` すること）を追加（計 9 件）。
- **範囲外の追加修正（2026-09-26、実機確認中のユーザー決定）**: マクロ記録画面（`RecordBox`）の右下のボタンのうち、**Add Rumble／Change Lightbar Color** は、記録中かつ Record Delays にチェックがあるときだけ表示されていた（2019-12-18 の上流取り込み `906946a7` からの条件。理由の記録なし。開始と終了の間に Wait を入れるためと推測）。ユーザー決定により、Record Delays の条件を撤廃し、**4th/5th Mouse Button と同じく「記録中だけ表示」**にした（`RecordBox.xaml.cs` の `RecordBtn_Click`）。Record Delays がオフの場合、開始と終了のステップの間に Wait が入らないため、必要に応じて記録後に Insert Wait やダブルクリックで待ち時間を調整する。ソース走査ガード `RecordBoxExtraButtonsVisibilityGuardTests.cs`（新規、1 件）を追加。検討の過程で見つけた関連事項（本 Step では変えない）: (1) 第 4・第 5 マウスボタンの「Stop 時に終了ステップを自動で足す」処理は、判定に使う `Toggle4thMouse`／`Toggle5thMouse` がどこでも true にならず動いていない、(2) マクロ再生の終了処理（`Mapping.EndMacro`）は、押したままのボタン・ライトバー・振動を元に戻さないため、終了ステップのないマクロは再生後もその状態が続く。

### Step7b-4: 文書・実機確認・完了報告
- **文書**: `Phase6-Status.md`（§1 の表、Step7b の節、§5、§6.5 に観察事項）、`Phase6-Plan.md`（Step7b の記載を §0.2・§1・§2 に追加。§1A.1-11）、`Phase6-Step8-Plan.md`（冒頭に「Step7b からの申し送り」を追加。`ProfileEditor.xaml.cs` の行番号と、`ExecuteSaveOrApply`・`CancelBtn_Click`・`Reload` の内容が変わるため、Step8-0 の台帳は Step7b 後の内容で作り直すこと）、モデル図 03・04（決定3＝M1）。`copilot-instructions.md` の変更は不要。
- **先送り**: 実機で確認できない項目は `Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録する。
- **完了報告書**: `Phase6-Step7b-Completion-Report.md`。

---

## 4. 実機確認項目（Step7b-3 の後に実施。Vader 4 Pro／DS4／DualSense）

### 4.0 準備
- ログの最小レベルを Debug にしておく（DEBUG 行で判定する項目があるため）。`[DI]` の Trace 行も見る場合は Trace にする。
- 用語: 「コントローラー一覧の Edit」＝メイン画面の Controllers タブで、各コントローラーの行にある **Edit** ボタン（スプリットボタン）。「New Profile」＝その右の▼を開くと出る **New Profile**。「プロファイル一覧」＝Profiles タブの **New**／**Edit** ボタンとダブルクリック。編集画面の下部のボタンは **Save**（保存して閉じる）／**Cancel**／**Apply**（保存して開いたまま）。
- 以下の「N」はコントローラーのスロット番号（0 始まり。コントローラー一覧の 1 行目が 0）。

### 4.1 主目的（即時反映の廃止）
1. コントローラー一覧の Edit で、接続中のコントローラーのプロファイルを開く。
   - ログ: `[ProfileEditor] Opened profile editor for device=8, targetDevice=N` と `[ProfileEditor] Reload: editSlot=8, targetDevice=N, profile=名前` が出る（決定6＝L1）。
2. ジャイロタブの Output Mode を Mouse に変える。**ポインタが動かない**こと。
   - ログ: この操作の時点で `ApplyProfile CALLED` が出ないこと。
3. Apply を押す。**ポインタが動き出す**こと（プロファイルでジャイロマウスの起動条件がある場合は、その条件を満たしたとき）。画面は開いたまま。
   - ログ: `[ProfileEditor] Apply: profile '名前' saved; re-applying to controller slot(s) that use it (targetDevice=N)` と、そのプロファイルを使っているスロットごとの `ApplyProfile CALLED: device=…, profile=名前, isTemp=False, source=Manual`（決定8）。

### 4.2 設定の反映（決定8: Save と Apply の違いは画面を閉じるかどうかだけ）
4. 編集して **Apply** を押すと、そのプロファイルを使っているコントローラーに反映され、画面は開いたまま（ログは 4.1-3 と同じ）。
5. 同じプロファイルを 2 台のコントローラーで使っている状態で、編集して **Apply**（画面は開いたまま）または **Save**（画面を閉じる）を押すと、両方に反映される。
   - ログ: 使用中のスロットの数だけ `ApplyProfile CALLED: device=…` が出る。Save のときは `[ProfileEditor] Save: profile '名前' saved; …`。

### 4.3 キャンセル
6. 編集（例: Output Mode を Mouse、ライトバー色の変更）して、保存も適用もせずに **Cancel** を押す。コントローラーの動作が変わらず、プロファイルファイル（`Profiles` フォルダの `名前.xml`）の更新日時も変わらないこと。
   - ログ: `[ProfileEditor] Cancel: edit slot 8 discarded; controller slots were not modified (targetDevice=N)`。`ApplyProfile CALLED` が出ないこと。

### 4.4 一時確認機能（Edit ボタン経由で実機に効くこと）
7. ランブルテスト（Rumble の Light／Heavy のテストボタン）で、そのコントローラーが振動する。Inverse Rumble Motors のチェックを変えてすぐテストし、左右が入れ替わる（決定4＝I1）。
8. ライトバー色のプレビュー: Lightbar タブで色を選ぶダイアログを開いている間、実機の色が変わり、閉じると元に戻る。
9. Controls タブのボタン設定画面（BindingWindow）のテストランブルと、Extra 欄の色選択中のライトバープレビュー。
10. BindingWindow の **Record A Macro** で、ライトバー色ステップの色を選ぶ間のプレビュー（決定1＝A）。手順（`RecordBox.xaml.cs` で確認）: 「記録」を押す → 右下の **Change Lightbar Color** を押す（一覧に `Lightbar Color: 255,255,255` が入る）→ もう一度押す（`Reset Lightbar` が入る）→ 「Stop」→ 一覧の `Lightbar Color: …` をダブルクリックして色選択画面を開く。色選択画面を開いている間、Edit ボタンのコントローラーのライトバーが選んだ色になり、閉じると元に戻ること。確認後は「キャンセル」で閉じる。
    - タッチパッドの Passthru: Record A Macro 画面を**開いている間**（記録中に限らない。ViewModel の生成時に設定し、保存・キャンセルで戻す）、そのコントローラーのタッチパッドがマウス操作にならないこと、閉じると元に戻ること。タッチパッドの出力が Mouse のプロファイルで確認すると分かりやすい。
11. Special Actions タブで Check Battery アクションを編集し、空・満充電の色選択中にライトバーがプレビューされる（決定1＝A）。
12. Controller Readings タブ: そのコントローラーの入力が表示され、編集中のデッドゾーン等を変えると、出力側の表示が変わる（保存しなくても画面上で確認できる）。
13. マッピング一覧の「for readout」をチェックし、コントローラーのボタンを押すと、その割り当てが選ばれる。

### 4.5 校正
14. スティックの Automatically recalibrate stick（Edit ボタン経由のみ可。一覧経由では従来どおり「Stick recalibration is only available if the profile editor is opened with the Edit button …」のメッセージが出る）。校正後、保存・適用すると補正が効く。
15. Gyro Calibration ボタン。
16. ステアリングホイール校正（vJoy 環境がない場合は Step11 へ先送り）。

### 4.6 一覧経由・新規作成
17. プロファイル一覧の Edit／ダブルクリックで開いて編集・保存する動作。ランブルテストはコントローラー0 に対して動く（従来どおり）。ライトバーのプレビューは従来どおり動かない。**Apply でも、そのプロファイルを使っているコントローラーへ再適用される**（決定8による変更。従来の一覧経由の Apply は保存のみだった）。
   - ログ: `Reload: editSlot=8, targetDevice=-1`。Apply／Save 時は、そのプロファイルを使っているコントローラーの数だけ `ApplyProfile CALLED: device=…` が出る。
18. コントローラー一覧の New Profile: プリセット選択 → 編集 → **Apply**／**Save** のどちらでも、そのコントローラーは元のプロファイルのまま（決定8。新しいプロファイルはどのコントローラーにも使われていないため）。新しいプロファイルは、Apply の時点でプロファイル一覧に現れる。コントローラーで使うには、コントローラー一覧のプロファイル選択で選ぶ。
   - ログ: `[ProfileEditor] Apply: profile '名前' saved; …` のあとに、`ApplyProfile CALLED` が出ないこと。
19. プロファイル一覧の New: 従来どおり作成・保存できる。

- 環境がなく実施できない項目は、`Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録する。

---

## 5. リスクと対策
- **編集スロット 8 の共有**: 一覧経由の編集で既に使われている（動作実績あり）。同時に開ける編集画面は 1 つ（`MainWindow.ShowProfileEditor` の `editor == null` の判定）。
- **`Global.ProfilePath[8]` の扱い**: `Reload` の `TEST_PROFILE_INDEX` の分岐が、そのまま新仕様の全経路で使われる。
- **適用時の副作用**: §1A.3 で確認済み。即時フックの処理は、すべて適用時に行われる。
- **一覧経由での挙動変化なし**: `targetDevice = −1` の場合、現行動作（`FuncDevNum = 0` へのフォールバック、`UseDevice(0, TEST)`、ライトバープレビューなし）を維持する。例外は決定4＝I1 のときの左右反転の読み先のみ（§2A 決定4）。
- **渡し忘れ**: 省略可能な引数を渡し忘れると、コンパイルは通るが実機に効かない。決定2 の必須引数化と、7b-3 のソース走査ガードで防ぐ。
- **テストと実アプリの違い（K7-1 の教訓）**: VM・ファクトリのテストは実際の DI ホストから解決したファクトリでも確認する。最終判定は §4 の実機確認とログで行う。
- **Step8 との衝突**: 本ステップ完了後に Step8 の台帳を作り直す（`ProfileEditor.xaml.cs` の行番号と、保存・適用・キャンセル・`Reload` の内容が変わるため）。

## 6. ロールバック方針
- ステップ単位で `git revert` する。7b-1・7b-2 は挙動を変えないため、単独で残しても問題ない。主目的の 7b-3 を revert すれば、旧動作（即時反映）に戻る。

## 7. 見積り（2026-09-26 改訂）
- 7b-0: 完了（2026-09-24 調査、2026-09-26 再確認）
- 7b-1: 約 2 時間（VM・ファクトリの拡張とテスト）
- 7b-2: 約 3.5 時間（決定1＝A。サブ画面 5 ファイル・VM 2 ファイル・ファクトリとテスト）
- 7b-3: 約 3 時間（`MainWindow`・`ProfileEditor` の書き換え、決定5・7 の削除、ガードテスト）
- 7b-4: 約 4 時間（文書、実機確認、完了報告）
- **合計: 約 1.5〜2 日**

## 8. 完了判定チェックリスト
- [x] Step7b-0 の調査項目 1〜6 が確認され、本書へ追記されている（2026-09-24）
- [x] 着手前再確認（台帳の再作成、旧調査との乖離の記録、決定事項の提示）（2026-09-26）
- [x] 決定1〜7 がユーザーにより確定し、本書に記録されている（2026-09-26、すべて推奨案。§2A.0）。決定8（Save／Apply の仕様）も確定・記録済み（2026-09-26）
- [ ] 保存・適用で、そのプロファイルを使っている全スロットに設定全体が再適用され、使っていないコントローラー（New Profile の作成元を含む）には適用されない（決定8）
- [ ] プロファイル編集画面の編集スロットが、常に `TEST_PROFILE_INDEX` である
- [ ] 編集中の変更が、保存・適用まで接続中のコントローラーに反映されない
- [ ] キャンセルでコントローラーのスロットが変化しない
- [ ] ランブルテスト、ライトバー色プレビュー（`BindingWindow`・マクロ記録・バッテリー確認のものを含む。決定1＝A）、入力読み取り表示、各種校正が `targetDevice` で動く
- [ ] 一覧経由の編集の動作が従来と変わらない（決定8 による Apply の再適用を除く）
- [ ] `ProfileEditorMappingDevTypeGuardTests` を含む既存テストと新規テストが全件成功（`dotnet build`、`dotnet test`）
- [ ] 実機確認（§4）が完了、または Step11 の先送り台帳に登録済み
- [ ] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step8-Plan.md`（台帳の作り直しの申し送り）を更新、モデル図 03・04 を更新（決定3＝M1）、完了報告書を作成
