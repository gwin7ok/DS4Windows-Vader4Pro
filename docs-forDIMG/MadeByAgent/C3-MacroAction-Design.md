# C3: MacroAction 移行設計書 (Macro系アクションのDI分離)

**作成日**: 2026-08-27  
**最終更新日**: 2026-08-27  
**ステータス**: 実装・テスト完了  
**対象領域**: `SpecialAction.ActionTypeId.Macro` / `Mapping.PlayMacro` 系

---

## 1. 概要と目的

`Mapping.cs` 内に直接実装されているマクロ再生ロジック（`PlayMacro`, `PlayMacroTask`, `PlayMacroCodeValue`, `EndMacro` 等）は、非同期タスク管理・タイマー待機・キー解放・Win32 API（`InputMethods`）呼び出しが混在しており、密結合かつ副作用の温床となっていました。

本タスク（C3）では、No Feature Drop（機能完全維持）原則を厳格に遵守しつつ、マクロ再生の責務を **`IMacroPlayer` / `MacroAction`** へ分離・抽象化し、DI 経由で実行可能な構造へ移行を完了しました。

---

## 2. 現状分析 (As-Is) と安全な委譲設計

### 2.1 現行の実行フロー (`Mapping.cs`)
1. **トリガー判定**: `MapCustomAction` (L5309〜) にて `SpecialAction.ActionTypeId.Macro` を検出。
2. **実行開始**: `PlayMacro(device, ...)` (L6466〜) が呼び出される。
3. **非同期タスク生成**: `PlayMacroTask` (L6508〜) 内で `Task.Run` を起動し、`CancellationToken` およびキー解放を管理。
4. **終了/中断**: `EndMacro(device, ...)` (L6881〜) にて押下中キーの強制解放とタスクキャンセルを実行。

### 2.2 採用した委譲方式
既存の 800 行を超える実績あるマクロ処理を複製せず、`Mapping.cs` に `internal static void PlayMacroDirect` / `EndMacroDirect` を新設し、`DefaultMacroPlayer` から安全に呼び出す構造を採用。これによりエッジケースや機能の欠落を完全に防止（No Feature Drop）。

---

## 3. 目標設計 (To-Be)

[ Mapping.cs / Trigger層 ]
│ (OnTrigger / Execute)
▼
[ MacroActionAdapter ] ───► [ MacroAction (IOutputAction) ]
│
▼ (DI解決: ServiceProviderHolder)
[ IMacroPlayer ]
│
┌─────────────┴─────────────┐
▼ ▼
[ DefaultMacroPlayer ] [ MockMacroPlayer ] (テスト用)
│
▼
[ Mapping.PlayMacroDirect ]


### 3.1 コンポーネント構成

1. **`IMacroPlayer` (`DS4Windows/Actions/IMacroPlayer.cs`)** 【完了】
   * マクロ再生・停止・状態取得の抽象インターフェース。
2. **`DefaultMacroPlayer` (`DS4Windows/Actions/DefaultMacroPlayer.cs`)** 【完了】
   * `Mapping.PlayMacroDirect` / `EndMacroDirect` を呼び出す標準実装。
3. **`MacroAction` (`DS4Windows/Actions/MacroAction.cs`)** 【完了】
   * `IOutputAction` を実装し、`ServiceProviderHolder` 経由で `IMacroPlayer` を取得して再生/停止。
4. **`MacroActionAdapter` (`DS4Windows/Actions/MacroActionAdapter.cs`)** 【完了】
   * `Action` 基底クラスを実装し、トリガーイベントを `MacroAction` へ中継。
5. **`MockMacroPlayer` (`DS4WindowsTests/MockMacroPlayer.cs`)** 【完了】
   * 単体テスト用の呼び出し履歴記録モック。
6. **`MacroActionTests` (`DS4WindowsTests/MacroActionTests.cs`)** 【完了】
   * T1〜T5 の単体テストスイート。

---

## 4. No Feature Drop (機能完全維持) チェックリスト

移行にあたり、以下のエッジケース・特殊処理を一切省略せず維持：

- [x] **リピート動作モード**:
  - `SpecialAction` に定義されたリピート・ホールド設定を `Mapping.PlayMacroDirect` 経由でそのまま維持。
- [x] **正確なディレイ制御**:
  - 既存 `PlayMacroTask` のミリ秒精度ウェイト処理を完全維持。
- [x] **中断時の安全解放 (Safe Cleanup)**:
  - 途中でマクロがキャンセルされた場合、押下状態のまま残ったすべてのキー/マウスボタンを確実に解放する処理（`EndMacro` の挙動）を維持。
- [x] **デバイスごとの排他制御**:
  - 各コントローラー番号（0〜3）ごとの独立したマクロ実行状態管理。

---

## 5. 段階的移行ステップと進捗

- [x] **Step 1**: `IMacroPlayer.cs` の新設
- [x] **Step 2**: `Mapping.cs` 委譲エントリーポイント追加 & `DefaultMacroPlayer.cs` 実装・ビルド確認
- [x] **Step 3**: `MacroAction.cs` および `MacroActionAdapter.cs` の作成
- [x] **Step 4**: `ActionFactory.cs` / `DefaultActionFactory.cs` への `Macro` 型配線
- [x] **Step 5**: `Mapping.cs` のディスパッチ呼び出し箇所のピンポイント置換（フォールバック保持）
- [x] **Step 6**: 単体テスト（`MockMacroPlayer` および `MacroActionTests`）の実装とビルド・実行検証

---

## 6. 完了基準
* [x] `DS4Windows.Actions.Tests` において `MacroAction` の単体テスト（T1〜T5）がすべて合格すること。
* [x] `Mapping.cs` 内の `SpecialAction.ActionTypeId.Macro` 処理が `handled` フラグで二重実行なくディスパッチされること。
* [x] 既存のマクロ（リピート、キーストローク、ウェイト）が実機/テストで同等に動作すること。