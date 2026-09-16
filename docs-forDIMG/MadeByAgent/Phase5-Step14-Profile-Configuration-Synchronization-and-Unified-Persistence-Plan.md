# Phase 5 - Step 14: Profile Configuration Synchronization and Unified Persistence Plan[cite: 2]

## 1. アーキテクチャ原則と方針 (Architecture Principles & Guidelines)[cite: 2]
本ドキュメントは、プロファイル設定画面（ProfileEditor）における一部の設定項目（ProfileActions やネストされたサブ設定オブジェクトなど）の変更値がプロファイル設定ファイル（XML）へ正しくシリアライズ・保存されない不具合に対する、根本的かつ統一的な解決に向けた全体設計・実装計画である[cite: 2]。

理想のDI化移行後（DI-App-Wide-Migration-Plan.md）のレイヤーアーキテクチャおよびモデル図の設計思想に基づき、以下の原則を遵守する[cite: 2]。

* DI/レイヤー分離の遵守: UI層（ProfileEditor.xaml.cs 等）にシリアライズ形式の知識や個別書き戻しコードを置かない[cite: 2]。
* 一元的な変更検知（Dirty Tracking）: すべての設定項目（通常プロパティ、ネストオブジェクト、コレクション・特殊文字列）の変更が、モデル層で一意に検知される仕組みを構築する[cite: 2]。

---

## 2. 課題の背景と原因 (Background & Problem Statement)[cite: 2]
プロファイル編集画面の一部の設定変更（例: スペシャルアクションの有効/無効チェック、ネストされた詳細設定など）が保存されない原因は以下の通りである[cite: 2]。

* データ同期・バインディングの欠損: UI上でリストやネストプロパティが変更された際、XMLシリアライズの対象となる結合文字列（例: ProfileActions のスラッシュ区切りなど）や親モデルへの変更通知（Dirtyフラグの伝搬）が自動で行われていない[cite: 2]。
* 責務の分散: 個別のUIビハインドコード（ProfileEditor.xaml.cs）やコントロール固有のイベントに処理が依存しており、永続化レイヤーとUI層の間で状態の乖離が発生している[cite: 2]。

---

## 3. 対象となる全 9 カテゴリの詳細実装設計 (Target Settings Categories & Breakdown)[cite: 2]

不具合が顕現している項目および、同種の構造的リスク（同期漏れ・ネスト非連動）を抱えている全 9 カテゴリを統一管理の対象とする[cite: 2]。

### パターン A: コレクション・区切り文字列の双方向マッピング[cite: 2]
対象: ProfileActions, Sensitivity[cite: 2]

* ProfileActions の詳細設計:[cite: 2]
  * 課題: UIの ObservableCollection<SpecialActionItem> のチェック状態（IsActive 等）が変化した際、XMLの <ProfileActions> タグ用文字列（例: name1/name2/name3）が自動生成されない[cite: 2]。
  * 解決策:[cite: 2]
    - コレクション側、またはモデルのプロパティセッターに自動結合ロジック（UpdateProfileActionsString()）を実装[cite: 2]。
    - 各 SpecialActionItem の PropertyChanged イベントを監視し、変更があった瞬間に ProfileActions 文字列を再構築して Dirty フラグを立てる[cite: 2]。
* Sensitivity の詳細設計:[cite: 2]
  * 課題: 6つの感度値（LS, RS, L2, R2, SX, SZ）がパイプ区切り文字列（1|1|1|1|1|1）として保存されるが、個別スライダー変更時に即時文字列へ反映されない[cite: 2]。
  * 解決策: 個別プロパティ変更時に内部で配列または結合文字列を自動更新するセッターパイプラインを共通化[cite: 2]。

---

### パターン B: ネストされたサブオブジェクトの変更検知バブリング (Bubble-up)[cite: 2]
対象:[cite: 2]
* GyroControlsSettings[cite: 2]
* TouchpadAbsMouseSettings[cite: 2]
* LSOutputSettings / RSOutputSettings (FlickStick)[cite: 2]
* LSAxialDeadOptions / RSAxialDeadOptions[cite: 2]
* GyroMouseSmoothingSettings / GyroMouseStickSmoothingSettings[cite: 2]
* LSDeltaAccelSettings / RSDeltaAccelSettings[cite: 2]
* DualSenseControllerSettings.RumbleSettings[cite: 2]

* 詳細設計:[cite: 2]
  * 課題: サブクラス内のプロパティ（例: AxialDeadOptions.DeadZoneX）が変更されても、親のプロファイルコンテナ側がそれを知るすべがないため、シリアライザが変更を検知できない[cite: 2]。
  * 解決策:[cite: 2]
    - すべてのネストされたサブ設定クラスが INotifyPropertyChanged を実装する[cite: 2]。
    - サブクラスのインスタンス初期化時に、親プロファイルコンテナへの参照（またはイベントハンドラー SubObject_PropertyChanged）をバインドする[cite: 2]。
    - 子プロパティが変更された際、親側へイベントをバブリング（転送）させ、親側の Dirty 状態を強制的に true にする共通ヘルパー基盤を導入する[cite: 2]。

---

## 4. 具体的なコードパターン（実装イメージ）[cite: 2]

### A. ネストオブジェクトの変更バブリングベースパターン[cite: 2]
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

### B. ProfileActions の自動同期パターン[cite: 2]
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

---

## 5. 段階的実装計画と具体的なファイル・手順 (Step-by-Step Implementation Roadmap)[cite: 2]

* Step 1: ネストされた各サブ設定クラスへの ProfileSubSettingBase の継承とバブリング適用[cite: 2]
  * 対象ファイルパス: DS4Windows/Config/GyroControlsSettings.cs, DS4Windows/Config/TouchpadAbsMouseSettings.cs, DS4Windows/Config/AxialDeadOptions.cs, DS4Windows/Config/RumbleSettings.cs 等[cite: 2]
  * 内容: 各サブ設定クラスに ProfileSubSettingBase を継承させ、プロパティ変更時に RaisePropertyChanged() を呼び出すことでバブリングイベントを発火させる[cite: 2]。
  
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

* Step 2: 親コンテナ（BackingStore / ProfileDTO）でのサブ設定変更の購読（バブリング受取）[cite: 2]
  * 対象ファイルパス: DS4Windows/BackingStore.cs, DS4WinWPF/DS4Control/DTOXml/ProfileDTO.cs[cite: 2]
  * 内容: 子オブジェクト（サブ設定）のプロパティ変更イベント（OnSubPropertyChanged）を親クラス側で購読し、ファイル保存時の Dirty フラグを確実に連動させる[cite: 2]。
  
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

* Step 3: ProfileSettingsViewModel および UI 層（ProfileEditor）との連動確認[cite: 2]
  * 対象ファイルパス: DS4WinWPF/DS4Forms/ViewModels/ProfileSettingsViewModel.cs, DS4WinWPF/DS4Forms/ProfileEditor.xaml.cs[cite: 2]
  * 内容: UIからの入力変更値が ViewModel を経由して各サブ設定のプロパティセッターへ確実に到達し、バブリングを通じてモデルまで変更が伝播することを確認[cite: 2]。

* Step 4: 統合ユニットテストによる永続化の検証[cite: 2]
  * 対象ファイルパス: DS4WindowsTests/ProfileRepositoryTests.cs (または新規テストファイル)[cite: 2]
  * 内容: ネストされたサブ設定の値を変更した状態で ProfileRepository.SaveProfile を実行し、生成される XML ファイルにその変更値が正しく出力されているかを自動テストで保証する[cite: 2]。
  
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

---

## 6. 期待される効果 (Expected Benefits)[cite: 2]
* 網羅性と堅牢性: 今回問題となった項目だけでなく、将来追加されるすべての設定項目に対しても一貫した保存・読込の信頼性を担保できる[cite: 2]。
* 保守性の向上: DI移行計画の思想（責務の分離と明確なレイヤー依存関係）に合致し、コードの複雑性を大幅に軽減する[cite: 2]。