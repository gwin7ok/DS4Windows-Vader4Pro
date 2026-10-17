# Phase7-Step10c 完了報告書: `BackingStore` の永続化の切り出し

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step10c-Plan.md`（Q1〜Q8 はすべて推奨どおり承認）。上位: `Phase7-Step10-Plan.md`  
状態: 実装完了。W1〜W6 のビルド・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行。W1 はゴールデンの生成・確認・コミットを含む）。W7 は文書のみ。実機確認 U-20（設定ファイルの互換）は未。

---

## 1. 結果の要約

- `BackingStore`（6,522 行）から、設定ファイルの読み書きとプリセットを 6 つの型へ切り出した。**`BackingStore.cs` は 1,660 行**になり、設定の状態（公開のフィールド・プロパティ）と、名前引き・後処理の補助だけを持つ。依存の向きは「シリアライザー → `BackingStore`」で、`BackingStore` は永続化の型を知らない。
- 切り出した型（`DS4Control/Services/`、具象クラスの Singleton）: `SpecialActionsXmlSerializer`（465 行）、`LinkedProfilesXmlSerializer`（281 行）、`ControllerConfigsXmlSerializer`（186 行）、`AppSettingsXmlSerializer`（556 行）、`LegacyProfileXmlSerializer`（3,093 行）、`ProfilePresets`（604 行）。静的な `VersionUtil` も新設した。
- 保存形式・ロジック・ログ・要素の順序は変えていない（移動のみ）。**ゴールデンテスト 11 ファイル**（保存した XML の全文と、読込後の状態）で、切り出しの前後が一致することを各 Wave で確認した。
- 数値: `Global.*` 93 → **69**。`AppHost.GetService|Services` 4、`Program.rootHub` 0、`ServiceProviderHolder` 17 は変更なし。Phase7 開始時（733）からの累計 −664。
- `Global` のアクション保存系のシム（`SaveAction` ほか 5 名）、`CreateStdActions`、`CompileVersionNumber*` を削除した。

## 2. Wave ごとの実績

- W1: ゴールデンテストの基盤（`GoldenFile`、`BackingStoreStateDumper`、`BackingStoreStateFiller`、`PersistenceGoldenContext`、`PersistenceHarness`）と 11 件のテスト。ファイルが無いと生成して失敗する方式で、ユーザーが切り出し前のコードで生成・確認・コミットした。再実行 3 回（§3）。
- W2: `Actions.xml`・リンクプロファイル・コントローラー固有設定の 3 つ。`Global` のシム 6 名を削除し、`SpecialActionRepository` に実行中のアクション状態のクリアと読み直しの検証を移した。再ビルド・再テスト 3 回。
- W3: アプリ設定（`AppSettingsXmlSerializer`）と `VersionUtil`。パス・環境は `IPathService`／`IEnvironmentService` から得る。再ビルド・再テスト 0 回。
- W4: プロファイルの**読込側**（`LegacyProfileXmlSerializer`）。再ビルド 1 回（§3）。
- W5: プロファイルの**保存側**を同じクラスへ。作業用の `m_Xdoc` を `BackingStore` から削除。再ビルド・再テスト 2 回（§3）。
- W6: プリセット（`ProfilePresets`）。再ビルド・再テスト 0 回。
- W7: 台帳 §15、モデル図（03、04）、`Phase7-Status.md`、チェックリスト U-20、本報告書。

## 3. 計画との差と、調査で判明した事項

- **W4／W5 を入れ替えた**（W4＝読込側、W5＝保存側）。`LoadProfile` が `SaveProfile` を呼ぶため、保存を先に移すと `BackingStore` からシリアライザーへの逆向きの依存が要る。読込を先に移し、`store.SaveProfile` を、保存側の移動後に同じクラスのメソッドにした。
- **切り出しの道具**: 移動と `store.` の修飾付けは、スクリプト（コメント・文字列を除く識別子のみを置換。ローカルと同名の識別子は検出して除外）で行った。**宣言形式の見落とし（カンマ区切りの宣言 `SZDeadzone` など）で、W4・W5 の各 1 回、ビルドエラーになった**（型検査が検出。実行時の不具合にはならない）。
- **移動先で `Global` をやめた**: `Global.appdatapath` → `IPathService.AppDataPath`（未設定時の代替値が違うが、デバッグログとプロファイルの置き場所の組み立てのみで、本番では起きない）、`Global.exedirpath` → `IPathService.ProgramDirectory`、`Global.AdminNeeded`／`IsAdministrator`／`exeversion` → `IEnvironmentService`、`Global.useDInputOnly` → `IProfileSettingsService.UseDInputOnlyArray`（実行時の「DInput のみ」の配列。`BackingStore` の `dinputOnly` とは別物）、`Global.GetAction`／`GetActionIndexOf` → `store.GetAction`／`store.GetActionIndexOf`（本体では `Global.store` と同じ実体）。
- **ゴールデンの再実行（W1）の原因（いずれもテストの問題で、本体の回帰ではない）**:
  1. プロファイルの読込で `Global.RefreshActionAlias` が NRE: 出力 KBM のマッピングが未設定だった。テストの実行環境で、`Global.ProfileSettingsServiceInstance`（最初に解決したコンテナの instance を静的に保持する）に設定する形にした。
  2. 正規化の置換 `app_version="<V>"` が不正な XML になり、読込の入力に使うと何も読まれず、読込後の状態の差が空になっていた。妥当な値 `0.0.0.0` に変え、影響する生成物 5 ファイルを作り直した。
  3. `IVirtualKBMLifecycle` の名前空間（`DS4Windows.Services`）の using 漏れ。
- **W2 の再実行の原因**: ゴールデン以外のテスト（ソース文字列を照合するもの 3 件、`Global.ProfileSettingsServiceInstance` の静的な保持による実行順依存 1 件）。**`Step10FourGlobalRatchetGuardTests` の `SpecialActionRepository` の基準値（3）を W2 で 0 に更新し忘れ**、W7 で直した（基準値は「超えない」検査のため、取りこぼしても失敗しない。ラチェットの実数と基準値を突き合わせる手順を、今後の Wave の終わりに入れる）。
- **`m_Xdoc` の扱い**: 旧 `BackingStore` では 1 つの `XmlDocument` を全員が共有していたが、各シリアライザーが自分のフィールドとして持つ形にした（同じ使い方・同じ排他。共有による干渉は減る方向）。排他は `ProfileXmlStore.XmlIoLock`（ガードレール 5.1）のまま。
- **ゴールデンの対象外**: コントローラー固有設定（`ControllerConfigs.xml`。`DS4Device` の実体が要る）。移動の規模が小さく（186 行、`BackingStore` のメンバー 5 個）、実機確認 U-20 で確認する。
- **未接続・孤立コード**: なし（新しく見つけたものはない）。

## 4. 起動順序・スレッド・ガードレールへの影響

- 5.1（同一 XML の排他ロック）: `ProfileXmlStore.XmlIoLock` の構造を変えていない。シリアライザーはロックを持たない（従来どおり）。
- 5.2（適用時 Halt）: `LoadProfile` の呼び出し順を変えていない。5.4（On-Demand パス評価）: シリアライザーは `IPathService.AppDataPath`（毎回評価）を使う。コンテナ構築時に設定ファイルを読む処理はない。
- DI の依存: `ProfileXmlStore` と `ProfileRepository` の依存が増えた（6 つのシリアライザー）。いずれも `BackingStore` などの Singleton を受けるだけで、循環はない（コンテナの解決をテストで確認）。

## 5. 未完了・持ち越し

- **実機確認 U-20**（チェックリスト §11）: 実際の設定フォルダー（バックアップ）で、読込・保存の互換を確認する（ハードウェアは不要）。U-11〜U-19 も未（U-13 は先送り）。10e の総合検証でまとめて行う。
- 10d: `BackingStore` に残る `Global` 参照 7（`appdatapath`×3、`ProfileSettingsServiceInstance`×2、`useDInputOnly`、`RefreshActionAlias`）、`Global.store` の解消と `BackingStore` のコンストラクター注入、環境・パスの窓口（`exeversion` ほか）、`using static DS4Windows.Global;`、別名（`CUSTOM_EXE_CONFIG_FILENAME`／`CONFIG_VERSION`／`XML_EXTENSION`）の削除、`CURRENT_DS4_CONTROLLER_LIMIT` の `BackingStore` の分（7）。`configFileDecimalCulture`（9 件）の置き場所。
- 10e: `CURRENT_DS4_CONTROLLER_LIMIT` の画面・サービスの分、`AppHost` の暗黙構築、`Mapping` の依存の整理、`ColorUtil` の孤立コード、総合検証（U-11〜U-20）と Phase7 の完了報告。

## 6. 次の Step

- **10d**: 調査と `Phase7-Step10d-Plan.md` の作成から着手する（ユーザーの指示を待つ）。
