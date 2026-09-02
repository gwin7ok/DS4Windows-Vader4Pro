# フェーズ4-Step10-2-C C-1/C-2 完了報告書: Composition Root 一本化と ControlService DI 化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`

## 1. 実施内容

### C-1 Composition Root 一本化

- `App.xaml.cs` に残っていた旧 `new ServiceCollection()`／`BuildServiceProvider()` の構築ブロックを削除。
- Action 系 5 登録を `ServiceRegistration.cs` へ統合。
- Action エントリの事前確保を `AppHost` の Host 構築後に移動。
- `AppHost` の `Host.Services` を `ServiceProviderHolder` へ設定し、既存の Provider 参照箇所も同じ Provider を利用するよう統合。
- `AppHost.CreateHost(IConfiguration, ArgumentParser)` を追加し、起動時 parser を DI 登録できるようにした。

### C-2 ControlService DI 化

- `ControlService` を `ServiceRegistration.cs` に Singleton 登録。
- App 側の通常生成を削除し、`AppHost.GetService<ControlService>()` で解決。
- 起動時 parser は AppHost の parser 付き構築経路から注入。
- `App.rootHub`／`Program.rootHub` への互換代入を維持。
- 既存の `Stop`、`ShutDown`、Host Dispose による終了処理は変更していない。

## 2. 機械的確認

- `App.xaml.cs` の `new ServiceCollection`: 0 件
- `App.xaml.cs` の `BuildServiceProvider`: 0 件
- `App.xaml.cs` の `new ControlService`: 0 件
- `ServiceRegistration.cs` の `ControlService` Singleton 登録: 1 件
- `ServiceProviderHolder.SetProvider`: AppHost 内に 3 overload 対応
- AppHost 解決インスタンスから `rootHub` へ代入する経路: 維持

## 3. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- 静的エラー確認: 問題なし
- Actions／Standalone テストビルド・テスト実行: 未実施
- 実機起動・終了、Singleton 同一性、バックグラウンドスレッド終了: 未実施
- コミット・リモート反映: 未実施

## 4. 次の作業

1. ユーザー側で Actions／Standalone テストビルド・テスト実行。
2. 問題がなければコミット・push。
3. 実機で起動、接続、プロファイル操作、終了を確認。
4. その後 C-3 の `rootHub` 呼び出し元分類へ進む。
