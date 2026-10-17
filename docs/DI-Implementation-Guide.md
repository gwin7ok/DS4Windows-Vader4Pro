# DI 実装ガイド — 概要と導入手順

作成日: 2025-12-20

目的: 既存の DS4Windows コードベースを機能を維持したまま段階的に DI（依存性注入）ベースへ移行する際の設計指針、導入手順、注意点をまとめる。

---

## 要約
- 現状: `.NET` / WPF アプリで `IManagedActionManager` 等のサービスが既に使われており、DI を体系化する土台あり。
- 目標: `Mapping` 等に散在する副作用（キー/マウス送出、マクロ、プロファイル切替、プロセス起動など）を `ActionManager` と各 `Action` / `Controller` 経由で一元化し、テスト性・保守性を向上させる。
- 推奨スタック: `Microsoft.Extensions.Hosting` + `Microsoft.Extensions.DependencyInjection`（組込み DI）を中心に、必要に応じて `Scrutor`（自動スキャン/デコレータ）や将来的に `Autofac` を導入。

---

## 推奨フレームワークと理由
- Microsoft.Extensions.Hosting / DI
  - .NET 標準で互換性が高く軽量。アプリ起動時に Host を構築すれば、ログ・構成・ライフサイクルを統合できる。
- Scrutor
  - `services.Scan(...)` により実装の自動登録やデコレータが手軽。リファクタ時の作業負荷低下に有益。
- Autofac（オプション）
  - より高度なコンテナ機能が必要な場合に差し替え可能（`Autofac.Extensions.DependencyInjection`）。
- Prism / DryIoc（検討）
  - 大規模な WPF MVVM の再編を行う場合のみ検討。移行コスト高。

---

## 目標設計（高レベル）
- アプリ全体で `IServiceProvider` を単一の源泉にする（`Host.Services` を利用）。
- `IManagedActionManager`, `IActionBindingFactory`, `IVirtualKBM` 等のインターフェースを DI に登録。
- `Mapping.cs` はトリガー検出までを担当し、送出は `ActionManager.DispatchTrigger...` を通す（`TriggerContext` を渡す）。
- 各送出（Key/Mouse/Macro/Launch）は `IOutputAction` 実装にまとめ、実際の副作用はコントローラ／ハンドラが担当する。

---

## 導入手順（段階的）
1. 前準備
   - 既存のサービス登録（`App.xaml.cs` 等）を `IServiceCollection` に統一する準備をする。
2. Host の導入（小さな PR）
   - 追加 NuGet:
     - `Microsoft.Extensions.Hosting`
     - `Microsoft.Extensions.DependencyInjection`
     - `Microsoft.Extensions.Logging`
     - `Microsoft.Extensions.Configuration`
   - `App.xaml.cs` の起動シーケンスを次のように変更（概念）:

```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) => {
        services.AddSingleton<IManagedActionManager, DefaultActionManager>();
        // 追加サービス登録
    })
    .Build();

// Application 起動前に host.Start() を呼び出す（必要に応じて）
App.ServiceProvider = host.Services;
```

3. サービス登録の整流化
   - すべての `new` による直接生成／グローバル参照を見直し、インジェクション可能にする。
   - `Scrutor` を用いた自動スキャンで `services.Scan(...)` を導入することでボイラープレートを減らす。
4. 小機能ずつ移行（1 機能 = 1 PR）
   - 優先順: Key send → Mouse send → Macro → Profile switch → Launch process
   - 各 PR の方針: まず `ActionManager.Dispatch...` を呼ぶ（既存フォールバックは残す）→ テスト → フォールバック削除 PR
5. テスト整備
   - ユニット: `xUnit` + `Moq` で `IManagedActionManager` と `VirtualKBMBase` をモック
   - 統合: Host を使った軽量統合テスト（Dispatch → Controller → MockOutputHandler）
6. CI と PR ガード
   - GitHub Actions: `dotnet restore` / `dotnet build` / `dotnet test`
   - PR テンプレートに回帰テスト手順と手動検証手順を必須化

---

## Key send ラップ（実践ガイド）
- 新規: `KeyOutputAction : IOutputAction` を作成し、`Execute(OutputContext)` / `Stop(OutputContext)` 内で `VirtualKBMBase` の既存メソッドを呼ぶ。
- Mapping の書き換え:
  - 既存の `outputKBMHandler.PerformKeyPress...` 箇所を `ActionManager.DispatchTriggerEstablished(action, device, logical, native, useScan, outputKBMHandler)` 経由に切り替える。
  - まずは `handled == true` の場合に直接呼び出しを抑止するフォールバック方式を採用。
- テスト: `KeyOutputAction` が `VirtualKBMBase` の該当メソッドを呼ぶユニットテストを作成。

---

## CI / 開発フロー（推奨）
- ブランチ: `main`（安定） / `integration`（実機検証） / `feature/*`（小粒）
- PR: 1 機能 1 PR、ユニットテスト必須、手動検証手順を PR に記載
- ローカル検証コマンド:

```bash
dotnet restore
dotnet build ./DS4Windows/DS4WinWPF.csproj -c Debug
dotnet test ./DS4WindowsTests/DS4WindowsTests.csproj --no-build
```

---

## 注意点とリスク対策
- マクロやキーリピートの微妙なタイミング差は既存 `outputKBMHandler` のロジックを `KeyOutputAction` にそのまま移植して回避。
- Updater / UI の `Process.Start` 系は仕様上直接呼ぶケースが合理的なため、優先度低として別途方針を決定。

---

## 付録: 推奨 NuGet 一覧
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration
- Scrutor
- xunit / Moq / coverlet.collector

---

保存場所: `g:/Cursor_Folder/DS4Windows-Vader4Pro/docs/DI-Implementation-Guide.md`

このガイドに基づき最初の実作業（`Host` 化 or `KeyOutputAction` PR）をどちらから着手しますか？
