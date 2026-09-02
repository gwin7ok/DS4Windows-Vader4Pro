# フェーズ4-Step10-2-C-5-2 `tempprofilename`／`useTempProfile` 移行計画

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `Phase4-Step10-2-C-Plan.md`

## 1. 目的

`tempprofilename` と `useTempProfile` の呼び出し元を、`Global` 配列シム経由から `IProfileSettingsService` のデバイス単位APIへ移行する。

移行後の正規経路は次のとおりとする。

```text
呼び出し元
  ↓
GetTempProfileName / SetTempProfileName
GetUseTempProfile / SetUseTempProfile
  ↓
ProfileSettingsService
```

`Global.tempprofilename` と `Global.useTempProfile` は、未移行経路向けの互換シムとして移行完了後も当面残す。

## 2. 現在のDI境界

`IProfileSettingsService` には次のAPIが既に存在する。

- `string GetTempProfileName(int deviceIndex)`
- `void SetTempProfileName(int deviceIndex, string value)`
- `bool GetUseTempProfile(int deviceIndex)`
- `void SetUseTempProfile(int deviceIndex, bool value)`

`ProfileSettingsService` はデバイス番号の境界チェックと変更通知を既に実装している。配列全体の `UseTempProfileArray`／`TempProfileNameArray` は互換境界およびサービス内部状態として維持する。

## 3. 現状の主な呼び出し元

| 呼び出し元 | 現在の用途 | 移行API | 移行上の注意 |
|---|---|---|---|
| `AutoProfileChecker.cs` | 自動プロファイルの比較、現在の一時プロファイル名のログ | `GetUseTempProfile`／`GetTempProfileName` | 自動プロファイルの適用・解除判定を同じ順序で維持 |
| `ControlService.cs` | 接続時の自動プロファイル判定、接続後の状態参照 | `GetUseTempProfile` | 再接続時の通常／自動プロファイル分岐を変更しない |
| `Mapping.cs` | Profile Action抑制、一時状態のログ、復帰対象名の保存 | `GetUseTempProfile`／`GetTempProfileName` | `UntriggerAction` の `prevProfileWasTemporary`／`prevProfileName` と同じスナップショットを保存 |
| `MainWindow.xaml.cs` | 現在プロファイル表示・状態取得 | `GetUseTempProfile`／`GetTempProfileName` | UI表示値と実際の適用状態を一致させる |
| `ScpUtil.cs` | `LoadProfile`／`LoadTempProfile` 後の状態更新、各Blank／Defaultロード後のリセット | `SetUseTempProfile`／`SetTempProfileName` | 状態更新の順序を維持し、ロード成功・失敗時の既存条件を変えない |
| `ProfileRepository.cs` | DI経由の一時プロファイル適用・解除 | 既にDI API使用 | 回帰確認のみ実施 |

`DS4LightBar.cs` の参照はコメントのみであり、実行コード移行の対象外とする。

## 4. 実施フェーズ

### C-5-2-1: `ScpUtil` の状態更新を移行

1. `LoadProfile` と `LoadTempProfile` の成功・失敗条件を固定する。
2. `Global.tempprofilename[device]`／`Global.useTempProfile[device]` の要素代入を、対応する `Set...` APIへ置換する。
3. Blank／Default系ロード処理のリセットも同じAPIへ置換する。
4. 一時プロファイル適用成功時だけ名前と一時フラグを設定する既存挙動を維持する。

### C-5-2-2: 実行時の読み取りを移行

1. `Mapping` のプロファイルAction判定と復帰対象保存を移行する。
2. `ControlService` の接続・再接続判定を移行する。
3. `AutoProfileChecker` の比較・解除判定を移行する。
4. `MainWindow` の表示・状態取得を移行する。
5. `ScpUtil` 内のプロファイル名表示・状態参照を移行する。

### C-5-2-3: Legacyログを整理

- `Global.tempprofilename` getterと `Global.useTempProfile` getterの高頻度アクセスログは出力しない。
- `Global` のsetterログは互換シムが実際に使用された場合の監査用として残す。
- 通常のDI API呼び出しでは `[Legacy]` ログを出力しない。
- `Get...`／`Set...` APIのデバイス範囲外入力は既存どおり安全に無視する。

## 5. 維持する状態遷移

- 通常プロファイル適用後は `useTempProfile=false`、`tempprofilename=空`。
- 一時プロファイル適用成功後は `useTempProfile=true`、`tempprofilename=適用名`。
- 通常プロファイル復帰後は一時状態をクリアする。
- 自動プロファイル解除時は `ProfilePath` の通常プロファイルを維持する。
- Profile Action解除時の `prevProfileName` と `prevProfileWasTemporary` の記録を変更しない。
- 再接続時に、一時プロファイル中か通常プロファイル中かを従来どおり判定する。

## 6. 完了条件

- 実行コードから `Global.tempprofilename` と `Global.useTempProfile` の参照がなくなる。
- `Global` の両シム定義だけが互換用途で残る。
- 一時プロファイル適用・復帰、自動プロファイル、再接続、UI表示が従来と同じ動作になる。
- 高頻度getterのLegacyログが接続・入力・プロファイル適用時に出力されない。
- Actions／Standaloneのビルドとテストが成功する。
- 実機で一時プロファイル適用、通常復帰、切断・再接続を確認する。

## 7. 失敗時の切り分け

- プロファイル名だけが不一致: `GetTempProfileName`／`SetTempProfileName` の更新順序を確認する。
- 一時判定だけが不一致: `GetUseTempProfile`／`SetUseTempProfile` の成功条件とリセット箇所を確認する。
- 復帰先が不一致: `Mapping` の `prevProfileName`／`prevProfileWasTemporary` の保存タイミングを確認する。
- Legacyログだけが残る: 未移行呼び出し元を検索し、シムログ削除で済ませない。
