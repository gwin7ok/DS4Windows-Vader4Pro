# C4: ProfileSwitchAction 移行設計書 (Profile切替系アクションのDI分離)

**作成日**: 2026-08-27  
**最終更新日**: 2026-08-27  
**ステータス**: 実装・テスト完了  
**対象領域**: `SpecialAction.ActionTypeId.Profile` / `Global.ApplyProfile` 系

---

## 1. 概要と目的

`Mapping.cs` 内に直接実装されていたプロファイル切り替え処理（`Global.ApplyProfile`, `HaltReportingRunAction`, `LoadProfile`, `LoadTempProfile` 等）は、UI 通知・静的グローバル参照・デバイスループ停止処理が強く結合していました。

本タスク（C4）では、No Feature Drop（機能完全維持）原則を厳格に遵守しつつ、プロファイル切り替えの責務を **`IProfileSwitcher` / `ProfileSwitchAction`** へ分離・抽象化し、DI 経由で実行可能な構造へ移行を完了しました。

---

## 2. 現状分析 (As-Is) と安全な委譲設計

### 2.1 現行の実行フロー (`Mapping.cs`)
1. **トリガー判定**: `MapCustomAction` (L5309〜) にて `SpecialAction.ActionTypeId.Profile` を検出。
2. **Untrigger 状態保持**: 一時プロファイル切り替えの場合、`deviceRuntime[device].UntriggerAction` に現在のプロファイル名を記録。
3. **入力キー安全解放**: 押しっぱなし防止のため、トリガーキーに割り当てられているキー/マクロを解放。
4. **非同期プロファイル適用**: `Task.Run` 内で `d.HaltReportingRunAction` を呼び出し、`Global.ApplyProfile` でプロファイルを適用。
5. **後続アクション連動**: 新プロファイル側で同一トリガーを持つアクションの状態を同期。
6. **Untrigger 復帰**: `MapCustomAction` 末尾の Untrigger 処理にて、条件成立時に `LoadProfile` / `LoadTempProfile` で元のプロファイルに復帰。

### 2.2 採用した委譲方式
既存の `Global.ApplyProfile` および `HaltReportingRunAction` の精密な排他・通知制御を壊さないよう、`Mapping.cs` に `ApplyProfileDirect` / `RestoreProfileDirect` の委譲エントリーポイントを用意し、`DefaultProfileSwitcher` から安全に呼び出す構造を採用（No Feature Drop）。

---

## 3. 目標設計 (To-Be)

```
[ Mapping.cs / Trigger層 ]
       │ (OnTrigger / Execute)
       ▼
[ ProfileSwitchActionAdapter ] ───► [ ProfileSwitchAction (IOutputAction) ]
                                            │
                                            ▼ (DI解決: ServiceProviderHolder)
                                    [ IProfileSwitcher ]
                                            │
                              ┌─────────────┴─────────────┐
                              ▼                           ▼
                    [ DefaultProfileSwitcher ]  [ MockProfileSwitcher ] (テスト用)
                              │
                              ▼
                    [ Mapping.ApplyProfileDirect ]
```

### 3.1 コンポーネント構成

1. **`IProfileSwitcher` (`DS4Windows/Actions/IProfileSwitcher.cs`)** 【完了】
   * プロファイル切り替え・復帰の抽象インターフェース。
2. **`DefaultProfileSwitcher` (`DS4Windows/Actions/DefaultProfileSwitcher.cs`)** 【完了】
   * `Mapping.ApplyProfileDirect` / `RestoreProfileDirect` を呼び出す標準実装。
3. **`ProfileSwitchAction` (`DS4Windows/Actions/ProfileSwitchAction.cs`)** 【完了】
   * `IOutputAction` を実装し、`ServiceProviderHolder` 経由で `IProfileSwitcher` を取得して実行/復帰。
4. **`ProfileSwitchActionAdapter` (`DS4Windows/Actions/ProfileSwitchActionAdapter.cs`)** 【完了】
   * `Action` 基底クラスを実装し、トリガー・リリースイベントを `ProfileSwitchAction` へ中継。
5. **`MockProfileSwitcher` (`DS4WindowsTests/MockProfileSwitcher.cs`)** 【完了】
   * 単体テスト用の呼び出し履歴記録モック。
6. **`ProfileSwitchActionTests` (`DS4WindowsTests/ProfileSwitchActionTests.cs`)** 【完了】
   * T1〜T5 の単体テストスイート。

---

## 4. No Feature Drop (機能完全維持) チェックリスト

移行にあたり、以下のエッジケース・特殊処理を一切省略せず維持：

- [x] **一時プロファイルと通常プロファイルの判別**:
  - `useTempProfile` および `prevProfileName` の管理を完全維持。
- [x] **安全なデバイス入力停止**:
  - `d.HaltReportingRunAction` によるレポートスレッド排他制御を維持。
- [x] **トースト通知・ログ出力**:
  - `Global.ProfileChangedNotification` および `AppLogger.LogProfileChanged` の出力を維持。
- [x] **Untrigger（復帰）条件の制御**:
  - `automaticUntrigger` / `uTrigger` による元のプロファイルへの自動復帰処理を維持。
- [x] **同一トリガーキーのアクション同期**:
  - プロファイル切り替え直後に同一コントロールを持つアクションの状態引き継ぎを維持。

---

## 5. 段階的移行ステップと進捗

- [x] **Step 1**: `IProfileSwitcher.cs` の新設
- [x] **Step 2**: `Mapping.cs` 委譲エントリーポイント追加 & `DefaultProfileSwitcher.cs` 実装・ビルド確認
- [x] **Step 3**: `ProfileSwitchAction.cs` および `ProfileSwitchActionAdapter.cs` の作成
- [x] **Step 4**: `ActionFactory.cs` / `DefaultActionFactory.cs` への `Profile` 型配線
- [x] **Step 5**: `Mapping.cs` のディスパッチ呼び出し箇所のピンポイント置換（フォールバック保持）
- [x] **Step 6**: 単体テスト（`MockProfileSwitcher` および `ProfileSwitchActionTests`）の実装とビルド・実行検証

---

## 6. 完了基準
* [x] `DS4Windows.Actions.Tests` において `ProfileSwitchAction` の単体テスト（T1〜T5）がすべて合格すること。
* [x] `Mapping.cs` 内の `SpecialAction.ActionTypeId.Profile` 処理が `handled` フラグで二重実行なくディスパッチされること。
* [x] プロファイル切り替え（一時プロファイル、Untrigger復帰、通知）が実機/テストで正常に動作すること。