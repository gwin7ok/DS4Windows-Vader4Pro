# [作業計画書] フェーズ2 Step 2-4: 呼び出し箇所の置換 (Actions配下 + マクロ実行14箇所)

## 1. 目的と背景
Actions サブシステム（`KeyOutputAction`, `MouseOutputAction`, `MacroAction`）および `Mapping.cs` のマクロ実行処理（`PlayMacro`, `EndMacro`）において、静的メンバ `Global.outputKBMHandler` への直接依存を排除し、DI 登録された `IVirtualKBM` 経由の呼び出しへ切り替える。

## 2. 作業スコープと対象ファイル
- **Actions サブシステム (Step 2-4-A)**:
  - `DS4Windows/Actions/MouseOutputAction.cs` (`IVirtualKBM` の注入/利用に対応)
  - `DS4Windows/Actions/KeyOutputAction.cs` (`IVirtualKBM` との整合性確認)
  - `DS4Windows/Actions/DefaultMacroPlayer.cs`
- **Mapping.cs マクロ再生 (Step 2-4-B)**:
  - `DS4Windows/DS4Control/Mapping.cs` 内の 14 箇所の `outputKBMHandler` 呼び出し置換
- **ドキュメント**:
  - `docs-forDIMG/MadeByAgent/Phase2-Step2-4-Plan.md` (本計画書)
  - `docs-forDIMG/MadeByAgent/Phase2-Step2-4-Report.md` (報告書)
  - `docs-forDIMG/MadeByAgent/Phase2-Status.md` (進捗ダッシュボード更新)

## 3. 完了条件
- [ ] Actions サブシステムの出力アクションが `IVirtualKBM` を利用して動作すること。
- [ ] `Mapping.cs` 内のマクロ実行（14箇所）が `IVirtualKBM` 経由で実行されること。
- [ ] プロジェクトのビルドがエラーなく完了すること。
