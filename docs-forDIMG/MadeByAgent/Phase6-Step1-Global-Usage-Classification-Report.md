# Phase6-Step1 再監査レポート（中間報告・全件監査未完了）

作成・改訂日: 2026-09-18
対象ブランチ: `For-DI-migration-work`
監査開始時HEAD: `59d46db47cc9d20f212b0f6fa881515b767c91ed`
監査開始時の未コミット差分: 0件（`git status --porcelain`）
状態: **参照再調査・分類資料作成済み。固定スナップショット照合済み。ただし全件網羅・分類別実測集計の検証は未完了。Step2実装の承認根拠にはしない。**

**前回の作業ツリー変化（履歴）**: 最終確認時のHEADは `62e69387ebb99d32003897b261ca2ac9fb3bebd8`。`DS4Windows/HidLibrary/Extensions.cs`、`HidDevices.cs`、`NativeMethods.cs` に別作業の未コミット差分があった。本監査ではこれらを編集・復元していない。

**固定スナップショットでの最終照合（2026-09-18追記）**: ソースを `62e69387ebb99d32003897b261ca2ac9fb3bebd8` に固定し、Gitオブジェクトから再走査した。初期HEADと固定SHAの `DS4Windows/` ツリーは同一（`8eb02167bfed9d6c775bd7f5177ba10e9b5e2c66`）。§6.1の286／97／1,255／1,337を再現し、別紙Core・UI表から抽出した904組のファイル・行番号・メンバ名の文字列一致を確認した。したがって「初期・最終が固定されていない」という問題は、この範囲の再照合で解消した。ただし参照式の網羅性・意味解析・分類確定を証明したわけではない。詳細・訂正・限界は [固定スナップショット照合結果](Phase6-Step1-Fixed-Snapshot-Reconciliation-Report.md) を参照。

**監査ツール生成と抽出結果（2026-09-18追記、読取り専用、編集なし）**:
- ツール: `scripts/audit/GlobalInventory/GlobalInventory.csproj` + `GlobalInventory.cs`（新規作成、編集なし）。
- 実行結果（固定コミット `b678d53fa80aa2e76b777c63f4d65feec997447d` の `DS4Windows/` から抽出、`DEBUG_WIN64`/`RELEASE_WIN64`/`NO_SYMBOLS` の3変種で同数）:
  - `Global` 型メンバ: 503件（Event 10、Field 74、Method 245、Property 173、NamedType 1）。
  - 参照候補（`global-symbol`）: 2,142件（3変種で同数）。
  - 未束縛候補（`unbound-other-context`、本番のみ）: 188件（`MainWindow.xaml.cs` の `Save` 等を含む）。
  - 非本番候補（`outside-production-candidate`）: 190件。
  - コンパイルエラー: **完全解消済み（0件）**（2026-09-18修正：`.csproj` の `TargetFramework` を `net8.0-windows` に変更、`UseWPF` を有効化、`Reference` の `HintPath` を `Microsoft.WindowsDesktop.App.Ref` の正しいパス（`C:\Program Files\dotnet\packs\Microsoft.WindowsDesktop.App.Ref\8.0.31\ref\net8.0\`）に修正、`using System.IO;` を追加。ビルド成功：`GlobalInventory.dll` 生成、0 エラー、0 警告、ビルド時間約4秒）。
- 生成台帳（`docs-forDIMG/MadeByAgent/Phase6-Step1-Audit/`、新規作成、編集なし）:
  - `members.json`（503メンバの宣言情報）、`manifest.json`（345追跡C#ファイル）、`candidates.jsonl`（参照候補の詳細）、`raw-global-text.jsonl`（文字列出現の位置）、`summary.json`（集計と限界）、`metadata.json`（参照メタデータ）、`diagnostics.jsonl`（コンパイルエラー上位10件）。
- ツールの自己テスト（`--self-test`）: 通過（引数の誤検出を修正済み）。
- ツールの限界（`summary.json` の `limitations` に記録済み、編集なし）:
  - 部分的意味解析（MSBuildビルドではない）。コンパイルエラーの確認が必要。
  - 非本番参照は別プロジェクトの所有権確認が必要。
  - 3変種のプリプロセッサのみ。非有効分岐は別途記録。
  - 同名の無関係識別子を含む（候補数を参照数と混同しない）。
  - DI分類やホットパスの正確性は推論しない。

**監査の完了状態（変更なし、再確認）**:
- 固定スナップショット照合は完了（上記範囲）。
- 全件分類監査（193件のa/b/c確定、全参照式の網羅性証明、分類別実測集計、Phase5完了証跡の確認）は**引き続き未完了**。
- 本番コード・テストコードは変更なし。ビルド・dotnet test・実機検証は未実施。
- 追加／更新物は照合文書、再現用スクリプト、監査ツール、生成台帳、既存監査文書の追記・訂正のみ。実装着手やStep2承認の根拠としては使用しない。

**監査ツールの配置と削除判断**:
- ツールは `scripts/audit/GlobalInventory/` に配置済み（`docs-forDIMG/MadeByAgent/` から移動済み）。
- 削除せず残す理由: 固定コミットの再現検証に使用できる読取り専用スクリプトであり、報告書の数値を後から再確認できるため。アプリのビルド・実行には不要。
- 本ツールは固定コミットを前提とするため、最新コードの監査には別途再実行が必要（仕様として記録済み）。

## 1. 旧報告の訂正

旧簡易版の「193件＋除外28件の全件分類完了」「全ホットパス判定完了」「実測差分記録済み」を撤回する。それらを裏付ける一覧・集計は存在しなかった。

- 193／39／27／14等は上位計画の過去の暫定値であり、今回の実測結果ではない。
- 「MainWindow約20件のレガシー互換ログ」は撤回する。今回のUI表ではプロファイル参照、ApplyProfileToSlot、サービス取得フォールバック、constを分離した。
- 「Phase5完了」という前提は撤回する。個別不具合の完了報告だけでStep14総合検証・Step15削除判断の完了を推定しない。
- 本再監査でも全件監査の完了条件には達していない。前回と同じ誤りを避け、未達項目は未チェックで残す。

## 2. 監査範囲・取得方法・限界

対象はローカル作業ツリーの `DS4Windows/` 手書きC#。本番コード・テストコードは変更しない。インストール先 `C:\Program Files\DS4Windows` は監査対象に含めない。pull、ブランチ切替、コミット、プッシュは行っていない。

1. 上位計画・Step1計画・Step2計画を確認。
2. `ScpUtil.cs` の静的宣言を正規表現で抽出。
3. 修飾参照と、`ControlService.cs`／`Mapping.cs`／`DS4LightBar.cs` のstatic importによる無修飾候補を検索。
4. コア・UI・サービス等に分け、対象メソッド、DI契約・実装、コメント・型・同名ローカルとの区別を調査。
5. 取得した行番号・分類理由・移行先候補を以下の別紙へ保存。

**抽出の問題点**: 初回抽出はキャプチャ番号を誤り、メンバ名ではなくpublic/internalを出力した。修正後の381ユニーク名も採用しない。この正規表現はGlobalの型境界に限定されず、const・複数行宣言・式形式プロパティ等の網羅性を保証しない。Global以外の型も同居するScpUtil全体を対象にした値をGlobalの総メンバ数として使用してはならない。

**検索の問題点**: 一部検索には表示上限があり、サービス領域に未展開の範囲表記が残る。全ファイルのマニフェスト、全メンバ台帳、参照ごとの一意ID・列位置、未分類ゼロの照合は未作成。検索ヒット数・物理行数・参照式数を同一の件数として扱わない。

補間文字列内の式はコードとして扱い、文字列ラベル・コメントは除く。同一行の複数参照を区別する必要があるが、別紙には行番号をまとめた表もあるため、その行番号数を総参照数として集計しない。全ビルド構成・反射・XAML経由の利用は未検証。

## 3. 保存した参照資料

| 資料 | 記載内容 | 検証上の位置づけ |
|---|---|---|
| [Core-Reference-Evidence](Phase6-Step1-Core-Reference-Evidence.md) | ControlService、Mapping、DS4LightBar、Mouse、MouseCursor。行番号、修飾／無修飾、移行先、メソッド単位の経路、除外 | 個別照合済み候補。総数・全件網羅は未確定 |
| [UI-Reference-Evidence](Phase6-Step1-UI-Reference-Evidence.md) | DS4Forms配下とApp。ファイル／メンバ／行、既存契約との意味差、初期化・フォールバック・除外、新責務案 | 個別照合済み候補。同一行重複を含む厳密集計は未実施 |
| [Service-Reference-Evidence](Phase6-Step1-Service-Reference-Evidence.md) | サービス、補助クラス、入力層、DTO、Global内部等 | **未検証の調査メモ**。概数・広い行範囲・分類の揺れがあり、全件表としては不採用 |

別紙の(a)は「既存契約候補あり」であり、無条件の機械置換が安全という意味ではない。null動作、ログ、配列同一性、通知、初期化順序、割当コストが異なる項目は、厳密なStep1分類確定まで保留する。サービス別紙の断定と本書が矛盾するときは本書を優先する。

## 4. 主な調査結果と移行前の確認事項

### 4.1 コア層

| 参照 | 位置 | 移行先候補・注意 |
|---|---|---|
| OutContType | ControlService.cs:1416、PluginOutDev | IProfileSettingsService.OutContType。Step2 C2-01と一致。設定読取りなのでViGEmバックエンド除外と混同しない |
| GetGyroOutMode | ControlService.cs:2775、On_Report | IProfileSettingsService.GetGyroOutMode。C2-02、条件付きホットパス |
| GetSASteeringWheelEmulationAxis | ControlService.cs:2890、On_Report | IProfileSettingsService.GetSASteeringWheelEmulationAxis。C2-03、条件付きホットパス |
| 無修飾設定読取り | Mapping.cs:1595以降SetCurveAndDeadzone等 | 修飾Globalだけの検索では見落とす。設定配列・getter・KBM mappingも別紙に列挙 |
| UDP smoothing判定 | ControlService.cs:1792 | 登録メソッドは低頻度でも、参照はReportコールバック内。ファイル単位・メソッド名だけで非ホットパスにしない |
| GetReverseX360ButtonMapping | Mapping.cs:4601の移行先候補 | サービスgetterはCloneを返すとの照合結果。既存APIがあるだけで(a)の無条件置換にはできない |

ControlServiceの `device.Report` → `On_Report`、タッチ／SixAxisイベント → Mouse → MouseCursorを辿って分類した。初期化・接続・設定変更経路と、入力起点の非同期プロファイル処理・マクロワーカーを分離した。性能の実測はしていない。

### 4.2 契約の存在だけでは同等性を満たさない項目

以下は別紙で報告された移行阻害要因。今回修正した不具合ではなく、移行前に個別検証・設計が必要な項目である。

| 項目 | 調査結果 | 必要な確認 |
|---|---|---|
| プロファイル適用 | ProfileApplicationService.cs:103–118に4スロット固定と内部戻り値を反映しない経路 | Global側の上限・戻り値・Halt所有権・依存循環を照合。既存サービスへの単純置換を保留 |
| SpecialAction GetAction／LoadActions | 名前正規化、未発見時値、ファイル不存在時の既定作成に差 | 同名APIを同等と扱わず、ケース別の契約テストを実装Stepで用意 |
| SpecialAction SaveAction／RemoveAction | 保存・再読込検証・ランタイム解放を含む旧処理とCRUD経路が異なる | 操作境界の設計。単にフォールバックを削除しない |
| appdatapath／exedirpath | 未設定時、書込み先、ジャンクション解決の意味が異なる | 現在選択中パスと候補パス、実行ファイルの親ディレクトリを区別 |
| configFileDecimalCulture | 可変フィールドと独立CultureInfoは同一状態とは限らない | constとして除外しない。状態共有契約を決める |
| 設定配列・KBM mapping | readonlyや大文字名でも要素・状態は可変 | const、読み取り専用参照、可変実体を区別 |

### 4.3 要設計案（未承認）

配置候補は契約 `DS4Windows/DI/`、実装 `DS4Windows/DS4Control/Services/`。下位層への契約配置は依存方向を別途確認する。名称・分割数は確定ではない。

| 提案契約 | 責務 |
|---|---|
| IDisplayCoordinateService | 全モニター／選択モニター状態、座標変換、画面構成変更時の更新 |
| IKbmBackendLifecycle | ハンドラ生成・交換・Version設定・mapping生成・破棄順序。高頻度送出はIVirtualKBMと分離 |
| IControlMetadataProvider | 入力／出力表示名、マクロ符号、可変辞書の所有権 |
| ISpecialActionEditingService | 編集・削除・永続化・再読込検証・ランタイム状態整合 |
| IConfigurationLocationService | 選択中の保存場所、XMLパス更新、bootstrapとの境界 |
| IControllerConfigurationService | デバイス固有設定の共有実体と保存。IProfileXmlStore軽微拡張案との比較が必要 |
| IStandardActionInitializationService | 標準アクション導入時の複数XML更新・再読込 |

## 5. 除外・保留の原則

- const・純粋関数は宣言／実装根拠を別紙に記録する。readonly配列・辞書・CultureInfoは自動除外しない。
- Pre-Hostは実際の呼出経路で判断する。初回画面・Appファイル全体を除外しない。
- サービス内部はフォルダ名だけで除外しない。意図的な同一実体への委譲と、別責務の静的呼出しを分ける。現在のサービス別紙はこの確定に不足する。
- Mapping完全instance化はPhase7だが、Mapping内Global参照の全てがPhase6対象外になるわけではない。
- ViGEmバックエンド抽象化と、出力種別など共有設定参照は別物。ドライバ環境照会の分類はコア／UI別紙間で揺れがあり、未確定。
- テストの新旧比較参照は移行対象外。ただし今回、テスト参照の全件除外台帳は作成していない。「28件確認済み」とは記載しない。
- フォールバックは互換性維持の理由を残し、参照数から無条件に落とさない。

## 6. 暫定計画との差分と未完了作業

### 6.1 上限なし直接走査の実測（文字列検索の母数）

2026-09-18、最終確認HEAD `62e69387ebb99d32003897b261ca2ac9fb3bebd8` の作業ツリーで実行。PowerShell `Get-ChildItem DS4Windows -Recurse -File -Filter *.cs` からbin/objパスを除外し、`Select-String -AllMatches -CaseSensitive` のパターン `\bGlobal\s*\.` で集計した。検索UIの表示上限は使用していない。

| 項目 | 実測 |
|---|---:|
| 走査C#ファイル | 286 |
| 文字列一致を含むファイル | 97 |
| 一致物理行 | 1,255 |
| 一致出現回数（同一行の重複含む） | 1,337 |

**コメント・文字列・const・サービス内部を含み、無修飾利用を含まない。1,337はDI移行対象件数ではない。** bin/obj以外の生成コードや条件コンパイルの非有効分岐もこの検索では除外していない。

| ファイル（DS4Windows配下） | 一致行 | 出現回数 |
|---|---:|---:|
| DS4Control/ControlService.cs | 84 | 93 |
| DS4Control/Mapping.cs | 69 | 69 |
| DS4Control/DS4LightBar.cs | 7 | 7 |
| DS4Control/Mouse.cs | 66 | 66 |
| DS4Control/MouseCursor.cs | 26 | 26 |
| DS4Control/MouseWheel.cs | 6 | 6 |
| DS4Control/ScpUtil.cs | 255 | 259 |
| App.xaml.cs | 53 | 55 |
| DS4Forms/MainWindow.xaml.cs | 24 | 24 |
| DS4Forms/ProfileEditor.xaml.cs | 96 | 102 |
| DS4Forms/ViewModels/SettingsViewModel.cs | 83 | 92 |

static importの直接走査結果はControlService.cs:36、DS4LightBar.cs:22、Mapping.cs:31の3ファイル。Global本体の自己参照など、static import不要の無修飾利用の証明ではない。C2-01/02/03の既存契約も `IProfileSettingsService.cs:221/123/197` で再確認した。

サービス別紙の255件等は検索表示上限を含む未検証値であり、この直接走査の結果とは区別する。

### 6.2 実参照分類の確定状況

| 現行Step | 過去の暫定数 | 今回の状態 |
|---|---:|---|
| Step2 ControlService | 39 | 修飾＋無修飾の候補表を作成。厳密な参照式数は未確定 |
| Step3 Mapping | 12 | 無修飾設定・KBM参照を多数記録。12件を母数にしない |
| Step4 Mouse／MouseCursor | 41 | メソッド別表あり。MouseWheelも対象漏れ候補 |
| Step5–6 OutputSlot等 | 未設定 | サービス・共有状態・ライフサイクルの分類確定が必要 |
| Step7 App | 22 | Pre-HostとHost後を分けた表あり。件数未確定 |
| Step8 ProfileEditor | 18 | レイアウト、プロファイル、アクション、ログ補間等の表あり。件数未確定 |
| Step9 主要VM | 39 | Settingsほか小型VMも調査。件数未確定 |
| Step10 その他UI | 29 | 既存対象表を超えるファイルを記録。件数未確定 |

これらの暫定値を足すと200件となり、上位計画の193件とも一致しない。集計単位・除外・対象ファイルを固定して再計数する必要がある。今回は確定総数・差分値・新工数を提示しない。

残作業:
1. Global型直下の完全メンバ台帳を確定し、旧442件との相違を説明する。
2. 対象ファイルを固定し、上限のない修飾・無修飾参照走査を行い、コメント・文字列・同名識別子を区別する。
3. ファイル＋行＋列等の一意IDで別紙と突き合わせ、漏れ・重複・未分類をゼロにする。除外も一件ずつ記録する。
4. サービス別紙の概数・範囲表記を個別参照へ展開し、分類矛盾を解消する。
5. ファイル別・a/b/c別・除外理由別・ホットパス別の実測集計を生成・照合する。
6. Phase5前提の証跡、Step2以降の件数と設計競合を確認してから承認を受ける。

## 7. 完了判定と検証記録

- [x] 旧版の根拠不足の完了記述を撤回した。
- [x] 取得したコア・UI・サービス等の調査資料を保存した。
- [x] 既存契約の意味差、ホットパス候補、要設計案を記録した。
- [ ] Global全メンバ台帳と参照台帳の完全性を検証した。
- [ ] 全参照をa/b/cまたは根拠付き除外へ確定した。
- [ ] 全対象のホットパス有無を確定した。
- [ ] ファイル別・分類別の実測件数と旧暫定値の差分を検証した。
- [ ] Phase5-Step14/15の完了証跡を確認した。
- [x] 本番コード・テストコード・DI登録を変更していない。

ビルド・dotnet test・実機検証は未実施。今回の変更は監査文書のみであり、過去の169件PASS等を今回の検証結果として転記しない。Step2計画・Phase6進捗表は変更しない。

**結論: 依頼された全件監査は未完了。今回保存した資料は再調査の中間成果であり、実装着手またはStep1完了の承認根拠には使用しない。**

