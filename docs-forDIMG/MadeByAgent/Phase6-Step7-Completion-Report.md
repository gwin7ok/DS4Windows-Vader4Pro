# Phase6-Step7 完了報告書: `App.xaml.cs` Post-Host 領域の Global 直参照解消（Composition Root の整理）

作成日: 2026-09-25  
状態: **完了確定（2026-09-25）**（Step7-1〜7-4 の各段階でビルド・テストビルド・テスト実行成功、実機確認済み。3 項目は Step11 へ先送り）  
対象ブランチ: `For-DI-migration-work`  
計画書: `docs-forDIMG/MadeByAgent/Phase6-Step7-Plan.md`  
着手前の HEAD: `56f6e8bc`

---

## 1. 結果の要約
- `App.xaml.cs` の Post-Host 領域（明示的なホスト構築と `CreateControlService` の後）の `Global` 直参照 39 件のうち 38 件を、DI サービス経由に置き換えた。残る 1 件（`AttemptSave` の `Global.appdatapath = null`）は、置き換え先がないため TODO 付きで温存した（決定3）。
- `App` は WPF の `Application` でコンストラクタ注入ができないため、Composition Root として `InitializePostHostServices()` で 7 つのサービスを 1 回だけ解決し、フィールドに保持する形にした（決定1）。
- Pre-Host 領域（ホスト構築より前の起動処理と `CheckOptions`）の 12 件、共用ヘルパー `ApplyLanguageSetting` の 2 件、const の 1 件は温存した。Pre-Host 領域のコードは変更していない（コメントの追加 2 箇所のみ）。
- `App.xaml.cs` の `Global.` 参照は **54 件 → 16 件**（温存 15 件＋TODO 付き温存 1 件）。
- 調査の過程で、DI 移行作業による既存の回帰（起動引数 `-virtualkbm` が 2026-09-02 以降無視されていた）を見つけ、是正した（決定5・決定7）。
- DI ホストが `Global` の静的初期化の中で起動引数なしに先に作られていることが判明した。根本対応は持ち越し事項 K7-1 とした。

## 2. 計画書からの主な変更点（Step7-0 の台帳再作成）
- 旧計画の C7-30〜C7-32（終了処理の `outputKBMHandler.Disconnect`、2 回目の `Save`、`ResetConnectionFlags`）は存在しなかった。
- 旧計画の「`Application_Exit`」は実際には `CleanShutdown`、「`SaveWhere` 処理」は `CreateConfDirSkeleton`／`AttemptSave` だった。
- 旧計画で Pre-Host とされた 110〜111 行は、Pre-Host・Post-Host・App の外の 3 経路から呼ばれる共用ヘルパー `ApplyLanguageSetting` の中にあった。
- 件数を参照式単位で数え直した（旧「32 件＋保護 11 件＋const 1 件」→ 現行「置き換え対象 39 件＋温存 15 件」）。
- 移行先サービスの挙動差を 2 つ見つけた。
  - `SpecialActionRepository.LoadActions()` は `Actions.xml` がないと何もせず `false` を返し、`Global.LoadActions()`（既定アクションを作って `true`）と動作が違った。そのまま置き換えると、初回起動で全プロファイルの XML に「Disconnect Controller」が書き足される。
  - `PathService.AppDataPath` のセッターは `Global.appdatapath` を変えない。
- 旧計画の確認手順の起動オプション表記 `--driverinstall` は誤りで、正しくは `-driverinstall`（または `driverinstall`）だった。関連文書とコメントを修正した。

## 3. ユーザー決定（詳細は計画書 §4）
- **決定1＝A**: `InitializePostHostServices()` でフィールドに 1 回だけ解決する。
- **決定2＝L2**: `SpecialActionRepository.LoadActions()` の早期 `return false` を削除して `Global` と同じ動作にし、App から使う。
- **決定3＝P1**: `Global.appdatapath = null` は TODO 付きで温存。`PathService.AppDataPath` のセッターは Step12 の削除候補。
- **決定4＝K**: `ApplyLanguageSetting` の 2 件は温存（共用ヘルパーのため二重経路を作らない）。
- **決定5＝A**: `-virtualkbm` の回帰を本 Step で是正する。
- **決定6＝N**: `Program.rootHub` などの `Global` 以外の静的結合は本 Step の対象外とし、後の Step／Phase で Pure DI 化する（`Phase6-Step12-Plan.md` §4 に登録）。
- **決定7＝H**（Step7-4 の実機確認後に追加）: 起動引数の保持役 `IStartupArguments` をコンテナに登録し、`AppHost.CreateHost(config, parser)` が値を設定する。根本原因（ホストの暗黙構築）の是正（案 R）は持ち越し K7-1。

## 4. 実施した変更
- **Step7-1（`c2cebab2`）: 既存サービスへの契約追加（7 メンバー、いずれも `Global` への薄い委譲）**
  - `IPathService.RoamingAppDataPath`／`HasMultipleSaveLocations`
  - `IAppSettingsService.UseLang`
  - `IProfileRepository.SaveAsProfile`／`LoadLinkedProfiles`
  - `IDeviceStateService.ResetConnectionFlags`
  - `ISpecialActionRepository.CreateStandardActions`
  - あわせて `SpecialActionRepository.LoadActions()` を是正（決定2）。
- **Step7-2（`81f57801`、警告の解消 `2df5d661`）: サービス保持と起動前半の置き換え**
  - フィールド 7 個、`InitializePostHostServices()`、`ResolvePostHostService<T>()`（解決できなければ型名入りの例外）を追加。
  - 起動前半（P7-01〜P7-11）と、初回起動の補助 `CreateConfDirSkeleton`／`AttemptSave`（P7-26〜P7-37）の 23 件を置き換え。実行ファイルのフォルダは、`Global.exedirpath` と値が異なりうる `IPathService.ExecutableDirectory` ではなく、`Path.GetDirectoryName(_pathService.ExecutablePath)` で求める。
- **Step7-3（`48e626e6`）: 起動後半と終了処理の置き換え**
  - 起動後半（P7-12〜P7-25）と `CleanShutdown` の保存（P7-39）の 15 件を置き換え。
  - 温存箇所の TODO（決定3・決定4）と、Pre-Host 境界の説明コメントを追加。
- **表記修正（`62801e87`）**: `--driverinstall` → `-driverinstall`（コード中のコメント、テスト、計画書）。
- **Step7-4（`a1bde524` → 再実装 `35355bf1`）: `-virtualkbm` の回帰の是正**
  - 最初の是正（コンテナに登録された `ArgumentParser` を使う）は、テストは成功したが実機で効果がなかった。ホストが `Global` の静的初期化で起動引数なしに先に作られ、`CreateHost(config, parser)` が既存のホストを返すだけになっていたため。
  - 決定7＝H で再実装: [新規] `DI/IStartupArguments.cs`、[新規] `DS4Control/Services/StartupArguments.cs`。`ServiceRegistration` に登録し、`ControlService` の生成で起動引数を取り出して渡す。`AppHost.CreateHost(config, parser)` は、ホストが構築済みでも保持役に値を設定する。
  - モデル図 `04-Service-Lifecycle-Spec.md`（登録サービス一覧に追加・注釈）と `03-Class-Interface-Diagram.md`（注釈）を更新。
- **Step7-5（本報告書）**: 文書の更新と引き継ぎの登録（§7）。

## 5. 追加・変更したテスト（`DS4WindowsTests`）
- `Phase6Step7ContractExtensionTests`（新規、8 件）: 追加した契約の `Global` との状態共有、`LoadActions` が `Actions.xml` のない一時フォルダで既定アクションを作ること、実ファイルを書き換える委譲のソース走査。
- `AppPostHostGlobalReferenceGuardTests`（新規、4 件）: `App.xaml.cs` の `Global` 参照を領域ごと（Pre-Host／Post-Host、各メソッド）に件数で固定するソース走査ガード、`InitializePostHostServices` の呼び出し位置、温存箇所の TODO の存在、7 サービスの解決可否。
- `ServiceRegistrationArgumentParserTests`（新規、4 件）: 起動引数なしでコンテナを先に作ってから起動引数を設定する実際の起動順序で、`ControlService` に起動引数が渡ること。構築済みの `AppHost` への `CreateHost(config, parser)` で起動引数が設定されること。
- `MappingHotPathAllocationTests`（変更）: Step7-1 のテスト追加で実行順が変わり、ランタイムの一過性の割り当て（784 バイト／20000 回）で失敗したため、計測を最大 3 回の最小値に改めた（呼び出しごとの割り当ての検出力は維持）。本番コードの変更なし。

## 6. 検証結果
- **ビルド・テスト**: Step7-1〜7-4 の各段階で、ビルド・テストビルド・テスト実行がすべて成功（ユーザー確認）。途中で出た問題と対処:
  - xUnit2013 の警告（`Assert.Equal` で件数を比較）→ `Assert.Single` に修正。
  - BG1002（`obj` の途中生成物 `.baml` の欠落）→ `obj\x64\Debug` の削除で解消（コード変更とは無関係）。
  - CS0104（テストでの `AppHost` の名前の衝突）→ `DS4WinWPF.AppHost` に修飾。
- **実機確認（2026-09-25、詳細は計画書 §6.6）**:
  - 通常の起動・終了（プロファイル・言語・テーマの反映、ログ、HidHide、コントローラー接続時のプロファイル適用、スペシャルアクション、終了時の保存）: 問題なし。
  - `-m`、多重起動、`-driverinstall`: 問題なし。
  - 初回起動（言語選択 → 保存場所 → 初回ユーティリティ → 既定プロファイルの自動作成）: 問題なし。`Actions.xml` に「Disconnect Controller」が作られ、「Default」プロファイルでは有効になっていない（`CreateStandardActions` が実行されなかった＝決定2 の期待どおり）。
  - `-virtualkbm fakerinput`: DEBUG ログ `Output KBM handler … for fakerinput` で、起動引数が `ControlService` に届くことを確認。引数なしは従来どおり `for sendinput`。
- **Step11 への先送り（`Phase6-Step11-Plan.md` §3.3 に登録）**: プログラムフォルダと AppData の両方に設定がある状態での起動、書き込めない場所での `AttemptSave` のコピー処理、サインアウト・シャットダウン時の保存。
- **Step7 とは無関係と判断した観察**: 初回起動時の更新確認で `api.github.com` への接続が `SocketException (10013)` になった（Windows 側が接続を拒否。WARN として記録されるだけでアプリは継続）。

## 7. 持ち越し事項と他 Step への引き継ぎ
- **K7-1（`Phase6-Status.md` §6.5、`Phase6-Step12-Plan.md` §4）**: DI ホストが `Global` の静的初期化（`ScpUtil.cs:779` の `fallbackProfileRepository`）の中で起動引数なしに先に作られ、`App.xaml.cs` の明示的な構築が空振りしている。Phase7 で `Global` の静的初期化を整理する際に是正する（決定7 の案 R）。
- **K7-2（`Phase6-Status.md` §6.5）**: FakerInput が未導入の環境で `-virtualkbm fakerinput` を明示すると、外部ライブラリが接続成功を返し、SendInput へのフォールバックが起きない（Step7 の回帰ではない）。対応の要否はユーザー判断。
- **Step12（`Phase6-Step12-Plan.md` §2）**: `IPathService`／`PathService` の `AppDataPath` セッターの削除候補。`Global.ResetConnectionFlags`／`LoadActions`／`CreateStdActions` は DI サービスの委譲先として残るため削除対象にならない旨を注記。防壁3 の件数を 12 に更新。
- **Phase7（`Phase6-Step12-Plan.md` §4）**: `Program.rootHub` と `App.xaml.cs` の Service Locator の Pure DI 化（決定6）。各利用側は Step8〜10 でコンストラクタ注入へ切り替える。
- **Step8（`Phase6-Step8-Plan.md` 冒頭）**: `ProfileEditor` の `Global.UseLang` は `IAppSettingsService.UseLang` で置き換えられること、`App.logHolder` 等を変更していないこと、`MainWindow` のコンストラクタ変更時は `App.xaml.cs` も直すこと、K7-1 の前提。
- **Step7b**: 本 Step は `ProfileEditor.xaml.cs`／`MainWindow.xaml.cs` を変更していないため、影響なし。

## 8. 完了判定チェックリストの結果
- [x] Step7-0: 台帳の再作成と計画改訂
- [x] 決定1〜7 のユーザー確認と記録
- [x] P7-01〜P7-39 の置き換え（38 件、P7-38 は TODO 付きで温存）
- [x] E7-01〜E7-15 の温存（ガードテストで固定）
- [x] Pre-Host 領域のコードの非変更（コメントの追加 2 箇所のみ）
- [x] 決定5 の是正（決定7＝H、実機確認済み）
- [x] ビルド・テストビルド・テスト実行の成功
- [x] 実機確認の完了（3 項目は Step11 の先送り台帳に登録）
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step8-Plan.md`／`Phase6-Step11-Plan.md`／`Phase6-Step12-Plan.md`、モデル図 03・04 の更新と、本報告書の作成
