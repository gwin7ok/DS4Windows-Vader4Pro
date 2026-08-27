# [作業計画書] フェーズ2 Step 2-3: DI登録 (AppHost.cs)

## 1. 目的と背景
`AppHost.cs` に `IVirtualKBM` の実装として `OutputKBMHandlerAdapter` を `Singleton` 登録し、DIコンテナ経由で仮想KBM出力機能を取得可能にする。

## 2. 作業スコープ
- `DS4Windows/DI/AppHost.cs` (DI登録追加)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-3-Plan.md` (計画書)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-3-Report.md` (報告書)
- `docs-forDIMG/MadeByAgent/Phase2-Status.md` (ダッシュボード更新)

## 3. 完了条件
- `AppHost.Services.GetService<IVirtualKBM>()` から `OutputKBMHandlerAdapter` のインスタンスが解決できること。
- ビルドエラーが発生しないこと。
