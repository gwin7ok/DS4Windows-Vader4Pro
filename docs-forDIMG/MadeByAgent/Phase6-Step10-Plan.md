# Phase6-Step10 計画書: 残存小型UI・小型ViewModel・補助クラスの機能ドメイン別ペアPure DI化

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスおよびStep9引き継ぎ合意に基づく推奨案［機能ドメイン別3グループ分割・View/ViewModelペアPure DI化］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`  
参照エビデンス:  
  - `Phase6-Step1-UI-Reference-Evidence.md` §2〜§4（UI層・小型ViewModel層 実地監査記録）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類台帳）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase6-Step9-Plan.md`（主要ViewModel移行計画・残存小型ViewModel引き継ぎ台帳）  

---

## 0. 背景と改訂の経緯

### 0.1 旧計画書からの精査とStep9からの引き継ぎ合意
旧計画書（2026-09-11）では、「`WelcomeDialog`, `PresetOption`, `SaveWhere`, `BindingWindow` の4ファイル・暫定29件のみ」をスコープとしていた。  
しかし、2026-09-18の Step1 精密再監査（`Phase6-Step1-UI-Reference-Evidence.md` §2〜§4）および Step9 計画策定における重要合意により、**Phase6 の最終実装ステップである本 Step10 の真の全容**が以下の通り確定した：

1. **Step9 から正式に引き継がれた残存小型 ViewModel 群（12ファイル・実参照約30箇所）**:  
   `AboutViewModel`, `AutoProfilesViewModel`, `BindingWindowViewModel`, `ControllerListViewModel`, `ControllerRegDeviceOptsViewModel`, `LanguagePackViewModel`, `LogViewModel`, `MappingListViewModel`, `RecordBoxViewModel`, `RenameProfileViewModel`, `TouchButtonUserControlViewModel`, `UpdaterWindowViewModel`
2. **残存する小型 View（XAML コードビハインド 11ファイル・実参照約35箇所）**:  
   `WelcomeDialog.xaml.cs`, `SaveWhere.xaml.cs`, `BindingWindow.xaml.cs`, `About.xaml.cs`, `AutoProfiles.xaml.cs`, `DupBox.xaml.cs`, `LanguagePackControl.xaml.cs`, `LanguageSelectDialog.xaml.cs`, `RecordBox.xaml.cs`, `SpecialActionEditor.xaml.cs`, `UpdaterWindow.xaml.cs`
3. **スペシャルアクション系 ViewModel 群（7ファイル・実参照約10箇所）**:  
   `SpecialActionsListViewModel.cs`（残存フォールバックF）, `CheckBatteryViewModel`, `MacroViewModel`, `PressKeyViewModel` 等の `SaveAction` / `configFileDecimalCulture`
4. **補助データクラス（1ファイル・実参照約10箇所）**:  
   `PresetOption.cs`（プリセット初期化時のコントローラー種別・マッピング参照）

### 0.2 方針の確定: 【推奨案: 選択肢A（機能ドメイン別 3グループ分割・View/ViewModel ペア Pure DI 化）】
本ステップは、**Phase6 において Global 直接参照を完全に根絶する最終防衛ライン**である。残存ファイルを先送りせず、かつ過渡期のバインディング切れを防ぐため、以下の推奨方針を採用する：

- **View と ViewModel のペア改修**:  
  画面（View）とビューモデル（ViewModel）を同時に Pure DI 化することで、WPF のデータバインディング整合性を 100% 保証し、画面単位での結合動作確認を可能とする。
- **機能ドメイン別 3グループ分割**:  
  全約25ファイルを「①起動・システム・環境系」「②プロファイル・入力マッピング系」「③スペシャルアクション編集系」の3つの論理ドメインに分割し、安全・確実にサブPRで消化する。
- **Phase6 目標の完全達成**:  
  本ステップ完了をもって、除外項目（const定数・Pre-Host等）を除く**DS4Windows全体の全クラスから Global 直接参照が文字通り 100% 根絶**される。

---

## 1. 目的

1. 残存するすべての小型 View（11ファイル）、小型 ViewModel（12ファイル＋SpecialActions）、および `PresetOption.cs` において、コンストラクタ経由の Pure DI 構造を確立する。
2. 対象全ファイル内に存在する全実利用 Global 直接参照（計約72箇所）を、注入された DI サービス経由の呼び出しへ完全置換する。
3. 機能ドメイン別に View と ViewModel をペアで移行し、UI バインディング切れや画面クラッシュのリスクを完全に排除する。
4. Phase6 の大目標である「アプリケーション全域からの Global 直接参照根絶」を完全に達成し、Step11（実機総合検証）および Step12（完了判定）への完全な引き渡しを行う。

---

## 2. 全件参照台帳（全3グループ・完全カタログ）

---

### 表1: グループ1【起動・環境・システム設定系】（実参照計23箇所、除外3箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G1-01 | `WelcomeDialog.xaml.cs` | 28, 31 | `IsWin10OrGreater()`, `IsWin8OrGreater()` | (b) | `_environmentService.IsWin10OrGreater()`, `.IsWin8OrGreater()` |
| C10-G1-02 | `WelcomeDialog.xaml.cs` | 38, 104 | `RefreshViGEmBusInfo()` | (b) | `_environmentService.RefreshViGEmBusInfo()` |
| C10-G1-03 | `WelcomeDialog.xaml.cs` | 39, 105 | `IsViGEmBusInstalled()` | (b) | `_environmentService.IsViGEmBusInstalled()` |
| C10-G1-04 | `WelcomeDialog.xaml.cs` | 45 | `IsHidHideInstalled` | (b) | `_environmentService.HidHideInstalled` |
| C10-G1-05 | `WelcomeDialog.xaml.cs` | 52 | `IsFakerInputInstalled` | (b) | `_environmentService.IsFakerInputInstalled` |
| C10-G1-06 | `SaveWhere.xaml.cs` | 18 | `AdminNeeded()` | (b) | `_environmentService.AdminNeeded()` |
| C10-G1-07 | `SaveWhere.xaml.cs` | 29, 36 | `SaveWhere`（代入・判定） | (b) | `_pathService.SaveWhere` |
| C10-G1-08 | `SaveWhere.xaml.cs` | 42〜62 | `exedirpath`, `appDataPpath`（7箇所） | (a)/(b) | `_pathService.ExecutablePath`, `_pathService.DefaultAppDataPath` |
| C10-G1-09 | `SaveWhere.xaml.cs` | 69 | `SaveDefault(SaveWhere)` | (b) | `_pathService.SaveDefault(...)` |
| C10-G1-10 | `SaveWhere.xaml.cs` | 87, 88 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G1-11 | `About.xaml.cs` | 21 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-G1-12 | `AboutViewModel.cs` | 19 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-G1-13 | `UpdaterWindow.xaml.cs` | 27, 35 | `IsAdministrator()`（2箇所） | (a) | `_environmentService.IsAdministrator()` |
| C10-G1-14 | `UpdaterWindowViewModel.cs`| 42, 58 | `LastVersionChecked`（2箇所） | (b) | `_appSettingsService.LastVersionChecked` |
| C10-G1-15 | `LanguageSelectDialog.xaml.cs`| 31 | `UseLang` | (b) | `_appSettingsService.UseLang` |
| C10-G1-16 | `LanguageSelectDialog.xaml.cs`| 32, 42 | `SetCulture(...)`（2箇所） | (b) | `_appSettingsService.SetCulture(...)` |
| C10-G1-17 | `LanguagePackControl.xaml.cs` | 49 | `Save()` | (a) | `_appSettingsService.Save()` |
| C10-G1-18 | `LanguagePackViewModel.cs` | 45〜60 | `UseLang`（3箇所） | (b) | `_appSettingsService.UseLang` |
| C10-G1-19 | `LogViewModel.cs` | 28 | `exeversion` | (a) | `_environmentService.ApplicationVersion` |
| C10-EX-01 | `WelcomeDialog.xaml.cs` | 19, 20 | `FindConfigLocation`, `Load` | 除外 | Pre-Host ブートストラップ処理（手動呼出分岐） |
| C10-EX-02 | `WelcomeDialog.xaml.cs` | 80 | `BLANK_VIGEM_BUS_VERSION` | 除外 | 真の定数（`const string`） |

---

### 表2: グループ2【プロファイル・入力マッピング・プリセット系】（実参照計38箇所、除外5箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G2-01 | `BindingWindow.xaml.cs` | 65 | `ds4inputNames` | (a) | `_profileSettings.Ds4InputNames` |
| C10-G2-02 | `BindingWindow.xaml.cs` | 69, 71 | `xboxDefaultNames`, `ds4DefaultNames` | (a) | `_profileSettings.XboxDefaultNames`, `.Ds4DefaultNames` |
| C10-G2-03 | `BindingWindow.xaml.cs` | 142 | `InverseRumbleMotors[device]` | (a) | `_profileSettings.InverseRumbleMotors[device]` |
| C10-G2-04 | `BindingWindowViewModel.cs`| 41 | `outDevTypeTemp[device]` | (a) | `_outputSlotService.OutDevTypeTemp[device]` |
| C10-G2-05 | `BindingWindowViewModel.cs`| 89, 94 | `RefreshActionAlias(...)`（2箇所） | (b) | `_specialActionRepository.RefreshActionAlias(...)` |
| C10-G2-06 | `AutoProfiles.xaml.cs` | 46〜143 | `appdatapath`（5箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-07 | `AutoProfiles.xaml.cs` | 88 | `CreateAutoProfiles(...)` | (a) | `_autoProfileService.CreateAutoProfiles(...)` |
| C10-G2-08 | `AutoProfiles.xaml.cs` | 112, 118 | `UseCustomSteamFolder`, `CustomSteamFolder` | (b) | `_appSettingsService.UseCustomSteamFolder`, `.CustomSteamFolder` |
| C10-G2-09 | `AutoProfilesViewModel.cs` | 42, 53 | `AutoProfileRevertDefaultProfile`（2箇所） | (b) | `_appSettingsService.AutoProfileRevertDefaultProfile` |
| C10-G2-10 | `AutoProfilesViewModel.cs` | 65, 78 | `autoProfileSwitchNotifyChoice`（2箇所） | (b) | `_appSettingsService.AutoProfileSwitchNotifyChoice` |
| C10-G2-11 | `DupBox.xaml.cs` | 31, 48 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-12 | `RecordBox.xaml.cs` | 45, 58 | `appdatapath`（2箇所） | (b) | `_pathService.AppDataPath` |
| C10-G2-13 | `RecordBoxViewModel.cs` | 29 | `ProfileSettingsServiceInstance` | (a) | コンストラクタ注入 `_profileSettings` へ統一 |
| C10-G2-14 | `RecordBoxViewModel.cs` | 73 | `macroDS4Values` | (a) | `_profileSettings.MacroDS4Values` |
| C10-G2-15 | `RenameProfileViewModel.cs`| 34 | `appdatapath` | (b) | `_pathService.AppDataPath` |
| C10-G2-16 | `TouchButtonUserControlViewModel.cs`| 28, 44 | `TouchpadButtonMode`（2箇所） | (a) | `_profileSettings.TouchpadButtonMode` |
| C10-G2-17 | `MappingListViewModel.cs` | 52, 58 | `GetDS4CSetting(...)`（2箇所） | (a) | `_profileSettings.GetDS4CSetting(...)` |
| C10-G2-18 | `MappingListViewModel.cs` | 74, 80 | `getX360ControlString(...)`（2箇所） | (a) | `_profileSettings.GetX360ControlString(...)` |
| C10-G2-19 | `MappingListViewModel.cs` | 105, 112 | `defaultButtonMapping`（2箇所） | (a) | `_profileSettings.DefaultButtonMapping` |
| C10-G2-20 | `ControllerListViewModel.cs` | 35〜44 | `ProfileSettingsServiceInstance` 等（4箇所） | (a) | 注入サービス利用へ統一（フォールバック削除） |
| C10-G2-21 | `ControllerRegDeviceOptsViewModel.cs` | 40〜64 | `UseMoonlight`, `UseAdvancedMoonlight`（4箇所） | (b) | `_appSettingsService.UseMoonlight`, `.UseAdvancedMoonlight` |
| C10-G2-22 | `ControllerRegDeviceOptsViewModel.cs` | 112〜138 | `SaveControllerConfigs(...)`（3箇所） | (b) | `_profileXmlStore.SaveControllerConfigsForDevice(...)` |
| C10-G2-23 | `PresetOption.cs` | 45〜68 | `OutContType`（4箇所） | (a) | メソッド引数または `_profileSettings.OutContType` |
| C10-G2-24 | `PresetOption.cs` | 74〜114 | デフォルトマッピング設定（6箇所） | (a) | `_profileSettings.DefaultButtonMapping` 参照 |
| C10-EX-03 | `BindingWindow.xaml.cs` | 42〜57 | `TEST_PROFILE_INDEX`（4箇所） | 除外 | 真の定数（`const int`） |
| C10-EX-04 | `RecordBox.xaml.cs` | 30 | `TEST_PROFILE_INDEX` | 除外 | 真の定数（`const int`） |

---

### 表3: グループ3【スペシャルアクション編集系】（実参照計11箇所）

| ID | ファイル名 | 行番号 | 参照メンバ | 分類 | 移行先サービス・メンバ |
|---|---|---|---|---|---|
| C10-G3-01 | `SpecialActionEditor.xaml.cs` | 85 | `RemoveAction(...)` | (a) | `_specialActionRepository.RemoveAction(...)` |
| C10-G3-02 | `SpecialActionsListViewModel.cs`| 42 | `ProfileSettingsServiceInstance` | (a) | コンストラクタ注入 `_profileSettings` へ統一 |
| C10-G3-03 | `SpecialActionViewModel.cs` | 各所 | `SaveAction(...)` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-04 | `CheckBatteryViewModel.cs` | 各所 | `configFileDecimalCulture`（2箇所） | (b) | `_environmentService.ConfigFileDecimalCulture` |
| C10-G3-05 | `MacroViewModel.cs` | 各所 | `configFileDecimalCulture`（2箇所）, `SaveAction` | (a)/(b) | `_environmentService.*`, `_specialActionRepository.*` |
| C10-G3-06 | `MultiActButtonViewModel.cs` | 各所 | `SaveAction` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-07 | `PressKeyViewModel.cs` | 各所 | `SaveAction` | (a) | `_specialActionRepository.SaveAction(...)` |
| C10-G3-08 | 他サブアクション ViewModel | 各所 | アクション保存・設定読込（4箇所） | (a) | `_specialActionRepository.*` |

---

## 3. アーキテクチャ設計・View/ViewModel ペア Pure DI 化

すべての小型ダイアログおよびコントロールにおいて、以下の一貫した設計ルールを適用する。

### 3.1 ViewModel の Pure DI コンストラクタ拡張
すべての小型 ViewModel のコンストラクタで、必要な DI サービスを明示的に受け取り、`readonly` フィールドに格納する。
```csharp
// BindingWindowViewModel.cs の設計例
public class BindingWindowViewModel
{
    private readonly IOutputSlotService _outputSlotService;
    private readonly ISpecialActionRepository _specialActionRepository;

    public BindingWindowViewModel(
        IOutputSlotService outputSlotService = null,
        ISpecialActionRepository specialActionRepository = null)
    {
        _outputSlotService = outputSlotService ?? AppHost.GetService<IOutputSlotService>();
        _specialActionRepository = specialActionRepository ?? AppHost.GetService<ISpecialActionRepository>();
    }
    ...
}
```

### 3.2 View（XAML コードビハインド）の DataContext ペア配線
View はコンストラクタで対応する ViewModel を受け取り、`DataContext` に設定する。自身が直接必要とするサービス（`IPathService` 等）もコンストラクタで受け取る。
```csharp
// SaveWhere.xaml.cs の設計例
public partial class SaveWhere : Window
{
    private readonly IPathService _pathService;
    private readonly IEnvironmentService _environmentService;

    public SaveWhere(
        IPathService pathService = null,
        IEnvironmentService environmentService = null)
    {
        InitializeComponent();
        _pathService = pathService ?? AppHost.GetService<IPathService>();
        _environmentService = environmentService ?? AppHost.GetService<IEnvironmentService>();
    }
    ...
}
```

---

## 4. PR分割計画とマイクロステップ

変更の安全性と追跡性を最大化するため、以下の**5つのサブPR（マイクロステップ）**に分割して進める。

```text
【Phase6-Step10 マイクロステップ構成】
├─ Step10-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step10-1 (PR-1): グループ1【起動・環境・システム設定系】View/ViewModelペアPure DI化（23箇所）
├─ Step10-2 (PR-2): グループ2【プロファイル・マッピング・プリセット系】View/ViewModelペアPure DI化（38箇所）
├─ Step10-3 (PR-3): グループ3【スペシャルアクション編集系】View/ViewModelペアPure DI化（11箇所）
└─ Step10-4 (PR-4): 全体クリーンアップ・未検知参照ゼロ検証・完了報告書作成
```

---

### Step10-0: 着手前提検証・ベースライン記録（実装なし）
1. Step9 完了後の作業ツリー状態、HEAD コミットハッシュ、未コミット差分ゼロを確認。
2. ソリューション全体のビルド（`dotnet build -c Release`）および全単体テスト（`dotnet test`）が 100% グリーンであることを記録。

---

### Step10-1 (PR-1): グループ1【起動・環境・システム設定系】のペア Pure DI 化
- **対象**: 表1に定義された全ファイル（View 6ファイル、ViewModel 4ファイル、実参照計23箇所）
- **作業内容**:
  1. `WelcomeDialog`, `SaveWhere`, `About`, `UpdaterWindow`, `LanguageSelectDialog`, `LanguagePackControl` のコンストラクタを拡張。
  2. `AboutVM`, `UpdaterWindowVM`, `LanguagePackVM`, `LogVM` のコンストラクタを拡張。
  3. 各ファイル内の `Global.*` を注入フィールド経由へ置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - Welcome画面、保存先選択画面、言語選択画面の正常表示を確認。

---

### Step10-2 (PR-2): グループ2【プロファイル・マッピング・プリセット系】のペア Pure DI 化
- **対象**: 表2に定義された全ファイル（View 4ファイル、ViewModel 8ファイル、`PresetOption.cs`、実参照計38箇所）
- **作業内容**:
  1. `BindingWindow`, `AutoProfiles`, `DupBox`, `RecordBox` のコードビハインドを拡張。
  2. `BindingWindowVM`, `AutoProfilesVM`, `MappingListVM`, `RecordBoxVM`, `RenameProfileVM`, `TouchButtonVM`, `ControllerListVM`, `ControllerRegDeviceOptsVM` を Pure DI 化。
  3. `PresetOption.cs` のマッピング参照を引数渡し・サービス経由へ置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - ボタン割り当て画面、自動プロファイル編集画面が正常に動作することを確認。

---

### Step10-3 (PR-3): グループ3【スペシャルアクション編集系】のペア Pure DI 化
- **対象**: 表3に定義された全ファイル（View 1ファイル、ViewModel 8ファイル、実参照計11箇所）
- **作業内容**:
  1. `SpecialActionEditor.xaml.cs` のコンストラクタ拡張と置換。
  2. `SpecialActionsListViewModel` および `SpecialActions/` 配下の各サブViewModelの Pure DI 化。
  3. アクション保存（`SaveAction`）、削除（`RemoveAction`）、カルチャ参照を置換。
- **検証**:
  - `dotnet test` 全件合格を確認。
  - スペシャルアクションの作成・編集・保存・削除が正常に動作することを確認。

---

### Step10-4 (PR-4): 全体クリーンアップ・未検知参照ゼロ検証・完了報告
- **作業内容**:
  1. 静的コード解析を実行し、除外リスト（const定数・Pre-Host等）以外の `Global.` 直接参照がソリューション全体から完全にゼロになったことを確認。
  2. `DS4WindowsTests` および `StandaloneTests` の全自動テストを実行。
  3. 完了報告書（`Phase6-Step10-Completion-Report.md`）を作成。
- **検証**:
  - 全自動テスト 100% 合格を確認。

---

## 5. テスト・実機動作検証計画

### 5.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストクラスを追加する（1ファイル1型）：
1. **`SmallViewModelsDiWiringTests.cs`**:
   - `AutoProfilesViewModel`, `BindingWindowViewModel`, `MappingListViewModel`, `UpdaterWindowViewModel` 等が全モックを受け取って正常にインスタンス化されること。
2. **`SpecialActionEditorDiTests.cs`**:
   - 各種スペシャルアクション ViewModel の `SaveAction` 実行時に、`ISpecialActionRepository` へ正しくデータが渡ることを検証。
3. **`SaveWherePathServiceTests.cs`**:
   - `SaveWhere` ダイアログの選択結果が `IPathService` に正確に反映されること。

### 5.2 画面別手動動作検証手順（Phase6-Step11連携）
1. **起動・ダイアログ画面**:
   - 初回起動ダイアログ（`SaveWhere`）での「Appdata」「Program Folder」の選択と保存確認。
   - `WelcomeDialog` での ViGEm / HidHide インストール状態表示確認。
2. **ボタン割り当て・自動プロファイル画面**:
   - コントローラーのボタンをクリックして `BindingWindow` を開き、ボタンマッピングを変更・保存。
   - `AutoProfiles` 画面でアプリケーションとプロファイルの紐付けを作成・保存。
3. **スペシャルアクション画面**:
   - 特殊アクション（バッテリー確認、マクロ、キーコンボ等）を新規作成し、正常に保存・一覧表示されること。

---

## 6. ロールバック方針

特定画面の起動失敗やバインディング切れが発生した場合は、該当PRを直ちに `git revert` して直前の健全コミットに復旧する。

---

## 7. 完了判定チェックリスト

- [ ] 表1（グループ1: 23箇所）、表2（グループ2: 38箇所）、表3（グループ3: 11箇所）に定義された全72箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 各表に定義された明示的除外項目（const定数、Pre-Hostブートストラップ）以外の Global 直接参照が対象ファイル内に存在しないこと。
- [ ] すべての小型 View と ViewModel がペアで Pure DI 化され、バインディング切れや画面クラッシュが発生しないこと。
- [ ] `PresetOption.cs` のプリセット初期化処理が DI サービス経由で正常に機能すること。
- [ ] ソリューション全体において、Phase6 対象外と規定されたもの以外の Global 直接参照が完全にゼロ（根絶）であること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 全画面（Welcome、SaveWhere、BindingWindow、AutoProfiles、SpecialActionEditor等）の手動動作確認が正常であること。
- [ ] §8 の追加作業（出力スロット管理の `IOutputSlotService` 経由化）が完了していること。

---

## 8. 追加作業: 出力スロット管理の `IOutputSlotService` 経由化（2026-09-24 追加、Phase6-Step6 の決定1＝案R による）

### 8.1 背景
- `IOutputSlotService.PluginSlot`／`UnplugSlot` は、Phase5-Step12 で「UI からの仮想スロット操作の正式な入口」として追加されたが、UI 側の切り替え（Phase5-Step12 タスク Step12-5 の当初の意図）が行われず、呼出元 0 件のまま残っている。
- 実際の呼び出し側は、`ControlService.AttachUnboundOutDev`／`DetachUnboundOutDev` を直接呼んでいる:
  - 出力スロット管理画面: `DS4Forms/ViewModels/CurrentOutDeviceViewModel.cs`（`OutSlot_PluginRequest`／`OutSlot_UnplugRequest`）と、それに生の `ControlService`／`OutputSlotManager` を渡す `OutputSlotManagerControl.xaml.cs`（`SetupDataContext`）。
  - UDP コマンド: `DS4Forms/MainWindow.xaml.cs` の `outputslot.<n>.unplug／plugds4／plugx360`（1403〜1407 行付近）。
- 詳細な差分（スレッド、事前条件と種別、依存の取り方、イベント）は `Phase6-Step6-Plan.md` §3.1 を参照。

### 8.2 作業内容（着手時に現行コードと突き合わせて詳細化する）
1. `OutputSlotService.PluginSlot`／`UnplugSlot` を、画面側の現行の挙動と同等にする: `ControlService.EventDispatcher` へのディスパッチ、事前条件（接続状態・入力割り当て）の確認、プラグイン時の種別の決め方。
2. `OutputSlotService` の `ControlService` 取得を、`Program.rootHub` フォールバックから Pure DI へ移す。循環依存（`ControlService` → `Func<IOutputSlotService>`）を避けるため、`Func<ControlService>` による遅延解決、または仮想スロット操作用の出力ポートを導入する（Step2 決定D1 と同じ考え方）。
3. `CurrentOutDeviceViewModel`／`OutputSlotManagerControl` を、`IOutputSlotService` 経由の操作へ切り替える（View/ViewModel ペアの Pure DI 化。画面更新のイベントを `OutputSlotManager.SlotAssigned`／`SlotUnassigned` のままにするか、`OutputSlotChanged` にするかを決める）。
4. `MainWindow.xaml.cs` の UDP コマンドの plug／unplug を、`IOutputSlotService` 経由へ切り替える（`MainWindow` は Step8／Step9 でも扱うため、実施順は着手時に調整する）。
5. テスト: `PluginSlot`／`UnplugSlot` の事前条件とディスパッチ、画面 ViewModel がサービスを呼ぶこと。実機確認: 出力スロット管理画面での手動の抜き差し、恒久スロット（Permanent）の保存、UDP コマンド（環境があれば）。

### 8.3 ガードレール
- ViGEm ネイティブドライバ保護（Phase5-Plan §5.5）: `OutputSlotManager` の非同期キュー（`DeferredPlugin`／`DeferredRemoval`）と破棄順序には手を触れず、`ControlService` の正規 API への中継に留める。