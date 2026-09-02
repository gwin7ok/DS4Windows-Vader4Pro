# フェーズ5計画書: DIサービス内部 Legacy 経路監査と責務分離

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase4-Plan.md`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`

## 1. 位置づけ

Phase4 では DI 基盤、Composition Root、主要呼び出し元、ViewModel Factory を整備した。Phase5 では、DI サービスの内部実装が `Global`／`Program.rootHub`／Legacy 実体へ再委譲している経路を監査し、影響範囲と優先度を確定したうえで責務分離を進める。

DI インターフェース経由で呼ばれているだけでは、内部実体の DI 化完了とは判定しない。

## 2. Phase4 からの引継ぎ

- `ProfileRepository` 内部の `Global.LoadProfile`／`Global.SaveProfile`
- `ProfileApplicationService` 内部の `Global.ApplyProfile`／`Global.LoadProfile`／`Global.LoadTempProfile`
- `SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions`
- `PathService` 内部の `Global.appdatapath`
- `ProfileSettingsService` 内部の `Program.rootHub` 参照
- Save／Apply の操作ログ、保存成否、再適用成否、通知経路の統一
- CP4 前の自動テスト化、CP4 実機検証、Legacy shim 削除判断

監査の基準結果は `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md` に記録する。

## 3. 実施ステップ

### Phase5-Step1: 詳細監査と優先度付け

1. DI 登録済み全サービスの `Global`／`rootHub`／静的実体参照を棚卸しする。
2. 各参照を、設定データ、XML永続化、プロファイル適用、副作用、通知、パス、デバイス状態、KBMに分類する。
3. 既存動作、ログ、スレッド、イベント、失敗時挙動を固定する。
4. 移行単位、リスク、テスト方法、実機確認要否を決定する。

### Phase5-Step2: プロファイル XML 読込・保存

`ProfileRepository` が `Global.LoadProfile`／`Global.SaveProfile` に再委譲する構造を対象とする。XMLパース、設定値反映、保存を専用契約へ分離する。入力停止、出力デバイス操作、Action再構築は適用サービスへ混在させない。

ロード順、既定値、欠落設定、カルチャ、ファイルパス、例外、ログを維持する。

### Phase5-Step3: プロファイル適用・復帰

`ProfileApplicationService` を実体の所有者とし、通常／一時プロファイルのロード、デバイス停止・再開、状態更新、通知、Action再構築を責務別に整理する。

通常 GUI 切替、編集画面 Save／Apply、SpecialAction、AutoProfile のすべてが同じ適用契約を使用することを目標とする。二重ロード、`isTemp`、復帰スナップショット、通知回数を回帰検証する。

### Phase5-Step4: Save／Apply の操作結果と通知

保存成否と再適用成否を戻り値で扱い、操作単位の `[DI]` ログを追加する。再適用対象の判定を明示し、`ProfileChanged` 通知が通常切替と同じ経路で発生することを確認する。

通知設定による抑制と、ログ出力を分けて扱う。

### Phase5-Step5: SpecialAction 永続化

`SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions` を分離する。Actions XML の CRUD、一覧更新、runtime ActionManager の再構築を適切な境界に整理する。

### Phase5-Step6: 残存サービス境界

`PathService`、`ProfileSettingsService`、デバイス状態、KBMアダプターを Phase2／Phase3 の既存方針と重複しないよう整理する。共有 `BackingStore` を当面維持する場合は、その理由と終了条件を記録する。

### Phase5-Step7: 自動テストと実機検証

各ステップ完了時に Debug ビルド、Actions／Standalone テストを実行する。自動テストで代替できない HID、WPF、ドライバ、長時間安定性は実機確認へ残す。

### Phase5-Step8: Legacy shim の削除判断

全呼び出し元、内部再委譲、実機経路、フォールバック使用実績を確認した後、shim ごとに削除可否を判断する。削除は別変更として実施する。

## 4. 完了条件

- DI サービス内部の Legacy 再委譲が分類・記録されている。
- プロファイル読込・保存・適用・復帰の担当境界が明確である。
- 通常 GUI 切替と編集画面 Save／Apply が同じ適用契約を使用する。
- Save／Apply の保存結果、再適用結果、ログ、通知が検証可能である。
- 自動テストと必要な実機検証が完了している。
- shim の削除可否が根拠付きで判断されている。

## 5. 進行ルール

一度に複数の責務を移行しない。各ステップで既存機能、ログ、状態遷移を確認し、Legacy shim は新経路の動作確認前に削除しない。
