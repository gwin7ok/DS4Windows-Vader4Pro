# Phase7-Step6 計画書: 出力デバイス種別の所有権の移動

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Plan.md` §2（Step6）、`Phase7-Residual-Ledger.md`  
状態: 実装完了（Q1〜Q5 はすべて推奨どおり承認。W1・W2 のビルド・テスト成功、W3 は文書。実機確認 U-16 は未）。結果は `Phase7-Step6-Completion-Report.md`。

---

## 1. 調査結果

### 1.1 対象の配列（`Global` の静的配列）

- `Global.outDevTypeTemp`（9 要素、初期値は全部 X360）: プロファイル編集画面で選んだ、未確定の出力種別（UI 一時状態）。
- `Global.activeOutDevType`（9 要素、初期値は全部 None）: スロットごとの、現在仮想バスに接続中の種別（実行時接続状態）。
- どちらも `IOutputSlotService.OutDevTypeTemp`／`ActiveOutDevType` が、同じ配列の参照を返しているだけである（`OutputSlotService.cs:175,179`。Phase6-Step5 決定2＝案X の暫定）。状態の複製はない。
- 配列の参照は再代入されない（要素の書き込みだけ）。`ControlService` は参照を 1 回だけキャッシュする（`ControlService.cs:120-124`）。この前提は移管後も守る。

### 1.2 参照の実数（本体）

- `Global.activeOutDevType` の直接参照: `BackingStore.cs` の **9 箇所**（1965、5943、6002、6037、6118、6233、6282、6388 ほか。いずれもプロファイル適用時に `oldContType` として読む）。コメントアウト済みの 3 件は対象外。
- `IOutputSlotService.ActiveOutDevType` 経由: `ControlService`（10 箇所、キャッシュ経由の読み書き）、`MainWindow`（UDP のプロパティ取得 1 箇所）。
- `IOutputSlotService.OutDevTypeTemp` 経由: `ProfileSettingsViewModel`（3 箇所）、`BindingWindowViewModel`（1）、`PressKeyViewModel`（1）。
- `Global.outDevTypeTemp` を直接読み書きする本体のコードは **0 件**（`OutputSlotService` のみ）。
- 以前の計画（`Phase7-Plan.md`）の「約 45 参照」には、`outputKBMHandler`（約 35）が含まれていた。配列そのものは、本体で約 25 箇所、テストで約 23 箇所。

### 1.3 移行前（上流）のコードとの比較

- 上流（`a871e750`「Added new field array for specifying active controller types」）から、`Global` の静的配列で、**要素の読み書きだけを行い、ロックはない**。書き込みは `ControlService` の制御スレッド／UI スレッド、読み取りは UI スレッドと入力スレッド。この挙動は DI 移行前も同じで、不具合ではない。
- 移管後も、ロックは追加しない（機能を変えない）。配列の参照は再代入しない。

### 1.4 `BackingStore` の事情

- `BackingStore` は `Global` が生成する（コンテナの外）。`IProcessInspector` と同じく、**プロパティ注入**で渡すのが最小である（Step5 W4 と同じ手法）。
- 起動の最初（コンテナ構築前）にも、`Load` から `oldContType` を読む経路がある。このときは全部 None（接続前）であり、既定の None 配列を持たせれば挙動は同じになる。書き込みは `ControlService` だけなので、既定の配列が書き換えられることはない。

### 1.5 `outputKBMHandler`／`outputKBMMapping`（持ち越しの確認）

- `Global.outputKBMHandler` は静的なフィールドで、`OutputKBMHandlerAdapter`（約 20 件）、`OutputKBMHandlerLifecycle`（7）、`SyntheticDispatcher`（4）、`Mapping.cs`（フォールバック 1）から使われる。
- `SyntheticDispatcher` と `Mapping` は静的クラスで、引数でハンドラを受け取れない箇所がある。ここを注入に直すには `Mapping` の instance 化（Step8）が前提になる。Step6 では扱わない（Q4）。

### 1.6 `OutputSlotChanged` イベント

- 購読者は本体に 0 件（Phase7-Step2 W6 で温存。「Step6 で再判断」の TODO つき）。UI（出力スロット管理画面）の更新が、`OutputSlotManager` 側のイベントで足りているかは未確認（Q5）。

---

## 2. 決定が必要な事項

### Q1: 配列の持ち主

- A（推奨）: `OutputSlotService` の readonly なインスタンスフィールドが持つ。`OutDevTypeTemp`／`ActiveOutDevType` はそれを返す。`Global` の配列は削除する。`OutputSlotService` は Singleton なので、プロセス内で 1 つの配列になる（従来の静的配列と同じ）。
- B: 専用の状態クラス（1 型 1 ファイル）を新設して、`OutputSlotService` と `BackingStore` が共有する。型が増えるが、`BackingStore` が `IOutputSlotService` に依存しなくて済む。
- C: 現状維持（Step10 まで `Global`）。

### Q2: `BackingStore` への渡し方

- A（推奨）: `BackingStore.ActiveOutDevType`（プロパティ。既定は None の配列）を新設し、`ServiceRegistration` の `BackingStore` の登録が `IOutputSlotService.ActiveOutDevType` を設定する（Step5 W4 の `ProcessInspector` と同じ）。Step10 の `BackingStore` 分割で、コンストラクター注入へ。
- B: `ApplyProfile` などの引数で渡す（呼び出し元の変更が多い）。

### Q3: `Global` 側の扱い

- A（推奨）: `Global.outDevTypeTemp`／`activeOutDevType` を削除する（本体の直接参照は `BackingStore` の 9 件だけで、Q2 で解消するため。互換のシムは不要）。テストは `IOutputSlotService` 経由へ書き換える。
- B: 転送プロパティとして温存する（Step10 まで）。

### Q4: `outputKBMHandler`／`outputKBMMapping` の扱い

- A（推奨）: Step6 では扱わない。Step7〜9（`Mapping` の instance 化）で、`SyntheticDispatcher`／`Mapping` の整理とあわせて移す。台帳に持ち越しとして記録する。
- B: Step6 で `OutputKBMHandlerLifecycle` へ実体を移す（静的クラスから読む箇所に、結局 `Global` かロケーターが残る）。

### Q5: `OutputSlotChanged` イベント

- A（推奨）: 温存し、TODO の対象を「Step10」へ更新する（UI の更新経路の実機確認なしに削除しない）。
- B: Step6 で削除する（購読者 0 件。ただし画面更新が足りているかの確認を、実機に頼る）。

---

## 3. 実装の Wave（推奨案の場合）

- W1: 所有権の移動。`OutputSlotService` のインスタンスフィールド化、`BackingStore.ActiveOutDevType`（プロパティ＋既定値）、`ServiceRegistration` での設定、`BackingStore` の 9 箇所の置き換え。ビルド・テストの確認。
- W2: `Global.outDevTypeTemp`／`activeOutDevType` の削除。テスト（`OutputSlotServiceTests`、`PatternCViewModelTests`、`ProfileSettingsViewModelTargetDeviceTests`、`ProfileEditorGlobalReferenceGuardTests`、`Step10FourTwoGlobalReferenceGuardTests`）の更新。新しい回帰ガード `Phase7Step6OutputTypeOwnershipGuardTests`（配列の参照が安定、`Global` に配列がない、`BackingStore` が配列を `Global` から読まない、`ControlService` のキャッシュが `OutputSlotService` と同一）。ラチェット基準値の更新（`BackingStore.cs` 166 → 157 を想定。実測で確定）。
- W3: 台帳 §9、チェックリスト U-16、モデル図 03／04、完了報告書、`Phase7-Status.md`。

## 4. リスクと確認

- 起動直後の `BackingStore` の既定配列から、コンテナ構築後の `OutputSlotService` の配列への切り替え。接続前は全部 None なので、結果は同じ（§1.4）。
- 配列の参照の安定（`ControlService` のキャッシュ）。ガードテストで固定する。
- 実機確認（U-16）: 起動、コントローラー接続、プロファイル切替で出力種別（X360 ⇔ DS4）を変える、プロファイル編集画面で出力種別を変えてキャンセル、再接続。スロット 5〜8 は確認不能のまま（U-2）。

## 5. 範囲外（持ち越し）

- `outputKBMHandler`／`outputKBMMapping`（Q4。Step7〜9）。
- `BackingStore` のコンストラクター注入（Step10）。
- `Mapping` の Locator、`Actions/` の Locator（Step7〜9、Phase8）。
