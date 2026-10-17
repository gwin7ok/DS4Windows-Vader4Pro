# Phase7-Step6 完了報告書: 出力デバイス種別の所有権の移動

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step6-Plan.md`（Q1〜Q5 はすべて推奨どおり承認）  
状態: 実装完了。W1（再ビルド 2 回）・W2 のビルド・テスト実行は成功（ユーザーが実行）。W3 は文書のみ。実機確認 U-16 は未。

---

## 1. 結果の要約

- `Global.outDevTypeTemp`（編集画面の未確定の出力種別）と `Global.activeOutDevType`（スロットごとの接続中の種別）を削除し、実体を `OutputSlotService` の readonly フィールドへ移した。
- `BackingStore` は、自身の `ActiveOutDevType`（プロパティ。既定は全スロット None）を読む。`IOutputSlotService` の登録が、`OutputSlotService` の生成時に同じ配列を設定する。
- `Global.*`: 446 → **438**（`BackingStore.cs` 166 → 159、`OutputSlotService.cs` 2 → 1）。Phase7 開始時（733）からの累計は −295。`AppHost.GetService` 11、`rootHub` 0、`ServiceProviderHolder` 22 は変更なし。
- 挙動は変えていない（要素の読み書きだけ、ロックなし、配列の参照は再代入しない）。既存の不具合の是正はなし。

## 2. Wave ごとの実績

- W1: `OutputSlotService` の配列の所有、`BackingStore.ActiveOutDevType`、`BackingStore` の読み取り 8 件（計画書の「9」は数え違い）の置き換え、関連テスト 4 本の書き換え。
- W2: `Global` の 2 配列の削除、コメントの更新、`Phase7Step6OutputTypeOwnershipGuardTests`（4 本）、ラチェットの更新。
- W3: 台帳 §9、チェックリスト U-16、モデル図 03／04、本報告書、`Phase7-Status.md`。

## 3. 調査で判明した重要事項

- **循環によるテストホストのクラッシュ（W1）**: 最初の実装は、`BackingStore` の登録のラムダの中で `IOutputSlotService` を解決していた。`IOutputSlotStore` → `IPathService`（`PathService` のコンストラクターが `BackingStore` を受け取る）→ `BackingStore` と循環し、ラムダ内の解決は循環の検出をすり抜けて、実行時にスタックオーバーフローになった。受け渡しを `IOutputSlotService` の登録側（生成時に `BackingStore` へ設定）へ移して解消した。配列を書き込むのは `ControlService` だけで、`IOutputSlotService` を通すため、書き込みの前に必ず接続済みになる。ガードテストで固定した。
- ガードの許可リスト（`Step10FourTwoGlobalReferenceGuardTests`）は、配列名を許可していた。移管後は、配列の長さの定数（`TEST_PROFILE_ITEM_COUNT`）の許可に更新した。
- `OutputSlotService` の初期化で `Global.TEST_PROFILE_ITEM_COUNT`（定数）を使うため、ラチェットの `Global` の参照が 1 件残る（定数の再配置は Step10）。

## 4. 設計上の判断

- Q1＝A（`OutputSlotService` が持つ）、Q2＝A（`BackingStore` にプロパティ注入）、Q3＝A（`Global` の配列を削除。転送は残さない）、Q4＝A（`outputKBMHandler` は Step7〜9）、Q5＝A（`OutputSlotChanged` は温存。TODO の再判断先を Step10 へ）。
- 同じ名前のサービスを複数作ると、配列は別になる（従来は静的な配列で共有されていた）。本体ではコンテナの Singleton だけなので、プロセス内で 1 つになる。テストでは、サービスごとに独立した配列になり、テスト間の干渉が減った（`StaticStateScope` の復元処理を削除）。

## 5. 未完了・持ち越し

- **実機確認 U-16**（チェックリスト §11）: 出力種別の入れ替え（X360 ⇔ DS4）、編集画面のキャンセル、切断・再接続、`activeoutdevtype` の取得。U-11〜U-15 も未。
- Step7〜9 へ: `outputKBMHandler`／`outputKBMMapping`（`OutputKBMHandlerAdapter`、`OutputKBMHandlerLifecycle`、`SyntheticDispatcher`、`Mapping` のフォールバック）、`Mapping` の `AppHost` 6・`ServiceProviderHolder` 5、`Global.ProfileSettingsServiceInstance`、互換 `AppHost`、`CreationNote`。
- Phase8 へ: `Actions/` の `ServiceProviderHolder` 17。
- Step10 へ: `BackingStore.ActiveOutDevType` のコンストラクター注入、`OutputSlotChanged` の再判断、`TEST_PROFILE_ITEM_COUNT` の再配置、`Global` の窓口 4 つ、`MainWindow` の引数の見直し。

## 6. 次の Step

- **Step7**: `Mapping` の特性化テスト（入力 → 出力の固定）と、静的な状態の保持クラスへの切り出し、`BackingStore` → `Mapping` の逆参照の解消（`Phase7-Plan.md` §2）。調査と `Phase7-Step7-Plan.md` の作成から着手する。Step7〜9 は最大のリスク（ホットパス、ゼロアロケーション、確認不能の U-2・U-5・U-6）を持つ。
