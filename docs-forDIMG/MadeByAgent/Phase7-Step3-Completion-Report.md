# Phase7-Step3 完了報告書: `BackingStore` の DI 化と `Global` の転送の削除

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step3-Plan.md`（各 Wave の記録は §7）  
状態: 実装完了。W1〜W5 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行）。W6 は文書とガードの整備で、コードの変更は無い。実機確認 U-12 は未。

---

## 1. 結果の要約

- `BackingStore`（設定の唯一の実体）を DI のシングルトンとして登録した（実体は `Global.store` と同一）。
- 7 つのサービスが、`BackingStore` を必須の依存として受け取るようになった。任意引数と `Global.store` へのフォールバック（`ProfileXmlStore`）を廃止した。
  - `ProfileXmlStore`、`ProfileRepository`、`AppSettingsService`、`AppearanceSettingsService`、`DeviceStateService`、`AppNotificationService`、`AutoProfileService`。
- `Global` の転送メンバーを **76 名** 削除した（W2 34、W3 21、W4 5、W5 16）。
- ラチェット基準値（全域）: `Global.*` 723 → **516**（計画の見込みは約 520）。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。
- 計画は「約 95 名」だったが、実際の削除は 76 名。差の 19 名は次の理由による。
  - 他の呼び出し元があり、サービス側だけ切り替えて `Global` に残したもの（Q5。§3 参照）。
  - 調査後に、プロファイル適用などの状態と絡むと分かり、Step3c へ持ち越したもの（§5 参照）。

## 2. Wave ごとの実績

- W1: `BackingStore` の登録、`ProfileXmlStore` の必須化、`ProfileRepository` への注入（`Global.store` の 5 箇所）。`Global.*` 723 → 715。
- W2: `AppSettingsService` の画面・ウィンドウの寸法、動作設定。35 名を `_config` に置換し、34 名を `Global` から削除（`Notifications` は残す）。715 → 610。
- W3: `AppSettingsService` の UDP／OSC、更新確認。メソッド型の転送 9 組と更新確認のプロパティを置換し、21 名を削除（アドレス 2 件の `Trim` は維持）。
- W4: 処理を持つ転送。`FakeExeName` の検証、UDP 平滑化の値と変更通知（静的イベント → サービスのインスタンスイベント）、`LastVersionChecked` の番号算出をサービスへ移した。`Global` の 5 名を削除、`LastVersionChecked` は読み取り専用にした。
- W5: ほかのサービス。`ProfileRepository`（リンクプロファイルの 4 操作を含む）、外観、自動プロファイル、接続フラグ、通知、`RefreshExtrasButtons`。16 名を削除。`MainWindow` の `?? new AppearanceSettingsService()`（Pure DI 違反）を、未登録なら例外にする形に変更。
- W6: ガードテストの名前一覧の完成、台帳（`Phase7-Residual-Ledger.md` §5）、モデル図（03、04）、チェックリスト U-12、本報告書。

## 3. 設計上の判断と、その結果

- **削除は同じ Wave で行った**（Q3）。旧 `Global` 経由と新サービスの一致だけを確かめる等価性テストは、削除せず、`Global.store.<field>` を使う形に書き換えて残した（サービスと `BackingStore` の状態共有の確認として有効なため。計画書 §1.4 の「削除」から変更）。
- **処理を持つ転送はサービスへ移した**（Q4）。
  - `FakeExeName`: ファイル名として無効な値を黙って無視する動作は同じ。`null` を代入すると例外になる点も従来どおり。
  - UDP 平滑化の変更通知: 発火の条件と順序は同じ。発火元が `Global` の静的イベントから `AppSettingsService`（`this`）に変わった。購読者は `ControlService` のみ（すでに `_appSettings` 経由）。静的イベントの購読が解除されないまま残る問題も解消した。
  - リンクプロファイルの操作: シリアルからコロンを除く正規化を `ProfileRepository` へ移した。
- **他の呼び出し元があるメンバーは `Global` に残した**（Q5）: `Notifications`（`MainWindow`）、`UseLang`、`LogMinLevel`、`LogMaxArchiveFiles`、`UseCurrentTheme`、`UseMoonlight`、`UseAdvancedMoonlight`、`DeviceOptions`、`getOSCServerPortNum`（`ControlService`）、`LastVersionChecked`（読み取りだけ。`Changelog`）。サービス側はすべて `BackingStore` へ切り替え済みで、`Global` 側は呼び出し元の Step で削除する。
- **回帰ではない**: 設定値の保存・読込は `BackingStore` のフィールドを直接使うため影響しない。機能・ログ・安全弁への影響は無い。

## 4. 調査で判明した重要事項

- **ソース文字列の正規表現で、隣接する式を確認するテスト**が 2 件（`Phase6Step7ContractExtensionTests.ProfileRepository_NewMembers_DelegateToGlobal` の `SaveAsProfile`／`LoadLinkedProfiles`）、`Global.store`／`Global.LoadLinkedProfiles` から `_config` への置換で壊れた。期待する式を直して解消した。今後も、置換の前に `Assert.Matches(@"…Global\…")` 型のテストを検索する。
- **`Global.X` の検索は、`DS4Windows.Global.X` の形式も対象にする**: `LogMinLevel`／`LogMaxArchiveFiles`／`UseLang` は、この形式で `App.xaml.cs`／`LoggerHolder` から呼ばれていた。W3 で走査の正規表現を直して、削除せずに残した。
- **`Global` 内部の無修飾の使用**: `ProfilePath`／`OlderProfilePath`／`SelectedProfile` は `Global.ApplyProfile` が内部で使うため、`Global` に残した（Step3c）。
- 行末が混在している（`Global.cs` は LF、多くのサービスは CRLF）。置換スクリプトは、ファイルごとに行末を判定して書き込む形にした。

## 5. 未完了・持ち越し

- **Step3c（別計画書）**: `ProfilePresetLoader` の 10 名、`ProfileRepository.LoadBlankProfile`、`ProfilePath` 系 3 名、`SelectedProfileChanged` ほかのイベント、`LoadTempProfile`、`ApplyProfile` 系、`ProfileChangedNotification`。`Global.tempprofileDistance` や `ProfileSettingsServiceInstance` などの状態と絡むため、プロファイル適用全体の再設計として扱う。推奨: Step4（環境・パスの実体の移管）の後に、計画書を作成する。
- Step4 へ: `firstRun`／`runHotPlug`、`SetCulture`、環境・パスの実体、`Global.cs` に残る `new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)`（`*Instance` 系の最後のフォールバック）。
- Step5 へ: `ProfileXmlStore.Load/SaveControllerConfigs`（`Program.rootHub` を走査）、`MainWindow` などの Service Locator。
- Step6 へ: `outDevTypeTemp`／`activeOutDevType`、`outputKBMHandler`。
- **実機確認 U-12**（チェックリスト §11 に記録済み。未）。
- 実機確認 U-11（Step2 の確認）も未。

## 6. 次の Step

- **Step4: 起動・終了シーケンスの近代化**（`Phase7-Plan.md` §2）。ブートストラップ、Pre-Host、`AppHost` のオーバーロード、ホストの `Dispose`、`CreationNote`、`ReEnableDevice` の接続、環境・パスの実体の移管。計画書 `Phase7-Step4-Plan.md` を作成して承認を得てから着手する。
- Step3c の計画書を、Step4 の後に作成するか、Step4 に取り込むかを、Step4 の調査で判断する。
