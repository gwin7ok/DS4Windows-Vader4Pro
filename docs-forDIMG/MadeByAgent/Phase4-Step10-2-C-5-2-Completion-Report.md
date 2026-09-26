# フェーズ4-Step10-2-C-5-2 実装報告書: 一時プロファイル状態のDI API移行

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `Phase4-Step10-2-C-5-2-TempProfile-State-Reference-Baseline.md`

## 1. 実装内容

`tempprofilename` と `useTempProfile` の実行時アクセスを、Global 配列シムから `IProfileSettingsService` のデバイス単位 API へ移行した。

- `ScpUtil.cs`: 通常・一時・Blank・Default プロファイルの状態更新を `SetTempProfileName` / `SetUseTempProfile` へ移行
- `Mapping.cs`: Profile Action 判定、復帰対象スナップショット、ログ値の取得を `Get...` API へ移行
- `ControlService.cs`: 接続時および入力処理時の一時プロファイル判定を `GetUseTempProfile` へ移行
- `AutoProfileChecker.cs`: 自動プロファイル比較・解除判定・ログ値の取得を `Get...` API へ移行
- `MainWindow.xaml.cs`: Query のプロファイル名表示を `Get...` API へ移行
- `ScpUtil.cs`: 移行済みの高頻度 `tempprofilename` getter ログを抑制。setter の Legacy 監査ログと互換 shim は維持

状態遷移の順序と条件は変更していない。`Global.tempprofilename` / `Global.useTempProfile` は未移行経路向けに残している。

## 2. 検証結果

- アプリ Debug ビルド: 成功、警告 0、エラー 0
- Actions テストプロジェクト ビルド: 成功、警告 0、エラー 0
- Standalone テストプロジェクト ビルド: 成功、警告 0、エラー 0
- Actions テスト: 85件成功
- Standalone テスト: 13件成功
- ソース検索: 実行コードの Global 配列参照なし。残存は互換 shim とコメントのみ

## 3. 残作業

実機確認は未実施のため、本ステップは「実装・自動検証済み、実機確認待ち」とする。

- 一時プロファイル適用後の `useTempProfile=true` / 名前の一致
- 通常プロファイル復帰後の一時状態クリア
- 自動プロファイル適用・解除および再接続・切断
- Query による現在プロファイル名表示
- 実機ログで `tempprofilename` / `useTempProfile` getter の Legacy ログが出ないこと
