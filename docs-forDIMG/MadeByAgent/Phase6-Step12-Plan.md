# Phase6-Step12 計画書: 呼出元0件旧静的シムの安全防壁付き物理削除とPhase6最終完了判定

作成日: 2026-09-09  
改訂日: 2026-09-18（Step2〜Step10確定スコープ・Step11実機総合検証成果統合・選択肢2［安全防壁付き物理削除］採用・全面拡充改訂）  
状態: Wave 1〜6 実装済み（2026-10-17）。最終のビルド・テストと、実機確認（U-8・U-9）の結果待ち  
**2026-10-14 注記（文書整合）**: 本書の「Phase7 引き継ぎ101箇所」「Pre-Host 11箇所」は 2026-09-18 時点の記載で、現行の正と異なる。(1) Step3 は 2026-09-21 に方針を転換し、`Mapping.cs` の実参照150件を全件解消した（2026-09-24 完了）。防壁2の対象は「101箇所」ではなく、`Mapping` の静的 `profileSettings` 束縛（方針P2、TODO 付き）と温存3件である（`Phase6-Step3-Plan.md` §3.8）。(2) Pre-Host 領域は 2026-09-25 の Step7-0 で12箇所に再集計した。(3) Step7 決定6＝N、Step5 の実行時状態配列、K7-1 などの登録は §4 を参照。着手時（Step12-0）に、現行コードで削除候補の台帳を作り直すこと（`Phase6-Status.md` §6.3）。  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.12  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（Core層・Mapping層 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md`（UI層・ViewModel層 監査記録）  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step10-Plan.md`（各ステップ確定実装計画書）  
  - `Phase6-Step11-Plan.md`（第1層自動テスト ＆ 第2層実機総合検証計画）  

---

## 0A. Step12-0 の結果: 台帳の作り直し・決定事項の提示（2026-10-17、実装なし・承認待ち）

HEAD は `e0bc8258`（作業ツリーはクリーン）。以降の §0〜§6 は 2026-09-18 版のままで、現行コードと食い違う箇所がある。食い違いの一覧と改訂案は §0A.5。本節の数値は、2026-10-17 時点の現行コードを走査して得た。

### 0A.1 集計の方法

- 走査の道具: Python のスクリプト（`scripts/audit/Step12/` に保存。`audit.py`＝コメント除去と宣言の抽出、`count.py`＝`Global`、`countb.py`＝`BackingStore`、`cascade.py`＝連鎖、`gen.py`＝Wave 割り当て）。`dotnet` は使っていない。
- 対象: `DS4Windows`、`DS4WindowsTests`、`StandaloneTests` の全 `.cs`（`obj`／`bin` を除く）。コメントは除いた。文字列リテラルの中は数える（ガードテストの文字列の誤検出は、削除の Wave ごとに目視で確認する）。
- 参照の数え方（`Global` の各メンバーごと）:
  - 修飾参照: `Global.名前`、`DS4Windows.Global.名前`。
  - 非修飾参照: `using static DS4Windows.Global;` を持つファイル（コードは `Mapping.cs`、`DS4LightBar.cs` の 2 件のみ。テストの該当行は文字列）の中の、単語としての名前。
  - 内部参照: `Global.cs` 内の、別メンバーからの参照。型名と同名のメンバー（`OutContType` など）は内部参照として多めに数える（安全側）。
  - 連鎖: 「外部の呼び出し元が 0 件で、内部の参照元がすべて同じく不要」なメンバーは、不要とみなす（固定点まで繰り返し）。
- 確認した「抜け道」:
  - XAML の `x:Static`／`Binding`: `Global` への参照は 0 件（`ControllerRegisterOptionsWindow.xaml` の "Global" は文字列「JoyCon Global Options」）。
  - リフレクション: `typeof(Global)` は、イベントの送信元（sender）の値としてだけ使われている（`Global.cs` 2、テスト 3）。`GetType("…Global")`、`nameof(Global.…)`、別名（`using X = Global`）、継承は 0 件。
  - `partial class Global` は無い（Global は `Global.cs` の 1 ファイル）。
- 精度の限界: `BackingStore` はインスタンスなので、名前だけで数える（`.名前` と初期化子の `名前 =`）。0 件の判定は確実だが、1 件以上の判定は偶然の同名で過大になる。

### 0A.2 `Global` の結果（487 名、宣言 494 件）

- 外部の呼び出し元が 0 件: 286 名。うち 33 名は、生きているメンバーの内部部品（フォールバック実体、`m_Config`、ViGEm の内部関数など）で、削除できない。
- **不要（削除候補）: 253 名、宣言 258 件。** `Global.cs` の非空行 836 行（全体 3,472 行の約 24%）。
- 生存: 234 名。内訳（外部の呼び出し元の分類。台帳 `Phase6-Step11-Global-Residual-Ledger.md` の A〜H と対応）:
  - D（サービス実装の薄いシム）のみが呼ぶ: 144 名。§2.1 の「薄いシム」の委譲先。サービスが `Global` の実体を呼ぶ構造のため、呼び出し元は 0 件にならない。Phase7（実体のサービス側への移管）まで温存。
  - G（第 1〜2 層・Actions・Phase7）を含む: 32 名。温存。
  - E（Composition Root・Pre-Host・MainWindow／ProfileEditor）を含む: 12 名。温存。
  - UI（定数・名前表のみ）: 7 名。F（DTO・永続化）: 4 名。複合（D＋F、B＋D＋F）: 2 名。温存。
  - 内部部品のみ（外部の呼び出し元 0、内部の生きたメンバーが使う）: 33 名。温存。

#### 削除禁止の 5 領域との突き合わせ

- 防壁 1（`BackingStore`）: 不要 258 件のうち `BackingStore` 自身のメンバーは 0 件（`Global` の転送アクセサだけ）。`BackingStore` 側は §0A.3。
- 防壁 2（Phase7 持ち越し）: 不要 258 件のうち、`Mapping.cs`・`DS4LightBar.cs` の非修飾参照、`SyntheticDispatcher`、第 1 層（`DS4Device` 系）が参照するものは 0 件。`Mapping.cs` の静的 `profileSettings` 束縛（方針 P2、TODO 付き）と温存 3 件は、`Global` のメンバーではないので影響を受けない。
- 防壁 3（Pre-Host）: 該当 0 件（`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`、`FindConfigLocation`、`multisavespots`、`SetCulture`、`UseLang` などはすべて生存）。
- 防壁 4（定数・純粋計算）: 該当 1 件（`DS4_STICK_AXIS_MIDPOINT`）。→ 要判断（下記）。
- 防壁 5（ViGEm クライアント依存）: 指定の 4 つ（`RefreshViGEmBusInfo`、`IsRunningSupportedViGEmBus`、`vigembusVersion`、`vigemInstalled`）は生存。隣接する `CheckForDevice` は不要だが、防壁の対象ではない（下記）。

#### 分類結果（不要 258 件）

- **削除可: 256 件**（Wave 割り当ては §0A.6、名簿は §0A.8）。§2.4 の判断は、すべて「使うべきでないもの」:
  - ProfileSettings 系の互換アクセサ 150 件: 呼び出し側の切り替えは Step2〜10 で完了済み。DI サービス（`IProfileSettingsService` など）という正規の経路があり、`Global` 側は重複する並行実装。
  - `*Instance` のフォールバック群 19 件: DI コンテナ経由が正規の経路。本体の呼び出し元は 0 件で、テストが「DI または代替」の取得口として使っているだけ。
  - 上流由来の孤立コード 25 件: 4 つのイベント（`ControllerStatusChange`、`BatteryStatusChange`、`ControllerRemoved`、`DeviceStatusChange`）と発火メソッドは、上流の 1.4.x 時代に作られ、購読側の記述が上流の `d02317b5`「Minor code clean up」で消えて以降（`git log -S` で確認）、購読者も発火元も無い（DI 化とは無関係）。引数型 `BatteryReportArgs`、`ControllerRemovedArgs`、`DeviceStatusChangeEventArgs` の 3 型も、これらだけが使うため連鎖して不要（各 1 ファイル、`git rm`）。`SerialChangeArgs`／`DeviceSerialChange` は `DeviceStateService` が使うので生存。ほかに旧フィールド（`m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`）、`DS4Mapping`、`DownloadLang`、`CheckForDevice`（ViGEm の確認は `CheckForSysDevice` 系に移っている。`ee5ff50e` ほか）、重複実装の `NormalizeActionName`（正本は `Util.NormalizeActionName`）、呼び出し元 0 の旧 getter（`getUseExclusiveMode`、`getQuickCharge`、`GetMinToTaskbar`、`getFlashWhenLate`、`getFlashWhenLateAt`、`IsUsingUDPServerSmoothing`）、`DebouncingMsHasChanged`。
  - `BackingStore` 直転送の薄いラッパー 62 件: `GetDS4Action`、`UpdateDS4CSetting`、`SaveProfile` など。呼び出し元は 0 件で、DI 側に正規の経路がある。
- **要判断: 2 件**
  - `DS4_STICK_AXIS_MIDPOINT`（定数 128、呼び出し元 0）: 防壁 4 の定数。推奨は**温存**（害が無く、削除の利益が無い）。
  - `getDefaultX360ControlBinding`（`defaultButtonMapping` の添字参照の 1 行）: 呼び出し元 0。推奨は**削除可**（表 `defaultButtonMapping` は生存するので、情報は失われない）。
- **温存: 0 件**（防壁にかかるものは、上の「要判断」の 1 件だけ）。

#### 注意が必要な個別事項

- `GetProfileActionIndexOf`（`Global` 側）: K2（未検出時に 0 を返す。`Phase6-Status.md` の持ち越し項目 K2）の対象の片方。`Global` 側を消しても K2 は解消しない（`ProfileActionProvider` 側に残る）。K2 は別途の判断のまま。
- `*Instance` のフォールバック群（Wave 3）: 静的初期化子（`new SpecialActionRepository(...)`、`new PathService()` など 5 件）が消えるため、`Global` の静的初期化の順序が変わる。K7-1（ホストの暗黙構築）には、`fallbackProfileRepository` が Step10-4-2b で遅延生成に変わった現在、直接の影響は無い見込み。ただし**実機で起動ログ（`Host initialized with runtime parser` が出ること）を確認する**。
- 計画書 §2 の代表例のうち、実際に不要なのは 7 件（`getUseExclusiveMode`、`getFlashWhenLate`、`getFlashWhenLateAt`、`getQuickCharge`、`IsUsingUDPServerSmoothing`、`containsCustomAction`、`containsCustomExtras`）だけ。`setXxx` 系 4 件と `UseAbsoluteEDID` は、そもそも存在しない。残りの約 23 件（`isUsingOSCServer` ほか）は、サービスの委譲先として生存している。

### 0A.3 `BackingStore` とその他の静的シム

- `BackingStore`（6,869 行、348 名）:
  - 防壁 1 のとおり、フィールド・配列・プロパティは削除しない。サービス（`ProfileSettingsService`、`AppSettingsService` ほか）が内部で直接使っているため、`Global` の不要分（上の 258 件）を消しても、新たに 0 件になるのは 14 名だけ（連鎖の計算結果）。
  - その 14 名の内訳: 定数 4（`DEFAULT_CHECK_EVERY_UNIT`、`DEFAULT_CHECK_EVERY_VALUE`、`DEFAULT_SA_OUTPUT_CURVE_ID`、`DEFAULT_STICK_OUTPUT_CURVE_ID`）→ 防壁 1・4 のため**温存**。メソッド 10: `GetDS4Action`、`GetDS4Extra`、`GetDS4KeyType`、`GetDS4STrigger`（`Global` の不要ラッパー経由でのみ到達）、`GetGyroOutModeString`、`GetGyroOutModeType`（private。内部でも未使用）、`SaveOld`、`PostProcessLoad`、`UsedSavedProfileString`、`RebuildProfileActionDicts`。
  - 判断: **要判断。推奨は温存して Phase7 へ**（`SaveOld`／`PostProcessLoad`／`RebuildProfileActionDicts` は、2025-11-11 の `576426b8`「3.9.9以前のprofile.xmlと互換性のある保存読込メソッドを正として残す」に絡む、旧形式の保存・読込の設計判断で、DI 移行の作業物ではない。ユーザーが別途に方針を決める項目）。
- `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`、互換ラッパー）: 呼び出し元は `Mapping.cs:86` の `AppHost.GetService<IVirtualKBM>()` の 1 件だけ（`Mapping.cs` は Phase7 の持ち越しなので、この呼び出しは変えない）。`Host`、`CreateHost`、`GetService(Type)`、`Dispose`、private フィールド `_loggedResolvedTypes` は、呼び出し元 0 件 → **削除可**（Wave 5）。
- `DS4WinWPF.AppHost.Dispose()`: 呼び出し元は互換ラッパーだけ（アプリ終了時にホストを破棄する呼び出しは、元から無い）。移行前からの状態のため**今回は触らない**（要判断・Phase7 の起動シーケンス整理で扱う）。
- `PathService.AppDataPath`／`IPathService.AppDataPath` の**セッター**（Step7 決定 3＝P1 で登録済みの削除候補）: 本体の呼び出し元は 0 件。テストは 1 件（`Phase6Step10GroupOneContractTests.cs:100`、セッターの挙動を確認）。**削除可**（Wave 5。該当テストも削除する）。コンストラクタ引数 `PathService(string appDataPath)` と `_customAppDataPath` は残す。
- `ServiceProviderHolder`、`Program.rootHub`: 呼び出し元がある（`rootHub` は 25 件）。Phase7 の対象で、今回は触らない。
- **契約側（DI インターフェース）の 0 件メンバー: 52 件**（`IProfileSettingsService` 22、`IDeviceStateService` 7、`IOutputSlotService` 5 ほか。名前の一致で数えた概算）。`Global` の不要分を消した後の件数で、§2.4 の「契約だけ作って切り替え未実施」の可能性があるため、**一括削除しない**。推奨は Step12 の範囲外とし、Phase7 の台帳へ登録（個別の経緯調査が必要）。

### 0A.4 テストへの影響

- 不要メンバーを参照するテストは 15 ファイル（`StandaloneTests` 1 を含む）。
  - 等価性テスト（旧シムと新サービスが同じ値を返すことの確認）: `ControlServicePr4ShimEquivalenceTests`（1）、`ControlServicePr5ShimEquivalenceTests`（11）。旧シム専用の部分は削除、生存するプロパティで読めるものは書き換える（例: `getFlashWhenLateAt()` → `FlashWhenLateAt`）。
  - `*Instance` を使うテスト: `DeviceStateServiceTests`、`EnvironmentServiceTests`、`OutputSlotServiceTests`、`PathServiceTests`、`ProfileRepositoryTests`、`SpecialActionRepositoryTests`、`ProfileSettingsViewModelTestFactory`、`ProfileSettingsViewModelTests`。コンストラクタで直接生成するか、`AppHost.GetService` に書き換える。
  - 契約テスト: `ProfileSettingsServiceTests`（16 名）、`Phase6Step3ContractExtensionTests`（`GetControlSettingsGroup`、`GetProfileActionIndexOf`）、`Phase6Step10GroupThreeContractTests`・`SpecialActionsListViewModelMissingActionTests`（`GetActions`）、`MappingGyroControlsHelperTests`（`IsUsingSAForControls`）、`AppSettingsServiceTests`（`Save`）。
  - `StandaloneTests/TestSpecialActionUtils`: `Util` と `Global` の `NormalizeActionName` の一致を確認するテスト。`Global` 側が消えるので、そのテストだけ削除（`Util` 側のテストは残す）。
- ラチェット（`Step10FourGlobalRatchetGuardTests`）の走査対象は `DS4Windows` 配下のみ。テストの書き換えでは基準値は動かない。

### 0A.5 計画書の失効箇所と改訂案

- 全体: 「`ScpUtil.cs`」は存在しない（Step10b W5 で `Global.cs` 3,472 行／`BackingStore.cs` 6,869 行／`Changelog.cs` に分割）。→ 対象を `DS4Windows/DS4Control/Global.cs` に改める。
- §0.1: 「実機は Flydigi Vader 4 Pro／DS4／DualSense」→ Vader 4 Pro は削除済み。実機は DualShock 4 v.2（Bluetooth）。確認不能の項目は `Phase6-Step11-RealDevice-Verification-Checklist.md` §11。件数は、ラチェット基準値（`Global.*` 780、`AppHost` 48、`Program.rootHub` 25）に合わせる。
- §1 防壁 1: `ScpUtil.cs` → `BackingStore.cs`。「全フィールド・配列・プロパティ」と書かれており、メソッドの扱いが未定義。→ §0A.3 のとおり、メソッドは要判断（推奨は温存）と明記する。
- §1 防壁 2: 「101 箇所」「`SetCurveAndDeadzone`（34）」ほか → 失効。Step3 は 2026-09-21 に方針を転換し、`Mapping.cs` の実参照 150 件を解消済み。現行の `Mapping.cs` は `Global.*` 53 件（うち定数 50）、`AppHost` 6、`rootHub` 1。防壁 2 の中身は「`Mapping` の静的 `profileSettings` 束縛（方針 P2）、温存 3 件、`SyntheticDispatcher`、第 1 層」（台帳の分類 G）に置き換える。
- §1 防壁 3: 「11 箇所」→ 12 箇所（Step7-0 の再集計）に統一。§3、§4、§6 の「11 件」も同様。
- §1 防壁 4・5: 内容は有効。`Clamp`、`getTransitionedColor` は生存。
- §2 代表例: 約 35 名のうち 7 名だけが 0 件、5 名は存在しない、残りは生存。→ 代表例の一覧を削除し、§0A.8 の名簿に置き換える。「不要となった旧イベントシム」は、DI の旧イベントシムではなく上流由来のイベント 4 種（§0A.2）。
- §2「Step7 からの登録」: `AppDataPath` のセッターは有効（テスト 1 件の削除を追記）。`ResetConnectionFlags`、`LoadActions`、`CreateStdActions` は温存のまま（有効）。
- §3:
  - Step12-0 は本節で実施済み（検証の記録は §0A.9）。
  - Step12-1 の成果物 `Phase6-Step12-Deletion-Candidates-Report.md` → 本節 §0A.2・§0A.8 が代わりになる（別ファイルは作らない）。
  - Step12-2 の「1 PR で全削除」→ Wave に分割（§0A.6）。
  - Step12-3 の「`DS4Windows.Actions.Tests`」→ `DS4WindowsTests`（csproj 名は `DS4Windows.Actions.Tests.csproj`）、582 件＋`StandaloneTests` 13 件。ビルド・テストはユーザーが実行する。
  - 型を消す（イベント引数 3 型）ときは、1 ファイル 1 型の規約（§3.3 原則 1）に沿って `git rm` で消す。ガードテストを新設するときも 1 ファイル 1 型。
- §4（最終引き継ぎ台帳）: 「`Mapping.cs` 参照分（101 箇所）」→ 上記の置き換え、「Pre-Host（11 箇所）」→ 12。`Program.rootHub` は「App 内 12 行、19 ファイル」→ 現行 25 件。新規に追加する行:
  - 生存する `Global` の実体（D 分類 144 名）: Phase7 でサービス側へ移管して消す。
  - `BackingStore` の旧保存系メソッド 10 件（要判断）。
  - DI インターフェースの 0 件メンバー 52 件（個別判断）。
  - `DS4WinWPF.AppHost.Dispose()` に呼び出し元が無いこと。
  - K7-1: `fallbackProfileRepository` は Step10-4-2b で遅延生成に変更済み（K7-1 の記述の更新）。
- §5 ロールバック: 「PR-2 の削除コミットを revert」→ Wave 単位でコミットを分け、Wave ごとに revert できるようにする。
- §6 チェックリスト: 「101 件」「11 件」の置き換え、テスト数（595）、`ScpUtil.cs` → `Global.cs`、Wave ごとのラチェット基準値の更新と新規ガードテストの成功を追加。

### 0A.6 削除の進め方（選択肢と推奨）

#### 決定 1: 削除の範囲

- 案 A（推奨）: `Global` の不要 256 件＋`DS4Windows.AppHost`（互換）の未使用 5 件＋`AppDataPath` のセッター。要判断 2 件は、`getDefaultX360ControlBinding` を削除、`DS4_STICK_AXIS_MIDPOINT` を温存。`BackingStore`・DI インターフェースは触らない。
- 案 B: `Global` の不要 256 件だけ（互換ラッパーとセッターは別 Step）。
- 案 C: 案 A に加え、`BackingStore` のメソッド 10 件と DI インターフェースの 0 件メンバーも削除（§2.4 の個別調査が必要で、旧保存形式の判断を含むため非推奨）。

#### 決定 2: 段階（1 回の範囲）

- 案 A（推奨）: 5 つの Wave（各 1 コミット。Wave ごとにユーザーがビルド・テストを実行し、結果を待ってから次へ）。
  - Wave 1（孤立コード 25 件＋イベント引数 3 型）: 宣言 25 件、非空行 約110。ラチェット `Global.cs` の `Global` 47→45。テストの書き換え: 等価性テスト 2 ファイルの数行、`TestSpecialActionUtils`。
  - Wave 2（ProfileSettings 系の互換アクセサ）: 宣言 150 件、非空行 359。`Global` 45→38。テストの書き換え: `ProfileSettingsServiceTests` ほか。
  - Wave 3（`*Instance` のフォールバック群）: 宣言 19 件、非空行 139。`Global` 38→26、`AppHost` 11→5、`rootHub` 9→6。テストの書き換え: 8 ファイル。**実機で起動ログを確認する**。
  - Wave 4（`BackingStore` 直転送ラッパー）: 宣言 62 件＋`getDefaultX360ControlBinding`、非空行 224。`AppHost` 5→4。テストの書き換え: 契約テストほか約 6 ファイル。
  - Wave 5（互換 `AppHost` の未使用 5 件、`AppDataPath` のセッター、ガードテスト、完了報告書）。互換ラッパーの `GetService<T>` は `Mapping.cs:86` が使うので残す。
  - 最終のラチェット基準値: `Global.cs` = (rootHub 6, AppHost 4, Global 26)。全域では `Global.*` 780→747、`AppHost` 48→40、`Program.rootHub` 25→22（Wave 5・6 と §0A.21 を含む最終値）。
- 案 B: 1 回で全削除（コミット 1 つ）。差分が約 1,100 行と大きく、失敗したときの原因の切り分けが難しい。非推奨。
- 案 C: メンバー単位（約 250 回）。安全だが、ビルド・テストの回数が過大。非推奨。

#### 決定 3: ガードテストの追加

- 案 A（推奨）: 新規テスト `Step12DeadShimRemovalGuardTests`（1 ファイル 1 型）。(1) 削除した名前が `Global.cs` に再び現れないこと、(2) 防壁にあたる名前（`BackingStore` の主要メンバー、Pre-Host の `Global` メンバー、定数、ViGEm の 4 つ）が残っていること。ラチェットとは別に、Wave ごとに名簿を追記する。
- 案 B: ラチェットの基準値更新のみ（件数が増えなければ通るため、削除した名前の復活は検出できない）。

#### 決定 4: テストの扱い

- 案 A（推奨）: 旧シムを検証していたテストは、(a) 生存するプロパティ・サービスへ書き換えられるものは書き換え、(b) 「旧シムと新サービスの一致」そのものが目的のテストは削除する。削除したテストの一覧は、完了報告書に記録する。
- 案 B: テストをすべて残し、旧シムを `[Obsolete]` で温存（削除の目的に反するため非推奨）。

#### 決定 5: 契約側・BackingStore の扱い

- 推奨: `BackingStore` のメソッド 10 件（旧保存系を含む）と DI インターフェースの 0 件メンバー 52 件は、Step12 の範囲外とし、Phase7 の台帳へ登録（§4 の表へ追記）。`DS4WinWPF.AppHost.Dispose()` も同様。

#### 決定 6: 実機確認

- 推奨: Wave 3 の後に、起動して接続・プロファイル切替までを確認する（起動ログの `Host initialized with runtime parser` を含む）。ほかの Wave は、ビルド・テストの成功を条件とする（削除対象は呼び出し元 0 件で、実行経路に載っていない）。確認不能の項目は、チェックリスト §11 の台帳に追記する。

### 0A.7 承認をお願いしたい事項

- 決定 1〜6 の推奨案（A、A、A、A、Phase7 へ登録、Wave 3 後の実機確認）でよいか。
- 要判断 2 件（`DS4_STICK_AXIS_MIDPOINT` は温存、`getDefaultX360ControlBinding` は削除）でよいか。
- §0A.5 の改訂案を、承認後に §0〜§6 へ反映してよいか（承認までは §0A のみを追記し、本文は変更していない）。

### 0A.8 付録: 削除可メンバーの名簿（Wave 別。括弧はテストの参照件数）

- **Wave 1（孤立コード）**
  - メンバー: `m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`、`CheckForDevice`、`ControllerStatusChange`、`ControllerStatusChanged`、`BatteryStatusChange`、`OnBatteryStatusChange`、`ControllerRemoved`、`OnControllerRemoved`、`DeviceStatusChange`、`OnDeviceStatusChanged`、`getUseExclusiveMode`、`DS4Mapping`、`getQuickCharge`、`GetMinToTaskbar`、`DownloadLang`、`getFlashWhenLate`（テスト1）、`getFlashWhenLateAt`（テスト2）、`IsUsingUDPServerSmoothing`（テスト2）、`DebouncingMsHasChanged`、`NormalizeActionName`（テスト3）
  - 同時に `git rm`: `DS4Control/BatteryReportArgs.cs`、`DS4Control/ControllerRemovedArgs.cs`、`DS4Control/DeviceStatusChangeEventArgs.cs`
- **Wave 2（ProfileSettings 系の互換アクセサ）**
  - メンバー: `tempprofilename`（テスト1）、`touchpadActive`（テスト3）、`RightStickDriftXAxis`、`RightStickDriftYAxis`、`LeftStickDriftXAxis`、`LeftStickDriftYAxis`、`InverseRumbleMotors`、`ButtonMouseInfos`、`RumbleBoost`、`getRumbleBoost`（テスト1）、`setRumbleAutostopTime`、`getRumbleAutostopTime`、`EnableTouchToggle`、`getEnableTouchToggle`（テスト1）、`IdleDisconnectTimeout`、`EnableOutputDataToDS4`、`getEnableOutputDataToDS4`、`TouchSensitivity`、`LightbarSettingsInfo`、`DinputOnly`、`getDInputOnly`、`StartTouchpadOff`、`TouchOutMode`、`SATriggers`、`getSATriggers`、`SATriggerCond`、`getSATriggerCond`、`SetSaTriggerCond`、`GyroOutputMode`、`GetGyroOutMode`、`SAMousestickTriggers`、`GetSAMouseStickTriggers`、`SAMouseStickTriggerCond`、`GetSAMouseStickTriggerCond`、`SetSaMouseStickTriggerCond`、`GyroMouseStickTriggerTurns`、`GetGyroMouseStickTriggerTurns`、`GyroMouseStickHorizontalAxis`、`getGyroMouseStickHorizontalAxis`、`GyroMouseStickInf`、`GetGyroMouseStickInfo`、`GyroSwipeInf`、`GetGyroSwipeInfo`、`GyroMouseStickToggle`、`SetGyroMouseStickToggle`、`SASteeringWheelEmulationAxis`、`SASteeringWheelEmulationRange`、`GetSASteeringWheelEmulationRange`、`TouchDisInvertTriggers`、`GyroSensitivity`、`getGyroSensitivity`、`GyroSensVerticalScale`、`getGyroSensVerticalScale`、`GyroInvert`、`getGyroInvert`、`GyroTriggerTurns`、`getGyroTriggerTurns`、`GyroMouseHorizontalAxis`、`getGyroMouseHorizontalAxis`、`GyroMouseDeadZone`、`GetGyroMouseDeadZone`、`SetGyroMouseDeadZone`、`GyroMouseToggle`、`SetGyroMouseToggle`、`SetGyroControlsToggle`、`GyroMouseInfo`、`GyroControlsInf`、`GetGyroControlsInfo`、`WheelSmoothInfo`、`SAWheelFuzzValues`、`getLowColor`、`getChargingColor`、`getCustomColor`、`getUseCustomLed`、`getFlashColor`、`TapSensitivity`、`DoubleTap`、`getDoubleTap`、`ScrollSensitivity`、`getScrollSensitivity`、`LowerRCOn`、`TouchClickPassthru`、`TouchpadButtonMode`、`TouchpadJitterCompensation`、`TouchpadInvert`、`SXDeadzone`、`getSXDeadzone`、`SZDeadzone`、`getSZDeadzone`、`LSModInfo`、`RSModInfo`、`SXMaxzone`、`getSXMaxzone`、`SZMaxzone`、`getSZMaxzone`、`LSRotation`、`RSRotation`、`SXSens`、`getSXSens`、`SZSens`、`getSZSens`、`LSSens`、`RSSens`、`BTPollRate`、`getBTPollRate`、`DualSenseRumbleEmulationMode`、`UseGenericRumbleStrRescaleForDualSenses`、`DualSenseHapticPowerLevel`、`SquStickInfo`、`LSAntiSnapbackInfo`、`RSAntiSnapbackInfo`、`LSOutputSettings`、`RSOutputSettings`、`setLsOutCurveMode`、`getLsOutCurveMode`、`lsOutBezierCurveObj`、`setRsOutCurveMode`、`getRsOutCurveMode`、`rsOutBezierCurveObj`、`L2ModInfo`（テスト1）、`R2ModInfo`（テスト1）、`L2Sens`（テスト1）、`R2Sens`（テスト1）、`L2OutputSettings`（テスト1）、`R2OutputSettings`（テスト1）、`l2OutBezierCurveObj`（テスト1）、`r2OutBezierCurveObj`（テスト1）、`OutputVirtualTriggerButton`（テスト1）、`OutputDS4TriggerMode`（テスト1）、`setL2OutCurveMode`（テスト1）、`getL2OutCurveMode`（テスト1）、`setR2OutCurveMode`（テスト1）、`getR2OutCurveMode`（テスト1）、`setSXOutCurveMode`、`getSXOutCurveMode`、`sxOutBezierCurveObj`、`setSZOutCurveMode`、`getSZOutCurveMode`、`szOutBezierCurveObj`、`TrackballMode`、`getTrackballMode`、`TrackballFriction`、`getTrackballFriction`、`TouchMouseStickInf`、`TouchRelMouse`、`LaunchProgram`、`getDS4CSettings`、`GetDS4CSetting`
- **Wave 3（`*Instance` フォールバック群）**
  - メンバー: `profileRepository`、`fallbackProfileRepository`、`ProfileRepositoryInstance`（テスト6）、`specialActionRepository`、`fallbackSpecialActionRepository`、`SpecialActionRepositoryInstance`（テスト3）、`deviceStateService`、`fallbackDeviceStateService`、`DeviceStateServiceInstance`（テスト3）、`outputSlotService`、`fallbackOutputSlotService`、`CreateFallbackOutputSlotService`、`OutputSlotServiceInstance`（テスト6）、`pathService`、`fallbackPathService`、`PathServiceInstance`（テスト3）、`environmentService`、`fallbackEnvironmentService`、`EnvironmentServiceInstance`（テスト4）
- **Wave 4（`BackingStore` 直転送ラッパー）**
  - メンバー: `getTouchSensitivity`、`TouchActive`、`GetTouchActive`（テスト1）、`IsUsingTouchpadForControls`（テスト1）、`IsUsingSAForControls`（テスト1）、`getTouchDisInvertTriggers`、`getTapSensitivity`、`getTouchpadJitterCompensation`、`getTouchpadInvert`、`GetL2ModInfo`、`getL2Deadzone`、`GetR2ModInfo`、`getR2Deadzone`、`getLSDeadzone`、`getRSDeadzone`、`getLSAntiDeadzone`、`getRSAntiDeadzone`、`GetLSDeadInfo`、`GetRSDeadInfo`、`SXAntiDeadzone`、`getSXAntiDeadzone`、`SZAntiDeadzone`、`getSZAntiDeadzone`、`getLSMaxzone`、`getRSMaxzone`、`getL2AntiDeadzone`、`getR2AntiDeadzone`、`getL2Maxzone`、`getR2Maxzone`、`getLSRotation`、`getRSRotation`、`getL2Sens`、`getR2Sens`、`getLSSens`、`getRSSens`、`GetSquareStickInfo`、`GetLSAntiSnapbackInfo`、`GetRSAntiSnapbackInfo`、`GetTouchMouseStickInfo`、`GetOutputDS4TriggerMode`（テスト1）、`getProfileActionCount`（テスト1）、`CalculateProfileActionCount`、`UpdateDS4CSetting`、`UpdateDS4Extra`、`GetDS4Action`、`GetDS4KeyType`、`GetDS4Extra`、`GetDS4STrigger`、`GetControlSettingsGroup`（テスト1）、`HasCustomActions`、`HasCustomExtras`、`containsCustomAction`（テスト1）、`containsCustomExtras`（テスト1）、`GetActions`（テスト8）、`GetProfileActionIndexOf`（テスト2）、`CalculateProfileActionDicts`、`Save`（テスト1）、`SaveProfile`、`SaveAsProfile`、`getDefaultX360ControlBinding`
- **Wave 5**
  - `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`）の `Host`、`CreateHost`、`GetService(Type)`、`Dispose`、`_loggedResolvedTypes`。
  - `IPathService.AppDataPath`／`PathService.AppDataPath` のセッター。

### 0A.9 Step12-0 の前提検証の記録

- Step11 の判定: GO（`Phase6-Step11-Completion-Report.md` §5、2026-10-17）。
- HEAD: `e0bc8258`、作業ツリーはクリーン（走査の開始時点）。ベースラインのテスト結果は、Step11 の記録（`DS4WindowsTests` 582、`StandaloneTests` 13、全件成功、ビルド警告 0）を採用する。Step12-0 では `dotnet` を実行していない。
- 本節の追記と、走査スクリプト（`scripts/audit/Step12/`）の追加以外に、コードの変更は無い。

### 0A.10 承認された決定（2026-10-17）

- 決定 1＝案 A、決定 2＝案 A（5 Wave）、決定 3＝案 A（`Step12DeadShimRemovalGuardTests`）、決定 4＝案 A、決定 5＝Phase7 へ登録、決定 6＝Wave 3 の後に実機確認。
- 要判断: `DS4_STICK_AXIS_MIDPOINT` は温存、`getDefaultX360ControlBinding` は削除（Wave 4）。
- §0A.5 の改訂案を §0〜§6 へ反映済み（2026-10-17）。Wave 1 に着手。

---

### 0A.11 Wave 1 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 削除（`Global.cs`）: 24 件 — `m_IdleTimeout`、`exeFileName`、`exeversionLong`、`fullExeVersionLong`、`localAppDataPpath`、`linkedProfileCheck`、`DS4Mapping`、`DownloadLang`、`CheckForDevice`、イベント 4 種とその発火メソッド 4 件（`ControllerStatusChange`／`ControllerStatusChanged`、`BatteryStatusChange`／`OnBatteryStatusChange`、`ControllerRemoved`／`OnControllerRemoved`、`DeviceStatusChange`／`OnDeviceStatusChanged`）、`getUseExclusiveMode`、`getQuickCharge`、`GetMinToTaskbar`、`getFlashWhenLate`、`getFlashWhenLateAt`、`IsUsingUDPServerSmoothing`、`NormalizeActionName`（付随する孤立コメント 1 行も削除）。
- `git rm`: `DS4Control/BatteryReportArgs.cs`、`ControllerRemovedArgs.cs`、`DeviceStatusChangeEventArgs.cs`。
- **Wave 1 から外した 1 件（§0A.6 の予定からの変更）: `DebouncingMsHasChanged`（`Global.DebouncingMsChanged` の発火メソッド）。**
  - 理由: `DS4Device.SetupDebouncer`（`DS4Device.cs:1442`）が `Global.DebouncingMsChanged` を購読しており、削除するとこの購読が永久に発火しない状態を固定してしまうため。
  - 経緯（`git log -S`）: 発火元は上流 `2b357940`（2024-08-26）で追加され、プロファイル編集画面の保存処理が呼んでいた。2026-09-14 の `9da60b85`（ProfileEditor 保存処理の統合）で呼び出しが `Global.ApplyProfile(...)`（ホットリロード）に置き換わり、現在の呼び出し元は 0 件。
  - 懸念: 接続中のコントローラーでデバウンス時間（`DebouncingMs`）を変更して保存したとき、`Debouncer` の持続時間が即時に更新されない可能性がある（再接続で反映）。`ApplyProfile` 側でデバウンサーが更新されるかは未確認。**回帰かどうかの確認が必要**（Phase6 より前の変更）。修正の前にユーザーの承認を得る。確認不能なら §11 の台帳に追記する。
  - 扱い: 温存（Wave 4 の後に、確認の結果を見て削除または修正を判断する）。`DebouncingMsHasChanged` を含む形で名簿 §0A.8 の Wave 1 は 25 件→24 件。
- **回帰の調査結果（2026-10-17、静的調査）: 回帰を確認。**
  - 事実: `Debouncer` の持続時間は、デバイスを開くとき（`DS4Device.cs:910` の `SetupDebouncer`、`DS3Device`／`DualSenseDevice`／`JoyConDevice`／`SwitchProDevice` も同様）に `DebouncingMs[スロット]` で決まる。接続中に変える経路は、`Global.DebouncingMsChanged` の購読ラムダ（`DS4Device.cs:1442`）による `SetDuration` だけ。`SetDuration` の呼び出しは、ほかに無い。`ControlService.CheckProfileOptions`（`ApplyProfile` から呼ばれる）にも、デバウンサーの更新は無い。
  - 事実: このイベントの発火元（`Global.DebouncingMsHasChanged()`）は、プロファイル編集画面の保存処理が `HasDebouncingMsChanged` を見て呼んでいた。`9da60b85`（2026-09-14）で、この呼び出しが `ApplyProfile`（ホットリロード）に置き換わった。`HasDebouncingMsChanged` は今も設定されるが、読む箇所が無い。
  - 影響: 接続中のコントローラーで、プロファイル編集画面のデバウンス時間を変更して保存・適用しても、再接続するまで反映されない（移行前は即時に反映）。プロファイル切替でも、デバウンス時間が更新されない（上流 `2b357940` 以来の仕様）。
  - 修正案（承認待ち）: 案 A（推奨）＝`ControlService.CheckProfileOptions` に `device.Debouncer?.SetDuration(...)`（`_profileSettings.DebouncingMs[ind]`）を 1 行追加。`ApplyProfile` の経路（編集画面の保存・適用、プロファイル切替、接続時）のすべてで反映され、Global のイベント・購読ラムダ・`DebouncingMsHasChanged`・サービス側の `NotifyDebouncingMsChanged` を不要にできる（別 Wave で削除）。案 B＝移行前の動作の復元だけ（編集画面の保存後に `IProfileSettingsService.NotifyDebouncingMsChanged()` を呼び、`DS4Device` の購読先をサービス側のイベントへ変更）。案 C＝修正しない。
- **回帰の是正（案 A、2026-10-17 承認・実施。ビルド・テストはユーザーの実行待ち）**:
  - `ControlService.CheckProfileOptions` に `device.Debouncer?.SetDuration(TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[ind]))` を追加した。
  - 調査中に、**上流（2024-09）から続く別の不具合**を発見し、同じ変更で是正した（回帰ではない。移行前も同じ）: `Debouncer.ProcessInput` の先頭の `duration.TotalMilliseconds == 0` が、コンストラクタ引数（生成時の値）を見ており、`SetDuration` で更新されなかった。デバイスを 0ms（既定）で開いたあとに時間を変えても、デバウンスが働かない。現在の値を持つフィールド `_duration` を追加し、`ProcessInput`・`AddDebouncer`・`SetDuration` で使うようにした（`Debouncer.cs`）。これが無いと、案 A だけでは 0ms → 500ms の変更が効かない。
  - テスト（新規 `DebouncerSetDurationTests`、3 件）: 0ms で生成して `SetDuration` した場合に働くこと、0 に戻すと無効になること、`CheckProfileOptions` に更新の呼び出しがあること。
  - 実機の確認（ユーザー）: 接続中に「Other」タブの Debouncing を 500 にして適用し、ボタンの連打が抑えられること。0 に戻すと抑えられなくなること。
  - 未実施（Wave 4 の後に判断）: `DebouncingMsHasChanged`、`Global.DebouncingMsChanged`、`DS4Device` の購読ラムダ（`DS4Device.cs:1442`）、`IProfileSettingsService.NotifyDebouncingMsChanged`／`DebouncingMsChanged` は、案 A により不要になった。削除は実機確認の後。
- テストの書き換え: `ControlServicePr4ShimEquivalenceTests`（旧 getter との比較 1 行を削除）、`ControlServicePr5ShimEquivalenceTests`（`IsUsingUDPServerSmoothing()` → `UseUDPSeverSmoothing`、`getFlashWhenLateAt()` → `FlashWhenLateAt`）、`StandaloneTests/TestSpecialActionUtils`（`Util` と `Global` の一致テストを、`Util` の期待値の固定テスト `Util_NormalizeActionName_FixedExpectations` に置換）。削除したテストケースは無し（観点は維持）。
- ラチェット: `Global.cs` = (rootHub 9, AppHost 11, Global **45**)。全域は `Global.*` 780→778。`Global.cs` の件数は走査で確認済み（45）。
- 新規ガード: `DS4WindowsTests/Step12DeadShimRemovalGuardTests.cs`（削除済み名の再宣言の禁止、削除済みファイルの不在、防壁の 18 名の存在）。
- ユーザーに依頼: ビルド（警告 0）と `dotnet test`（`DS4WindowsTests` 582＋新規 3 件、`StandaloneTests` 13）。結果を確認後、Wave 2 へ。

---

### 0A.12 Wave 2 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 前提: Wave 1 とデバウンスの是正は、ビルド・テスト成功・コミット・リモート反映済み。実機確認は先送り（チェックリスト §11 の U-8）。
- 再走査: Wave 1 後に走査をやり直し、Wave 2 の 148 名が引き続き呼び出し元 0 件であることを確認した（新たに不要になったのは、温存・削除の判断済みの 2 件のみ）。
- 削除（`Global.cs`）: 150 名（宣言 152 件。オーバーロード 2 組を含む）。ProfileSettings 系の互換アクセサ（配列プロパティ、`getXxx`／`GetXxx`／`setXxx`）。
  - 計画からの変更: `TouchActive`、`GetTouchActive`（Wave 4 の予定）を Wave 2 に含めた。`touchpadActive`（Wave 2）を参照するため、同時に消さないとコンパイルできない。Wave 4 は 62→60 件になる。
  - 付随する孤立コメントは無し（セクション見出し 2 行と、元からあるコメントアウトされた旧コードは残した。Wave 4 で整理する）。
- テストの書き換え:
  - `ControlServicePr5ShimEquivalenceTests`: `TouchpadActiveArray_OfDiSingleton_IsTheSameArrayAsGlobalTouchActive` → `…_ReflectsWrites`（`Global.touchpadActive`／`GetTouchActive` を、サービスの `GetTouchpadActive` に置換）。`TouchOutMode_And_OutputDS4TriggerMode_MatchGlobalGetters` → `EnableTouchToggle_And_RumbleBoost_GettersMatchArrays`（サービスのメソッドと配列の一致）。
  - `ProfileSettingsServiceTests`: 旧シム専用の `GlobalShim_ShouldSynchronizeWithService`、`TriggerSettings_ShouldShareBackingStoreWithGlobalShim` を**削除**。`TriggerCurveMode_ShouldDelegateThroughServiceAndGlobalShim` → `TriggerCurveMode_ShouldRoundTripThroughService`（サービスだけで確認）。
  - 削除したテスト: 2 件（旧シムと新サービスの一致のみが目的）。テスト数は、Wave 1 後の件数から 2 件減る。
- ラチェット: `Global.cs` = (rootHub 9, AppHost 11, Global **38**)。全域は `Global.*` 778→771。`Global.cs` の件数は走査で確認済み（38）。
- ガード: `Step12DeadShimRemovalGuardTests` に 150 名を追記（宣言の検出式で、現行の `Global.cs` に残っていないことを確認済み）。
- 残る呼び出し元の確認: 削除した名前の `Global.` 参照・非修飾参照（`Mapping.cs`、`DS4LightBar.cs`）・`Global.cs` 内の参照は、走査で 0 件。

---

### 0A.13 Wave 3 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 前提: Wave 2 は、ビルド・テスト成功。再走査で、Wave 3 の 19 名が引き続き呼び出し元 0 件で、ほかの Wave 外メンバーからの内部参照も 0 件であることを確認した。
- 削除（`Global.cs`）: 19 件 — `profileRepository`／`fallbackProfileRepository`／`ProfileRepositoryInstance`、`specialActionRepository`／`fallbackSpecialActionRepository`／`SpecialActionRepositoryInstance`、`deviceStateService`／`fallbackDeviceStateService`／`DeviceStateServiceInstance`、`outputSlotService`／`fallbackOutputSlotService`／`CreateFallbackOutputSlotService`／`OutputSlotServiceInstance`、`pathService`／`fallbackPathService`／`PathServiceInstance`、`environmentService`／`fallbackEnvironmentService`／`EnvironmentServiceInstance`。あわせて、削除したブロックの見出し・説明コメントを整理した（残る見出しは `INotificationService` のブロックのみ）。
- 静的初期化への影響: `Global` の静的初期化子から、`new SpecialActionRepository(...)`、`new DeviceStateService()`、`new PathService()`、`new EnvironmentService()` の 4 つの生成が消えた（`fallbackProfileRepository`／`fallbackOutputSlotService` は遅延生成だった）。残る静的初期化子は、`ProfileSettingsService`、`ProfileXmlStore`、`AppNotificationService` のフォールバック、定数、`hidHideInstalled` ほか。
- テスト（8 ファイル）:
  - 削除（旧シムの同期確認だけが目的）: `DeviceStateServiceTests`・`EnvironmentServiceTests`・`OutputSlotServiceTests`・`PathServiceTests`・`SpecialActionRepositoryTests` の `GlobalShim_…` 各 1 件（計 5 件）。
  - 書き換え: `ProfileRepositoryTests.GlobalShim_ShouldSynchronizeWithRepository` → `ApplyAndRestoreProfileDirect_ShouldUpdateTempProfileState`（リポジトリを直接使う。挙動の確認は維持）。`ProfileSettingsViewModelTestFactory`（`?? Global.…Instance` のフォールバックを除去）、`ProfileSettingsViewModelTests`（不要な代入 1 行を除去）。
  - テスト数の増減: 5 件減（Wave 1 後の件数 −2［Wave 2］−5［Wave 3］）。
- ラチェット: `Global.cs` = (rootHub **6**, AppHost **5**, Global **26**)。全域は `Global.*` 771→759、`AppHost` 48→42、`Program.rootHub` 25→22。走査で確認済み。
- ガード: `Step12DeadShimRemovalGuardTests` に 19 名を追記。
- 実機確認（決定 6）: Wave 3 の後に、ユーザーが実施する。手順: (1) アプリを起動し、ログに `Host initialized with runtime parser` が出ること（K7-1）。(2) コントローラーを接続し、スロットに表示されること。(3) プロファイルを切り替え、ボタン入力が反映されること。(4) 設定の保存先（`Profiles` の読み書き）が従来どおりであること。結果は §11 の台帳または本節に記入する。

---

### 0A.14 Wave 4 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 前提: Wave 3 は、ビルド・テスト成功。実機確認は先送り（チェックリスト §11 の U-9。最終 Wave の後にまとめて実施）。
- 再走査: 58 名が引き続き呼び出し元 0 件で、Wave 外メンバーからの内部参照も 0 件。
- 削除（`Global.cs`）: 58 名（`getDefaultX360ControlBinding` を含む）。`BackingStore` へ直接転送していた薄いラッパー（`GetDS4Action`／`UpdateDS4CSetting`／`SaveProfile`／`Save`／`GetActions` ほか、`getXxxDeadzone`／`getXxxSens` 系）。あわせて、削除済み部品のコメントアウトされた旧コード（約 45 行）と、宛先を失った `///` の説明、見出し `Step10-2-A-3` を整理した。`Global.cs` は 3,472 行 → 2,414 行。
- 温存（決定どおり）: `DS4_STICK_AXIS_MIDPOINT`。Wave 1 で外した `DebouncingMsHasChanged`（下記）。
- テストの書き換え（7 ファイル。削除したテストは無し）:
  - `AppSettingsServiceTests`: `Global.Save()` → `service.Save()`。
  - `ControlServicePr5ShimEquivalenceTests`: 旧シムとの比較 3 行を削除（サービス側の確認は残す）。
  - `MappingGyroControlsHelperTests`: 期待値を `mode == GyroOutMode.Controls` に置換。
  - `Phase6Step3ContractExtensionTests`: 旧シムとの比較 2 件と `GetControlSettingsGroup` の比較 1 行を削除。
  - `Phase6Step10GroupThreeContractTests`、`SpecialActionsListViewModelMissingActionTests`: `Global.GetActions()` → `Global.store.actions`（同じ実体）。
  - テスト数の増減: 0 件。
- ラチェット: `Global.cs` = (rootHub 6, AppHost **4**, Global 26)。全域は `Global.*` 759、`AppHost` 41、`Program.rootHub` 22（最終目標と一致）。
- ガード: `Step12DeadShimRemovalGuardTests` に 58 名を追記。
- 既知の持ち越し（Wave 5 の後に判断）: デバウンスの通知経路 — `DebouncingMsHasChanged`、`Global.DebouncingMsChanged`、`DS4Device.cs:1442` の購読ラムダ、`IProfileSettingsService.NotifyDebouncingMsChanged`／`DebouncingMsChanged`。案 A により不要になったが、実機確認（U-8）の後に削除を判断する。

---

### 0A.15 Wave 5 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 前提: Wave 4 は、ビルド・テスト成功。
- 削除:
  - `DS4Windows.AppHost`（`DI/Compat/AppHost.cs`）の `Host`、`CreateHost`、`GetService(Type)`、`Dispose`、private フィールド `_loggedResolvedTypes`。残したのは `GetService<T>()` のみ（`Mapping.cs:86`、`App.xaml.cs` の呼び出しが使う）。クラスに TODO（Phase7: `Mapping` の instance 化で呼び出しがなくなった時点で、クラスごと削除）を付けた。
  - `IPathService.AppDataPath`／`PathService.AppDataPath` のセッター（`{ get; set; }` → `{ get; }`）。関連する説明コメントを更新。コンストラクタ引数 `PathService(string appDataPath)` と `_customAppDataPath` は残した。
- テスト: セッター専用の `Phase6Step10GroupOneContractTests.PathService_AppDataPathSetter_DoesNotChangeGlobalAppDataPath` を**削除**（1 件減）。
- ラチェット: `DI/Compat/AppHost.cs` = (0, 1, 0)（`AppHost` 2→1）。全域の最終値（走査で確認）: `Global.*` **759**、`AppHost` **40**、`Program.rootHub` **22**。
- ガード: `Step12DeadShimRemovalGuardTests` に `Wave5_RemovedApiSurface_DoesNotReappear` を追加（`AppDataPath` のセッターが無いこと、互換ラッパーが `GetService<T>` だけであること）。
- 完了報告書: `Phase6-Step12-Completion-Report.md`、`Phase6-Completion-Report.md` を作成した（最終のビルド・テストの結果と実機確認の記入欄あり）。

---

### 0A.16 実機確認 U-8・U-9 の結果と調査（2026-10-17）

- **U-9（起動・接続・プロファイル切替・保存先の読み書き）: ○**。ただし、起動ログの `Host initialized with runtime parser` は出なかった。
  - 評価: §0A.13 の確認手順 (1) は誤り。`AppHost.CreateHost(config, parser)` は、ホストが**すでに作られている**とき `startup arguments applied to IStartupArguments (host already created)` を出し、`Host initialized with runtime parser` は、その呼び出しで新たに作ったときだけ出る。K7-1（`Global` の静的初期化で、ホストが先に暗黙構築される）が残る現状では、後者は通常の起動で出ない（Wave 3 より前から同じ）。Wave 3 の影響ではないと判断する。前者のログがあれば、期待どおり。
- **U-8（デバウンス）: 不合格**。500ms にしても、連打が抑えられない（ユーザー提供のログでは、押下から約 140〜180ms で離されたことが、そのまま出力されている）。
  - 原因（静的調査）: **プロファイルの XML（旧形式。2025-11-11 の `142c24c8` 以降、これを正としている）の保存・読込が、`DebouncingMs` を扱っていない**。`BackingStore.cs` に `debouncingMs` の宣言（649 行）以外の記述が無く、`SaveProfile` も `LoadProfile` も読み書きしない。`ProfileDTO` には要素 `DebouncingMs` があるが、これを使う `ProfileRepository.LoadProfile(filePath, …)` は、実際の読込経路ではない。結果、編集画面で 500 にして保存・適用しても、スロットへの再適用（`LoadProfile`）で 0 に戻る。
  - Phase6 の変更との関係: 今回の是正（`CheckProfileOptions` と `Debouncer`）は、値が正しく届く場合に必要で、有効。ただし、それだけでは効かない。この読み書きの欠落は、Phase6 より前からの不具合（旧形式を正にした 2025-11-11 以降、設定が保存されていない）。
  - 同じ欠落の設定（`ProfileDTO` にあり、旧形式の読み書きに無い要素の突き合わせ結果）: `DebouncingMs`、`LeftStickDriftXAxis`／`LeftStickDriftYAxis`／`RightStickDriftXAxis`／`RightStickDriftYAxis`（スティックのドリフト補正）、`InverseRumbleMotors`、`UseDs3PitchRollSim`、`DS4OutputTriggerMode`、`GyroMouseJitterCompensation`、`GyroMouseStickJitterCompensation`。計 10 要素。画面では設定できても、保存されず、適用時に既定値へ戻る（Phase6 の対象外。ユーザーの承認が必要）。
- 修正案（承認待ち）: 案 1＝`DebouncingMs` だけを、旧形式の `SaveProfile`／`LoadProfile` に追加（要素名 `DebouncingMs`。旧版は未知の要素を無視するため、3.9.9 との互換は保たれる）。案 2＝上記 10 要素すべてを追加（別の Step として扱う。配列・列挙型の変換とテストが要る）。案 3＝修正しない（U-8 を「既知の制限」として台帳に残す）。

---

### 0A.17 U-8 の是正: 旧形式のプロファイル XML に 10 要素を追加（2026-10-17、案 2 を承認・実施。ビルド・テストはユーザーの実行待ち）

- 変更（`BackingStore.cs`）:
  - 保存: `SaveProfile` が `SaveLegacyExtraSettings(rootElement, device)` を呼び、10 要素（`DebouncingMs`、`InverseRumbleMotors`、`UseDs3PitchRollSim`、`LeftStickDriftXAxis`／`LeftStickDriftYAxis`／`RightStickDriftXAxis`／`RightStickDriftYAxis`、`DS4OutputTriggerMode`、`GyroMouseJitterCompensation`、`GyroMouseStickJitterCompensation`）を書く。要素名は `ProfileDTO` と同じ。旧版は未知の要素を無視するため、3.9.9 との互換は保たれる。
  - 読込: `LoadProfile` が `LoadLegacyExtraSettings(device, rootname)` を呼ぶ。要素が無い、または値が不正なときは、`ResetProfile` で設定済みの既定値のまま（`missingSetting` は立てない。古い全プロファイルについて「設定が足りない」というログを出さないため）。`DebouncingMs` は 0〜1000（画面の範囲）に収める。`UseDs3PitchRollSim` は全体で 1 つの値のため、要素があるときだけ代入する（`ProfileDTO.MapTo` と同じ扱い）。
  - リセット: `ResetProfile` に、`DebouncingMs`（0）、`InverseRumbleMotors`（false）、ドリフト補正 4 件（0）、`DS4OutputTriggerMode`（既定）を追加。ジッター補正は、既存の `gyroMouseInfo.Reset()`／`gyroMStickInfo.Reset()` が既定値に戻す。`UseDs3PitchRollSim` はリセットしない（全体で 1 つの値のため）。
- テスト（新規 `ProfileLegacyXmlRoundTripTests`、5 件）: 保存→読込で 10 要素が戻ること、要素が無い XML では既定値になること、不正な値は無視されること、`DebouncingMs` の上限、`SaveProfile`／`LoadProfile` が補助メソッドを呼んでいること（ソース走査）。
- 影響: 画面で設定できていたが保存されていなかった 10 設定が、保存・再適用・再起動後も保持される。スティックのドリフト補正と DS4 のトリガー出力モードなど、今まで毎回既定に戻っていた設定が効くようになるため、**既存のプロファイルで、画面上の値と実際の動作が変わることはない**（要素が無い古いプロファイルは既定値＝従来の実際の動作）。保存し直したプロファイルから、新しい値が反映される。
- 実機確認（ユーザー。U-8 の再実施）: (1) Other タブの Debouncing を 500 にして「適用」。連打が抑えられること。(2) アプリを再起動し、値が 500 のまま残っていること。(3) 0 に戻して「適用」し、抑えられなくなること。(4) 任意: スティックのドリフト補正を変更して保存し、再起動後も保持されること。
- 持ち越し: デバウンスの通知経路（`DebouncingMsHasChanged` ほか）の削除は、U-8 の再確認の後に判断する。

---

### 0A.18 方針決定: プロファイルの保存形式は旧形式に統一し、プロファイル用 DTO を削除する（2026-10-17、ユーザー決定。実施時期はエージェントが決定）

- **決定（ユーザー）**: 旧形式（3.9.9 互換）を正とし、DTO は最終的に削除して、旧形式だけに統一する。実施時期は、エージェントが最適な箇所で決める。
- **分析の根拠**（調査結果）:
  - プロファイル（`Profiles\*.xml`）の読み書きは、すでに旧形式だけが実際に使われている（`ProfileRepository` の DTO 側 `LoadProfile(filePath, …)`／`SaveProfile(filePath, …)` は、呼び出し元もテストも 0 件）。経緯は `6926c563`（2026-08-31 作成）、`576426b8`・`142c24c8`（2025-11-11、旧形式を正に戻した）。削除しても、現行の機能は変わらない。
  - 旧形式の結合は薄い: `SaveProfile` 557 行（`Global.` 7 件、`control.` 0 件）、`LoadProfile` 2,315 行（`Global.` 7 件、`control.` 5 件）。Pure DI 化の障害にならない（Phase7 で `LegacyProfileXmlSerializer` として切り出す）。
  - DTO の長所（保存と読込が 1 対、欠落の突き合わせ基準）は、固定した要素一覧のテストと、往復・ゴールデンのテストで代替する。
- **実施時期の決定**: **Step12 の Wave 6（Phase6 の内側）で、プロファイル用 DTO を削除する。** 理由:
  1. 呼び出し元 0 件のコードで、方針も決まったため、Step12（呼び出し元 0 件の旧シムの物理削除）の目的に合う。先送りしても、削除の条件（呼び出し元 0 件）は変わらず、台帳に「使われない 2,668 行」が残るだけになる。
  2. Phase7 の前に消しておくと、`BackingStore` の解体で、旧形式の読み書きの切り出しだけを考えればよくなる（DTO との二重管理を避けられる）。
  3. 一方で、削除の前に、DTO が担っていた「欠落の検出」を、テストへ移す必要がある。そのため、**削除は、安全網のテストが通った後**に行う。
- **手順**:
  - 準備（本節で実施）: `ProfileLegacyXmlCompletenessTests` を追加した。ProfileDTO の 175 要素のうち、読み取り専用の移行用 13 要素を除く **162 要素**の名前を固定し、旧形式の `SaveProfile` がそれらをすべて書くことを確認する（U-8 の 10 要素の再発も検出する）。ビルド・テストはユーザーの実行待ち。通らなければ、DTO の削除に進まず、先に原因（旧形式の欠落）を直す。
  - Wave 6（テストが通ったら実施）: `ProfileDTO.cs`（2,668 行）、ProfileDTO だけが使う型（`DS4ControlAssignementSerializer` 系、`TouchpadMouseStickSerializer`、`StickAxialDeadOptionsSerializer`、`StickDeltaAccelSettings`、`StickModeOutputSettings` ほか。他から使われる型は、削除前に使用箇所を再確認して残す）、`ProfileRepository` の DTO 側の読み書き、`IProfileRepository` の 2 メンバーを削除。ラチェットの基準値を更新し、削除で呼び出し元が 0 件になった `Global` のメンバー（`getDS4ControlsByName` ほか）を再集計して、削除候補に加える。
  - Phase7: 旧形式の読み書きを `BackingStore` から切り出し（`LegacyProfileXmlSerializer`）、ゴールデンファイルによる往復テストで固定する。
- **対象外**（現行の正として使用中のため、この決定では削除しない）: アプリ設定 `Profiles.xml`（`AppSettingsDTO`）、`Actions.xml`（`ActionsDTO`）、`LinkedProfiles.xml`、`Auto Profiles.xml`、`OutputSlots` の DTO。これらは DTO 方式で保存され、旧形式の読込は代替の読み込み（フォールバック）である。「旧形式だけに統一」をこれらへ広げるかは、別途の確認事項（保存形式の統一方針。Phase7 の台帳に登録）。

---

### 0A.19 U-8 の再確認の結果（2026-10-17）

- ビルド・テストは成功。実機で、デバウンス 500ms が**効くようになった**ことを確認した。
- 現象: 500ms で、a を割り当てたボタンを連打すると、押しっぱなしのときと同じ「a の繰り返し出力と停止」が、約 500ms ごとに繰り返される。
- 評価: **`Debouncer` の仕様（上流のコミット `2544d646`、2024-09-02 から不変）であり、今回の是正の不具合ではない。** `Debouncer.ProcessInput` は、押下（入力の変化）を検出すると、`Duration` が経過するまで「押されたまま」を返す（離した入力を、`Duration` が経過するまで出力に反映しない）。経過した時点で、実際の入力（押している間は押下、離していれば離した状態）に戻る。このため、押すのは即時に認識され、離すのは最短 500ms 後になる。連打の間隔が 500ms より短いと、ボタンは押しっぱなし相当になり、OS のキーリピートで a が繰り返される。デバウンスは、接点のチャタリング（数十 ms）を消すための機能で、500ms は極端な設定値。
- 推奨: 実用の値は 10〜50ms 程度。30ms などで、連打が通常どおり入力されること（チャタリングだけが消えること）を確認する。
- 仕様変更の選択肢（希望があれば別途）: 現在の方式は「押下を最短 N ms 保持」。一般的なチャタリング除去（押下直後・離した直後の N ms 間は、変化を無視）に変えると、連打の間隔が N ms 以上なら通常どおり入力される。これは上流の機能の挙動を変えるため、承認が要る。

---

### 0A.20 Wave 6 の実施記録: プロファイル用 DTO の削除（2026-10-17、ビルド・テストはユーザーの実行待ち）

- 前提: 旧形式の欠落 10 要素の是正（§0A.17）と、安全網 `ProfileLegacyXmlCompletenessTests`（162 要素）は、ビルド・テスト成功。デバウンスは実機で効くことを確認済み（§0A.19）。
- 削除の範囲の確定: `ProfileDTO` を起点に、「他のファイルから参照されなくなった型」を、参照の有無が変わらなくなるまで繰り返して求めた（文字列・コメントを除いた識別子で判定）。結果は **19 ファイル、4,298 行**:
  - `ProfileDTO.cs`（2,669 行）
  - `DS4ControlAssignementSerializer.cs`、`DS4ControlAssignmentSerializerBase.cs`、`DS4ControlButtonAssignmentSerializer.cs`、`DS4ControlExtrasAssignmentSerializer.cs`、`DS4ControlKeyAssignmentSerializer.cs`、`DS4ControlKeyTypeAssignmentSerializer.cs`、`DS4ControlLightbarMacroAssignmentSerializer.cs`、`DS4ControlMacroAssignmentSerializer.cs`
  - `TouchpadMouseStickSerializer.cs`、`StickAxialDeadOptionsSerializer.cs`、`StickDeltaAccelSettings.cs`、`StickModeOutputSettings.cs`
  - `GyroControlsSettings.cs`、`GyroSwipeSettings.cs`、`GyroMouseSmoothingSettings.cs`、`GyroMouseStickSmoothingSettings.cs`、`SASteeringWheelSmoothingOptions.cs`、`DualSenseControllerSettings.cs`
  - いずれも `git rm`。ほかから使われる型（`AppSettingsDTO`、`ActionsDTO`、`AutoProfilesDTO`、`XmlDataUtilities`、`FlickStickSettings` ほか）は、残した。
  - `BackingStore.cs`・`ProfileMigration.cs` にあった型名は、XML の要素名の文字列とコメントだけで、型の参照ではなかった（削除の影響なし）。
- 削除（コード）: `ProfileRepository` の DTO 側 `LoadProfile(filePath, …)`／`SaveProfile(filePath, …)`（71 行。あわせて `using` 2 件）、`IProfileRepository` の同 2 メンバー。
- 連鎖で呼び出し元が 0 件になった `Global` の 2 件を削除: `getDS4ControlsByName`、`getX360ControlsByName`（`BackingStore` への転送。`BackingStore` 側の同名メソッドは、旧形式のローダーが使うので残す）。
- ラチェット基準値: `ProfileDTO.cs`（Global 2）と `DS4Control*AssignmentSerializer` 6 ファイル（計 8）の項目を除いた（Global 計 10 減）。`Global.cs` は (6, 4, 26) のまま。**全域の最終値（走査で確認）: `Global.*` 749、`AppHost` 40、`Program.rootHub` 22。**
- ガード: `Step12DeadShimRemovalGuardTests` に、削除した 19 ファイルの不在と、2 件の名前を追記。
- テスト: DTO を参照するテストは無し（変更なし）。テスト数の増減は 0。
- 残る DTO（現行の正）: アプリ設定（`AppSettingsDTO`）、`ActionsDTO`、`LinkedProfilesDTO`、`AutoProfilesDTO`、`OutputSlotPersistDTO` と、その部品。旧形式への統一の範囲は、Phase7 で判断（§0A.18）。

---

### 0A.21 既存の課題 3 件への対応（2026-10-17、Wave 6 のビルド・テスト成功後。ビルド・テストはユーザーの実行待ち）

- 前提: U-8 の「再起動後の 500ms の保持」を、ユーザーが確認済み（チェックリスト U-8 を更新）。
- **課題 2: `TouchpadMouseStick` の `OutputStick` が 2 回書かれる → 修正**。
  - 原因: `BackingStore.SaveProfile` が、出力軸の要素を `OutputStick` と同じ名前で書いていた。読込は `OutputStickAxes` を読むため、出力軸（XY など）が保存されても読み込まれず、毎回既定値に戻っていた。上流のコミット `858bd549`（2022-10-20）から続く不具合で、移行の回帰ではない。
  - 修正: 書き出す要素名を `OutputStickAxes` にした。読込は、`OutputStickAxes` が無い古いファイルでは、2 つ目の `<OutputStick>` を軸として読む（すでに保存された値を救済）。
  - テスト: `ProfileLegacyXmlCompletenessTests.SaveProfile_TouchpadMouseStick_WritesOutputStickAndAxesOnce`（`OutputStick` と `OutputStickAxes` が 1 つずつ）。
- **課題 3: デバウンスの通知経路 → 削除**。デバウンス時間の更新は、案 A（`CheckProfileOptions`）で行われるため不要。削除: `Global.DebouncingMsHasChanged`、`Global.DebouncingMsChanged`、`DS4Device` の購読ラムダ（静的イベントへの購読が解除されないリークもあった）、`IProfileSettingsService`／`ProfileSettingsService` の `DebouncingMsChanged`・`NotifyDebouncingMsChanged`。`ProfileSettingsViewModel` の `HasDebouncingMsChanged`・`DebouncingMsChanged` は、他の `HasXxxChanged` と同じ型の画面側の仕組みのため残した。ラチェット: `DS4Device.cs` (0,0,3) → (0,0,1)。全域は `Global.*` 747、`AppHost` 40、`Program.rootHub` 22。ガードに名前を追記。
- **課題 1: 起動ログに `Host initialized with runtime parser` が出ない（K7-1）→ 原因特定の準備**。
  - 事実: このメッセージは、`AppHost.CreateHost(config, parser)` が**新たに**ホストを作ったときだけ出る。ホストが先に作られていると、`startup arguments applied to IStartupArguments (host already created)` が出る。出ないのは、ホストが、明示的な構築より前に、暗黙に作られているため。
  - 対応: `AppHost.GetService<T>`／`GetService(Type)` が、ホスト未構築で暗黙にホストを作るとき、解決しようとした型と呼び出し元のスタックを、Trace ログ（`[DI] implicit CreateHost (no parser) triggered by resolving …`）に出すようにした。動作は変わらない（ログのみ）。
  - **結果（2026-10-17 の起動ログ）**: `Host initialized with runtime parser`（Trace）は、上記の暗黙構築の診断ログ（Trace）と同じく、出なかった。原因は、ホスト構築の時点では、ロガーの最小レベル（Trace）がまだ反映されておらず（`Logger initialized` はホスト構築の後）、`IsTraceEnabled` が false で、Trace が捨てられるため。つまり「欠落」は、ログのタイミングの問題で、暗黙構築かどうかは、このログでは判別できない。
  - **追加対応**: ホストの作成経路を `AppHost.CreationNote` に保持し、ロガー初期化後の Info（`[DI] Host creation path: …`、`AppHost.CreateHost() called successfully` の直後）で出力する。暗黙構築の場合は、解決しようとした型とスタックが入る。Trace の既存ログとレベルは変更していない。
  - **確定（2026-10-17 15:28 の起動ログ）**: `[DI] Host creation path: explicit CreateHost(config, parser) created the host`。ホストは、暗黙ではなく、App.xaml.cs の明示的な `CreateHost(config, parser)` で作られている。K7-1 の不具合は無い（Trace が捨てられていただけ）。`AppHost.cs` の、「ホストは Global の静的初期化で先に作られる」旨のコメント（Step7-4）は、現状と合わない。コメントは訂正済み（`AppHost.cs`、`IStartupArguments.cs`、`ServiceRegistration.cs`）。`Host creation path` のログと `CreationNote` は、ユーザー決定により残す。
  - 次の作業: 次の起動のログを取り、このログから、暗黙の構築を起こしている呼び出し元を特定する。特定できたら、`Global` の静的初期化（または Pre-Host の経路）を修正して、明示的な構築を最初にする（K7-1 の案 R。Phase7 で予定していたが、原因次第で前倒しできる）。

---

## 0. 背景と方針（2026-10-17 改訂。承認済みの決定は §0A.10）

### 0.1 Step2〜Step11 の完了実績と残存コードの状況
- Phase6 の Step2〜Step10・Step10b・Step11 で、アプリ全域の `Global` 直接参照をコンストラクタ経由の Pure DI サービス呼び出しへ置き換えた。全域の残存は、ラチェット基準値で `Global.*` 780、`AppHost` 48、`Program.rootHub` 25（分類は `Phase6-Step11-Global-Residual-Ledger.md`）。
- Step11 の総合検証は判定 GO（2026-10-17）。自動テストは `DS4WindowsTests` 582 件＋`StandaloneTests` 13 件が成功、ビルド警告 0。実機は DualShock 4 v.2（Bluetooth）で検証した（Vader 4 Pro は削除済み）。確認不能の項目は `Phase6-Step11-RealDevice-Verification-Checklist.md` §11。
- 静的クラス `Global`（`DS4Windows/DS4Control/Global.cs`、3,472 行。旧 `ScpUtil.cs` は Step10b W5 で `Global.cs`／`BackingStore.cs`／`Changelog.cs` に分割済み）に、呼び出し元が 0 件になった旧ラッパー・旧プロパティが残っている。

### 0.2 方針: 安全防壁付きの物理削除
- Phase6 の成果として、呼び出し元 0 件の旧ラッパーを物理削除し、`Global.cs` をスリム化する。ただし §1 の 5 つの保護防壁に属するものは削除しない。
- 削除は §0A.2 の台帳（削除可 256 件）に基づき、Wave 単位（§3）で行う。

---

## 1. 5大保護防壁（物理削除の絶対禁止リスト）

1. **防壁1: `BackingStore`（`DS4Control/BackingStore.cs`）の全フィールド・配列・プロパティ**: 各 DI サービスが内部で直接参照する正本（SSOT）。Phase7 まで温存。メソッドは要判断（推奨は温存して Phase7 へ。§0A.3、決定5）。
2. **防壁2: Phase7 持ち越し（分類 G）**: `Mapping.cs`（`Global.*` 53 件［うち定数 50］、`AppHost` 6、`rootHub` 1）、その静的 `profileSettings` 束縛（方針 P2、TODO 付き）と温存 3 件、`SyntheticDispatcher`（`outputKBMHandler`／`outputKBMMapping`）、第 1 層（`DS4Device`／`DS4Devices`／`DS4SixAxis`）、`DS4LightBar`。Phase7 の `Mapping` インスタンス化まで温存。
3. **防壁3: Pre-Host ブートストラップ**: `App.xaml.cs` の DI コンテナ構築前の 12 箇所（Step7-0 の再集計）、共用ヘルパー `ApplyLanguageSetting` の 2 件、`AttemptSave` の `appdatapath = null`（TODO 付き）。静的呼び出しのまま温存。
4. **防壁4: 真の定数と純粋計算**: `MAX_DS4_CONTROLLER_COUNT`、`TEST_PROFILE_INDEX`、`ASSEMBLY_RESOURCE_PREFIX`、`RESOURCES_PREFIX` ほかの定数、`Clamp`、`getTransitionedColor` ほか。`DS4_STICK_AXIS_MIDPOINT` は呼び出し元 0 だが、定数のため温存（決定）。
5. **防壁5: ViGEm クライアント依存**: `RefreshViGEmBusInfo`、`IsRunningSupportedViGEmBus`、`vigembusVersion`、`vigemInstalled`。仮想バス刷新まで温存。

---

## 2. 物理削除の対象

- 対象の正本は §0A.2（分類）と §0A.8（Wave 別の名簿）。2026-09-18 版にあった「代表例」の一覧は、現行コードと食い違っていたため廃止した（実際に 0 件だったのは 7 件、存在しないものが 5 件、残りは薄いシムの委譲先として生存）。
- **Step7 からの登録（有効）**: `IPathService.AppDataPath` と `PathService.AppDataPath` のセッター（`{ get; set; }` → `{ get; }`）。理由: private フィールド `_customAppDataPath` に保存するだけで `Global.appdatapath` を変えない孤立した実装（§2.4 で「使うべきでないもの」）。本体の呼び出し元は 0 件。テストは 1 件（`Phase6Step10GroupOneContractTests.cs:100`）で、同時に削除する。`PathService(string appDataPath)` と `_customAppDataPath` は残す。Wave 5 で実施。
- **削除対象外（有効）**: `Global.ResetConnectionFlags()`、`Global.LoadActions()`、`Global.CreateStdActions()` は、DI サービスの委譲先として使われ続ける。
- **削除対象外（今回決定）**: `BackingStore` のメソッド 10 件、DI インターフェースの 0 件メンバー 52 件、`DS4WinWPF.AppHost.Dispose()` は Phase7 の台帳へ登録（決定5）。

---

## 3. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行し、結果を待って次へ進む）

- **Step12-0**: 完了（§0A、§0A.9）。
- **Wave 1**: 孤立コード 24 件＋イベント引数 3 型（`git rm`）。実施済み（§0A.11。`DebouncingMsHasChanged` は回帰確認のため温存）。テストの書き換えを伴う。ラチェット `Global.cs` の `Global` 47→45。
- **Wave 2**: ProfileSettings 系の互換アクセサ 150 名（宣言 152 件）。実施済み（§0A.12）。`Global` 45→38。
- **Wave 3**: `*Instance` のフォールバック群 19 件。実施済み（§0A.13。実機確認待ち）。`Global` 38→26、`AppHost` 11→5、`rootHub` 9→6。**この Wave の後に実機で起動・接続・プロファイル切替を確認**（起動ログ `Host initialized with runtime parser`）。
- **Wave 4**: `BackingStore` 直転送ラッパー 58 件（`getDefaultX360ControlBinding` を含む）。実施済み（§0A.14）。`AppHost` 5→4。
- **Wave 5**: 実施済み（§0A.15）。
- **Wave 6**: プロファイル用 DTO（19 ファイル、4,298 行）と、DTO 側の読み書きの削除。実施済み（§0A.20）。互換 `AppHost`（`DI/Compat/AppHost.cs`）の未使用 5 件、`AppDataPath` のセッター、ガードテストの最終確認、完了報告書（`Phase6-Step12-Completion-Report.md`、`Phase6-Completion-Report.md`）。
- **各 Wave の共通作業**:
  - 対象の削除とテストの書き換え（旧シム専用の等価性テストは削除、書き換え可能なものは生存プロパティ・サービスへ）。削除したテストは一覧にして完了報告書へ記録する。
  - `Step10FourGlobalRatchetGuardTests` の `Global.cs` の基準値を更新する。
  - `Step12DeadShimRemovalGuardTests`（1 ファイル 1 型）に、削除した名前を追記する（再混入の禁止）。防壁の名前の存在確認も含める。
  - 検証: ユーザーが `dotnet build` と `dotnet test` を実行（警告 0、全件成功）。
- **最終のラチェット基準値**: `Global.cs` = (rootHub 6, AppHost 4, Global 26)。全域では `Global.*` 780→747、`AppHost` 48→40、`Program.rootHub` 25→22（Wave 5・6 と §0A.21 を含む最終値）。

---

## 4. Phase7 への最終引き継ぎ台帳

Step12 完了後に残るもの（Phase7 で解体）:

| 残存コンポーネント | 残存理由 | Phase7 での扱い |
|---|---|---|
| `BackingStore` の全フィールド・配列・プロパティ | DI サービスの正本データ | ドメインエンティティへ移譲して消去 |
| `BackingStore` の旧保存系メソッド 10 件（`SaveOld`、`PostProcessLoad`、`UsedSavedProfileString`、`RebuildProfileActionDicts`、`GetDS4Action` ほか） | 3.9.9 互換の保存・読込の設計判断（`576426b8`）に絡む。要判断 | 保存方式の方針決定とあわせて個別に判断 |
| `Global` の生存する実体（分類 D の 144 名） | サービス実装が `Global` の実体へ委譲する薄いシム（§2.1） | 実体をサービス側へ移管して消す |
| 分類 G（`Mapping` の静的 `profileSettings` 束縛 P2、温存 3 件、`SyntheticDispatcher`、第 1 層） | ホットパス。`Mapping` の instance 化が前提 | `Mapping` の instance 化で解消 |
| Pre-Host 12 箇所 ＋ 共用ヘルパー 2 件 ＋ `AttemptSave` の `appdatapath = null` | DI コンテナ構築前の起動 | 起動シーケンスの近代化で整理 |
| `Global.outDevTypeTemp`／`activeOutDevType`（実行時状態の静的配列） | `OutputSlotService` が裏づけとして返す（Step5 の案X） | 所有を `OutputSlotService` へ移す（案Y。`Phase6-Step5-Plan.md` §9） |
| K7-1: DI ホストの暗黙構築（2026-10-17 に解消を確認） | 通常の起動では、`App.xaml.cs` の明示的な `CreateHost(config, parser)` がホストを作る（起動ログ `Host creation path: explicit …` で確認、§0A.21）。`fallbackProfileRepository` は削除済み。残るのは、`-driverinstall` 分岐の `Global.Load()` など、明示的な構築より前に `GetService` が呼ばれる経路だけ（起動引数は `IStartupArguments` 経由で、どちらの順序でも渡る） | 起動シーケンスの近代化（Pre-Host の整理）で扱う |
| `Program.rootHub`（Step12 後 22 件）と `App.xaml.cs` の Service Locator | 共有の入口。`App` が所有者 | 各利用側をコンストラクタ注入へ切り替え、すべてなくなった時点で削除 |
| `DS4WinWPF.AppHost.Dispose()` | 呼び出し元が互換ラッパーだけ。アプリ終了時のホスト破棄が元から無い | 起動・終了シーケンスの整理で扱う |
| `AppHost.CreationNote` と `[DI] Host creation path` の Info ログ | ホストの作成経路の記録。ユーザー決定で残す（2026-10-17、§0A.21） | 起動シーケンスの整理の後に、残すか判断 |
| `DI/Compat/AppHost.cs`（`GetService<T>()` のみ） | `Mapping.cs:86` が使用中 | 参照が無くなった時点でクラスごと削除 |
| DI インターフェースの 0 件メンバー 52 件 | §2.4 の個別の経緯調査が必要 | 個別に判断 |
| K2: `GetProfileActionIndexOf` の未検出時の戻り値 | `ProfileActionProvider` 側に残る | 別途判断 |
| プロファイル用 DTO（`ProfileDTO` 一式）の削除 | 方針決定済み（旧形式を正とする）。Step12 Wave 6 で削除する（§0A.18） | Wave 6 で完了。Phase7 では、旧形式の読み書きを `LegacyProfileXmlSerializer` として切り出し、ゴールデンテストで固定 |
| アプリ設定ほかの DTO 方式（`AppSettingsDTO`、`ActionsDTO`、`LinkedProfiles`、`Auto Profiles`、`OutputSlots`） | 現行の正として使用中 | 「旧形式だけに統一」を広げるかを、Phase7 で判断（§0A.18） |
| 真の定数（`const`、`DS4_STICK_AXIS_MIDPOINT` を含む） | 共通定数値 | ドメインごとの定数クラスへ再配置 |

---

## 5. ロールバック方針

1. Wave ごとにコミットを分け、問題が出た Wave のコミットだけを `git revert` する（直前の Wave の状態へ復帰）。
2. 原因の単離: スタックトレースやビルドエラーから、動的参照されていたメンバーを特定し、保護リストに追加したうえで §0A.8 の名簿を更新する。

---

## 6. 完了判定チェックリスト（Phase6 全体の完了ゲート）

- [x] 5 つの保護防壁（`BackingStore` の正本、Phase7 持ち越し、Pre-Host 12 箇所、定数・純粋計算、ViGEm 依存）が保護されている。`Step12DeadShimRemovalGuardTests` の存在確認が成功している。
- [x] 削除した全メンバーが、削除前の走査で呼び出し元 0 件だった（§0A.8 の名簿と一致）。
- [x] `Global.cs` から旧ラッパーが削除され、ラチェットの基準値が `Global.cs` = (6, 4, 26)、全域 747／40／22 になっている。
- [x] `dotnet build` が警告 0・エラー 0。
- [x] `DS4WindowsTests` と `StandaloneTests` の全件が成功している（テスト数の増減は完了報告書に記録）。
- [x] Wave 3 の後の実機確認（起動・接続・プロファイル切替）が完了している。
- [x] §4 の引き継ぎ台帳が完全に記録されている。
- [x] `Phase6-Step12-Completion-Report.md` と `Phase6-Completion-Report.md` が作成されている。
