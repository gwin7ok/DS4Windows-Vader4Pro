# Phase 5 Step 14 フォローアップ実行計画書
サブ設定イベント購読の冪等化・DeltaAccel配線・ProfileActionsインターフェース統一
(SubSettings Event Idempotence, DeltaAccel Wiring, and ProfileActions Interface Unification Plan)

## 1. 目的と背景

Phase 5 Step 14 において、プロファイル設定の全9カテゴリに対する双方向同期およびサブ設定バブリング通知基盤を構築し、単体テスト全 178 件をパスした。
本計画は、実機長期稼働および Phase 6（完全DI化・静的解体）に向けた品質強化として、以下の 3 つの課題を Phase 5 の仕上げとして確実に対処することを目的とする。

1. **課題①（イベント購読の冪等化）**: プロファイル切替を繰り返した際のイベント多重登録・メモリリークを完全に防止する（全解除 → 再登録サイクルの確立）。
2. **課題②（DeltaAccel配線）**: クラス実装が完了している `DeltaAccelSettings` のバブリング通知を `ProfileSettingsService` へ正式接続し、全9カテゴリのパイプラインを 100% 完結させる。
3. **課題④（ProfileActionsインターフェース化）**: `ProfileEditor.xaml.cs` 内に残る静的 `Global.ProfileActions` 直接アクセスを `IProfileSettingsService` 経由へ統一し、UI層の疎結合化を徹底する。

---

## 2. 各課題の詳細設計と解決方針

### 2.1 課題①: サブ設定イベント購読の冪等化（全解除してから再登録）

#### 現状の課題
- `ProfileSettingsService.WireSubSettingsEvents(deviceIndex)` では、`lsMod.xAxisDeadInfo.OnSubPropertyChanged += (prop) => ...` のように無名ラムダ式でイベントを追加している。
- C# では無名ラムダ式を `-=` で解除できないため、プロファイル読込（`LoadProfile`）のたびにイベントハンドラが累積し、1 回の値変更で `ProfileSettingChanged` が多重発火するリスクがある。

#### 解決策: デリゲート保持による「全解除 → 再登録」パイプライン
1. `ProfileSettingsService` 内に、スロット（0〜7）ごとに各サブ設定用の **名前付きイベントハンドラ（デリゲート参照）** を配列または辞書として保持する。
2. `UnwireSubSettingsEvents(int deviceIndex)` メソッドを新設し、指定スロット（または全スロット）の既存購読を安全にすべて `-=` で解除する。
3. `WireSubSettingsEvents(int deviceIndex)` の冒頭で、必ず `UnwireSubSettingsEvents(deviceIndex)` を実行してから新規登録を行う。
4. これにより、何回 `LoadProfile` や `WireSubSettingsEvents` が呼ばれても、**常に登録数は 1 つ（冪等性 100%）** が保証され、メモリリークや多重発火が原理的に発生しなくなる。

---

### 2.2 課題②: `DeltaAccelSettings` のサービス層バブリング配線

#### 現状の課題
- `DeltaAccelSettings.cs` 自体は `ProfileSubSettingBase` を継承し変更通知機能を持つが、`ProfileSettingsService.WireSubSettingsEvents` の監視ループ内に配線コードが含まれていない。

#### 解決策
- `WireSubSettingsEvents` 内で、各スロットの `LSOutputSettings` および `RSOutputSettings` から `outputSettings.controlSettings.deltaAccelSettings` を取得し、その `OnSubPropertyChanged` を購読対象に追加する。
- 同様に `UnwireSubSettingsEvents` での購読解除対象にも追加する。

---

### 2.3 課題④: `ProfileEditor.xaml.cs` における静的 `Global.ProfileActions` のインターフェース化

#### 現状の課題
- `ProfileEditor.xaml.cs` の保存時（`ExecuteSaveOrApply`）およびチェックボックス変更時（`SpecialActionCheckBox_Click`）に、`Global.ProfileActions[deviceNum] = ...` と静的配列を直接更新している。
- プロファイル読込時（`Reload`）にも `Global.ProfileActions` を直接参照している。

#### 解決策
1. `IProfileSettingsService` に `List<string>[] ProfileActions { get; set; }` プロパティを追加する（実体は `BackingStore` / `SafeConfig.profileActions` に委譲）。
2. `ProfileEditor.xaml.cs` 内の `Global.ProfileActions` への直接参照・代入を、すべて既にコンストラクタで注入されている `profileSettings.ProfileActions[deviceNum]`（または `profileSettings` 経由）に差し替える。
3. これにより、UI 層から静的 `Global` への直接代入依存を排除し、完全なサービス経由アクセスへ統一する。

---

## 3. 変更対象ファイル一覧

| ファイルパス | 変更区分 | 主な変更内容 |
|---|---|---|
| `DS4Windows/DI/IProfileSettingsService.cs` | 改修 | `UnwireSubSettingsEvents` メソッド追加、`ProfileActions` プロパティ追加 |
| `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | 改修 | デリゲート保持、`UnwireSubSettingsEvents` 実装、`WireSubSettingsEvents` の冪等化（事前解除）、`DeltaAccelSettings` 購読追加、`ProfileActions` 実装 |
| `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | 改修 | `Global.ProfileActions` の参照・代入を `profileSettings.ProfileActions` へ差し替え |
| `DS4WindowsTests/ProfileSettingsServiceSubSettingsTests.cs` | 拡充 | `WireSubSettingsEvents` を連続呼出ししても多重発火しないこと（冪等性）の検証テスト追加、`DeltaAccel` 連動テスト追加 |

---

## 4. 段階的実装手順（マイクロステップ）

### Step 1: `IProfileSettingsService` および `ProfileSettingsService` の改修
- `ProfileActions` プロパティの追加（`SafeConfig?.profileActions` 委譲）。
- スロットごとのハンドラキャッシュ保持フィールドの追加。
- `UnwireSubSettingsEvents(int deviceIndex)` の実装。
- `WireSubSettingsEvents(int deviceIndex)` の冒頭で `Unwire` を呼ぶように改修し、`DeltaAccelSettings` の購読を追加。

### Step 2: `ProfileEditor.xaml.cs` の静的参照差し替え
- `Reload`、`ExecuteSaveOrApply`、`SpecialActionCheckBox_Click` における `Global.ProfileActions` を `profileSettings.ProfileActions` に置換。

### Step 3: 単体テストの拡充と検証
- `ProfileSettingsServiceSubSettingsTests.cs` に以下のテストを追加：
  1. `WireSubSettingsEvents_CalledMultipleTimes_ShouldOnlyFireOnce`（多重登録防止テスト）
  2. `SubSettingChange_DeltaAccel_ShouldFireProfileSettingChanged`（DeltaAccel 配線テスト）
- `dotnet build` および `dotnet test` で全テスト（178件以上）のパスを確認。

---

## 5. 検証手順と完了基準

### 5.1 完了基準チェックリスト
- [ ] `WireSubSettingsEvents` を 10 回連続で呼び出しても、サブ設定変更時の `ProfileSettingChanged` 発火回数が厳密に 1 回であること。
- [ ] `DeltaAccelSettings` のプロパティ（`Multiplier` 等）変更時に `ProfileSettingChanged` が発火すること。
- [ ] `ProfileEditor` 内で `Global.ProfileActions` への直接アクセスが 0 箇所になっていること。
- [ ] 単体テストがすべて成功すること（全テスト PASS）。
- [ ] 実機においてプロファイル切替や設定保存が正常に動作すること。