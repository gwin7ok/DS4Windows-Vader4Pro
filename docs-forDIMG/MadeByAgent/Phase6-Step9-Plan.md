# Phase6-Step9 計画書: 主要4大ViewModelのPure DI徹底とカテゴリ別段階的移行

作成日: 2026-09-11  
改訂日: 2026-10-13（Step9-0 着手前再確認。冒頭に §0A［現行の正］を追加。§0〜§8 は 2026-09-18 版の旧記載として残す）。2026-09-18（Phase6-Step1 再監査エビデンスに基づく推奨案［主要4大ViewModel集中 ＋ カテゴリ別段階的移行・Pure DI徹底］採用・全面拡充改訂）  
状態: **完了（2026-10-14）**。Step9-0〜9-5（3c・3d を追加）。完了報告書 `Phase6-Step9-Completion-Report.md`。次は Step10  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §3（ViewModel層 実地監査記録・全79箇所）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類・シム台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase6-Step8-Plan.md`（案3-A ProfileSettingsViewModel 拡充計画）  

---

> **Step8 からの申し送り（2026-10-13 追記。Step9-0 で必ず確認すること）**
> - **ProfileSettingsViewModel の台帳は作り直しが必要**: Step8 で `ProfileSettingsViewModel.cs` に `SaveProfile`・`LoadBlankProfile`・`RefreshCustomsFlags`・`RefreshExtraProfileInfo`・`ResetOutDevTypeTemp` を追加し、行番号が変わった。本書の表4（C9-P01〜P10、実参照 10 箇所）は使えない。2026-10-13 時点の現行コードでは、定数・コメントを除く `Global.` 参照は 11 箇所（`IsUsingMinViGEm117333`、`ProfileSettingsServiceInstance` 2 件［コンストラクタ 2 箇所］、`OutputSlotServiceInstance`、`ProfileRepositoryInstance`、`exedirpath` 3 件、`defaultButtonMapping`、`RefreshActionAlias`）。
> - **Step8 で追加した ViewModel メソッドは `profileRepo`／`outputSlotService` のみを使い、新しい `Global.` 参照を増やしていない**。Step9 の「コンストラクタ注入済みのフィールドへ一本化」の対象はそのまま。
> - **`ProfileEditor` のコンストラクタ**は `ProfileEditor(int targetDevice, IAppSettingsService, IProfileSettingsService)`。ViewModel を Pure DI 化してファクトリの契約（`IViewModelFactory`）を変える場合は、`ProfileEditor` の生成側（`MainWindow.ShowProfileEditor`）とモデル図 03・04 も確認すること。
> - **`SpecialActionsListViewModel`**（`Global.GetActions()`、`Global.RemoveAction`）、`MappingListViewModel` の `Global.` 参照は Step10 のグループ2・3 の範囲（Step8 では触れていない）。
> - 詳細は `Phase6-Step8-Plan.md` §0A、`Phase6-Step8-Completion-Report.md`。

---

## 0A. 着手前再確認（Step9-0、2026-10-13、HEAD `f13d5c7c`）

> **本節が現行の正**。§0〜§8（旧台帳 C9-S／W／T／P、旧設計、旧マイクロステップ、旧テスト計画）は 2026-09-18 版で、現行コードと合わない記述を多く含むため、下記 0A.1〜0A.8 で置き換える（旧記載は経緯として残す）。決定事項（0A.3）はユーザー確認後に確定する。

### 0A.1 旧計画との乖離（現行コードとの突き合わせ結果）
1. **件数**: 旧 79 件 → 現行 **114 件**（`Global.` 参照式の出現数。コメント内の言及と定数を除く）。内訳は `SettingsViewModel` 89、`MainWindowsViewModel` 9、`TrayIconViewModel` 6、`ProfileSettingsViewModel` 10。旧計画は同じ行の複数参照を 1 件と数えていたため、実数より小さい。定数は別に `TrayIconViewModel` 12（`RESOURCES_PREFIX`）、`ProfileSettingsViewModel` 1（`ASSEMBLY_RESOURCE_PREFIX`）で、いずれも対象外。
2. **`SettingsViewModel` の旧台帳（C9-S01〜S47）は現行コードと別物**。旧台帳は `Global.getUseExclusiveMode()` のようなメソッドや、`_appSettingsService`／`_environmentService`／`_pathService`／`_appearanceSettingsService`／`_profileSettings` のフィールドを前提にしていたが、現行は `Global.UseExclusiveMode` のようなプロパティ参照で、フィールドは `_appSettings`（`IAppSettingsService`）の 1 つだけ（`Notifications`／`StartMinimized`／`MinimizeToTaskbar`／`CloseMinimizes`／`Save` に使用済み）。旧 C9-S46（`ProfileSettingsServiceInstance`）、C9-S47（`TouchSensitivity`）に対応する参照は存在しない。ファイルは 679 行で、旧台帳の行番号（〜661）は別の箇所を指す。
3. **`ProfileSettingsViewModel` は 10 件**（Step8 申し送りの「11」は、箇所の内訳を足すと 10 で、grep の結果も 10）。旧 C9-P10「残存フォールバック代入」は、コンストラクタの `?? Global.*Instance`（旧 P02〜P04）と同じもので、独立した 1 件ではない。旧 P08 の `_profileSettings.DefaultButtonMapping` は存在しない（実際は `IProfileSettingsService.GetDefaultButtonMapping()` で、呼び出しごとに配列を複製して返す）。旧 P09 の `ISpecialActionRepository.RefreshActionAlias` も存在しない（B6 シムは作られていない）。
4. **契約はほぼ揃っている**: 旧計画の B3・B5・B12 は、Step2 までに大半が追加済み。不足は次のとおり（0A.2 の合計に対応）。
   - `IAppSettingsService`: `UseCustomSteamFolder`、`CustomSteamFolder`、`FakeExeName`、`LogMaxArchiveFiles` の 4 つ。
   - `IEnvironmentService`: `IsViGEmBusInstalled()`、`IsRunningSupportedViGEmBus()`、`IsUsingMinViGEm117333()`、`AdminNeeded()` の 4 つ。
   - プログラムフォルダの参照（決定3）、バッテリー変化の購読（決定4）、`RefreshActionAlias`（決定5）。
5. **`IPathService.ExecutableDirectory` は `Global.exedirpath` と値が異なりうる**。前者は `AppContext.BaseDirectory`（末尾にパス区切りが付く）、後者は「ジャンクション解決済みの実行ファイルの親フォルダ」（`Directory.GetParent(exelocation).FullName`）。Step7 の `App.xaml.cs` も同じ理由で `Path.GetDirectoryName(IPathService.ExecutablePath)` を使った。`exedirpath` を `ExecutableDirectory` へそのまま置換すると、動作が変わる（決定3）。
6. **`MainWindowsViewModel` にコンストラクタはない**。DI には Singleton（既定コンストラクタ）で登録され、`MainWindow.xaml.cs:125` が解決し、null のときだけ `new MainWindowsViewModel()`（129 行）にフォールバックする。モデル図 04 §2 の依存記述（`IAppSettingsService`、`ControlService`）は現行と異なる（どちらも使っていない）。
7. **`MainWindowsViewModel` の `Global.` 9 件のうち 6 件は、呼出元 0 件のメソッド内にある**（0A.4。§2.4 の対象）。
8. **`TrayIconViewModel`**: コンストラクタは `(ControlService service, ProfileList profileListHolder)`、生成は `MainWindow.xaml.cs:160` の 1 箇所。`trayTitle` は `public static` の初期化子（`Global.exeversion`）で、利用者は同じクラスの 331 行だけ（外部の参照なし）。
9. **`ProfileSettingsViewModel` の生成側**:
   - `ViewModelFactory.CreateProfileSettingsViewModel` は `profileSettings` と `targetDevice` だけを渡す。`controlService`／`outputSlotService`／`profileRepo` はコンストラクタ内の `Program.rootHub`／`AppHost.GetService`／`Global.*Instance` で補っている。
   - `ProfileEditor.xaml.cs:319` に、ファクトリが null のときのレガシーフォールバック `new ProfileSettingsViewModel(deviceNum, targetDevice: targetDevice)` がある（`ProfileEditorTargetDeviceGuardTests` が `new ProfileSettingsViewModel(` の出現数 1 を検査している）。
   - `PresetMenuHelper`（同じファイルの 3678 行）のコンストラクタも `?? Global.ProfileSettingsServiceInstance` を持つ。
   - `controlService` は、ジャイロ系の 4 つの setter（2567、2577、2601、2780 行）が `IProfileSettingsService` へ渡す引数としてだけ使われる。
10. **`Global` 以外の静的結合**（件数 114 には含まれない）:
    - `SettingsViewModel`: コンストラクタの `AppHost.GetService`（404 行）、`App.logHolder`（652〜671 行、5 箇所）、`StartupMethods.*`（静的クラス。コンストラクタと変更ハンドラ）、`Util.GetHidHideClientPath()`（382 行）。
    - `MainWindowsViewModel`: `App.requestClient`（54、97 行。0A.4 の対象メソッド内）。
    - `TrayIconViewModel`: `App.Current.Dispatcher`（139 行）。
    - `ProfileSettingsViewModel`: `Program.rootHub`（2932 行）、`AppHost.GetService` 2 件（2933、2934 行）。
11. **`MainWindow.xaml.cs` の生成箇所**（Step9 で呼び出しを合わせる必要がある）: `new SettingsViewModel()`（134 行）、`new TrayIconViewModel(controlService, profileListHolder)`（160 行）、`new MainWindowsViewModel()`（129 行、フォールバック）。いずれも `MainWindow` が保持済みのサービス（`appSettingsService`、`appearanceSettingsService`、`environmentService`、`pathService`、`profileRepo`、`controlService`）で足りる。

### 0A.2 再確認した台帳（現行コード、実移行対象 114 件）
矢印の右は置換先（案）。「既存」は現行の契約に既にあるもの、「新規」は追加が必要なもの。

#### 表A. `SettingsViewModel.cs`（89 件、行番号は 2026-10-13 時点）
- **A1. 基本設定・外観（20 件、既存）**
  - `UseExclusiveMode`（45、46）、`SwipeProfiles`（51、52）、`ProfileChangedNotification`（116、117）、`DCBTatStop`（126×2）、`FlashWhenLate`（127×2）、`FlashWhenLateAt`（128×2）、`QuickCharge`（132×2）→ `IAppSettingsService`（既存）
  - `UseIconChoice`（136、139、141）、`UseCurrentTheme`（149、152、154）→ `IAppearanceSettingsService`（既存）
- **A2. 更新確認（10 件、既存）**
  - `CheckUpdateStartupEnabled`（164、167、189、612）、`CheckEveryValue`（181、194、196）、`CheckEveryUnit`（215、407、615）→ `IAppSettingsService`（既存）
- **A3. OSC（14 件、既存）**
  - `isUsingOSCServer`／`setUsingOSCServer`（225、228、229）、`getOSCServerPortNum`／`setOSCServerPort`（234）、`isInterpretingOscMonitoring`／`setInterpretingOscMonitoring`（236）、`isUsingOSCSender`／`setUsingOSCSender`（240、243、244）、`getOSCSenderPortNum`／`setOSCSenderPort`（249）、`getOSCSenderAddress`／`setOSCSenderAddress`（253、254）→ `IAppSettingsService.UseOscServer`／`OscServerPort`／`InterpretingOscMonitoring`／`UseOscSender`／`OscSenderPort`／`OscSenderAddress`（既存）
- **A4. UDP と平滑化（18 件、既存）**
  - `isUsingUDPServer`（260、263、292）、`setUsingUDPServer`（264）、`getUDPServerListenAddress`／`set`（272、273）、`getUDPServerPortNum`／`set`（275）→ `IAppSettingsService.UseUdpServer`／`UdpServerListenAddress`／`UdpServerPort`（既存）
  - `UseUDPSeverSmoothing`（279、282、284、292）、`UDPServerSmoothingMincutoff`（298、301、303）、`UDPServerSmoothingBeta`（311、314、316）→ `IAppSettingsService.UseUdpServerSmoothing`／`UDPServerSmoothingMincutoff`／`UDPServerSmoothingBeta`（既存）
- **A5. Steam・偽装 exe・HidHide・優先度（11 件）**
  - `UseCustomSteamFolder`（324、327）、`CustomSteamFolder`（335、338、342）、`FakeExeName`（361、364、366）→ `IAppSettingsService`（**新規 4 メンバーのうち 3**）
  - `hidHideInstalled`（381）→ `IEnvironmentService.HidHideInstalled`（既存）
  - `ProcessPriority`（391、394）→ `IAppSettingsService.ProcessPriority`（既存）
- **A6. パス（9 件）**
  - `exedirpath`（516、517、518、625、629、630）→ 決定3
  - `exelocation`（626、627、632）→ `IPathService.ExecutablePath`（既存）
- **A7. 環境（1 件、既存）**: `IsAdministrator()`（442）→ `IEnvironmentService.IsAdministrator()`
- **A8. ログ（6 件）**
  - `LogMinLevel`（663、666、668）→ `IAppSettingsService.LogMinLevel`（既存）
  - `LogMaxArchiveFiles`（646、649、651）→ `IAppSettingsService`（**新規 4 メンバーのうち 1**）
- 合計 20＋10＋14＋18＋11＋9＋1＋6＝**89 件**（grep の確認値と一致）。既存の契約への置換で足りるのは 69 件、新規メンバーが要るのは 11 件、決定3 に依存するのは 9 件。

#### 表B. `MainWindowsViewModel.cs`（9 件）
- 呼出元あり（`CheckDrivers`、`MainWindow.xaml.cs:231` が呼ぶ。3 件）
  - `IsViGEmBusInstalled()`（123）→ `IEnvironmentService`（**新規**）
  - `IsRunningSupportedViGEmBus()`（124）→ `IEnvironmentService`（**新規**）
  - `exelocation`（127）→ `IPathService.ExecutablePath`（既存）
- 呼出元 0 件のメソッド内（6 件。0A.4）
  - `RunUpdaterCheck`: `exedirpath`（71）
  - `DownloadUpstreamVersionInfo`: `appdatapath`（93）
  - `LauchDS4Updater`: `exedirpath`（146）、`IsAdministrator()`（147）、`exeFileName`（157）、`AdminNeeded()`（160）

#### 表C. `TrayIconViewModel.cs`（6 件）
- `exeversion`（35、`public static` の初期化子）→ `IEnvironmentService.ApplicationVersion`（既存。インスタンスの値にする。小さな整理として扱う）
- `iconChoiceResources[UseIconChoice]`（98）→ `IAppearanceSettingsService.GetIconResourcePath(UseIconChoice)`（既存）。`UseIconChoice` も同じサービス（既存）
- `BatteryChanged`（99）→ 決定4
- `ProfilePath[idx]`（199）→ `IProfileRepository.ProfilePath`（既存）
- `exedirpath`（255）→ 決定3
- 除外（定数）: `RESOURCES_PREFIX`（447〜458、12 件）

#### 表D. `ProfileSettingsViewModel.cs`（10 件）と `PresetMenuHelper`
- `IsUsingMinViGEm117333()`（2913）→ `IEnvironmentService`（**新規**）
- コンストラクタのフォールバック（2931、2933、2934。`PresetMenuHelper` の 3681）→ 決定1（`ProfileSettingsServiceInstance`×2、`OutputSlotServiceInstance`、`ProfileRepositoryInstance`）
- `exedirpath`（3599、3602、3611）→ 決定3
- `defaultButtonMapping`（4093）→ `IProfileSettingsService.GetDefaultButtonMapping()`（既存。複製を返すため、ループの前に 1 回だけ取得する）
- `RefreshActionAlias`（4097）→ 決定5
- 除外（定数）: `ASSEMBLY_RESOURCE_PREFIX`（2952）

#### 合計
- 89＋9＋6＋10＝**114 件**。既存の契約で足りるのは、Settings 69、Main 1（`ExecutablePath`）、Tray 3（`ApplicationVersion`、`GetIconResourcePath`＋`UseIconChoice`、`ProfilePath`）、ProfileSettings 1（`GetDefaultButtonMapping`）。残りは新規メンバー（IAppSettingsService 4、IEnvironmentService 4）と、決定1・3・4・5 に依存する。

### 0A.3 決定事項（2026-10-13 確定: 決定1＝S9-A、決定2＝C1、決定3＝P2、決定4＝B1、決定5＝A1、決定6＝D1、決定7＝T1、決定8＝K1。すべて推奨案）
以下は提示時の前提・選択肢・推奨理由の記録。

#### 決定1: 対象範囲とコンストラクタの方式（静的結合をどこまで解消するか）
- **前提**: 4 つの ViewModel のコンストラクタには、`?? AppHost.GetService<…>()`、`?? Global.*Instance`、`?? Program.rootHub` というフォールバックがある（0A.1-10）。これは `copilot-instructions.md` §3.1（Pure DI。Service Locator を持ち込まない）に反する。一方、`App.logHolder`・`App.requestClient`・`App.Current`・`StartupMethods`・`Util` は Global ではない静的結合で、Step8 の決定1＝S1 により Step10 の台帳へ送ってある。
- **案S9-A（推奨）: `Global.` 114 件に加えて、4 つの ViewModel（と `PresetMenuHelper`）のコンストラクタ内フォールバックをすべて解消し、依存を必須引数にする**
  - 理由: Step9 の名称・目的（主要 4 大 ViewModel の Pure DI 化）に合致し、Step8 の決定2＝L2（必須引数）と同じ方針になる。渡し忘れはコンパイラが検出する。生成側は `MainWindow`（保持済みのサービスを渡す）、`ViewModelFactory`、`ProfileEditor` の 3 箇所に限られる。`App.logHolder` などは Step10 へ送る。
  - 影響: `new ProfileSettingsViewModel(device, service)` のように引数を省略しているテスト（`ProfileSettingsViewModelTests` 3 箇所、`ProfileSettingsViewModelTargetDeviceTests` 2 箇所）を、同じマイクロステップで修正する。`ProfileEditor.xaml.cs:319` のレガシーフォールバックは、ファクトリが必ず解決できる前提で削除する（`ProfileEditorTargetDeviceGuardTests` の検査も更新）。
- **案S9-B: `Global.` 114 件のみ。コンストラクタは任意引数＋フォールバックのまま（旧計画 §3 の設計）**
  - 利点: 差分が小さく、テストの修正が要らない。欠点: Service Locator が残り、Step9 の名称と合わない。フォールバックの `Global.*Instance` は Step12 の削除対象のままになる。
- **案S9-C: S9-A に加えて、`App.logHolder`（5 箇所）・`App.requestClient` も契約（インターフェース）にする**
  - 欠点: ログ基盤への変更になり、§2.3（ログ出力の完全維持）の検証コストが大きい。Step8 の決定1 の S3 と同じ理由で、本 Step の範囲を超える。

- **確定（2026-10-13）: S9-A**

#### 決定2: `ProfileSettingsViewModel` への `ControlService` の渡し方
- **前提**: `controlService` は、ジャイロ系 4 つの setter が `IProfileSettingsService` へ渡す引数としてだけ使われる。現在は `Program.rootHub` で補っている。DI には `ControlService` が Singleton で登録されており（`Program.rootHub ?? new ControlService(…)`）、既存のテストでも `AppHost.GetService<ControlService>()` で解決している。
- **案C1（推奨）: `ViewModelFactory` が、`Func<ControlService>`（遅延解決）で受け取り、生成時に解決して ViewModel へ渡す**
  - 理由: Step2 決定D1（`Func<IOutputSlotService>`）の前例どおり、ファクトリの構築時には `ControlService` を作らない。K7-1 の注意（`ControlService` は明示的なホスト構築より前に解決しない）も守れる。ViewModel から `Program.rootHub` が消える。
  - 影響: `ServiceRegistration` に `Func<ControlService>` の登録が 1 つ増える。ファクトリ経由のテスト（`PatternCViewModelTests`）では、生成時に `ControlService` が解決される（既存のテストが同じ解決を行っているため、問題は想定しない）。
- **案C2: ファクトリが `Program.rootHub` を渡す**
  - 利点: 差分が小さい。欠点: 静的結合（`Program.rootHub`）がファクトリに移るだけで残る。

- **確定（2026-10-13）: C1**

#### 決定3: プログラムフォルダ（`Global.exedirpath`）の置換先
- **前提**: 0A.1-5 のとおり、`IPathService.ExecutableDirectory` は `exedirpath` と値が異なりうるため置換先にできない。対象は `SettingsViewModel` 6、`TrayIconViewModel` 1、`ProfileSettingsViewModel` 3、`MainWindowsViewModel` 2（0A.4 で削除する場合は 0）。Step10 の `SaveWhere`（5）、`Util.cs`（2）、`StartupMethods`（4）にも同じ参照がある。
- **案P2（推奨）: `IPathService` に、`Global.exedirpath` と同じ値を返す新メンバー（例: `ProgramDirectory`）を追加する**
  - 理由: 参照が 10 箇所以上あり、同じ式（`Path.GetDirectoryName(ExecutablePath)`）の複製を避けられる。値の定義（ジャンクション解決済み）をサービスの契約として明示でき、Step10 の呼び出し元も同じメンバーへ置換できる。`ExecutableDirectory` の既存の利用者（`MainWindow` の 3 箇所）は変更しない。
  - 影響: `IPathService` の実装は 1 つ（`PathService`）。テスト用の自前実装は無い（確認済み）。
- **案P1: 各所で `Path.GetDirectoryName(IPathService.ExecutablePath)` と書く**
  - 利点: 契約の追加が要らない（Step7 の前例）。欠点: 同じ式が十数箇所に複製される。
- **案P3（不採用）: `IPathService.ExecutableDirectory` へ置換する**
  - 値が変わる（末尾の区切り、ジャンクションの扱い）ため、§2.2（現在の機能の完全維持）に反する。

- **確定（2026-10-13）: P2**

#### 決定4: バッテリー変化（`Global.BatteryChanged`）の購読の契約
- **前提**: 通知を出す側は `IAppearanceSettingsService.InvokeBatteryChanged`（`ControlService` が使用。実体は `Global.InvokeBatteryChanged`）だが、受け取る側（`TrayIconViewModel`）の契約が無く、`Global.BatteryChanged` の静的イベントを直接購読している。
- **案B1（推奨）: `IAppearanceSettingsService` に `event EventHandler<byte> BatteryChanged` を追加する**
  - 理由: 発行（`InvokeBatteryChanged`）と購読が同じサービスに揃う。実装は `Global.BatteryChanged` への add／remove の委譲にして、送信元（`typeof(Global)`）も引数も従来と同じにする。`TrayIconViewModel` の依存が `IAppearanceSettingsService` に集約される（アイコン選択と同じサービス）。
- **案B2: `IDeviceStateService` に追加する（旧計画の B12）**
  - 欠点: 発行側（`IAppearanceSettingsService`）と購読側が別サービスになる。
- **案B3: 購読は `Global.BatteryChanged` のまま残す**
  - 欠点: Step9 の対象（`TrayIconViewModel` の `Global.` 参照）が 1 件残り、Step12 でシムを削除する際に再度対応が要る。

- **確定（2026-10-13）: B1**

#### 決定5: `RefreshActionAlias` の契約
- **前提**: `Global.RefreshActionAlias(DS4ControlSettings, bool)` は、出力 KBM のマッピング（`Global.outputKBMMapping`＝`IProfileSettingsService.OutputKBMMapping` と同じ実体）で仮想キーの別名を更新する。利用者は `PresetMenuHelper`（本 Step）、`BindingWindowViewModel`（706、770 行。Step10）、`Global` 自身（9882 行）。
- **案A1（推奨）: `IProfileSettingsService` に `RefreshActionAlias(DS4ControlSettings setting, bool shift)` を追加する**
  - 理由: マッピングの実体（`OutputKBMMapping`）と同じサービスで、`PresetMenuHelper` と `BindingWindowViewModel` は既にこのサービスを保持している。実装は `Global.RefreshActionAlias` へ委譲する薄いシム。
- **案A2: `ISpecialActionRepository` に追加する（旧計画の B6）**
  - 欠点: `Actions.xml` の管理とは無関係な責務で、`PresetMenuHelper` にはこのサービスの参照が無い。

- **確定（2026-10-13）: A1**

#### 決定6: 呼出元 0 件のコードの扱い（§2.4）
- **前提（調査結果）**: `MainWindowsViewModel` の `RunUpdaterCheck`、`DownloadUpstreamVersionInfo`、`LauchDS4Updater`、`IsNET8Available`、`DownloadUpstreamUpdaterVersion`（private）、公開フィールド `updaterExe` は、外部の呼出元が 0 件（`CheckDrivers` と `FullTabsEnabled` だけが使われる）。`git log -S` で経緯を調べると、2025-12-12 のコミット `b55d160b`（「Updater機能の改善とリリースページへのリンクを追加」）で `MainWindow.xaml.cs` の呼び出しが消え、現在の更新処理は `UpdaterWindow.xaml.cs` が `DS4Updater.exe` を自前で起動している。これらは、上流（DS4Windows 本家）の更新経路の残骸で、本プロジェクトの目的（Pure DI 化と4層モデルの依存方向の整理）に照らして「本来使うべき API」ではない。`SettingsViewModel` の `SettingsViewModel_CheckForUpdatesChanged`（586 行）も、購読がコメントアウト（496 行）されたままで呼出元 0 件（2019-12-18 のインポート時から同じ状態）。
- **案D1（推奨）: 上記を削除する（`MainWindowsViewModel` の `Global.` 参照 9 件が 3 件になり、`App.requestClient` 2 件と `HttpProgress`／`GithubRelease` の参照も消える）**
  - 理由: 使うべきでない孤立実装で、残すと Pure DI 化の対象として無駄に置換・テストが要る。更新の機能そのものは `UpdaterWindow` が担っており、機能の削減ではない。§2.2 により、ユーザーの承認を得て実施する。
- **案D2: 削除せず、§3.3 原則4 の TODO コメントを付けて温存し、置換も行う**
  - 欠点: 6 件の置換と、誰も呼ばないコードのテストが必要になる。

- **確定（2026-10-13）: D1**（削除は Step9-2［`MainWindowsViewModel`］と Step9-3a［`SettingsViewModel_CheckForUpdatesChanged`］で、経緯の再確認［`git log -S`］のうえ実施する）

#### 決定7: 後始末のない既存テスト（`ProfileSettingsServiceSubSettingsTests.ProfileActions_GetSet_ShouldPersistAndNotify`）
- **前提**: このテストは `Global.store.profileActions` を 8 要素の配列に置き換えたまま戻さない。実行順によって、`Global.ProfileActions[8]`（編集スロット）が範囲外になり、別のテストが失敗する。Step9 は編集スロットを使うテスト（`ProfileSettingsViewModel`）を触るため、再び当たる可能性がある。
- **案T1（推奨）: Step9-1 で、`finally` で元の配列へ戻す 1 箇所の修正をする**
  - 理由: 修正は小さく、`Phase6-Status.md` §6.4-1 の方針（変更した static 状態は戻す）に沿う。Step8-4 で `ProfileEditorBackendViewModelTests` が独立した配列で回避した経緯がある。
- **案T2: 修正せず、Step9 の新規テストが独立した配列を使う（Step8-4 と同じ回避）**
  - 欠点: 他のテストへの影響が残り続ける。

- **確定（2026-10-13）: T1**

#### 決定8: K7b-1（実機確認中の異常終了。原因未特定）の扱い
- **前提**: 再現は不定（再現手順・イベントログの記録は `Phase6-Status.md` §6.5 K7b-1）。Step9 は `MainWindowsViewModel`／`SettingsViewModel`／トレイ／プロファイル編集（`ProfileSettingsViewModel`）と、実機操作の多い画面に触れる。Step9 で変えるコードにポインタ操作や非同期 I/O は無く、K7b-1 の候補（`DS4Device`、`HidDevice`、`DS4Sixaxis`、`DS4OutDeviceExt`）には触れない。
- **案K1（推奨）: 切り分けは Step11 まで寝かせる。Step9 の実機確認中に異常終了が出た場合は、基準コミット（`f13d5c7c`）のビルドで同じ手順を試して、Step9 の回帰かを判別する**
  - 理由: 先に切り分ける手間（Step7b 前のビルドで 10 回程度の試行）を使わずに、必要になったときだけ最小の判別を行える。判別に使う基準コミットは本節で固定済み。
- **案K2: Step9 の前に、`Phase6-Status.md` §6.5 の手順（1）〜（2）だけ先に実施する**
  - 利点: Step9 の確認中の異常終了を、すぐ K7b-1 と判断できる。欠点: ユーザーの手間が先に発生し、結果によらず Step9 の作業は変わらない。
- **案K3: 切り分けを Step9 の最初のマイクロステップとして計画に入れる**
  - 欠点: Step9 の範囲外の調査が Step の完了判定に混ざる（Step7b でも切り離した）。

- **確定（2026-10-13）: K1**（基準コミットは `f13d5c7c`）

### 0A.4 呼出元 0 件・未使用コードの確認（§2.4）
- **`MainWindowsViewModel` の更新関連メンバー**（決定6）: 経緯の調査結果は決定6 の前提のとおり。判断は「使うべきでないもの（上流の更新経路の残骸。現行の経路は `UpdaterWindow`）」。削除は決定6 の承認後に行う。
- **`SettingsViewModel.SettingsViewModel_CheckForUpdatesChanged`**: 同上。
- **契約のみで未接続のもの**: 今回の置換先で新たに見つかったものは無い。`IAppearanceSettingsService.InvokeBatteryChanged` は `ControlService` が使用中。
- **注意（§2.4 の観察のみ。Step9 の範囲外）**: `TrayIconViewModel` は `BatteryChanged` を購読するだけで解除しない（`Dispose` を持たない。アプリの存続期間と同じ寿命のため実害は無い）。置換後も同じ寿命・同じ動作とし、変更しない。

### 0A.5 マイクロステップ（決定1〜8 の確定により確定）
- **Step9-0（今回）**: 着手前確認、台帳の再作成、決定事項の提示（本節）。
- **Step9-1（契約の追加と後始末。完了確定 2026-10-13。ビルド・テストビルド・テスト実行成功、コミット・反映済み）**
  - 実装内容: `IAppSettingsService` に `UseCustomSteamFolder`／`CustomSteamFolder`／`FakeExeName`／`LogMaxArchiveFiles`、`IEnvironmentService` に `IsViGEmBusInstalled`／`IsRunningSupportedViGEmBus`／`IsUsingMinViGEm117333`／`AdminNeeded`、`IPathService.ProgramDirectory`、`IAppearanceSettingsService.BatteryChanged`、`IProfileSettingsService.RefreshActionAlias`。実装はいずれも `Global` への薄い委譲（実装クラスは各 1 つ。テスト用の自前実装は無いことを確認済み）。`FakeExeName` は、`Global.FakeExeName` が不正なファイル名を黙って無視するため、値が実際に変わったときだけ通知する。
  - テスト: 新規 `Phase6Step9ContractExtensionTests`（12 件）、`ProfileSettingsServiceSubSettingsTests.ProfileActions_GetSet_ShouldPersistAndNotify` を `finally` で元の配列へ戻すよう修正（決定7＝T1）。
  - 計画からの変更: コンストラクタの `[DI]` Trace ログ（0A.7）は、対象のコンストラクタを変更する Step9-2〜9-4 で追加する。モデル図 03・04 の更新（追加した契約のメンバー記述）は Step9-5 で行う。
  - `IAppSettingsService`（4 メンバー）、`IEnvironmentService`（4 メンバー）、`IPathService`（決定3）、`IAppearanceSettingsService`（決定4）、`IProfileSettingsService`（決定5）の追加と、各実装（`Global` への薄い委譲）。
  - 既存の実装クラス以外に、これらの契約を実装するテスト用の自前クラスが無いことは確認済み（Step9-1 の着手時に再確認する）。
  - 決定7 の修正（`finally` で元に戻す）。
  - テスト: 新規 `Phase6Step9ContractExtensionTests`（各メンバーが `Global` の値と一致すること、書き込みが `Global` に届くこと、`BatteryChanged` が `InvokeBatteryChanged` の送信元・引数で届くこと）。
- **Step9-2（`MainWindowsViewModel` と `TrayIconViewModel`。完了確定 2026-10-13。ビルド・テストビルド・テスト実行成功。実機確認: Trace ログで `[DI] MainWindowsViewModel: created …` と `[DI] TrayIconViewModel: subscribed BatteryChanged …` の 2 行を確認、起動時のエラーなし。トレイのバッテリー表示・ツールチップの目視は未報告）**
  - 実装内容: `MainWindowsViewModel` は、呼出元 0 件のメンバー（6 件。再確認でも外部の参照は xaml・テストを含め 0 件）を削除し、コンストラクタを `(IEnvironmentService, IPathService)` の必須引数にした（`CheckDrivers` は `IsViGEmBusInstalled`／`IsRunningSupportedViGEmBus`／`ExecutablePath`）。`Global.` 参照は 0 件。`TrayIconViewModel` のコンストラクタは `(ControlService, ProfileList, IAppearanceSettingsService, IProfileRepository, IEnvironmentService, IPathService)`（必須。null は `ArgumentNullException`）。`trayTitle` はインスタンスの値（`ApplicationVersion`）、アイコンは `GetIconResourcePath`、購読は `appearanceSettings.BatteryChanged`、プロファイル名は `profileRepository.ProfilePath`、プログラムフォルダは `pathService.ProgramDirectory`。残る `Global.` は定数 `RESOURCES_PREFIX` のみ。生成側は `MainWindow.xaml.cs` の 2 箇所（フォールバックの `new MainWindowsViewModel(environmentService, pathService)` と `new TrayIconViewModel(…)`）。
  - `[DI]` Trace ログ（追加）: `[DI] MainWindowsViewModel: created (IEnvironmentService, IPathService injected)`、`[DI] TrayIconViewModel: subscribed BatteryChanged via IAppearanceSettingsService`。
  - テスト: 新規 `MainWindowsAndTrayViewModelGlobalReferenceGuardTests`（ソース走査、必須引数、DI 解決）。トレイの動作そのもの（`ContextMenu` を生成するため STA が必要）は単体テストを書かず、契約テスト（Step9-1 の `BatteryChanged`）とソース走査で代え、実機確認（0A.7-5）で確認する。
  - `MainWindowsViewModel`: 呼出元 0 件のメンバーの削除（決定6＝D1。`RunUpdaterCheck`、`DownloadUpstreamVersionInfo`、`LauchDS4Updater`、`IsNET8Available`、`DownloadUpstreamUpdaterVersion`、`updaterExe`。削除の前に `git log -S` と呼出元 0 件を再確認する）と、コンストラクタへの `IEnvironmentService`／`IPathService` の必須引数化。`MainWindow` のフォールバック（129 行）の呼び出しを合わせる。
  - `TrayIconViewModel`: コンストラクタに `IAppearanceSettingsService`／`IProfileRepository`／`IEnvironmentService`／`IPathService` を必須引数で追加。`trayTitle` をインスタンスの値にする。`MainWindow` の生成呼び出し（160 行）を合わせる。
  - テスト: ソース走査ガード（`Global.` の再混入の検査。定数 `RESOURCES_PREFIX` は許可）。`TrayIconViewModel` は WPF の `ContextMenu`／`MenuItem` を生成するため、単体テストの実行環境（STA スレッド）の制約を確認し、動作テストが書けない場合は、契約テスト（Step9-1）とソース走査で代える。
- **Step9-3a（完了確定 2026-10-13。ビルド・テストビルド・テスト実行成功、コミット・反映済み）**: `SettingsViewModel` の 69 件（A1〜A4、`hidHideInstalled`、`ProcessPriority`、`IsAdministrator`、`LogMinLevel`）を `IAppSettingsService`／`IAppearanceSettingsService`／`IEnvironmentService` 経由へ置換。コンストラクタは `(IAppSettingsService, IAppearanceSettingsService, IEnvironmentService)` の必須引数にした（**計画との差**: コンストラクタの必須引数化を 3b から 3a へ前倒し。`IPathService` は 3b で使うときに追加）。`MainWindow.xaml.cs:134` の生成呼び出しを合わせた。呼出元 0 件の `SettingsViewModel_CheckForUpdatesChanged` と、コメントアウトされた購読行を削除（決定6＝D1）。`[DI] SettingsViewModel: created`（Trace）を追加。残る `Global.` は 20 件（Steam 5、偽装 exe 3、`exedirpath` 6、`exelocation` 3、`LogMaxArchiveFiles` 3）。テスト: 新規 `SettingsViewModelGlobalReferenceGuardTests`。
  - 元の計画: `SettingsViewModel`、既存の契約分 69 件: A1〜A4、A5 のうち `hidHideInstalled`／`ProcessPriority`、A7、A8 の `LogMinLevel`。あわせて、購読がコメントアウトされたままの `SettingsViewModel_CheckForUpdatesChanged` を削除（決定6＝D1）。
- **Step9-3b（完了確定 2026-10-14。ビルド・テスト・実機確認成功）**: 残り 20 件（`UseCustomSteamFolder`／`CustomSteamFolder`／`FakeExeName`／`LogMaxArchiveFiles` は `IAppSettingsService`、`exedirpath` 6 件は `IPathService.ProgramDirectory`、`exelocation` 3 件は `IPathService.ExecutablePath`）を置換し、コンストラクタに `IPathService` を追加（`(IAppSettingsService, IAppearanceSettingsService, IEnvironmentService, IPathService)`）。`SettingsViewModel.cs` の `Global.` 参照は 0 件（定数も使っていない）。`MainWindow.xaml.cs:134` の生成呼び出しを合わせた。`SettingsViewModelGlobalReferenceGuardTests` に 6 メンバーと「`Global.` が 0 件」の検査を追加。
  - 元の計画: `SettingsViewModel`、新規メンバー・パス分 20 件とコンストラクタ: A5 の Steam・偽装 exe、A6、A8 の `LogMaxArchiveFiles`、コンストラクタの必須引数化（`IAppSettingsService`、`IAppearanceSettingsService`、`IEnvironmentService`、`IPathService`）、`MainWindow` の生成呼び出し（134 行）。
  - テスト: `SettingsViewModel` の各プロパティの読み書きが `Global` に届くこと（既存の `SettingsViewModelWatchpoint2Tests` は DI から解決するため維持）。ソース走査ガード。
- **Step9-3c（追加。完了確定 2026-10-14。ビルド・テスト・実機確認成功）: OSC 送信の NullReferenceException の修正（K9-1）**
  - 発見の経緯: Step9-3b の実機確認で、設定タブの「Enable Monitoring Service」（OSC 送信）をオンにするとアプリが終了した。Windows イベントログとアプリのログ（`OSC SENDER STARTED …` の 3.5ms 後に `NullReferenceException`、`ControlService.OSCPreMappingStep`）から原因を特定。
  - 原因: `UseOscSender` が先に true になり（`SettingsViewModel` の setter）、`oscSender` の生成（View の Click ハンドラ → `ChangeOSCSenderStatus(true)`）が終わる前に、入力スレッドが `oscSender.Send` を呼ぶ競合。Step2 の直前のコミット（`da16de4d^`）にも同じ構造があり、Step9 の置換による回帰ではない（Step2-PR-1 から先送りされていた OSC の実機確認で初めて露見した）。
  - 修正（ユーザー承認 2026-10-14）: `ControlService` に null 安全な `SendOsc` を追加し、`oscSender.Send(` の 26 箇所をすべて置換。無効化時はフィールドを先に null にしてから閉じる。通常時の動作は同じ（送信側が未生成の間は送信しない）。
  - テスト: 新規 `ControlServiceOscSenderRaceTests`（未生成時の `OSCPreMappingStep` が例外にならないこと、無効化、ソース走査）。
  - 実機確認: OSC 送信のオン／オフでクラッシュしないこと。ログ: `OSC SENDER STARTED AT IP: … PORT: …`／`OSC SENDER STOPPED`（GUI ログ）。受信側のアプリが無い場合は、送信先ポートが開いていなくても UDP のため例外にならない。
- **Step9-3d（追加。完了確定 2026-10-14。ビルド・テスト・実機確認成功）: 偽装 exe 名の既存不具合の修正（K9-2）**
  - 発見の経緯: Step9-3b の実機確認で、偽装 exe 名を設定すると exe だけが作られ（`.runtimeconfig.json`／`.deps.json` が作られない）、名前を変えても古いファイルが消えなかった。
  - 原因: `CreateFakeExe` が設定ファイルの元を `{exelocation}.runtimeconfig.json`（＝`DS4Windows.exe.runtimeconfig.json`。実在しない）から組み立て、`File.Copy` が `FileNotFoundException` になる。この例外は WPF のバインディングが握りつぶすためアプリは落ちず、setter が `FakeExeNameChanged`（作成）の後に呼ぶ `FakeExeNameChangeCompare`（古いファイルの削除）が実行されなかった。2021-05-09（`56408101`、`2e128dcd`）から変わっておらず、Step9 の置換（`Global.exelocation`→`IPathService.ExecutablePath`、同じ値）による回帰ではない。
  - 修正（ユーザー承認 2026-10-14）: 設定ファイルの元を拡張子を除いた `実行ファイル名.runtimeconfig.json`／`.deps.json` にし、元が無ければコピーを省略。古いファイルの削除を新規作成より先に行う（setter の呼び出し順を入れ替え）。コピーは上書き可。作成・削除は `SettingsViewModel` の公開静的メソッド（`CreateFakeExeFiles`／`DeleteFakeExeFiles`）にして、一時フォルダでテスト可能にした。
  - テスト: 新規 `SettingsViewModelFakeExeTests`（6 件）。
  - 実機確認: 偽装 exe 名（拡張子は付けない。例 `TestFake`）を設定 → プログラムフォルダに `TestFake.exe`、`TestFake.runtimeconfig.json`、`TestFake.deps.json` ができる。名前を変更（例 `TestFake2`）→ 古い 3 ファイルが消え、新しい 3 ファイルができる。名前を空にする → 古い 3 ファイルが消える。
- **Step9-4（実装済み 2026-10-14、ユーザーのビルド・テスト・実機確認待ち。Step9-3c・3d は完了確定: ビルド・テスト・実機確認成功）**: `ProfileSettingsViewModel`・`PresetMenuHelper`・生成側
  - `ProfileSettingsViewModel`: `Global.` 10 件を解消（`IsUsingMinViGEm117333`→`IEnvironmentService`、`exedirpath` 3 件→`IPathService.ProgramDirectory`、`defaultButtonMapping`→`IProfileSettingsService.GetDefaultButtonMapping()`［ループの前に 1 回取得］、`RefreshActionAlias`→`IProfileSettingsService.RefreshActionAlias`）。コンストラクタは `(int device, IProfileSettingsService, ControlService, IOutputSlotService, IProfileRepository, IEnvironmentService, IPathService, int targetDevice = -1)`。`targetDevice` 以外はすべて必須。`ControlService` のみ null を許容（ジャイロ系 setter への引数としてだけ使われ、従来も `Program.rootHub` が null の環境では null だったため）。`?? AppHost.GetService`／`?? Global.*Instance`／`?? Program.rootHub` はすべて削除。非定数の `Global.` は 0 件（残るのは定数 `ASSEMBLY_RESOURCE_PREFIX` のみ）。
  - `PresetMenuHelper`: コンストラクタの `IProfileSettingsService` を必須に（`Global.ProfileSettingsServiceInstance` のフォールバックを削除）。
  - `ViewModelFactory`: 依存をすべて必須引数に（`IProfileSettingsService`、`IProfileRepository`、`ISpecialActionRepository`、`IOutputSlotService`、`IEnvironmentService`、`IPathService`、`Func<ControlService>`）。`CreateProfileSettingsViewModel` が `ControlService` を生成のたびに遅延解決して渡す（決定2＝C1）。`ServiceRegistration` に `Func<ControlService>` を登録。契約 `IViewModelFactory.CreateProfileSettingsViewModel(int device, int targetDevice = -1)` は変更なし。
  - `ProfileEditor.xaml.cs`: ファクトリが解決できない場合のレガシーフォールバック（`new ProfileSettingsViewModel(deviceNum, targetDevice: targetDevice)`）を削除（解決できなければ `InvalidOperationException`）。
  - テスト: 新規 `ProfileSettingsViewModelGlobalReferenceGuardTests`、`ProfileSettingsViewModelTestFactory`（テストでの生成を 1 か所に集約）。`ProfileSettingsViewModelTests`・`ProfileSettingsViewModelTargetDeviceTests`・`ProfileEditorTargetDeviceGuardTests`（`new ProfileSettingsViewModel(` の不在を検査）を更新。
  - 元の計画（Step9-4）:
  - `Global.` 10 件の置換、コンストラクタの必須引数化（`IProfileSettingsService`、`ControlService`［決定2］、`IOutputSlotService`、`IProfileRepository`、`IEnvironmentService`、`IPathService`）。`targetDevice` の分離（編集スロットと実機）は変えない。
  - `ViewModelFactory`（必須引数の供給）、`ProfileEditor.xaml.cs` のレガシーフォールバックの削除、`ServiceRegistration`（`Func<ControlService>`）。
  - テスト修正: `ProfileSettingsViewModelTests`、`ProfileSettingsViewModelTargetDeviceTests`、`ProfileEditorTargetDeviceGuardTests`。維持する検査: `ProfileEditorGlobalReferenceGuardTests`、`ProfileEditorMappingDevTypeGuardTests`、`ProfileEditorBackendViewModelTests`。
- **Step9-5（完了 2026-10-14）**: 完了確認と文書更新（モデル図 03・04、Status／Plan、Step10 への申し送り、Step11 の先送り台帳、完了報告書）。元の計画:
  - **（元の記載）**: ソース走査ガードの確定、モデル図 03・04 の更新（04 §2 の `MainWindowsViewModel`／`SettingsViewModel`／`ProfileSettingsViewModel`／`TrayIconViewModel` の依存、`IViewModelFactory`／`ViewModelFactory` の依存、追加した契約のメンバー記述）、`Phase6-Status.md`／`Phase6-Plan.md`、`Phase6-Step10-Plan.md` 冒頭への申し送り、`Phase6-Step11-Plan.md` §3.3 への先送り登録、完了報告書 `Phase6-Step9-Completion-Report.md`。

### 0A.6 Step9 と Step10 の境界
- **Step9 の範囲（本節の台帳）**: `SettingsViewModel`、`MainWindowsViewModel`、`TrayIconViewModel`、`ProfileSettingsViewModel`（と同じファイルの `PresetMenuHelper`）。および、これらの生成側の呼び出し合わせのみ（`MainWindow.xaml.cs` の 3 行、`ViewModelFactory`、`ProfileEditor.xaml.cs` の 1 箇所、`ServiceRegistration`）。
- **Step10 の範囲（Step9 では変更しない）**:
  - 小型 ViewModel と View: `AboutViewModel`、`AutoProfilesViewModel`、`BindingWindowViewModel`（`RefreshActionAlias` の呼び出し 706、770 行を含む。契約は Step9-1 で用意）、`ControllerListViewModel`、`ControllerRegDeviceOptsViewModel`、`LanguagePackViewModel`、`LogViewModel`、`MappingListViewModel`、`RecordBoxViewModel`、`RenameProfileViewModel`、`TouchButtonUserControlViewModel`、`UpdaterWindowViewModel`、`SpecialActionsListViewModel`、小型 View 11 ファイル、`PresetOption.cs`。
  - 静的結合: `App.logHolder`、`App.requestClient`、`App.Current`、`StartupMethods.*`、`Util.*`（決定1 の S9-A／S9-B の場合）。
  - `MainWindow.xaml.cs` のコンストラクタ内の `AppHost.GetService`（サービスの解決が 10 件以上並ぶ。113〜122 行）と、`Program.rootHub`／`Global.*Instance` へのフォールバック。Step9 では 3 つの ViewModel の生成呼び出しだけ合わせる。
- **Step10 へ送る追加項目**: `exedirpath` の参照（`SaveWhere` 5、`Util.cs` 2、`StartupMethods` 4。決定3 の契約を使う）、`RefreshActionAlias`（`BindingWindowViewModel` 2。決定5 の契約を使う）。

### 0A.7 実機確認項目（案。`Phase6-Step11-Plan.md` §3.3 に登録可能なものは登録する）
Step7 の教訓として、各項目に判定に使うログ・値を併記する。**注意**: アプリ設定の変更（`AppSettingsService` の setter）には、変更時のログ行が無い。判定に使えるログは `[DI] AppSettingsService.Save succeeded`（Trace レベル。保存時）などに限られる。そのため、Step9-1 で次の Trace ログを追加する案とする（§2.3、ログ接頭辞 `[DI]`。既存のログは変更しない）。
- `[DI] SettingsViewModel: created` と、`[DI] TrayIconViewModel: subscribed BatteryChanged via IAppearanceSettingsService`（各コンストラクタ）。
- Trace／Debug ログを有効にする手順は、実機確認の前にコード（`AppLogger.IsTraceEnabled`、`LogMinLevel` の扱い）で確認してから案内する（ここでは推測で書かない）。`[ProfileEditor] Opened profile editor …` は Debug レベル（`ProfileEditor.xaml.cs:295`）、`[DI] ViewModelFactory: Created …` は Trace レベル。

1. **起動とメイン画面（`MainWindowsViewModel`）**: 通常起動し、起動ログにエラーが無いこと。ViGEmBus の状態確認（`CheckDrivers`）で、導入済みの環境では `-driverinstall` の昇格起動が走らないこと。
   - 見る: アプリのログ（GUI のログタブ）に `-driverinstall` 関連の起動・エラーが出ない。
2. **設定タブの読み込み・変更・保存（`SettingsViewModel`）**: 設定タブを開き、各設定（排他モード、スワイプ、通知、遅延警告、クイックチャージ、トレイアイコン選択、テーマ、更新確認、OSC、UDP と平滑化、Steam フォルダ、偽装 exe 名、プロセス優先度、ログ設定）を変更し、アプリを終了して再起動した後も値が維持されること。
   - 見る: 変更後に `Profiles.xml`（保存場所の `Profiles.xml`）の該当タグの値（`UseExclusiveMode`、`QuickCharge`、`UseOSCServer`、`UDPServerPort`、`UseCustomSteamFolder`、`CustomSteamFolder`、`FakeExeName`、`LogMaxArchiveFiles`、`LogMinLevel`、`CheckEveryValue`、`CheckEveryUnit` など）、および保存時の `[DI] AppSettingsService.Save succeeded`。
3. **偽装 exe（`CreateFakeExe`、A5・A6）**: 偽装 exe 名を設定・変更して、プログラムフォルダに `名前.exe`／`.runtimeconfig.json`／`.deps.json` が作られ、名前を変えると古いものが消えること（プログラムフォルダが `Global.exedirpath` と同じ場所であること）。
4. **ログ設定の即時反映（A8）**: `LogMinLevel`／`LogMaxArchiveFiles` を変更して、ログの出力レベルが変わること。
5. **トレイアイコン（`TrayIconViewModel`）**: コントローラーを接続し、トレイアイコンの選択を「バッテリー」にしたとき、バッテリー残量に応じたアイコンへ切り替わること。ツールチップに `DS4Windows v<バージョン>` と各コントローラーの接続種別・バッテリーが出ること。コンテキストメニューでプロファイルの選択（現在のプロファイルにチェック）と「Open Program Folder」が動くこと。
   - 見る: ツールチップの 1 行目（`trayTitle`）がバージョン文字列（`Global.exeversion`）と一致すること。
6. **プロファイル編集（`ProfileSettingsViewModel`、Step8 と同じ確認）**: 一覧から Edit で開き、編集・保存ができること。ProfileEditor を開いたときのログに `[DI] ViewModelFactory: Created ProfileSettingsViewModel for Device 8, targetDevice N`（Trace）と `[ProfileEditor] Opened profile editor for device=8, targetDevice=N` が出ること。
7. **プロファイル編集の個別機能**: Bezier カーブエディタを開くボタン（`exedirpath` を使う 3 箇所。プログラムフォルダ内の `BezierCurveEditor\index.html` が開くこと）、ボタンのプリセット適用（`PresetMenuHelper`。`defaultButtonMapping` と `RefreshActionAlias`。プリセット適用後、ボタンの割り当てがプリセットのとおりになること）、`UsingMinViGEm173333`（ViGEmBus の最小バージョン判定。タッチパッドの Passthru 関連の表示が従来と同じこと）。
8. **異常終了が起きた場合（決定8＝K1 の場合）**: Windows のイベントログの例外種別を記録し、基準コミット `f13d5c7c` のビルドで同じ手順を再試行して、同じ異常終了が起きるかを確認する（`Phase6-Status.md` §6.5 K7b-1）。

### 0A.8 リスクと対策（追加分）
- **見た目は同じでも値が変わる置換**: `exedirpath`→`ExecutableDirectory`（決定3 の案P3）は値が変わる。`GetDefaultButtonMapping()` は複製を返すため、ループの中で毎回呼ぶと割り当てが増える（ループの前に 1 回だけ取得する）。
- **静的初期化の循環**（`Phase6-Status.md` §6.4-3）: `trayTitle` の静的初期化子をインスタンスの値へ変えるのは、この問題の予防にもなる。新しい契約の実装（`EnvironmentService`、`PathService`）の静的初期化子から `Global` を呼ばない（`EnvironmentServiceStaticInitGuardTests` を維持）。
- **テストの自前実装**: 契約にメンバーを追加したとき、実装するテスト用のクラスが無いかを全テストファイルで確認する（§6.4-4）。現時点では実装クラス以外に無いことを確認済み。
- **ソース走査ガードの維持**: `ProfileEditorGlobalReferenceGuardTests`／`ProfileEditorTargetDeviceGuardTests`／`ProfileEditorMappingDevTypeGuardTests`／`ProfileEditorBackendViewModelTests`。ViewModel の書き方を変えるときは、同じマイクロステップ内で検査文字列を更新する。
- **Step10b**: `ProfileSettingsViewModel.cs` には複数の型（`PresetMenuHelper`、`TriggerModeChoice` など）がある。分割は Step10b で行い、本 Step で型を追加しない（§3.3 原則1）。

### 0A.9 完了判定チェックリスト（案）
- [x] 4 つの ViewModel の非定数 `Global.` 参照が 0 件（ソース走査ガードで固定）
- [x] 4 つの ViewModel（と `PresetMenuHelper`）のコンストラクタに `AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックが無い（決定1＝S9-A の場合）
- [x] 追加した契約（`IAppSettingsService` 4、`IEnvironmentService` 4、決定3・4・5 の各 1）が `Global` と同じ値・同じ動作であることをテストで固定
- [x] 後始末のない既存テストの修正（決定7）、既存テストと新規テストが全件成功（`dotnet build`、`dotnet test`）
- [x] 実機確認（0A.7）問題なし（K9-1・K9-2 は修正して再確認、K9-3 は先送り）。先送りは `Phase6-Step11-Plan.md` §3.3 へ登録
- [x] モデル図 03・04 の更新、`Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step10-Plan.md`（申し送り）の更新、完了報告書の作成

---

## 0. 背景と方針確定（推奨案: 選択肢1の採用）【旧版・2026-09-18。現行は §0A】

### 0.1 暫定39件から実質79箇所への精査結果
旧計画書（2026-09-11）では主要4大ViewModelの対象件数を「暫定39件」としていたが、2026-09-18に実施された Step1 精密再監査（`Phase6-Step1-UI-Reference-Evidence.md` §3）において、実コード走査の結果、以下の通り計79箇所（除外const 14件を除く）の実参照式が存在することが判明した：

- **`SettingsViewModel.cs`**: 暫定23件から拡大し、実質**計45行・55箇所**の実参照式が存在（OSC/UDP送受信、平滑化、Steamフォルダ、プロセス優先度、管理者判定、ログ設定等）。
- **`MainWindowsViewModel.cs`**: **計8箇所**（ViGEm判定、管理者権限、パス取得等）。
- **`TrayIconViewModel.cs`**: **計6箇所**（除外const 12件を除く、バージョン、アイコンリソース、`BatteryChanged` イベント、プロファイルパス等）。
- **`ProfileSettingsViewModel.cs`**: **計10箇所**（除外const 2件を除く、ViGEm最小バージョン判定、ボタンマッピング、アクションエイリアス、フォールバックF等）。

### 0.2 方針の確定: 【推奨案: 選択肢1（主要4大ViewModel集中 ＋ カテゴリ別段階的移行・Pure DI徹底）】
全16個の ViewModel を一度に改修するビッグバン型（選択肢2）は差分が100箇所以上に膨れ上がり、デバッグとレビューの負荷が過大となる。  
本ステップでは、リスクを最小限に抑えつつアーキテクチャの美しさを最大化するため、以下の**中枢優先・リスク分散型アプローチ**を採用する：

1. **アプリの中枢を担う主要4大ViewModelにスコープを限定**:  
   アプリのライフサイクル、サーバー通信、バックグラウンド常駐、プロファイル設定を統括する中核4ファイル（79箇所）の Pure DI 化を確実に先行完了させる。
2. **コンストラクタ引数注入（Pure DI）の徹底**:  
   クラス内部での `AppHost.GetService<T>()` の都度解決（サービスロケータ）に甘えることなく、不足している依存サービス（`IEnvironmentService`, `IPathService`, `IDeviceStateService` 等）はコンストラクタ引数へ明示的に追加注入（オプショナル引数 ＋ フォールバック付き）し、完全なテスタビリティを確立する。
3. **最難関 `SettingsViewModel`（55箇所）の機能カテゴリ別PR分割**:  
   多岐にわたる設定項目を一挙に置換せず、「①基本設定・テーマ・ログ」「②OSC / UDP / 平滑化」「③環境・パス・外部連携」に分割して安全に適用する。
4. **残存小型ViewModel（12ファイル・約30箇所）の Step10 への明確な引き継ぎ**:  
   `AboutViewModel` や `AutoProfilesViewModel` などの小型 ViewModel 群は、次ステップ（Phase6-Step10）において、対応する View（XAML）とペアで漏れなく 100% Pure DI 化する。

---

## 1. 目的

1. `SettingsViewModel.cs`, `MainWindowsViewModel.cs`, `TrayIconViewModel.cs`, `ProfileSettingsViewModel.cs` の4大主要ViewModelにおいて、コンストラクタ引数経由の Pure DI 構造を確立する。
2. 4ファイル内に存在する全79箇所の Global 直接参照を、DI サービス経由の呼び出しへ完全置換する。
3. Step8（案3-A）で `ProfileSettingsViewModel` に移設されたプロファイル保存・適用・ブランク作成機能と完全に調和させ、内部に残存するサービスフォールバック（F）を整理する。
4. Step10 で実施される残存小型 ViewModel（12ファイル）の Pure DI 化に向けた強固な基盤を確立する。

---

## 2. 全件参照台帳（主要4大ViewModel: 全79箇所・完全カタログ）【旧版・2026-09-18。現行は §0A.2】

### 表1: `SettingsViewModel.cs`（ID: C9-S01 〜 C9-S55、計55箇所）

| ID | 行番号 | 設定項目／経路 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C9-S01 | 89 | 排他モード | `Global.getUseExclusiveMode()` | (a) | `_appSettingsService.UseExclusiveMode` |
| C9-S02 | 103 | スワイプ切替 | `Global.SwipeProfiles` | (a) | `_appSettingsService.SwipeProfiles` |
| C9-S03 | 120 | 通知設定 | `Global.Notifications` | (a) | `_appSettingsService.Notifications` |
| C9-S04 | 158 | 切断時BT切断 | `Global.DCBTatStop` | (b) | `_appSettingsService.DCBTatStop` |
| C9-S05 | 162 | 遅延警告フラッシュ | `Global.FlashWhenLate` | (b) | `_appSettingsService.FlashWhenLate` |
| C9-S06 | 181 | クイックチャージ | `Global.QuickCharge` | (b) | `_appSettingsService.QuickCharge` |
| C9-S07 | 188 | アイコン選択 | `Global.UseIconChoice` | (a) | `_appSettingsService.UseIconChoice` |
| C9-S08 | 196 | テーマ設定 | `Global.UseCurrentTheme` | (a) | `_appearanceSettingsService.UseCurrentTheme` |
| C9-S09 | 209 | アップデート確認 | `Global.CheckWhen` | (a) | `_appSettingsService.CheckWhen` |
| C9-S10〜S15 | 279〜326 | OSC サーバー・送信設定（6箇所） | `isUsingOSCServer`, `getOSCServerPortNum`, `isUsingOSCSender`, `getOSCSender*`, `isInterpretingOscMonitoring` | (b) | `_appSettingsService.UseOscServer`, `OscServerPort`, `UseOscSender`, `OscSenderPort`, `OscSenderAddress`, `InterpretingOscMonitoring` |
| C9-S16〜S18 | 333〜349 | UDP 平滑化設定（3箇所） | `IsUsingUDPServerSmoothing`, `UDPServerSmoothingMincutoff`, `UDPServerSmoothingBeta` | (b) | `_appSettingsService.UseUdpServerSmoothing`, `UDPServerSmoothingMincutoff`, `UDPServerSmoothingBeta` |
| C9-S19 | 359, 608 | カスタムSteamフォルダ（2箇所） | `Global.CustomSteamFolder` | (b) | `_appSettingsService.CustomSteamFolder` |
| C9-S20 | 366 | デフォルトプロファイル復帰 | `Global.AutoProfileRevertDefaultProfile` | (b) | `_appSettingsService.AutoProfileRevertDefaultProfile` |
| C9-S21 | 373, 615 | 偽装プロセス名（2箇所） | `Global.FakeExeName` | (b) | `_appSettingsService.FakeExeName` |
| C9-S22 | 380 | HidHide キャッシュ | `Global.HidHideInstalledCache` | (b) | `_environmentService.HidHideInstalled` |
| C9-S23 | 389, 658 | 絶対EDID設定（2箇所） | `Global.UseAbsoluteEDID` | (b) | `_appSettingsService.UseAbsoluteEDID` |
| C9-S24 | 400 | プロセス優先度 | `Global.ProcessPriority` | (b) | `_appSettingsService.ProcessPriority` |
| C9-S25 | 411 | 管理者権限判定 | `Global.IsAdministrator()` | (a) | `_environmentService.IsAdministrator()` |
| C9-S26〜S27 | 443, 454 | 実行パス・ディレクトリ（2箇所） | `Global.exedirpath`, `Global.exelocation` | (a) | `_pathService.ExecutablePath` 経由 |
| C9-S28〜S29 | 473, 474 | UDP平滑化イベント（2箇所） | `UDPServerSmoothingMincutoffChanged`, `BetaChanged` | (b) | `_appSettingsService.UDPServerSmoothing*Changed` 購読 |
| C9-S30 | 476 | モニター情報取得 | `Global.GrabCurrentMonitors()` | (b) | `_environmentService.GrabCurrentMonitors()` |
| C9-S31〜S33 | 487〜497 | UDP サーバー設定（3箇所） | `isUsingUDPServer`, `getUDPServerPortNum`, `getUDPServerListenAddress` | (a) | `_appSettingsService.UseUdpServer`, `UdpServerPort`, `UdpServerListenAddress` |
| C9-S34〜S35 | 517, 522, 661, 662 | ログ設定（4箇所） | `LogMaxArchiveFiles`, `LogMinLevel` | (b) | `_appSettingsService.LogMaxArchiveFiles`, `LogMinLevel` |
| C9-S36〜S45 | 641〜655 | 保存時設定代入（10箇所） | `setUDPServer*`, `setOSC*`, `UDPServerSmoothing*` 等 | (a)/(b) | `_appSettingsService` プロパティセッター呼び出し |
| C9-S46 | 656 | プロファイルサービス参照 | `Global.ProfileSettingsServiceInstance` | (a) | 注入済み `_profileSettings` 利用 |
| C9-S47 | 659 | タッチ感度設定 | `Global.TouchSensitivity` | (a) | `_profileSettings.TouchSensitivity` |

---

### 表2: `MainWindowsViewModel.cs`（ID: C9-W01 〜 C9-W08、計8箇所）

| ID | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|
| C9-W01 | 71, 146 | `Global.exedirpath`（2箇所） | (a) | `System.IO.Path.GetDirectoryName(_pathService.ExecutablePath)` |
| C9-W02 | 93 | `Global.appdatapath` | (a) | `_pathService.AppDataPath` |
| C9-W03 | 123 | `Global.IsViGEmBusInstalled()` | (b) | `_environmentService.IsViGEmBusInstalled()` |
| C9-W04 | 124 | `Global.IsRunningSupportedViGEmBus()` | (b) | `_environmentService.IsRunningSupportedViGEmBus()` |
| C9-W05 | 127 | `Global.exelocation` | (a) | `_pathService.ExecutablePath` |
| C9-W06 | 147 | `Global.IsAdministrator()` | (a) | `_environmentService.IsAdministrator()` |
| C9-W07 | 157 | `Global.exeFileName` | (a) | `System.IO.Path.GetFileName(_pathService.ExecutablePath)` |
| C9-W08 | 160 | `Global.AdminNeeded()` | (b) | `_environmentService.AdminNeeded()` |

---

### 表3: `TrayIconViewModel.cs`（ID: C9-T01 〜 C9-T06、実参照6箇所、除外12箇所）

| ID | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|
| C9-T01 | 35 | `Global.exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C9-T02 | 98 | `Global.iconChoiceResources` | (a) | `_appearanceSettingsService.IconChoiceResources` |
| C9-T03 | 98 | `Global.UseIconChoice` | (a) | `_appearanceSettingsService.UseIconChoice` |
| C9-T04 | 99 | `Global.BatteryChanged` | (b) | `_deviceStateService.BatteryChanged`（B12シム） |
| C9-T05 | 199 | `Global.ProfilePath[device]` | (a) | `_profileRepository.ProfilePath[device]` |
| C9-T06 | 255 | `Global.exedirpath` | (a) | `System.IO.Path.GetDirectoryName(_pathService.ExecutablePath)` |
| C9-TEX01〜12 | 48〜92 | 各種トレイアイコンパス定数（12箇所） | 除外 | 真の定数（`const string`、リソースパス） |

---

### 表4: `ProfileSettingsViewModel.cs`（ID: C9-P01 〜 C9-P10、実参照10箇所、除外2箇所）

| ID | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|
| C9-P01 | 2976 | `Global.IsUsingMinViGEm117333()` | (b) | `_environmentService.IsUsingMinViGEm117333()` |
| C9-P02 | 2992, 3780 | `Global.ProfileSettingsServiceInstance` | (a) | コンストラクタ注入済み `_profileSettings` へ一本化（F整理） |
| C9-P03 | 2994 | `Global.OutputSlotServiceInstance` | (a) | コンストラクタ注入済み `_outputSlotService` へ一本化（F整理） |
| C9-P04 | 2995 | `Global.ProfileRepositoryInstance` | (a) | コンストラクタ注入済み `_profileRepository` へ一本化（F整理） |
| C9-P05〜P07 | 3698〜3710 | `Global.exedirpath`（3箇所） | (a) | `System.IO.Path.GetDirectoryName(_pathService.ExecutablePath)` |
| C9-P08 | 4192 | `Global.defaultButtonMapping` | (a) | `_profileSettings.DefaultButtonMapping` |
| C9-P09 | 4196 | `Global.RefreshActionAlias` | (b) | `_specialActionRepository.RefreshActionAlias`（B6シム） |
| C9-P10 | 4200付近 | 残存フォールバック代入 | (a) | 完全削除（コンストラクタ保証） |
| C9-PEX01〜02 | 4181, 4187 | `Global.TEST_PROFILE_INDEX` | 除外 | 真の定数（`const int`） |

---

## 3. アーキテクチャ設計・Pure DI コンストラクタ拡張【旧版・2026-09-18。現行は §0A.3 の決定1・2】

各 ViewModel のコンストラクタを拡張し、クラス内部での `AppHost.GetService<T>()` の都度解決を完全排除する。

```csharp
// SettingsViewModel.cs コンストラクタ設計
public SettingsViewModel(
    IAppSettingsService appSettingsService,
    IEnvironmentService environmentService = null,
    IPathService pathService = null,
    IAppearanceSettingsService appearanceSettingsService = null,
    IProfileSettingsService profileSettingsService = null)
{
    _appSettingsService = appSettingsService;
    _environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();
    _pathService = pathService ?? AppHost.GetService<IPathService>();
    _appearanceSettingsService = appearanceSettingsService ?? AppHost.GetService<IAppearanceSettingsService>();
    _profileSettingsService = profileSettingsService ?? AppHost.GetService<IProfileSettingsService>();
    ...
}
```

```csharp
// TrayIconViewModel.cs コンストラクタ設計
public TrayIconViewModel(
    IProfileSettingsService profileSettingsService,
    IAppearanceSettingsService appearanceSettingsService = null,
    IDeviceStateService deviceStateService = null,
    IProfileRepository profileRepository = null,
    IEnvironmentService environmentService = null,
    IPathService pathService = null)
{
    _profileSettingsService = profileSettingsService;
    _appearanceSettingsService = appearanceSettingsService ?? AppHost.GetService<IAppearanceSettingsService>();
    _deviceStateService = deviceStateService ?? AppHost.GetService<IDeviceStateService>();
    _profileRepository = profileRepository ?? AppHost.GetService<IProfileRepository>();
    _environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();
    _pathService = pathService ?? AppHost.GetService<IPathService>();
    ...
}
```

---

## 4. PR分割計画とマイクロステップ【旧版・2026-09-18。現行は §0A.5】

安全確実に進行するため、以下の**6つのサブPR（マイクロステップ）**に分割して実装する。

```text
【Phase6-Step9 マイクロステップ構成】
├─ Step9-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step9-1 (PR-1): サービスシムの追加整備（B3, B5, B6, B12）
├─ Step9-2 (PR-2): MainWindowsViewModel & TrayIconViewModel の Pure DI 化（14箇所）
├─ Step9-3 (PR-3): SettingsViewModel: 基本設定・テーマ・更新・ログの置換（20箇所）
├─ Step9-4 (PR-4): SettingsViewModel: OSC / UDP / 環境 / パスの置換（35箇所）
├─ Step9-5 (PR-5): ProfileSettingsViewModel の残存参照解消とフォールバック整理（10箇所）
└─ Step9-6 (PR-6): 全体回帰検証・単体テスト追加・Step10引き継ぎ記録・完了報告書作成
```

---

### Step9-0: 着手前提検証・ベースライン記録（実装なし）
1. Step8 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step9-1 (PR-1): サービスシムの追加整備
- **対象**: `IAppSettingsService`, `IEnvironmentService`, `IDeviceStateService`, `ISpecialActionRepository`
- **作業内容**:
  1. `IAppSettingsService` に OSC/UDP/Steam/優先度/ログ関連シム（B3）を追加。
  2. `IEnvironmentService` に ViGEm判定、AdminNeeded、モニター列挙シム（B5）を追加。
  3. `IDeviceStateService` に `BatteryChanged` イベントシム（B12）を追加。
  4. `ISpecialActionRepository` に `RefreshActionAlias` シム（B6）を追加。
- **検証**:
  - `dotnet test` 全件合格を確認。

---

### Step9-2 (PR-2): `MainWindowsViewModel` & `TrayIconViewModel` の Pure DI 化
- **対象**: `MainWindowsViewModel.cs`（C9-W01〜W08）, `TrayIconViewModel.cs`（C9-T01〜T06）
- **作業内容**:
  1. 両 ViewModel のコンストラクタを拡張し、Pure DI 化。
  2. 対象14箇所を注入フィールド参照へ置換。
- **検証**:
  - 単体テスト: トレイアイコンのバッテリー変化イベント購読テストを追加。
  - `dotnet test` 全件合格を確認。

---

### Step9-3 (PR-3): `SettingsViewModel` 基本設定・テーマ・ログの置換
- **対象**: `SettingsViewModel.cs`（C9-S01〜S09, S34〜S35, S46〜S47等、計20箇所）
- **作業内容**:
  1. 排他モード、スワイプ、通知、DCBTatStop, FlashWhenLate, QuickCharge, アイコン、テーマ、ログ設定を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - 設定画面を開き、基本オプションのチェックが正しく反映されることを手動確認。

---

### Step9-4 (PR-4): `SettingsViewModel` OSC / UDP / 環境 / パスの置換
- **対象**: `SettingsViewModel.cs`（C9-S10〜S33, S36〜S45等、計35箇所）
- **作業内容**:
  1. OSC送受信、UDP平滑化、Steamフォルダ、FakeExeName、EDID、プロセス優先度、モニター列挙等を置換。
  2. `SettingsViewModel.cs` 内の Global 直参照をゼロ化。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - OSC/UDP のポート・アドレス変更が正しく保存されることを確認。

---

### Step9-5 (PR-5): `ProfileSettingsViewModel` の残存参照解消とフォールバック整理
- **対象**: `ProfileSettingsViewModel.cs`（C9-P01〜P10、計10箇所）
- **作業内容**:
  1. ViGEm判定、パス、ボタンマッピング、アクションエイリアスを置換。
  2. `Global.*Instance` へのフォールバック代入を完全削除し、コンストラクタ保証へ移行。
- **検証**:
  - `dotnet test` 全件合格を確認。

---

### Step9-6 (PR-6): 全体回帰検証・単体テスト追加・Step10引き継ぎ・完了報告
- **作業内容**:
  1. `SettingsViewModelDiTests.cs`, `TrayIconViewModelDiTests.cs` 等を追加。
  2. 残存する12個の小型 ViewModel の Step10 引き継ぎ台帳を確定。
  3. 完了報告書（`Phase6-Step9-Completion-Report.md`）を作成。
- **検証**:
  - 全自動テスト 100% 合格を確認。

---

## 5. テスト・回帰検証計画【旧版・2026-09-18。現行は §0A.5・§0A.7】

### 5.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストクラスを追加する（1ファイル1型）：
1. **`SettingsViewModelDiTests.cs`**:
   - `SettingsViewModel` がすべてのモックサービスを受け取って正常にインスタンス化されること。
   - OSC/UDP や平滑化プロパティの変更が、`IAppSettingsService` の対応プロパティに即時伝搬されること。
2. **`TrayIconViewModelBatteryTests.cs`**:
   - `IDeviceStateService.BatteryChanged` イベント発火時に、トレイアイコンのバッテリー残量表示・ツールチップが正しく更新されること。
3. **`MainWindowsViewModelViGEmTests.cs`**:
   - `IEnvironmentService.IsViGEmBusInstalled` の戻り値に応じて、UI 上のドライバ状態表示が正確に連動すること。

### 5.2 実機・UI動作検証手順（Phase6-Step11連携）
1. **設定タブの読み込み・保存**:
   - 設定画面を開き、OSC 送信先アドレスや UDP 平滑化設定、テーマを変更して保存。アプリ再起動後に設定が維持されること。
2. **タスクトレイアイコン動作**:
   - コントローラー接続時、トレイアイコンにバッテリー残量が表示され、切断・再接続が追従すること。
3. **メイン画面ステータス表示**:
   - メインウィンドウ下部の ViGEm バス状態や管理者権限状態が正しく表示されること。

---

## 6. Phase6-Step10 への引き継ぎ台帳（残存小型ViewModel群）【2026-09-18 版。境界の現行は §0A.6】

以下の12ファイル（実参照計約30箇所）は、次ステップ **Phase6-Step10** において、対応する View（XAML）とペアで完全に Pure DI 化される：

| ViewModel ファイル名 | 残存参照箇所 | Step10 での Pure DI 移行方針 |
|---|---|---|
| `AboutViewModel.cs` | 1箇所 (`exeversion`) | `IEnvironmentService` をコンストラクタ注入 |
| `AutoProfilesViewModel.cs` | 4箇所 (`AutoProfileRevert*` 等) | `IAutoProfileService`, `IAppSettingsService` をコンストラクタ注入 |
| `BindingWindowViewModel.cs` | 3箇所 (`outDevTypeTemp`, アクション) | `IOutputSlotService`, `IProfileSettingsService` をコンストラクタ注入 |
| `ControllerListViewModel.cs` | 4箇所 (フォールバック `*Instance`) | 注入済みサービスへ一本化（フォールバック完全削除） |
| `ControllerRegDeviceOptsViewModel.cs`| 7箇所 (Moonlight設定, XML保存) | `IAppSettingsService`, `IProfileXmlStore` をコンストラクタ注入 |
| `LanguagePackViewModel.cs` | 3箇所 (`UseLang`) | `IAppSettingsService` をコンストラクタ注入 |
| `LogViewModel.cs` | 1箇所 (`exeversion`) | `IEnvironmentService` をコンストラクタ注入 |
| `MappingListViewModel.cs` | 6箇所 (`GetDS4CSetting` 等) | `IProfileSettingsService` をコンストラクタ注入 |
| `RecordBoxViewModel.cs` | 2箇所 (マクロ設定) | `IProfileSettingsService` をコンストラクタ注入 |
| `RenameProfileViewModel.cs` | 1箇所 (`appdatapath`) | `IPathService` をコンストラクタ注入 |
| `TouchButtonUserControlViewModel.cs` | 2箇所 (`TouchpadButtonMode`) | `IProfileSettingsService` をコンストラクタ注入 |
| `UpdaterWindowViewModel.cs` | 2箇所 (`LastVersionChecked`) | `IAppSettingsService` をコンストラクタ注入 |

---

## 7. ロールバック方針

設定画面やトレイアイコンのバインディング切れ等が発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。

---

## 8. 完了判定チェックリスト【旧版・2026-09-18。現行は §0A.9】

- [ ] 表1〜表4に定義された主要4大ViewModel内の全79箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 表3・表4の除外項目（const定数14箇所）以外の Global 直接参照が4ファイル内に存在しないこと。
- [ ] 4大ViewModelのすべてのコンストラクタが Pure DI 構造に拡張され、クラス内でのサービスロケータ都度解決が排除されていること。
- [ ] `BatteryChanged`（B12）イベントが `IDeviceStateService` に実装され、トレイアイコンで正常に動作すること。
- [ ] 設定画面（OSC/UDP/平滑化/ログ/テーマ等）の全項目が `IAppSettingsService` と連動し、正しく永続化されること。
- [ ] 第6章に定義された残存小型ViewModel（12ファイル）が Step10 へ漏れなく引き継がれていること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。