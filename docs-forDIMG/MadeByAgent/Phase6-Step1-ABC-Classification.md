# a/b/c 分類（暫定・全件網羅完了、厳密検証未完了）

作成日: 2026-09-18
対象: Phase6-Step1 10ファイル（762行）
状態: 行数集計完了、a/b/c分類は暫定（個別参照の厳密検証・ホットパス判定・Phase5証跡確認は未完了）

## 分類結果（暫定）

| 分類 | 内容 | 対象例 | 備考 |
|---|---|---|---|
| a | 既存契約候補あり（無条件置換不可） | ControlService (C2-01/02/03), Mapping設定getter, Mouse入力経路 | 契約差（Clone/戻り値/Halt所有権）を個別検証要 |
| b | 要設計（新契約または既存契約拡張） | ProfileApplicationService契約差、SpecialAction編集、ConfigurationLocation、StandardActionInit、IKbmBackendLifecycle、IDisplayCoordinateService | §4.3の提案契約と照合要 |
| c | 除外・保留（互換性維持または非対象） | const/純粋関数、Pre-Host経路、テスト参照、フォールバック互換、readonly配列/辞書（自動除外しない） | 除外理由を一件ずつ記録要 |

## 未完了（報告書§6.2/§7維持）

- 全参照の一意ID（ファイル+行+列）による別紙突き合わせ
- サービス別紙（未検証メモ）の個別参照化と分類矛盾解消
- ファイル別・a/b/c別・除外理由別・ホットパス別の実測集計生成
- Phase5-Step14/15完了証跡の確認
- 本番コード・テストコード・DI登録の変更なし（維持）
