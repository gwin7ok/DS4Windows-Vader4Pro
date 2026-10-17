# Phase7-Step4 計画書: 起動・終了シーケンスの近代化

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-Plan.md` §2（Step4）、`Phase7-0-Survey.md` §1.4・§1.9・§1.10・D3（S1: コンテナ構築の前にブートストラップを作り、同一インスタンスを登録する）、`Phase7-Step3c-Completion-Report.md` §5  
状態: **Q1〜Q7 を推奨どおり承認（2026-10-17）。W1〜W5 完了。結果と計画との差は `Phase7-Step4-Completion-Report.md`。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 0. 結論（先に）

- 目的: 起動処理（Pre-Host）で `Global` の静的な状態に頼っている部分を、コンテナ構築の前に作る **ブートストラップのサービス**（`PathService`／`EnvironmentService`）に置き換え、同じインスタンスをコンテナへ登録する（D3＝S1）。あわせて `AppHost` の整理、ホストの破棄、`-driverinstall` の暗黙のホスト構築の解消を行う。
- 対象は 3 つのまとまり。
  - **環境の調べ物（プローブ）**: ViGEm／HidHide／FakerInput／OS／管理者権限の判定を、`Global` から `EnvironmentService` へ移す。
  - **保存場所と実行ファイルの情報**: `FindConfigLocation`／`SaveWhere`／`SaveDefault`、`appdatapath`／`exedirpath`／`exelocation`／`exeversion`／`firstRun`／`multisavespots` を、`PathService` が所有する形にする。
  - **起動・終了の手順**: `AppHost` の整理、`Dispose` の呼び出し、`-driverinstall`／`-reenableDevice` の分岐。
- 起動順序を変えるため、Phase7 でも実機確認の重要度が高い。特性化テストを先に書ける部分は書くが、`FindConfigLocation` は実行環境のフォルダに依存するため、入力を注入できる形にしてからテストする（§2）。
- 効果の見込み: `Global.*` 488 → 約 430（確定値は W1 の冒頭の走査で出す）。

## 1. 調査結果

### 1.1 現行の起動シーケンス（`App.Application_Startup`）
1. 通知の登録（`AppNotificationRegistration.Initialize`）。
2. **Pre-Host の最初の塊**: `Global.FindConfigLocation()` → `LogRotator.PerformStartupRotation(Global.appdatapath, Global.LogMaxArchiveFiles)` → `LoggerHolder.ApplyBootstrapMinLogLevel(Global.LogMinLevel)`。
3. 引数の解析（`--complete-install` の処理、`ArgumentParser.Parse`）→ `CheckOptions`（`-driverinstall`／`-reenableDevice`／`-runtask` ほか。ここで `exitApp` なら終了）。
4. 多重起動の検査、プロセス優先度、`timeBeginPeriod`、キーボード設定、`Global.RefreshViGEmBusInfo()`、通信用のイベントとスレッド。
5. `AppHost.CreateHost(config, parser)`（明示的なホスト構築）→ `CreateControlService`（`Program.rootHub` に代入）→ `InitializePostHostServices`。
6. 初回起動なら言語選択と保存場所の選択（`PathService`／`EnvironmentService` を渡して表示）→ 設定の読込（`AppSettingsService.Load`）→ `LoggerHolder` の生成。
7. 以降は Post-Host（サービス経由）。終了は `CleanShutdown`（`rootHub` の停止と `ShutDown`、設定の保存、通信スレッドの終了、コントローラーレジストリの破棄、`LogManager` のフラッシュ）。

### 1.2 Pre-Host の `Global` 参照（`App.xaml.cs`、12 箇所）
- 言語: `ApplyLanguageSetting` の `Global.UseLang`／`Global.SetCulture`（共用ヘルパー。Pre-Host の `-driverinstall` 分岐、Post-Host、`LanguagePackControl` から呼ばれる）。
- 最初の塊（手順 2）: `FindConfigLocation`、`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`。
- `RefreshViGEmBusInfo`（手順 4 と `-driverinstall`）。
- `-driverinstall`: `FindConfigLocation`、`Global.Load()`、`UseLang`、`UseCurrentTheme`。
- `AttemptSave`: `Global.appdatapath = null`（保存に失敗したとき、保存場所を取り消す）。
- `LoggerHolder`: `Global.appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`。

### 1.3 `AppHost` の現状
- `CreateHost()`（本体の呼び出し 0 件、テスト 58 件）、`CreateHost(string[] args)`（呼び出し **0 件**）、`CreateHost(config, parser)`（本体の 1 件）。`CreateHost()` の `configuration` 引数は使われていない。
- 暗黙のホスト構築: `GetService` の呼び出し時にホストが無いと、`CreateHost()` を呼ぶ（`CreationNote` に記録）。本体でこの経路に来るのは **`-driverinstall` 分岐の `Global.Load()` だけ**（`Global.Load()` が `AppHost.GetService<IAppSettingsService>()` を呼ぶため）。K7-1 の残存経路。
- `AppHost.Dispose()`: **呼び出し 0 件**。`CleanShutdown` はホストを破棄しない。IDisposable な登録サービスは `DefaultControllerRegistry` のみ（これは `CleanShutdown` が `ClearControllersForDevice` で手動で片付けている）。そのため、破棄を足しても実害は小さいが、ホスト自身の資源（設定ファイルの監視など）が終了時に解放されない。

### 1.4 `Global` の環境・パス状態と、参照元（コメントを除く）
- **実行ファイル**: `exelocation`（`PathService` 1、`DefaultElevatedProcessLauncher` 1）、`exedirpath`（`BackingStore` 2、`PathService` 1、テスト 6）、`exeversion`（`BackingStore` 2、`Changelog` 2、`OutputSlotPersist` ほか計 6、テスト 2）、`fileVersion`。静的初期化で一度だけ計算される（ジャンクション解決を含む）。
- **保存場所**: `appdatapath`（`App` 2、`LoggerHolder` 1、`BackingStore` 6、`PathService` 2、`Global` 内 2、**テスト 46**）、`appDataPpath`／`multisavespots`（`PathService`）、`firstRun`（`AppSettingsService`）、`FindConfigLocation`（`App` 2、`WelcomeDialog` 1）、`SaveWhere`／`SaveDefault`（`PathService`）。`SaveWhere` は `BackingStore` のファイルパス（`m_Profile` ほか 4 つ）も更新する。
- **環境のプローブ**: `IsAdministrator`（`BackingStore`、`Util`、`EnvironmentService`）、`AdminNeeded`（`BackingStore`、`EnvironmentService`）、`CheckIfVirtualDevice`／`GetInstanceIdFromDevicePath`（`DS4Devices`、`EnvironmentService`）、`vigemBusVersionInfo`／`hidHideInstalled`／`fakerInputVersion`／`Is…Installed`／`IsWin8OrGreater` ほか（**`EnvironmentService` だけが呼ぶもの**が約 15 名）、`fakerInputInstalled`（`VirtualKBMFactory`）、`Refresh…Info`（`App` 2、`EnvironmentService`）。
- **実行時のフラグ**: `runHotPlug`（`AppSettingsService` 経由で `ControlService`／`MainWindow` が読み書きする、保存しないフラグ）。

### 1.5 静的初期化のハザード（変更の制約）
- `Global` は静的コンストラクターを持ち、`m_Config = new BackingStore()` を静的に作る。`BackingStore` の既定値の一部は `Global` の定数を参照する。`EnvironmentService.ProcessControllerSlotLimit` は、静的初期化の循環を避けるため `Global` を呼ばない作りになっている（回帰ガード: `EnvironmentServiceStaticInitGuardTests`）。**環境の移管でこの循環を作らないこと**が最大の制約。
- `Global.exelocation` などは `Process.GetCurrentProcess().MainModule` に依存する。テストのホストでは、テストプロセスのパスになる。

### 1.6 `ReEnableDevice`
- `-reenableDevice` 分岐は、ホスト構築の前に `DS4Devices.reEnableDevice` を静的に呼ぶ（昇格した補助プロセスが、デバイスを有効化して終了するだけ）。`IDs4DeviceRegistry.ReEnableDevice`（Phase7-Step2 で「接続」と判断して TODO を付けた契約）は未接続。実装の `Ds4DeviceRegistryAdapter` は状態を持たない。

### 1.7 既存の課題の発見（修正は承認後）
- **ログ設定の早期適用が既定値のまま**: 手順 2 の `LogMaxArchiveFiles`／`LogMinLevel` は、`Profiles.xml` の読込（手順 6）より前に読むため、ユーザーが設定した値ではなく `BackingStore` の既定値になる。コメントは「ユーザー設定の最小レベルを早期に適用」と書いているが、実際には効かない。`25d60ecb`（2025-12-14、DI 移行より前）で入った機能からの動作で、**回帰ではない**。起動直後のログの量とローテーションの世代数に影響する程度。Q7 で扱いを決める。

## 2. 設計（D3＝S1）

- **ブートストラップ**: `Application_Startup` の冒頭で、`PathService` と `EnvironmentService` を `new` で作る（コンテナの構築前）。この 2 つが次を所有する。
  - `PathService`: 実行ファイルの情報（`exelocation`／`exedirpath`／`exeversion`）、保存場所（`appdatapath`）、`firstRun`／`multisavespots`、`FindConfigLocation`／`SaveWhere`／`SaveDefault` の処理。**入力（実行ファイルのフォルダ、ローミングのフォルダ）をコンストラクター引数で受け取れる**ようにして、4 通りの分岐をテストで固定できるようにする。`SaveWhere` が更新する `BackingStore` のファイルパスは、`BackingStore` を引数に受ける。
  - `EnvironmentService`: ViGEm／HidHide／FakerInput／OS／管理者権限のプローブと、その状態（バージョン、導入済みか）。
- **登録**: `AppHost.CreateHost(config, parser, bootstrap)` で、この 2 つの**同じインスタンス**をコンテナへ登録する（`ServiceRegistration` の既存の `AddSingleton<IPathService, PathService>()` は、ホストが外から渡された場合を除く既定にとどめる。テストは従来どおり `CreateHost()` を使う）。
- **`Global` の窓口**: 下位層（`BackingStore`、`LoggerHolder`、`DS4Devices`、`Util` ほか）が静的に読む `Global.appdatapath`／`exedirpath`／`exeversion` などは、**ブートストラップの `PathService` を指す読み取りの窓口**として残す（`Global.AttachBootstrap` で、起動の最初に一度だけ設定する。テストは従来どおり `Global.appdatapath` を設定できるよう、設定側も窓口を通す）。下位層への注入は、`BackingStore` の永続化を切り出す Step10 で行う（TODO を付ける）。
- **`AppHost`**: `CreateHost(string[])` を削除、`CreateHost()` の未使用の引数を削除（テストの 58 件は `CreateHost()` のまま）。`-driverinstall` 分岐の前に明示的にホストを構築し、`Global.Load()` による暗黙の構築を無くす（Q2）。`CleanShutdown` の最後に `AppHost.Dispose()` を呼ぶ（Q3）。`CreationNote` と暗黙構築の経路は、Step5 で Service Locator を整理するまで残す（Q5）。

## 3. ご判断をお願いしたい事項（選択肢と推奨）

- **Q1: 保存場所・実行ファイル情報の所有を `PathService` へ移すか（Step4 のスコープ）** — (A・推奨) 移す（D3＝S1 の内容そのもの）。`Global.appdatapath` ほかは、ブートストラップの `PathService` を指す窓口として残し、下位層への注入は Step10。(B) 今回は環境のプローブと `AppHost`／起動手順の整理だけにし、パスの所有は Step10 に先送りする（リスクは小さいが、Pre-Host の `Global` 参照が残る）。
- **Q2: `-driverinstall` 分岐の暗黙のホスト構築** — (A・推奨) 分岐の前に `AppHost.CreateHost(config, parser)` を明示的に呼び、設定は `IAppSettingsService.Load()` で読む。`Global.Load()` は削除する（呼び出しは `App` と `WelcomeDialog` の 2 件）。(B) `-driverinstall` の処理を、通常の起動と同様にホスト構築の後（Post-Host）へ移す（順序が大きく変わるため、リスクが高い）。
- **Q3: ホストの破棄** — (A・推奨) `CleanShutdown` の最後（`rootHub` の停止・`ShutDown` とレジストリの破棄の後、`LogManager` のフラッシュの前）で `AppHost.Dispose()` を呼ぶ。例外は握る。防壁 5（ドライバ破棄順序）を崩さないため、`ControlService.Stop`／`ShutDown` の後に置く。(B) 呼ばずに、`Dispose` を削除する。
- **Q4: `-reenableDevice` の接続** — (A・推奨) 分岐で `Ds4DeviceRegistryAdapter` を生成して `IDs4DeviceRegistry.ReEnableDevice` を呼ぶ（ホストは構築しない。契約を接続し、TODO を解消する）。(B) 契約を削除し、静的呼び出しを恒久的な Pre-Host の例外として残す（Step2 の「接続」の判断を取り消す）。
- **Q5: `AppHost` の整理と `CreationNote`** — (A・推奨) `CreateHost(string[])` と未使用の引数を削除する。`CreationNote` と暗黙構築の経路は、本体で暗黙構築が起きなくなった後も、診断用として Step5 まで残す。(B) `CreationNote` と起動ログ（`[DI] Host creation path`）もここで削除する（ユーザー決定「残す」を変更する）。
- **Q6: 環境のプローブの移管** — (A・推奨) `EnvironmentService` だけが呼ぶ約 15 名（バージョン・導入状態・OS 判定・HidHide の判定ほか）を、`Global` から `EnvironmentService` へ移して削除する。静的初期化の循環を作らない（`EnvironmentServiceStaticInitGuardTests` を維持）。`IsAdministrator`／`AdminNeeded`／`CheckIfVirtualDevice`／`GetInstanceIdFromDevicePath`／`fakerInputInstalled` など他の層も呼ぶものは、`Global` に残し、呼び出し元の Step で移す。(B) 今回は移さない。
- **Q7: ログ設定の早期適用（§1.7）** — (A・推奨) 今回は直さず、既存の課題として記録する（回帰ではなく、影響は起動直後のログに限られる）。(B) 直す（`Profiles.xml` のログ設定だけを先に読む手順を追加。起動順序の変更になるため、実機確認が増える）。

## 4. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行）

- **W1: 環境のプローブの移管**（Q6）
  - 冒頭の走査で対象を確定。`EnvironmentService` が状態（バージョン、導入済みか）と処理を持ち、`Global` の該当メンバーを削除。テストの参照を書き換え。`EnvironmentServiceStaticInitGuardTests` が成功すること。
- **W2: `AppHost` と終了の手順**（Q2・Q3・Q5）
  - `CreateHost(string[])` と未使用の引数を削除。`-driverinstall` の前に明示的にホストを構築し、`Global.Load()` を削除（`WelcomeDialog` も切り替え）。`CleanShutdown` に `AppHost.Dispose()` を追加。テストの追加（`AppHost.Dispose` の再入、暗黙構築が起きないこと）。
- **W3: `PathService` への所有の移管**（Q1）
  - `FindConfigLocation`／`SaveWhere`／`SaveDefault` と、`exelocation`／`exedirpath`／`exeversion`／`appdatapath`／`firstRun`／`multisavespots` を `PathService` へ。入力を注入できる形にして、分岐 4 通りのテストを追加。`Global` の窓口（`AttachBootstrap`）を追加。`AppSettingsService.FirstRun` は `PathService` の状態へ委譲。`runHotPlug` は `AppSettingsService` のインスタンスの状態にする。
- **W4: Pre-Host の置き換え**（Q1・Q4）
  - `App.xaml.cs` の最初の塊と `RefreshViGEmBusInfo`、`AttemptSave` を、ブートストラップのインスタンス経由に。`CreateHost` にブートストラップを渡して同じインスタンスを登録。`-reenableDevice` を `IDs4DeviceRegistry` 経由に（Q4）。
- **W5: ガード・台帳・モデル図・完了報告書**
  - ラチェットの基準値、`Phase7Step4…GuardTests`（削除した `Global` のメンバーの再宣言禁止、`AppHost` の公開メソッドの固定）、台帳、モデル図（04 の起動シーケンス）、チェックリスト U-14、`Phase7-Step4-Completion-Report.md`。
- 各 Wave の共通作業: 削除の直前に、`Global.<名前>`、`DS4Windows.Global.<名前>`、`using static` の無修飾の使用（`Mapping.cs`、`DS4LightBar.cs`）、テストの参照、ソース文字列を正規表現で確認するテストを再走査する。

## 5. リスクと検証

- **起動順序**: 手順 2 の最初の塊と、`LoggerHolder` の生成の順序を変えない（ログの初期化の順序を崩すと起動不能になる）。W3〜W4 の前後で、起動ログの出力順を比較する。
- **静的初期化の循環**（§1.5）: `EnvironmentService`／`PathService` が `Global` の静的初期化を起こさないこと。`EnvironmentServiceStaticInitGuardTests` を維持し、`PathService` にも同様のガードを追加する。
- **`FindConfigLocation` のテスト**: 現行は、実行ファイルのフォルダとローミングのフォルダが固定のため固定できない。入力を注入できる形にして、新しい実装に対してテストする（現行コードとの同一性は、処理の分岐をそのまま移すことと、W3 のレビューで確認する）。
- **テストの参照**: `Global.appdatapath` を参照するテストが 46 件。窓口を通して従来どおり設定できるようにして、書き換えを最小にする。
- **終了時のハング・ViGEm の残留**（Q3）: `AppHost.Dispose()` を足すため、実機で終了を確認する。
- **実機確認 U-14**（チェックリスト §11 に追記予定）: 初回起動（言語選択、保存場所の選択）、既存環境の起動（設定の復元、ログ）、保存場所が 2 か所にある場合、終了時（ハングしないこと、ViGEm の仮想デバイスが残らないこと）、`-driverinstall`、`-reenableDevice`。

## 6. 完了判定チェックリスト

- [ ] W1〜W4 のビルド（警告 0）・テストビルド・テスト実行が成功している。
- [ ] Pre-Host の `Global` 参照（§1.2、12 箇所）が、ブートストラップのサービス経由になっている（承認内容に従う）。
- [ ] `-driverinstall` で、暗黙のホスト構築が起きない（`CreationNote` が明示的な構築になっている）。
- [ ] `AppHost.Dispose()` が終了時に呼ばれる。
- [ ] `FindConfigLocation` の分岐 4 通りのテストが成功している。
- [ ] ラチェットの基準値が更新されている。
- [ ] 台帳、モデル図、`Phase7-Status.md` が更新されている。
- [ ] 実機確認 U-14。
- [ ] `Phase7-Step4-Completion-Report.md` が作成されている。
