# フェーズ4-Step10-2-C-5-3 DIサービス内部 Legacy 経路監査報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `Phase4-Step10-2-C-Plan.md`

## 1. 調査目的

Step10-2-B では呼び出し元を DI 契約へ接続したが、DI サービスの内部実装が `Global`／`Program.rootHub` へ再委譲していないかを監査する。DI インターフェース経由で呼ばれていても、内部で Legacy 実装へ戻る経路は、完全な責務分離とは扱わない。

## 2. 監査結果

| 対象 | 内部に残る Legacy 経路 | 判定 | 後続方針 |
|---|---|---|---|
| `ProfileRepository` | `Global.LoadProfile`、`Global.SaveProfile`、`Global.ProfilePath`、`Global.appdatapath` | 移行漏れ | C-5-4 で XML 読込・保存を専用ローダー／ライターへ分離 |
| `ProfileApplicationService` | `Global.ApplyProfile`、`Global.LoadProfile`、`Global.LoadTempProfile`、`Global.CompleteProfileApplication` | 移行漏れ | C-5-5 で適用・復帰・共通完了処理をサービス内部へ移設 |
| `SpecialActionRepository` | `Global.LoadActions`、`Global.SaveActions` | 同型の残存 | C-5-6 で Actions XML の読込・保存責務を分離 |
| `PathService` | `Global.appdatapath` | 境界残存 | C-5-7 でパス初期化元を Environment／Path 契約へ整理 |
| `OutputKBMHandlerAdapter` | `Global.outputKBMHandler` | 別領域の既存アダプター | Phase2 の IVirtualKBM 方針に引継ぎ、C-5 の対象外 |
| `ProfileSettingsService` | `Global.store`、一部 `Program.rootHub` | 境界残存 | 設定データ共有は維持し、rootHub 参照だけ別途分類 |
| `ViewModelFactory` | AppHost 解決によるフォールバック | 互換経路 | CP4 まで維持。C-8 で削除判断 |

## 3. 重要な発見

`IProfileRepository` の登録と `ProfileRepository.LoadProfile` の存在だけでは、XML 読込処理の DI 化完了とは判定できない。現在の `ProfileRepository` は実質的に `Global.LoadProfile` の呼び出しラッパーであり、今回の編集画面不具合はこの未分離境界で発生した。

また、`BackingStore.LoadProfile` は設定値の XML パースだけでなく、プロファイル初期化、入力状態リセット、出力デバイス操作、プロセス起動、Action 再構築まで担っている。そのため、単純な `Global` 呼び出し置換ではなく、責務を分割して段階移行する必要がある。

## 4. 対象外・引継ぎ

- `OutputKBMHandlerAdapter` は Phase2 の `IVirtualKBM` 責務として扱う。
- `Global.store` は当面 `BackingStore` の共有境界として維持し、データ二重化を避ける。
- `Program.rootHub` の高頻度入力経路は C-5 ではなく既存の rootHub／デバイスサービス計画へ引き継ぐ。
- Legacy shim の削除は、後続移行と実機確認が完了する C-8 まで行わない。

## 5. 結論

Step10-2-B の調査では、DI サービスの外側にある呼び出し元を主対象としており、DI サービス内部の Legacy 再委譲を網羅できていなかった。したがって、Step10-2-B の完了条件は「DI 入口への接続」までであり、「Legacy 実体の除去」まで含めるのは不正確だった。

C-5-3 で本監査を実施した結果、次の実装順序を追加する。

1. C-5-4: プロファイル XML 読込・保存の責務分離
2. C-5-5: プロファイル適用・復帰の責務分離
3. C-5-6: SpecialAction XML 読込・保存の責務分離
4. C-5-7: 残存 DI サービス内部参照の整理
5. C-6: 自動テスト化判定・実装・実行
