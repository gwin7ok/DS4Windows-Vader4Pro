# フェーズ2 進捗状況ダッシュボード (KBM出力の抽象化: IVirtualKBM)

## 1. 概要
- **目的**: `Mapping.cs` および `Actions/` 配下で直接 `Global.outputKBMHandler` 等を呼び出している仮想キーボード・マウス（KBM）操作を `IVirtualKBM` インターフェース経由に抽象化し、DIによる疎結合化とテスタビリティ向上を図る。
- **開始日**: 2026-08-28
- **現在のステータス**: Step 2-4 進行中 (Step 2-4-A 適用中)

---

## 2. ステップ別進捗一覧

| ステップ | 概要 | 状態 | 成果物 / 対象ファイル |
| :--- | :--- | :---: | :--- |
| **Step 2-1** | `IVirtualKBM` インターフェース設計 | **完了** | `IVirtualKBM.cs`<br>`Phase2-Step2-1-Plan.md`<br>`Phase2-Step2-1-Report.md` |
| **Step 2-2** | `VirtualKBMBase` への適用・アダプタ新設 | **完了** | `VirtualKBMBase.cs` (`: IVirtualKBM`)<br>`OutputKBMHandlerAdapter.cs`<br>`Phase2-Step2-2-Plan.md`<br>`Phase2-Step2-2-Report.md` |
| **Step 2-3** | DI登録 (`AppHost` / `ServiceRegistration`) | **完了** | `AppHost.cs`<br>`ServiceRegistration.cs`<br>`Phase2-Step2-3-Plan.md`<br>`Phase2-Step2-3-Report.md` |
| **Step 2-4** | 呼び出し箇所置換 (Actions + マクロ14箇所) | **進行中** | `MouseOutputAction.cs`<br>`Phase2-Step2-4-Plan.md`<br>`Mapping.cs` (マクロ部) |
| **Step 2-5** | 通常1:1マッピング (48箇所) の置換 | 未着手 | `Mapping.cs` (※影響大のため独立検証・PR分割) |
| **Step 2-6** | 単体テスト整備・結合検証 | 未着手 | `DS4Windows.Tests/Services/VirtualKBMTests.cs` |

---

## 3. 各ステップの詳細状況

### Step 2-4: 呼び出し箇所置換 (Actions + マクロ14箇所) 【進行中】
- **Step 2-4-A (Actions配下)**: `MouseOutputAction` へ `IVirtualKBM` 注入・委譲処理を追加。
- **Step 2-4-B (マクロ14箇所)**: 次回 `Mapping.cs` 内のマクロ呼び出し箇所を置換予定。
