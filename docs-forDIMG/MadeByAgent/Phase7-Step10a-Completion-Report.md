# Phase7-Step10a 完了報告書: `Global` の定数・資源パス・`Clamp` の再配置

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step10a-Plan.md`（Q1〜Q5 はすべて推奨どおり承認）。上位: `Phase7-Step10-Plan.md`  
状態: 実装完了。W1〜W4 のビルド・テスト実行は成功（ユーザーが各 Wave で実行。W2〜W4 は各 1 回の再実行）。W5 は文書のみ。実機確認は 10a 単独の項目なし（10e の総合検証に含める）。

---

## 1. 結果の要約

- `Global` の定数 5 つ、資源パスの定数 2 つ、`Clamp` を削除し、すべての利用箇所（本体・テスト）を `ControllerLimits`／`ResourcePaths`／`NumericUtil` に切り替えた。値と計算は変えていない。
- 数値: `Global.*` 355 → **107**。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計 −626。
- `BackingStore.cs` の `Global` 参照は 159 → 27（定数 132 件を切り替え）。
- 機能・ログの変更なし。既存の不具合の発見・是正なし。

## 2. Wave ごとの実績

- W1: `BackingStore.cs` の定数 132 件（スクリプトで置換。コメント行は除外）。ラチェット 159 → 27。ビルド・テスト 1 回で成功。
- W2: 制御層・DTO・サービス・Actions・画面の 15 ファイルの定数参照。`ControlService`／`ProfileSettingsService` の再宣言を `ControllerLimits` から取る形に。ガード `Phase7Step10aW2ControllerLimitsGuardTests`。再実行 1 回（§3）。
- W3: `ResourcePaths` に定数 2 つを追加。画面側 11 ファイルの 74 件を置換。`Global.iconChoiceResources` は一時的に参照（TODO）。ガード `Phase7Step10aW3ResourcePrefixGuardTests`。再実行 1 回（§3）。
- W4: `Clamp` の残り 9 件、テスト 約 40 件の置換、`Global` の別名・シムの削除。`Step12DeadShimRemovalGuardTests` の名簿を移動（`ProtectedInGlobal` → `RemovedFromGlobal`）。ラチェット更新。再実行 1 回（§3）。
- W5: 台帳 §13、モデル図（03、04）、`Phase7-Status.md`、計画書の数値訂正、本報告書。

## 3. 計画との差と、調査で判明した事項

- **目標値は 100 ではなく 107**。計画書の「355 → 100」は見積もりの数え違いで、実測は 107（Step10 計画書の「約 107」が正しかった）。計画書を訂正した。
- 再実行の原因（いずれもテストの期待値・テストのパスの問題で、本体の回帰ではない）:
  - W2: 件数を固定していた 2 つのガード（`AppPostHostGlobalReferenceGuardTests` の `CleanShutdown` の期待 `MAX_DS4_CONTROLLER_COUNT×1`、`ControlServiceGlobalReferenceGuardTests` の許容リスト 2 件）を、定数が `ControllerLimits` に替わった結果に合わせて 0 件に更新。
  - W3: 新しいガードの探索パスが 1 階層ずれていた（`DS4Windows` の層を足し忘れ）。
  - W4: `MouseGlobalReferenceGuardTests` が `Global.Clamp` の参照 4 件を期待していたため、0 件に更新。
- 教訓: 件数を固定するガードは、ラチェットの基準値以外にも複数ある（`AppPostHost`、`ControlService`、`Mouse` など）。次回の機械的な置換の前に、`Assert.Equal(<件数>, ...)` を含む参照ガードを先に洗い出す。
- `OutputSlotManager.cs` と `AutoProfileEntity.cs` の参照は、完全修飾（`DS4Windows.Global.X`）だったため、ラチェットの数え方（`(?<![\w.])Global\.`）の対象外だった。切り替えは行った。
- `Global.cs` のトレイアイコンの表（`iconChoiceResources`）は、下位層から上位層（画面側の `ResourcePaths`）への一時的な参照になった。10b で画面側へ移す（TODO を `Global.cs` に記載）。

## 4. 起動順序・スレッド・ガードレールへの影響

- なし。定数は `const` から `const` への置換で、コンパイル後の値は同じ。`Clamp` の呼び出し先を替えただけで、ホットパスの割り当ては増えない（`MappingHotPathAllocationTests` は成功）。

## 5. 未完了・持ち越し

- 10b: 表と文字列変換（`getX360ControlString`、`macroDS4Values`、`iconChoiceResources`、`defaultButtonMapping`、`reverseX360ButtonMapping`、名前表）の再配置、契約の再判断（`ProfileSubSettingBase.OnSubPropertyChanged`、`ProfileApplicationService` の未使用の依存、`UsingMaxControllers`、`OutputSlotChanged`）、孤立定数 8 件、`MainWindow` の引数 18 個。
- 10c〜10e: `Phase7-Step10-Plan.md` §3。`using static DS4Windows.Global;`（`Mapping.cs`、`DS4LightBar.cs`）は 10d で削除。
- 実機確認 U-11〜U-19: 先送り（10e の総合検証）。10e の確認項目に、トレイアイコンとプロファイル編集の画像の表示を加える。

## 6. 次の Step

- **10b**: 調査と `Phase7-Step10b-Plan.md` の作成から着手する（ユーザーの指示を待つ）。
