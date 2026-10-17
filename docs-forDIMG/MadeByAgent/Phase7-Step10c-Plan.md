# Phase7-Step10c 計画書: `BackingStore` の永続化の切り出し

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Step10-Plan.md`（Q1〜Q9 は推奨どおり承認）§3「10c」・Q3・Q4、`Phase7-Step10b-Completion-Report.md` §5、`Phase7-0-Survey.md` §5（D2・D4）  
状態: **Q1〜Q8 は推奨どおり承認（2026-10-18）。W1〜W6 完了（ビルド・テスト成功。W4／W5 は保存と読込を入れ替えた）。W7 は文書。10c 完了。完了報告書は `Phase7-Step10c-Completion-Report.md`。**

---

## 1. 目的と位置づけ

- `BackingStore`（6,522 行）から、設定ファイルの読み書き（永続化）を別の型へ移し、`BackingStore` を「設定の状態を持つ型」に絞る。保存形式は変えない（D4＝F1）。
- 切り出しは「移動」であり、ロジック・要素の順序・ログは変えない。利用者の設定ファイルが壊れないことを、**ゴールデンテスト（切り出し前に取った期待値との照合）**で固定する。
- 見込み: 約 5,000 行が `BackingStore` の外へ出て、`BackingStore.cs` は約 1,500 行（状態のフィールドとプロパティ、名前引き、後処理の補助）になる（Step10 計画書の「約 3,200 行」は過小な見積もりで、実測に基づき訂正する）。
- `Global.store` の解消と `BackingStore` のコンストラクター注入は 10d。10c では、新しい型が `IPathService` を受けるなど、10d につながる形にとどめる。

---

## 2. 調査結果

### 2.1 永続化にあたるメソッドの規模（`BackingStore.cs`）

- プロファイル（XML。旧形式）: `SaveProfile` 558 行、`LoadProfile` 2,325 行、`SaveAsProfile`、`SaveLegacyExtraSettings`／`LoadLegacyExtraSettings`／`ReadLegacyElement`（約 55 行）。後処理: `ResetProfile`（143 行）、`PostLoadSnippet`（45 行）、`CheckOldDevicestatus`、`PortOldGyroSettings`、`ResetMouseProperties`。
- アプリ設定（`Profiles.xml`）: `Load` 416 行（旧形式の読込の互換を含む）、`Save` 68 行（`AppSettingsDTO` を使う）、`LoadFakeExeName`。
- スペシャルアクション（`Actions.xml`）: `LoadActions` 162 行、`SaveActions`／`SaveActionNew`／`SaveAction`／`CreateAction`（計 約 215 行）。`ActionsLoaded` イベントを発行する（購読者は `ActionsLoadedRuntimeReset`）。
- リンクプロファイル: `LoadLinkedProfiles`／`SaveLinkedProfiles`／`createLinkedProfiles`（計 約 240 行）。
- コントローラー固有設定: `LoadControllerConfigsForDevice`／`SaveControllerConfigsForDevice`（計 約 115 行）。
- プリセット: `LoadBlankDS4Profile`／`LoadBlankProfile`／`LoadDefault*`（10 種。計 約 540 行）。呼び出しは `ProfilePresetLoader` だけ。
- 合計: 約 4,600〜5,300 行（共通の補助を含めるかで幅がある）。

### 2.2 結合の度合い（切り出しやすさ）

- `BackingStore` の状態は、**公開のフィールド・プロパティが約 250 個、非公開は 27 個**。永続化のメソッドが触る非公開メンバーは、作業用の `XmlDocument m_Xdoc`（protected）と、補助メソッド 10 個程度（`axisOutputCurveString`／`axisOutputCurveId`、`OutContDeviceString`／`OutContDeviceId`、`GetLightbarModeString`／`GetLightbarModeType`、`ResetProfile`、`PortOldGyroSettings`、`CheckOldDevicestatus`、`PostLoadSnippet`、`ResetMouseProperties`）だけ。したがって、別の型へ移しても、状態への公開アクセスで足りる。
- 参照する `BackingStore` のメンバーの数（コメントを除く。移した後に `store.` の修飾が要る件数）:
  - リンクプロファイル: 4 個・10 件。コントローラー固有設定: 5 個・20 件。スペシャルアクション: 5 個・55 件。→ 低い。
  - アプリ設定 `Load`: 65 個・169 件。`Save`: 2 個・3 件。→ 中。
  - プロファイル `SaveProfile`: 97 個・503 件。`LoadProfile`: 119 個・587 件。レガシー拡張とプリセット: 114 個・298 件。→ 高い。
- 全般の特徴: `BackingStore` が `Global.appdatapath`／`exedirpath`／`exeversion`／`CONFIG_VERSION`／`XML_EXTENSION`（永続化の定数）と `Global.ProfileSettingsServiceInstance` などを 27 箇所で使う。

### 2.3 呼び出し元

- 本体（`BackingStore` 外から、`store.` 等の形）: `LoadProfile` 2、`SaveProfile` 1、`SaveAsProfile` 1、`Load` 2、`Save` 2、`SaveActions` 3、`SaveAction` 1、`LoadActions` 3、`LoadLinkedProfiles` 1、`SaveLinkedProfiles` 2、`LoadControllerConfigsForDevice` 1、`SaveControllerConfigsForDevice` 1、`LoadBlank*`／`LoadDefault*` 各 1（`ProfilePresetLoader`）。
- 間に挟まる層: `ProfileXmlStore`（`IProfileXmlStore` の実装。排他ロック `XmlIoLock` を持ち、プロファイル・アプリ設定・コントローラー設定を委譲）、`ProfileRepository`（リンクプロファイル）、`SpecialActionRepository`（アクション。`Global.SaveAction`／`RemoveAction`／`CreateStdActions` のシム経由）、`ProfilePresetLoader`。
- `Global` のシム: `SaveAction`／`SaveActions`／`RemoveAction`／`LoadActions`。呼び出し元は `SpecialActionRepository`（と、テスト数件）だけ。
- テスト: `Load` 17、`Save` 7、`SaveProfile` 10、`SaveAction` 10、`LoadActions` 8 など、直接呼ぶものが多い。リフレクションで `m_Xdoc`／`ResetProfile` に触るものがある（`ProfileLegacyXmlRoundTripTests`、`ProfileLegacyXmlCompletenessTests`）。

### 2.4 既存の保護網（ゴールデンの代わりになるか）

- `ProfileLegacyXmlRoundTripTests`（10 要素の往復）、`ProfileLegacyXmlCompletenessTests`（要素の欠落）、`BackingStoreAppSettingsLoadTests`、`AppSettingsDtoTests`（Standalone）、`ProfileXmlStoreTests`、`ProfileRepositoryTests`、`SpecialActionRepositoryTests`。
- いずれも一部の要素・設定だけの検査で、**プロファイル全体の保存結果（約 500 行の要素）と、読込後の状態全体を固定するものはない**。切り出しの前にゴールデンを整える必要がある。

---

## 3. 決定事項と質問（推奨つき）

### Q1. 切り出しの方法
- 推奨: **A. 型の単位で切り出す（新しいクラスに移す）。結合の低いものから高いものへ、段階的に進める**。移した本体の `BackingStore` のメンバー参照には、スクリプトで `store.`（引数 `BackingStore store`）の修飾を付ける。スクリプトは、移動対象の識別子と同名のローカル変数・引数を検出して報告し、衝突は手作業で直す。ビルドの型検査が取りこぼしを検出する（既存の `m_Xdoc` は、新しい型のインスタンスフィールドにする）。
  - 新しい型（名前は仮。いずれも `DS4Control/Services/` に実装。具象クラスのまま DI の Singleton に登録）: `SpecialActionsXmlSerializer`、`LinkedProfilesXmlSerializer`、`ControllerConfigsXmlSerializer`、`AppSettingsXmlSerializer`、`LegacyProfileXmlSerializer`、`ProfilePresets`。
- B: `partial class BackingStore` でファイルだけを分割する。 → 安全で機械的だが、型は 1 つのままで、依存の方向（`BackingStore` が永続化を知らない形）は得られない。Phase7 の目的を満たさない。
- C: 切り出さない。 → Step10 の Q3 の承認に反する。

### Q2. ゴールデンテストの方式
- 推奨: **A. 「期待値の取得（キャプチャ）を先に行い、以後はそれと照合する」方式**。コードの実行はユーザーが行うため、次の手順にする。
  - W1 でテストを追加する。ゴールデンファイル（`DS4WindowsTests/Golden/` 配下。保存した XML のテキストと、読込後の `BackingStore` の公開状態を決定的な文字列にしたもの）が**存在しなければ、テストが自動で生成して「ゴールデンを生成した。確認してコミットし、再実行すること」と失敗する**。ユーザーが切り出し前のコードでテストを実行し、生成されたファイルをコミットする。以後のテストは、ファイルがあれば照合する。
  - 対象: プロファイル（全要素に既定値と異なる値を設定した保存結果。ゴールデンの XML を読み込んだ後の状態）、アプリ設定（`Profiles.xml` の保存結果と、旧形式を含む読込後の状態）、スペシャルアクション、リンクプロファイル、コントローラー固有設定、プリセット 10 種（読込後の状態）。
  - 状態の文字列化は、`BackingStore` の公開フィールド・プロパティをリフレクションで列挙し、配列・リスト・辞書・列挙型・構造体を決定的な順序と書式で出力する（日時・パスなど揺れる値は除外または固定する）。
- B: 目視確認だけで進める。 → 設定が壊れても気づけない。

### Q3. 呼び出し元の扱い
- 推奨: **A. 呼び出し元を新しい型に切り替え、`BackingStore` から移したメソッドを削除する**（`BackingStore` に委譲メソッドを残さない）。`ProfileXmlStore`／`ProfileRepository`／`SpecialActionRepository`／`ProfilePresetLoader` が新しい型をコンストラクターで受ける。`Global` のシム（`SaveAction`／`SaveActions`／`RemoveAction`／`LoadActions`）は呼び出し元が `SpecialActionRepository` とテストだけのため、同時に削除する（`Step12DeadShimRemovalGuardTests` の名簿へ追加）。
- B: `BackingStore` に 1 行の委譲を残す。 → `BackingStore` が永続化の型を知る逆向きの依存が残る。

### Q4. `m_Xdoc`（作業用の `XmlDocument`）と排他
- 推奨: **A. 現状の意味を維持する**。各シリアライザーのインスタンスフィールドとして持ち、同一 XML の排他ロック（`ProfileXmlStore.XmlIoLock`、ガードレール 5.1）の下で使う。呼び出しごとのローカル化などの再設計はしない（挙動の変更になり、移動の範囲を超えるため）。
- B: ローカル変数に変える。 → スレッド安全性は上がるが、補助メソッド（`LoadLegacyExtraSettings` など）の引数が変わり、リスクが増える。

### Q5. パス・環境の参照（`Global.appdatapath`／`exedirpath`／`exeversion`）
- 推奨: **A. 新しい型は `IPathService`（と必要なら `IEnvironmentService`）をコンストラクターで受け、`Global` のこれらの窓口を使わない**。`BackingStore` のパスのフィールド（`m_Actions` など）は 10d まで残す。これにより `Global` 参照が 10c でも減る（移動先で 0 を目指す）。
- B: 移動した本体では `Global.` をそのまま使う。 → 10d で再度書き換えることになる。

### Q6. プリセット（`LoadBlank*`／`LoadDefault*`）の扱い
- 推奨: **A. 10c の最後の Wave で `ProfilePresets` に移す**（`ProfilePresetLoader` がこれを呼ぶ）。状態の代入が中心で、ゴールデン（読込後の状態）で固定できる。
- B: 10c では移さない。 → `BackingStore` に約 540 行の状態の代入コードが残る。

### Q7. Wave の構成
- 推奨: **A. 次の 7 Wave。各 Wave の後にユーザーがビルド・テストを実行する**。
  - W1: ゴールデンテストの整備（テストとヘルパーの追加のみ。本体は変えない）。**ユーザーがゴールデンを生成してコミットするまで W2 に進まない**。
  - W2: 低結合の 3 つ（`SpecialActionsXmlSerializer`、`LinkedProfilesXmlSerializer`、`ControllerConfigsXmlSerializer`）と、`Global` のシムの削除。
  - W3: アプリ設定（`AppSettingsXmlSerializer`。`Load`／`Save`／`LoadFakeExeName`）。
  - W4: プロファイルの保存（`LegacyProfileXmlSerializer` の保存側: `SaveProfile`／`SaveAsProfile`／`SaveLegacyExtraSettings`）。
  - W5: プロファイルの読込（同 読込側: `LoadProfile`／`LoadLegacyExtraSettings`／後処理と補助）。
  - W6: プリセット（`ProfilePresets`）。
  - W7: 文書。
- B: プロファイルの保存と読込を同時に移す。 → 差分が約 2,900 行になり、失敗時の切り分けが難しい。

### Q8. 実機確認 U-20（設定ファイルの互換）
- 推奨: **A. 10c の後に、実機（または利用者の設定のバックアップ）での確認項目 U-20 を追加する**。内容: 既存の設定フォルダー（`Profiles.xml`、`Profiles/*.xml`、`Actions.xml`、`LinkedProfiles.xml`、`ControllerConfigs.xml`、`Auto Profiles.xml`）で起動し、設定が従来どおり読め、保存した結果が元のファイルと差がないこと（`diff`。日時のコメント行を除く）。ハードウェアは不要。実施の時期は、他の U と同様に先送りして 10e の総合検証でまとめて行ってよい（ユーザー判断）。
- B: 追加しない。 → 単体テストが届かない実データでの互換が未確認のまま終わる。

---

## 4. Wave 計画（Q1〜Q8 が推奨どおりの場合）

- **W1: ゴールデンテスト**
  - `BackingStoreStateDumper`（テスト用のヘルパー）と、`GoldenFile`（ファイルがなければ生成して失敗する仕組み）を追加。
  - テスト: `Phase7Step10cGoldenProfileTests`（保存・読込）、`…AppSettingsTests`、`…ActionsTests`、`…LinkedProfilesTests`、`…ControllerConfigsTests`、`…PresetsTests`。
  - 手順（ユーザー）: ビルド→テスト実行（ゴールデンが生成されて失敗）→生成物を確認してコミット→再実行して成功。
- **W2: 低結合の 3 つ**
  - 新しい 3 つの型を `DS4Control/Services/` に作り、本体を移動。`ServiceRegistration` に登録。`SpecialActionRepository`（`Global.SaveAction` などの呼び出しを置き換え）、`ProfileRepository`、`ProfileXmlStore`、`App`、`ActionsLoadedRuntimeReset` との接続（`ActionsLoaded` イベントの発行元の扱いは W2 の冒頭で確認）を切り替え。
  - `Global.SaveAction`／`SaveActions`／`RemoveAction`／`LoadActions` を削除。テストの呼び出しを更新。
- **W3: アプリ設定**
  - `AppSettingsXmlSerializer`。`AppSettingsService`／`ProfileXmlStore` の呼び出しを切り替え。`Global.CompileVersionNumberFromString`（`BackingStore` で 2 件）と `configFileDecimalCulture` などの扱いを確認（バージョン比較の純粋関数は移動先で使う型に置く）。
- **W4: プロファイルの保存**、**W5: プロファイルの読込**
  - `LegacyProfileXmlSerializer`。W4 で保存側、W5 で読込側と補助（`ResetProfile`、`PostLoadSnippet`、`CheckOldDevicestatus`、`PortOldGyroSettings`、`ResetMouseProperties`）を移す。`ProfileXmlStore`／`ProfileRepository` の呼び出しを切り替え。リフレクションを使う既存テストを新しい型に合わせて更新。
- **W6: プリセット**
  - `ProfilePresets`。`ProfilePresetLoader` を切り替え。
- **W7: 文書**
  - 台帳 §15、モデル図（03、04）、Status、完了報告書、チェックリスト U-20。ラチェットの基準値は各 Wave で、新しいファイルへ引き継ぐ（合計を増やさない）。

## 5. リスクと対策

- 利用者の設定ファイルの破損・欠落: ゴールデン（保存結果の全文と、読込後の状態）で固定する。要素の順序・書式（インデント、改行、コメント行の文言）は変えない。日時を含む行は、比較時に正規化する。
- `store.` の修飾の取りこぼし・誤修飾: スクリプトで、移動対象の識別子と同名のローカル・引数を検出する。ビルドで型検査（取りこぼしは未定義の識別子のエラーになる）。同名の別物（例: 引数 `device` とメンバー）の取り違えは、ゴールデンで検出する。
- 同一 XML の排他ロック（ガードレール 5.1）: `XmlIoLock` を持つ `ProfileXmlStore` の構造を変えない。シリアライザーはロックを持たない（現状と同じ）。
- 件数を固定するガード（10a・10b の教訓）: 実装前に `Assert.Equal(<件数>, …)` を含む参照ガードを洗い出す。特に `BackingStore` の `Global` 参照（27）を新しいファイルへ引き継ぐ（合計を増やさない）。
- リフレクションを使う既存テスト（`m_Xdoc`、`ResetProfile`）: W4・W5 で新しい型を対象に更新する。
- ホットパス: 触れない（設定の読み書きのみ。入力処理の経路ではない）。ただし `LoadProfile` はプロファイル適用時（ガードレール 5.2 の Halt の下）に呼ばれるため、呼び出し順を変えない。
- ガードレール: 5.1（同一 XML 排他）、5.2（適用時 Halt）。5.4（On-Demand パス評価）: 新しい型は、パスの決定後に作られるよう、`Func` 経由またはコンストラクターの依存を `IPathService` に限る。

## 6. 完了条件

- 7 Wave が完了し、ゴールデンテストが成功（ユーザーが実行）。`BackingStore` から永続化のメソッドが無くなり、`BackingStore.cs` が約 1,500 行になっている（実測 1,660 行）。
- `Global` のシム（`SaveAction` ほか 4 名）が削除されている。新しい型に `Global.appdatapath`／`exedirpath`／`exeversion` の参照がない。ラチェット（合計 93 を超えない。見込みは移動先での減少）。
- 文書の更新（台帳、モデル図、Status、完了報告書、U-20）。

## 7. 持ち越し

- 10d: `Global.store`、`BackingStore` に残るパスのフィールド（`m_Actions` など）、`ProfileSettingsServiceInstance` 経由の設定の窓口、環境・パスの窓口、`using static DS4Windows.Global;`、`CURRENT_DS4_CONTROLLER_LIMIT`（`BackingStore` の分）。`configFileDecimalCulture` と設定の定数（`CONFIG_VERSION` ほか）の最終的な置き場所（W3 で移動先を決め、残りは 10d）。
- 10e: `CURRENT_DS4_CONTROLLER_LIMIT`（画面・サービスの分）、`AppHost` の暗黙構築、`Mapping` の依存の整理、`ColorUtil` の孤立コード、総合検証（U-11〜U-20）。
