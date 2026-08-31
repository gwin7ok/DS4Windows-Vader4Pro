# フェーズ4-Step4 計画書: 入力・出力・デバイス状態サービス

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §4.1, §5, §6.6（全体計画）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.2, §2, §3 Step4（Phase4詳細計画）
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
  - 設定・プロファイル・SpecialAction の 3 大データ中核基盤が DI 化され、実機検証（Checkpoint 1）で全 12 項目が合格。
- **Step4 で行うこと**:
  - `Global`（`ScpUtil.cs`）に集中している「デバイス・コントローラー状態（29件）」および「出力・仮想コントローラー状態（47件）」を DI サービスとして分離・構築する。
  - 第2層（ドメイン・デバイス状態: `IDeviceStateService`）および第3層（信号・出力スロット: `IOutputSlotService`）の境界を明確化する。

### 0.2 4層モデルにおける責務境界（全体計画書 §3）
- **第2層（ドメイン・デバイス層）: `IDeviceStateService`**:
  - 実機コントローラーの接続状態、接続台数、スロット別 `DS4Device` 参照取得、バッテリー状態、接続種別（BT/USB）の型安全アクセス。
- **第3層（信号・アクション実行層）: `IOutputSlotService` / `IVirtualKBM`**:
  - 仮想コントローラー（ViGEmBus: Xbox 360 / DS4）出力スロットの管理、プラグイン/プラグアウト状態の管理、仮想 KBM 送出管理。
- **第4層（UI/Application層）**:
  - UI ViewModel はこれらのサービスを通じてコントローラー状態や出力スロット状態を取得・表示する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IDeviceStateService` インターフェース設計 (第2層)
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

### 1.2 `IOutputSlotService` インターフェース設計 (第3層)
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
| `DS4Windows/DI/IDeviceStateService.cs` | 新規 | **DI永続資産** | デバイス状態アクセスの契約インターフェース |
| `DS4Windows/DI/IOutputSlotService.cs` | 新規 | **DI永続資産** | 出力スロット・仮想コントローラー管理の契約インターフェース |
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
- `DS4Windows/DI/IDeviceStateService.cs` および `DS4Windows/DI/IOutputSlotService.cs` を新規作成（名前空間: `DS4Windows.DI`）。

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

