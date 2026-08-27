# [作業計画書] フェーズ2 Step 2-2: VirtualKBMBase へのインターフェース適用・アダプタ新設

## 1. 目的と背景
`VirtualKBMBase` 抽象クラスに `IVirtualKBM` を適用し、DIコンテナ経由で注入可能にする。また `Global.outputKBMHandler` の遅延初期化に対応するため `OutputKBMHandlerAdapter` を作成する。

## 2. 作業スコープ
- `DS4Windows/DS4Control/Services/IVirtualKBM.cs` (シグネチャ完全同期)
- `DS4Windows/DS4Control/OutputKBM/VirtualKBMBase.cs` (`IVirtualKBM` 実装)
- `DS4Windows/DS4Control/Services/OutputKBMHandlerAdapter.cs` (遅延委譲アダプタ新設)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-2-Plan.md` (計画書)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-2-Report.md` (報告書)

## 3. 完了条件
- `VirtualKBMBase` が `IVirtualKBM` を実装し、ビルドエラーが発生しないこと。
- `OutputKBMHandlerAdapter` が `Global.outputKBMHandler` への安全な委譲を行っていること。
