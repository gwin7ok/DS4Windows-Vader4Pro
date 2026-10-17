# Phase7-Step10b 完了報告書: 持ち越し事項の消化と、`Global` の表・純粋関数の再配置

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step10b-Plan.md`（Q1〜Q8 はすべて推奨どおり承認。Q3 の `OnSubPropertyChanged` の削除を含む）。上位: `Phase7-Step10-Plan.md`  
状態: 実装完了。W1〜W4 のビルド・テスト実行は成功（ユーザーが各 Wave で実行。W4 は 2 回の再ビルド）。W5 は文書のみ。実機確認は 10b 単独の項目なし（10e の総合検証に含める）。

---

## 1. 結果の要約

- `Global` に残っていた表・文字列変換・色の純粋関数を、用途ごとの静的クラスへ移して削除した（宣言は移動のみで、内容は同一）。
  - `X360ControlTables`、`DS4ControlNames`、`ColorUtil`、`TrayIconResources`（画面側）。
- 持ち越しの契約を、`copilot-instructions` §2.4 の手順（経緯の調査→判断→承認→実施）で消化した。
  - 削除: `ProfileSubSettingBase.OnSubPropertyChanged`、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`、`ControlService.USING_MAX_CONTROLLERS`（静的シム）、孤立定数 4 件。
  - 接続: `IEnvironmentService.UsingMaxControllers`（3 箇所の呼び出し元）、`DEFAULT_CHECK_EVERY_VALUE`／`_UNIT`（既定値の出所）。
  - 温存: `IOutputSlotService.OutputSlotChanged`、`MainWindow` の引数 18 個。
- 数値: `Global.*` 107 → **93**。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計 −640。
- 機能・ログの変更なし。

## 2. Wave ごとの実績

- W1: `X360ControlTables`／`DS4ControlNames` の新設（宣言を移動）。`BackingStore` の 1 引数の `getX360ControlString`（XML 保存用の名前）も、状態を持たない switch のため `X360ControlTables` へ移した。呼び出し元 6 ファイルを切り替え。ガード `Phase7Step10bW1ControlTablesGuardTests`。ビルド・テストは 1 回で成功。
- W2: `TrayIconResources`／`ColorUtil` の新設。`DS4LightBar`／`Mapping` の `getTransitionedColor` を `ColorUtil.` で修飾。`MappingStep3FinalGlobalReferenceGuardTests` を更新。ガード `Phase7Step10bW2TrayIconAndColorGuardTests`。ビルド・テストは 1 回で成功。
- W3: `OnSubPropertyChanged` の削除（テスト 2 件を `PropertyChanged` の検査に替えた）、`ProfileApplicationService` の依存の削除（`TestRepositoryFactory` ほか 7 箇所のテストの呼び出しを更新）、`OutputSlotChanged` の TODO 更新。ガード `Phase7Step10bW3ContractCleanupGuardTests`。ビルド・テストは 1 回で成功。
- W4: `UsingMaxControllers` の接続と静的シムの削除、孤立定数の整理。ガード `Phase7Step10bW4UsingMaxControllersGuardTests`。再ビルド 2 回（§3）。
- W5: 台帳 §14、モデル図（03、04）、`Phase7-Status.md`、本報告書。

## 3. 計画との差と、調査で判明した事項

- **目標値は約 80 ではなく 93**。W3・W4 は `Global.` の参照を減らさない作業のため、見積もりが過大だった。計画書に実測を記載した。
- **W4 の不具合（実装中に検出して修正済み）**: `ProgramItem` のコンストラクターに引数 `usingExpandedControllers` を追加したが、フィールドへの代入を書き忘れた。ビルドの警告（CS0649「フィールドは割り当てられません」）で検出し、代入を追加した。この警告を見落とすと、自動プロファイル画面で拡張コントローラー（5〜8 番目）の行が常に非表示になる不具合になるところだった。ガード（`ProgramItem` の表示の検査）を追加して固定した。
- **W4 のもう 1 件の再ビルド**: `AutoProfilesViewModel` をコンストラクターで直接生成していたテスト（`Step10GroupTwoGlobalReferenceGuardTests`）2 行の引数を更新した。事前の検索（`new AutoProfilesViewModel(`）が、テストプロジェクトの引数を省略した検索になっていた。
- **テストの文字列エスケープの不具合（修正済み）**: ガードテストの C# の文字列を Python のスクリプトで書いたとき、`\n`／`\f` が制御文字に化けた（`@"C:\no\such\file.exe"` の改行とフォームフィード）。テストは通っていたが、意図しない文字列になっていたため、W5 の前に正しいパスへ直した。教訓: バックスラッシュを含む C# の文字列は、Python の通常文字列に埋め込まず、raw 文字列か Write ツールで書く。
- **移動前から呼び出し元 0 件のコード**: `ColorUtil.applyRatio(Color, Color, uint)` と `HuetoRGB(float, float, float)`（旧 `Global` の WPF 色のブレンド用）。削除は未承認のため、内容を変えずに移し、TODO を付けた。10e で経緯の調査と承認を経て判断する。
- **新たな発見（持ち越し）**: `ControlService.CURRENT_DS4_CONTROLLER_LIMIT`（`USING_MAX_CONTROLLERS` の仲間の静的シム）は、30 箇所以上（16 ファイル、テスト 5 件）から使われている。`IEnvironmentService.ControllerSlotLimit` の注入に置き換える作業は、`BackingStore` の分を 10d、画面・サービスの分を 10e で行う。
- `Phase7Step2RemovedContractMembersGuardTests` の「残した契約」には `UsingMaxControllers` が含まれる。接続後も契約は残る（本体の利用者ができた）ため、更新は不要だった。

## 4. 起動順序・スレッド・ガードレールへの影響

- なし。表・関数は移動のみ。`UsingMaxControllers` は、従来の静的な値（`EnvironmentService.ProcessControllerSlotLimit == 8`）と同じ値を、注入されたインスタンスから読む（プロセス内で不変）。`AutoProfileHolder` は DI の Singleton のまま、`IEnvironmentService` が追加の依存になった（登録済みのため解決できる）。

## 5. 未完了・持ち越し

- 10c: `BackingStore` の永続化の切り出し（`LegacyProfileXmlSerializer` ほか）。ゴールデンテストを先に整備する。`configFileDecimalCulture`、`CompileVersionNumber*`、設定の定数、`LoadActions`／`SaveAction` などの薄いシム。
- 10d: `Global.store` の解消、環境・パスの窓口、`ProfileSettingsServiceInstance` 経由の設定の窓口、`using static DS4Windows.Global;`、`CURRENT_DS4_CONTROLLER_LIMIT`（`BackingStore` の分）。
- 10e: `CURRENT_DS4_CONTROLLER_LIMIT`（画面・サービスの分）、`AppHost` の暗黙構築、`Mapping` の依存の整理、`ColorUtil` の孤立コードの判断、総合検証（実機確認の一括）と完了報告。
- 実機確認 U-11〜U-19: 先送り（10e の総合検証）。10e の確認項目に、トレイアイコンの切り替え、自動プロファイル画面（8 台表示）、ライトバーの色の遷移、自動プロファイルの保存を加える。

## 6. 次の Step

- **10c**: 調査と `Phase7-Step10c-Plan.md` の作成から着手する（ユーザーの指示を待つ）。
