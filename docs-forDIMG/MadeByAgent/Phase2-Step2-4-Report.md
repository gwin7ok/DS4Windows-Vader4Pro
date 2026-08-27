# [実装報告書] フェーズ2 Step 2-4: 呼び出し箇所の置換 (Actions配下 + マクロ実行14箇所)

## 1. 実施概要
Actions サブシステムおよび `Mapping.cs` マクロ再生処理の `IVirtualKBM` 置換を完了した。

1. **Step 2-4-A (Actions サブシステム)**:
   - `MouseOutputAction.cs` に `IVirtualKBM` コンストラクタ注入および DI 解決を追加。
   - `KeyOutputAction.cs`、`DefaultMacroPlayer.cs` との連携を整合。
2. **Step 2-4-B (マクロ実行 14 箇所)**:
   - `Mapping.cs` に `VirtualKBM` プロパティを導入。
   - `PlayMacroTask`、`EndMacro` 周辺の `Global.outputKBMHandler` 呼び出し（計14箇所）を `VirtualKBM`（＝`IVirtualKBM`）経由に安全に置換。

## 2. 成果物
- `DS4Windows/Actions/MouseOutputAction.cs`
- `DS4Windows/DS4Control/Mapping.cs`
- `docs-forDIMG/MadeByAgent/Phase2-Step2-4-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase2-Step2-4-Report.md`
- `docs-forDIMG/MadeByAgent/Phase2-Status.md`

## 3. 次のステップ
- **Step 2-5**: 通常の1:1マッピング処理（48箇所）の置換（※影響範囲が広いため独立ステップ・慎重に適用）
