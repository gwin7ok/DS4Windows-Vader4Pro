# Phase6-Step9 計画書: 主要4大ViewModelのPure DI徹底とカテゴリ別段階的移行

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスに基づく推奨案［主要4大ViewModel集中 ＋ カテゴリ別段階的移行・Pure DI徹底］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §3（ViewModel層 実地監査記録・全79箇所）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類・シム台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase6-Step8-Plan.md`（案3-A ProfileSettingsViewModel 拡充計画）  

---

## 0. 背景と方針確定（推奨案: 選択肢1の採用）

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

## 2. 全件参照台帳（主要4大ViewModel: 全79箇所・完全カタログ）

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

## 3. アーキテクチャ設計・Pure DI コンストラクタ拡張

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

## 4. PR分割計画とマイクロステップ

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

## 5. テスト・回帰検証計画

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

## 6. Phase6-Step10 への引き継ぎ台帳（残存小型ViewModel群）

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

## 8. 完了判定チェックリスト

- [ ] 表1〜表4に定義された主要4大ViewModel内の全79箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 表3・表4の除外項目（const定数14箇所）以外の Global 直接参照が4ファイル内に存在しないこと。
- [ ] 4大ViewModelのすべてのコンストラクタが Pure DI 構造に拡張され、クラス内でのサービスロケータ都度解決が排除されていること。
- [ ] `BatteryChanged`（B12）イベントが `IDeviceStateService` に実装され、トレイアイコンで正常に動作すること。
- [ ] 設定画面（OSC/UDP/平滑化/ログ/テーマ等）の全項目が `IAppSettingsService` と連動し、正しく永続化されること。
- [ ] 第6章に定義された残存小型ViewModel（12ファイル）が Step10 へ漏れなく引き継がれていること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。