# Phase 1: SpecialAction 判定・実行の分離 - 最新進捗状況 (2026-08-27 更新)

## 1. 全体進捗サマリー

| ステップ | 担当アクション / 項目 | 状態 | 完了日 | 備考 |
| :--- | :--- | :---: | :---: | :--- |
| **A** | Direct Callsites インベントリ作成 & テスト基盤 | **完了** | 2026-08-26 | `Direct-Callsites-Inventory.md`, `MockManagedActionManager.cs` 作成済 |
| **B** | `Mapping.cs` の DispatchTrigger 厳密化 | **完了** | 2026-08-26 | `Mapping.cs` の `DispatchInputEdge` / `DispatchOrSetBeingTriggered` を実装, フォールバック保持 |
| **C1** | Key send 系 (`KeyOutputAction`) | **完了** | 2026-08-26 | `KeyOutputAction.cs` 既存利用, 配線確認済 |
| **C2** | Mouse / Move 系 (`MouseOutputAction`) | **完了** | 2026-08-26 | `MouseOutputAction.cs` 新規作成 |
| **C3** | Macro 系 (`MacroAction` / `DefaultMacroPlayer`) | **完了** | 2026-08-27 | `IMacroPlayer`, `DefaultMacroPlayer`, `MacroAction`, `MacroActionAdapter`, `ActionFactory` 配線, `Mapping.cs` 置換, 単体テスト(T1〜T5)通過 |
| **C4** | Profile 切替 (`ProfileSwitchAction`) | **完了** | 2026-08-27 | `IProfileSwitcher`, `DefaultProfileSwitcher`, `ProfileSwitchAction`, `ProfileSwitchActionAdapter`, `ActionFactory` 配線, `Mapping.cs` 置換, 単体テスト(T1〜T5)通過 |
| **C5** | Launch program (`LaunchProcessAction`) | **完了** | 2026-08-27 | 4引数対応, Adapter新設, `Mapping.cs`置換, `MockProcessLauncher`改修, 単体テスト(T1〜T6)実装・ビルド成功 |
| **D** | フォールバック削除と整流化 | **未着手** | - | 全アクション移行完了に伴い着手可能 |
| **E** | ドキュメントとロールアウト | **未着手** | - | 最終成果物の整理 |

---

## 2. 直近の完了作業詳細

### 2.1 C4: Profile 切替系アクションの全面移行と単体テスト完了 (2026-08-27)
1. **`IProfileSwitcher` インターフェースの新設**:
   * プロファイル切り替えおよび一時プロファイルからの復帰を抽象化する `IProfileSwitcher.cs` を作成。
2. **`DefaultProfileSwitcher` の実装 & `Mapping.cs` 委譲エントリーポイント追加**:
   * `Mapping.cs` に `ApplyProfileDirect` / `RestoreProfileDirect` を新設し、`HaltReportingRunAction` やトースト通知等の既存ロジックを完全維持（No Feature Drop）。
3. **`ProfileSwitchAction` & `ProfileSwitchActionAdapter` の新設**:
   * `IOutputAction` および `Action` 基底クラスを実装し、`ServiceProviderHolder` による DI 解決とフォールバックを両立。
4. **`ActionFactory` / `DefaultActionFactory` への配線**:
   * `SpecialAction.ActionTypeId.Profile` 判定時に `ProfileSwitchActionAdapter` を生成するように配線。
5. **`Mapping.cs` のピンポイント置換**:
   * `MapCustomAction` 内の `SpecialAction.ActionTypeId.Profile` 処理を `DispatchInputEdge` 経由に置換し、二重実行防止の `handled` フラグ捕捉を実装。
6. **単体テストの実装とパス (`DS4WindowsTests`)**:
   * `MockProfileSwitcher.cs` および `ProfileSwitchActionTests.cs` (T1〜T5) を作成し、テストプロジェクトのビルドおよび全テストパスを確認。

---

## 3. 残存タスクと優先順位

1. **Step D: 移行用フォールバックの削除と整流化 (最優先)**
   * 全アクション（C1〜C5）の動作安定確認後、不要となった旧直接呼び出しロジックの段階的整理・削除。
2. **Step E: Phase 1 完了レビュー & 文書化**
   * Phase 1 の最終レビュー、移行ドキュメントの更新、Phase 2（KBM出力抽象化）への引き継ぎ準備。

---

## 4. 参照ドキュメント

* `docs-forDIMG/DI-App-Wide-Migration-Plan.md` (全体移行計画)
* `.github/copilot-instructions.md` (移行作業ガイドライン)
* `docs-forDIMG/MadeByAgent/Direct-Callsites-Inventory.md` (呼び出し箇所インベントリ)
* `docs-forDIMG/MadeByAgent/C5-LaunchProcessAction-Implementation.md` (C5 実装記録)
* `docs-forDIMG/MadeByAgent/C3-MacroAction-Design.md` (C3 設計書)
* `docs-forDIMG/MadeByAgent/C3-MacroAction-Implementation.md` (C3 実装記録)
* `docs-forDIMG/MadeByAgent/C4-ProfileSwitchAction-Design.md` (C4 設計書)
* `docs-forDIMG/MadeByAgent/C4-ProfileSwitchAction-Implementation.md` (C4 実装記録)