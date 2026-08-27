# [作業計画書] フェーズ2 Step 2-3: DI登録 (AppHost.cs / ServiceRegistration.cs)

## 1. 目的と背景
DIコンテナ（`AppHost` / `ServiceRegistration`）に `IVirtualKBM` の実装として `OutputKBMHandlerAdapter` を `Singleton` 登録し、DI経由で仮想KBM出力機能を利用可能にする。

## 2. 作業スコープ
- `DS4Windows/DI/AppHost.cs` (Host構築・名前空間整合)
- `DS4Windows/DI/ServiceRegistration.cs` (`IVirtualKBM` の Singleton 登録)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-3-Plan.md` (本計画書)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-3-Report.md` (報告書)
- `docs-forDIMG/MadeByAgent/Phase2-Status.md` (ダッシュボード更新)

## 3. 完了条件
- [x] `ServiceRegistration.cs` に `IVirtualKBM` が Singleton 登録されていること。
- [x] 名前空間の依存関係（`DS4Windows.DI`, `DS4Windows.Services`, `DS4WinWPF.DI`）が正しく解決されていること。
- [x] プロジェクト全体のビルド（`dotnet publish`）が成功すること。
