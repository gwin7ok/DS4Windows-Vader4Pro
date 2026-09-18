# Phase6-Step7 計画書: `App.xaml.cs` のPost-Host Global直参照解消（起動シーケンス段階的移行）

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスに基づく推奨案［起動シーケンス段階的移行 ＋ 既存サービス集約］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.5, §5.5  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §5（`App.xaml.cs` 実地監査記録・全44箇所）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  

---

## 0. 背景と改訂の経緯

### 0.1 暫定22件から全44箇所への精査と境界の解明
旧計画書（2026-09-11）では、「暫定22件の調査と Pre-Host 境界の確認」を先行課題としていた。  
2026-09-18に実施された Step1 再監査（`Phase6-Step1-UI-Reference-Evidence.md` §5）において、`App.xaml.cs`（約1,100行）内の全 Global 参照（計44箇所）の実地走査が完了し、以下の通りライフサイクル境界が完全に確定した：

- **Pre-Host 除外（原理的DI対象外・カテゴリA）: 計11箇所**  
  DIコンテナ（`AppHost.CreateHost` 298行目）が構築される前に実行される初期ログローテーション（`appdatapath`, `LogMaxArchiveFiles`, `LogMinLevel`）、多重起動判定、および `--driverinstall` 特殊起動分岐。これらは DI コンテナが存在しない前提で動作するブートストラップ処理であり、全体計画書§4.5に基づき「安全に静的のまま温存」する。
- **真の定数除外（const）: 計1箇所**  
  1025行の `Global.MAX_DS4_CONTROLLER_COUNT`（const 4）。
- **Post-Host 実移行対象: 計32箇所**  
  Host構築後に実行される言語設定、設定保存場所判定（`SaveWhere`）、設定ファイル読込・保存、プロファイル・アクション初期化、管理者権限判定、終了時（`Application_Exit`）の設定永続化。

### 0.2 方針の確定: 【推奨案: 選択肢1（起動シーケンス段階的移行 ＋ 既存サービス集約）の採用】
`App.xaml.cs` はアプリケーションの **Composition Root（起動エントリーポイント）** であり、起動シーケンスを別クラス（Bootstrapper）へ丸ごと切り出すような大規模リファクタリング（選択肢2）は、起動不能クラッシュを招くリスクが極めて高い。  
本ステップでは、WPF の Composition Root パターンに忠実に従った以下の推奨方針を採用する：

1. **Composition Root キャッシュフィールドの導入**:
   `AppHost.CreateHost()` 直後に、起動および終了に必要な DI サービス（`IAppSettingsService`, `IPathService`, `IEnvironmentService`, `IProfileRepository`, `ISpecialActionRepository`, `IAppearanceSettingsService`, `IDeviceStateService`）を解決してプライベートフィールドへ一度だけ保持し、以降のメソッドではフィールド経由で参照する（都度解決の散乱を防止）。
2. **Pre-Host 境界の絶対的保護**:
   Host構築前の11箇所は手つかずのまま温存し、自己矛盾（コンテナ構築前のDI解決エラー）のリスクを原理的にゼロとする。
3. **分類(c)の既存サービス集約**:
   `multisavespots`（保存場所判定）は `IPathService`、`DeviceOptions` は `IAppSettingsService`、`CreateStdActions` は `ISpecialActionRepository` に薄い委譲シムを追加して集約し、過剰なインターフェース新設を避けて堅牢に完了させる。

---

## 1. 目的

1. `App.xaml.cs` において、`AppHost.CreateHost()` 以降に実行される Post-Host 領域の Global 直参照（全32箇所）を、DI サービス経由の呼び出しへ完全移行する。
2. Pre-Host 領域（11箇所）および const 定数（1箇所）を正しく除外として温存し、アプリの起動・終了シーケンスの絶対的安定性を保証する。
3. アプリケーションの Composition Root におけるサービス配線構造を完成させ、`Global.Save()`, `Global.Load()`, `Global.appdatapath` 等の正本への一元化（SSOT）を完了する。

---

## 2. 全件参照台帳（全44箇所・完全カタログ）

### 表1: Post-Host 実移行対象（ID: C7-01 〜 C7-32、計32箇所）

| ID | 行番号 | メソッド／経路 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C7-01 | 310 | `Application_Startup` | `Global.firstRun` | (a) | `_appSettingsService.FirstRun` |
| C7-02 | 336 | `Application_Startup` | `Global.multisavespots` | (c) | `_pathService.HasMultiSaveSpots`（集約シム） |
| C7-03 | 349 | `Application_Startup` | `Global.appdatapath` | (b) | `_pathService.AppDataPath` |
| C7-04 | 356 | `Application_Startup` | `Global.Load()` | (a) | `_appSettingsService.Load()` |
| C7-05 | 362 | `Application_Startup` | `Global.Save()` | (a) | `_appSettingsService.Save()` |
| C7-06 | 368 | `Application_Startup` | `Global.exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C7-07 | 374 | `Application_Startup` | `Global.exeFileName` | (a) | `System.IO.Path.GetFileName(_pathService.ExecutablePath)` |
| C7-08 | 418 | `Application_Startup` | `Global.appdatapath` | (b) | `_pathService.AppDataPath` |
| C7-09 | 426 | `Application_Startup` | `Global.UseLang` | (b) | `_appSettingsService.UseLang` |
| C7-10 | 428 | `Application_Startup` | `Global.DeviceOptions` | (c) | `_appSettingsService.DeviceOptions` |
| C7-11 | 430 | `Application_Startup` | `Global.Save()` | (a) | `_appSettingsService.Save()` |
| C7-12 | 438 | `Application_Startup` | `Global.SaveAsProfile(i, ...)` | (b) | `_profileRepository.SaveAsProfile(i, ...)` |
| C7-13 | 441 | `Application_Startup` | `Global.ProfilePath[i]` | (a) | `_profileRepository.ProfilePath[i]` |
| C7-14 | 441 | `Application_Startup` | `Global.OlderProfilePath[i]` | (a) | `_profileRepository.OlderProfilePath[i]` |
| C7-15 | 448 | `Application_Startup` | `Global.ResetConnectionFlags()` | (b) | `_deviceStateService.ResetConnectionFlags()` |
| C7-16 | 452 | `Application_Startup` | `Global.LoadActions()` | (b) | `_specialActionRepository.LoadActions()` |
| C7-17 | 454 | `Application_Startup` | `Global.CreateStdActions()` | (c) | `_specialActionRepository.CreateStandardActions()` |
| C7-18 | 458〜460 | `Application_Startup` | `Global.UseLang`（3箇所） | (b) | `_appSettingsService.UseLang` |
| C7-19 | 462, 463 | `Application_Startup` | `Global.UseCurrentTheme` | (a) | `_appearanceSettingsService.UseCurrentTheme` |
| C7-20 | 467 | `Application_Startup` | `Global.LoadLinkedProfiles()` | (b) | `_profileRepository.LoadLinkedProfiles()` |
| C7-21 | 482 | `Application_Startup` | `Global.IsAdministrator()` | (a) | `_environmentService.IsAdministrator()` |
| C7-22 | 485 | `Application_Startup` | `Global.hidHideInstalled` | (b) | `_environmentService.HidHideInstalled` |
| C7-23 | 541〜543 | `SaveWhere` 処理 | `Global.appdatapath`（3箇所） | (b) | `_pathService.AppDataPath` |
| C7-24 | 557 | `SaveWhere` 処理 | `Global.Save()` | (a) | `_appSettingsService.Save()` |
| C7-25 | 564〜572 | `SaveWhere` 処理 | `Global.appDataPpath`（5箇所） | (b) | `_pathService.DefaultAppDataPath` |
| C7-26 | 565, 567 | `SaveWhere` 処理 | `Global.exedirpath`（2箇所） | (a) | `System.IO.Path.GetDirectoryName(_pathService.ExecutablePath)` |
| C7-27 | 570 | `SaveWhere` 処理 | `Global.exedirpath` | (a) | `System.IO.Path.GetDirectoryName(_pathService.ExecutablePath)` |
| C7-28 | 585 | `SaveWhere` 処理 | `Global.appdatapath = ...`（代入） | (b) | `_pathService.AppDataPath = ...` |
| C7-29 | 999 | `Application_Exit` | `Global.Save()` | (a) | `_appSettingsService.Save()` |
| C7-30 | 1002 | `Application_Exit` | `Global.outputKBMHandler?.Disconnect()` | (a) | `_virtualKBM?.Disconnect()` |
| C7-31 | 1010 | `Application_Exit` | `Global.Save()` | (a) | `_appSettingsService.Save()` |
| C7-32 | 1018 | `Application_Exit` | `Global.ResetConnectionFlags()` | (b) | `_deviceStateService.ResetConnectionFlags()` |

---

### 表2: 明示的除外台帳（ID: C7-EX01 〜 C7-EX12、計12箇所）

| ID | 行番号 | 参照メンバ | 実行タイミング | 除外理由（根拠） |
|---|---|---|---|---|
| C7-EX01 | 110 | `Global.UseLang` | Pre-Host | 多重起動判定前の初期言語確認（コンテナ未構築） |
| C7-EX02 | 111 | `Global.SetCulture` | Pre-Host | 初期カルチャ適用（コンテナ未構築） |
| C7-EX03 | 154 | `Global.FindConfigLocation()` | Pre-Host | 設定ファイル位置検索（ブートストラップ処理） |
| C7-EX04 | 157 | `Global.appdatapath` | Pre-Host | 初期ログローテーションのパス決定（コンテナ未構築） |
| C7-EX05 | 157 | `Global.LogMaxArchiveFiles` | Pre-Host | 初期ログローテーション設定（コンテナ未構築） |
| C7-EX06 | 162 | `Global.LogMinLevel` | Pre-Host | 初期ログレベル設定（コンテナ未構築） |
| C7-EX07 | 289 | `Global.RefreshViGEmBusInfo()` | Pre-Host | ViGEm バス環境初期化（Host構築前必須情報） |
| C7-EX08 | 604 | `Global.RefreshViGEmBusInfo()` | Pre-Host | `--driverinstall` 特殊起動分岐（コンテナ構築前終了） |
| C7-EX09 | 607 | `Global.FindConfigLocation()` | Pre-Host | `--driverinstall` 特殊起動分岐 |
| C7-EX10 | 608 | `Global.Load()` | Pre-Host | `--driverinstall` 特殊起動分岐 |
| C7-EX11 | 612〜615 | `Global.UseLang`, `UseCurrentTheme` | Pre-Host | `--driverinstall` 特殊起動分岐でのUIテーマ適用 |
| C7-EX12 | 1025 | `Global.MAX_DS4_CONTROLLER_COUNT` | Any | 真の定数（`const int`、4層モデル共通値） |

---

## 3. アーキテクチャ設計・Composition Root 解決

### 3.1 キャッシュフィールドと初期化設計
`App.xaml.cs` は WPF アプリケーションのエントリーポイント（Composition Root）であるため、コンストラクタインジェクションは利用できない。  
`AppHost.CreateHost(...)` 直後に、必要な DI サービスをプライベートフィールドへ一度だけ解決・保持する初期化メソッド（`InitializePostHostServices`）を設ける。

```csharp
// DS4Windows/App.xaml.cs
public partial class App : Application
{
    private IAppSettingsService _appSettingsService;
    private IPathService _pathService;
    private IEnvironmentService _environmentService;
    private IProfileRepository _profileRepository;
    private ISpecialActionRepository _specialActionRepository;
    private IAppearanceSettingsService _appearanceSettingsService;
    private IDeviceStateService _deviceStateService;
    private IVirtualKBM _virtualKBM;

    private void InitializePostHostServices()
    {
        _appSettingsService = AppHost.GetService<IAppSettingsService>();
        _pathService = AppHost.GetService<IPathService>();
        _environmentService = AppHost.GetService<IEnvironmentService>();
        _profileRepository = AppHost.GetService<IProfileRepository>();
        _specialActionRepository = AppHost.GetService<ISpecialActionRepository>();
        _appearanceSettingsService = AppHost.GetService<IAppearanceSettingsService>();
        _deviceStateService = AppHost.GetService<IDeviceStateService>();
        _virtualKBM = AppHost.GetService<IVirtualKBM>();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // 1. Pre-Host 領域（ログ初期化、多重起動判定、Pre-Host除外11件）は既存のまま実行
        ...
        
        // 2. DI ホスト構築
        AppHost.CreateHost(services => {
            ServiceRegistration.Register(services, parser);
        });

        // 3. Post-Host サービス解決
        InitializePostHostServices();

        // 4. 以降の起動処理はすべて注入フィールド経由で実行
        if (!_appSettingsService.FirstRun)
        {
            ...
            _appSettingsService.Load();
            ...
        }
    }
}
```

### 3.2 既存サービスへの集約（分類b・cシム）
新規インターフェースを乱立させず、以下の通り既存サービスに薄いシムを追加する：
- **`IPathService`**:
  - `string DefaultAppDataPath { get; }` → `Global.appDataPpath`
  - `bool HasMultiSaveSpots { get; }` → `Global.multisavespots`
- **`IAppSettingsService`**:
  - `string UseLang { get; set; }` → `Global.UseLang`
  - `ControlServiceDeviceOptions DeviceOptions { get; }` → `Global.DeviceOptions`
- **`ISpecialActionRepository`**:
  - `void LoadActions()` → `Global.LoadActions()`
  - `void CreateStandardActions()` → `Global.CreateStdActions()`
- **`IProfileRepository`**:
  - `bool SaveAsProfile(int deviceIndex, string profileName)` → `Global.SaveAsProfile(deviceIndex, profileName)`
  - `void LoadLinkedProfiles()` → `Global.LoadLinkedProfiles()`
- **`IDeviceStateService`**:
  - `void ResetConnectionFlags()` → `Global.ResetConnectionFlags()`

---

## 4. PR分割計画とマイクロステップ

変更の安全性と追跡性を最大化するため、以下の**4つのサブPR（マイクロステップ）**に分割して進める。

```text
【Phase6-Step7 マイクロステップ構成】
├─ Step7-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step7-1 (PR-1): 既存DIサービスへのシム追加（分類b・c集約）
├─ Step7-2 (PR-2): Post-Host サービス解決基盤の導入と基本設定・パスの置換（C7-01〜C7-11, C7-23〜C7-28）
├─ Step7-3 (PR-3): プロファイル・アクション・デバイス初期化の置換（C7-12〜C7-22）
└─ Step7-4 (PR-4): Application_Exit の置換・起動回帰検証・完了報告書作成（C7-29〜C7-32）
```

---

### Step7-0: 着手前提検証・ベースライン記録（実装なし）
1. Step6 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step7-1 (PR-1): 既存DIサービスへのシム追加
- **対象**: `IPathService`, `IAppSettingsService`, `ISpecialActionRepository`, `IProfileRepository`, `IDeviceStateService`
- **作業内容**:
  1. §3.2 に定義された薄い委譲シムを追加（BackingStore / Global 委譲）。
- **検証**:
  - 単体テスト: 新規追加シムが Global / BackingStore と完全に連動することを検証。
  - `dotnet test` 全件合格を確認。

---

### Step7-2 (PR-2): Post-Host 解決基盤と基本設定・パスの置換
- **対象**: `App.xaml.cs`（C7-01 〜 C7-11, C7-23 〜 C7-28）
- **作業内容**:
  1. `InitializePostHostServices()` を実装。
  2. `firstRun`, `Load()`, `Save()`, `exeversion`, `exeFileName`, `AppDataPath`, `DefaultAppDataPath`, `exedirpath`, `SaveWhere` 処理を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - 通常起動でエラーなく設定がロードされることを手動確認。

---

### Step7-3 (PR-3): プロファイル・アクション・デバイス初期化の置換
- **対象**: `App.xaml.cs`（C7-12 〜 C7-22）
- **作業内容**:
  1. `SaveAsProfile`, `ProfilePath`, `OlderProfilePath`, `ResetConnectionFlags`, `LoadActions`, `CreateStdActions`, `UseLang`, `UseCurrentTheme`, `LoadLinkedProfiles`, `IsAdministrator`, `hidHideInstalled` を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - プロファイル・スペシャルアクション一覧が正常にロードされることを手動確認。

---

### Step7-4 (PR-4): Application_Exit の置換と最終検証
- **対象**: `App.xaml.cs`（C7-29 〜 C7-32）
- **作業内容**:
  1. `Application_Exit` 内の `Save()`, `Disconnect()`, `ResetConnectionFlags()` を置換。
  2. Pre-Host の 11箇所が変更されずに温存されていることを検証。
  3. 完了報告書（`Phase6-Step7-Completion-Report.md`）を作成。
- **検証**:
  - アプリ終了時に設定が正常に保存されることを確認。
  - 全自動テスト合格を確認。

---

## 5. テスト・回帰検証計画

### 5.1 自動単体テスト計画
1. **`AppCompositionRootTests.cs`**:
   - `AppHost` 構築後、`InitializePostHostServices` で解決されるすべてのサービスが非nullであること。
2. **`AppShimIntegrationTests.cs`**:
   - `IPathService.HasMultiSaveSpots`, `IPathService.DefaultAppDataPath`, `ISpecialActionRepository.CreateStandardActions` 等のシムが、正本と矛盾なく同期して機能すること。

### 5.2 起動・終了回帰検証手順（Phase6-Step11連携）
1. **通常起動シーケンス**:
   - アプリを起動し、スプラッシュ画面またはメインウィンドウが正常に表示されること。
   - 保存されているプロファイル・テーマ・言語が正しく反映されること。
2. **コマンドライン引数付き起動**:
   - `DS4Windows.exe -m`（最小化起動）でトレイアイコンのみ起動すること。
   - `DS4Windows.exe --driverinstall` で特殊インストーラ分岐がクラッシュせず動作すること（Pre-Host温存の証明）。
3. **終了シーケンス**:
   - メインウィンドウを閉じた際、またはタスクトレイメニューから「Exit」を選択した際、クラッシュせず正常終了し、`Profiles.xml` などのタイムスタンプが更新されること。

---

## 6. ロールバック方針

起動シーケンスで例外やフリーズが発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。Pre-Host 領域には手を触れていないため、切り戻しは極めて迅速かつ確実に行える。

---

## 7. 完了判定チェックリスト

- [ ] 表1に定義された Post-Host の全32箇所が、DI サービス経由の呼び出しへ置換されていること。
- [ ] 表2に定義された Pre-Host 領域（11箇所）および const 定数（1箇所）が、意図通り静的呼び出しのまま温存されていること。
- [ ] `App.xaml.cs` 冒頭での不要な `using static DS4Windows.Global;` への依存が最小化されていること。
- [ ] 分類(b)・(c)のシムが既存サービスへ過不足なく集約され、BackingStore と連動していること。
- [ ] 通常起動、最小化起動、および `--driverinstall` 起動がすべて正常に動作すること。
- [ ] アプリ終了時に設定・アクションが正常に保存されること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。