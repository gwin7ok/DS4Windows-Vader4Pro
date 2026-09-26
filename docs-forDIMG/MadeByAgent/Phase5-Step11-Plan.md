# フェーズ5-Step11 計画書: デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step11（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `docs-forDIMG/MadeByAgent/Phase5-Step10-Plan.md`（残存サービス境界・IDeviceStateAccessor活用）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `DS4Devices.cs` の既存の静的メソッド群（`findControllers` 等）は即座に解体・削除せず、内部で `IDs4DeviceRegistry` へ委譲する薄い互換シムとする。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - USB / Bluetooth の HID デバイス列挙、VID/PID の照合ルール、コントローラー種別（DS4, DualSense, SwitchPro, JoyCon, Vader4Pro 等）の判定と初期化処理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - デバイス検出時・切断時のログ出力（`AppLogger.LogToGui` / `AppLogger.LogTrace`）を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `IDs4DeviceRegistry` の契約を完全化し、`Ds4DeviceRegistryAdapter` を Singleton として `DS4Windows/DI/ServiceRegistration.cs` に登録する。
- **§3.2 巨大ファイルの編集方針**:
  - `ControlService.cs`（8,000行超）および `DS4Devices.cs`（ハードウェア通信層）の編集は最小限のピンポイント置換に留める。

---

## 0. Step11の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#17（`IDs4DeviceRegistry` → `Ds4DeviceRegistryAdapter`）、および静的クラス `DS4Devices.cs` を対象とする。

1. **`Ds4DeviceRegistryAdapter` の単なる丸投げ委譲**:
   Phase 3〜4 で `IDs4DeviceRegistry` が導入されたが、実体 `Ds4DeviceRegistryAdapter.cs` は `DS4Devices.findControllers()`, `DS4Devices.getDS4Controllers()`, `DS4Devices.stopControllers()` 等の静的メソッドを呼んでいるだけの薄いパススルーに留まっている。
2. **`ControlService` からの直接静的アクセス残存**:
   バックエンドの中核である `ControlService.cs` は、DI インターフェース `IDs4DeviceRegistry` が存在するにもかかわらず、デバイスのスキャンや切断処理で依然として静的 `DS4Devices` を直接参照している箇所が存在する。
3. **ハードウェア層の物理的制約**:
   `DS4Devices.cs` は Windows の HidLibrary（低レイヤー HID API）と強く結合しており、VID/PID テーブルや排他制御などハードウェア固有のデリケートなロジックを多数保持している。この内部を不用意に破壊してはならない。

### 0.2 全体4層モデルにおける位置づけ
本Stepは **第4層 4-c ハードウェア・デバイスサービス層** に属する。Strangler Fig パターンを適用し、ハードウェア通信の実体（第1層・第2層）を安定に温存したまま、第4層サービスとしての境界を確立する。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（Strangler Fig パターン: `IDs4DeviceRegistry` 契約強化と段階的シム化）** および **論点2：案1（生デバイス検出・列挙責務に専念し、論理スロット管理と明確に分離）** を採用する。

### 1.1 `IDs4DeviceRegistry` インターフェースの契約強化（新規、第4層 4-c）
デバイス検出・列挙・停止に必要な操作と、接続変更を通知するイベントを契約として明確に定義する。`DS4Windows/DS4Control/Services/IDs4DeviceRegistry.cs` を更新する。

```csharp
namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        // 検出された生デバイスの列挙（スロット番号とは独立）
        IEnumerable<DS4Device> Devices { get; }

        // デバイス検出・再スキャンの実行
        void FindControllers();

        // 全デバイスの停止・クリーンアップ
        void StopControllers();

        // 接続・切断イベント
        event Action<DS4Device> DeviceAttached;
        event Action<DS4Device> DeviceRemoved;
    }
}
```

---

### 1.2 生デバイス列挙と論理スロット管理の明確な分離（関心の分離）
- **`IDs4DeviceRegistry` の責務**:
  OS から認識された物理コントローラー（`DS4Device`）の検出、切断検知、デバイスインスタンスのライフサイクル追跡に専念する。
- **`ControlService` の責務**:
  検出された `DS4Device` をどのスロット番号（0〜3番）に割り当てるか、どのプロファイルを紐付けるかという「論理スロット管理」は `ControlService` 側の責務として維持し、ハードウェアレジストリにはスロットの概念を持ち込まない。

---

### 1.3 `DS4Devices`（静的クラス）の薄いシム化（§2.1 互換維持）
`DS4Devices.cs` の内部にある HID 列挙ロジックや VID/PID テーブルはそのまま安定的に活用しつつ、外部に公開されている静的メソッド群を `IDs4DeviceRegistry` への委譲シムとして整理する。

```csharp
// DS4Devices.cs（シム化イメージ）
public static class DS4Devices
{
    private static IDs4DeviceRegistry Registry => AppHost.Services.GetService<IDs4DeviceRegistry>();

    public static void findControllers()
    {
        if (Registry != null)
            Registry.FindControllers();
        else
            findControllersInternal(); // DI未初期化時のフォールバック
    }
}
```

---

### 1.4 `ControlService.cs` の `IDs4DeviceRegistry` 経由への置換
`ControlService.cs` のコンストラクタ（またはプロパティ）で `IDs4DeviceRegistry` を受け取るようにし、ホットプラグスキャン時（`Hotplug`）やサービス停止時（`Stop`）の静的 `DS4Devices.findControllers()` / `DS4Devices.stopControllers()` 呼び出しを、注入された `_deviceRegistry` 経由にピンポイントで置き換える。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース拡張 | `DS4Windows/DS4Control/Services/IDs4DeviceRegistry.cs` | 接続イベント（`DeviceAttached`/`Removed`）等の契約追加 |
| アダプター改修 | `DS4Windows/DS4Control/Services/Ds4DeviceRegistryAdapter.cs` | 実装の完全化、イベント中継、ライフサイクル管理 |
| シム化 | `DS4Windows/DS4Library/DS4Devices.cs` | 静的メソッドのピンポイント委譲シム化（列挙ロジックは温存） |
| コントロール改修 | `DS4Windows/DS4Control/ControlService.cs` | `IDs4DeviceRegistry` 注入、静的 `DS4Devices` 参照の置換 |
| 単体テスト新設 | `DS4WindowsTests/Ds4DeviceRegistryTests.cs` | モックデバイスを用いた列挙・切断イベントの自動テスト新設 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step11-1: `DS4Devices.cs` の呼び出し元精査
1. ソリューション全体を grep し、静的 `DS4Devices` を直接参照している箇所（`ControlService.cs` 等）を全件洗い出す。

### タスク Step11-2: `IDs4DeviceRegistry` の契約拡張
1. `IDs4DeviceRegistry.cs` を更新し、イベントおよび操作メソッドを定義する。

### タスク Step11-3: `Ds4DeviceRegistryAdapter` のリファクタリング
1. `Ds4DeviceRegistryAdapter.cs` を改修し、拡張された契約を実装する。

### タスク Step11-4: `ControlService.cs` のピンポイント置換
1. `ControlService` のホットプラグ・スキャンおよび停止処理を、注入された `IDs4DeviceRegistry` 経由に変更する。

### タスク Step11-5: `DS4Devices.cs` の静的シム化
1. 既存の外部静的呼び出し元との互換性を保つため、`DS4Devices` のパブリックメソッドをシム化する（§2.1）。

### タスク Step11-6: 単体テスト作成と自動テスト実行
1. `Ds4DeviceRegistryTests.cs` を新設。
2. モックを用いてデバイス検出イベントおよび停止処理が正しく動作することを自動テストで検証。
3. `dotnet test` でリグレッションがないことを確認。

### タスク Step11-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step11 を「計画書承認済」に更新。
3. `Phase5-Step11-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **実機認識タイミングの破綻** | 高 | 低レイヤーの HidLibrary 列挙本体には手を触れず、薄い境界化のみを行う（§1.3）。 |
| **ControlService 編集による破壊** | 中 | 巨大ファイル `ControlService.cs` 全体を再生成せず、対象メソッドのみをピンポイントで置換する（§3.2）。 |
| **未移行コードのコンパイルエラー** | 低 | 静的 `DS4Devices` を薄いシムとして温存し、既存コードを破壊しない（§2.1）。 |

---

## 5. 完了判定基準

- [ ] `IDs4DeviceRegistry` の契約が拡張され、接続イベント等が定義されていること。
- [ ] `ControlService.cs` 内からの静的 `DS4Devices` への直接依存が `IDs4DeviceRegistry` 経由に置換されていること。
- [ ] 静的 `DS4Devices.cs` がシム化され、既存の呼び出し元互換が維持されていること。
- [ ] 実機コントローラーの検出・切断が従来通り正常に動作すること。
- [ ] 単体テストが成功し、ビルドエラー・警告増がないこと。
