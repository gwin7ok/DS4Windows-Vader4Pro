# フェーズ5-Step12 計画書: 出力スロット層（OutputSlot）の整理

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step12, §5.5（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `docs-forDIMG/MadeByAgent/Phase5-Step10-Plan.md`（PathService・オンデマンド評価）
- `docs-forDIMG/MadeByAgent/Phase5-Step11-Plan.md`（デバイス検出・列挙の分離）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `ControlService.outputSlotManager` などの既存フィールド参照は、移行期間中も互換性プロパティとして温存する。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 仮想 Xbox 360 / DualShock 4 のプラグイン・アンプラグ、スロット永続化（`OutputSlots.xml`）、遅延キューイング処理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 仮想デバイスの接続・切断時の既存ログ出力を維持し、`[DI]` 標準化ログを出力する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `IOutputSlotService` の契約を拡充し、スロット永続化を担う `IOutputSlotStore` を新設して Singleton 登録する。
- **§3.2 巨大ファイルの編集方針**:
  - `ControlService.cs`（8,000行超）および `OutputSlotManagerControl.xaml.cs` はピンポイント置換に留める。
- **§5.5 ネイティブドライバ保護（最重要ガードレール）**:
  - Windows カーネルドライバ（`ViGEmBus.sys`）との非同期 PnP 通信やアンマネージドハンドル（`ViGEmClient`）の破棄順序を崩さず、薄いアダプター境界に留める。

---

## 0. Step12の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#20（`IOutputSlotService` → `OutputSlotService`）、`OutputSlotManager.cs`、`OutputSlotPersist.cs`、および `OutputSlotManagerControl.xaml.cs` を対象とする。

1. **実体への直接癒着**:
   `IOutputSlotService` は導入されたが、バックエンド実体である `OutputSlotManager` は `Program.rootHub.outputSlotManager` として抱え込まれており、サービス側からの参照が静的シングルトンに依存している。
2. **UI からの直接静的アクセス**:
   `OutputSlotManagerControl.xaml.cs`（仮想スロット管理UI）が、一部で `Program.rootHub.outputSlotManager` を直接叩いてスロットの抜き差しを行っている。
3. **永続化の静的ファイル I/O**:
   仮想コントローラーの永続スロット割り当てを読み書きする `OutputSlotPersist.cs` が静的メソッドで直接ファイル I/O を行っており、単体テスト時にディスク上の `OutputSlots.xml` が汚染される。
4. **カーネルドライバ（ViGEm）の物理的制約**:
   仮想ゲームパッドのプラグイン／アンプラグは Windows OS の PnP（プラグ＆プレイ）サブシステムと非同期通信するため、数ミリ秒〜数百ミリ秒のハードウェア遅延を伴う。このキューイング構造を不用意に解体してはならない。

### 0.2 全体4層モデルにおける位置づけ
本Stepは **第4層 4-c デバイス・ハードウェアサービス層** の締めくくりであり、ドメイン3（デバイス・インフラ系: Step 10〜12）を完全完結させる。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（薄いアダプター境界パターン: ViGEm 物理通信の完全温存）** および **論点2：案1（`IOutputSlotStore` の新設による永続化 I/O の抽象化）** を採用する。

### 1.1 `IOutputSlotService` インターフェースの契約拡充（第4層 4-c）
UI および `ControlService` が必要とするスロット操作（状態取得、プラグイン要求、アンプラグ要求、スロット変更イベント）を網羅する契約を確立する。`DS4Windows/DI/IOutputSlotService.cs` を更新する。

```csharp
namespace DS4Windows.DI
{
    public interface IOutputSlotService
    {
        // 登録されている出力デバイス一覧
        IReadOnlyList<OutSlotDevice> OutputSlots { get; }

        // プラグイン・アンプラグ操作要求（非同期キューへ投入）
        void PluginSlot(int slotNumber, OutputDeviceType devType);
        void UnplugSlot(int slotNumber);

        // スロット設定の永続化
        bool LoadOutputSlots();
        bool SaveOutputSlots();

        // スロット状態変更通知イベント
        event EventHandler<SlotChangedEventArgs> SlotChanged;
    }
}
```

---

### 1.2 薄いアダプター境界による ViGEm ネイティブドライバ保護（ガードレール §5.5）
`OutputSlotManager.cs` の内部実装（ViGEm クライアントへのリクエスト直列化キュー、PnP 非同期完了待機、アンマネージドハンドルの破棄順序）には**一切手を触れず、物理通信の挙動を完全に温存**する。
`OutputSlotService` は `OutputSlotManager` の薄いラッパーとして機能し、上位から `Program.rootHub` への参照を完全に遮断する。

---

### 1.3 `IOutputSlotStore` によるスロット永続化（`OutputSlots.xml`）の抽象化
`OutputSlotPersist.cs` の静的ファイル I/O を分離するため、`IOutputSlotStore` を新設する。
`OutputSlotService` に `IOutputSlotStore` および `IPathService`（Step 10）を注入し、ファイルパスのオンデマンド解決とテスト容易性を確保する。

```csharp
namespace DS4Windows.DI
{
    public interface IOutputSlotStore
    {
        bool Load(string filePath, OutputSlotManager slotManager);
        bool Save(string filePath, OutputSlotManager slotManager);
    }
}
```

```csharp
// DS4Windows/DS4Control/Services/OutputSlotStore.cs 実装イメージ
public class OutputSlotStore : IOutputSlotStore
{
    public bool Load(string filePath, OutputSlotManager slotManager)
    {
        return OutputSlotPersist.Load(filePath, slotManager);
    }

    public bool Save(string filePath, OutputSlotManager slotManager)
    {
        return OutputSlotPersist.Save(filePath, slotManager);
    }
}
```

---

### 1.4 `OutputSlotService` のリファクタリングと `Program.rootHub` 排除
`OutputSlotService` のコンストラクタで `IOutputSlotStore`、`IPathService`、および `OutputSlotManager`（または内部生成）を受け取り、静的シングルトン `Program.rootHub` への依存を 0 件にする。

```csharp
// OutputSlotService.cs 実装イメージ
public class OutputSlotService : IOutputSlotService
{
    private readonly OutputSlotManager _slotManager;
    private readonly IOutputSlotStore _store;
    private readonly IPathService _pathService;

    public OutputSlotService(
        IOutputSlotStore store,
        IPathService pathService,
        OutputSlotManager slotManager = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _slotManager = slotManager ?? Program.rootHub?.outputSlotManager ?? new OutputSlotManager();
    }
...
}
```

---

### 1.5 UI（`OutputSlotManagerControl.xaml.cs`）のサービス経由化
UI 側で直接呼ばれていた `Program.rootHub.outputSlotManager` を、注入された `IOutputSlotService` 呼び出しに置き換える。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース拡張 | `DS4Windows/DI/IOutputSlotService.cs` | プラグイン／アンプラグ、変更イベント等の契約拡充 |
| ストアインターフェース | `DS4Windows/DI/IOutputSlotStore.cs` | `OutputSlots.xml` の永続化契約新設 |
| ストア実装 | `DS4Windows/DS4Control/Services/OutputSlotStore.cs` | 永続化 I/O の薄いラッパー実装 |
| サービス改修 | `DS4Windows/DS4Control/Services/OutputSlotService.cs` | ストア注入、パスオンデマンド評価、rootHub排除 |
| UI 改修 | `DS4Windows/DS4Forms/OutputSlotManagerControl.xaml.cs` | `IOutputSlotService` 経由へのピンポイント置換 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IOutputSlotStore` の Singleton 登録追加 |
| 単体テスト新設 | `DS4WindowsTests/OutputSlotServiceTests.cs` | `Mock<IOutputSlotStore>` によるスロット変更・保存テスト新設 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step12-1: `OutputSlotManager` および UI 呼び出し元の精査
1. `OutputSlotManagerControl.xaml.cs` 等における `Program.rootHub.outputSlotManager` 参照箇所を全件特定する。

### タスク Step12-2: `IOutputSlotStore` & `OutputSlotStore` の新設
1. `DS4Windows/DI/IOutputSlotStore.cs` を新規作成。
2. `DS4Windows/DS4Control/Services/OutputSlotStore.cs` を新規作成。

### タスク Step12-3: `IOutputSlotService` の契約拡張
1. `DS4Windows/DI/IOutputSlotService.cs` にプラグイン・アンプラグ・変更イベントを追加。

### タスク Step12-4: `OutputSlotService` のリファクタリング
1. `OutputSlotService.cs` に `IOutputSlotStore` と `IPathService` を注入。
2. `Program.rootHub.outputSlotManager` 依存を解消し、アダプター境界を確立（§1.2 ガードレール準拠）。

### タスク Step12-5: UI（`OutputSlotManagerControl.xaml.cs`）の改修
1. UI の直接参照を `IOutputSlotService` 呼び出しにピンポイント置換。

### タスク Step12-6: DI コンテナ登録追加
1. `DS4Windows/DI/ServiceRegistration.cs` に `IOutputSlotStore` を追加登録。

### タスク Step12-7: 単体テスト作成と自動テスト実行
1. `OutputSlotServiceTests.cs` を新設し、モックを使ったプラグイン・永続化の自動テストを作成。
2. `dotnet test` で全テストパスを確認。

### タスク Step12-8: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step12 を「計画書承認済」に更新。
3. `Phase5-Step12-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **ViGEm ドライバハング・BSoD** | 高 | `OutputSlotManager` の PnP キューおよび `ViGEmClient` 破棄順序を完全温存し、アダプター境界に限定する（§1.2）。 |
| **PnP 非同期遅延による競合** | 中 | 既存のスロット状態（プラグ待ち、アンプラグ待ち）追跡機構を崩さない。 |
| **テスト時の実ファイル汚染** | 低 | `IOutputSlotStore` をモック化し、単体テスト時のファイル出力をトラップする（§1.3）。 |

---

## 5. 完了判定基準

- [ ] `IOutputSlotStore` が新設され、スロット永続化が抽象化されていること。
- [ ] `OutputSlotService.cs` 内から `Program.rootHub` への参照が 0 件になっていること。
- [ ] `OutputSlotManagerControl.xaml.cs` が `IOutputSlotService` 経由で動作していること。
- [ ] ViGEm ドライバ通信の非同期キューイングと破棄順序が温存されていること（§1.2）。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
