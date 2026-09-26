# Phase 5 - Step 14: Profile Configuration Synchronization and Unified Persistence Plan

## 1. アーキテクチャ原則と方針 (Architecture Principles & Guidelines)
本ドキュメントは、プロファイル設定画面（ProfileEditor）における一部の設定項目（ProfileActions やネストされたサブ設定オブジェクトなど）の変更値がプロファイル設定ファイル（XML）へ正しくシリアライズ・保存されない不具合に対する、根本的かつ統一的な解決に向けた全体設計・実装計画である。

理想のDI化移行後（DI-App-Wide-Migration-Plan.md）のレイヤーアーキテクチャおよびモデル図の設計思想に基づき、以下の原則を遵守する。

* DI/レイヤー分離の遵守: UI層（ProfileEditor.xaml.cs 等）にシリアライズ形式の知識や個別書き戻しコードを置かない。
* 一元的な変更検知（Dirty Tracking）: すべての設定項目（通常プロパティ、ネストオブジェクト、コレクション・特殊文字列）の変更が、モデル層で一意に検知される仕組みを構築する。

---

## 2. 課題の背景と原因 (Background & Problem Statement)
プロファイル編集画面の一部の設定変更（例: スペシャルアクションの有効/無効チェック、ネストされた詳細設定など）が保存されない原因は以下の通りである。

* データ同期・バインディングの欠損: UI上でリストやネストプロパティが変更された際、XMLシリアライズの対象となる結合文字列（例: ProfileActions のスラッシュ区切りなど）や親モデルへの変更通知（Dirtyフラグの伝搬）が自動で行われていない。
* 責務の分散: 個別のUIビハインドコード（ProfileEditor.xaml.cs）やコントロール固有のイベントに処理が依存しており、永続化レイヤーとUI層の間で状態の乖離が発生している。

---

## 3. 対象となる全 9 カテゴリの詳細実装設計 (Target Settings Categories & Breakdown)

不具合が顕現している項目および、同種の構造的リスク（同期漏れ・ネスト非連動）を抱えている全 9 カテゴリを統一管理の対象とする。

### パターン A: コレクション・区切り文字列の双方向マッピング
対象: ProfileActions, Sensitivity

* ProfileActions の詳細設計:
  * 課題: UIの ObservableCollection<SpecialActionItem> のチェック状態（IsActive 等）が変化した際、XMLの <ProfileActions> タグ用文字列（例: name1/name2/name3）が自動生成されない。
  * 解決策:
    - コレクション側、またはモデルのプロパティセッターに自動結合ロジック（UpdateProfileActionsString()）を実装。
    - 各 SpecialActionItem の PropertyChanged イベントを監視し、変更があった瞬間に ProfileActions 文字列を再構築して Dirty フラグを立てる。
* Sensitivity の詳細設計:
  * 課題: 6つの感度値（LS, RS, L2, R2, SX, SZ）がパイプ区切り文字列（1|1|1|1|1|1）として保存されるが、個別スライダー変更時に即時文字列へ反映されない。
  * 解決策: 個別プロパティ変更時に内部で配列または結合文字列を自動更新するセッターパイプラインを共通化。

---

### パターン B: ネストされたサブオブジェクトの変更検知バブリング (Bubble-up)
対象:
* GyroControlsSettings
* TouchpadAbsMouseSettings
* LSOutputSettings / RSOutputSettings (FlickStick)
* LSAxialDeadOptions / RSAxialDeadOptions
* GyroMouseSmoothingSettings / GyroMouseStickSmoothingSettings
* LSDeltaAccelSettings / RSDeltaAccelSettings
* DualSenseControllerSettings.RumbleSettings

* 詳細設計:
  * 課題: サブクラス内のプロパティ（例: AxialDeadOptions.DeadZoneX）が変更されても、親のプロファイルコンテナ側がそれを知るすべがないため、シリアライザが変更を検知できない。
  * 解決策:
    - すべてのネストされたサブ設定クラスが INotifyPropertyChanged を実装する。
    - サブクラスのインスタンス初期化時に、親プロファイルコンテナへの参照（またはイベントハンドラー SubObject_PropertyChanged）をバインドする。
    - 子プロパティが変更された際、親側へイベントをバブリング（転送）させ、親側の Dirty 状態を強制的に true にする共通ヘルパー基盤を導入する。

---

## 4. 具体的なコードパターン（実装イメージ）

### A. ネストオブジェクトの変更バブリングベースパターン
```csharp
public abstract class ProfileSubSettingBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public event Action<string> OnSubPropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        OnSubPropertyChanged?.Invoke(propertyName);
    }
}
```

### B. ProfileActions の自動同期パターン
```csharp
public class ProfileConfigurationModel : INotifyPropertyChanged
{
    private ObservableCollection<SpecialActionItem> _specialActions;
    
    public ObservableCollection<SpecialActionItem> SpecialActions 
    { 
        get => _specialActions;
        set
        {
            if (_specialActions != value)
            {
                if (_specialActions != null) _specialActions.CollectionChanged -= OnActionsChanged;
                _specialActions = value;
                if (_specialActions != null) _specialActions.CollectionChanged += OnActionsChanged;
                RaisePropertyChanged();
            }
        }
    }

    private void OnActionsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        SyncProfileActionsString();
    }

    public string ProfileActions { get; private set; }

    private void SyncProfileActionsString()
    {
        ProfileActions = string.Join("/", SpecialActions.Where(a => a.IsActive).Select(a => a.Name));
        RaisePropertyChanged(nameof(ProfileActions));
    }
}
```

---

## 5. 段階的実装計画と具体的なファイル・手順 (Step-by-Step Implementation Roadmap)

* Step 1: ネストされた各サブ設定クラスへの ProfileSubSettingBase の継承とバブリング適用
  * 対象ファイルパス: DS4Windows/Config/GyroControlsSettings.cs, DS4Windows/Config/TouchpadAbsMouseSettings.cs, DS4Windows/Config/AxialDeadOptions.cs, DS4Windows/Config/RumbleSettings.cs 等
  * 内容: 各サブ設定クラスに ProfileSubSettingBase を継承させ、プロパティ変更時に RaisePropertyChanged() を呼び出すことでバブリングイベントを発火させる。
  
  ```csharp
  namespace DS4Windows
  {
      public class GyroControlsSettings : ProfileSubSettingBase
      {
          private int _gyroMode;
          public int GyroMode
          {
              get => _gyroMode;
              set
              {
                  if (_gyroMode != value)
                  {
                      _gyroMode = value;
                      RaisePropertyChanged();
                  }
              }
          }
      }
  }
  ```

* Step 2: 親コンテナ（BackingStore / ProfileDTO）でのサブ設定変更の購読（バブリング受取）
  * 対象ファイルパス: DS4Windows/BackingStore.cs, DS4WinWPF/DS4Control/DTOXml/ProfileDTO.cs
  * 内容: 子オブジェクト（サブ設定）のプロパティ変更イベント（OnSubPropertyChanged）を親クラス側で購読し、ファイル保存時の Dirty フラグを確実に連動させる。
  
  ```csharp
  public class BackingStore
  {
      private GyroControlsSettings _gyroControls = new GyroControlsSettings();
      public GyroControlsSettings GyroControls
      {
          get => _gyroControls;
          set
          {
              if (_gyroControls != value)
              {
                  if (_gyroControls != null) _gyroControls.OnSubPropertyChanged -= SubObject_OnSubPropertyChanged;
                  _gyroControls = value;
                  if (_gyroControls != null) _gyroControls.OnSubPropertyChanged += SubObject_OnSubPropertyChanged;
                  MarkAsDirty();
              }
          }
      }
      public bool IsDirty { get; private set; } = false;

      public BackingStore()
      {
          _gyroControls.OnSubPropertyChanged += SubObject_OnSubPropertyChanged;
      }

      private void SubObject_OnSubPropertyChanged(string propertyName) => MarkAsDirty();
      private void MarkAsDirty() => IsDirty = true;
  }
  ```

* Step 3: ProfileSettingsViewModel および UI 層（ProfileEditor）との連動確認
  * 対象ファイルパス: DS4WinWPF/DS4Forms/ViewModels/ProfileSettingsViewModel.cs, DS4WinWPF/DS4Forms/ProfileEditor.xaml.cs
  * 内容: UIからの入力変更値が ViewModel を経由して各サブ設定のプロパティセッターへ確実に到達し、バブリングを通じてモデルまで変更が伝播することを確認。

* Step 4: 統合ユニットテストによる永続化の検証
  * 対象ファイルパス: DS4WindowsTests/ProfileRepositoryTests.cs (または新規テストファイル)
  * 内容: ネストされたサブ設定の値を変更した状態で ProfileRepository.SaveProfile を実行し、生成される XML ファイルにその変更値が正しく出力されているかを自動テストで保証する。
  
  ```csharp
  [Fact]
  public void NestedSubSetting_Change_ShouldBePersistedToXml()
  {
      var settings = new ProfileSettingsService();
      var repository = new ProfileRepository(settings);
      int deviceIndex = 0;
      string profileName = "NestedTestProfile";

      settings.GetBackingStore(deviceIndex).GyroControls.GyroMode = 2;
      bool saveResult = repository.SaveProfile(deviceIndex, profileName);
      Assert.True(saveResult);

      string filePath = repository.GetProfilePath(profileName);
      Assert.True(File.Exists(filePath));
      string xmlContent = File.ReadAllText(filePath);
      Assert.Contains("2", xmlContent);

      if (File.Exists(filePath)) File.Delete(filePath);
  }
  ```

---

## 6. 期待される効果 (Expected Benefits)
* 網羅性と堅牢性: 今回問題となった項目だけでなく、将来追加されるすべての設定項目に対しても一貫した保存・読込の信頼性を担保できる。
* 保守性の向上: DI移行計画の思想（責務の分離と明確なレイヤー依存関係）に合致し、コードの複雑性を大幅に軽減する。

---

## 7. 【追加計画】RumbleSettings の正式ネスト化とサブ設定宣言的配線リファクタリング（選択肢C）

### 7.1 現状の課題と経緯
1. **実プロファイルXML構造の維持**:
   実プロファイル（例: `原神DS4_for_gwin.xml`）において、ランブル設定は以下のように表現されている。
   - ルート直下の共通フラット設定: `<RumbleBoost>`, `<RumbleAutostopTime>`
   - コントローラー個別設定内のネスト設定: `<DualSenseControllerSettings>` 配下の `<RumbleSettings>`（`<EmulationMode>`, `<EnableGenericRumbleRescale>`, `<HapticPowerLevel>`）
2. **内部実装のねじれ**:
   - `ProfilePropGroups.cs` の `RumbleSettings : ProfileSubSettingBase` は `new` されておらず UI からも未使用の「孤立クラス」であった。
   - `ProfileSettingsService` 側ではルート直下相当のランブル設定がフラットな配列プロパティとして保持されていたが、セッターで `OnProfileSettingChanged` が呼ばれておらず、ランブル設定単体変更時の Dirty 検知（UI の Apply ボタン活性化）が欠落していた。
   - `WireSubSettingsEvents` メソッド内で各サブ設定のイベント購読を1行ずつ手動記述していたため、購読漏れ（配線忘れ）が発生するリスクが存在していた。

### 7.2 採択方針
1. **RumbleSettings の正式ネストオブジェクト化**:
   孤立クラスを廃止・放置せず、`ProfileSubSettingBase` を継承する正規のネスト設定オブジェクトとして再構築・統合する。
2. **宣言的配線 ＋ 完全性検証テスト（選択肢C）の採用**:
   本番コードは明示的登録リスト（ラムダ式アクセサ群）を反復処理する方式とし、テストプロジェクト側でリフレクションを用いて「到達可能なすべての `ProfileSubSettingBase` インスタンスが登録リストに含まれているか」を機械的・網羅的に検証する。
3. **XMLファイル完全後方互換性の維持**:
   内部オブジェクトモデルがネスト化されても、シリアライズ（保存）およびデシリアライズ（読込）時のXMLタグ配置・階層構造は従来通りを100%維持し、既存の全プロファイルXML（`原神DS4_for_gwin.xml` 等）との完全な相互互換性を担保する。

### 7.3 詳細実装計画

#### タスク 7.3.1: RumbleSettings の正規化と ProfileSettingsService 統合
- **対象ファイル**:
  - `DS4Windows/DS4Control/ProfilePropGroups.cs`
  - `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`
  - `DS4Windows/DS4Control/DTOXml/ProfileDTO.cs` / `XmlDataUtilities.cs`
- **内容**:
  1. `RumbleSettings` クラスのプロパティ定義を整備し、`ProfileSubSettingBase` に基づく変更通知（Dirty通知）を正しく伝播させる。
  2. `ProfileSettingsService` 内のフラットなランブル保持構造を `RumbleSettings` オブジェクトへ移行・集約する。
  3. UIバインディング（ProfileEditor等）および外部公開プロパティは透過的なアクセサを提供し、既存UI/ViewModelとの互換性を維持する。
  4. XMLの読込・保存時、ルート直下の `<RumbleBoost>`, `<RumbleAutostopTime>` および `<DualSenseControllerSettings>` 配下のタグと内部 `RumbleSettings` 間で過不足なくマッピングを行い、XMLスキーマを一切破壊しない。

#### タスク 7.3.2: SubSettings 宣言的配線（Declarative Wiring）へのリファクタリング
- **対象ファイル**:
  - `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`
- **内容**:
  1. `WireSubSettingsEvents` / `UnwireSubSettingsEvents` を手動個別記述からデータ駆動設計へ刷新。
  2. サブ設定インスタンスへのアクセス式（ラベル、Getterラムダ）をまとめた宣言的登録リスト（例: `IEnumerable<Func<ProfileSettingsService, int, ProfileSubSettingBase>>`）を定義。
  3. 購読・購読解除処理は、このリストを反復処理して `ProfileSubSettingBase.SettingChanged` を一括で結線・解除する構造に変更。

#### タスク 7.3.3: 単体テストによる完全性検証テスト（Integrity Test）の実装
- **対象ファイル**:
  - `DS4WindowsTests/ProfileSettingsServiceSubSettingsTests.cs`
- **内容**:
  1. リフレクションを用いて `ProfileSettingsService`（および配下のネストオブジェクト・配列）から到達可能な `ProfileSubSettingBase` 派生インスタンスを再帰走査・収集するヘルパーをテストプロジェクト内に作成。
  2. 「リフレクションで走査・検出されたサブ設定のインスタンス群」と「宣言的登録リストにより実際に配線されたインスタンス群」を照合・比較するアサーションを実装。
  3. 未登録の `ProfileSubSettingBase` が存在する場合はテストを即座に失敗させ、今後の機能追加時における配線漏れをCI段階で自動検知・防止する。

### 7.4 検証手順および完了基準
- [x] `DS4WindowsWPF.sln` の完全ビルドが警告・エラーなく通過すること。
- [x] 単体テスト（完全性検証テスト含む）がすべてパスすること。
- [x] 実プロファイル（`原神DS4_for_gwin.xml` 等）の読み込み・保存でXMLの差分・タグ構造の破壊が発生しないこと。
- [x] ProfileEditor 画面でランブル設定のみを変更した際、変更が即座に検知されプロファイル適用（Apply）が可能になること。

---

## 8. 【追加計画2】RumbleSettings の BackingStore 移設による SSOT 化（選択肢A）および適用ボタンのフラグ制御撤廃

### 8.1 課題と背景
1. **プロファイル切替時の値非追従（新規リグレッション）**:
   - タスク 7.3.1 において、`ProfileSettingsService` 内に独自インスタンスの並行配列 `_rumbleSettings` を保持し、コンストラクタで一度だけコピー初期化する設計としていた。
   - しかし、実際のプロファイル読込処理（`ScpUtil.cs` 内の `LoadProfile`）は XML の `<RumbleBoost>` / `<RumbleAutostopTime>` の値を `BackingStore` の生配列（`rumble[device]` 等）に直接代入する。
   - このため、プロファイルを切り替えても `_rumbleSettings` が置き去りになり、実機へ送信されるランブル設定値が新しいプロファイルの値に切り替わらない構造的不整合が発生していた。
2. **他の8カテゴリとのアーキテクチャ乖離**:
   - `gyroControlsInf` 等の他の8カテゴリはすべて `BackingStore` が配列の実体を保持し、`ProfileSettingsService` はそこへの「薄いパススルー」となっていた。RumbleSettings のみ並行コピー配列を持っていたことが根本原因であった。
3. **適用ボタン（Apply）のフラグ制御によるユーザー体験阻害**:
   - 変更検知フラグやイベント購読により `applyBtn.IsEnabled` の有効/無効を切り替えていたが、画面の初期ロード状態や特定の操作経路でボタンが無効のままとなり、ユーザーが意図したタイミングで適用できない問題が生じていた。

### 8.2 採択方針
1. **選択肢A（BackingStore への完全移設・SSOT化）の採用**:
   - 他の8カテゴリと完全に同一の設計原則に統一する。
   - `BackingStore` に `Global.TEST_PROFILE_ITEM_COUNT`（9スロット分）の `RumbleSettings[]` を正式配置し、インスタンス生成・保持を一元化する。
   - `ProfileSettingsService.RumbleSettings` は `SafeConfig.rumbleSettings` への純粋な薄いパススルーとする。
   - `LoadProfile`（XML読込）時に、`rumbleSettings[device]` のプロパティへも値を直接設定する。
2. **適用ボタンのフラグ制御撤廃・常時有効化**:
   - プロファイル保存は全項目をXMLへ完全上書き（フルライト）する冪等な処理であるため、ボタンの可否制御フラグおよび購読ハンドラを全撤去し、常にクリック可能な常時有効設計とする。

### 8.3 詳細実装タスク

#### タスク 8.3.1: BackingStore への RumbleSettings 配列移設と連動
- **対象ファイル**: `DS4Windows/DS4Control/ScpUtil.cs`
- **内容**:
  1. `BackingStore` クラスのフィールドに `rumbleSettings` 配列（サイズ9）を定義し、全要素をインスタンス化。
  2. `BackingStore` コンストラクタ内で、各スロットの `RumbleSettingsChanged` イベントを生配列 `rumble` / `rumbleAutostopTime` に連動させるリスナーを接続。
  3. `LoadProfile` メソッド内で、`<RumbleBoost>` / `<RumbleAutostopTime>` をパースした際に `rumbleSettings[device]` にも即時代入する。

#### タスク 8.3.2: ProfileSettingsService の薄いパススルー化
- **対象ファイル**: `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`
- **内容**:
  1. 独自の `_rumbleSettings` 配列、`InitRumbleSettings()` メソッド、手動双方向同期リスナーを完全削除。
  2. `public RumbleSettings[] RumbleSettings => SafeConfig?.rumbleSettings;` のパススルー定義に変更。
  3. `GetRumbleBoost` / `GetRumbleAutostopTime` / `SetRumbleAutostopTime` も `SafeConfig.rumbleSettings` を直接参照するよう簡素化。

#### タスク 8.3.3: 適用ボタンの制御コード完全撤去
- **対象ファイル**: `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`
- **内容**:
  1. `SetupEvents` / `UnregisterEvents` から `ProfileSettingsService_ProfileSettingChanged` ハンドラの結線・解除コードを削除。
  2. ハンドラメソッド自体、および各イベント内の `applyBtn.IsEnabled` 操作コードを全削除。
  3. コンストラクタ等で `applyBtn.IsEnabled = true;` を常時担保。

### 8.4 検証手順および完了基準
- [x] ビルドおよび全単体テスト（178件）が成功すること。
- [x] 実機検証にて、異なるランブル値を持つ2つのプロファイルを切り替えた際、UIおよび実機モーター出力（Test Heavy/Light）が正常に追従すること。
- [x] 適用ボタンが変更有無にかかわらず常時利用可能であり、プロファイル保存・適用が確実に機能すること。