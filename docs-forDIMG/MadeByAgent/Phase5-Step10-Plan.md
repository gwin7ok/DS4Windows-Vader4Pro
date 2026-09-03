# フェーズ5-Step10 計画書: 残存サービス境界の整理（Path / ProfileSettings / KBM / UdpServer）

作成日: 2026-09-02（改訂日: 2026-09-03・UDPサーバー境界追記）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step10, §5.4（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-Addendum-Findings-Report.md`（追加監査・未割当課題）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `PathService`、`ProfileSettingsService`、`UdpServer` の既存のパブリックシグネチャを破壊しない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - パス解決（AppData／ローカル実行フォルダー）、ランブル設定、KBM送出、CemuhookプロトコルによるUDPモーション送信を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存のログレベルおよびログ出力を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。Phase3 で導入済みの `IDeviceStateAccessor` を最大限に活用する。
- **§3.2 巨大ファイルの編集方針**:
  - `UdpServer.cs`（916行）のバイナリパケット送出本体には手を触れず、ライフサイクル（開始・停止・状態取得）の薄いラッパー境界にとどめる。

---

## 0. Step10の位置づけと現状分析

### 0.1 対象範囲と現状の課題（GitHub実コード確認済み）
`Phase5-Step1-legacy-delegation-audit-report.md` および `Phase5-Step1-Addendum-Findings-Report.md`（発見3）に基づき、以下の4項目を対象とする。

1. **`IPathService` → `PathService`（#13）**: `Global.appdatapath` の起動時キャッシュ競合リスク。
2. **`IProfileSettingsService` → `ProfileSettingsService`（#6）**: `Program.rootHub.DS4Controllers[]` 参照の排除。
3. **KBMアダプター（`IVirtualKBM` / `OutputKBMHandlerAdapter`）**: DI登録状況の調査と確定。
4. **【追加】`UdpServer.cs`（Cemuhook モーションサーバー）**:
   - `MainWindow.xaml.cs` や `ControlService` から直接起動・停止され、`Program.rootHub.DS4Controllers` を直接ポーリングして外部配信している。
   - ライフサイクル管理が DI コンテナの外部に放置されており、テスト時のポート競合やモック化ができない。

### 0.2 `PathService` の現状と初期化順序リスク
実コード（`PathService.cs` line 18〜28）では、初回アクセス時に `Global.appdatapath` を読み込んで `_appDataPath` フィールドに永続キャッシュしている。
アプリ起動時に `Global.FindConfigLocation()`（設定フォルダ探索）が実行される**前**に DI コンテナの初期化等で `IPathService` にアクセスされた場合、空文字列（＝`AppContext.BaseDirectory`）でキャッシュが固定され、以降正しいプロファイルパスが読み込めなくなる潜在バグ（初期化順序の競合）が存在する。

### 0.3 `ProfileSettingsService` の `Program.rootHub` 直接参照
実コード（`ProfileSettingsService.cs`）において、以下の2箇所で静的シングルトン `Program.rootHub` を直接参照している。
- `GetRumbleBoost(int deviceIndex)`（line 42）: `Program.rootHub.DS4Controllers[deviceIndex]`
- `SetRumbleAutostopTime(int index, int value)`（line 65）: `Program.rootHub.DS4Controllers[index]`

### 0.4 全体4層モデルにおける位置づけ
いずれも**第4層 4-c 設定・環境・ハードウェアアクセス基盤**に属する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `PathService` のキャッシュ完全撤廃（ベストプラクティス・ガードレール §5.4準拠）
静的文字列 `Global.appdatapath` の参照はナノ秒オーダーで処理されるため、メモリキャッシュする実質的なメリットが皆無である。
不具合リスクの原因となっていたプライベートフィールド `_appDataPath` および排他ロック `_syncLock` を**完全に撤廃**し、プロパティ getter で常に `Global.appdatapath` を直接返す設計とする。
これにより、起動順序に依存しない 100% 安全なオンデマンド評価を実現する。

```csharp
public class PathService : IPathService
{
    // キャッシュを完全撤廃し、常に最新の Global.appdatapath を安全に返す（オンデマンド評価）
    public string AppDataPath => Global.appdatapath;
    public string DefaultProfilesPath => Path.Combine(AppDataPath, "Profiles");
}
```

---

### 1.2 `ProfileSettingsService` の `IDeviceStateAccessor` 経由への置換
Phase3 で整備済みの `IDeviceStateAccessor` を `ProfileSettingsService` のコンストラクタに注入し、`Program.rootHub` への依存を排除する。

```csharp
public class ProfileSettingsService : IProfileSettingsService
{
    private readonly BackingStore _config;
    private readonly IDeviceStateAccessor _deviceStateAccessor;

    public ProfileSettingsService(BackingStore config = null, IDeviceStateAccessor deviceStateAccessor = null)
    {
        _config = config ?? Global.store;
        _deviceStateAccessor = deviceStateAccessor ?? AppHost.Services.GetService<IDeviceStateAccessor>();
    }

    public int GetRumbleBoost(int deviceIndex)
    {
        var device = _deviceStateAccessor?.GetDevice(deviceIndex);
        return device != null ? device.RumbleBoost : 0;
    }
}
```

---

### 1.3 KBMアダプターのDI登録と境界設定
`OutputKBMHandlerAdapter` を `ServiceRegistration.cs` に `AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>()` として正式登録する。

---

### 1.4 【追加】`IUdpServerService` によるモーションサーバーの境界化
ソケット通信や Cemuhook プロトコルのパケット生成（`UdpServer.cs`）本体は解体せず、ライフサイクル（開始・停止・稼働状態・ポート設定）をラップする `IUdpServerService` を新設する。
これにより、`MainWindow.xaml.cs` や `SettingsViewModel` からの直接ソケット操作を完全に排除する。

```csharp
namespace DS4Windows.DI
{
    public interface IUdpServerService
    {
        bool IsRunning { get; }
        void Start(int port, string listenAddress = "127.0.0.1");
        void Stop();
    }
}
```

```csharp
// DS4Windows/DS4Control/Services/UdpServerService.cs 実装イメージ
public class UdpServerService : IUdpServerService
{
    private UdpServer _server;

    public bool IsRunning => _server != null;

    public void Start(int port, string listenAddress = "127.0.0.1")
    {
        Stop();
        _server = new UdpServer(Program.rootHub); // 将来の完全分離への過渡期シム
        _server.Start(port, listenAddress);
        AppLogger.LogTrace($"[DI] UdpServerService started on {listenAddress}:{port}");
    }

    public void Stop()
    {
        if (_server != null)
        {
            _server.Stop();
            _server = null;
            AppLogger.LogTrace("[DI] UdpServerService stopped.");
        }
    }
}
```

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| サービス改修 | `DS4Windows/DS4Control/Services/PathService.cs` | `_appDataPath` キャッシュおよびロックの完全撤廃（On-Demand評価） |
| サービス改修 | `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | `IDeviceStateAccessor` 注入、`Program.rootHub` 直接参照の排除 |
| インターフェース新設 | `DS4Windows/DI/IUdpServerService.cs` | UDPモーションサーバーのライフサイクル契約新設 |
| サービス実装新設 | `DS4Windows/DS4Control/Services/UdpServerService.cs` | サーバー起動・停止の薄い委譲ラッパー実装 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `IVirtualKBM`、`IUdpServerService` 登録追加 |
| 単体テスト拡充 | `DS4WindowsTests/PathServiceTests.cs` | パス解決の独立テスト |
| 単体テスト拡充 | `DS4WindowsTests/ProfileSettingsServiceTests.cs` | `Mock<IDeviceStateAccessor>` を渡すテストの拡充 |
| 単体テスト新設 | `DS4WindowsTests/UdpServerServiceTests.cs` | モックによるサーバーライフサイクルの単体テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step10-1: アプリ起動シーケンスの調査
1. `App.xaml.cs` および `Program.cs` における `Global.FindConfigLocation()` の呼び出しタイミングを確認する。

### タスク Step10-2: KBMアダプターの所在・DI登録状況の調査
1. `OutputKBMHandlerAdapter.cs` の依存関係を確認し、DIコンテナへの安全な登録可否を精査する。

### タスク Step10-3: `PathService` のキャッシュ完全撤廃実装
1. `PathService.cs` から `_appDataPath` フィールドと `lock (_syncLock)` を削除し、getter で `Global.appdatapath` を直接返すよう修正する。

### タスク Step10-4: `ProfileSettingsService` の `Program.rootHub` 直接参照置換
1. `ProfileSettingsService.cs` のコンストラクタに `IDeviceStateAccessor` を追加する。
2. `GetRumbleBoost` および `SetRumbleAutostopTime` の `Program.rootHub.DS4Controllers[index]` を `_deviceStateAccessor.GetDevice(index)` に置換する。

### タスク Step10-5: KBMアダプターのDI登録
1. `ServiceRegistration.cs` に `IVirtualKBM` を登録する。

### タスク Step10-6: `IUdpServerService` & `UdpServerService` の新設・登録
1. `DS4Windows/DI/IUdpServerService.cs` を新規作成。
2. `DS4Windows/DS4Control/Services/UdpServerService.cs` を新規作成。
3. `ServiceRegistration.cs` に Singleton 登録を追加。

### タスク Step10-7: 単体テスト作成と自動テスト実行
1. `ProfileSettingsServiceTests.cs`、`PathServiceTests.cs`、`UdpServerServiceTests.cs` を作成・拡充。
2. `dotnet test` で全テストパスを確認。

### タスク Step10-8: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認。
2. `Phase5-Status.md` の Step10 を更新。
3. `Phase5-Step10-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **起動時パス誤確定** | 高 | キャッシュを完全撤廃し、On-Demand で常に最新の `Global.appdatapath` を返す（§1.1）。 |
| **UdpServer のポート競合** | 中 | `UdpServerService` 内部で起動前に必ず停止処理を呼び、多重バインドを防止する（§1.4）。 |
| **テストコードのコンパイルエラー** | 低 | `ProfileSettingsService` のコンストラクタ変更に伴い、テスト側で `Mock<IDeviceStateAccessor>` を補正する（§1.2）。 |

---

## 5. 完了判定基準

- [ ] `PathService` のキャッシュフィールドが削除され、直接参照になっていること。
- [ ] On-Demand パス評価が徹底され、起動順序に依存しないパス解決が保証されていること（§1.1）。
- [ ] `ProfileSettingsService.cs` 内から `Program.rootHub` への参照が 0 件になっていること。
- [ ] `IVirtualKBM` のDI登録が完了していること。
- [ ] `IUdpServerService` が新設され、モーションサーバーのライフサイクルが DI 境界化されていること（§1.4）。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
