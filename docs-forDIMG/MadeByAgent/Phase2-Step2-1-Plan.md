# [作業計画書] フェーズ2 Step 2-1: IVirtualKBM インターフェース設計

## 1. 目的と背景
`DS4Windows` の信号変換層（`Mapping.cs`）および実行層（`Actions/` 配下、マクロ再生など）が直接利用している仮想キーボード・マウス操作を抽象化し、DIコンテナからの注入およびモック（テスト容易性向上）を可能にするため、`IVirtualKBM` インターフェースを定義・設計する。

既存の `VirtualKBMBase` 抽象クラスの公開APIを漏れなく網羅し、後続のステップ（Step 2-2: 実装・アダプタ整備、Step 2-3: DI登録）への確実な橋渡しを行う。

## 2. 作業スコープと対象ファイル
- `DS4Windows/DS4Control/Services/IVirtualKBM.cs` (新規作成)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-1-Plan.md` (本計画書)
- `docs-forDIMG/MadeByAgent/Phase2-Step2-1-Report.md` (作業報告書)

## 3. 完了条件
- `IVirtualKBM` インターフェースが定義され、`VirtualKBMBase` のすべての公開操作が網羅されていること。
