# フェーズ5-Step10 計画書: 残存サービス境界の整理

作成日: 2026-09-02（改訂日: 2026-09-03・アーキテクチャガードレール反映）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step10, §5.4（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `PathService`、`ProfileSettingsService` の既存のパブリックシグネチャを破壊しない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - パス解決（AppData／ローカル実行フォルダー）、ランブルブースト値の読み取り、ランブル自動停止時間の設定を100%維持する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存のログレベルおよびログ出力を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `DS4Windows/DI/ServiceRegistration.cs` に行う。Phase3 で導入済みの `IDeviceStateAccessor` を最大限に活用する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.appdatapath` 等）はピンポイント置換のみ行う。

---

## 0. Step10の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の以下3項目を対象とする。

- `IPathService` → `PathService`（#13）: `Global.appdatapath` 読み取りとキャッシュ競合の解消
- `IProfileSettingsService` → `ProfileSettingsService`（#6）: `Program.rootHub.DS4Controllers[]` 参照の排除
- KBMアダプター（`IVirtualKBM` / `OutputKBMHandlerAdapter`）: DI登録状況の調査と確定

### 0.2 `PathService` の現状と初期化順序リスク
実コード（`PathService.cs` line 18〜28）では、初回アクセス時に `Global.appdatapath` を読み込んで `_appDataPath` フィールドに永続キャッシュしている。
アプリ起動時に `Global.FindConfigLocation()`（設定フォルダ探索）が実行される**前**に DI コンテナの初期化等で `IPathService` にアクセスされた場合、空文字列（＝`AppContext.BaseDirectory`）でキャッシュが固定され、以降正しいプロファイルパスが読み込めなくなる潜在バグ（初期化順序の競合）が存在する。

### 0.3 `ProfileSettingsService` の `Program.rootHub` 直接参照
実コード（`ProfileSettingsService.cs`）において、以下の2箇所で静的シングルトン `Program.rootHub` を直接参照している。
- `GetRumbleBoost(int deviceIndex)`（line 42）: `Program.rootHub.DS4Controllers[deviceIndex]`
- `SetRumbleAutostopTime(int index, int value)`（line 65）: `Program.rootHub.DS4Controllers[index]`

### 0.4 KBMアダプターの現状
`DS4Windows/DS4Control/Services/OutputKBMHandlerAdapter.cs`（`IVirtualKBM` 実装）が存在するが、`ServiceRegistration.cs` に登録されていない疑いがある。調査を行い、DIコンテナへの正式登録を行う。

### 0.5 全体4層モデルにおける位置づけ
いずれも**第4層 4-c**（設定・環境・ハードウェアアクセス基盤）に属する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `PathService` のキャッシュ完全撤廃（ベストプラクティス）
静的文字列 `Global.appdatapath` の参照はナノ秒オーダーで処理されるため、メモリキャッシュする実質的なメリットが皆無である。
不具合リスクの原因となっていたプライベートフィールド `_appDataPath` および排他ロック `_syncLock` を**完全に撤廃**し、プロパティ getter で常に `Global.appdatapath` を直接返す設計とする。
これにより、起動順序に依存しない 100% 安全なパス解決を実現する。

```csharp
public class PathService : IPathService
{
    // キャッシュを完全撤廃し、常に最新の Global.appdatapath を安全に返す
    public string AppDataPath => Global.appdatapath;
    public string DefaultProfilesPath => Path.Combine(AppDataPath, "Profiles");
}
```

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

### 1.3 KBMアダプターのDI登録と境界設定
タスク Step10-2 の調査結果に基づき、`OutputKBMHandlerAdapter` を `ServiceRegistration.cs` に `AddSingleton<IVirtualKBM, OutputKBMHandlerAdapter>()` として正式登録する。

---

### 1.4 アーキテクチャ・ガードレール: On-Demandパス評価による初期化順序の安全性保証（Phase5-Plan §5.4準拠）

#### 【問題の実態】
- `AppHost.Initialize()`（DIコンテナのビルド）は `App.xaml.cs` の極めて早期に実行される。
- 一方、ポータブルモード（実行フォルダ同居）か `%APPDATA%` モードかを判定・決定する `Global.FindConfigLocation()` はその後に呼び出される。
- もし Singleton サービス（`PathService`、`ProfileRepository` 等）のコンストラクタ内でパスプロパティ（`AppDataPath`）を先読みしてプライベートフィールドにキャッシュしてしまうと、まだパス探索が完了していないため、意図せず既定のフォールバックパス（空文字や BaseDirectory）でインスタンスが初期化・固定化され、設定やプロファイルが一切読み込めなくなる致命的な起動時障害が発生する。

#### 【推奨対策】
- `PathService` はキャッシュフィールドを保持せず、常にプロパティ getter で `Global.appdatapath` を直接評価する（§1.1）。
- `IPathService` を注入されるすべてのサービス（`ProfileRepository`、`SpecialActionRepository`、Step6 `AppSettingsService` 等）は、**コンストラクタ内でパス文字列を先読み・キャッシュしてはならず、メソッド呼び出し時に毎回 `_pathService.AppDataPath` や `DefaultProfilesPath` を参照する「On-Demand（オンデマンド）評価」を徹底**する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| サービス改修 | `DS4Windows/DS4Control/Services/PathService.cs` | `_appDataPath` キャッシュおよびロックの完全撤廃（On-Demand評価） |
| サービス改修 | `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | `IDeviceStateAccessor` 注入、`Program.rootHub` 直接参照の排除 |
| DI 登録 | `DS4Windows/DI/ServiceRegistration.cs` | `ProfileSettingsService` コンストラクタ解決確認、`IVirtualKBM` 登録追加 |
| 単体テスト拡充 | `DS4WindowsTests/PathServiceTests.cs` | パス解決の独立テスト |
| 単体テスト拡充 | `DS4WindowsTests/ProfileSettingsServiceTests.cs` | `Mock<IDeviceStateAccessor>` を渡すテストの拡充 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step10-1: アプリ起動シーケンスの調査
1. `App.xaml.cs` および `Program.cs` における `Global.FindConfigLocation()` の呼び出しタイミングを確認する。

### タスク Step10-2: KBMアダプターの所在・DI登録状況の調査
1. `OutputKBMHandlerAdapter.cs` の依存関係を確認し、DIコンテナへの安全な登録可否を精査する。

### タスク Step10-3: `PathService` のキャッシュ完全撤廃実装
1. `PathService.cs` から `_appDataPath` フィールドと `lock (_syncLock)` を削除し、getter で `Global.appdatapath` を直接返すよう修正する（§1.4 ガードレール準拠）。

### タスク Step10-4: `ProfileSettingsService` の `Program.rootHub` 直接参照置換
1. `ProfileSettingsService.cs` のコンストラクタに `IDeviceStateAccessor` を追加する。
2. `GetRumbleBoost` および `SetRumbleAutostopTime` の `Program.rootHub.DS4Controllers[index]` を `_deviceStateAccessor.GetDevice(index)` に置換する。

### タスク Step10-5: KBMアダプターのDI登録（調査結果に基づき実施）
1. `ServiceRegistration.cs` に `IVirtualKBM` を登録する。

### タスク Step10-6: 単体テスト作成と自動テスト実行
1. `ProfileSettingsServiceTests.cs` に `Mock<IDeviceStateAccessor>` を追加し、ランブル取得・設定のテストを更新・拡充する。
2. `dotnet test` で全テストパスを確認する。

### タスク Step10-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルド成功を確認する。
2. `Phase5-Status.md` を更新する。
3. `Phase5-Step10-Completion-Report.md` を作成する。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **起動時パス誤確定** | 高 | キャッシュを完全撤廃し、On-Demand で常に最新の `Global.appdatapath` を返す（§1.4）。 |
| **テストコードのコンパイルエラー** | 低 | `ProfileSettingsService` のコンストラクタ変更に伴い、テスト側で `Mock<IDeviceStateAccessor>` を補正する（§1.2）。 |

---

## 5. 完了判定基準

- [ ] `PathService` のキャッシュフィールドが削除され、直接参照になっていること。
- [ ] On-Demand パス評価が徹底され、起動順序に依存しないパス解決が保証されていること（§1.4）。
- [ ] `ProfileSettingsService.cs` 内から `Program.rootHub` への参照が 0 件になっていること。
- [ ] `IVirtualKBM` のDI登録状況が確定していること。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
