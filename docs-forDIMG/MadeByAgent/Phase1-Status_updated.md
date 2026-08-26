# Phase 1 進捗記録（2026-08-17 時点）

作成: Agent（DI移行作業用ブランチ）
参照元: docs/DI-Migration-Plan.md, docs/Direct-Callsites-Inventory.md, .github/copilot-instructions.md §2.1修正版

## 完了ステップ

### A — インベントリ & テスト基盤
- `docs/Direct-Callsites-Inventory.md` 作成完了（PerformKeyPress, PerformMouseButtonEvent, PlayMacro, Global.ApplyProfile, Process.Start の呼び出し箇所をインベントリ化）
- `DS4WindowsTests/MockManagedActionManager.cs` 完成（`IManagedActionManager` の全メンバー実装済み）

### B — Mapping.cs の DispatchTrigger 厳密化（フォールバック残存）
- `Mapping.cs` の `DispatchInputEdge` / `DispatchOrSetBeingTriggered` 実装済み
- `!handled` 時に `SetBeingTriggeredIf` を呼ぶフォールバックを維持（§2.1修正版に準拠：古い方式を削除せず、新しい方式の動作確認が取れるまで残す）
- 同時に複数の実装経路を持たない（1機能 = 1経路）

### C1 — Key send 系（KeyOutputAction）
- `DS4Windows/Actions/KeyOutputAction.cs` 既存確認（`IOutputAction` 実装、`Execute` / `Stop` で `TriggerContextImpl` + `KeyActionBinding` + `ActionManager.GetOrCreateControllerForAction` を使用）
- `KeyActionBinding` 経由で `OutputContext` を渡す設計が既に存在
- 追加の変更は不要（完了と判断）

## 未完了ステップ

### C2 — Mouse / Move 系（MouseOutputAction 集約）
- `Mapping.cs` の `PerformMouseButtonEvent` / `PerformMouseMoveEvent` 等を `MouseOutputAction` に集約する必要あり
- `MouseOutputAction` クラスの存在確認と `IOutputAction` 実装の確認が次のタスク

### C3 — Macro 系（MacroAction / MacroController）
- `PlayMacro` / `PlayMacroTask` の既存ロジックを `MacroController` に移す設計草案が必要
- 並列・非同期性があるため慎重にテスト（リスク高）

### C4 — Profile 切替（ProfileSwitchAction）
- `Global.ApplyProfile` の呼び出し元をラップして `ActionManager.Dispatch...` を呼ぶ
- `ProfileSwitchAction` の新規作成が必要

### C5 — 外部プロセス起動（LaunchProcessAction）
- `specActionLaunchProc` の抽象化（`LaunchProcessAction`）実装完了（2026-08-26、Claude対応）
- `DefaultActionFactory`/`ActionFactory` に `Program` 型のマッピングを追加（未配線だったTODOを解消）
- `LaunchProcessActionAdapter` 新規作成（`IOutputAction` → `SpecialActionBase` 橋渡し）
- `Mapping.cs` L5508-5569 をピンポイント置換（`handled` 捕捉により二重起動を防止、フォールバックは完全保持）
- 詳細は `docs-forDIMG/MadeByAgent/C5-LaunchProcessAction-Implementation.md` を参照
- **未実施**: `dotnet build` による実機ビルド確認（環境制約）、単体テストT1〜T6の追加
- `Process.Start` は仕様的に置換不可な場合があるため優先度低 → ①（SpecialAction起動）は対応完了。②⑥は引き続きフェーズ3スコープ

### D — フォールバック削除と整流化
- 実行条件: `handled == true` が各機能（Key, Mouse, Macro, Profile, Launch）で確実に返ることをテストで確認後
- 現在はフォールバック（`!handled` → `SetBeingTriggeredIf` / 直接 `outputKBMHandler` 呼び出し）を維持中
- §2.1修正版に従い、削除は別 PR（1機能 = 1 PR）で段階的に実施

### E — ドキュメントとロールアウト
- `docs/Direct-Callsites-Inventory.md` の更新（置換完了後の呼び出し箇所を反映）
- `docs/DI-Migration-Plan.md` のステップ完了マーク
- リリース候補ブランチでの実機テスト（特にマクロ・複合トグル）

## 現在のコード状態（要点）

- `AppHost.cs`: `Host.CreateDefaultBuilder()` で DI コンテナ構築（`App.xaml.cs` から呼び出し確認済み）
- `ServiceRegistration.cs`: `IProfileSettingsService` 登録済み（`ProfileSettingsServicePlaceholder` 実装）
- `Mapping.cs`: `DispatchInputEdge` / `DispatchOrSetBeingTriggered` 実装済み、フォールバック残存
- `KeyOutputAction.cs`: 既存（変更不要）
- `MockManagedActionManager.cs`: 完成
- `DS4WinWPF.csproj`: `Microsoft.Extensions.Hosting` 8.0.0, `Configuration` 8.0.0 追加済み

## C2 調査結果（2026-08-17 追加）
- `MouseOutputAction` はドキュメント上の設計名（`docs/OutputAction-Feature-Inventory.md` で要件定義のみ）。実ファイルはまだ存在しない。
- `Mapping.cs` の Mouse 系呼び出し箇所は `docs/Direct-Callsites-Inventory.md` にインベントリ済み（L1272〜L1377、L6648〜L6708 等）。
- `VirtualKBMBase` の `PerformMouseButtonEvent` / `PerformMouseButtonEventAlt` は既存（`FakerInputHandler.cs`, `SendInputHandler.cs`）。
- C2 は `MouseOutputAction` の新規作成（`KeyOutputAction` と同様のパターン）が必要。現在は未着手（次PR対象）。
- 2026-08-26: `MouseOutputAction.cs` 新規作成完了（`IOutputAction` 実装、`Execute`/`Stop` で `AppLogger.LogTrace` 維持、フォールバックは `Mapping.cs` 側に残存 — §2.1修正版準拠）。ビルド成功（`dotnet build` 通過）。

## 次の推奨アクション

1. `MouseOutputAction` の存在確認と `Mapping.cs` の Mouse 系呼び出し箇所のインベントリ再確認（C2 開始）
2. または `MacroController` 設計草案の作成（C3 開始）
3. または `Mapping.cs` の `handled == true` 安定性をテストで検証し、D の準備を進める

## 制約（§2.1修正版）

- 古い方式（直接 `outputKBMHandler` 呼び出し等）は削除せず残す（新しい DI 経路の動作確認が取れるまで）
- 新しい機能に複数の候補手段を同時に実装しない（1機能 = 1経路）
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）
