# DI オブジェクトグラフ設計ドキュメント

作成日: 2025-12-20

目的
- DS4Windows の段階的 DI 移行に先立ち、導入するフレームワーク、主要サービスのオブジェクトグラフ（Composition Root）設計、サービスのライフタイム方針、および最初の実装雛形の位置付けを定義する。

保存場所
- このドキュメント: `docs/DI/DI-ObjectGraph.md`
- サービス登録雛形: `DS4Windows/DI/ServiceRegistration.cs`
- Host スケルトン: `DS4Windows/DI/AppHost.cs`

---

1. 目標サマリ
- アプリ起動時に `Host`（`IHost`）を構築し `Host.Services` をアプリ全体の `IServiceProvider` とする。
- 主要サービスを `ConfigureServices` で登録し、Mapping/Controller/Action の依存をすべて DI 経由にしていく。
- 既存動作を壊さない段階移行（フォールバックの保持）を基本とする。

---

2. 推奨パッケージ
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration
- Scrutor (optional)
- xUnit / Moq / coverlet.collector (テスト用)

---

3. 高レベルのオブジェクトグラフ（テキスト図）

App (WPF) --> AppHost (Build Host)
AppHost -> IHost -> Host.Services (IServiceProvider)
Host.Services provides:
  - IManagedActionManager (Singleton)
  - IActionRegistry / IActionFactory (Singleton)
  - IActionBindingFactory (Singleton)
  - IVirtualKBM (Singleton or Scoped per-device)
  - IProfileService (Singleton)
  - IMacroService / MacroHostedService (HostedService)
  - Controllers (KeyButtonActionController, MacroController, MouseController) (Singleton)
  - ViewModels / Views (MainWindow resolved from DI)

簡単な依存例:
  Mapping -> (detect trigger) -> ActionManager.DispatchTrigger... -> TriggerContext -> ActionImpl (IInputAction/IOutputAction) -> Controller -> IVirtualKBM

---

4. サービス候補と推奨ライフタイム
- `IManagedActionManager` : Singleton
  - 理由: 全アクションエントリの単一点管理。アプリ全体で共有されるべき。
- `IActionBindingFactory` : Singleton
  - 理由: バインディングの生成ロジックはステートレス。
- `IVirtualKBM` (抽象ハンドラ): Singleton またはデバイス毎に Scoped
  - 理由: 物理リソースを扱うため管理しやすいライフタイムが重要（まずは Singleton）。テストではモックに差し替え。
- `IProfileService` : Singleton
- `IMacroService` : Singleton + `MacroHostedService : IHostedService`（バックグラウンド実行）
- 各 Controller (KeyButtonActionController 等) : Singleton
- ViewModels : Transient または Singleton（既存設計に合わせる）

---

5. Composition Root の責務
- Host の構築（`CreateDefaultBuilder()` を利用）
- `ConfigureServices` における全サービス登録
- ログ・設定の初期化
- `MainWindow` 等のルート UI を DI から解決し表示

---

6. 既存の静的ファサードとの共存戦略
- 既存 `ActionManager` static façade はすぐには削除せず、内部で `ServiceProviderHolder.Provider`（既に存在）を参照して DI 実装 (`DefaultActionManager`) をデリゲートする方式を維持。
- 段階的に static 経由のコードを DI 注入へ置換する。

---

7. 最初に追加するコード（実装雛形）
- `DS4Windows/DI/ServiceRegistration.cs` : `ConfigureServices(IServiceCollection services, IConfiguration config)` を提供。主要サービスの登録箇所。
- `DS4Windows/DI/AppHost.cs` : Host の Build / Start / Stop のラッパー。`App.xaml.cs` から呼び出す想定。

サンプルの役割分担:
- `ServiceRegistration` にサービス一覧を集中させる。
- 実際の `App.xaml.cs` は最初は手動で Host を呼び出し、段階的に移行する。

---

8. テスト方針
- `IManagedActionManager` と `IVirtualKBM` はインターフェースとしてテストでモック可能にする。
- Host をテスト用に構築するヘルパーを作成（例: `TestHostBuilder.Create()`）し、Unit / Integration テスト両方で再利用。

---

9. 次の具体タスク（短期）
- (1) NuGet 依存追加の PR（`Microsoft.Extensions.Hosting` 等）
- (2) `DS4Windows/DI/ServiceRegistration.cs`, `DS4Windows/DI/AppHost.cs` を追加する PR（小粒）
- (3) `IManagedActionManager` の `AddSingleton` 登録と起動時の簡易動作確認

---

10. 参考コードサンプルは `DS4Windows/DI` に雛形を配置しています。


---

(ドキュメント終了)