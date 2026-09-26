# フェーズ5-Step5 計画書: AutoProfile（自動プロファイル切替）の自律実行系DI化

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step5, §5.2, §5.3, §5.6（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step3-Plan.md`（プロファイル適用契約の一本化）
- `docs-forDIMG/MadeByAgent/Phase5-Step4-Plan.md`（結果伝播と通知の統一）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 既存の `AutoProfileChecker` 呼び出し経路は、新しい DI サービスへの移行完了まで互換シムを残す。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - アプリケーションパス・タイトル一致判定、正規表現マッチ、優先度順評価、未一致時の既定プロファイル復帰を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - プロファイル切替時のログ出力（`AppLogger.LogToGui` / `AppLogger.LogTrace`）を維持し、`[DI]` 標準化ログを出力する。
- **§3.1 DI (Dependency Injection) の実装**:
  - 新規インターフェース `IAutoProfileService` を新設し、コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
  - Phase 3 で導入済みの `IProcessInspector` を活用し、OS ネイティブ呼び出しを抽象化する。
- **§3.2 巨大ファイルの編集方針**:
  - `ControlService.cs` や `App.xaml.cs` の呼び出し箇所はピンポイント置換に留める。

---

## 0. Step5の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §4-5（第2次追加調査）に基づき、`AutoProfileChecker.cs` および `AutoProfileHolder.cs` を対象とする。

1. **DI 管理外での自律実行**:
   `AutoProfileChecker` はバックグラウンドタイマー（`System.Timers.Timer`）を自前で保持し自律動作しているが、DI コンテナの管理外でインスタンス化されている。
2. **静的シングルトンへの直接依存**:
   アクティブウィンドウの変更を検知した際、`Global.ApplyProfile(...)` や `Program.rootHub` を直接呼び出している（Step3で確立された統一適用契約 `IProfileApplicationService` を経由していない）。
3. **OS ネイティブ API への密結合とテスト不能**:
   フォアグラウンドプロセスの取得において、Windows API（`user32.dll` の `GetForegroundWindow` 等）を直接叩いているため、実機環境でゲームを起動しないと自動切替の単体テストが実行できない。

### 0.2 全体4層モデルにおける位置づけ
`AutoProfile` サブシステムは **第4層 4-c 設定・プロファイル・アクションサービス層** に属する。バックグラウンド自律実行サービスとして DI コンテナに統合し、プロファイル切替パイプラインを一本化する。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（`IAutoProfileService` 新設）** および **論点2：案1（`IProcessInspector` 活用）** を採用する。

### 1.1 `IAutoProfileService` インターフェース設計（新規、第4層 4-c）
自律実行サービスのライフサイクル（開始・停止）と手動評価メソッドを契約化する。`DS4Windows/DI/IAutoProfileService.cs`（名前空間 `DS4Windows.DI`）に新設する。

```csharp
namespace DS4Windows.DI
{
    public interface IAutoProfileService
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
        void CheckProfiles(); // タイマー周期またはイベント駆動による判定実行
    }
}
```

---

### 1.2 `AutoProfileService` 実装クラス設計（依存性の注入）
既存の `AutoProfileChecker` のロジックを継承・リファクタリングし、以下のサービスをコンストラクタ注入する。

- **`IProfileApplicationService`**: Step3・Step4 で確立されたプロファイル適用の唯一の契約。
- **`IProcessInspector`**: Phase 3 で導入されたプロセス情報取得サービス。OS API を直接叩かず本サービスに委譲する。
- **`IProfileSettingsService`**: 自動プロファイル機能の有効/無効設定フラグ等の参照。

```csharp
// DS4Windows/DS4Control/Services/AutoProfileService.cs 実装イメージ
public class AutoProfileService : IAutoProfileService, IDisposable
{
    private readonly IProfileApplicationService _profileAppService;
    private readonly IProcessInspector _processInspector;
    private readonly IProfileSettingsService _profileSettings;
    private readonly System.Timers.Timer _timer;
    private readonly object _syncLock = new object();

    public AutoProfileService(
        IProfileApplicationService profileAppService,
        IProcessInspector processInspector,
        IProfileSettingsService profileSettings)
    {
        _profileAppService = profileAppService ?? throw new ArgumentNullException(nameof(profileAppService));
        _processInspector = processInspector ?? throw new ArgumentNullException(nameof(processInspector));
        _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (s, e) => CheckProfiles();
    }
...
}
```

---

### 1.3 アーキテクチャ・ガードレールの適用（Phase5-Plan §5.2, §5.3, §5.6準拠）

#### 1.3.1 [マルチスレッド直列化]（§5.3）
- **【問題の実態】**: `_timer.Elapsed` はバックグラウンドスレッドで発火する。一方、UI（`ControllersViewModel` 等）は WPF UI スレッドでプロファイルを読み書きしている。`BackingStore` 内部のデータ構造はスレッドセーフではないため、並行アクセスで競合・データ破損が発生する。
- **【推奨対策】**: プロファイル切替を発火する際は、`Application.Current.Dispatcher.Invoke / BeginInvoke`（または `_syncLock` による同期オブジェクト）を利用して直列化し、UI 操作とタイマー処理の衝突を完全に防止する。

#### 1.3.2 [適用時入力停止（Halt）保証]（§5.2）
- **【問題の実態】**: ゲームプレイ中の激しい入力中にプロファイル辞書が再構築されると、ポーリングスレッド側で `InvalidOperationException: コレクションが変更されました` が発生し、アプリがサイレントクラッシュする。
- **【推奨対策】**: Step 3 で新設した `_profileAppService.ApplyProfile` を呼び出す。内部で `device.HaltReportingRunAction` が自動適用され、コントローラーの入力ループが安全に一時停止された状態でプロファイルが切り替わる。

#### 1.3.3 [切断時の一時プロファイル残留防止]（§5.6）
- **【問題の実態】**: 自動切替と一時プロファイルが競合している状態でコントローラーが物理切断された場合、状態リークが発生する。
- **【推奨対策】**: Step 3 の切断時クリーンアップと協調し、物理切断時は自動切替ルーティングの内部追跡フラグも初期化する。

---

### 1.4 `IProcessInspector` の活用によるテスト容易性の確立
フォアグラウンドプロセスの取得を `_processInspector.GetForegroundProcessInfo()` 等に委譲することで、**単体テストにおいて「特定のゲーム `.exe` がアクティブになった」状況を Moq で 100% 模倣（モック）できる**。
Windows OS のネイティブ環境に依存せず、CI や `dotnet test` 上で自動プロファイル切替の完全自動テストを実現する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IAutoProfileService.cs` | 自律実行サービスの新規契約（`Start`, `Stop`, `CheckProfiles`） |
| サービス実装 | `DS4Windows/DS4Control/Services/AutoProfileService.cs` | `AutoProfileChecker` をリファクタリング、DI注入、スレッド直列化 |
| サービス拡張 | `DS4Windows/DS4Control/Services/IProcessInspector.cs` | フォアグラウンドプロセス取得メソッドの確認・必要に応じた軽微な追加 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IAutoProfileService` → `AutoProfileService` の Singleton 登録 |
| 呼び出し元改修 | `DS4Windows/App.xaml.cs` または `ControlService.cs` | `IAutoProfileService` 注入とライフサイクル開始呼び出し |
| 単体テスト新設 | `DS4WindowsTests/AutoProfileServiceTests.cs` | `IProcessInspector` モックによる自動切替・直列化の自動テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step5-1: `AutoProfileChecker.cs` および `IProcessInspector` の実装精査
1. `AutoProfileChecker.cs` を精査し、保持しているタイマー、プロセス照合、プロファイル適用のコードを特定する。
2. `IProcessInspector.cs` の既存メソッドを確認し、アクティブウィンドウ情報取得に必要なシグネチャを精査する。

### タスク Step5-2: `IProcessInspector` の軽微な拡張（必要な場合のみ）
1. フォアグラウンドプロセス名・ウィンドウタイトル取得メソッドが未定義であれば `IProcessInspector` に追加し、`DefaultProcessInspector.cs` で実装する（§2.1 シム維持）。

### タスク Step5-3: `IAutoProfileService` インターフェースの新設
1. `DS4Windows/DI/IAutoProfileService.cs` を新規作成する。

### タスク Step5-4: `AutoProfileService` の実装
1. `DS4Windows/DS4Control/Services/AutoProfileService.cs` を作成。
2. `IProfileApplicationService`、`IProcessInspector`、`IProfileSettingsService` を注入。
3. ガードレール（§1.3.1 直列化、§1.3.2 Halt保証）を内包した照合・切替ロジックを実装。

### タスク Step5-5: DIコンテナ登録および起動配線
1. `DS4Windows/DI/ServiceRegistration.cs` に Singleton 登録を追加。
2. `App.xaml.cs`（または `ControlService`）で `IAutoProfileService` を取得し、`Start()` を呼び出すよう配線。

### タスク Step5-6: 単体テスト作成と自動テスト実行
1. `AutoProfileServiceTests.cs` を新設。
2. モック化した `IProcessInspector` から特定プロセス名を返却させ、`_profileAppService.ApplyProfile` が期待通り呼び出されることを検証。
3. `dotnet test` で全テストパスを確認。

### タスク Step5-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step5 を「計画書承認済」に更新。
3. `Phase5-Step5-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **タイマーとUIスレッドのデータ競合** | 高 | 切替発火時に UI スレッド（Dispatcher）へマーシャリングして直列化する（§1.3.1）。 |
| **激しい入力中のクラッシュ** | 高 | `IProfileApplicationService.ApplyProfile` の Halt 停止機構を経由して適用する（§1.3.2）。 |
| **プロセス監視のCPU負荷** | 低 | 既存のインターバル（1000ms等）を維持し、タイマー処理内で重いI/Oを行わない。 |
| **プロセス取得失敗（権限不足等）** | 低 | `IProcessInspector` 内で例外を捕捉し、安全にスキップ（既存維持）する。 |

---

## 5. 完了判定基準

- [ ] `IAutoProfileService` が新設され、DIコンテナに Singleton 登録されていること。
- [ ] `AutoProfileService` が `IProfileApplicationService` 経由でプロファイルを切り替えていること。
- [ ] タイマースレッドと UI スレッドの競合が防止されていること（§1.3.1）。
- [ ] `IProcessInspector` のモックによる完全自動単体テストが成功すること。
- [ ] ビルドエラーおよび警告の増加がないこと。
