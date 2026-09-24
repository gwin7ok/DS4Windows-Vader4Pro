# Phase6-Step7 計画書: `App.xaml.cs` Post-Host 領域の Global 直参照解消（Composition Root の整理）

作成日: 2026-09-11  
改訂日: 2026-09-24（Step7-0: Step6 完了後に現行コードと突き合わせ、参照台帳を再作成。旧改訂: 2026-09-18）  
状態: **Step7-0 完了。決定1〜6 はすべてユーザー決定済み（2026-09-24、§4.0）。Step7-1 完了（2026-09-24、コミット `c2cebab2`）。Step7-2 実装済み（ユーザーのビルド・テスト確認待ち）。Step7-3 以降は未着手**  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`（§2.1〜2.4、§3.1、§3.3、§3.4）、`docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.5、§5.5  
調査時点の HEAD: `56f6e8bc`（作業ツリーはクリーン）  
参照エビデンス（旧版の根拠。行番号・件数は本書 §2 で置き換える）:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §5  
  - `Phase6-Step1-ABC-Classification.md`、`Phase6-Step1-Unique-ID-Manifest.md`  

---

## 0. 旧計画書と現行コードの乖離（2026-09-24 に確認）

旧計画書（2026-09-18 改訂）の行番号の多くは現行コードとほぼ一致していた。一方で、存在しない参照、所属メソッドの誤り、境界の誤分類、移行先サービスの挙動差が見つかった。

### 0.1 存在しない参照（旧 ID を削除）
- **旧 C7-30**（1002 行 `Global.outputKBMHandler?.Disconnect()`）: 存在しない。1003 行は `DS4Windows.Util.timeEndPeriod(1)`。仮想 KBM の切断は `ControlService` 側で行われている。これに伴い、旧 §3.1 の `IVirtualKBM _virtualKBM` フィールドも不要。
- **旧 C7-31**（1010 行 `Global.Save()`）: 存在しない。終了処理の `Global.Save()` は 999 行の 1 箇所だけ。
- **旧 C7-32**（1018 行 `Global.ResetConnectionFlags()`）: 存在しない。`ResetConnectionFlags` は起動時の 448 行だけ。

### 0.2 所属メソッドの誤り
- 旧計画の「`Application_Exit`」は、実際には `CleanShutdown()`（981〜1037 行）。`CleanShutdown` は `Application_Exit`、`Application_SessionEnding`、未処理例外ハンドラ（`CurrentDomain_UnhandledException`）の 3 経路から呼ばれる。
- 旧計画の「`SaveWhere` 処理」（旧 C7-23〜C7-28）は、実際には次の 2 メソッド。
  - `CreateConfDirSkeleton()`（536〜553 行）: 初回起動時の設定フォルダ作成。
  - `AttemptSave()`（555〜590 行）: 初回起動時の保存と、書き込めない場合の AppData へのコピー。
  - `SaveWhere` は保存場所選択ダイアログ（`DS4Forms/SaveWhere.xaml.cs`）の名前で、App 側の処理名ではない。

### 0.3 境界の誤分類
- **旧 C7-EX01／EX02**（110〜111 行、`Global.UseLang`／`Global.SetCulture`）は Pre-Host ではない。これらは `ApplyLanguageSetting()`（94〜133 行）の中にあり、このメソッドは次の 3 種類の経路から呼ばれる**共用ヘルパー**である。
  - Post-Host: 327、361、426、459 行
  - Pre-Host: `CheckOptions` の `--driverinstall` 分岐（613 行）
  - App の外: `LanguagePackControl.xaml.cs:74`（公開ラッパー `ApplyLanguageSettingPublic` 経由）
- **旧 C7-EX12** の定数 `Global.MAX_DS4_CONTROLLER_COUNT` の値は 4 ではなく 8（`ScpUtil.cs:638`）。const である点は正しい。

### 0.4 「Pre-Host ＝ DI コンテナが存在しない」という前提の不正確さ
- `AppHost.GetService<T>()`（`DI/AppHost.cs`）は、ホストが未構築なら**その場で `CreateHost()` を呼んで暗黙に構築する**。
- `Global.Load()`／`Global.Save()`（`ScpUtil.cs:3567`、`3728`）は、すでに `IAppSettingsService` への委譲シムになっており、内部で `AppHost.GetService` を呼ぶ。
- このため `--driverinstall` 分岐の `Global.Load()`（608 行）は、明示的な `AppHost.CreateHost(…, parser)`（298 行）より前に、**引数パーサーを登録しないホストを暗黙に構築している**。分岐はその後アプリを終了するため実害はないが、「コンテナ未構築」という旧計画の説明は正確ではない。
- 通常起動の Pre-Host 領域（154〜289 行）で呼ばれる処理（`FindConfigLocation`、`LogRotator`、`LoggerHolder.ApplyBootstrapMinLogLevel`、`AppNotificationRegistration`、`KeyboardSettings`、`RefreshViGEmBusInfo`）は、`AppHost`／`ServiceProviderHolder` に触れないことを確認した。`Global` の静的初期化子も、フォールバック用の実体を `new` するだけでホストを構築しない。
- 結論: 温存の方針は変えない。ただし境界の定義は「DI コンテナが存在しない領域」ではなく、「明示的なホスト構築（298 行）より前に実行される起動処理」とする（§1.2）。

### 0.5 移行先サービスの挙動差（旧計画は想定していなかった）
- **`ISpecialActionRepository.LoadActions()` と `Global.LoadActions()` は同じ動作ではない**（旧 C7-16）。
  - `Global.LoadActions()` → `BackingStore.LoadActions()`（`ScpUtil.cs:9315`）は、`Actions.xml` がない場合、既定のスペシャルアクション「Disconnect Controller」を作って保存し、`true` を返す。
  - `SpecialActionRepository.LoadActions()`（`SpecialActionRepository.cs:54`）は、`Actions.xml` がないと**何もせずに `false` を返す**。
  - そのまま置き換えると、初回起動時（`Actions.xml` がない）に `CreateStdActions()` が走り、**全プロファイルの XML に「Disconnect Controller」を書き足す**という、現行にない動作が起きる。→ 決定2
  - `SpecialActionRepository.LoadActions()` は、アプリ本体・テストとも**呼出元が 0 件**（§2.4 の対象。§3.3 に経緯と判断を記す）。
- **`IPathService.AppDataPath` のセッターは `Global.appdatapath` に書き込まない**（旧 C7-28）。
  - `PathService.AppDataPath` のセッターは、private フィールド `_customAppDataPath` に保存するだけ（`PathService.cs:32`）。以後のゲッターはその値を返すが、`Global.appdatapath` は変わらない。
  - 旧計画どおり `Global.appdatapath = null` を `_pathService.AppDataPath = null` に置き換えると、`Global.appdatapath` が null にならない。→ 決定3
  - このセッターも、アプリ本体・テストとも**呼出元が 0 件**（§2.4 の対象。§3.3）。
- **`IPathService.AppDataPath` のゲッターは、`Global.appdatapath` が空のとき `AppContext.BaseDirectory` を返す**。
  - App 内の読み取り箇所（349、418、541〜543 行）では、その時点で `Global.appdatapath` が必ず設定済みであることを確認した（`FindConfigLocation` の `SaveWhere(…)`、または保存場所ダイアログ `SaveWhere.xaml.cs:51`／`92` の `Global.SaveWhere(…)` が、`ChoiceMade = true` より前に設定する）。したがって実際の動作差はない。
- **`IPathService.ExecutableDirectory` は `Global.exedirpath` と同じ値ではない**。
  - 前者は `AppContext.BaseDirectory`、後者は `Directory.GetParent(Global.exelocation)`（Scoop 等のジャンクションを解決したパス）。
  - 旧計画どおり、`exedirpath` の置き換えには `Path.GetDirectoryName(_pathService.ExecutablePath)`（`ExecutablePath` は `Global.exelocation` への委譲）を使う。`ExecutableDirectory` は使わない。
- **`Global.Save()`／`Global.Load()` はすでに `IAppSettingsService` への委譲シム**。`_appSettingsService.Save()`／`Load()` への置き換えは、ホスト構築後であれば完全に同じ動作（失敗時の `AppLogger.LogToGui` も同じ）。

### 0.6 旧計画が把握していなかった既存の不具合（Step7 の範囲で見つかった回帰）
- **起動引数 `-virtualkbm` が 2026-09-02 以降無視されている**。
  - `ControlService` は、コンストラクタで受け取った `ArgumentParser` の `VirtualkbmHandler` で仮想 KBM の出力方式を決める（`ControlService.cs:569`）。
  - DI 化の前（〜2026-09-01）は、`App.CreateControlService` が実際の `parser` を渡して `new ControlService(parser, …)` していた。
  - コミット `cc7a556b`（2026-09-02、ControlService の DI 化）で、生成が `AppHost.GetService<ControlService>()` に変わり、コミット `4c89cd91`（2026-09-04）で登録が `new ControlService(new ArgumentParser(), …)`（`ServiceRegistration.cs:89〜90`）になった。**空のパーサー**が渡されるため、`-virtualkbm` の指定は常に既定値に置き換わる。
  - 実際の `parser` は `AppHost.CreateHost(config, parser)` で `services.AddSingleton(parser)` として登録済みだが、誰も解決していない。
  - `copilot-instructions.md` §2.2（No Feature Drop）に反する状態。→ 決定5

### 0.7 その他の相違
- 旧 §3.1 のコード例 `AppHost.CreateHost(services => { ServiceRegistration.Register(services, parser); })` は現行と一致しない。現行は `AppHost.CreateHost(new ConfigurationBuilder().Build(), parser)` を try/catch で囲み、失敗は Trace ログだけで握りつぶしている（296〜304 行）。失敗した場合は直後の `CreateControlService` が例外で止まるため、現状でも「ホスト構築の失敗＝起動失敗」である。
- 旧計画の件数「実移行 32／Pre-Host 11／const 1（計 44）」は、同じ行の複数参照や存在しない参照を含む数え方だった。本書では**参照式単位**で数え直した（§2.6）。
- 旧 §5.1 のテスト `AppCompositionRootTests`（`InitializePostHostServices` の結果が非 null であること）は、WPF の `Application` を単体テストで生成できないため、そのままでは作れない。§5 で作り直す。
- 旧 Step7-0 の「エージェントが `dotnet build`／`dotnet test` を実行して記録」は、現在の運用（ビルド・テスト・コミットはユーザーが行う）と合わない。

---

## 1. 目的と境界

### 1.1 目的
1. `App.xaml.cs` の Post-Host 領域（§1.2）にある `Global` 直参照を、DI サービス経由に置き換える。
2. Pre-Host 領域・共用ヘルパー・const を、根拠を明記して静的のまま温存する。
3. Composition Root（`App`）で、起動・終了に必要なサービスを一度だけ解決して保持する形を作る。
4. §0.6 の回帰（`-virtualkbm` が無視される）を扱う（決定5）。

### 1.2 保護対象の境界（現行コードで特定）
- **Pre-Host 領域（温存）**:
  - `Application_Startup` の 143〜304 行。明示的なホスト構築 `AppHost.CreateHost(…, parser)`（298 行）を含む try/catch の終わりまで。
  - `CheckOptions(parser)`（592〜713 行）。233 行で、ホスト構築より前に呼ばれる。`--driverinstall` 分岐は 600〜624 行。
- **共用ヘルパー（Pre-Host と Post-Host の両方から呼ばれる）**: `ApplyLanguageSetting`（94〜133 行）。→ 決定4
- **Post-Host 領域（置き換え対象）**:
  - `Application_Startup` の 306 行（`CreateControlService(parser)`）〜 491 行。
  - `CreateConfDirSkeleton`（536〜553 行）、`AttemptSave`（555〜590 行）。どちらも Post-Host の 347 行・436 行からだけ呼ばれる。
  - `CleanShutdown`（981〜1037 行）の `Global.Save()`。`CleanShutdown` はホスト構築前（起動初期の例外・多重起動以外の早期終了）にも呼ばれうるが、保存は `skipSave == false` のときだけで、`skipSave` が `false` になるのは 450 行（Post-Host）以降に限られる。
- **サービス解決の挿入位置**: 306 行 `CreateControlService(parser);` の直後、307 行 `RenderOptions…` の前。

---

## 2. 参照台帳（現行コードから再作成、HEAD `56f6e8bc`）

- 数え方: `DS4Windows.Global.` または `Global.` によるメンバー参照を、参照式ごとに 1 件と数える。コメント行（544 行）は除く。
- 行番号は HEAD `56f6e8bc` の `DS4Windows/App.xaml.cs`。
- 「新設」は、既存の契約にないため Step7-1 で追加するメンバー（§3.1）。

### 2.1 Post-Host: `Application_Startup` 前半（初回起動・設定読込・ログ）— 11 件
- **P7-01** 310 行 `Global.firstRun` → `_appSettingsService.FirstRun`
- **P7-02** 336 行 `Global.multisavespots` → `_pathService.HasMultipleSaveLocations`（新設）
- **P7-03** 349 行 `Global.appdatapath`（エラーメッセージ） → `_pathService.AppDataPath`
- **P7-04** 356 行 `Global.Load()` → `_appSettingsService.Load()`
- **P7-05** 362 行 `Global.Save()` → `_appSettingsService.Save()`
- **P7-06** 368 行 `Global.exeversion` → `_environmentService.ApplicationVersion`
- **P7-07** 374 行 `Global.exeFileName` → `Path.GetFileName(_pathService.ExecutablePath)`（`Global.exeFileName` の定義式と同じ）
- **P7-08** 418 行 `Global.appdatapath`（ログ） → `_pathService.AppDataPath`
- **P7-09** 426 行 `Global.UseLang` → `_appSettingsService.UseLang`（新設）
- **P7-10** 428 行 `Global.DeviceOptions` → `_appSettingsService.DeviceOptions`
- **P7-11** 430 行 `Global.Save()` → `_appSettingsService.Save()`

### 2.2 Post-Host: `Application_Startup` 後半（プロファイル・アクション・言語・テーマ・環境）— 14 件
- **P7-12** 438 行 `Global.SaveAsProfile(0, "Default")` → `_profileRepository.SaveAsProfile(0, "Default")`（新設）
- **P7-13** 441 行 `Global.ProfilePath[i]` → `_profileRepository.ProfilePath[i]`
- **P7-14** 441 行 `Global.OlderProfilePath[i]` → `_profileRepository.OlderProfilePath[i]`
- **P7-15** 448 行 `Global.ResetConnectionFlags()` → `_deviceStateService.ResetConnectionFlags()`（新設）
- **P7-16** 452 行 `Global.LoadActions()` → 決定2
- **P7-17** 454 行 `Global.CreateStdActions()` → `_specialActionRepository.CreateStandardActions()`（新設）
- **P7-18〜P7-20** 458、459、460 行 `Global.UseLang`（3 件） → `_appSettingsService.UseLang`
- **P7-21〜P7-22** 462、463 行 `Global.UseCurrentTheme`（2 件） → `_appearanceSettingsService.UseCurrentTheme`
  - 462 行のローカル変数 `themeChoice` は使われていないが、本 Step では削除しない（動作に無関係な整理は混ぜない）。
- **P7-23** 467 行 `Global.LoadLinkedProfiles()` → `_profileRepository.LoadLinkedProfiles()`（新設）
- **P7-24** 482 行 `Global.IsAdministrator()` → `_environmentService.IsAdministrator()`
- **P7-25** 485 行 `Global.hidHideInstalled` → `_environmentService.HidHideInstalled`

### 2.3 Post-Host: 初回起動の補助メソッド — 13 件
- **P7-26〜P7-28** `CreateConfDirSkeleton` 541〜543 行 `Global.appdatapath`（3 件） → `_pathService.AppDataPath`
- **P7-29** `AttemptSave` 557 行 `Global.Save()` → `_appSettingsService.Save()`
- **P7-30〜P7-34** `AttemptSave` 564、566、568、569、572 行 `Global.appDataPpath`（5 件） → `_pathService.RoamingAppDataPath`（新設）
- **P7-35〜P7-37** `AttemptSave` 565、567、570 行 `Global.exedirpath`（3 件） → メソッド内のローカル変数 `Path.GetDirectoryName(_pathService.ExecutablePath)`（`ExecutableDirectory` は値が違うため使わない。§0.5）
- **P7-38** `AttemptSave` 585 行 `Global.appdatapath = null`（代入） → 決定3

### 2.4 Post-Host: 終了処理 — 1 件
- **P7-39** `CleanShutdown` 999 行 `Global.Save()` → `_appSettingsService.Save()`

### 2.5 温存（置き換えない）— 15 件
- **Pre-Host（`Application_Startup` 143〜304 行）: 5 件**
  - **E7-01** 154 行 `Global.FindConfigLocation()`（設定の場所の決定。ログローテーションに必要）
  - **E7-02** 157 行 `Global.appdatapath`（起動時ログローテーションの対象フォルダ）
  - **E7-03** 157 行 `Global.LogMaxArchiveFiles`（同上）
  - **E7-04** 162 行 `Global.LogMinLevel`（起動初期のログレベル）
  - **E7-05** 289 行 `Global.RefreshViGEmBusInfo()`（ViGEmBus 情報の取得）
- **Pre-Host（`CheckOptions` の `--driverinstall` 分岐）: 7 件**
  - **E7-06** 604 行 `Global.RefreshViGEmBusInfo()`
  - **E7-07** 607 行 `Global.FindConfigLocation()`
  - **E7-08** 608 行 `Global.Load()`（§0.4 のとおり、ここでホストが暗黙に構築される）
  - **E7-09〜E7-10** 612、613 行 `Global.UseLang`
  - **E7-11〜E7-12** 614、615 行 `Global.UseCurrentTheme`
- **共用ヘルパー `ApplyLanguageSetting`: 2 件**（決定4。推奨案では温存）
  - **E7-13** 110 行 `Global.UseLang = cultureCode`
  - **E7-14** 111 行 `Global.SetCulture(cultureCode)`（スレッドのカルチャを設定するだけの静的ユーティリティ。状態を持たない）
- **const: 1 件**
  - **E7-15** 1025 行 `Global.MAX_DS4_CONTROLLER_COUNT`（`public const int` ＝ 8）

### 2.6 件数のまとめ
- 参照式の合計: **54 件**（`Global.` を含む行は 52 行。157 行と 441 行に 2 件ずつ）
- Post-Host の置き換え対象: **39 件**（P7-01〜P7-39。うち P7-16 は決定2、P7-38 は決定3 の結果で扱いが決まる）
- 温存: **15 件**（Pre-Host 12、共用ヘルパー 2、const 1）

### 2.7 `Global` 以外の静的結合（本 Step の対象外。決定6）
- `Program.rootHub`: 365、483、487、490、507、520、724〜726（代入。App が所有者）、985〜992 行
- `AppHost.GetService<ControlService>()`: 724 行（`CreateControlService` 内。ControlService の生成元）
- `ServiceProviderHolder.Provider` と `IControllerRegistry` の解決: 1019〜1022 行（`CleanShutdown`）
- `ControlService.MAX_DS4_CONTROLLER_COUNT`: 439 行（const）
- 参考: `Global.Save()`／`Global.Load()` の内部の `AppHost.GetService`（§0.4）

### 2.8 Step7b・Step8 と重なる箇所
本 Step は `App.xaml.cs` の中だけを変更し、App の公開メンバー（静的フィールド・公開メソッド・イベント）のシグネチャは変えない。したがって、Step7b・Step8 で編集する `ProfileEditor.xaml.cs`／`MainWindow.xaml.cs` とのファイル上の衝突はない。ただし、次の依存関係がある。
- **`App.logHolder`（静的フィールド）**: `ProfileEditor.xaml.cs` の 24 箇所（114〜2256 行）と `SettingsViewModel.cs:652〜671` が直接参照している。本 Step では変更しない。Step8 で ProfileEditor のロガー参照を整理する場合、この静的フィールドが残っていることを前提にする。
- **`App.requestClient`（静的フィールド）**: `MainWindowsViewModel.cs:54`／`97`、`WelcomeDialog.xaml.cs`、`ScpUtil.cs:3970`／`4029` が参照。本 Step では変更しない。
- **App の公開メソッド・イベント**: `MainWindow.xaml.cs` が `ChangeTheme`（492 行）、`ThemeChanged`（2207 行）、`WriteIPCResultDataMMF`（1470 行）を、`LanguagePackControl.xaml.cs:74` が `ApplyLanguageSettingPublic` を呼ぶ。本 Step では変更しない。
- **`new DS4Forms.MainWindow(parser)`**（468 行）と `window.LateChecks(parser)`（491 行）: Step8 で `MainWindow` のコンストラクタを変える場合、この 2 行を合わせて直す必要がある。本 Step では変更しない。
- **`Global.UseLang` の他の参照**: `ProfileEditor.xaml.cs:280`（Step8 の対象）、`LanguagePackViewModel.cs`、`LanguageSelectDialog.xaml.cs`（Step10 の対象）。本 Step で新設する `IAppSettingsService.UseLang` を、Step8・Step10 でもそのまま使える。
- **App から生成するダイアログ**: `LanguageSelectDialog`、`SaveWhere`、`FirstLaunchUtilWindow`、`WelcomeDialog` は Step10（小型 View）の対象。本 Step では生成方法を変えない。
- **`Global.MAX_DS4_CONTROLLER_COUNT`**: `ProfileEditor` 側にも const の参照がある（Step8 で除外扱い）。本 Step と同じ扱い。

---

## 3. 設計

### 3.1 既存サービスへの契約追加（Step7-1）
既存の実装と同じく、`Global`（`BackingStore`）への薄い委譲とする。独自の状態は持たない。モデル図 03 はこれらのインターフェースの個別メンバーを記載していないため、モデル図の変更は不要。テスト用のモック（`Mock<…>` や手書きの実装）は、5 つの契約とも存在しないことを確認済み。
- **`IPathService`**（実装 `PathService`）
  - `string RoamingAppDataPath { get; }` → `Global.appDataPpath`（`%APPDATA%\DS4Windows`）
  - `bool HasMultipleSaveLocations { get; }` → `Global.multisavespots`（`FindConfigLocation` が設定する「プログラムフォルダと AppData の両方に設定がある」状態）
  - 名前は旧計画の `DefaultAppDataPath`／`HasMultiSaveSpots` から、意味が分かるものに変えた。旧計画の名前を好む場合は戻せる。
- **`IAppSettingsService`**（実装 `AppSettingsService`）
  - `string UseLang { get; set; }` → `Global.UseLang`。セッターは既存のプロパティと同じく、値が変わったときに `SettingChanged` を発行する（購読者は現在 0 件）。
- **`IProfileRepository`**（実装 `ProfileRepository`）
  - `bool SaveAsProfile(int deviceIndex, string profileName)` → `Global.store.SaveAsProfile(…)`（`Global.SaveAsProfile` は戻り値を捨てる `void` だが、実体の `BackingStore.SaveAsProfile` は `bool` を返す。App 側は従来どおり戻り値を使わない）
  - `bool LoadLinkedProfiles()` → `Global.LoadLinkedProfiles()`
- **`IDeviceStateService`**（実装 `DeviceStateService`）
  - `void ResetConnectionFlags()` → `Global.ResetConnectionFlags()`（既存の `IsFirstConnection`／`MarkConnected` と対になる）
- **`ISpecialActionRepository`**（実装 `SpecialActionRepository`）
  - `void CreateStandardActions()` → `Global.CreateStdActions()`
  - `LoadActions()` の扱いは決定2。

### 3.2 Composition Root でのサービス保持（Step7-2）
- `App` に private フィールドを 7 個追加する: `_appSettingsService`、`_pathService`、`_environmentService`、`_profileRepository`、`_specialActionRepository`、`_appearanceSettingsService`、`_deviceStateService`（旧計画の `IVirtualKBM` は不要になった。§0.1）。
- `private void InitializePostHostServices()` を追加し、§1.2 の挿入位置（306 行の直後）で 1 回だけ呼ぶ。
  - 各サービスは `AppHost.GetService<T>()` で解決し、null なら `InvalidOperationException` を投げる（`CreateControlService` の 725〜726 行と同じ扱い。現状でもホスト構築の失敗は起動失敗なので、動作は変わらない）。
  - 解決後に `[DI]` 接頭辞付きの Trace ログを 1 行出す（`copilot-instructions.md` §2.3）。
- `App` は WPF の `Application` で、コンストラクタ注入ができない。**Composition Root 自身がコンテナから解決するのは Service Locator ではなく、Pure DI の正規の形**（`copilot-instructions.md` §3.1 の禁止対象は、ViewModel やサービスが `AppHost.Services` を持ち込むこと）。

### 3.3 呼出元 0 件のコードの判断（`copilot-instructions.md` §2.4）
- **`SpecialActionRepository.LoadActions()`**
  - 経緯: コミット `c06a1c9b`（2026-08-31、Phase4-Step3「ISpecialActionRepository の実装とサービス登録」）で、Strangler Fig 移行用の契約として作られた。App 側の切り替えは行われず、今日まで未接続。
  - 判断: **使うべきもの**。App（第4層）が `Global` を直接呼ぶ箇所を、抽象経由に置き換えるための契約である。ただし `Actions.xml` がない場合の動作が `Global.LoadActions()` と違う（§0.5）。接続する前に、この差を是正する必要がある。→ 決定2
- **`PathService.AppDataPath` のセッター**
  - 経緯: `PathService` の初期実装からある。テスト用のコンストラクタ引数 `appDataPath` と同じ private フィールドに書き込む作りで、`Global.appdatapath` とは連動しない。
  - 判断: **使うべきでないもの**（Phase5-Step13 で是正した「`Global` と連動しない孤立した状態」と同じ型の実装）。App の 585 行の置き換え先にもならない。→ 決定3

### 3.4 `-virtualkbm` の回帰の是正（決定5＝案 A）
- `ServiceRegistration.cs:89〜90` の `new ArgumentParser()` を、`sp.GetService<ArgumentParser>() ?? new ArgumentParser()` に変える。
  - 通常起動: `AppHost.CreateHost(config, parser)` が登録した実際の `parser` が渡る（2026-09-01 以前の動作に戻る）。
  - `CreateHost()`（引数なし）やテストで作ったホスト: パーサーが登録されていないので、従来どおり空のパーサー。
- 変更は 1 行。`App.xaml.cs` は変えない。

---

## 4. 決定が必要な事項

### 4.0 ユーザー決定の記録（2026-09-24）
- **決定1: 案 A**（フィールドに保持し、`InitializePostHostServices()` で 1 回だけ解決する）
- **決定2: 案 L2**（`SpecialActionRepository.LoadActions()` の `File.Exists` による早期 `return false` を削除して `Global` と同じ動作にし、App から使う）
- **決定3: 案 P1**（585 行は温存して TODO。`PathService.AppDataPath` のセッターは Step12 の削除候補に登録）
- **決定4: 案 K**（`ApplyLanguageSetting` の 2 件は温存して TODO）
- **決定5: 案 A**（`-virtualkbm` の回帰を、本 Step の独立したマイクロステップ Step7-4 で是正する。§3.4 の 1 行の変更）
  - 当初は保留とし、起動オプションの役割と影響範囲の説明を受けたうえで決定した。
  - 起動オプションの役割: キーボード・マウス出力の方式（`sendinput`＝Windows 標準の SendInput、`fakerinput`＝FakerInput ドライバーの仮想デバイス）を起動時に指定する。指定しない場合は、FakerInput が導入済みなら `fakerinput`、なければ `sendinput` を自動で選ぶ。不正な値は無視され、`fakerinput` の接続に失敗した場合は `sendinput` に切り替わる。画面上の設定項目はなく、起動オプションだけで指定する。
  - 現状の影響: 指定が常に「自動選択」に置き換わる。実際に困るのは、FakerInput を導入した環境で `sendinput` を強制したい場合（ゲームとの相性の回避など）。
- **決定6: 案 N ＋ 後の Step／Phase で Pure DI 化**
  - 本 Step では `Program.rootHub` などの `Global` 以外の静的結合（§2.7）を変更せず、台帳に記録するだけにする。
  - ただし放置はせず、後の Step または Phase で、Pure DI の方針に沿う形（`App` がコンテナから `ControlService` を受け取り、`Program.rootHub` という静的な入口に頼らない構成）へ変更する。引き継ぎ先の計画書は Step7-5 で決めて登録する（候補: `Phase6-Step12-Plan.md` の Phase7 引き継ぎ、または `DI-App-Wide-Migration-Plan.md` の Phase7 項目）。

### 前提（全決定に共通）
- `App.xaml.cs` は アプリの起点（Composition Root）で、`ServiceRegistration` に登録された各サービスを組み立てて使う場所である。
- 本 Step の目的は「Post-Host の `Global` 直参照を DI サービス経由にする」こと。挙動は変えない（`copilot-instructions.md` §2.2）。
- 置き換え先のサービスが、置き換え元の `Global` と**完全に同じ動作をする**ことが前提になる。§0.5 で、この前提が成り立たない箇所が 2 つ見つかった（決定2、決定3）。

### 決定1: Post-Host で使うサービスの持ち方
- **案 A（推奨）: フィールドに保持し、`InitializePostHostServices()` で 1 回だけ解決する**（§3.2）
  - メリット: 解決箇所が 1 つにまとまり、`CleanShutdown` でも同じインスタンスを使える。解決失敗が起動直後に明示的な例外になる。
  - デメリット: フィールドが 7 個増える。
- 案 B: 使う箇所ごとに `AppHost.GetService<T>()` を呼ぶ
  - メリット: フィールドが増えない。
  - デメリット: 解決が約 30 箇所に散らばり、Service Locator の呼び出しが増える。Pure DI の方針に反する方向。
- 案 C: `Application_Startup` のローカル変数で持ち、補助メソッドには引数で渡す
  - メリット: フィールドが増えない。
  - デメリット: `AttemptSave`／`CreateConfDirSkeleton`／`CleanShutdown` のシグネチャが変わり、`CleanShutdown`（3 経路から呼ばれる）では別途解決が必要になる。
- **推奨理由**: 旧計画の方針と同じで、`CleanShutdown` を含むすべての経路で同じ扱いにできる。

### 決定2: `LoadActions`（P7-16）の置き換え方
- 前提: 現行の `Global.LoadActions()` は、`Actions.xml` がないと既定のアクションを作って `true` を返す。`SpecialActionRepository.LoadActions()` は `false` を返すだけで、そのまま置き換えると、初回起動で全プロファイルの XML が書き換わる（§0.5）。
- **案 L2（推奨）: `SpecialActionRepository.LoadActions()` の早期 `return false`（`File.Exists` の判定）を削除して `Global` と同じ動作にし、App から使う**
  - メリット: 呼出元 0 件の契約を、正しい動作にしてから接続できる（§2.4 の「使うべきもの」の手順どおり）。App の `Global` 参照が 1 件減る。
  - デメリット: `SpecialActionRepository.LoadActions()` の動作が変わる（呼出元 0 件なので影響はない）。また、このメソッドは想定外の例外もすべて `false` にする（`Global` 側は XML・操作の例外以外は外へ投げる）。つまり、現行なら起動が止まる種類の例外（ディスク I/O など）のとき、`CreateStdActions()` に進むようになる。この差は残る。
- 案 L1: P7-16 は `Global.LoadActions()` のまま温存し、TODO を付ける
  - メリット: 動作の変化がまったくない。
  - デメリット: `Global` 参照が 1 件残る。未接続の契約も残る（接続作業を別の Step に登録する必要がある）。
- 案 L3: L2 に加えて、例外の扱いも `Global` 側に合わせる（XML・操作の例外だけを `false` にし、他は外へ投げる）
  - メリット: 完全に同じ動作になる。
  - デメリット: リポジトリの例外処理が、他のメソッド（`SaveActions` など）と揃わなくなる。
- **推奨理由**: 実際に起きるのは `Actions.xml` がないケース（初回起動）で、これは L2 で完全に同じ動作になる。想定外の例外の差は「起動が止まる」か「標準アクションの作成に進む」かで、後者のほうが安全側である。

### 決定3: `Global.appdatapath = null`（P7-38）と `PathService.AppDataPath` のセッター
- 前提: 585 行は、初回起動で設定を保存できなかったときに、保存先を無効にしてアプリを終了する処理の一部。`PathService.AppDataPath` のセッターは `Global.appdatapath` を変えないので、置き換え先にならない（§0.5）。
- **案 P1（推奨）: 585 行は `Global.appdatapath = null` のまま温存して TODO を付ける。セッターは §2.4 の「使うべきでないもの」として、`Phase6-Step12-Plan.md` の削除候補に登録する（本 Step では触らない）**
  - メリット: 挙動の変化がない。失敗時だけ通る 1 行のために契約を変えずに済む。
  - デメリット: Post-Host に `Global` 参照が 1 件残る。
- 案 P2: セッターを `Global.appdatapath` へ書き込むように直し、585 行を `_pathService.AppDataPath = null` にする
  - メリット: Post-Host の `Global` 参照が 0 件になる。
  - デメリット: テスト用コンストラクタ（`_customAppDataPath`）との関係を設計し直す必要がある。null を書いた後、ゲッターは `AppContext.BaseDirectory` を返すので、「書いた値が読めない」セッターになる。
- 案 P3: 585 行は P1 と同じく温存し、セッターは本 Step の中で削除する
  - メリット: 誤用の地雷をすぐ取り除ける。
  - デメリット: 本 Step の範囲（App の置き換え）と関係の薄い契約変更が混ざる。
- **推奨理由**: P2 は 1 行のために契約の意味を変えることになり、割に合わない。削除は Step12（呼出元 0 件の旧 API の整理）の趣旨に合う。

### 決定4: `ApplyLanguageSetting` の 2 件（E7-13、E7-14）
- 前提: このメソッドは、Pre-Host（`--driverinstall` 分岐）、Post-Host、App の外（言語パック画面）の 3 種類から呼ばれる（§0.3）。
- **案 K（推奨）: 温存し、共用ヘルパーである理由を TODO コメントで残す**
  - メリット: `--driverinstall` 分岐（Pre-Host）で、フィールドがまだ null の状態を気にしなくてよい。
  - デメリット: `Global` 参照が 2 件残る。
- 案 S: `UseLang` の書き込みを `_appSettingsService` 経由にし、フィールドが null のとき（Pre-Host から呼ばれたとき）だけ `Global` へ書く
  - メリット: Post-Host から呼ばれたときは DI 経由になる。
  - デメリット: 同じ処理に 2 本の経路ができる。`SetCulture` はサービスの状態ではない静的ユーティリティなので、置き換え先がない。
- **推奨理由**: 分岐を増やす利点が小さい。Phase7 で `Global` を解体するときに、ヘルパーごと整理するのが自然。

### 決定5: `-virtualkbm` の回帰（§0.6）
- 前提: DI 化の過程（2026-09-02）で、起動引数 `-virtualkbm` が効かなくなった。移行作業そのものが原因の機能欠落で、`copilot-instructions.md` §2.2 に反する。
- **案 A（推奨）: 本 Step の独立したマイクロステップ（Step7-4）で是正する**（§3.4、1 行の変更）
  - メリット: Composition Root の配線の不具合で、本 Step の主題と一致する。変更が小さく、影響の範囲がはっきりしている。
  - デメリット: 本 Step の範囲が少し広がる。`-virtualkbm` を使う実機確認が 1 項目増える。
- 案 B: 本 Step では扱わず、持ち越し事項として `Phase6-Status.md` に記録し、別の Step で是正する
  - メリット: 本 Step の範囲を `App.xaml.cs` に限定できる。
  - デメリット: 既知の機能欠落が残る期間が延びる。
- **推奨理由**: 原因箇所（`ServiceRegistration.cs`）は `copilot-instructions.md` §3.1 で DI 登録の集約先と定められた Composition Root の一部で、本 Step の主題そのものである。

### 決定6: `Program.rootHub` などの `Global` 以外の静的結合（§2.7）
- **案 N（推奨）: 本 Step の対象外とし、台帳に記録するだけにする**
  - メリット: 本 Step の範囲が旧計画（`Global` 直参照の解消）どおりになる。`Program.rootHub` は約 15 ファイルが参照する共有の入口で、App の中だけ置き換えても結合は減らない。
  - デメリット: App の中に静的結合が残る。
- 案 Y: `ControlService` もフィールドに保持し、App 内の `Program.rootHub` の読み取り（代入の 724 行以外）を置き換える
  - メリット: App の中の見通しがよくなる。
  - デメリット: 未処理例外ハンドラ（507、520 行）はホスト構築前にも呼ばれうるため、null の扱いを個別に確認する必要がある。旧計画の範囲外。
- **推奨理由**: `Program.rootHub` の解消は、参照している全ファイルを見渡して行う必要があり、App だけを先に変える利点が小さい。

---

## 5. マイクロステップ

各ステップの後に、ユーザーが `dotnet build`・テストビルド・`dotnet test`（必要に応じて実機確認）を行い、結果を確認してから次へ進む。コミットもユーザーが行う。

```text
【Phase6-Step7 マイクロステップ構成】
├─ Step7-0: 調査・台帳の再作成・計画改訂（本書。完了）
├─ Step7-1: 既存サービスへの契約追加（§3.1）と LoadActions の是正（決定2）
├─ Step7-2: Composition Root のサービス保持と、起動前半・初回起動補助の置き換え（P7-01〜P7-11、P7-26〜P7-38）
├─ Step7-3: 起動後半・終了処理の置き換えと、温存箇所への TODO（P7-12〜P7-25、P7-39、E7-13〜E7-14）
├─ Step7-4: -virtualkbm の回帰の是正（決定5＝案 A）
└─ Step7-5: 文書の更新・完了報告
```

### Step7-0: 調査・台帳の再作成・計画改訂【完了（2026-09-24）】
- HEAD `56f6e8bc`、作業ツリーはクリーン。
- §0（乖離）、§2（台帳）、§4（決定事項）を作成。

### Step7-1: 契約追加と LoadActions の是正【完了（2026-09-24、ビルド・テストビルド・テスト実行成功、コミット `c2cebab2`）】
- §3.1 の 7 メンバーを、5 つのインターフェースと実装に追加する。
- 決定2＝L2 に従い、`SpecialActionRepository.LoadActions()` の早期 `return false`（`File.Exists` の判定）を削除し、§3.3 の経緯を説明コメントとして付ける。
- テスト（新規）: `Phase6Step7ContractExtensionTests`
  - `RoamingAppDataPath`・`HasMultipleSaveLocations`・`UseLang` が `Global` と同じ値を返し、書き込みが `Global` に反映されること（テストの最後に元の値へ戻す。`Phase6-Status.md` §6.4-1）。
  - `ResetConnectionFlags` の後、全スロットが `IsFirstConnection == true` になること（`MarkConnected` 後に呼んで確認し、終了時に元へ戻す）。
  - `LoadActions`（決定2＝L2）: `Actions.xml` がない場所を指す `BackingStore` を渡したとき、既定のアクションが作られて `true` が返ること（一時フォルダを使う）。
  - `SaveAsProfile`・`LoadLinkedProfiles`・`CreateStandardActions`: 実ファイルを書き換えるため、契約の存在と委譲先のソース走査で確認する（Step3-1 の `SaveControllerConfigsForDevice` と同じ扱い）。
- **実装結果（2026-09-24 実装、ユーザーのビルド・テスト確認待ち）**:
  - 契約（`DS4Windows/DI/`）: `IPathService`（`RoamingAppDataPath`、`HasMultipleSaveLocations`）、`IAppSettingsService`（`UseLang`）、`IProfileRepository`（`SaveAsProfile`、`LoadLinkedProfiles`）、`IDeviceStateService`（`ResetConnectionFlags`）、`ISpecialActionRepository`（`CreateStandardActions`）の計 7 メンバー。
  - 実装（`DS4Windows/DS4Control/Services/`）: いずれも `Global` への薄い委譲。`AppSettingsService.UseLang` のセッターは、他のプロパティと同じく値が変わったときだけ `SettingChanged` を発行する。`ProfileRepository.SaveAsProfile` は、戻り値を捨てる `Global.SaveAsProfile` ではなく同じ実体の `Global.store.SaveAsProfile` を呼んで成否を返す。`SpecialActionRepository.CreateStandardActions` は `[DI]` 付きの Trace ログを出す。
  - `SpecialActionRepository.LoadActions()`（決定2＝L2）: 冒頭の `if (!File.Exists(ActionsPath)) return false;` を削除し、経緯と残る差（想定外の例外も `false` にする点）をコメントに記録した。それ以外の処理（ロック、`[DI]` Trace ログ、`ActionsChanged` の発行、例外時の `false`）は変更していない。
  - 呼び出し側（`App.xaml.cs`）はまだ変更していない（Step7-2・7-3 で行う）。この時点では、新しいメンバーと是正した `LoadActions` の呼出元は、テストだけである。
  - テスト用のモック（`Mock<…>`・手書きの実装）は 5 つの契約とも存在しないため、テスト側の追随は不要（`Phase6-Status.md` §6.4-4 を確認済み）。モデル図の変更なし。
  - テスト（新規）: `DS4WindowsTests/Phase6Step7ContractExtensionTests.cs`（8 件）。`RoamingAppDataPath`・`HasMultipleSaveLocations`・`UseLang`（通知を含む）・`ResetConnectionFlags` の `Global` との状態共有、`LoadActions` が `Actions.xml` のない一時フォルダで既定アクション「Disconnect Controller」を作って `true` を返し、作ったファイルを別の `BackingStore` から読み込めること、`SaveAsProfile`／`LoadLinkedProfiles`／`CreateStandardActions` の委譲先と `LoadActions` に `File.Exists` が再混入していないことのソース走査。
  - 実機確認: この時点では App から呼ばれないため不要。Step7-2・7-3 の後に §6 の項目で確認する。
  - **テスト実行時の失敗と修正（2026-09-24）**: 既存テスト `MappingHotPathAllocationTests.ApplyStickCalibration_DoesNotAllocate` が「784 バイト／20000 回」で失敗した。
    - Step7-1 の変更とは無関係と判断した。`ApplyStickCalibration`（`Mapping.cs:2726`）は配列の読み取りと `Math.Clamp` だけで、Step7-1 で変更したコードを通らない。
    - 1 回の呼び出しごとの割り当てなら 20000 バイト以上になるはずで、784 バイトは計測区間の途中で**一度だけ**起きた割り当て（階層型 JIT・OSR による再コンパイル等、ランタイム側の一過性の処理）と考えられる。新しいテストクラスの追加で xUnit の実行順序とタイミングが変わり、表面化したとみられる。
    - 修正: 同クラスの 3 テストの計測を、ヘルパー `MeasureMinAllocatedBytes`（最大 3 回計測して最小値を採る）に置き換えた。呼び出しごとの割り当てはどの計測回にも現れるため、検出力は落ちない。本番コードは変更していない。

### Step7-2: サービス保持と、起動前半・初回起動補助の置き換え【実装済み・ビルド・テスト確認待ち（2026-09-24）】
- §3.2 のフィールドと `InitializePostHostServices()` を追加する。
- P7-01〜P7-11（起動前半）と P7-26〜P7-37（`CreateConfDirSkeleton`／`AttemptSave`）を置き換える。
- P7-38 は決定3＝P1 に従い、`Global.appdatapath = null` のまま温存して TODO を付ける。
- テスト（新規）: `AppPostHostGlobalReferenceGuardTests`（ソース走査ガード）
  - `App.xaml.cs` の `Global.` 参照が、§2.5 の温存箇所（と、この時点で未置換の P7-12〜P7-25、P7-39）以外にないこと。行番号ではなく、メソッド名と、`CreateControlService(parser);` より前か後かで判定する。
  - `InitializePostHostServices();` が `CreateControlService(parser);` の直後にあること。
- **実装結果（2026-09-24 実装、ユーザーのビルド・テスト確認待ち）**:
  - `App.xaml.cs` に private フィールド 7 個と `InitializePostHostServices()`、解決用ヘルパー `ResolvePostHostService<T>()`（`AppHost.GetService<T>()` が null なら型名入りの `InvalidOperationException`）を追加。`CreateControlService(parser);` の直後で 1 回呼ぶ。解決後に `[DI] App.InitializePostHostServices` の Trace ログを出す。
  - 置き換え 23 件: P7-01〜P7-11（起動前半 11 件）、P7-26〜P7-28（`CreateConfDirSkeleton`。ローカル変数 `appDataPath` に 1 回読んで 3 箇所で使う）、P7-29〜P7-37（`AttemptSave`。ローカル変数 `roamingAppDataPath`・`exeDirPath` に 1 回ずつ読んで使う。`exeDirPath` は `Path.GetDirectoryName(_pathService.ExecutablePath)` で、`ExecutableDirectory` を使わない理由をコメントに記載）。
  - P7-38 は `Global.appdatapath = null` のまま、決定3＝P1 の TODO コメントを付けて温存。
  - Pre-Host 領域・`CheckOptions`・`ApplyLanguageSetting` は変更していない。
  - この時点の `Global.` 参照は 31 件（温存 15 件＋P7-38＋Step7-3 で置き換える 15 件）。
  - テスト（新規）: `DS4WindowsTests/AppPostHostGlobalReferenceGuardTests.cs`（3 件）。領域（`Application_Startup` の Pre-Host／Post-Host、`CheckOptions`、`ApplyLanguageSetting`、`CreateConfDirSkeleton`、`AttemptSave`、`CleanShutdown`）ごとの `Global` メンバー参照の件数を期待値と照合し、ファイル全体の件数が合計と一致すること（他の場所に増えていないこと）、`InitializePostHostServices();` が `CreateControlService(parser);` の直後に 1 回だけあること、7 サービスが `AppHost` から解決できることを検証する。Step7-3 で Post-Host と `CleanShutdown` の期待値を最終形に更新する。
  - 実機確認: §6.1 の 1・2・6、§6.2 の 8〜10（起動・ログ・終了時の保存と、Pre-Host の非変更の確認）。初回起動（§6.3）は Step7-3 の後にまとめて行う。
  - 挙動の同一性: `Global.Save()`／`Load()` は元から `IAppSettingsService` への委譲、`exeversion`／`DeviceOptions`／`firstRun`／`UseLang`／`appDataPpath`／`multisavespots` は同じ `Global` の値を返す委譲、`exeFileName` は定義式と同じ `Path.GetFileName(Global.exelocation)`。`AppDataPath` のゲッターは `Global.appdatapath` が空のとき `AppContext.BaseDirectory` を返すが、置き換えた箇所では必ず設定済み（§0.5）。

### Step7-3: 起動後半・終了処理の置き換えと TODO
- P7-12〜P7-25、P7-39 を置き換える（P7-16 は決定2＝L2 により `_specialActionRepository.LoadActions()` へ置き換える）。
- 温存する箇所（決定3 の P7-38、決定4 の E7-13〜E7-14）に、`copilot-instructions.md` §3.3 原則4 の TODO コメント（理由、経緯、解消予定の Phase）を付ける。Pre-Host の 12 件には、境界の説明コメントを 1 箇所（298 行付近）に付ける。
- `AppPostHostGlobalReferenceGuardTests` を最終形（Post-Host の `Global.` 参照は温存を決めた箇所だけ）に更新する。

### Step7-4: `-virtualkbm` の回帰の是正（決定5＝案 A）
- §3.4 の 1 行を変更する。
- テスト（新規）: `ServiceRegistrationArgumentParserTests`
  - `ArgumentParser` を登録したサービスコレクションから `ControlService` を解決したとき、登録したパーサーが使われること。
  - 登録していない場合は、従来どおり空のパーサーで生成できること。
  - `ControlService` を実際に生成するテストが重い場合は、`ServiceRegistration.cs` のソース走査ガードに切り替える（実装時に判断して報告する）。

### Step7-5: 文書の更新・完了報告
- `Phase6-Status.md`、`Phase6-Plan.md` を更新する。
- 決定3＝P1 に従い、`Phase6-Step12-Plan.md` に `PathService.AppDataPath` のセッターの削除候補を登録する。
- 決定6 に従い、`Program.rootHub` 等（§2.7）の Pure DI 化を後続の計画書（Phase6 の後続 Step または Phase7）に登録する。
- 実機確認できなかった項目を `Phase6-Step11-Plan.md` §3.3 の先送り台帳に登録する。
- 完了報告書 `Phase6-Step7-Completion-Report.md` を作成する。

---

## 6. 実機確認項目

### 6.1 各ステップ共通（通常起動・終了）
1. 既存の設定がある状態で起動し、メインウィンドウが表示される。保存済みのプロファイル・言語・テーマが反映される。
2. ログに `DS4Windows version …`、`DS4Windows exe file: DS4Windows.exe`、`Running as Admin`／`Running as User` が従来どおり出る。
3. HidHide を導入している環境で、HidHide の確認処理（`CheckHidHidePresence`）のログが従来どおり出る。
4. コントローラーを接続し、割り当てたプロファイルが適用される（`ResetConnectionFlags` の確認。起動後の初回接続で、接続時の処理が従来どおり行われる）。
5. スペシャルアクションの一覧が従来どおり表示され、動作する（`LoadActions` の確認）。
6. 設定を変更してからアプリを終了し、`Profiles.xml` の更新日時が変わる。再起動で変更が残っている（`CleanShutdown` の保存）。
7. トレイメニューの Exit で終了しても、6 と同じく保存される。

### 6.2 起動引数と Pre-Host（変更していないことの確認）
8. `-m`（最小化起動）でトレイアイコンだけの状態で起動する。
9. 起動中にもう 1 つ起動すると、既存のウィンドウが前面に出て、2 つ目は終了する（多重起動判定）。
10. `--driverinstall` で、ドライバーのインストール画面（WelcomeDialog）がクラッシュせずに開き、閉じるとアプリが終了する。

### 6.3 初回起動（設定フォルダを一時的に退避して確認。環境の準備が難しければ Step11 へ先送り）
11. 言語選択 → 保存場所の選択 → 初回ユーティリティ画面 → 既定プロファイル「Default」の作成、の順に進み、メインウィンドウが開く。
12. `Actions.xml` が作られ、スペシャルアクション「Disconnect Controller」が 1 件ある。**既存プロファイルの XML に「Disconnect Controller」が追記されていない**（決定2 の確認）。
13. プログラムフォルダと AppData の両方に設定がある状態で起動すると、保存場所の選択画面に「両方にある」旨の選択肢が出る（`HasMultipleSaveLocations`）。

### 6.4 `-virtualkbm`（決定5＝案 A）
14. `-virtualkbm sendinput`（または環境にある別の方式）を付けて起動し、ログに出る仮想 KBM の方式が指定どおりになる。引数なしでは従来の既定の方式になる。

### 6.5 実機確認が難しい項目（Step11 へ先送りする見込み）
- 書き込みできない場所に設定がある場合の `AttemptSave` のコピー処理（P7-29〜P7-38）。
- Windows のサインアウト・シャットダウン時の保存（`Application_SessionEnding` → `CleanShutdown`）。

---

## 7. リスクと対策
- **サービス解決の失敗**: 現状でもホスト構築の失敗は起動失敗なので、`InitializePostHostServices` の例外は動作を悪化させない。例外メッセージに解決できなかった型の名前を含める。
- **`CleanShutdown` がホスト構築前に呼ばれる場合**: 保存は `skipSave == false`（450 行以降）のときだけで、そのときはフィールドが必ず設定済み。念のため、保存の呼び出しは `_appSettingsService` が null でないことを確認してから行う。
- **初回起動経路の確認の難しさ**: ソース走査ガードで置き換えの形を固定し、実機確認は §6.3 のとおり、可能な範囲で行う。
- **Step7b・Step8 との衝突**: §2.8 のとおり、App の公開メンバーは変えないので、ファイル上の衝突はない。

## 8. ロールバック方針
- マイクロステップ単位で `git revert` する。Pre-Host 領域には触れないため、切り戻しで起動経路が壊れることはない。
- Step7-1 の契約追加は、Step7-2・7-3 を戻した後でも残してよい（呼出元がなくなるだけ）。

## 9. 見積り
- Step7-1: 約 2 時間（契約 7 メンバー、LoadActions の是正、テスト）
- Step7-2: 約 2 時間（フィールドと解決、24 件の置き換え、ガードテスト）
- Step7-3: 約 1.5 時間（15 件の置き換え、TODO、ガードテストの更新）
- Step7-4: 約 1 時間（1 行とテスト）
- Step7-5: 約 1.5 時間
- **合計: 約 1 日**（実機確認の時間を除く）

## 10. 完了判定チェックリスト
- [x] Step7-0: 台帳の再作成と計画改訂（2026-09-24）
- [x] 決定1〜6 がユーザーに確認され、本書 §4.0 に記録されている（2026-09-24）
- [x] 決定5 が確定している（2026-09-24、案 A）
- [ ] 決定6 の引き継ぎ（`Program.rootHub` 等の Pure DI 化）が、後続の計画書に登録されている
- [ ] P7-01〜P7-39 が、決定どおり DI サービス経由に置き換えられている（温存を決めた箇所には TODO がある）
- [ ] E7-01〜E7-15（Pre-Host 12、共用ヘルパー 2、const 1）が、決定どおり温存されている
- [ ] Pre-Host 領域（143〜304 行、`CheckOptions`）のコードが変更されていない
- [ ] 決定5 の対応が完了している（案 A: 是正済み、案 B: 持ち越し事項に記録済み）
- [ ] `dotnet build`・テストビルド・`dotnet test` がすべて成功している
- [ ] 実機確認（§6）が完了している、または `Phase6-Step11-Plan.md` §3.3 に登録されている
- [ ] `Phase6-Status.md`／`Phase6-Plan.md` を更新し、`Phase6-Step7-Completion-Report.md` を作成している
