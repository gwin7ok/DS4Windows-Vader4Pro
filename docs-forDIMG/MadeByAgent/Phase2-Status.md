# フェーズ2 進捗状況ダッシュボード (KBM出力の抽象化: IVirtualKBM)

## 1. 概要
- **目的**: `Mapping.cs` および `Actions/` 配下で直接 `Global.outputKBMHandler` 等を呼び出している仮想キーボード・マウス（KBM）操作を `IVirtualKBM` インターフェース経由に抽象化し、DIによる疎結合化とテスタビリティ向上を図る。
- **開始日**: 2026-08-28
- **現在のステータス**: Step 2-5 完了 (ビルド成功確認済み) -> Step 2-6 着手

---

## 2. ステップ別進捗一覧

| ステップ | 概要 | 状態 | 成果物 / 対象ファイル |
| :--- | :--- | :---: | :--- |
| **Step 2-1** | `IVirtualKBM` インターフェース設計 | **完了** | `IVirtualKBM.cs`<br>`Phase2-Step2-1-Plan.md`<br>`Phase2-Step2-1-Report.md` |
| **Step 2-2** | `VirtualKBMBase` への適用・アダプタ新設 | **完了** | `VirtualKBMBase.cs` (`: IVirtualKBM`)<br>`OutputKBMHandlerAdapter.cs`<br>`Phase2-Step2-2-Plan.md`<br>`Phase2-Step2-2-Report.md` |
| **Step 2-3** | DI登録 (`AppHost` / `ServiceRegistration`) | **完了** | `AppHost.cs`<br>`ServiceRegistration.cs`<br>`Phase2-Step2-3-Plan.md`<br>`Phase2-Step2-3-Report.md` |
| **Step 2-4** | 呼び出し箇所置換 (Actions + マクロ14箇所) | **完了** | `MouseOutputAction.cs`<br>`Mapping.cs` (マクロ部)<br>`Phase2-Step2-4-Plan.md`<br>`Phase2-Step2-4-Report.md` |
| **Step 2-5** | 通常1:1マッピング (48箇所) の置換 | **完了** | `Mapping.cs` (全マッピング部)<br>`Phase2-Step2-5-Plan.md`<br>`Phase2-Step2-5-Report.md` |
| **Step 2-6** | 単体テスト整備・結合検証 | **進行中** | `MockVirtualKBM.cs`<br>`VirtualKBMTests.cs`<br>`Phase2-Step2-6-Plan.md` |

---

## 3. 各ステップの詳細状況

### Step 2-5: 通常1:1マッピング (48箇所) の置換 【完了】
- **実施内容**:
  - `Mapping.cs`、`ActionManager.cs`、`KeyButtonActionController.cs`、`RepeatHelper.cs` 等の全出力呼び出しおよび引数型を `IVirtualKBM` に統一。
  - Release ビルド（`dotnet publish`）の完全成功を確認。

### Step 2-6: 単体テスト整備・結合検証 【進行中】
- **予定内容**:
  - `MockVirtualKBM` および `VirtualKBMTests.cs` の作成、テスト実行パス検証、完了報告書作成。
