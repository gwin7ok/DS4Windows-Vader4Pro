# フェーズ5-Step6 計画書: アプリ全体設定（AppSettings）の永続化・状態管理のDI化

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step6, §5.1（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（Step2計画書。IProfileXmlStore新設）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.SaveSettings`／`Global.LoadSettings` 等の既存の静的呼び出し経路は、未移行コードとの互換性維持のため即座に削除せずシム化する。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - スタートアップ登録、トレイ最小化、通知設定、UDPサーバー設定、チェック更新設定等の全項目およびデフォルト値を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 設定保存・読込時のログ出力（`AppLogger`）を維持し、成否を追跡する `[DI]` 標準化ログを出力する。
- **§3.1 DI (Dependency Injection) の実装**:
  - 新規インターフェース `IAppSettingsService` を新設し、コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
  - Step 2 で新設した `IProfileXmlStore` と連携し、ファイル排他ロックを共有する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.SaveSettings`／`LoadSettings`）はピンポイント置換に留める。

---

## 0. Step6の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` §4-5（第2次追加調査）に基づき、`Profiles.xml` 内の `<AppSettings>` セクション、`AppSettingsDTO.cs`、および `Global.SaveSettings`／`LoadSettings` を対象とする。

1. **設定の二重構造と責任の混在**:
   DS4Windows には「コントローラーごとのプロファイル設定（Profile）」と「アプリ全体の共通設定（スタートアップ、通知、UDP等）」が存在する。現在 `IProfileSettingsService` はコントローラー別のプロファイル設定に特化しているが、アプリ全体設定を扱う専用の DI サービスが存在せず、`Global` の静的プロパティ群（`Global.runAtStartup` 等）に直アクセスされている。
2. **同一物理ファイルへの競合リスク（ガードレール §5.1）**:
   `Profiles.xml` には `<Profile>`（Step 2）と `<AppSettings>`（本Step 6）が同居している。別々のサービスが独立してファイル保存を行うと、ファイルロック競合（`IOException`）や、一方の保存が他方の変更ノードを消去する「ロストアップデート」の危険がある。

### 0.2 全体4層モデルにおける位置づけ
`AppSettings` サブシステムは **第4層 4-c 設定・プロファイル・アクションサービス層** に属する。アプリ全体設定の読み書きと状態通知を独立したサービスとして確立し、ドメイン1（プロファイル・設定系）を完全完結させる。

---

## 1. 設計方針とアーキテクチャ

事前検討に基づき、**論点1：案A（`IAppSettingsService` の完全新設）** および **論点2：案1（`IProfileXmlStore` への XML I/O 集約によるロストアップデート防止）** を採用する。

### 1.1 `IAppSettingsService` インターフェース設計（新規、第4層 4-c）
アプリ全体設定へのアクセス、永続化、および変更通知イベントを契約化する。`DS4Windows/DI/IAppSettingsService.cs`（名前空間 `DS4Windows.DI`）に新設する。

```csharp
namespace DS4Windows.DI
{
    public interface IAppSettingsService
    {
        // 主要設定プロパティ（AppSettingsDTO に対応）
        bool RunAtStartup { get; set; }
        bool CloseMinimizes { get; set; }
        bool UseUdpServer { get; set; }
        int UdpServerPort { get; set; }
        bool NotificationsEnabled { get; set; }
        int CheckWhen { get; set; }

        // 永続化操作
        bool LoadSettings();
        bool SaveSettings();

        // 変更通知イベント
        event EventHandler<string> SettingChanged;
    }
}
```

---

### 1.2 `IProfileXmlStore` への AppSettings I/O の統合（ガードレール §5.1準拠）
`Profiles.xml` に対する物理アクセス窓口を一本化するため、Step 2 で新設された `IProfileXmlStore` を拡張し、AppSettings 用のメソッドを追加する。

```csharp
namespace DS4Windows.DI
{
    public partial interface IProfileXmlStore
    {
        // Step 2 既存: プロファイル用
        bool LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control,
            string overridePath = "", bool xinputChange = true, bool postLoad = true);
        bool SaveProfileXml(int deviceIndex, string profileName);

        // Step 6 新設: アプリ全体設定用（同一プロセス内排他ロックを共有）
        bool LoadAppSettingsXml(AppSettingsDTO appSettings);
        bool SaveAppSettingsXml(AppSettingsDTO appSettings);
    }
}
```

- **ロストアップデート防止のメカニズム**:
  `ProfileXmlStore` 内の単一の排他ロック（`_fileLock`）下で `SaveAppSettingsXml` を実行する。
  `Profiles.xml` を開いた際、プロファイル定義ノード群（`<Profile>`）には手を触れず、`<AppSettings>` ノードのみをピンポイントで更新して保存することで、プロファイル保存とアプリ設定保存の競合・上書きを物理的に防止する。

---

### 1.3 `AppSettingsService` 実装クラス設計
- `DS4Windows/DS4Control/Services/AppSettingsService.cs` を作成。
- コンストラクタで `IProfileXmlStore` および `IPathService`（Step 10）を受け取る。
- 内部で `AppSettingsDTO` をキャッシュし、プロパティ getter/setter を通じて安全にアクセスさせる。
- 設定変更時は過渡期シム（§2.1）として静的 `Global` の該当プロパティへも値を反映し、未移行コードの動作を 100% 保証する。

```csharp
// AppSettingsService.cs 実装イメージ
public class AppSettingsService : IAppSettingsService
{
    private readonly IProfileXmlStore _xmlStore;
    private readonly IPathService _pathService;
    private AppSettingsDTO _dto = new AppSettingsDTO();

    public AppSettingsService(IProfileXmlStore xmlStore, IPathService pathService)
    {
        _xmlStore = xmlStore ?? throw new ArgumentNullException(nameof(xmlStore));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    }

    public bool RunAtStartup
    {
        get => _dto.RunAtStartup;
        set
        {
            if (_dto.RunAtStartup != value)
            {
                _dto.RunAtStartup = value;
                Global.runAtStartup = value; // 過渡期シム同期
                SettingChanged?.Invoke(this, nameof(RunAtStartup));
            }
        }
    }

    public bool SaveSettings()
    {
        bool success = _xmlStore.SaveAppSettingsXml(_dto);
        if (success)
            AppLogger.LogTrace("[DI] AppSettings saved successfully.");
        else
            AppLogger.LogToGui("Failed to save application settings.", true);
        return success;
    }
    ...
}
```

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IAppSettingsService.cs` | アプリ全体設定アクセスの新規契約 |
| サービス実装 | `DS4Windows/DS4Control/Services/AppSettingsService.cs` | 設定値管理、変更通知、過渡期同期シムの実装 |
| ストア拡張 | `DS4Windows/DI/IProfileXmlStore.cs` | `LoadAppSettingsXml` / `SaveAppSettingsXml` の追加 |
| ストア実装拡張 | `DS4Windows/DS4Control/Services/ProfileXmlStore.cs` | `<AppSettings>` ノードの排他ロック保存実装（ロストアップデート防止） |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IAppSettingsService` → `AppSettingsService` の Singleton 登録 |
| 単体テスト新設 | `DS4WindowsTests/AppSettingsServiceTests.cs` | 設定値変更通知、保存成否、排他I/Oの単体テスト新設 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step6-1: `Profiles.xml` の `<AppSettings>` 構造と `AppSettingsDTO.cs` の精査
1. `AppSettingsDTO.cs` の全プロパティおよび `ScpUtil.cs`（`Global.SaveSettings`）の XML ノード構造を精査する。

### タスク Step6-2: `IProfileXmlStore` への AppSettings I/O メソッド追加
1. `DS4Windows/DI/IProfileXmlStore.cs` に `LoadAppSettingsXml` / `SaveAppSettingsXml` を定義する。

### タスク Step6-3: `ProfileXmlStore` での排他保存実装
1. `DS4Windows/DS4Control/Services/ProfileXmlStore.cs` に実装を追加。
2. 既存の `_fileLock` 内で `<AppSettings>` ノードのピンポイント更新を記述し、排他直列化を保証する（§1.2）。

### タスク Step6-4: `IAppSettingsService` & `AppSettingsService` の新設
1. `DS4Windows/DI/IAppSettingsService.cs` を新規作成。
2. `DS4Windows/DS4Control/Services/AppSettingsService.cs` を新規作成し、DTO連携と過渡期同期を実装。

### タスク Step6-5: DIコンテナ登録と動作確認
1. `DS4Windows/DI/ServiceRegistration.cs` に `services.AddSingleton<IAppSettingsService, AppSettingsService>();` を追加。

### タスク Step6-6: 単体テスト作成と自動テスト実行
1. `AppSettingsServiceTests.cs` を新設。
2. モック化した `IProfileXmlStore` に対する保存・読込、およびプロパティ変更イベントの自動テストを作成。
3. `dotnet test` で全テストパスを確認。

### タスク Step6-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step6 を「計画書承認済」に更新。
3. `Phase5-Step6-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **Profiles.xml のロストアップデート** | 高 | `ProfileXmlStore` 内でファイルアクセスを排他ロックし、`<AppSettings>` のみをピンポイント更新する（§1.2）。 |
| **静的 Global との不整合** | 中 | setter 内で `Global` の静的フィールドへ即座に値を同期する過渡期シムを配置する（§1.3）。 |
| **保存失敗の握りつぶし** | 低 | `SaveSettings()` が `bool` を返し、失敗時は `[DI]` ログおよび GUI エラーを出力する。 |

---

## 5. 完了判定基準

- [ ] `IAppSettingsService` が新設され、DIコンテナに Singleton 登録されていること。
- [ ] `Profiles.xml` に対する XML I/O が `IProfileXmlStore` に集約され、排他制御下で安全に保存されること（§1.2）。
- [ ] 設定プロパティの変更時に過渡期シムとして `Global` 静的フィールドへも値が同期されること（§1.3）。
- [ ] 単体テストが新規作成され、すべてパスすること。
- [ ] ビルドエラーおよび警告の増加がないこと。
