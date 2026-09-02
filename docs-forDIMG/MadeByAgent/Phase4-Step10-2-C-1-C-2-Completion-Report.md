# フェーズ4-Step10-2-C C-1/C-2 実装前確認記録: Composition Root 一本化と ControlService DI 化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`

## 1. 確定した実装方針

### C-1 Composition Root 一本化（未実装）

- 旧 `ServiceCollection`／`BuildServiceProvider` を削除する。
- Action 系登録を `ServiceRegistration.cs` へ統合する。
- Action エントリの事前確保を AppHost の Host 構築後に行う。
- `Host.Services` を `ServiceProviderHolder` へ設定し、Provider を一本化する。

### C-2 ControlService DI 化（未実装）

- `ControlService` を `ServiceRegistration.cs` に Singleton 登録する。
- App 側の通常生成を削除し、AppHost から解決する。
- 起動時 parser は AppHost の parser 付き構築経路から注入する。
- `App.rootHub`／`Program.rootHub` への互換代入を維持する。
- 既存の `Stop`、`ShutDown`、Host Dispose による終了処理を維持する。

## 2. 実装前の確認事項

- `App.xaml.cs` の旧 Provider 構築を削除できること。
- Action 系登録と `PreallocateEntries` を移動できること。
- `ControlService` の parser、Registry、ProfileSettings の生成順序を維持できること。
- AppHost 解決インスタンスから `rootHub` へ互換代入すること。

## 3. 検証状況

- C-0 の文書調査・方針整理: 完了
- C-1/C-2 のコード実装・ビルド・テスト: 未実施
- 実機起動・終了、Singleton 同一性、バックグラウンドスレッド終了: 未実施
- C-1/C-2 の実装コミット・リモート反映: 未実施

## 4. 次の作業

1. C-1 のコード実装と Debug ビルドを行う。
2. C-2 のコード実装と Debug ビルドを行う。
3. ユーザー側テスト、実機確認後にコミット・pushする。
