# フェーズ4-Step5 計画書: 環境・UI・通知サービス

作成日: 2026-08-31
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §3.3, §4.1, §5, §6.6（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase4-Plan.md` §1.1.1, §1.2, §2, §3 Step5（Phase4詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`（Phase4進捗管理）
- `docs-forDIMG/MadeByAgent/Phase4-Step4-Completion-Report.md`（Step4完了報告）
- `docs-forDIMG/MadeByAgent/Phase4-Step0-Global-Member-Inventory.md`（Global棚卸し一覧）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global` の環境・パス・通知関連メンバー（`appdatapath`, `runAtStartup`, `startMinimized`, `notifications` 等）は削除せず、新設する各サービスへの薄いデリゲートシムとして残す。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - ファイルパス解決ロジック（AppData / ProgramData / カレントディレクトリ）、ウィンドウ幾何情報（幅・高さ・位置）、スタートアップ登録・最小化起動フラグ、トースト通知の挙動を 100% 維持する。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui` 等の既存ログ出力を厳格に維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - インターフェース契約は `DS4Windows/DI/`（名前空間 `DS4Windows.DI`）に配置（**DI永続資産**）。
  - 実装クラスは `DS4Windows/DS4Control/Services/`（名前空間 `DS4Windows`）に配置（**DI永続資産**）。
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs` は全体を再生成せず、対象メンバーのみをピンポイントで置換する。
- **資材のライフサイクル識別**:
  - DI永続資産（残るもの）と過渡期シム（Strangler Fig 移行用）を明確に区別して管理する。

---

## 0. Step5の位置づけと現状分析

### 0.1 Step0〜Step4の成果とStep5のスコープ
- **Step1〜Step4 で完了したこと**:
  - 設定（`IProfileSettingsService`）、プロファイル（`IProfileRepository`）、SpecialAction（`ISpecialActionRepository`）、デバイス状態（`IDeviceStateService`）、出力スロット（`IOutputSlotService`）の各 DI サービスが稼働。
- **Step5 で行うこと**:
  - `Global`（`ScpUtil.cs`）に集中している「パス・環境・UI・ログ関連（19件）」の static メンバーを、単一責任の原則に従い 3 つの DI サービス（`IPathService`, `IEnvironmentService`, `INotificationService`）として分離・DI化する。
  - Step6（Composition Root 一本化）および Step7〜9（ViewModel DI 化）に向けて、UI が必要とする環境・通知・パスの依存関係を完全に整備する。

### 0.2 全体4層モデルにおける責務境界と本Stepの位置づけ（全体計画書 §3.3 準拠）
全体計画書（`DI-App-Wide-Migration-Plan.md` §3.3）および Phase4 計画書（`Phase4-Plan.md` §1.1.1）で規定された **全体4層モデル（実行時3層 ＋ UI層）** に基づき、本Step（Step 5）の位置づけを以下のように整理する：

1. **第1層: 入力監視層**
   - コントローラーの機種差を吸収し、`DS4State` に正規化して上位へ渡す。
2. **第2層: 信号変換層（拡張版）**
   - 入力から「何を出力すべきか」を決定する（2-a 基本マッピング, 2-b SpecialAction判定, 2-c アクション選択, 2-d マクロ分解）。
3. **第3層: 信号出力層（拡張版）**
   - 決定された内容を実際に副作用として実行する（3-a 仮想コントローラー出力, 3-b KBM出力, 3-c アプリ内アクション実行）。
4. **第4層: UI層（制御面）**
   - ユーザーが設定・プロファイル・状態を操作し、サービス経由で実行時3層へ設定を反映する。
   - **4-a. View**: WPF の画面・UserControl。
   - **4-b. ViewModel**: 画面状態、入力値検証、画面イベントの調整。
   - **4-c. 設定／状態サービス 【★本Step対象】**:
     - `IPathService`: アプリケーションの物理ファイルパス解決・ディレクトリ管理。
     - `IEnvironmentService`: OS起動時実行、最小化起動、ウィンドウ位置・サイズ、言語設定。
     - `INotificationService`: トースト通知、タスクバー点滅、通知イベント発行。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IPathService` インターフェース設計 (第4層 4-c)
契約インターフェースは `DS4Windows/DI/IPathService.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IPathService
    {
        string AppDataPath { get; set; }
        string ExecutableDirectory { get; }
        string ProfilesPath { get; }
        string ActionsPath { get; }
        
        string GetProfilePath(string profileName);
        string GetAutoProfilesPath();
    }
}
```

### 1.2 `IEnvironmentService` インターフェース設計 (第4層 4-c)
契約インターフェースは `DS4Windows/DI/IEnvironmentService.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public interface IEnvironmentService
    {
        bool RunAtStartup { get; set; }
        bool StartMinimized { get; set; }
        bool CloseMinimizes { get; set; }
        string UseLang { get; set; }
        
        int FormWidth { get; set; }
        int FormHeight { get; set; }
        int FormLocationX { get; set; }
        int FormLocationY { get; set; }
        
        event EventHandler EnvironmentSettingChanged;
    }
}
```

### 1.3 `INotificationService` インターフェース設計 (第4層 4-c)
契約インターフェースは `DS4Windows/DI/INotificationService.cs`（名前空間 `DS4Windows.DI`）に定義する。

```csharp
namespace DS4Windows.DI
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public bool IsToast { get; }

        public NotificationEventArgs(string title, string message, bool isToast = true)
        {
            Title = title;
            Message = message;
            IsToast = isToast;
        }
    }

    public interface INotificationService
    {
        bool NotificationsEnabled { get; set; }
        bool FlashTaskbar { get; set; }
        
        void SendNotification(string title, string message, bool isToast = true);
        
        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}
```

### 1.4 `Global` (in `ScpUtil.cs`) シム設計
`Global.appdatapath`, `Global.runAtStartup`, `Global.startMinimized`, `Global.notifications` 等の静的メンバーは、新設サービスへの薄いシムとする。

```csharp
private static DS4Windows.DI.IPathService pathService = null;
private static readonly DS4Windows.DI.IPathService fallbackPathService = new PathService();

public static DS4Windows.DI.IPathService PathServiceInstance
{
    get
    {
        if (pathService != null) return pathService;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.IPathService>();
            if (service != null)
            {
                pathService = service;
                return pathService;
            }
        }
        catch { }
        return fallbackPathService;
    }
    set => pathService = value;
}

private static DS4Windows.DI.IEnvironmentService environmentService = null;
private static readonly DS4Windows.DI.IEnvironmentService fallbackEnvironmentService = new EnvironmentService();

public static DS4Windows.DI.IEnvironmentService EnvironmentServiceInstance
{
    get
    {
        if (environmentService != null) return environmentService;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.IEnvironmentService>();
            if (service != null)
            {
                environmentService = service;
                return environmentService;
            }
        }
        catch { }
        return fallbackEnvironmentService;
    }
    set => environmentService = value;
}

private static DS4Windows.DI.INotificationService notificationService = null;
private static readonly DS4Windows.DI.INotificationService fallbackNotificationService = new NotificationService();

public static DS4Windows.DI.INotificationService NotificationServiceInstance
{
    get
    {
        if (notificationService != null) return notificationService;
        try
        {
            var service = AppHost.GetService<DS4Windows.DI.INotificationService>();
            if (service != null)
            {
                notificationService = service;
                return notificationService;
            }
        }
        catch { }
        return fallbackNotificationService;
    }
    set => notificationService = value;
}
```

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IPathService.cs` | 新規 | **DI永続資産** | 第4層 4-c パス解決の契約インターフェース |
| `DS4Windows/DI/IEnvironmentService.cs` | 新規 | **DI永続資産** | 第4層 4-c 環境・起動設定の契約インターフェース |
| `DS4Windows/DI/INotificationService.cs` | 新規 | **DI永続資産** | 第4層 4-c 通知管理・イベント通知の契約インターフェース |
| `DS4Windows/DS4Control/Services/PathService.cs` | 新規 | **DI永続資産** | `IPathService` の本番実装クラス |
| `DS4Windows/DS4Control/Services/EnvironmentService.cs` | 新規 | **DI永続資産** | `IEnvironmentService` の本番実装クラス |
| `DS4Windows/DS4Control/Services/NotificationService.cs` | 新規 | **DI永続資産** | `INotificationService` の本番実装クラス |
| `DS4Windows/DI/ServiceRegistration.cs` | 更新 | **DI永続資産** | 各サービスの Singleton 登録 |
| `DS4Windows/DS4Control/ScpUtil.cs` | 更新 | **過渡期シム** | `Global` のパス・環境・通知メンバーを新サービスへのシムへピンポイント置換 |
| `DS4WindowsTests/PathServiceTests.cs` | 新規 | **テスト資産** | パス解決・ディレクトリ管理の単体テスト |
| `DS4WindowsTests/EnvironmentServiceTests.cs` | 新規 | **テスト資産** | 起動・幾何情報・言語設定の単体テスト |
| `DS4WindowsTests/NotificationServiceTests.cs` | 新規 | **テスト資産** | 通知設定・イベント発火の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase4-Step5-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase4-Step5-Completion-Report.md` | 新規 | ドキュメント | Step5完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase4-Status.md` | 更新 | ドキュメント | Step5進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step5-1: インターフェース定義（契約）
- `DS4Windows/DI/IPathService.cs`, `IEnvironmentService.cs`, `INotificationService.cs` を新規作成（名前空間: `DS4Windows.DI`）。

### タスク Step5-2: 実装クラス作成（実体）
- `DS4Windows/DS4Control/Services/PathService.cs`, `EnvironmentService.cs`, `NotificationService.cs` を新規作成（名前空間: `DS4Windows`）。
- スレッドセーフな設定保持、パス解決、変更イベント通知を実装。

### タスク Step5-3: DI コンテナ登録更新
- `DS4Windows/DI/ServiceRegistration.cs` に 3 サービスの Singleton 登録を追加。

### タスク Step5-4: `Global` (in `ScpUtil.cs`) ピンポイントシム化
- `ScpUtil.cs` に `PathServiceInstance`, `EnvironmentServiceInstance`, `NotificationServiceInstance` シムを追加し、対象プロパティを委譲。

### タスク Step5-5: 単体テスト作成と自動テスト実行
- `DS4WindowsTests/PathServiceTests.cs`, `EnvironmentServiceTests.cs`, `NotificationServiceTests.cs` を作成・実行。
- 回帰テスト（`Actions.Tests` 31件, `StandaloneTests` 13件, 全新設テスト）の通過を確認。

### タスク Step5-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認。
- `Phase4-Status.md` を更新し、`Phase4-Step5-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| ファイルパスの環境依存（AppDataの有無） | Step5-2 | `Global.appdatapath` のフォールバックロジック（`AppContext.BaseDirectory`）を完全に維持する。 |
| ウィンドウサイズ等の既定値不整合 | Step5-2 | 旧 `Global` の初期値（`formWidth`, `formHeight` 等）と完全一致させる。 |
| 巨大ファイル `ScpUtil.cs` の編集によるコード欠損 | Step5-4 | ピンポイント置換を徹底し、対象ブロックのみを書き換える。 |

---

## 5. 完了判定基準

- [ ] `IPathService`, `IEnvironmentService`, `INotificationService` が `DS4Windows/DI/` に定義されている（DI永続資産）。
- [ ] `PathService`, `EnvironmentService`, `NotificationService` が `DS4Windows/DS4Control/Services/` に実装されている（DI永続資産）。
- [ ] `ServiceRegistration.cs` に登録され、`AppHost` から解決できる。
- [ ] `Global` のパス・環境・通知関連メンバーがシム化され、既存コードが無変更で動作する。
- [ ] 新設した単体テストおよび既存の回帰テストが全件成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase4-Status.md` が更新され、`Phase4-Step5-Completion-Report.md` が作成されている。

