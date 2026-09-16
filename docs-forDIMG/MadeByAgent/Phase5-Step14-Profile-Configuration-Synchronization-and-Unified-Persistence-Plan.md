# Phase 5 - Step 14: Profile Configuration Synchronization and Unified Persistence Plan

## 1. アーキテクチャ原則と方針 (Architecture Principles & Guidelines)
本ドキュメントは、プロファイル設定画面（`ProfileEditor`）における一部の設定項目（`ProfileActions` やネストされたサブ設定オブジェクトなど）の変更値がプロファイル設定ファイル（XML）へ正しくシリアライズ・保存されない不具合に対する、根本的かつ統一的な解決に向けた全体設計・実装計画である。

理想のDI化移行後（`DI-App-Wide-Migration-Plan.md`）のレイヤーアーキテクチャおよびモデル図の設計思想に基づき、以下の原則を遵守する。

* **DI/レイヤー分離の遵守**: UI層（`ProfileEditor.xaml.cs` 等）にシリアライズ形式の知識や個別書き戻しコードを置かない。
* **一元的な変更検知（Dirty Tracking）**: すべての設定項目（通常プロパティ、ネストオブジェクト、コレクション・特殊文字列）の変更が、モデル層で一意に検知される仕組みを構築する。

---

## 2. 課題の背景と原因 (Background & Problem Statement)
プロファイル編集画面の一部の設定変更（例: スペシャルアクションの有効/無効チェック、ネストされた詳細設定など）が保存されない原因は以下の通りである。

* **データ同期・バインディングの欠損**: UI上でリストやネストプロパティが変更された際、XMLシリアライズの対象となる結合文字列（例: `ProfileActions` のスラッシュ区切りなど）や親モデルへの変更通知（Dirtyフラグの伝搬）が自動で行われていない。
* **責務の分散**: 個別のUIビハインドコード（`ProfileEditor.xaml.cs`）やコントロール固有のイベントに処理が依存しており、永続化レイヤーとUI層の間で状態の乖離が発生している。

---

## 3. 対象となる全 9 カテゴリの詳細実装設計 (Target Settings Categories & Breakdown)

不具合が顕現している項目および、同種の構造的リスク（同期漏れ・ネスト非連動）を抱えている全 9 カテゴリを統一管理の対象とする。

### パターン A: コレクション・区切り文字列の双方向マッピング
対象: `ProfileActions`, `Sensitivity`

* **`ProfileActions` の詳細設計**:
  * **課題**: UIの `ObservableCollection<SpecialActionItem>` のチェック状態（`IsActive` 等）が変化した際、XMLの `<ProfileActions>` タグ用文字列（例: `name1/name2/name3`）が自動生成されない。
  * **解決策**: 
    - コレクション側、またはモデルのプロパティセッターに自動結合ロジック（`UpdateProfileActionsString()`）を実装。
    - 各 `SpecialActionItem` の `PropertyChanged` イベントを監視し、変更があった瞬間に `ProfileActions` 文字列を再構築して Dirty フラグを立てる。
* **`Sensitivity` の詳細設計**:
  * **課題**: 6つの感度値（LS, RS, L2, R2, SX, SZ）がパイプ区切り文字列（`1|1|1|1|1|1`）として保存されるが、個別スライダー変更時に即時文字列へ反映されない。
  * **解決策**: 個別プロパティ変更時に内部で配列または結合文字列を自動更新するセッターパイプラインを共通化。

---

### パターン B: ネストされたサブオブジェクトの変更検知バブリング (Bubble-up)
対象: 
* `GyroControlsSettings`
* `TouchpadAbsMouseSettings`
* `LSOutputSettings` / `RSOutputSettings` (FlickStick)
* `LSAxialDeadOptions` / `RSAxialDeadOptions`
* `GyroMouseSmoothingSettings` / `GyroMouseStickSmoothingSettings`
* `LSDeltaAccelSettings` / `RSDeltaAccelSettings`
* `DualSenseControllerSettings.RumbleSettings`

* **詳細設計**:
  * **課題**: サブクラス内のプロパティ（例: `AxialDeadOptions.DeadZoneX`）が変更されても、親のプロファイルコンテナ側がそれを知るすべがないため、シリアライザが変更を検知できない。
  * **解決策**:
    - すべてのネストされたサブ設定クラスが `INotifyPropertyChanged` を実装する。
    - サブクラスのインスタンス初期化時に、親プロファイルコンテナへの参照（またはイベントハンドラー `SubObject_PropertyChanged`）をバインドする。
    - 子プロパティが変更された際、親側へイベントをバブリング（転送）させ、親側の Dirty 状態を強制的に `true` にする共通ヘルパー基盤を導入する。

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

## 5. 段階的実装計画 (Step-by-Step Implementation Plan)

* **Step 1: サブオブジェクトの変更検知バブリング基盤の整備**
  * ネストされた設定クラス群に共通の変更通知メカニズムを適用し、子要素の変更が親プロファイルモデルへ確実に伝達されるようにする。
* **Step 2: 特殊コレクション・文字列の自動同期ハンドラーの導入**
  * `ProfileActions` や `Sensitivity` などの複合文字列データを管理するセッター／同期ヘルパーを定義し、UI操作からの保存漏れを構造的に根絶する。
* **Step 3: `ProfileEditor` のバインディング処理の整理と集約**
  * UIイベントハンドラーに散らばっていた個別ロジックを整理し、統一されたモデル更新フローへ統合する。
* **Step 4: ユニットテストによる品質担保 (Unit Testing)**
  * 各種設定（特に `ProfileActions` やネスト設定）を変更した上で保存処理をシミュレートし、生成される XML に期待値が正しく書き出されているかを検証する自動テストコードを `DS4Windows.Actions.Tests` に追加する。

---

## 6. 期待される効果 (Expected Benefits)
* **網羅性と堅牢性**: 今回問題となった項目だけでなく、将来追加されるすべての設定項目に対しても一貫した保存・読込の信頼性を担保できる。
* **保守性の向上**: DI移行計画の思想（責務の分離と明確なレイヤー依存関係）に合致し、コードの複雑性を大幅に軽減する。