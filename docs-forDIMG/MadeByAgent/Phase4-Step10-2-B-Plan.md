# フェーズ4-Step10-2-B Stage2 詳細計画書: 呼び出し元DI直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提: `Phase4-Step10-2-A`（A-1〜A-9）完了、Stage1実機ベースライン検証完了
関連文書:
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-Completion-Report.md`
- `docs-forDIMG/MadeByAgent/Phase4-Step10-2-A-RealDevice-Verification-Checklist.md`
- `docs-forDIMG/MadeByAgent/Phase4-Status.md`

## 1. 目的と適用範囲

Stage1で`IProfileSettingsService`へ接続した設定値・設定操作について、呼び出し元の`Global`直接参照をDIサービス直接参照へ段階的に置換する。対象は次の3ファイルと、実行時引数を組み立てるFactoryである。

- `DS4Windows/DS4Control/ControlService.cs`
- `DS4Windows/DS4Control/Mapping.cs`
- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`
- `DS4Windows/DS4Control/Services/ViewModelFactory.cs`（Factory経由の注入配線）

Stage1の`Global`後方互換シムは、対象外呼び出し元との互換性のため当面維持する。対象外のプロファイル管理実行、出力ハンドラ、環境・パス・デバイス状態処理は本Stageの直接置換対象に含めない。

## 2. 現状のDI境界

| 対象 | 現状 | Stage2での変更 |
|---|---|---|
| `ControlService` | `ArgumentParser`と`IDs4DeviceRegistry`をコンストラクタで受け取る | `IProfileSettingsService`を追加注入し、対象`Global`設定参照を置換 |
| `Mapping` | staticメソッド・static状態を保持し、`Global`をusing static参照 | Mapping全体のinstance化は行わず、DI設定アクセサを明示的に受け取る境界を追加 |
| `ProfileSettingsViewModel` | `ProfileSettingsViewModel(int device)`で生成され、多数の`Global`参照を持つ | `IProfileSettingsService`を追加注入し、Factoryから渡す。直接newフォールバックは互換用に残す |
| `ViewModelFactory` | DIサービスを保持するが、生成時にViewModelへ渡していない | `CreateProfileSettingsViewModel`で設定サービスを渡す。Factoryログは維持 |
| `IProfileRepository` | 実装済みだがUIの読込・保存はGlobal経由 | Stage2-B-2でUIのプロファイル操作をリポジトリへ接続 |

## 3. 実施順序

### Stage2-B-1: ProfileSettingsViewModelのDI直接参照化

最初に設定画面の直接参照を置換する。影響範囲をA-1〜A-9カテゴリに分け、1カテゴリごとにビルドとユーザー側テストを行う。

1. `ProfileSettingsViewModel`へ`IProfileSettingsService`をコンストラクタ注入。
2. `ViewModelFactory`から注入済みサービスを渡す。
3. A-1〜A-9に対応する`Global`参照を、`_profileSettings`参照へ置換。
4. Stage1対象外のGlobal参照は変更しない。
5. 既存の`ProfileSettingsViewModel(device)`直接生成フォールバックは、サービス未指定時にGlobalシムを使用する互換経路として維持する。

カテゴリ順:

| サブタスク | 対象 | 備考 |
|---|---|---|
| B-1-1 | A-1 スティック | LS/RS設定・カーブ |
| B-1-2 | A-2 トリガー | L2/R2設定・カーブ |
| B-1-3 | A-3 タッチパッド | タッチ設定・出力設定 |
| B-1-4 | A-4 ジャイロ | ジャイロ設定・取得 |
| B-1-5 | A-5 ライトバー・ランブル | 色、ランブル、DualSense設定 |
| B-1-6 | A-6 ボタン・マウス | ボタンマウス、スクロール、トラックボール |
| B-1-7 | A-7 SA・デッドゾーン | SA、SX/SZ、カーブ |
| B-1-8 | A-8 残余設定 | デバイスオプション、ドリフト、DInput |
| B-1-9 | A-9 Mapping関連 | 通知、デバウンス、KBM参照 |

### Stage2-B-2: プロファイル操作のDI直接参照化

`ProfileEditor.xaml.cs`に残るプロファイル読込・保存・切替のGlobal呼び出しを、`IProfileRepository`へ置換する。これはStage2の3主要ファイルに加えてUI操作の実経路をDIへ接続するために必要な隣接配線である。

- `ProfileRepository`内部の再帰的な`Global.LoadProfile`／`Global.SaveProfile`呼び出しを、既存永続化処理を壊さない形で別途整理する。
- `Global.ApplyProfile`の副作用（停止、通知、Untrigger、デバイス反映）は、既存`IProfileSwitcher`設計との責務境界を確認してから置換する。
- 1回のユーザー操作が複数回実行されないことを重点確認する。

### Stage2-B-3: ControlServiceのDI直接参照化

`ControlService`へ`IProfileSettingsService`を注入し、A-1〜A-9対象の`Global`設定参照を置換する。static定数・純粋関数・Stage1対象外サービスは変更しない。

### Stage2-B-4: MappingのDI境界整理

`Mapping`全体のinstance化は行わない。static構造を維持したまま、DIサービスを必要とする高頻度経路を明示的なアクセサまたは既存の実行コンテキスト経由へ段階移行する。

- 入力ループ内で毎回`AppHost.GetService`を呼ばない。
- DIサービス参照は初期化時に1回解決し、既存のstatic状態と競合しないようにする。
- `Global.Clamp`、定数、対象外の出力ハンドラは本Stageの対象外とする。

## 4. ログ方針

- DI直接参照の入口には`[DI] <クラス>.<メソッド>: <詳細>`形式のTraceログを必要最小限で追加する。
- 高頻度のgetter、入力ポーリングループ、配列要素アクセスごとにはログを追加しない。
- `Global`シムのLegacyアクセスログは、実機接続時に連発する高頻度getterについては出力しない。変更操作・フォールバック使用など低頻度の監査ログを優先する。

## 5. 各サブタスクの検証

各サブタスクで次を行う。

1. エージェント: Debug x64ビルドを実行し、警告0・エラー0を確認。
2. ユーザー: Actions／Standaloneのテストビルドとテスト実行を行う。
3. ユーザー: 必要な実機確認を行い、Stage1ベースラインとの差分を記録。
4. ユーザー: 問題がなければコミットとリモート反映を行う。
5. エージェント: 成果報告書と進捗表を更新。

## 6. Stage2完了条件

- `ControlService`、`Mapping`、`ProfileSettingsViewModel`のStage1対象`Global`参照がDI直接参照へ移行している。
- `ProfileEditor`のプロファイル操作が`IProfileRepository`経由で実行される。
- Stage1対象の設定変更で、必要な`[DI]`ログが出力される。
- 高頻度getter／入力ポーリングによるログ連発が発生しない。
- 自動テスト、Debugビルド、Stage2後の実機検証が成功する。
- Stage1で記録された`△`および未実施項目を再評価し、結果を記録する。
