# C3: MacroAction 移行設計書 (Macro系アクションのDI分離)

**作成日**: 2026-08-27  
**ステータス**: 設計完了 / レビュー待ち  
**対象領域**: `SpecialAction.ActionTypeId.Macro` / `Mapping.PlayMacro` 系

---

## 1. 概要と目的

`Mapping.cs` 内に直接実装されているマクロ再生ロジック（`PlayMacro`, `PlayMacroTask`, `PlayMacroCodeValue`, `EndMacro` 等）は、非同期タスク管理・タイマー待機・キー解放・Win32 API（`InputMethods`）呼び出しが混在しており、密結合かつ副作用の温床となっています。

本タスク（C3）では、No Feature Drop（機能完全維持）原則を厳格に遵守しつつ、マクロ再生の責務を **`IMacroPlayer` / `MacroAction`** へ分離・抽象化し、DI 経由で実行可能な構造へ移行します。

---

## 2. 現状分析 (As-Is)

### 2.1 現行の実行フロー (`Mapping.cs`)
1. **トリガー判定**: `MapCustomAction` (L5309〜) にて `SpecialAction.ActionTypeId.Macro` を検出。
2. **実行開始**: `PlayMacro(device, action, ...)` (L6448) が呼び出される。
3. **非同期タスク生成**: `PlayMacroTask` (L6490) 内で `Task.Run` を起動し、`CancellationToken` および `macroPlaying[device]` フラグでライフサイクルを管理。
4. **ステップ逐次実行**:
   * キー押下/解放: `InputMethods.performSCKeyPress`, `InputMethods.performSCKeyRelease`
   * マウス操作: `InputMethods.MouseEvent`, `InputMethods.MouseWheel`
   * 待機: `Task.Delay` またはスピンウェイト
   * リピートモード判定: `action.macroRepeat`, `action.macroHold`
5. **終了/中断**: `EndMacro(device)` (L6863) にて押下中キーの強制解放とタスクキャンセルを実行。

### 2.2 現行の課題
* `Mapping.cs` 内部で静的配列（`macroPlaying[device]` 等）やスレッドを直接管理しているため、テスタビリティが皆無。
* `InputMethods` への直接依存があり、低レベル出力のモック化が不可能。

---

## 3. 目標設計 (To-Be)

[ Mapping.cs / Trigger層 ] │ (OnTrigger / Execute) ▼ [ MacroActionAdapter ] ───►
[ MacroAction (IOutputAction) ] │ ▼ (DI解決: ServiceProviderHolder) [ IMacroPlayer
] │ ┌─────────────┴─────────────┐ ▼ ▼ [ DefaultMacroPlayer ] [ MockMacroPlayer ]
(テスト用) │ ▼ [ InputMethods / KBM ]


### 3.1 新設・改修するコンポーネント

1. **`IMacroPlayer` (新規インターフェース: `DS4Windows/Actions/IMacroPlayer.cs`)**
   * マクロシーケンスの再生・停止・状態取得を抽象化。
   ```csharp
   public interface IMacroPlayer
   {
       bool IsPlaying(int deviceIndex);
       void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default);
       void Stop(int deviceIndex);
   }

2.  DefaultMacroPlayer (新規サービス: DS4Windows/Actions/DefaultMacroPlayer.cs)

      - 既存 Mapping.cs の PlayMacroTask / PlayMacroCodeValue / EndMacro
        ロジックを機能欠落なく移植。
      - DI コンテナ（AppHost）にシングルトンとして登録。

3.  MacroAction (新規アクションクラス: DS4Windows/Actions/MacroAction.cs / IOutputAction
    実装)

      - IOutputAction を実装し、ServiceProviderHolder.Provider から IMacroPlayer
        を取得して再生を実行。

4.  MacroActionAdapter (新規アダプタークラス: DS4Windows/Actions/MacroActionAdapter.cs /
    IActionAdapter 実装)

      - ActionFactory / DefaultActionFactory から生成され、Mapping.cs からのアクション実行要求を中継。

5. No Feature Drop (機能完全維持) チェックリスト

移行にあたり、以下のエッジケース・特殊処理を一切省略せず移植します：

- [ ] リピート動作モード:
    - macroRepeat = true（トリガー解除までリピート）
    - macroHold = true（トリガー維持中のみキーを押し続ける挙動）
- [ ] 正確なディレイ制御:
    - ミリ秒精度のウェイト処理および 0ms 時のスキップ処理
- [ ] 中断時の安全解放 (Safe Cleanup):
    - 途中でマクロがキャンセルされた場合、押下状態のまま残ったすべてのキー/マウスボタンを確実に解放する処理（EndMacro の挙動）
- [ ] デバイスごとの排他制御:
    - 各コントローラー番号（0〜3）ごとの独立したマクロ実行状態管理

6. 段階的移行ステップ (マイクロ・ステップ計画)

  - Step 1: IMacroPlayer.cs の新設
  - Step 2: DefaultMacroPlayer.cs の実装（既存ロジックの完全移植）
  - Step 3: MacroAction.cs および MacroActionAdapter.cs の作成
  - Step 4: ActionFactory.cs / DefaultActionFactory.cs への Macro 型配線
  - Step 5: Mapping.cs のディスパッチ呼び出し箇所のピンポイント置換（フォールバック保持）
  - Step 6: 単体テスト（MockMacroPlayer および MacroActionTests）の実装とビルド検証

7. 完了基準

  - DS4Windows.Actions.Tests において MacroAction の単体テストがすべて合格すること。
  - Mapping.cs 内の SpecialAction.ActionTypeId.Macro 処理が handled
    フラグで二重実行なくディスパッチされること。
  - 既存のマクロ（リピート、キーストローク、ウェイト）が実機/テストで同等に動作すること。

