# Phase7-Step4 完了報告書: 起動・終了シーケンスの近代化

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step4-Plan.md`（Q1〜Q7 はすべて推奨どおり承認）  
状態: 実装完了。W1〜W4 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。W3 は 1 回の再ビルド）。W5 は文書のみ。実機確認 U-14 は未。

---

## 1. 結果の要約

- 起動前（Pre-Host）で `Global` の静的な状態に頼っていた部分を、コンテナの構築より前に作る **ブートストラップのサービス**（`PathService`／`EnvironmentService`）へ移した。`CreateHost` へ渡して、同じインスタンスをコンテナへ登録する。
- `Global.*`: 488 → **452**（−36）。`AppHost` 40 → 39。`Program.rootHub` 22 → 21（基準値の過大分を修正）。Phase7 開始時（733）からの累計は −281。
- 起動・終了の手順:
  - `-driverinstall` 分岐の暗黙のホスト構築（K7-1 の残存経路）を解消。設定の読込の前に、起動引数つきで明示的に作る。
  - `CleanShutdown` の最後で `AppHost.Dispose()` を呼ぶ。Dispose の後は `GetService` が暗黙にホストを作り直さない。
  - `-reenableDevice` を `IDs4DeviceRegistry` の契約経由にした（Step2 で「接続」と判断していた TODO を解消）。

## 2. Wave ごとの実績

- W1（Q6）: ViGEmBus／HidHide／FakerInput／OS の調べ物とその状態（`Global` の 27 名）を `EnvironmentService` へ移した。HidHide／FakerInput の初回の調べ物は、静的初期化ではなく最初に読まれたときに行う（`EnvironmentServiceStaticInitGuardTests` を維持）。`Global.*` 488 → 471。
- W2（Q2・Q3・Q5）: `AppHost.CreateHost(string[])` と `CreateHost()` の未使用引数を削除。`Global.Load()` を削除し、`-driverinstall` 分岐は明示的にホストを作って `IAppSettingsService.Load` を呼ぶ。`WelcomeDialog` の `loadConfig` 引数（唯一の呼び出し元が直前に同じ処理をしていた重複）を削除。`CleanShutdown` に `AppHost.Dispose()` を追加。
- W3（Q1）: 実行ファイルの情報、保存先、初回起動・保存場所が 2 か所の判定、`FindConfigLocation`／`SaveWhere`／空のプロファイル一覧の作成を `PathService` へ移した。`FindConfigLocation` は、実行ファイルのフォルダ・ローミングのフォルダ・ファイルの有無を引数で受けるオーバーロードを持ち、4 通りの分岐をテストで固定した。`runHotPlug` は `AppSettingsService` のインスタンスの状態にした。`Global.*` 471 → 452。
- W4（Q1・Q4）: `App` が起動の最初にブートストラップのインスタンスを作り、Pre-Host で使い、`CreateHost`（2 箇所）へ渡す。`-reenableDevice` を契約経由に。`App` の `Global.appdatapath`／`FindConfigLocation`／`RefreshViGEmBusInfo` の参照を解消。
- W5: ラチェットの基準値の更新、台帳 §7、モデル図 03／04、チェックリスト U-14、本報告書。

## 3. 設計上の判断と、計画との差

- **状態は static（計画書 §2 との差）**: 計画では、ブートストラップのインスタンスが状態を持ち、`Global` の窓口がそれを指す形だった。実際には、`PathService`／`EnvironmentService` の状態は **static** で持つ。理由は次のとおり。
  - 下位層（`BackingStore` のフィールド初期化子、`LoggerHolder`）が静的に読む。
  - 既存テストが `Global.appdatapath` を書き換える（約 46 件）。`new PathService()` の既定のインスタンスも同じ値を見る必要がある。
  - プロセス全体で 1 つの値が正しい（インスタンスごとの状態を許すと、Pre-Host と DI のインスタンスで食い違う危険がある）。
  - 結果として、ブートストラップのインスタンスと、コンテナのインスタンスは、どちらも同じ状態を見る。コンテナへ同じインスタンスを登録する手順は、そのまま実装した。
- **`Global` の窓口**: `appdatapath`／`exedirpath`／`exelocation`／`exeversion` は、`PathService` の状態を指す読み書きの窓口として残した（呼び出し元の多くが下位層）。Step10 で `BackingStore` の永続化を切り出すときに、`IPathService` の注入に置き換えて廃止する。
- **`PathService` の依存**: `SaveWhere` が `BackingStore` のファイルパスを更新するため、`PathService(string appDataPath = null, BackingStore config = null)`。省略時は `Global.store`（TODO: Step10 で必須の依存へ）。
- **`WelcomeDialog`**: `loadConfig` と、中の `FindConfigLocation`／`Load` の重複呼び出しを削除した（呼び出し元が直前に同じ処理を済ませている）。
- **`-reenableDevice` の差（不正な起動引数の場合のみ）**: デバイスのインスタンス ID が空のとき、旧実装は `DS4Devices.reEnableDevice` が例外で終了していたが、アダプタ（`ReEnableDevice`）は何もせずに戻る。正しい起動（ID あり）では動作は同じ。

## 4. 調査で判明した重要事項

- `ratchet` の `Program.rootHub` の合計 22 は、`Global.cs` の基準値 6 が過大だった（実数 5）。コードの変更ではなく、基準値を実数 5 に直した（合計 21）。
- `GlobalPattern` の先読みにより、`DS4Windows.Global.X` と書く参照（`App.xaml.cs`、`LoggerHolder`）は、ラチェットでは数えられない。移管の確認は、`AppPostHostGlobalReferenceGuardTests`（`\bGlobal\.`）と、各 Wave の回帰ガードで行った。
- `AppPostHostGlobalReferenceGuardTests.RetainedGlobalReferences_HaveTodoComments` は、`App.xaml.cs` に `TODO(Phase6-Step7 決定3＝P1)`／`決定4＝K` の文字列が残ることを要求する。コードを置き換えたときも、コメントは残した。
- 既存の課題（回帰ではない）: 起動直後のログローテーションとログの最小レベルは、`Profiles.xml` を読む前に参照されるため、ユーザーの設定ではなく `BackingStore` の既定値になる。DI 移行前（2025-12-14）の機能からの動作。Q7＝A により記録のみ。直す場合は、ログ設定だけを先に読む手順の追加（起動順序の変更。実機確認が増える）。

## 5. 未完了・持ち越し

- **実機確認 U-14**（チェックリスト §11）。最も重要なのは、終了時にホストを破棄する点（固まらない、ViGEm の仮想デバイスが残らない）と、初回起動・保存場所の選択。U-11、U-12、U-13 も未。
- Step5 へ: `Program.rootHub`（21）と Service Locator（`AppHost.GetService` 39、`ServiceProviderHolder` 23）の削除。`Global.cs` の `*Instance` フォールバック（`new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)` ほか）。`AppHost.CreationNote` と暗黙構築の経路の整理。
- Step10 へ: `Global` の窓口 4 つ（`appdatapath` ほか）、`IsAdministrator`／`AdminNeeded`／`GetInstanceIdFromDevicePath`／`CheckIfVirtualDevice`、`PathService` の `Global.store` の既定。
- ユーザー判断が残る事項: ログ設定の早期適用（§4）を直すか。

## 6. 次の Step

- **Step5: `Program.rootHub` と Service Locator の削除**（`Phase7-Plan.md` §2）。調査と `Phase7-Step5-Plan.md` の作成から着手する。
