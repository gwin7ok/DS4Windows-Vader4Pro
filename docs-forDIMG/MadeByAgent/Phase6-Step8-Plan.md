# Phase6-Step8 計画書: `ProfileEditor.xaml.cs` の段階的MVVM移設と `MainWindow.xaml.cs` 解体推進

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスに基づく案3-A［段階的MVVM移設による Fat View 解体・Pure DI 推進］採用・全面拡充改訂）
再改訂日: 2026-10-13（Step7b 完了後の現行コード［HEAD `e022684b`］との突き合わせ。§0A を追加し、台帳の作り直し・決定事項・マイクロステップ・実機確認項目を改訂）    
状態: **完了（2026-10-13）**。Step8-0〜8-5 完了。完了報告書 `Phase6-Step8-Completion-Report.md`。（経過: Step8-0 完了、決定1〜6 確定［すべて推奨案］。Step8-1（契約 B8・B10 の追加）完了確定（ビルド・テストビルド・テスト実行成功、反映済み）。Step8-2（レイアウト・言語 23 件）完了確定（ビルド・テスト・実機確認 1・2 成功、反映済み）。Step8-3（G2〜G8、35 件）完了確定（ビルド・テスト・実機確認成功、反映済み）。Step8-4（保存・ブランク読み込み等の ViewModel 移設）完了確定。Step8-5 完了確認済み**。現行の正は §0A。§2〜§5 は旧版（2026-09-18）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §2（`ProfileEditor.xaml.cs` 実地監査記録・全98箇所）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類・契約差台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  

---

> **Step7 からの申し送り（2026-09-25 追記。Step8-0 の台帳再作成で確認すること）**
> - `ProfileEditor.xaml.cs:280` の `Global.UseLang` は、Step7-1 で新設した `IAppSettingsService.UseLang`（`Global.UseLang` への委譲）へ置き換えられる。
> - `ProfileEditor.xaml.cs` の `App.logHolder`（静的フィールド、約 24 箇所）と、`MainWindow.xaml.cs` が呼ぶ App の公開メンバー（`ChangeTheme`、`ThemeChanged`、`WriteIPCResultDataMMF`）は、Step7 では変更していない。
> - `MainWindow` のコンストラクタを変える場合は、`App.xaml.cs` の `new DS4Forms.MainWindow(parser)` と `window.LateChecks(parser)` も合わせて直す（Composition Root 側。`App` は `InitializePostHostServices` で解決済みのサービスをフィールドに持っている）。
> - 起動時の DI ホストは、`Global` の静的初期化で起動引数なしに先に作られている（持ち越し K7-1）。`AppHost.GetService` はいつでも同じシングルトンを返すため Step8 の作業への影響はないが、「ホストは `App.xaml.cs` の明示的な構築で作られる」という前提で設計しないこと。
> - 詳細は `Phase6-Step7-Plan.md` §2.8、`Phase6-Step7-Completion-Report.md`。

> **Step7b からの申し送り（2026-09-26 追記。Step8-0 で必ず確認すること）**
> - **台帳は作り直しが必要**: Step7b で `ProfileEditor.xaml.cs` を大きく変更したため、本書 §2 以降の行番号・参照の台帳（C8-xx）は使えない。Step8-0 で、Step7b 完了後のコードから台帳を作り直すこと。
> - **編集スロットと実機の分離**: `ProfileEditor` の `deviceNum` は `private readonly int deviceNum = Global.TEST_PROFILE_INDEX;` に固定された（作業スロット）。実機は `targetDevice`（コンストラクタ `ProfileEditor(int targetDevice)` でだけ受け取る。一覧経由は `ProfileEditor.NoTargetDevice`＝−1）。`Reload(ProfileEntity profile = null)` は `targetDevice` を受け取らない。`ProfileEditor.DeviceNum` は削除済み。MVVM 移設後も、編集スロットと実機を同じ番号で兼用しないこと（モデル図 03・04 の 2026-09-26 の注釈）。
> - **保存・適用（B9 の前提が変わった）**: `ExecuteSaveOrApply` は Save・Apply のどちらでも保存後に `ProfileSaved` を通知するだけになり、`ProfileEditor` から `ApplyProfileToSlot` を直接呼ぶ箇所はなくなった（決定8。違いは画面を閉じるかどうかだけ）。再適用は `MainWindow.SyncProfileListAndControllers`（そのプロファイル名を使うスロットにだけ `Global.ApplyProfileToSlot`）が行う。本書の C8-44（`ApplyProfileToSlot` を `ProfileSettingsViewModel.SaveProfile()` へ移す案）と §3.3 の B9 シムの設計は、この前提で見直すこと。
> - **キャンセル**: `CancelBtn_Click` はコントローラーのスロットへの再読み込み（`HaltReportingRunAction`／`LoadProfile`）を行わない。
> - **削除済みの即時フック**: `GyroOutModeCombo_SelectionChanged`、`FrictionUD_ValueChanged`（XAML の属性を含む）、`ProfileSettingsViewModel` の 6 件（`touchPad[device]`／`deltaAccelProcessors[device]` への即時リセット）。
> - **`targetDevice` を受け取るようになった型**: `ProfileSettingsViewModel`・`BindingWindowViewModel`・`RecordBoxViewModel`（省略可能、既定 −1）、`BindingWindow`・`RecordBox`・`RecordBoxWindow`・`SpecialActionEditor`（必須）、`IViewModelFactory.CreateProfileSettingsViewModel`／`CreateRecordBoxViewModel`（省略可能）。
> - **維持が必要なソース走査ガード**: `ProfileEditorTargetDeviceGuardTests`（作業スロットの固定、キャンセル、Save／Apply の通知、`targetDevice` の受け渡し、削除したコードの再混入）、`ProfileEditorMappingDevTypeGuardTests`（`Reload`／`RefreshEditorBindings` の `UpdateMappingDevType`）、`RecordBoxExtraButtonsVisibilityGuardTests`、`StickDeadZoneBindingGuardTests`。ロジックを ViewModel へ移すときは、これらのガードを移設先に合わせて書き換えること（検査の意図は維持する）。
> - **持ち越し K7b-1**: 実機確認中の異常終了（原因未特定、先送り）。`ProfileEditor` の操作中に発生したが、Step7b の回帰である証拠はない。`Phase6-Status.md` §6.5。
> - 詳細は `Phase6-Step7b-Plan.md`、`Phase6-Step7b-Completion-Report.md`。

## 0A. 着手前再確認（Step8-0、2026-10-13、HEAD `e022684b`）

> **本節が現行の正**。§2（旧台帳）・§3（旧設計）・§4（旧マイクロステップ）・§5（旧テスト計画）は 2026-09-18 版で、Step7b 完了後の現行コードと合わないため、下記 0A.1〜0A.6 で置き換える（旧記載は経緯として残す）。決定事項（0A.3）はユーザー確認後に確定する。

### 0A.1 旧計画との乖離（現行コードとの突き合わせ結果）
1. **件数**: 実移行対象は旧 60 件 → 現行 **58 件**。除外の定数は旧 38 件 → 現行 **37 件**（`ASSEMBLY_RESOURCE_PREFIX` 29、`RESOURCES_PREFIX` 5、`TEST_PROFILE_INDEX` 3）。行番号はすべて変わっている（正は 0A.2）。`ProfileEditor.xaml.cs` は 2,498 行。
2. **旧 C8-44（`ApplyProfileToSlot`）は消滅**。Step7b 決定8 で、保存・適用のどちらでも `ProfileSaved` を通知するだけになり、`ProfileEditor` から `ApplyProfileToSlot` を呼ぶ箇所はなくなった。
3. **旧 §3.1 の `SaveProfile` 設計は採用できない**。旧設計は「保存後に `ApplyProfileToSlot(DeviceIndex, true)` を呼ぶ」ものだが、決定8 に反する（再適用は `MainWindow.SyncProfileListAndControllers` の責務。`ProfileEditorTargetDeviceGuardTests` が `ApplyProfileToSlot(` の不在を検査している）。
4. **契約差の吸収シムは B8 と B10 だけが必要**。
   - B3（`UseLang`）: Step7-1 で `IAppSettingsService.UseLang` として追加済み。
   - B9（`ApplyProfileToSlot`）: 呼び出し元が消えたため不要（作らない。§2.4 の考え方に従い、使わない API は増やさない）。
   - B8（`GetActionOrPlaceholder`）: 必要。`Global.GetAction` は、名前を `Trim` して大文字小文字を無視して探し、見つからなければ **`new SpecialAction("null","null","null","null")` を返す**。一方 `ISpecialActionRepository.GetAction` は、`Trim` せず、見つからなければ **`null` を返す**。呼び出し元（`CreateActionItem(action)`）は `null` を想定していないため、同じ契約の新メンバーが必要。
   - B10（`LoadBlankDevProfile`）: 必要。ただし旧計画の `LoadBlankDevProfile(int deviceIndex)` では不足。`Global.LoadBlankDevProfile(device, launchprogram, control, xinputChange, postLoad)` は、(a) 空プロファイルの読み込み、(b) 既定スペシャルアクションの作成、(c) `CacheExtraProfileInfo`、(d) 一時プロファイル名・使用フラグ・`tempprofileDistance` のクリア、まで行う。呼び出しは `(deviceNum, false, controlService, false)`。同じ呼び出しが `DS4Control/PresetOption.cs:76` にもある。
5. **新規サービスの追加はほぼ不要**。58 件のうち 55 件は、すでに存在する契約への置換で足りる（0A.2）。旧計画が想定した「VM への大規模移設」は、Step5〜7b で `profileRepository`・`IProfileSettingsService` 経由への置換が先に進んだため、残りは **フォールバック経路と直接参照の置換**が中心になっている。
6. **旧計画に無かった静的結合**（本 Step の件数 58 には含まれない）:
   - `App.logHolder`（24 箇所）、コンストラクタ内の `AppHost.GetService`（4 箇所）と `Program.rootHub` へのフォールバック（1 箇所）、`App.Current.MainWindow`（`window.Owner`、7 箇所）。決定1。
7. **`ProfileSettingsViewModel.InverseRumbleMotors`（2877 行）が既にある**。`RumbleTestBtn_Click` の 5 箇所は、編集スロットの値を返すこの VM のプロパティへ置換でき（Step7b 決定4＝I1 と同じ値）、新規契約は要らない。
8. **`ProfileSettingsViewModel` 側にも `Global.` が 19 件**（`?? Global.ProfileSettingsServiceInstance` 等のフォールバック）ある。これは Step9（C9-P01〜P10）の対象で、本 Step では触れない。
9. **`OutConTypeCombo_SelectionChanged` の二重書き込み**: `Global.OutContType`（`m_Config.outputDevType`）と `Global.ProfileSettingsServiceInstance.OutContType`（`SafeConfig?.outputDevType`）は同じ配列を指すため、同じ値を 2 回書いている（動作は同じ）。置換時に 1 回へ整理する（決定5）。
10. **ガードテストの更新が必要**: `ProfileEditorTargetDeviceGuardTests` の `Assert.Contains("SaveProfile(deviceNum,", body)`（154 行）は、保存呼び出しの書き方を変えると失敗する。該当するマイクロステップで、同じコミット内に更新する。

### 0A.2 再確認した台帳（現行 `ProfileEditor.xaml.cs`、実移行対象 58 件）
矢印の右は本 Step での置換先（案）。「編集スロット」は常に `TEST_PROFILE_INDEX`（`deviceNum`）。

#### G1. レイアウト・言語（23 件）
- `UseLang`（293）→ `IAppSettingsService.UseLang`
- `FormWidth`／`FormHeight`（356、357）→ `IAppSettingsService`（既存）
- 既定値へのリセット（348〜352）、保存（387〜392）、保存ログ内の読み取り（395）、復元（411〜427）の列幅 20 式 → `IAppSettingsService`（既存。`ProfileEditorLeftWidth`／`RightWidth`、`SpecialActionName`／`Trigger`／`DetailColWidth`）
- 受け取り方は決定2。

#### G2. 編集スロットの読み込み（Reload。9 件）
- `Global.ProfilePath[TEST_PROFILE_INDEX] = profile.Name`（1119）→ `IProfileRepository.ProfilePath`（既存）
- `Global.LoadBlankDevProfile(deviceNum, false, controlService, false)`（1133）→ B10（決定4）
- `Global.ProfileActions`（1138 ×2、1139）→ `IProfileRepository.ProfileActions`（既存）
- `Global.LSModInfo[deviceNum]`／`RSModInfo[deviceNum]`（1154、1155、1172、1184）→ `IProfileSettingsService.LSModInfo`／`RSModInfo`（既存）。4 件。

#### G3. 保存・キャンセル（3 件）
- `Global.ProfileActions[deviceNum] = …`（1382、`ExecuteSaveOrApply`）→ `IProfileRepository.ProfileActions`
- `Global.SaveProfile(deviceNum, profileName)`（1394）→ `else` のフォールバック。`profileRepository` はコンストラクタで必ず非 null（`?? new ProfileRepository()`）なので、`else` を削除
- `Global.outDevTypeTemp[deviceNum] = X360`（1275、`CancelBtn_Click`）→ `IOutputSlotService.OutDevTypeTemp`（既存）

#### G4. ランブルテスト（5 件）
- `Global.InverseRumbleMotors[deviceNum]`（1516、1560、1567、1579、1588、`RumbleTestBtn_Click`）→ `profileSettingsVM.InverseRumbleMotors`（既存。編集スロットの値＝Step7b 決定4）

#### G5. 出力コントローラー種別（3 件）
- `Global.OutContType[deviceNum] = selected`（1774）、`Global.ProfileSettingsServiceInstance.OutContType[deviceNum] = selected`（1775、1777）→ `IProfileSettingsService.OutContType`（既存）。二重書き込みを 1 回へ（決定5）。

#### G6. スペシャルアクション（5 件）
- `Global.GetAction`（1806、1872）→ B8（`ISpecialActionRepository.GetActionOrPlaceholder`。決定3）
- `Global.ProfileActions`（1849、`EditActionBtn_Click` の Saved、名前の置換）、同（1917、`SpecialActionCheckBox_Click`）→ `IProfileRepository.ProfileActions`
- `Global.CacheExtraProfileInfo(deviceNum)`（1861）→ `IProfileRepository.CacheExtraProfileInfo`（既存）

#### G7. キャッシュ更新（9 件）
- `Global.CacheProfileCustomsFlags(profileSettingsVM.Device)` 8 件（1292、2060、2124、2251、2281、2320、2363、2396。いずれも `BindingWindow` を閉じた後）→ `IProfileRepository.CacheProfileCustomsFlags`（既存）
- `Global.CacheExtraProfileInfo(profileSettingsVM.Device)`（2033、`ProfileEditor_Closed`）→ `IProfileRepository.CacheExtraProfileInfo`

#### G8. エクスポート（1 件）
- `Global.appdatapath`（2438、`ExportActionsXml` 相当）→ `IPathService.AppDataPath`（既存）

#### 合計
- G1 23＋G2 9＋G3 3＋G4 5＋G5 3＋G6 5＋G7 9＋G8 1＝**58 件**（`grep` での確認値と一致）。
- 既存の契約への置換で足りるのは 55 件。新しい契約が要るのは B8（`GetAction` 2 件）と B10（`LoadBlankDevProfile` 1 件）の 3 件。
- 除外（定数）は別に 37 件。

### 0A.3 決定事項（2026-10-13 確定: 決定1＝S1、決定2＝L2、決定3＝B8-1、決定4＝B10-1、決定5＝O1、決定6＝M-A。すべて推奨案）

> 確定時の補足（決定3）: `GetActionOrPlaceholder` の委譲先は、`Global.GetAction` そのものではなく、その実体である `BackingStore.GetAction`（`SpecialActionRepository._config`。既定は `Global.store`＝`m_Config`）にした。結果は `Global.GetAction` と一致し、リポジトリが自身のストアを使うためテストでも検証できる。

以下は提示時の前提・選択肢・推奨理由の記録。

#### 決定1: Step8 の対象範囲（静的結合をどこまで解消するか）
- **前提**: `ProfileEditor.xaml.cs` には、`Global.` の 58 件（本 Step の台帳）のほかに、`App.logHolder`（24）、コンストラクタ内の `AppHost.GetService`（4）と `Program.rootHub` フォールバック（1）、`App.Current.MainWindow`（7）がある。`App.logHolder` と App の公開メンバーは Step7 で変更しておらず、`MainWindow` のコンストラクタ周りは Step9／Step10 の範囲。
- **案S1（推奨）: `Global.` の 58 件だけを本 Step で解消する**
  - 理由: 全体計画（Step8 の目的 4）と件数が一致し、差分が小さい。`App.logHolder` はログ基盤（Step7 決定）、コンストラクタの `AppHost.GetService` は `MainWindow` の解体（Step9／10）と一緒に直すのが筋で、ここで先に変えると二度手間になる。残る静的結合は Step10 の台帳へ登録する。
- **案S2: コンストラクタの `AppHost.GetService`／`Program.rootHub` も解消する**
  - 利点: `ProfileEditor` が Service Locator を持たなくなる。欠点: `MainWindow` の生成呼び出し（A1〜A5、`ShowProfileEditor`）が必須引数だらけになり、旧計画が避けようとした「バケツリレー」になる。
- **案S3: `App.logHolder` まで含めて解消する**
  - 欠点: ログ基盤の入れ替えになり、Step8 の範囲を超える（ログ出力は §2.3 により完全維持が必要で、検証コストが大きい）。

#### 決定2: レイアウト設定（列幅・ウィンドウサイズ・言語。23 件）の受け取り方
- **前提**: これらはエディタ画面自体の見た目の設定で、編集中のプロファイルとは無関係。`ProfileEditor` は View で、`new ProfileEditor(targetDevice)` で生成され、コンストラクタで `IProfileRepository` 等を `AppHost.GetService` で解決している（Step7b 時点）。`MainWindow` は `IAppSettingsService appSettingsService` を既にフィールドで持っている。
- **案L1: `ProfileEditor` のコンストラクタで `AppHost.GetService<IAppSettingsService>()` を解決する**
  - 利点: 既存の流儀と同じで、生成呼び出しは変わらない。欠点: Service Locator が 1 件増える（Pure DI の原則 §3.1 に反する）。
- **案L2（推奨）: `ProfileEditor` のコンストラクタに `IAppSettingsService` を必須引数で追加し、`MainWindow` から渡す**
  - 利点: Pure DI の原則に沿う。渡し忘れはコンパイラが検出する。`MainWindow` は既に持っているため、追加の解決は不要。呼び出し箇所は `ShowProfileEditor` の 1 箇所（`new ProfileEditor(...)`）だけ。
  - 欠点: `Phase6-Step8-Plan.md` §0.2 が避けた「サービスの横流し」が 1 つ発生する（1 サービスのみ）。
- **案L3: `ProfileSettingsViewModel` へレイアウト用のプロパティを追加する**
  - 欠点: 編集対象のプロファイルと無関係な設定が、プロファイル編集 VM に入り、責務が混ざる。ファクトリの契約（モデル図 03・04）の再変更も必要。
- **推奨理由**: 責務の分離（レイアウトはプロファイルと無関係）と Pure DI の原則の両方を満たすのは L2 で、変更も 1 サービス・1 箇所に収まる。

#### 決定3: B8（`GetActionOrPlaceholder`）の追加先と名前
- **前提**: 0A.1-4 のとおり、`Global.GetAction`（placeholder を返す）と `ISpecialActionRepository.GetAction`（`null` を返す）は契約が違う。
- **案B8-1（推奨）: `ISpecialActionRepository` に `GetActionOrPlaceholder(string name)` を追加し、`Global.GetAction` へ委譲する薄いシムとして実装する**
  - 理由: 既存の `GetAction` の契約（`null`）を変えず、呼び出し元 2 件（1806、1872）が同じ動作を保てる。§2.1 の「薄いシム」に沿う。
- **案B8-2: 呼び出し側で `GetAction(name) ?? placeholder` と書く**
  - 欠点: placeholder の定義（`"null"` ×4）が View に複製される。`Trim`・大文字小文字の差も残る。

#### 決定4: B10（ブランクプロファイル読み込み）の契約
- **前提**: 0A.1-4 のとおり、現行の呼び出しは `(device, launchprogram=false, control, xinputChange=false)` で、`ControlService` を渡す。`IProfileRepository.LoadTempProfile(..., ControlService control, ...)` に前例がある。`PresetOption.cs:76` にも同じ呼び出しがある。
- **案B10-1（推奨）: `IProfileRepository` に `LoadBlankProfile(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)` を追加し、`Global.LoadBlankDevProfile` へ委譲する薄いシムとして実装する**
  - 理由: 既存の呼び出しの引数と、(a)〜(d) の副作用をそのまま保てる。`LoadTempProfile` と同じ形で、`PresetOption.cs` も後で同じ契約へ置換できる（本 Step では `PresetOption.cs` は変更しない。Step10 の台帳へ登録）。
- **案B10-2: `ControlService` を取らない `LoadBlankProfile(int deviceIndex)` にして、シムの内部で `Program.rootHub` を使う**
  - 欠点: シムの内部に静的結合を持ち込む。

#### 決定5: `OutConTypeCombo_SelectionChanged` の二重書き込み
- **前提**: 0A.1-9 のとおり、同じ配列に同じ値を 2 回書いている。
- **案O1（推奨）: `IProfileSettingsService.OutContType[deviceNum] = selected` の 1 回にする**
  - 動作は同じ（同じ配列）。実機確認でログ（`OutContType` の変更ログがあれば）を比較する。
- **案O2: 2 回のまま、それぞれを DI 経由へ置換する**
  - 利点: 差分が機械的。欠点: 冗長な書き込みが残る。

#### 決定6: MVVM 移設をどこまでやるか
- **前提**: 旧計画の目的は、`ProfileEditor` のバックエンド操作を `ProfileSettingsViewModel` へ移すこと（案3-A）。ただし 0A.1-5 のとおり、置換先のサービスはほぼ存在する。移設の価値は「UI なしで検証できること」にある。
- **案M-A（推奨）: 二段構え**
  1. 前半: `Global.` 58 件を、既存・追加契約への置換で解消する（機械的で、動作中立）。
  2. 後半: 次の 2 つだけを `ProfileSettingsViewModel`／`SpecialActionsListViewModel` へ移す。
     - 保存の一連の処理（有効アクション名の `ProfileActions` への反映 → `IProfileRepository.SaveProfile`。再適用はしない）。
     - ブランクプロファイル読み込みと LS／RS 補正値の初期化。
  - 8 件の `CacheProfileCustomsFlags` は、VM のメソッド 1 つ（`RefreshCustomsFlags()` 相当）へ集約するかを、実装時の差分を見て決める（決定として再提示しない小さな整理）。
  - 理由: 全体計画の目的（テスト可能性）を満たしつつ、View の見た目・イベントの順序に関わる変更を最小にできる。
- **案M-B: 旧計画どおり、全面的に VM へ移設する**
  - 欠点: `SaveSplitterAndColumnWidths()`（UI 依存）、`specialActionsVM`（別 VM）との結合など、View に残すべきものまで動かす必要があり、Step7b で確定した順序（`UpdateMappingDevType` の呼び出し順など）を崩す危険がある。
- **案M-C: 置換のみ（移設は Step9 以降へ）**
  - 利点: 最小。欠点: 全体計画の「UI なしで保存を検証する」目的が達成されない。

### 0A.4 呼出元 0 件・未使用コードの確認（§2.4）
- **旧計画の B9 シム（`IProfileApplicationService.ApplyProfileToSlot`）**: 本 Step では追加しない（呼び出し元が消えたため）。既存の `IProfileApplicationService` の他のメンバーは触れない。
- 本 Step で新たに見つかった呼出元 0 件のメンバーは、現時点ではない（置換作業中に見つけたら、§2.4 に従い経緯を調べて提案する）。

### 0A.5 マイクロステップ（決定1〜6 の確定により確定）
- **Step8-0（完了）**: 着手前確認、台帳の再作成、決定事項の提示（本節）。
- **Step8-1（完了確定 2026-10-13。ビルド・テストビルド・テスト実行成功、反映済み）**: 契約の追加（B8、B10）とテスト
  - 変更: `DI/ISpecialActionRepository.cs`（`GetActionOrPlaceholder`）、`DS4Control/Services/SpecialActionRepository.cs`、`DI/IProfileRepository.cs`（`LoadBlankProfile`）、`DS4Control/Services/ProfileRepository.cs`、新規テスト `DS4WindowsTests/Phase6Step8ContractExtensionTests.cs`。
  - 既存の `IProfileRepository`／`ISpecialActionRepository` の実装は各 1 件のみ（テスト用の自前実装なし）。`ProfileEditor.xaml.cs` はまだ変更していない（呼び出し側の置換は Step8-2 以降）。
  - `ISpecialActionRepository.GetActionOrPlaceholder`（決定3）、`IProfileRepository.LoadBlankProfile`（決定4）。
  - テスト: 未発見時に placeholder（`"null"` ×4）を返すこと、`Trim` と大文字小文字の無視、`Global.GetAction` と同じ結果であること。`LoadBlankProfile` が `Global.LoadBlankDevProfile` と同じ副作用（既定アクション、一時プロファイルのクリア）を持つこと。
- **Step8-2（完了確定 2026-10-13。ビルド・テスト成功、反映済み。実機確認項目 2・3 のうちスプリッター／列幅の保存・復元とリセットボタンは問題なし）**: レイアウト・言語 23 件の置換（G1。決定2）
  - `ProfileEditor` のコンストラクタを `ProfileEditor(int targetDevice, IAppSettingsService appSettingsService)` にした（必須。`null` は `ArgumentNullException`）。`MainWindow.ShowProfileEditor` の 1 箇所だけが `appSettingsService`（`MainWindow` が保持済み）を渡す。
  - 置換: 列幅 20 式（リセット・保存・保存ログ・復元）、`FormWidth`／`FormHeight`、`UseLang`（コンストラクタのカルチャ設定）。`AppSettingsService` の各プロパティは `Global.*` の get／set をそのまま通すため、読み書きの値は同じ（setter は値が変わったときだけ `SettingChanged` を通知）。
  - テスト: 新規ガード `ProfileEditorGlobalReferenceGuardTests`（解消済みメンバーの再混入と、コンストラクタ注入を検査。以後のステップで `ResolvedMembers` を追加）。`ProfileEditorTargetDeviceGuardTests` の生成呼び出しの文字列を更新。
- **Step8-3（完了確定 2026-10-13。ビルド・テスト・実機確認［プロファイルを開く、New Profile のプリセットキャンセル、Save／Apply］すべて問題なし）**: G2〜G8（35 件）の置換。`ProfileEditor.xaml.cs` の非定数 `Global.` 参照は **0 件**になった（コメント中の言及のみ）。
  - コンストラクタは `ProfileEditor(int targetDevice, IAppSettingsService appSettingsService, IProfileSettingsService profileSettingsService)`（必須。`MainWindow` が保持済みの 2 サービスを渡す）。**計画（決定2）からの追加**: `IProfileSettingsService` を 1 つ増やした。`LSModInfo`／`RSModInfo`／`OutContType` を、置換前と同じ配列・同じタイミングで読み書きするため（`ProfileSettingsViewModel` 経由だと、ViewModel 生成前の `OutConTypeCombo_SelectionChanged` の書き込みが落ちる）。
  - `profileRepository`（コンストラクタで必ず非 null）へ置換: `ProfilePath`（1 件）、`LoadBlankProfile`（B10、1 件）、`ProfileActions`（6 件）、`CacheProfileCustomsFlags`（8 件）、`CacheExtraProfileInfo`（2 件）。`SaveProfile` の `else`（`Global.SaveProfile` へのフォールバック）は削除。
  - `profileSettingsService` へ置換: `LSModInfo`／`RSModInfo`（4 件）、`OutContType`（決定5＝O1。二重書き込みを 1 回に）。
  - ViewModel 経由へ置換: `InverseRumbleMotors`（5 件。`profileSettingsVM.InverseRumbleMotors`、編集スロットの値）、`outDevTypeTemp`（`ProfileSettingsViewModel.ResetOutDevTypeTemp()` を新設）、`GetAction`（2 件。`SpecialActionsListViewModel.GetActionOrPlaceholder` を新設。B8）、`appdatapath`（`SpecialActionsListViewModel.ActionsFilePath` を新設。`ISpecialActionRepository.ActionsPath`）。
  - テスト: `ProfileEditorGlobalReferenceGuardTests.ResolvedMembers` に G2〜G8 のメンバーを追加。コンストラクタ・生成呼び出しの検査文字列を更新（`ProfileEditorTargetDeviceGuardTests` も）。
- **Step8-4（完了確定 2026-10-13。ビルド・テスト・実機確認成功、反映済み）**: 決定6＝M-A の後半（ViewModel への移設）
  - `ProfileSettingsViewModel` に追加: `SaveProfile(profileName)`（編集スロットを保存。再適用はしない）、`LoadBlankProfile(ControlService control)`（呼び出し元の `ControlService` をそのまま渡す。`profileRepo.LoadBlankProfile(device, false, control, false)`）、`RefreshCustomsFlags()`、`RefreshExtraProfileInfo()`。
  - `SpecialActionsListViewModel` に追加: `CommitEnabledActionsToProfile()`（`SyncProfileActionsString()` ＋ `ProfileActions[deviceNum] = GetEnabledActionNames()`。保存時とチェック切り替え時の重複 2 箇所を集約）。
  - `ProfileEditor.xaml.cs`: 上記を呼ぶだけにした。`CacheProfileCustomsFlags` 8 件・`CacheExtraProfileInfo` 2 件は `profileSettingsVM.Refresh*()` へ集約（決定として再提示しない小さな整理）。`profileRepository` の直接利用は `LoadProfile`／`ProfilePath`／`ProfileActions` の参照だけになった。
  - **計画との差**: 「LS／RS 補正値の初期化」は、`Reload` 内の読み取りが軸別スティック設定の画面部品（`axialLSStickControl.UseDevice` 等）と Controller Readings のデッドゾーン表示の初期化＝UI の配線であるため、View に残した（ViewModel に移す価値がない）。
  - テスト: 新規 `ProfileEditorBackendViewModelTests`（`CommitEnabledActionsToProfile` が編集スロットだけに書くことの動作検証［既存テスト `ProfileActions_GetSet_ShouldPersistAndNotify` が `Global.store.profileActions` を 8 要素の配列に置き換えたまま戻さないため、このテストは独立した 9 要素の配列へ差し替えて実行順に依存しないようにした。後始末のない既存テストの 1 件として §6.5 の持ち越しに該当］、ViewModel のメソッドの委譲形と再適用がないことのソース走査、View が repository を直接呼ばないことのガード）。`ProfileEditorTargetDeviceGuardTests` の保存呼び出しの検査文字列を更新。
- **Step8-5（完了 2026-10-13）**: 完了確認と文書更新
  - `ProfileEditor.xaml.cs` の非定数 `Global.` 参照が 0 件（コメント中の言及と定数 37 件のみ）であることをソース走査ガードで固定済み。
  - モデル図 01〜04: 確認のうえ、03（`ProfileSettingsViewModel`・`SpecialActionsSubViewModel` のメンバー・依存と注釈）、04（§2 の責務・依存と注釈）を更新。01・02 は構造変更なしで注釈のみ追記。
  - `MainWindow` のスリム化: 決定2＝L2 により、`MainWindow` は 2 サービスを渡す形になった（バケツリレーの撤廃ではなく、Pure DI の原則を優先）。`MainWindow` の解体は Step9／Step10 の範囲。
  - `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step9-Plan.md`（申し送り）／`Phase6-Step11-Plan.md` §3.3 を更新し、完了報告書を作成。

### 0A.6 実機確認項目（案。`Phase6-Step11-Plan.md` §3.3 の先送り台帳に登録可能なものは登録する）
Step7 の教訓として、各項目に「見るログ」を併記する。

1. **起動と編集画面の表示**: Edit ボタン／一覧のダブルクリックで開く。
   - ログ: `[ProfileEditor] Opened profile editor for device=8, targetDevice=N`、`[ProfileEditor] Reload: editSlot=8, targetDevice=N, profile=…`（Step7b 決定6）。
2. **レイアウトの保存・復元（G1）**: スプリッターと、スペシャルアクションの列幅を変えて閉じ、再度開く。アプリを再起動して再度開く。
   - ログ: `[SaveSplitterAndColumnWidths] Saved left=… right=… nameCol=…`（`App.logHolder` の Debug ログ）の値と、再度開いたときの `[RestoreSplitterAndColumnWidths]` 系の値が一致する。
3. **言語（G1）**: 言語を切り替えて、エディタを開いたときにスペシャルアクションの一覧の見出しが現在の言語になっていること。
4. **新規作成（New Profile）→ ブランク読み込み（G2、B10）**: プリセット選択でキャンセルしたとき、空のプロファイルになり、スペシャルアクションが既定（Disconnect Controller 等）になること。
5. **保存・適用（G3）**: 編集して Save／Apply。Step7b §4.2 と同じ結果（そのプロファイルを使っている全コントローラーに再適用）。`ProfileActions` の変更（スペシャルアクションのチェック）が保存されること。
   - ログ: `[ProfileEditor] Save: profile '名前' saved; …` と `ApplyProfile CALLED: device=…`。
6. **キャンセル（G3）**: 出力種別の一時値が元に戻り、コントローラーに影響しないこと。
   - ログ: `[ProfileEditor] Cancel: edit slot 8 discarded; …`。
7. **ランブルテスト（G4）**: Inverse Rumble Motors をオン／オフして、左右が入れ替わること。
8. **出力コントローラー種別（G5）**: Xbox 360 ⇄ DS4 を切り替えて、ボタン名の表記が追従し、保存後に反映されること（Step6 と同じ）。
9. **スペシャルアクション（G6）**: 新規作成・編集（名前の変更）・削除・チェックの切り替えと、保存後の一覧の復元。名前を変更したとき、プロファイル側の参照が新しい名前になること。
10. **ボタン設定画面を閉じた後のフラグ更新（G7）**: BindingWindow を閉じた後、マッピングの表示・出力が最新になること（カスタムフラグの更新漏れがないこと）。
11. **Actions.xml のエクスポート（G8）**: Special Actions タブのエクスポートで、保存場所の `Actions.xml` が複製されること。AppData とプログラムフォルダのどちらの保存場所でも確認する（保存場所が 2 つある環境がなければ、片方のみとし、もう一方を Step11 の台帳へ登録）。
12. **一覧経由の編集の回帰（Step7b §4.6）**: 一覧の Edit／New で開き、編集・保存できる。ライトバーのプレビューは従来どおり動かない。

### 0A.7 リスクと対策（追加分）
- **順序の崩れ**: `Reload` 内の `LSModInfo`／`ProfileActions` の取得順、`UpdateMappingDevType`（`UpdateLateProperties()` の直後）を変えない。`ProfileEditorMappingDevTypeGuardTests` を維持する。
- **フォールバックの削除**: `Global.SaveProfile` の `else` を消しても、`profileRepository` が null にならないこと（コンストラクタの `?? new ProfileRepository()`）を、テストまたはソース走査で確認する。
- **B8 の `null` と placeholder の取り違え**: 置換後に `null` が返ると `CreateActionItem(null)` で例外になる。Step8-1 のテストで固定する。
- **Step9 との境界**: `ProfileSettingsViewModel` の `Global.` 19 件・`?? Global.…` フォールバックは Step9。Step8 では触れない。
- **Step10b**: `ProfileEditor.xaml.cs` には `HoverImageInfo`、`RumbleType`、`ResourcePaths`、`ControlIndexCheck` などの複数型がある。分割は Step10b で行い、本 Step で型を追加しない（§3.3 原則1）。

### 0A.8 完了判定チェックリスト（現行、2026-10-13 完了）
- [x] `ProfileEditor.xaml.cs` の非定数 `Global.` 参照が 0 件（58 件を解消。`ProfileEditorGlobalReferenceGuardTests`）
- [x] B8（`ISpecialActionRepository.GetActionOrPlaceholder`）、B10（`IProfileRepository.LoadBlankProfile`）が追加され、`Global` と同じ契約であることをテストで固定（`Phase6Step8ContractExtensionTests`）。B3 は Step7-1 で追加済み。B9 は呼び出し元が消えたため作らない
- [x] 保存の一連・ブランク読み込み・キャッシュ更新が ViewModel のメソッドになり、UI なしで検証できる（`ProfileEditorBackendViewModelTests`）。再適用は `ProfileSaved` を受けた `MainWindow` のまま（Step7b 決定8）
- [x] `ProfileEditorMappingDevTypeGuardTests`・`ProfileEditorTargetDeviceGuardTests` を含む既存テストと新規テストが全件成功（`dotnet build`、`dotnet test`）
- [x] 実機確認（レイアウト保存・復元、プロファイルを開く、New Profile のプリセットキャンセル、Save／Apply、スペシャルアクションの保存）問題なし
- [x] 先送り（Step11 §3.3）: Actions.xml のエクスポートを、保存場所が 2 つある環境で確認していない場合のみ登録（0A.6-11）
- [x] 文書更新（Status／Plan／Step9 申し送り／Step11 台帳）、完了報告書の作成

---

## 0. 背景と改訂の経緯

### 0.1 暫定18件から全98箇所への精査と課題の所在
旧計画書（2026-09-11）では、「暫定18件の調査」とされていたが、2026-09-18に実施された Step1 再監査（`Phase6-Step1-UI-Reference-Evidence.md` §2）において、`ProfileEditor.xaml.cs`（約2,500行）内の Global 参照の全容が精査され、以下の通り確定した：

- **真の定数除外（const）: 計38箇所**  
  `TEST_PROFILE_INDEX` 4件、`ASSEMBLY_RESOURCE_PREFIX` 29件、`RESOURCES_PREFIX` 5件。
- **実移行対象（実参照式）: 計60箇所**  
  UI列幅・ウィンドウサイズ（22箇所）、プロファイルパス・ブランク初期化（6箇所）、アクション・キャッシュ管理（9箇所）、プロファイル保存・スロット適用・ランブル設定（11箇所）、各タブのカスタムフラグ更新（10箇所）、言語・エクスポートパス（2箇所）。

### 0.2 方針の確定: 【案3-A: 段階的MVVM移設（Fat View 解体 ＋ MainWindow スリム化）の採用】
従来の「View のコードビハインドにサービスを直接注入または都度解決する」方式（選択肢1や選択肢2）では、以下の構造的破綻が発生する：
1. **`MainWindow.xaml.cs` の解体が阻害される**:  
   `MainWindow` が `ProfileEditor` に 7〜8 個もの DI サービスを手動で横流しする「バケツリレーのハブ」となり、すでに肥大化している `MainWindow.xaml.cs` のコードがさらに増大してしまう。
2. **Fat View / Smart UI アンチパターンの温存**:  
   XAML のコードビハインド（`ProfileEditor.xaml.cs`）が、ファイルの保存、スロットへのプロファイル適用、XMLエクスポートといったバックエンドのビジネスロジックを直接実行し続けるため、GUI を起動しなければ単体テストができない不健全な構造が残る。

本ステップでは、4層アーキテクチャモデル（`02-Layer-Architecture-Diagram.md`）の理想形に従い、**バックエンド操作ロジックを `ProfileSettingsViewModel` へ移設し、View は純粋な描画と ViewModel の呼び出しだけに特化させる「案3-A（段階的MVVM移設）」**を採用する。  
これにより、`MainWindow.xaml.cs` の解体をダイレクトに推進し、プロファイル編集機能全体のテスタビリティを飛躍的に向上させる。

---

## 1. 目的

1. `ProfileEditor.xaml.cs` 内のバックエンド操作ロジック（保存、スロット適用、ブランクプロファイル読込、キャッシュ更新等）を `ProfileSettingsViewModel` へ移設し、View を「Dumb View（薄いView）」へ純化する。
2. `MainWindow.xaml.cs` がエディタに対してサービスをバケツリレーする必要を無くし、`MainWindow` の肥大化したコードの解体・スリム化を前進させる。
3. Step1 で判明した重要契約差（B8: `GetActionOrPlaceholder`、B9: `ApplyProfileToSlot`、B10: `LoadBlankDevProfile`）を薄いシムで完全に吸収し、保存・適用時のリグレッションをゼロにする。
4. `ProfileEditor.xaml.cs` 内の Global 直参照（全60箇所）を完全に根絶する。
5. UI を起動することなく、ViewModel 単体で「プロファイルの保存・スロット適用」を検証できる自動単体テストを確立する。

---

## 2. 全件参照台帳（全60箇所・完全カタログ） 【旧版・2026-09-18。現行は §0A】

### 表1: 実移行対象台帳（ID: C8-01 〜 C8-60、計60箇所）

| ID | 行番号 | 参照メンバ | 現行の役割 | 案3-A における移行先・設計方針 | 分類 |
|---|---|---|---|---|---|
| C8-01〜C8-20 | 340〜419 | `ProfileEditorLeftWidth`, `ProfileEditorRightWidth`, `SpecialAction*ColWidth`（各4箇所、計20式） | スプリッター・カラム幅の保存・復元 | `_appSettingsService.*`（View のレイアウト設定として直接参照） | (a) |
| C8-21〜C8-22 | 348, 349 | `FormWidth`, `FormHeight` | エディタウィンドウサイズ | `_appSettingsService.FormWidth / Height` | (a) |
| C8-23 | 284 | `UseLang` | 表示言語 | `_appSettingsService.UseLang`（B3シム） | (b) |
| C8-24 | 2453 | `appdatapath` | Actions.xml のエクスポート元パス | `ProfileSettingsViewModel.ExportActionsXml()` 内で `_pathService.AppDataPath` 参照 | (b) |
| C8-25〜C8-27 | 1113, 1280, 1284 | `ProfilePath[device]` | プロファイルファイル名取得 | `ProfileSettingsViewModel` のプロパティへ集約 | (a) |
| C8-28 | 1128 | `LoadBlankDevProfile` | 初期化時のブランク読込 | `ProfileSettingsViewModel.LoadBlankProfile()` へ移設（B10シム呼び出し） | (b) |
| C8-29〜C8-32 | 1155〜1182 | `LSModInfo`, `RSModInfo`（各2箇所） | デッドゾーン等の初期化 | `ProfileSettingsViewModel.LoadBlankProfile()` 内へ集約 | (a) |
| C8-33〜C8-37 | 1133〜1940 | `ProfileActions`（5箇所） | プロファイルアクション操作 | `ProfileSettingsViewModel` のプロパティ・コレクション操作へ集約 | (a) |
| C8-38〜C8-39 | 1829, 1895 | `GetAction`（2箇所） | アクション名正規化・取得 | `ISpecialActionRepository.GetActionOrPlaceholder`（B8シム）へ委譲 | (b) |
| C8-40〜C8-41 | 1884, 2054 | `CacheExtraProfileInfo`（2箇所） | 拡張プロファイル情報キャッシュ | `ProfileSettingsViewModel.SaveProfile()` / 更新メソッド内へ集約 | (a) |
| C8-42 | 1271 | `outDevTypeTemp` | 出力デバイス一時選択値 | `_outputSlotService.OutDevTypeTemp`（Step5三態SSOT連動） | (a) |
| C8-43 | 1407 | `SaveProfile` | プロファイルXML保存実行 | `ProfileSettingsViewModel.SaveProfile()` へ移設 | (a) |
| C8-44 | 1419 | `ApplyProfileToSlot` | スロットへの動的適用 | `ProfileSettingsViewModel.SaveProfile()` 内で B9 シム呼び出し | (b) |
| C8-45 | 1797 | `OutContType` | 出力コントローラー種別 | `_profileSettings.OutContType`（Step5三態SSOT連動） | (a) |
| C8-46〜C8-47 | 1798, 1800 | `ProfileSettingsServiceInstance` | プロファイルサービス直接参照 | ViewModel 注入済み `_profileSettings` へ統一 | (a) |
| C8-48〜C8-52 | 1528〜1600 | `InverseRumbleMotors`（5箇所） | ランブル反転フラグ | `ProfileSettingsViewModel` のプロパティへバインディング集約 | (a) |
| C8-53〜C8-60 | 1299〜2414 | `CacheProfileCustomsFlags`（8箇所） | タブ切替時のフラグ更新 | `ProfileSettingsViewModel.UpdateCustomsFlags()` へ集約移設 | (a) |

---

### 表2: 明示的除外台帳（ID: C8-EX01 〜 C8-EX03、計38箇所）

| ID | 行番号 | 参照メンバ | 出現数 | 除外理由（根拠） |
|---|---|---|---|---|
| C8-EX01 | 1111, 1113, 1141, 1150 | `Global.TEST_PROFILE_INDEX` | 4 | 真の定数（`const int`、4層モデル共通値） |
| C8-EX02 | 940〜1055 | `Global.ASSEMBLY_RESOURCE_PREFIX` | 29 | 真の定数（`const string`、WPF リソース URI） |
| C8-EX03 | 2484〜2488 | `Global.RESOURCES_PREFIX` | 5 | 真の定数（`const string`、WPF パック URI） |

---

## 3. アーキテクチャ設計: 案3-A（MVVM 移設と契約差吸収） 【旧版・2026-09-18。現行は §0A】

### 3.1 `ProfileSettingsViewModel` への責務集約設計
バックエンド操作およびデータ永続化の責務を、View から `ProfileSettingsViewModel` へ移設する。

```csharp
// DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs への追加設計
public partial class ProfileSettingsViewModel : INotifyPropertyChanged
{
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileApplicationService _profileApplicationService;
    private readonly ISpecialActionRepository _specialActionRepository;
    private readonly IPathService _pathService;

    // C8-43, C8-44, C8-40, C8-53: プロファイル保存・適用・キャッシュ更新の一元処理
    public bool SaveProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return false;

        // 1. プロファイルXMLの保存
        bool success = _profileRepository.SaveProfile(DeviceIndex, profileName);
        if (!success) return false;

        // 2. スロットへの即時適用（B9シム: Halt停止・上限値保証）
        _profileApplicationService.ApplyProfileToSlot(DeviceIndex, true);

        // 3. キャッシュフラグの更新
        _profileRepository.CacheProfileCustomsFlags(DeviceIndex);
        _profileRepository.CacheExtraProfileInfo(DeviceIndex);

        return true;
    }

    // C8-28, C8-29〜32: ブランクプロファイル読込と初期化の一元処理
    public void LoadBlankProfile()
    {
        // B10シムの呼び出し
        _profileRepository.LoadBlankDevProfile(DeviceIndex);
        
        // 内部設定プロパティのリセット通知
        RefreshAllProperties();
    }

    // C8-24: アクションエクスポートの一元処理
    public void ExportActionsXml(string destinationFilePath)
    {
        string sourcePath = Path.Combine(_pathService.AppDataPath, "Actions.xml");
        File.Copy(sourcePath, destinationFilePath, true);
    }
}
```

### 3.2 `ProfileEditor.xaml.cs` の「Dumb View 化」設計
View 内の巨大な直接操作コードを、ViewModel のメソッド呼び出し1行に置き換える。

```csharp
// DS4Windows/DS4Forms/ProfileEditor.xaml.cs: 保存ボタンハンドラ等
private void SaveButton_Click(object sender, RoutedEventArgs e)
{
    // 変更前（Fat View）:
    // 数十行に及ぶ Global.* 直接操作、Halt制御、キャッシュフラグ直接代入
    
    // 変更後（Dumb View: 案3-A）:
    bool result = profileSettingsVM.SaveProfile(txtProfileName.Text);
    if (result)
    {
        // UI 表示更新（ステータス表示など）のみを実行
    }
}
```

### 3.3 重要契約差（B8, B9, B10）の薄いシム設計
Step1 で特定された意味差・契約差を確実に吸収するシムを各サービスに実装する：

1. **`IProfileApplicationService.ApplyProfileToSlot`（B9シム）**:
   ```csharp
   // Services/ProfileApplicationService.cs
   public void ApplyProfileToSlot(int deviceIndex, bool notify = true)
   {
       // Global.ApplyProfileToSlot の挙動（Halt停止、CURRENT_DS4_CONTROLLER_LIMIT上限、ログ記録）を完全維持して実行
       Global.ApplyProfileToSlot(deviceIndex, notify);
   }
   ```
2. **`ISpecialActionRepository.GetActionOrPlaceholder`（B8シム）**:
   ```csharp
   // Services/SpecialActionRepository.cs
   public SpecialAction GetActionOrPlaceholder(string actionName)
   {
       // 名前正規化と、未発見時に placeholder（"null"）を返す互換動作を保証
       return Global.GetAction(actionName);
   }
   ```
3. **`IProfileRepository.LoadBlankDevProfile`（B10シム）**:
   ```csharp
   // Services/ProfileRepository.cs
   public void LoadBlankDevProfile(int deviceIndex)
   {
       Global.LoadBlankDevProfile(deviceIndex);
   }
   ```

### 3.4 `MainWindow.xaml.cs` 解体への効果
`ProfileEditor` の生成において、`MainWindow.xaml.cs` はサービスインスタンスを横流しする必要がなくなり、単に生成済みの `profileSettingsVM` をバインドして View を開くだけになる。  
これにより、`MainWindow.xaml.cs` 内の肥大化したコードが劇的に削ぎ落とされ、解体が大きく前進する。

---

## 4. PR分割計画とマイクロステップ 【旧版・2026-09-18。現行は §0A】

変更規模とリスクを完全に制御するため、以下の**5つのサブPR（マイクロステップ）**に分割して実装を進める。

```text
【Phase6-Step8 マイクロステップ構成】
├─ Step8-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step8-1 (PR-1): 契約差吸収シム（B3, B8, B9, B10）のサービス追加
├─ Step8-2 (PR-2): View レイアウト・列幅・ウィンドウサイズの是正（C8-01〜C8-23）[23箇所]
├─ Step8-3 (PR-3): 初期化・ブランク読込ロジックの ViewModel 移設（C8-25〜C8-32）[8箇所]
├─ Step8-4 (PR-4): 保存・適用・キャッシュ更新ロジックの ViewModel 移設（C8-33〜C8-60）[29箇所]
└─ Step8-5 (PR-5): MainWindow 連携スリム化・全体回帰検証・完了報告書作成
```

---

### Step8-0: 着手前提検証・ベースライン記録（実装なし）
1. Step7 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step8-1 (PR-1): 契約差吸収シムのサービス追加
- **対象**: `IProfileApplicationService`, `ISpecialActionRepository`, `IProfileRepository`, `IAppSettingsService`
- **作業内容**:
  1. §3.3 に定義された B3 (`UseLang`), B8 (`GetActionOrPlaceholder`), B9 (`ApplyProfileToSlot`), B10 (`LoadBlankDevProfile`) を追加。
- **検証**:
  - 単体テスト: 追加したシムが正本ロジックと連動していることを検証。
  - `dotnet test` 全件合格を確認。

---

### Step8-2 (PR-2): View レイアウト・列幅・ウィンドウサイズの是正
- **対象**: `ProfileEditor.xaml.cs`（C8-01 〜 C8-23、計23箇所）
- **作業内容**:
  1. `ProfileEditorLeftWidth`, `ProfileEditorRightWidth`, `SpecialAction*ColWidth`, `FormWidth`, `FormHeight` を `_appSettingsService` 経由へ置換。
  2. 言語設定 `UseLang`（B3シム）を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - エディタ画面の開閉で列幅・ウィンドウサイズが維持されることを確認。

---

### Step8-3 (PR-3): 初期化・ブランク読込ロジックの ViewModel 移設
- **対象**: `ProfileEditor.xaml.cs`, `ProfileSettingsViewModel.cs`（C8-25 〜 C8-32、計8箇所）
- **作業内容**:
  1. `ProfileSettingsViewModel` に `LoadBlankProfile()` メソッドを新設。
  2. `ProfileEditor.xaml.cs` 側の直接呼び出し（`LoadBlankDevProfile`, `LSModInfo`, `RSModInfo` 等）を ViewModel 呼び出しに置き換え。
- **検証**:
  - 単体テスト: `LoadBlankProfile()` の呼び出しで各種モディファイアが初期化されることを検証。
  - `dotnet test` 全件合格を確認。

---

### Step8-4 (PR-4): 保存・適用・キャッシュ更新ロジックの ViewModel 移設
- **対象**: `ProfileEditor.xaml.cs`, `ProfileSettingsViewModel.cs`（C8-33 〜 C8-60、計29箇所）
- **作業内容**:
  1. `ProfileSettingsViewModel` に `SaveProfile()`, `ExportActionsXml()` を新設。
  2. `ProfileEditor.xaml.cs` 内の `SaveProfile`, `ApplyProfileToSlot`, `CacheProfileCustomsFlags`, `CacheExtraProfileInfo`, `ProfileActions`, `InverseRumbleMotors` 直接操作を ViewModel へ集約。
  3. View 側の該当コードを ViewModel メソッド呼び出し1行に置き換え。
- **検証**:
  - 単体テスト: UI なしで `SaveProfile` が実行され、保存・スロット適用・キャッシュ更新が走ることをモック検証。
  - `dotnet test` 全件合格を確認。

---

### Step8-5 (PR-5): MainWindow 連携スリム化・全体回帰検証・完了報告
- **作業内容**:
  1. `MainWindow.xaml.cs` 側の ProfileEditor 起動コードにおける不要な横流し処理を整理・スリム化。
  2. `ProfileEditor.xaml.cs` 内の Global 直参照がゼロ（除外const 38件を除く）であることを確認。
  3. 完了報告書（`Phase6-Step8-Completion-Report.md`）を作成。
- **検証**:
  - プロファイルの新規作成、編集、保存、実機コントローラーへの即時反映を手動確認。
  - 全自動テスト合格を確認。

---

## 5. テスト・実機動作検証計画 【旧版・2026-09-18。現行は §0A】

### 5.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストクラスを追加する（1ファイル1型）：
1. **`ProfileSettingsViewModelMvvmTests.cs`**:
   - `profileSettingsVM.SaveProfile("TestProfile")` を呼び出した際、`IProfileRepository.SaveProfile`、`IProfileApplicationService.ApplyProfileToSlot`、および各種キャッシュフラグ更新が正しい順序で呼び出されること。
   - `profileSettingsVM.LoadBlankProfile()` を呼び出した際、`IProfileRepository.LoadBlankDevProfile` が実行されること。
2. **`ProfileEditorShimEquivalenceTests.cs`**:
   - B8 (`GetActionOrPlaceholder`), B9 (`ApplyProfileToSlot`), B10 (`LoadBlankDevProfile`) のシムが期待通りの契約（未発見時 placeholder、Halt停止等）を充足すること。

### 5.2 実機・UI動作検証手順（Phase6-Step11連携）
1. **プロファイルの編集と保存**:
   - ProfileEditor を開き、ボタン割り当てやスティック感度を変更して「Save」を実行。
   - プロファイル XML が正常に更新され、接続中のコントローラーに設定が即座に反映されること。
2. **ブランクプロファイルの読み込み**:
   - 「New」または初期化操作を行い、すべての設定が安全にデフォルト値に戻ること。
3. **エディタのレイアウト維持**:
   - エディタのスプリッター（左右幅）やカラム幅を変更してアプリを再起動し、サイズが正常に復元されること。

---

## 6. ロールバック方針

プロファイル保存や適用で不整合が発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。

---

## 7. 完了判定チェックリスト 【旧版・2026-09-18。現行は §0A.8】

- [ ] `ProfileEditor.xaml.cs` 内のバックエンド操作ロジックが `ProfileSettingsViewModel` へ移設されていること。
- [ ] 表1に定義された全60箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 表2に定義された除外項目（const定数38箇所）以外の Global 直接参照が `ProfileEditor.xaml.cs` 内に存在しないこと。
- [ ] 重要契約差（B8, B9, B10）の薄いシムが実装され、プロファイル保存・適用の挙動が完全に保護されていること。
- [ ] `MainWindow.xaml.cs` からのエディタ呼び出しコードがスリム化され、MainWindow 解体が前進していること。
- [ ] UI を起動せずに `ProfileSettingsViewModel.SaveProfile()` を検証する単体テストが追加され、合格していること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーを用いたプロファイル編集・保存・即時反映が正常に動作すること。