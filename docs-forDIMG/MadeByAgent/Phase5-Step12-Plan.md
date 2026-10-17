# フェーズ5-Step12 計画書: 出力スロット層（OutputSlot）の整理と実配線化

作成日: 2026-09-03（改訂日: 2026-09-03・実体実配線への改定）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step12, §5.5（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-Addendum-Findings-Report.md`（追加監査・孤立死コードの特定）
- `docs-forDIMG/MadeByAgent/Phase5-Step10-Plan.md`（PathService・オンデマンド評価）
- `docs-forDIMG/MadeByAgent/Phase5-Step11-Plan.md`（デバイス検出・列挙の分離）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `ControlService.outputslotMan` などの既存フィールド参照は、移行期間中も互換性プロパティとして温存する。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 仮想 Xbox 360 / DualShock 4 のプラグイン・アンプラグ、スロット永続化（`OutputSlots.xml`）、遅延キューイング処理を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 仮想デバイスの接続・切断時の既存ログ出力を維持し、`[DI]` 標準化ログを出力する。
- **§3.1 DI (Dependency Injection) の実装**:
  - `IOutputSlotService` の契約を拡充し、スロット永続化を担う `IOutputSlotStore` を新設して Singleton 登録する。
  - **死コードの根絶**: 孤立したダミー配列を撤廃し、本物の `OutputSlotManager` 実体へ確実に接続（実配線）する。
- **§3.2 巨大ファイルの編集方針**:
  - `ControlService.cs`（8,000行超）および `OutputSlotManagerControl.xaml.cs` はピンポイント置換に留める。
- **§5.5 ネイティブドライバ保護（最重要ガードレール）**:
  - Windows カーネルドライバ（`ViGEmBus.sys`）との非同期 PnP 通信やアンマネージドハンドル（`ViGEmClient`）の破棄順序を崩さず、薄いアダプター境界に留める。

---

## 0. Step12の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-Addendum-Findings-Report.md`（発見2）に基づき、以下の重大な不整合を対象とする。

1. **【重大発見】`OutputSlotService` が実運用から切り離された孤立死コードである実態**:
   実コード `OutputSlotService.cs` を精査したところ、自前で `private readonly OutSlotDevice[] _outputDevices = new OutSlotDevice[4];` というインメモリ配列を抱えているだけで、**ViGEm ドライバを実際に操作している本物の `OutputSlotManager` と一切接続されていないダミー実装**であることが判明した。
2. **本物の実体（`ControlService.outputslotMan`）への直結残存**:
   アプリ全体で実際に仮想コントローラーを抜き差ししているのは `ControlService` が保持する生の `outputslotMan`（`OutputSlotManager` インスタンス）である。
   `MainWindow.xaml.cs` も `slotManControl.SetupDataContext(controlService: App.rootHub, App.rootHub.OutputslotMan)` として生のインスタンスを UI に直接渡しており、`IOutputSlotService` は完全に形骸化している。
3. **永続化の静的ファイル I/O**:
   仮想コントローラーの永続スロット割り当てを読み書きする `OutputSlotPersist.cs` が静的メソッドで直接ファイル I/O を行っており、単体テスト時にディスク上の `OutputSlots.xml` が汚染される。
4. **カーネルドライバ（ViGEm）の物理的制約（ガードレール §5.5）**:
   仮想ゲームパッドのプラグイン／アンプラグは Windows OS の PnP（プラグ＆プレイ）サブシステムと非同期通信するため、数ミリ秒〜数百ミリ秒のハードウェア遅延を伴う。このキューイング構造を不用意に解体してはならない。

### 0.2 全体4層モデルにおける位置づけ
本Stepは **第4層 4-c デバイス・ハードウェアサービス層** の締めくくりである。孤立した死コードを撤廃し、本物の実体へ「実配線」することで、ドメイン3（デバイス・インフラ系: Step 10〜12）を完全完結させる。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（薄いアダプター境界パターン: ViGEm 物理通信の完全温存）** および **論点2：案1（`IOutputSlotStore` の新設による永続化 I/O の抽象化）** を採用し、さらに **本物の実体への実配線** を断行する。

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

### 1.2 【最重要】孤立実装の撤廃と本物 `OutputSlotManager` への実配線
`OutputSlotService.cs` の内部にある独自のダミー配列（`_outputDevices`）を**完全に削除**する。
コンストラクタで本物の `OutputSlotManager` インスタンス（`ControlService` が保持する実体）を受け取り、すべての操作を実体に委譲する「本物のサービス」として再構築する。

```csharp
// OutputSlotService.cs 実装イメージ（実配線化）
public class OutputSlotService : IOutputSlotService
{
    private readonly OutputSlotManager _slotManager; // 本物の実体
    private readonly IOutputSlotStore _store;
    private readonly IPathService _pathService;

    public OutputSlotService(
        OutputSlotManager slotManager,
        IOutputSlotStore store,
        IPathService pathService)
    {
        _slotManager = slotManager ?? throw new ArgumentNullException(nameof(slotManager));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    }

    // ダミー配列ではなく、本物の実体のスロット一覧を返す！
    public IReadOnlyList<OutSlotDevice> OutputSlots => _slotManager.OutputSlots;

    public void PluginSlot(int slotNumber, OutputDeviceType devType)
    {
        _slotManager.DeferredPlugin(slotNumber, devType); // 実体のキューへ投入
    }

    public void UnplugSlot(int slotNumber)
    {
        _slotManager.DeferredUnplug(slotNumber); // 実体のキューへ投入
    }
    ...
}
```

---

### 1.3 薄いアダプター境界による ViGEm ネイティブドライバ保護（ガードレール §5.5）
`OutputSlotManager.cs` の内部実装（ViGEm クライアントへのリクエスト直列化キュー、PnP 非同期完了待機、アンマネージドハンドルの破棄順序）には**一切手を触れず、物理通信の挙動を完全に温存**する。
`OutputSlotService` は `OutputSlotManager` の薄いラッパーとして機能し、上位から生のインスタンスへの直接アクセスを完全に遮断する。

---

### 1.4 `IOutputSlotStore` によるスロット永続化（`OutputSlots.xml`）の抽象化
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

---

### 1.5 UI（`OutputSlotManagerControl.xaml.cs`）および `ControlService` のサービス経由化
UI 側および `ControlService` 内部で直接呼ばれていた `outputslotMan` 直参照を、注入された `IOutputSlotService` 経由に置き換える。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース拡張 | `DS4Windows/DI/IOutputSlotService.cs` | プラグイン／アンプラグ、変更イベント等の契約拡充 |
| ストアインターフェース | `DS4Windows/DI/IOutputSlotStore.cs` | `OutputSlots.xml` の永続化契約新設 |
| ストア実装 | `DS4Windows/DS4Control/Services/OutputSlotStore.cs` | 永続化 I/O の薄いラッパー実装 |
| サービス改修 | `DS4Windows/DS4Control/Services/OutputSlotService.cs` | ダミー配列撤廃、本物 `OutputSlotManager` への実配線 |
| UI 改修 | `DS4Windows/DS4Forms/OutputSlotManagerControl.xaml.cs` | 生の `OutputSlotManager` 直渡しを廃止、`IOutputSlotService` 経由化 |
| コントロール改修 | `DS4Windows/DS4Control/ControlService.cs` | `outputslotMan` への外部直アクセスを `IOutputSlotService` へ集約 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IOutputSlotStore`、実配線 `OutputSlotService` の登録 |
| 単体テスト新設 | `DS4WindowsTests/OutputSlotServiceTests.cs` | `Mock<IOutputSlotStore>` によるスロット変更・保存テスト新設 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step12-1: 実体 `OutputSlotManager` の呼び出し元精査
1. `ControlService.cs`、`MainWindow.xaml.cs`、`OutputSlotManagerControl.xaml.cs` における `outputslotMan` 参照箇所を全件特定する。

### タスク Step12-2: `IOutputSlotStore` & `OutputSlotStore` の新設
1. `DS4Windows/DI/IOutputSlotStore.cs` を新規作成。
2. `DS4Windows/DS4Control/Services/OutputSlotStore.cs` を新規作成。

### タスク Step12-3: `IOutputSlotService` の契約拡張
1. `DS4Windows/DI/IOutputSlotService.cs` にプラグイン・アンプラグ・変更イベントを追加。

### タスク Step12-4: `OutputSlotService` の実配線リファクタリング
1. `OutputSlotService.cs` から独自のダミー配列（`_outputDevices`）を完全削除。
2. コンストラクタで実体 `OutputSlotManager`、`IOutputSlotStore`、`IPathService` を受け取り、実体へ委譲（§1.2 実配線化）。

### タスク Step12-5: UI（`OutputSlotManagerControl.xaml.cs`）および呼び出し元の改修
1. 生の `outputslotMan` の直渡しを `IOutputSlotService` 呼び出しにピンポイント置換。

### タスク Step12-6: DI コンテナ登録の更新
1. `DS4Windows/DI/ServiceRegistration.cs` において、`OutputSlotService` に本物の `OutputSlotManager` が渡されるよう登録を整備。

### タスク Step12-7: 単体テスト作成と自動テスト実行
1. `OutputSlotServiceTests.cs` を新設し、モックを使ったプラグイン・永続化の自動テストを作成。
2. `dotnet test` で全テストパスを確認。

### タスク Step12-8: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step12 を更新。
3. `Phase5-Step12-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **二重管理による状態乖離** | 高 | `OutputSlotService` 内のダミー配列を完全撤廃し、実体 `OutputSlotManager` に一本化する（§1.2）。 |
| **ViGEm ドライバハング・BSoD** | 高 | `OutputSlotManager` の PnP キューおよび `ViGEmClient` 破棄順序を完全温存し、アダプター境界に限定する（§1.3）。 |
| **テスト時の実ファイル汚染** | 低 | `IOutputSlotStore` をモック化し、単体テスト時のファイル出力をトラップする（§1.4）。 |

---

## 5. 完了判定基準

- [ ] `OutputSlotService` のダミー配列が撤廃され、本物の `OutputSlotManager` に実配線されていること（§1.2）。
- [ ] `IOutputSlotStore` が新設され、スロット永続化が抽象化されていること。
- [ ] `OutputSlotManagerControl.xaml.cs` が `IOutputSlotService` 経由で動作していること。
- [ ] ViGEm ドライバ通信の非同期キューイングと破棄順序が温存されていること（§1.3）。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
