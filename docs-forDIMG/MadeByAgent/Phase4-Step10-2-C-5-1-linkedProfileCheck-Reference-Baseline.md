# フェーズ4-Step10-2-C-5-1 `linkedProfileCheck` 現状参照箇所固定

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `Phase4-Step10-2-C-Plan.md`

## 1. 固定した現状

`linkedProfileCheck` の保持データは `ProfileSettingsService` の `_linkedProfileCheck` に移行済みである。一方、呼び出し元の一部は `Global.linkedProfileCheck` 配列シムを使用している。

そのため、`Global.linkedProfileCheck[index]` の要素参照や要素代入でも、配列を返すGlobal getterが先に実行される。現在のgetterはLegacy Traceログを出力するため、接続、切断、プロファイル再構築など異なる操作で同じログが出力される。

## 2. 対象ファイルと参照箇所

| ファイル | 現在の参照内容 | 現状の依存 | C-5-1移行方針 |
|---|---|---|---|
| `DS4Windows/DS4Control/ControlService.cs` | 接続時のリンク状態設定で`Global.linkedProfileCheck[index]`を読み書き | `IProfileSettingsService`をコンストラクタ注入済み | `GetLinkedProfileCheck`／`SetLinkedProfileCheck`へ置換 |
| `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs` | UI getter/setter、リンク名更新、デバイス状態初期化で`Global.linkedProfileCheck[devIndex]`を参照 | `ControlService`のみコンストラクタ注入 | `IProfileSettingsService`を追加注入し、サービスAPIへ置換 |
| `DS4Windows/DS4Control/ScpUtil.cs` | `Global.linkedProfileCheck[i]`を保存処理と保存対象プロファイル参照で使用 | `ProfileSettingsServiceInstance`を保有 | Global配列getterをサービス直接参照へ置換 |

## 3. 呼び出し数の基準

現状ソースの対象文字列参照は次のとおりである。

- `ScpUtil.cs`: Globalシム宣言・getter/setterを含む定義、および内部参照2箇所
- `ControlService.cs`: 接続時の要素代入2箇所
- `ControllerListViewModel.cs`: 要素参照・代入7箇所
- 実際の呼び出し元は3ファイル。Globalシムの宣言とgetter/setter定義は移行後も互換境界として残す。

## 4. 維持する挙動

- LinkedProfiles.xmlに登録されたプロファイルの判定
- 接続時の`LinkedProfileUI`とリンク状態の設定
- Controller一覧のリンク状態表示・変更
- リンク有効時のLinkedProfiles.xml保存
- プロファイル保存時にリンク状態に応じて通常／Olderプロファイルを選択する処理
- 接続、切断、再接続時の既存状態とUI通知

## 5. 移行後の確認

### 静的確認

- 対象3ファイルの実行コードから`Global.linkedProfileCheck`参照がなくなること
- `Global.linkedProfileCheck`は互換シムの定義としてのみ残ること
- `GetLinkedProfileCheck`／`SetLinkedProfileCheck`のデバイス境界チェックを利用すること
- 高頻度getterのLegacyログが実行時に出力されないこと

### 実機・回帰確認

- コントローラー接続時にリンク済みプロファイルが適用されること
- リンク解除・再設定がUIとLinkedProfiles.xmlへ反映されること
- 切断・再接続後の選択プロファイルが変わらないこと
- 通常プロファイル保存時の対象プロファイル選択が変わらないこと
- `[Legacy] Global.linkedProfileCheck getter accessed via static shim` が接続・切断・プロファイル操作で出力されないこと

## 6. 現時点の仮説と判別チェック

仮説: 同じLegacyログが複数操作で出る主因は、`linkedProfileCheck`配列の値そのものではなく、配列要素アクセスのたびにGlobal getterが実行される設計である。

判別チェック: C-5-1移行後に対象3ファイルのGlobal参照を検索し、実機の接続・切断・リンク設定変更を行う。Global getterログが0件になり、リンク状態と保存動作が変わらなければ仮説を支持する。
