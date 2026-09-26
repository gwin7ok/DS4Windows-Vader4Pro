# Phase6-Step8 計画書: `ProfileEditor.xaml.cs` の段階的MVVM移設と `MainWindow.xaml.cs` 解体推進

作成日: 2026-09-11  
改訂日: 2026-09-18（Phase6-Step1 再監査エビデンスに基づく案3-A［段階的MVVM移設による Fat View 解体・Pure DI 推進］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
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

## 2. 全件参照台帳（全60箇所・完全カタログ）

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

## 3. アーキテクチャ設計: 案3-A（MVVM 移設と契約差吸収）

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

## 4. PR分割計画とマイクロステップ

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

## 5. テスト・実機動作検証計画

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

## 7. 完了判定チェックリスト

- [ ] `ProfileEditor.xaml.cs` 内のバックエンド操作ロジックが `ProfileSettingsViewModel` へ移設されていること。
- [ ] 表1に定義された全60箇所の実利用 Global 直接参照が完全に解消されていること。
- [ ] 表2に定義された除外項目（const定数38箇所）以外の Global 直接参照が `ProfileEditor.xaml.cs` 内に存在しないこと。
- [ ] 重要契約差（B8, B9, B10）の薄いシムが実装され、プロファイル保存・適用の挙動が完全に保護されていること。
- [ ] `MainWindow.xaml.cs` からのエディタ呼び出しコードがスリム化され、MainWindow 解体が前進していること。
- [ ] UI を起動せずに `ProfileSettingsViewModel.SaveProfile()` を検証する単体テストが追加され、合格していること。
- [ ] `dotnet build -c Release` でエラー・警告が0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーを用いたプロファイル編集・保存・即時反映が正常に動作すること。