# Phase7-Step9 完了報告書: 互換 `AppHost` の削除、`Mapping.cs` の `Global` 参照の再配置、K3

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step9-Plan.md`（Q1〜Q5 はすべて推奨どおり承認）  
状態: 実装完了。W1〜W3 のビルド・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。いずれも 1 回で成功）。W4 は文書のみ。実機確認 U-19 は未（ユーザー判断で先送り）。

---

## 1. 結果の要約

- 互換 `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`）を削除した。呼び出し元は 0 件だった。正本は `DS4WinWPF.AppHost` だけになった。
- `Mapping.cs` の `Global` 参照 41 件（定数 18、`Clamp` 23）と、状態の保持クラス 3 つの定数参照（各 1）を、新設した静的クラス `ControllerLimits`（定数 5 つ）と `NumericUtil`（`Clamp`）へ切り替えた。`Global` の同名の定数は別名、`Global.Clamp` は委譲するシムとして残した。
- K3（`macroDispatchInFlight`）のロックと確保を、`MacroRuntimeState.TryBeginDispatch`／`EndDispatch` に包んだ（挙動は同一）。`Mapping` はフラグの配列もロックも持たない。恒久の再設計は Phase8 へ持ち越し。
- 数値: `Global.*` 399 → **355**、`AppHost.GetService|Services` 5 → **4**、`Program.rootHub` 0、`ServiceProviderHolder` 17（変更なし）。Phase7 開始時（733）からの累計 −378。
- ロジックの変更なし。既存の不具合の発見・是正なし。

## 2. Wave ごとの実績

- W1: 互換 `AppHost` の削除。ラチェットの基準値の更新。`Step12DeadShimRemovalGuardTests` の検査を、ファイルが無い前提に替えた（ファイルが無いと検査が黙って省略される形だったため）。ガード `Phase7Step9W1CompatAppHostRemovedGuardTests`。ビルド・テストは 1 回で成功。
- W2: `ControllerLimits`／`NumericUtil` の新設。`Mapping.cs` の 41 件と保持クラス 3 つの 3 件をスクリプトで切り替え（コメント行は除外）。ラチェット 4 ファイルを 0 に。ガード `Phase7Step9W2NoGlobalInMappingGuardTests`（定数の値、`Global` 別名の一致、`Clamp` の境界値・NaN・min > max が旧実装と同一、4 ファイルに `Global.` が無いこと）。ビルド・テストは 1 回で成功。
- W3: `MacroRuntimeState` に 2 メソッドを追加し、`DispatchLock` を非公開に。`Mapping` の 2 箇所を切り替え、キャッシュフィールド 2 つを削除。`Phase7Step7MacroRuntimeStateGuardTests` の 2 検査を更新。ガード `Phase7Step9W3MacroDispatchGuardTests`。ビルド・テストは 1 回で成功。
- W4: 台帳 §12、モデル図（03、04）、チェックリスト U-19、`Phase7-Status.md`、本報告書。

## 3. 計画との差と、調査で判明した事項

- 計画どおりに実装した。差は次のとおり。
  - **ラチェットの合計は 354 ではなく 355**。計画書は 41＋3 を 45 と数え違えた（正しくは 44）。計画書を訂正した。
  - **`NumericUtil.Clamp` に `AggressiveInlining` を付けた**。旧 `Global.Clamp` は付けていなかったが、入力 1 回ごとに 23 回呼ばれる経路のため、呼び出しが 1 段増えても旧と同等以上になるようにした。本体は 1 文字も変えていない。割り当てゼロのテスト（`MappingHotPathAllocationTests`）は成功。
  - **`TryBeginDispatch` の範囲外の添字**: 旧実装は、ロックの中で配列を読んで例外になり、呼び出し元の `try { } catch { }` が握りつぶしていた。新実装も同じ位置（ロックの中）で `IndexOutOfRangeException` が呼び出し元へ伝わり、`PlayMacro` の `catch` が握りつぶす。テストで固定した。
  - `PlayMacroTask` の `finally` の後始末も、`try { } catch { }` の中で `EndDispatch` を呼ぶ形に保った（例外が後始末を壊さない）。
- 互換 `AppHost` の呼び出し元が 0 件だったことは、Step8 の予想どおり。`App.xaml.cs` の `AppHost.GetService` は `DS4WinWPF` 名前空間のため、互換版ではなく正本に束縛されていた。

## 4. 起動順序・スレッド・ガードレールへの影響

- 5.4（On-Demand パス評価）、5.2（適用時 Halt）、5.6（切断時クリーンアップ）: 触れていない。
- スレッド: K3 のロックは同じ（1 つのオブジェクトで保護）。ロックの対象が `Mapping` の複製参照から `MacroRuntimeState` 自身の private フィールドに替わっただけで、範囲・順序は同じ。`DispatchInFlight` の配列の参照は共有のまま（テストが読み取る）。
- 起動: 互換 `AppHost` の削除は、コンパイル時にのみ影響する（実行時の呼び出しは無かった）。

## 5. 未完了・持ち越し

- **実機確認 U-19**（チェックリスト §11）: スティック・トリガーの感度とデッドゾーン、Controls タブのマクロ（単発、連打、ホールドの二重実行防止）、スペシャルアクションのマクロ。U-11〜U-18 も未（U-13 は先送り）。
- Step10 へ: `Global` の定数の利用箇所（約 250 件。`BackingStore` 135 が最大）と `Global.Clamp` の残り 10 件の再配置、別名・シムの削除。静的な純粋関数の置き場所（Q4＝`Mapping` に残した）。`AppHost` の暗黙構築と `CreationNote`（Q5＝触らなかった）。`BackingStore` のコンストラクター注入と永続化の切り出し。`Mapping` の依存 11 個の整理。モデル図の最終更新。Phase7 の総合検証と完了報告。
- Phase8 へ: K3 の恒久対応（トリガー判定層とマクロ実行層の分離、実行層の一本化）、静的な `SyntheticDispatcher`、`Actions/` の `ServiceProviderHolder` 17。

## 6. 次の Step

- **Step10**（`Phase7-Plan.md` §2）: 仕上げ。調査と `Phase7-Step10-Plan.md` の作成から着手する（ユーザーの指示を待つ）。
