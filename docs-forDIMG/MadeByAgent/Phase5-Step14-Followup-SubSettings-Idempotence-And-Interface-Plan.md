# Phase 5 Step 14 フォローアップ計画書: サブ設定変更通知の冪等化・バブリング配線補完および ProfileActions インターフェース化（改定版）

## 1. 背景と目的

Phase 5 Step 14 の完了レビューおよび実機・ビルド検証において、以下の技術的課題が判明した。
本フォローアップ計画では、サービス層（`ProfileSettingsService`）および UI 層（`ProfileEditor.xaml.cs`）における設定変更通知の堅牢性向上、メモリリーク防止、静的参照の排除を完了することを目的とする。

1. **課題① サブ設定イベント購読の多重蓄積（冪等性の欠如）**:
   `WireSubSettingsEvents(int deviceIndex)` において無名ラムダ式等で購読を行っているため、プロファイル再読み込みのたびにハンドラが累積し、1 回の値変更に対して多重発火・メモリリークが発生するリスクがある。
2. **課題② `DeltaAccelSettings` の変更通知バブリング未配線**:
   `DeltaAccelSettings` は `ProfileSubSettingBase` を継承しているが、`WireSubSettingsEvents` の監視ループに含まれておらず、UI 側での変更がサービス層へ伝播しない。
3. **課題③ サブ設定クラス群における `ProfileSubSettingBase` 継承状況の不一致**:
   すべてのサブ設定が `ProfileSubSettingBase`（`OnSubPropertyChanged`）を持つわけではないため、継承・非継承を厳密にインベントリ化し、不適切なイベント登録によるコンパイルエラー・例外を防止する必要がある。
4. **課題④ データストア構造（`BackingStore`）と静的 `Global.ProfileActions` の整合性**:
   `Global.ProfileActions` は配列プロパティ自体が `get;` のみ（読み取り専用）であるため、直接再代入ができず配列要素単位の同期が必要である。また、サービス層の実体は `Global.SafeConfig` ではなく `BackingStore`（`Global.store`）である。
5. **課題⑤ `ProfileEditor.xaml.cs` における静的 `Global.ProfileActions` の残存**:
   UI 層で `Global.ProfileActions` への直接参照・更新が残存しており、DI 注入された `profileSettings` 経由への統一が必要である。

---

## 2. 現状課題と技術的知見の整理

### 2.1 課題①: サブ設定イベント購読の冪等化（デリゲート解除パイプライン）

#### 現状の課題
- `ProfileSettingsService.WireSubSettingsEvents(deviceIndex)` では、`lsMod.xAxisDeadInfo.OnSubPropertyChanged += ...` のようにイベントを購読している。
- 解除機構が存在しないため、`LoadProfile` やプロファイル切り替えが行われるたびにハンドラが累積する。

#### 解決策
- スロット（`deviceIndex`）ごとに登録解除デリゲートのリストを保持するキャッシュコレクション（`Dictionary<int, List<Action>> _subSettingsUnwireMap`）を新設。
- `UnwireSubSettingsEvents(int deviceIndex)` メソッドを新設し、キャッシュ内の解除デリゲートをすべて実行して `-=` 解除を行う。
- `WireSubSettingsEvents(int deviceIndex)` の冒頭で必ず `UnwireSubSettingsEvents(deviceIndex)` を呼び出し、**「全解除 → 再登録」パイプラインによる 100% の冪等性** を保証する。

---

### 2.2 課題②: `DeltaAccelSettings` のサービス層バブリング配線

#### 現状の課題
- スティックの加速度補正を担う `DeltaAccelSettings` は `ProfileSubSettingBase` を継承しているが、`ProfileSettingsService` 内で配線されていない。

#### 解決策
- 各スロットの左スティック出力設定（`LSOutputSettings[deviceIndex]`）および右スティック出力設定（`RSOutputSettings[deviceIndex]`）配下の `controlSettings.deltaAccelSettings` を購読対象に追加。
- 発火キー仕様:
  - 左スティック: `"LS_DeltaAccel_" + prop`
  - 右スティック: `"RS_DeltaAccel_" + prop`
- `UnwireSubSettingsEvents` においても確実に解除クロージャを登録する。

---

### 2.3 課題③: サブ設定クラス群の `ProfileSubSettingBase` 継承状況インベントリ

サブ設定オブジェクトのうち、`ProfileSubSettingBase` を継承し `OnSubPropertyChanged` を持つクラスと、持たない通常クラスの区分は以下の通りである。**非継承クラスに対してイベント購読コードを記述してはならない**。

| クラス名 / プロパティ | `ProfileSubSettingBase` 継承 | イベント通知（`OnSubPropertyChanged`） | `WireSubSettingsEvents` 購読対象 | 備考 |
| :--- | :---: | :---: | :---: | :--- |
| `StickDeadZoneInfo` (xAxis / yAxis) | **あり** | **あり** | **対象** | スティック軸デッドゾーン (`LS_X_`, `LS_Y_`, `RS_X_`, `RS_Y_`) |
| `TriggerDeadZoneZInfo` | **あり** | **あり** | **対象** | トリガーデッドゾーン (`L2_`, `R2_`) |
| `GyroControlsInfo` | **あり** | **あり** | **対象** | ジャイロ制御 (`GyroControls_`) |
| `TouchpadAbsMouseSettings` | **あり** | **あり** | **対象** | タッチパッド絶対座標 (`TouchAbs_`) |
| `StickOutputSettings` | **あり** | **あり** | **対象** | スティック出力曲線設定 (`LSOutput_`, `RSOutput_`) |
| `TriggerOutputSettings` | **あり** | **あり** | **対象** | トリガー出力設定 (`L2Output_`, `R2Output_`) |
| `DeltaAccelSettings` | **あり** | **あり** | **対象（新規追加）** | スティック加速度設定 (`LS_DeltaAccel_`, `RS_DeltaAccel_`) |
| `SensitivitySettings` | **あり** | **あり** | **対象** | 感度詳細設定 |
| `GyroMouseStickInfo` | **あり** | **あり** | **対象** | ジャイロマウスクスティック (`GyroMouseStick_`) |
| `GyroMouseInfo` | **あり** | **あり** | **対象** | ジャイロマウス (`GyroMouse_`) |
| `TouchMouseStickInfo` | **あり** | **あり** | **対象** | タッチマウスクスティック (`TouchMouseStick_`) |
| `TouchpadRelMouseSettings` | **なし** | **なし** | **対象外** | 通常 POCO クラス（購読すると CS1061 エラー） |
| `SquareStickInfo` | **なし** | **なし** | **対象外** | 通常 POCO クラス（購読すると CS1061 エラー） |
| `StickAntiSnapbackInfo` | **なし** | **なし** | **対象外** | 通常 POCO クラス（購読すると CS1061 エラー） |
| `GyroSwipeInfo` | **なし** | **なし** | **対象外** | 通常 POCO クラス |

---

### 2.4 課題④: データストア構造（`BackingStore`）と静的 `Global.ProfileActions` の仕様

1. **データストアの参照階層**:
   - `ProfileSettingsService` 内のバッキングストア実体は `BackingStore SafeConfig => _config ?? Global.store;` である。
   - `Global.SafeConfig` や `Global.lsModInfo` 等の静的プロパティは存在しないため、直接 `SafeConfig.lsModInfo` 経由でアクセスする。
2. **`Global.ProfileActions` の代入制約**:
   - `Global.ProfileActions` は `{ get; }`（読み取り専用プロパティ）であるため、`Global.ProfileActions = value;` はコンパイルエラー（CS0200）となる。
   - `ProfileActions` セッターでは、`SafeConfig.profileActions = value;` への代入を行うとともに、`Global.ProfileActions` の各配列要素を `value[i]` で同期する。

---

### 2.5 課題⑤: `ProfileEditor.xaml.cs` における静的 `Global.ProfileActions` の置換

`ProfileEditor.xaml.cs` 内には以下の 3 箇所で `Global.ProfileActions` への直接参照・更新が残存している。すでにコンストラクタ経由で注入されている `profileSettings` フィールドを活用し、インターフェースプロパティ `profileSettings.ProfileActions` 経由へ統一する。

1. **`Reload` メソッド**:
   プロファイル再読み込み時にスロットごとのアクション一覧を取得する箇所。
   ```csharp
   // 変更前
   List<string> actions = Global.ProfileActions[deviceNum];
   // 変更後
   List<string> actions = profileSettings.ProfileActions[deviceNum];
   ```
2. **`ExecuteSaveOrApply` メソッド**:
   プロファイル保存・適用時にアクションリストを同期・更新する箇所。
   ```csharp
   // 変更前
   Global.ProfileActions[deviceNum] = currentActions;
   // 変更後
   profileSettings.ProfileActions[deviceNum] = currentActions;
   ```
3. **`SpecialActionCheckBox_Click` メソッド**:
   特殊アクションのチェックボックス操作時にリストを更新する箇所。
   ```csharp
   // 変更前
   Global.ProfileActions[deviceNum].Add(actionName);
   // 変更後
   profileSettings.ProfileActions[deviceNum].Add(actionName);
   ```

---

## 3. 詳細設計

### 3.1 `IProfileSettingsService` の拡張

```csharp
namespace DS4Windows
{
    public interface IProfileSettingsService
    {
        // 既存メンバー（プロパティ、アクセスメソッド群）...

        // 課題④: ProfileActions のインターフェース公開
        List<string>[] ProfileActions { get; set; }

        // 課題①: 冪等化のための購読全解除メソッド
        void UnwireSubSettingsEvents(int deviceIndex);

        // 既存の購読メソッド
        void WireSubSettingsEvents(int deviceIndex);
    }
}
```

---

### 3.2 `ProfileSettingsService` の実装設計

```csharp
namespace DS4Windows
{
    public class ProfileSettingsService : IProfileSettingsService
    {
        private readonly object _subSettingsLock = new object();
        // スロットごとの購読解除アクション一覧
        private readonly Dictionary<int, List<Action>> _subSettingsUnwireMap = new Dictionary<int, List<Action>>();

        // 課題④: ProfileActions 実装
        public List<string>[] ProfileActions
        {
            get => SafeConfig?.profileActions ?? Global.ProfileActions;
            set
            {
                if (SafeConfig != null)
                {
                    SafeConfig.profileActions = value;
                }
                if (value != null && Global.ProfileActions != null)
                {
                    for (int i = 0; i < Math.Min(value.Length, Global.ProfileActions.Length); i++)
                    {
                        Global.ProfileActions[i] = value[i];
                    }
                }
                OnProfileSettingChanged(-1, nameof(ProfileActions), null, value);
            }
        }

        // 課題①: 指定スロットのサブ設定購読を全解除
        public void UnwireSubSettingsEvents(int deviceIndex)
        {
            if (deviceIndex < 0) return;

            lock (_subSettingsLock)
            {
                if (_subSettingsUnwireMap.TryGetValue(deviceIndex, out var unwireList))
                {
                    foreach (var unwire in unwireList)
                    {
                        try { unwire?.Invoke(); } catch { }
                    }
                    unwireList.Clear();
                    _subSettingsUnwireMap.Remove(deviceIndex);
                }
            }
        }

        // 課題① ＆ 課題②: 冒頭で Unwire を呼び出し、DeltaAccel を含めて再登録
        public void WireSubSettingsEvents(int deviceIndex)
        {
            if (deviceIndex < 0) return;

            lock (_subSettingsLock)
            {
                // 冒頭で既存購読を全解除（冪等性を保証）
                UnwireSubSettingsEvents(deviceIndex);

                var unwireList = new List<Action>();

                // スティックデッドゾーン (LS / RS)
                if (LSModInfo != null && deviceIndex < LSModInfo.Length && LSModInfo[deviceIndex] != null)
                {
                    var ls = LSModInfo[deviceIndex];
                    if (ls.xAxisDeadInfo != null)
                    {
                        Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "LS_X_" + prop, null, null);
                        ls.xAxisDeadInfo.OnSubPropertyChanged += h;
                        unwireList.Add(() => ls.xAxisDeadInfo.OnSubPropertyChanged -= h);
                    }
                    if (ls.yAxisDeadInfo != null)
                    {
                        Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "LS_Y_" + prop, null, null);
                        ls.yAxisDeadInfo.OnSubPropertyChanged += h;
                        unwireList.Add(() => ls.yAxisDeadInfo.OnSubPropertyChanged -= h);
                    }
                }

                if (RSModInfo != null && deviceIndex < RSModInfo.Length && RSModInfo[deviceIndex] != null)
                {
                    var rs = RSModInfo[deviceIndex];
                    if (rs.xAxisDeadInfo != null)
                    {
                        Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "RS_X_" + prop, null, null);
                        rs.xAxisDeadInfo.OnSubPropertyChanged += h;
                        unwireList.Add(() => rs.xAxisDeadInfo.OnSubPropertyChanged -= h);
                    }
                    if (rs.yAxisDeadInfo != null)
                    {
                        Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "RS_Y_" + prop, null, null);
                        rs.yAxisDeadInfo.OnSubPropertyChanged += h;
                        unwireList.Add(() => rs.yAxisDeadInfo.OnSubPropertyChanged -= h);
                    }
                }

                // トリガーデッドゾーン (L2 / R2)
                if (L2ModInfo != null && deviceIndex < L2ModInfo.Length && L2ModInfo[deviceIndex] != null)
                {
                    var l2 = L2ModInfo[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "L2_" + prop, null, null);
                    l2.OnSubPropertyChanged += h;
                    unwireList.Add(() => l2.OnSubPropertyChanged -= h);
                }

                if (R2ModInfo != null && deviceIndex < R2ModInfo.Length && R2ModInfo[deviceIndex] != null)
                {
                    var r2 = R2ModInfo[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "R2_" + prop, null, null);
                    r2.OnSubPropertyChanged += h;
                    unwireList.Add(() => r2.OnSubPropertyChanged -= h);
                }

                // ジャイロ制御
                if (GyroControlsInf != null && deviceIndex < GyroControlsInf.Length && GyroControlsInf[deviceIndex] != null)
                {
                    var gyro = GyroControlsInf[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "GyroControls_" + prop, null, null);
                    gyro.OnSubPropertyChanged += h;
                    unwireList.Add(() => gyro.OnSubPropertyChanged -= h);
                }

                // タッチパッド絶対座標
                if (TouchAbsMouse != null && deviceIndex < TouchAbsMouse.Length && TouchAbsMouse[deviceIndex] != null)
                {
                    var touchAbs = TouchAbsMouse[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "TouchAbs_" + prop, null, null);
                    touchAbs.OnSubPropertyChanged += h;
                    unwireList.Add(() => touchAbs.OnSubPropertyChanged -= h);
                }

                // スティック出力設定 ＆ 課題② DeltaAccelSettings の配線
                if (LSOutputSettings != null && deviceIndex < LSOutputSettings.Length && LSOutputSettings[deviceIndex] != null)
                {
                    var lsOut = LSOutputSettings[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "LSOutput_" + prop, null, null);
                    lsOut.OnSubPropertyChanged += h;
                    unwireList.Add(() => lsOut.OnSubPropertyChanged -= h);

                    // 左スティック DeltaAccelSettings
                    if (lsOut.controlSettings?.deltaAccelSettings != null)
                    {
                        var delta = lsOut.controlSettings.deltaAccelSettings;
                        Action<string> hDelta = prop => OnProfileSettingChanged(deviceIndex, "LS_DeltaAccel_" + prop, null, null);
                        delta.OnSubPropertyChanged += hDelta;
                        unwireList.Add(() => delta.OnSubPropertyChanged -= hDelta);
                    }
                }

                if (RSOutputSettings != null && deviceIndex < RSOutputSettings.Length && RSOutputSettings[deviceIndex] != null)
                {
                    var rsOut = RSOutputSettings[deviceIndex];
                    Action<string> h = prop => OnProfileSettingChanged(deviceIndex, "RSOutput_" + prop, null, null);
                    rsOut.OnSubPropertyChanged += h;
                    unwireList.Add(() => rsOut.OnSubPropertyChanged -= h);

                    // 右スティック DeltaAccelSettings
                    if (rsOut.controlSettings?.deltaAccelSettings != null)
                    {
                        var delta = rsOut.controlSettings.deltaAccelSettings;
                        Action<string> hDelta = prop => OnProfileSettingChanged(deviceIndex, "RS_DeltaAccel_" + prop, null, null);
                        delta.OnSubPropertyChanged += hDelta;
                        unwireList.Add(() => delta.OnSubPropertyChanged -= hDelta);
                    }
                }

                _subSettingsUnwireMap[deviceIndex] = unwireList;
            }
        }
    }
}
```

---

## 4. 実装手順

### Step 1: `IProfileSettingsService` および `ProfileSettingsService` の改修
1. 手元で元のコードを復元する（`git checkout HEAD -- DS4Windows/DI/IProfileSettingsService.cs DS4Windows/DS4Control/Services/ProfileSettingsService.cs`）。
2. `IProfileSettingsService.cs` に以下を追加：
   - `List<string>[] ProfileActions { get; set; }`
   - `void UnwireSubSettingsEvents(int deviceIndex);`
3. `ProfileSettingsService.cs` に以下を追加・改修：
   - `ProfileActions` プロパティ（SafeConfig と Global.ProfileActions の安全な要素同期）
   - `_subSettingsUnwireMap` フィールドの追加
   - `UnwireSubSettingsEvents(int deviceIndex)` メソッドの実装
   - `WireSubSettingsEvents(int deviceIndex)` の冒頭で `UnwireSubSettingsEvents(deviceIndex)` を実行
   - `WireSubSettingsEvents` 内に `LSOutputSettings[deviceIndex].controlSettings.deltaAccelSettings` および `RSOutputSettings[deviceIndex].controlSettings.deltaAccelSettings` の購読・解除コードを追加

### Step 2: `ProfileEditor.xaml.cs` の改修
1. `ProfileEditor.xaml.cs` を確認。
2. `Reload`、`ExecuteSaveOrApply`、`SpecialActionCheckBox_Click` 内の `Global.ProfileActions[deviceNum]` を、注入済みの `profileSettings.ProfileActions[deviceNum]` へ差し替え。

### Step 3: 単体テストの拡充（`ProfileSettingsServiceTests.cs`）
1. **冪等性テスト**:
   `WireSubSettingsEvents(0)` を 2 回連続で呼び出した後、サブ設定（例: `LSModInfo[0].xAxisDeadInfo.DeadZone`）の値を変更し、`ProfileSettingChanged` の発火回数が **1 回のみ** であることを検証（多重発火しないこと）。
2. **解除テスト**:
   `UnwireSubSettingsEvents(0)` を呼び出した後、値を変更してもイベントが発火しない（発火回数 0）ことを検証。
3. **DeltaAccel バブリングテスト**:
   `LSOutputSettings[0].controlSettings.deltaAccelSettings.Multiplier` を変更し、`"LS_DeltaAccel_Multiplier"` イベントが正常に発火することを検証。
4. **ProfileActions インターフェーステスト**:
   `profileSettings.ProfileActions` の読み書きが正常に機能することを検証。

### Step 4: ビルドおよび全自動テストの実行
1. `dotnet build DS4WindowsWPF.sln` が 0 エラー 0 警告で通ることを確認。
2. `dotnet test` により全単体テスト（既存テストおよび新規追加テスト）がパスすることを確認。

---

## 5. 検証手順・チェックリスト

| 項目 | 検証内容 | 期待結果 |
| :--- | :--- | :--- |
| **C-1** | `dotnet build DS4WindowsWPF.sln` | 0 エラーで正常ビルドされること |
| **C-2** | `WireSubSettingsEvents` を多重実行後のサブプロパティ変更 | イベントハンドラが累積せず、1 回のみ発火すること |
| **C-3** | `UnwireSubSettingsEvents` 実行後のサブプロパティ変更 | イベントが発火しないこと |
| **C-4** | `DeltaAccelSettings` のプロパティ変更 | `LS_DeltaAccel_*` / `RS_DeltaAccel_*` が正しくバブリングされること |
| **C-5** | `ProfileEditor.xaml.cs` 内の検索 | `Global.ProfileActions` への参照が 0 件であること |
| **C-6** | プロファイル保存・適用 | `profileSettings.ProfileActions` を通じてアクション設定が正しく保持されること |
| **C-7** | `dotnet test` の実行 | 全単体テストがグリーン（成功）であること |