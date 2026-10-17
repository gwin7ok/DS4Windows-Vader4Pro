# Phase7-Step7 完了報告書: `Mapping` の特性化テストと、静的な状態の切り出し

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step7-Plan.md`（Q1〜Q6 はすべて推奨どおり承認）  
状態: 実装完了。W1〜W4 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。W3・W4 は各 1〜2 回の再ビルド）。W5 は文書のみ。実機確認 U-17 は未。

---

## 1. 結果の要約

- `Mapping` の入力 → 出力を固定する特性化テストを追加した（`MapCustom` 7 本、`Commit`／`MapClick`／`getBool*Mapping`／`HandleDeviceDisconnect` 6 本）。Step8 で構造を変える前の安全網になる。
- `Mapping` の静的な状態のうち、外から触られるものを 3 つの保持クラス（`SyntheticInputState`、`MacroRuntimeState`、`StickMotionState`）へ移した。`Mapping` の同名メンバーは、保持クラスへの転送だけ（状態は持たない）。
- 外から使われていた入れ子の型 7 つ（`SyntheticState`、`PostMapStickData`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`、`DeltaSettingsProcessorGroup`、`Click`）を、1 ファイル 1 型のトップレベルへ出した（中身は変えていない）。
- `BackingStore` から `Mapping` への逆参照を 0 にした（`ActionsLoaded` イベント＋購読者 `ActionsLoadedRuntimeReset`。順序とログは従来どおり）。
- `Global.*`: 438 → **432**（`Mapping.cs` 52 → 43、新ファイル 3 つが各 1）。累計（733 から）−301。`AppHost.GetService` 11、`rootHub` 0、`ServiceProviderHolder` 22 は変更なし。
- 計算のロジックは変えていない。既存の不具合の是正はなし。

## 2. Wave ごとの実績

- W1: 特性化テスト。本体は変更なし。共通の土台 `MappingCharacterizationScope`（専用のデバイス添字 5、状態と `Global.outputKBMHandler` の差し替えと復元）と、記録用のスタブ `RecordingVirtualKbmBase`。ビルド・テストは 1 回で成功。
- W2: `SyntheticState` のトップレベル化、`SyntheticInputState`、`ActionsLoaded` イベント、`ActionsLoadedRuntimeReset`、`SyntheticDispatcher` の更新。ガード `Phase7Step7SyntheticInputStateGuardTests`。
- W3: `MacroRuntimeState`。`PlayMacro` の二重実行防止のガード（`MappingPlayMacroDispatchGuardTests`）の参照先を更新。ガード `Phase7Step7MacroRuntimeStateGuardTests`。再ビルド 1 回（`using` の不足）。
- W4: `StickMotionState`、入れ子の型 6 つのトップレベル化、`Mouse`／`ControlService` の更新。ガード `Phase7Step7StickMotionStateGuardTests`。再テスト 2 回（`Mouse.cs` の文字列を期待する `MouseTapClickHoldTests`、`Commit` の範囲の終端に `public enum Click` を使っていた `MappingCommitAndCalibrationGlobalReferenceGuardTests`）。
- W5: 台帳 §10、チェックリスト U-17、モデル図 03／04、本報告書、`Phase7-Status.md`。

## 3. 設計上の判断と、計画との差

- 計画（Q1〜Q6）どおりに実装した。
- **ボタン→キーの特性化テストは範囲外にした**。計画（Q1＝A）では「ボタン→キー（合成状態のカウント）」を含めていたが、実際の経路は `ActionManager`（静的な登録簿＝Phase8 の対象）経由で、合成状態のカウントでは観測できない。`ActionManager` の挙動を固定するのは Phase8 の範囲のため、Step7 では扱わず、実機確認 U-17 に含めた。ホイールも、`Commit` が実時間（150ms）で連続送出するため、フレーク防止で範囲外にした。
- **`ControlService` と `Mouse` は、保持クラスを注入ではなく `Shared`（暫定の静的な窓口）から得る**。計画（Q3＝A）は「コンテナ内のクラスは注入で受ける」だったが、`ControlService` のコンストラクターを変えると多数のテストの構築コードの改修になるため、Step8（`Mapping` の instance 化で `ControlService` の依存も整理する）で行う。保持クラスは DI の Singleton としても登録済みで、同じ instance を指す。
- `Mapping` 内部だけで使う状態（`stickValueHistory`、`lastStickAxisValues`、`outSqrStk`、`lastWheelGyroValues` ほか）は、計画どおり Step8 まで `Mapping` に残した。
- `Mapping` の転送は、`Mapping` の内部のコード（`deviceState` だけで 118 箇所）を書き換えないための措置。型の初期化時に参照を 1 度だけ保持するので、ホットパスの割り当ては増えない（`MappingHotPathAllocationTests` は成功）。

## 4. 調査で判明した重要事項

- **`MapCustom` の出力の形**: `outputfieldMapping`（デバイスごとの静的な作業領域）に書き、最後に `PopulateState` で出力の状態へ写す。ボタンの再割り当ては、元のボタンを `ResetToDefaultValue` で消して、標準の対応表（`reverseX360ButtonMapping`）の相手のボタンを立てる。テストはこの挙動を固定している。
- **`Commit` の差分方式**: 各ティックで `MapClick` が数えた回数と、前のティックの値の差で、押下／解放を送る。押したままのときは毎ティック `MapClick` が呼ばれる。ホイールだけは実時間で連続送出する。
- **スクリプトの落とし穴（作業手順）**: bash ツールのコマンド文字列中の `\\` は 1 つの `\` に潰れる。W2 のガードテストの正規表現 `\\b` が、制御文字（バックスペース）になって何にも一致せず、検査が無効になっていた。W3 で発見・修正した。以後、バックスラッシュを含む編集は Edit ツールか、ファイルに書いたスクリプトで行う。
- 文字列を期待する既存のテストは、型の移動で期待が変わる（W4 で 2 件）。型や定数の移動の前に、その名前を含む文字列のテストを検索する。

## 5. 未完了・持ち越し

- **実機確認 U-17**（チェックリスト §11）: 通常入力、キー・マウスの割り当て、ジャイロ、タッチ、フリックスティック、マクロ、スペシャルアクションの再読み込み、切断。U-11〜U-16 も未。
- Step8 へ: `Mapping` の instance 化、`profileSettings`／`profileApplication`／`VirtualKBM` の束縛、`AppHost` 6・`ServiceProviderHolder` 5 の解消、`Global.outputKBMHandler`／`outputKBMMapping`（Q6＝A）、保持クラスの `Shared` の削除とコンストラクター注入、`ControlService`／`Mouse`／`SyntheticDispatcher`／`Actions/*` の切り替え、`Mapping` 内部の状態の instance フィールド化。
- Step9 へ: 互換 `AppHost`、`Mapping.cs` の `Global` 参照 43（定数・`Clamp`）の再配置、K3（`macroDispatchInFlight` の再設計）。
- Phase8 へ: `Actions/` の `ServiceProviderHolder` 17、`ActionManager` の静的な呼び出し、ボタン→キーの経路の特性化。
- Step10 へ: `BackingStore` のコンストラクター注入ほか。

## 6. 次の Step

- **Step8: `Mapping` の instance 化**（`Phase7-Plan.md` §2）。DI のシングルトン＋抽象の窓口、呼び出し元の切り替え、静的な `profileSettings` 束縛の解消、`outputKBMHandler` の整理、保持クラスの `Shared` 削除。ホットパス・ゼロアロケーション・マクロの別スレッドがかかるため、調査と `Phase7-Step8-Plan.md` の作成から着手する。
