# Phase 5 - Step 14: Phase 5 総合自動テストと実機検証計画策定 計画書

## 1. 概要と目的
本ドキュメントは、DS4Windows の DI 移行作業における **Phase 5 - Step 14（総合自動テストと実機検証）** の実施計画書である。
Step 1 〜 Step 13 までに構築されたドメイン 1〜4（永続化、アクション・マクロ、デバイス・スロット、UI層）の全改修成果について、自動テストによる回帰ゼロの確定と、実機コントローラーを用いた E2E 総合検証（実機CP4）の遂行手順を規定する。

---

## 2. タスク構成
1. **【Step 14-1】 Phase 5 総合自動テストの網羅実行（リグレッションゼロ確定）**
2. **【Step 14-2】 実機動作確認チェックリスト（実機CP4）の確定と環境準備**
3. **【Step 14-3】 実機総合動作検証（実機CP4）の実施**
4. **【Step 14-4】 Step 14 完了報告書および実機検証記録の作成**

---

## 3. 詳細タスク仕様

### タスク Step 14-1: 総合自動テストの網羅実行
* **目的**: Phase 5 の全実装（計 169 テスト）を網羅実行し、全自動テストがグリーンであることを確定する。
* **対象プロジェクト**:
  * `DS4WindowsTests/DS4Windows.Actions.Tests.csproj`（156 テスト）
  * `StandaloneTests/StandaloneTests.csproj`（13 テスト）
* **実行コマンド**:
  * `dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug /p:platform=x64 --no-build -v normal`
  * `dotnet test ./StandaloneTests/StandaloneTests.csproj -c Debug /p:platform=x64 --no-build -v normal`
* **合格基準**: 全 169 件のテストが 100% 成功すること。

### タスク Step 14-2: 実機動作確認チェックリストの確定
* **目的**: Step 13 での重大発見（`AutoProfileChecker` 二重実体化問題の是正）および Watchpoint 2（スレッド安全・イベント購読解除）の反映を確認したチェックリスト（`Phase5-Step14-RealDevice-Verification-Checklist.md`）の確定。

### タスク Step 14-3: 実機総合動作検証（実機CP4）の実施
* **検証重点項目**:
  1. **基本起動 ＆ UI 整合**: `App.rootHub` 撤去後のメイン画面操作、設定変更の保存。
  2. **AutoProfile 実動**: `_autoProfileService` 直結後、ゲーム・プロセス検知によるプロファイル自動切替が正常に発動すること。
  3. **スレッド安全性**: コントローラーのホットプラグ時（USB/BT）に WPF クロススレッド例外が発生しないこと。
  4. **メモリリーク・ゴースト発火抑止**: 最小化・トレイ格納・画面破棄時の Dispose 動作確認。
  5. **入力停止（Halt）保証**: プロファイル切替時のボタン押下残留防止。
  6. **ViGEm 仮想コントローラー動作**: 仮想 Xbox 360 / DS4 出力の正常性。

### タスク Step 14-4: 完了報告書および実機検証記録の作成
* `docs-forDIMG/MadeByAgent/Phase5-Step14-Completion-Report.md` の作成。
* `docs-forDIMG/MadeByAgent/Phase5-Status.md` の更新（Step 14 完了、Step 15 着手へ）。

---

## 4. 完了基準（Definition of Done）
1. 全自動テスト（169 件）が 100% グリーンであること。
2. 実機チェックリストに基づく検証が実施され、重大な不具合がゼロであること。
3. `Phase5-Step14-Completion-Report.md` が作成され、最終ステップ（Step 15: Shim 棚卸し・神クラス第一次ダウンサイズ）への移行準備が整うこと。