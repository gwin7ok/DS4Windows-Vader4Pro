# フェーズ4-Step4 計画書: 入力・出力・デバイス状態サービス

作成日: 2026-08-31
最終更新日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §1.2, §2, §3 Step4（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step3-Completion-Report.md`（Step3完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step3-RealDevice-Verification-Checklist.md`（実機CP1全件合格）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`（Global棚卸し一覧）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` のデバイス・出力関連メンバー（`devices`, `activeControllers`, `OutSlotDevices` 等）は削除せず、新設する各サービスへの薄いデリゲートシムとして残す。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - コントローラー配列長（`MAX_DS4_CONTROLLER_COUNT` = 8, `TEST_PROFILE_ITEM_COUNT` = 9）、仮想コントローラー種別（Xbox 360 / DualShock 4）、ViGEm 出力ルーティング、スロット割り当て状態の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェース契約は `DS4Windows/DI/`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs` / `ControlService.cs` は全体を再生成せず、対象メンバーのみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step4の位置づけと現状分析

### 0.1 Step0〜Step3の成果とStep4のスコープ
- **Step1〜Step3 で完了したこと**:
  - 第4層 4-c（設定／状態サービス）のうち、設定（`IProfileSettingsService`）、プロファイル永続化（`IProfileRepository`）、SpecialAction永続化（`ISpecialActionRepository`）が DI 化され、実機検証（Checkpoint 1）で全 12 項目が合格。
- **Step4 で行うこと**:
  - `Global`（`ScpUtil.cs`）に集中している「デバイス・コントローラー状態（29件）」および「出力・仮想コントローラー状態（47件）」を DI サービスとして分離・構築する。
  - **第1層（入力監視層）** の物理デバイス状態および **第3層（信号出力層 3-a. 仮想コントローラー出力）** の出力スロット状態を、**第4層 4-c（設定／状態サービス）** へ安全に提供する DI サービス群（`IDeviceStateService`, `IOutputSlotService`）を確立する。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、各層の正確な定義および本Step（Step 4）の対象範囲を以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す。
   - **【★本Step関連】**: 物理コントローラーの接続・切断検知、バッテリー残量、通信種別（BT/USB）のデバイス状態を `IDeviceStateService` を通じて第4層 4-c へ公開する。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（副作用の実行は行わない）。
   - **2-a. 基本マッピング決定**: 1入力→1出力（コントローラー信号／KBM信号）の対応表引き。
   - **2-b. SpecialActionトリガー判定**: 複数入力の組み合わせで成立/解除を判定し、元入力の出力を抑制するか決定。
   - **2-c. アクション選択・パラメータ決定**: 成立したSpecialActionが「マクロ／プロファイル切替／プロセス起動／KBM出力」のどれかを判定し、実行に必要なパラメータを確定。
   - **2-d. マクロの分解**: トリガーされたマクロを時系列のKBM出力信号列に分解。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する。
   - **3-a. 仮想コントローラー出力 【★本Step対象】**: 2-aの結果をDS4/Xbox360規格で実出力（`outputDevices[ind]`）。本Stepで仮想コントローラー出力スロット管理（`IOutputSlotService`）を DI 化して第4層 4-c へ公開する。
   - **3-b. KBM出力**: 2-aの結果、および2-dで分解されたマクロの信号列を実際に時系列で送出（`outputKBMHandler` / `IVirtualKBM`）。
   - **3-c. アプリ内アクション実行**: 2-cで決定されたプロファイル切替・プロセス起動を実際に実行。
4. **第4層: UI層（制御面）**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View**: WPF の画面・UserControl。
   - **4-b. ViewModel**: 画面状態、入力値検証、画面イベントの調整。
   - **4-c. 設定／状態サービス 【★本Step対象】**: プロファイル（`IProfileSettingsService`, `IProfileRepository`: 完了）、SpecialAction（`ISpecialActionRepository`: 完了）、デバイス状態（`IDeviceStateService`: 本Step）、出力スロット状態（`IOutputSlotService`: 本Step）を DI 管理。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IDeviceStateService` インターフェース設計 (第1層デバイス状態 → 第4層 4-c サービス)
契約インターフェースは `DS4Windows/DI/IDeviceStateService.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IDeviceStateService
    {
        DS4Device[] Devices { get; }
        DS4Device GetDevice(int slotIndex);
        bool IsDeviceConnected(int slotIndex);
        int ConnectedControllersCount { get; }
        
        string GetDeviceMacAddress(int slotIndex);
        ConnectionType GetConnectionType(int slotIndex);
        int GetBatteryLevel(int slotIndex);
        
        event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
    }
}
```

### 1.2 `IOutputSlotService` インターフェース設計 (第3層 3-a 出力スロット → 第4層 4-c サービス)
契約インターフェースは `DS4Windows/DI/IOutputSlotService.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IOutputSlotService
    {
        OutSlotDevice[] OutSlotDevices { get; }
        OutSlotDevice GetOutSlotDevice(int slotIndex);
        bool IsSlotPlugin(int slotIndex);
        
        OutputDeviceType GetOutputDeviceType(int slotIndex);
        void SetOutputDeviceType(int slotIndex, OutputDeviceType deviceType);
        
        event EventHandler<OutputSlotChangedEventArgs> OutputSlotChanged;
    }
}
```

### 1.3 `Global` (in `ScpUtil.cs`) シム設計
`Global.devices`, `Global.activeControllers`, `Global.OutSlotDevices` 等の静的メンバーは、新設サービスへの薄いシムとする。

```csharp
private static DS4Windows.DI.IDeviceStateService deviceStateService = null;
private static readonly DS4Windows.DI.IDeviceStateService fallbackDeviceStateService = new DeviceStateService();

public static DS4Windows.DI.IDeviceStateService DeviceStateServiceInstance
{
    get
    {
        if (deviceStateService != null) return deviceStateService;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.IDeviceStateService>();
            if (service != null)
            {
                deviceStateService = service;
                return deviceStateService;
            }
        }
        catch { }
        return fallbackDeviceStateService;
    }
    set => deviceStateService = value;
}

private static DS4Windows.DI.IOutputSlotService outputSlotService = null;
private static readonly DS4Windows.DI.IOutputSlotService fallbackOutputSlotService = new OutputSlotService();

public static DS4Windows.DI.IOutputSlotService OutputSlotServiceInstance
{
    get
    {
        if (outputSlotService != null) return outputSlotService;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.IOutputSlotService>();
            if (service != null)
            {
                outputSlotService = service;
                return outputSlotService;
            }
        }
        catch { }
        return fallbackOutputSlotService;
    }
    set => outputSlotService = value;
}
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IDeviceStateService.cs` | 新規 | **DI永続資産** | 第1層デバイス状態を第4層へ公開する契約インターフェース |
| `DS4Windows/DI/IOutputSlotService.cs` | 新規 | **DI永続資産** | 第3層 3-a 仮想出力スロット管理の契約インターフェース |
| `DS4Windows/DS4Control/Services/DeviceStateService.cs` | 新規 | **DI永続資産** | `IDeviceStateService` の本番実装クラス |
| `DS4Windows/DS4Control/Services/OutputSlotService.cs` | 新規 | **DI永続資産** | `IOutputSlotService` の本番実装クラス |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | `IDeviceStateService`, `IOutputSlotService` の Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` のデバイス・出力関連メンバーを新サービスへのシムへピンポイント置換 |
| `DS4WindowsTests/DeviceStateServiceTests.cs` | 新規 | **テスト資産** | デバイス状態アクセス・スロット境界の単体テスト |
| `DS4WindowsTests/OutputSlotServiceTests.cs` | 新規 | **テスト資産** | 出力スロット・仮想コントローラー管理の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step4-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step4-Completion-Report.md` | 新規 | ドキュメント | Step4完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step4進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step4-1: インターフェース定義（契約）
- `DS4Windows/DI/IDeviceStateService.cs`（第1層/第4層 4-c）および `DS4Windows/DI/IOutputSlotService.cs`（第3層 3-a/第4層 4-c）を新規作成（名前空間: `DS4Windows.DI`）。

### タスク Step4-2: 実装クラス作成（実体）
- `DS4Windows/DS4Control/Services/DeviceStateService.cs` および `OutputSlotService.cs` を新規作成（名前空間: `DS4Windows`）。
- スレッドセーフな配列管理、変更イベント通知を実装。

### タスク Step4-3: DI コンテナ登録更新
- `DS4Windows/DI/ServiceRegistration.cs` に `IDeviceStateService` および `IOutputSlotService` の Singleton 登録を追加。

### タスク Step4-4: `Global` (in `ScpUtil.cs`) ピンポイントシム化
- `ScpUtil.cs` の `Global.DeviceStateServiceInstance` および `Global.OutputSlotServiceInstance` シムを追加。

### タスク Step4-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/DeviceStateServiceTests.cs` および `OutputSlotServiceTests.cs` を作成・実行。
- 回帰テスト（`Actions.Tests` 31件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step4-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step4-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| コントローラー接続・切断時のレースコンディション | Step4-2 | `DS4Device` 参照取得・配列更新時に `lock` 排他制御を適用する。 |
| 配列インデックス範囲外（0〜7スロット） | Step4-2 | `MAX_DS4_CONTROLLER_COUNT` (=8) の境界チェックを内包する。 |
| 巨大ファイル `ScpUtil.cs` の編集によるコード欠損 | Step4-4 | ピンポイント置換を徹底し、対象ブロックのみを書き換える。 |

---

## 5. 完了判定基準

- [ ] `IDeviceStateService` および `IOutputSlotService` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `DeviceStateService` および `OutputSlotService` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に登録され、`AppHost` から解決できる。
- [ ] `Global` のデバイス・出力関連メンバーがシム化され、既存コードが無変更で動作する。
- [ ] 新設した単体テストおよび既存の回帰テストが全件成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step4-Completion-Report.md` が作成されている。
