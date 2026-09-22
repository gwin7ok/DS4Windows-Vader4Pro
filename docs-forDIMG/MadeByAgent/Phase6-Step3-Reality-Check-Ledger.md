# Phase6-Step3 現行コード突き合わせ報告書 兼 grep 台帳（`Mapping.cs`）

作成日: 2026-09-21  
対象ブランチ: `For-DI-migration-work`（HEAD `77ed523`、2026-09-21 04:54 +0900）  
対象ファイル: `DS4Windows/DS4Control/Mapping.cs`（8,677 行）  
状態: **突き合わせ結果の報告。実装は未着手。§5 の論点に決定を得たうえで `Phase6-Step3-Plan.md` を改訂する**  
上位計画書: `Phase6-Step3-Plan.md`、`Phase6-Status.md` §6.6  
参照エビデンス: `Phase6-Step1-Core-Reference-Evidence.md` §2

---

## 0. 調査方法と限界

- `git clone --depth 1`（HEAD 固定）した作業ツリーを対象に、コメント・文字列リテラルを除去したうえで走査した。
- `Global` クラス（`ScpUtil.cs` 537〜4079 行）の宣言メンバ 476 件を抽出し、`Mapping.cs` 内の (a) `Global.` 修飾参照と (b) `using static DS4Windows.Global;`（31 行）経由の非修飾参照を照合した。別方式（`static` を含む行の全識別子）で取りこぼしを再検証し、追加の取りこぼしがないことを確認した。
- **誤検出 1 件を除外**: 8546 行の非修飾 `OutContType` は列挙型名（`OutContType.DS4`）であり、同行の `Global.OutContType[device]` とは別物。
- **限界**: この環境には dotnet がなく、コンパイル・Roslyn 意味診断は未実施。字句レベルの走査のため、`Mapping` 自身のメンバやローカル変数が `Global` のメンバと同名の場合の遮蔽（シャドーイング）は完全には判定していない。実装時の PR ごとに、ユーザー側のビルド・テストで確認する。
- 「移行先」列の識別子は、`DS4Windows/DI/I*.cs` の実在を機械確認済み（契約追加要と表示した項目を除き、不一致 0 件）。

---

## 1. 件数の突き合わせ

| 区分 | 件数 | 備考 |
|---|---:|---|
| 実参照（置換対象の候補） | **150** | 明示 `Global.` 12 ＋ 非修飾（`using static`）138 |
| 除外：純粋計算（`Clamp` 23、`getTransitionedColor` 2） | 25 | `Global.Clamp` は明示修飾 23 行、`getTransitionedColor` は非修飾 2 行 |
| 除外：真の定数（`MAX_DS4_CONTROLLER_COUNT` / `TEST_PROFILE_ITEM_COUNT`） | 26 | `const int` を実コードで確認済み（ScpUtil.cs 639〜641 行） |
| 合計 | 201 | |

**計画書の「115 件」との関係**: `Phase6-Step3-Plan.md` §0.1 の 115 は、`Phase6-Step1-Core-Reference-Evidence.md` §2 の表の**行数**（9＋34＋38＋29＋5）であり、1 行に複数の行番号を持つ行がある。実際の参照箇所は上表のとおり 201 件（うち実参照 150 件）。同表の行番号のうち、確認した範囲（初期化・座標変換・プロファイル適用の 10 件、`Commit` の `outputKBMMapping` 24 箇所）は現行 HEAD と一致した。ただし `SetCurveAndDeadzone` は、Step1 の表が 34 行、現行の参照が 40 箇所（1 行に複数参照を持つ行がある）。

### 1.1 実参照 150 件の内訳

| 判定 | 件数 | 意味 |
|---|---:|---|
| 置換可 | 135 | 既存契約に対応メンバがあり、機械的に置換できる |
| 契約追加要 | 8 | DI 契約へのメンバ追加が必要 |
| 要判断 | 4 | 置換すると割り当て・ログ増加など挙動差が出るため判断が必要 |
| 温存（案） | 3 | 防御フォールバックとして温存する案 |

| ホットパス区分 | 件数 |
|---|---:|
| H | 103 |
| A | 21 |
| H条件 | 16 |
| H/外部呼出し | 5 |
| N | 2 |
| H条件/A | 2 |
| 到達未確認 | 1 |

区分の表記は Step1 エビデンスに準拠（H＝毎レポート、H条件＝入力経路上だが条件成立時のみ、A＝非同期／マクロ、N＝初期化等）。

---

## 2. `Phase6-Step3-Plan.md` と現行コードの相違点

| # | 計画書の記述 | 現行コード・エビデンス | 影響 |
|---|---|---|---|
| 1 | §2 実装対象 C3-01〜10 の行番号が 107〜147 行の `MapCustom` 内 | 107〜147 行はネスト型 `SyntheticState.KeyPresses` と `ControlToXInput` の定義部で、`MapCustom`（2862 行〜）ではない。実位置は下表のとおり | 行番号・所属メソッドが全件誤り。台帳として使えない |
| 2 | 全 10 件を非ホットパス（N／N†）とする | `MapCustom`（H）、`Scale360degreeGyroAxis`（H）、`GetAbsMouseMapping`（H）など毎レポート経路に属するものを含む | 「非ホットだから引数渡しで安全」という前提が成立しない |
| 3 | §5.1「初期化ブロック」を private ヘルパーに抽出（`ProcessInitialCustomSettings` 例） | 該当する初期化ブロックは `MapCustom` 内に存在しない（10 件は静的フィールド初期化と 4 つの別メソッドに散在） | Step3-1〜3-2 の作業内容が実在しない |
| 4 | C3-05 と C3-09 がともに `ProfilePath[device]` | 現行は 1 箇所（5257 行）のみ | ID が 1 件過剰 |
| 5 | C3-01 の移行先を `IAppSettingsService.AbsUseAllMonitors` | `IAppSettingsService` に該当メンバなし。`absUseAllMonitors` は `PrepareAbsMonitorBounds` が更新する実行時状態で、永続設定ではない | 移行先の再決定が必要（論点3） |
| 6 | C3-07 の移行先を `IProfileXmlStore.SaveControllerConfigsForDevice(...)` | `IProfileXmlStore` には `LoadControllerConfigsForDevice` のみ。Save 側は未定義 | 契約追加が必要 |
| 7 | C3-06 `Global.ApplyProfile` を引数渡し／コールバックに置換 | DI 未登録時のフォールバックブロック内（5303〜5333 行）で、外側で `HaltReportingRunAction` を実行している。主経路は `DispatchInputEdge`。`IProfileApplicationService.ApplyProfile` は 4 以上のスロットを拒否し（ProfileApplicationService.cs 103 行）、`Global.ApplyProfile` の戻り値を捨てて成功扱いにし、MappingAction では自身で Halt しない（Step1 エビデンス §7 の 522〜523 行） | 無条件の置換は不適切（論点4） |
| 8 | 残り 101 件を「Phase7 引き継ぎ」とし、`using static` を残す | `using static` 経由の非修飾参照が 138 件。うち 60 種のメンバは大半が既存契約で置換可能（判定表参照） | 暗黙結合が Step12 まで残る。方針の再確認が必要（論点1） |
| 9 | §3 の引き継ぎ ID（C3-H01〜H101）と行番号 | 行番号は実コードと一致しない（例: `SetCurveAndDeadzone` は 1593 行から始まり、参照は 1595〜2778 行に現れる。計画書は 871〜1065 行） | 引き継ぎ台帳として使えない。本書 §4 で再作成 |

### 2.1 計画書の 10 件の実位置（Step1 エビデンスと現行 HEAD で一致）

| 計画書 ID | 実際の行 | メンバ | 所属メソッド | 区分 |
|---|---|---|---|---|
| C3-01 | 3640 U / 3674 Q | `absUseAllMonitors` | `MapCustom` | H |
| C3-02 | 3658, 3676 Q | `TranslateCoorToAbsDisplay` | `MapCustom` | H |
| C3-03/04 | 3668 U（と 6752 U） | `ButtonAbsMouseInfos` | `MapCustom（と GetAbsMouseMapping）` | H |
| C3-05/09 | 5257 U | `ProfilePath` | `MapCustomAction` | H条件 |
| C3-06 | 5315 Q | `ApplyProfile` | `MapCustomAction（Task.Run 内）` | A |
| C3-07 | 8249 Q | `SaveControllerConfigs` | `SAWheelEmulationCalibration` | H条件 |
| C3-08 | 8423 Q | `LoadControllerConfigs` | `Scale360degreeGyroAxis` | H条件 |
| C3-10 | 8546 Q | `OutContType` | `Scale360degreeGyroAxis` | H |

---

## 3. 計画書に無かった重要事項

### 3.1 静的束縛ハザード（`Mapping` の静的 `profileSettings`）

- `Mapping.cs` 73〜75 行の `profileSettings` は `static readonly` で、`Mapping` の型初期化時に `AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance` で束縛される。
- 型初期化時点で `AppHost` が未構築だと、`Global` 側の別インスタンス（`new ProfileSettingsService()` のフォールバック。宣言は ScpUtil.cs 715 行、getter の返却は 740 行）に**恒久的に**束縛される。
- 設定配列の多くは `Global.m_Config`（静的）へ委譲されるため、インスタンスが違っても値は同じになる。**ただし `OutputKBMMapping` は `{ get; set; }` の自動プロパティ（ProfileSettingsService.cs 600 行）でインスタンス自身が状態を持つ**。現行の非修飾 `outputKBMMapping` は `Global.ProfileSettingsServiceInstance` 経由（ScpUtil.cs 1091〜1095 行）で常に DI の Singleton を指すため、51 箇所を `profileSettings.OutputKBMMapping` に置換すると、束縛タイミング次第で参照先が変わり得る。
- 本番では `ControlService` 生成（Post-Host）後に `Mapping` へ初めて触れるため通常は問題にならないが、テスト（`StandaloneTests` が `Mapping` を直接参照）や将来の初期化順序変更で顕在化する。Step2 の教訓 B.13（静的初期化の循環）と同種。

### 3.2 機械的に置換できない項目

| 項目 | 箇所 | 理由 |
|---|---|---|
| `getProfileActions` → `PA.GetProfileActionNames` | 3274, 4946, 5320 | 実装が毎回 `ToArray()`。3274 行は `MapCustom`（H）。`P.ProfileActions[i]` を使えば割り当てなしで同一 |
| `GetProfileAction` → `PA.GetProfileAction` | 3285, 4978, 5324 | 実装が呼出ごとに `[DI] ... GetProfileAction` の Trace ログを出す（Trace 有効時）。3285 行は毎レポート・アクションごとに到達し得る。ログ維持ルール（copilot-instructions §2.3）との兼ね合い |
| `reverseX360ButtonMapping` → `P.GetReverseX360ButtonMapping()` | 4601 | DI getter は毎回 `Clone()`（ProfileSettingsService.cs 611 行）。`ProcessControlSettingAction`（H）で割り当てが発生 |
| `Global.ApplyProfile` → `AP.ApplyProfile` | 5315 | §2 の相違点 #7。`IProfileApplicationService` 側の K1（`deviceIndex >= 4`）と未解決 |
| `GetActions()` → `SA.Actions` | 4887, 4938 | `Actions` はコピーを返す。`SA.ActionList`（実体）を使う |
| `TranslateCoorToAbsDisplay` | 3658, 3676 | `absDisplayBounds`／`fullDesktopBounds` の状態に依存。純粋関数ではない |

### 3.3 `Global` 以外の静的結合

| 種別 | 箇所 | 備考 |
|---|---|---|
| Service Locator（`AppHost.GetService`） | 74, 77, 78, 6628 | 73〜75 行（`profileSettings`）と 76〜77 行（`profileApplication`）は `static readonly`（型初期化時に 1 回束縛。`profileApplication` は `??` フォールバックが無く、未構築なら null のまま）。78 行 `VirtualKBM` は毎回解決するプロパティ。6628 行は `IDeviceStateAccessor`（Rumble マクロ） |
| Service Locator（`ServiceProviderHolder.Provider`） | 212, 248, 301, 1341, 1418 | `TriggerContext` を組み立てて `ActionManager` へ渡す箇所 |
| `Program.rootHub` 直接参照 | 6632 | `IDeviceStateAccessor` 取得失敗時のフォールバック（`d == null`）。§2.1 により温存が原則 |
| `ActionManager` 静的ファサード | 17 行 | 意図された静的ファサード（全体計画書 §2.5）。Step3 の対象外 |
| `DS4LightBar` 静的メンバ | 32 行 | Step3 の対象外（`DS4LightBar.cs` は Step1 の別対象） |
| `ControlService.MAX_DS4_CONTROLLER_COUNT` | 652 | 定数（除外） |

### 3.4 テストへの影響

`Mapping.` を参照する既存テストは 4 ファイルのみ（`ControlTabAndSpecialActionKeyTests`、`DefaultActionManagerTests`、`MappingSpecialActionSuppressionTests`、`MappingControllerLifecycleTests`）。いずれも `ClearSyntheticActionCache`、`GetOrCreateSyntheticKeyAction`、`suppressedTriggerButtons`、`ClearKeyButtonControllersForDevice` などで、ホットパス（`SetCurveAndDeadzone`／`Commit`／`MapCustom`）の回帰を守るテストは**現状存在しない**。ホットパスを触る PR には、事前に特性化テストの追加が必要。

---

## 4. grep 台帳（実参照 150 件）

凡例: 形式 Q＝`Global.` 修飾、U＝`using static` による非修飾。判定は本書の**提案**であり、決定前の暫定値。

### `(静的フィールド初期化)`（N、2 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 75 | Q | `ProfileSettingsServiceInstance` | N | - | 温存（案） | 静的初期化の `??` フォールバック。防御コードとして温存（Step2 C2-02 と同扱い） |
| 78 | Q | `outputKBMHandler` | N | - | 温存（案） | `VirtualKBM` getter の `??` フォールバック。防御コードとして温存 |

### `Commit`（H、24 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 1356 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1363 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1369 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1375 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1381 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1387 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1389 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1391 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1393 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1395 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1433 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1440 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1447 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1453 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1460 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1466 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1473 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1479 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1486 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1492 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1499 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1501 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1510 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 1512 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `SetCurveAndDeadzone`（H、40 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 1595 | U | `getLSRotation` | H | IProfileSettingsService.`LSRotation` | 置換可 |  |
| 1599 | U | `getRSRotation` | H | IProfileSettingsService.`RSRotation` | 置換可 |  |
| 1603 | U | `GetLSAntiSnapbackInfo` | H | IProfileSettingsService.`LSAntiSnapbackInfo` | 置換可 |  |
| 1604 | U | `GetRSAntiSnapbackInfo` | H | IProfileSettingsService.`RSAntiSnapbackInfo` | 置換可 |  |
| 1616 | U | `GetLSDeadInfo` | H | IProfileSettingsService.`LSModInfo` | 置換可 |  |
| 1617 | U | `GetRSDeadInfo` | H | IProfileSettingsService.`RSModInfo` | 置換可 |  |
| 2107 | U | `GetL2ModInfo` | H | IProfileSettingsService.`L2ModInfo` | 置換可 |  |
| 2162 | U | `GetR2ModInfo` | H | IProfileSettingsService.`R2ModInfo` | 置換可 |  |
| 2216 | U | `getLSSens` | H | IProfileSettingsService.`LSSens` | 置換可 |  |
| 2227 | U | `getRSSens` | H | IProfileSettingsService.`RSSens` | 置換可 |  |
| 2235 | U | `getL2Sens` | H | IProfileSettingsService.`L2Sens` | 置換可 |  |
| 2239 | U | `getR2Sens` | H | IProfileSettingsService.`R2Sens` | 置換可 |  |
| 2243 | U | `GetSquareStickInfo` | H | IProfileSettingsService.`SquStickInfo` | 置換可 |  |
| 2262 | U | `getLsOutCurveMode` | H | IProfileSettingsService.`GetLsOutCurveMode` | 置換可 |  |
| 2371 | U | `lsOutBezierCurveObj` | H | IProfileSettingsService.`LsOutBezierCurveObj` | 置換可 |  |
| 2372 | U | `lsOutBezierCurveObj` | H | IProfileSettingsService.`LsOutBezierCurveObj` | 置換可 |  |
| 2385 | U | `lsOutBezierCurveObj` | H | IProfileSettingsService.`LsOutBezierCurveObj` | 置換可 |  |
| 2386 | U | `lsOutBezierCurveObj` | H | IProfileSettingsService.`LsOutBezierCurveObj` | 置換可 |  |
| 2409 | U | `getRsOutCurveMode` | H | IProfileSettingsService.`GetRsOutCurveMode` | 置換可 |  |
| 2518 | U | `rsOutBezierCurveObj` | H | IProfileSettingsService.`RsOutBezierCurveObj` | 置換可 |  |
| 2519 | U | `rsOutBezierCurveObj` | H | IProfileSettingsService.`RsOutBezierCurveObj` | 置換可 |  |
| 2531 | U | `rsOutBezierCurveObj` | H | IProfileSettingsService.`RsOutBezierCurveObj` | 置換可 |  |
| 2532 | U | `rsOutBezierCurveObj` | H | IProfileSettingsService.`RsOutBezierCurveObj` | 置換可 |  |
| 2537 | U | `getL2OutCurveMode` | H | IProfileSettingsService.`GetL2OutCurveMode` | 置換可 |  |
| 2576 | U | `l2OutBezierCurveObj` | H | IProfileSettingsService.`L2OutBezierCurveObj` | 置換可 |  |
| 2580 | U | `getR2OutCurveMode` | H | IProfileSettingsService.`GetR2OutCurveMode` | 置換可 |  |
| 2619 | U | `r2OutBezierCurveObj` | H | IProfileSettingsService.`R2OutBezierCurveObj` | 置換可 |  |
| 2624 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |
| 2627 | U | `getSXDeadzone` | H | IProfileSettingsService.`SXDeadzone` | 置換可 |  |
| 2628 | U | `getSZDeadzone` | H | IProfileSettingsService.`SZDeadzone` | 置換可 |  |
| 2629 | U | `getSXMaxzone` | H | IProfileSettingsService.`SXMaxzone` | 置換可 |  |
| 2630 | U | `getSZMaxzone` | H | IProfileSettingsService.`SZMaxzone` | 置換可 |  |
| 2631 | U | `getSXAntiDeadzone` | H | IProfileSettingsService.`SXAntiDeadzone` | 置換可 |  |
| 2632 | U | `getSZAntiDeadzone` | H | IProfileSettingsService.`SZAntiDeadzone` | 置換可 |  |
| 2633 | U | `getSXSens` | H | IProfileSettingsService.`SXSens` | 置換可 |  |
| 2634 | U | `getSZSens` | H | IProfileSettingsService.`SZSens` | 置換可 |  |
| 2680 | U | `getSXOutCurveMode` | H | IProfileSettingsService.`GetSxOutCurveMode` | 置換可 |  |
| 2727 | U | `sxOutBezierCurveObj` | H | IProfileSettingsService.`SxOutBezierCurveObj` | 置換可 |  |
| 2731 | U | `getSZOutCurveMode` | H | IProfileSettingsService.`GetSzOutCurveMode` | 置換可 |  |
| 2778 | U | `szOutBezierCurveObj` | H | IProfileSettingsService.`SzOutBezierCurveObj` | 置換可 |  |

### `ApplyStickCalibration`（H、8 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 2788 | U | `RightStickDriftXAxis` | H | IProfileSettingsService.`RightStickDriftXAxis` | 置換可 |  |
| 2790 | U | `RightStickDriftXAxis` | H | IProfileSettingsService.`RightStickDriftXAxis` | 置換可 |  |
| 2793 | U | `RightStickDriftYAxis` | H | IProfileSettingsService.`RightStickDriftYAxis` | 置換可 |  |
| 2795 | U | `RightStickDriftYAxis` | H | IProfileSettingsService.`RightStickDriftYAxis` | 置換可 |  |
| 2799 | U | `LeftStickDriftXAxis` | H | IProfileSettingsService.`LeftStickDriftXAxis` | 置換可 |  |
| 2801 | U | `LeftStickDriftXAxis` | H | IProfileSettingsService.`LeftStickDriftXAxis` | 置換可 |  |
| 2805 | U | `LeftStickDriftYAxis` | H | IProfileSettingsService.`LeftStickDriftYAxis` | 置換可 |  |
| 2807 | U | `LeftStickDriftYAxis` | H | IProfileSettingsService.`LeftStickDriftYAxis` | 置換可 |  |

### `MapCustom`（H、10 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 2896 | U | `getProfileActionCount` | H | IProfileActionProvider.`GetProfileActionCount` | 置換可 | BackingStore 直参照で Global と同一 |
| 2910 | U | `GetControlSettingsGroup` | H | IProfileSettingsService.`GetControlSettingsGroup` **(未定義)** | 契約追加要 | 契約に存在しない（`GetDS4CSettings` の List とは別型） |
| 3274 | U | `getProfileActions` | H | IProfileSettingsService.`ProfileActions` | 置換可 | `ProfileActions[i]`（List直参照）。PA.`GetProfileActionNames` は毎回 `ToArray()` のため不可 |
| 3285 | U | `GetProfileAction` | H | IProfileActionProvider.`GetProfileAction` | 要判断 | 実装が呼出ごとに Trace ログを出す（Trace有効時のみ）。ホットパスでのログ増加を要判断 |
| 3459 | U | `GetSASteeringWheelEmulationAxis` | H | IProfileSettingsService.`GetSASteeringWheelEmulationAxis` | 置換可 |  |
| 3640 | U | `absUseAllMonitors` | H | IEnvironmentService(案).`AbsUseAllMonitors` **(未定義)** | 契約追加要 | 契約に存在しない。移行先は論点3 |
| 3658 | Q | `TranslateCoorToAbsDisplay` | H | IEnvironmentService(案).`TranslateCoorToAbsDisplay` **(未定義)** | 契約追加要 | 契約に存在しない。状態依存（純粋関数ではない）。移行先は論点3 |
| 3668 | U | `ButtonAbsMouseInfos` | H | IProfileSettingsService.`ButtonAbsMouseInfos` | 置換可 |  |
| 3674 | Q | `absUseAllMonitors` | H | IEnvironmentService(案).`AbsUseAllMonitors` **(未定義)** | 契約追加要 | 契約に存在しない。移行先は論点3 |
| 3676 | Q | `TranslateCoorToAbsDisplay` | H | IEnvironmentService(案).`TranslateCoorToAbsDisplay` **(未定義)** | 契約追加要 | 契約に存在しない。状態依存（純粋関数ではない）。移行先は論点3 |

### `ProcessControlSettingAction`（H、4 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 4461 | U | `ButtonMouseInfos` | H | IProfileSettingsService.`ButtonMouseInfos` | 置換可 |  |
| 4475 | U | `ButtonMouseInfos` | H | IProfileSettingsService.`ButtonMouseInfos` | 置換可 |  |
| 4565 | U | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 4601 | U | `reverseX360ButtonMapping` | H | IProfileSettingsService.`GetReverseX360ButtonMapping` | 要判断 | DI getter は毎回 Clone。ホットパスで割り当てが発生 |

### `IfAxisIsNotModified`（到達未確認、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 4877 | U | `GetDS4CSetting` | 到達未確認 | IProfileSettingsService.`GetDS4CSetting` | 置換可 |  |

### `LogActionDoneCountOnTrigger`（H条件、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 4887 | U | `GetActions` | H条件 | ISpecialActionRepository.`ActionList` | 置換可 | `Actions` はコピーを返すため `ActionList`（実体）を使う |

### `MapCustomAction`（H条件、15 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 4938 | U | `GetActions` | H条件 | ISpecialActionRepository.`ActionList` | 置換可 | `Actions` はコピーを返すため `ActionList`（実体）を使う |
| 4946 | U | `getProfileActions` | H条件 | IProfileSettingsService.`ProfileActions` | 置換可 | `ProfileActions[i]`（List直参照）。PA.`GetProfileActionNames` は毎回 `ToArray()` のため不可 |
| 4978 | U | `GetProfileAction` | H条件 | IProfileActionProvider.`GetProfileAction` | 要判断 | 実装が呼出ごとに Trace ログを出す（Trace有効時のみ）。ホットパスでのログ増加を要判断 |
| 4979 | U | `GetProfileActionIndexOf` | H条件 | IProfileActionProvider.`GetProfileActionIndexOf` **(未定義)** | 契約追加要 | 契約に存在しない（プロファイル別の独自辞書。SA.GetActionIndex とは別物） |
| 4998 | U | `GetDS4CSetting` | H条件 | IProfileSettingsService.`GetDS4CSetting` | 置換可 |  |
| 5257 | U | `ProfilePath` | H条件 | IProfileRepository.`ProfilePath` | 置換可 |  |
| 5263 | U | `GetDS4CSetting` | H条件 | IProfileSettingsService.`GetDS4CSetting` | 置換可 |  |
| 5268 | U | `outputKBMMapping` | H条件 | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 5276 | U | `outputKBMMapping` | H条件 | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 5315 | Q | `ApplyProfile` | A | IProfileApplicationService.`ApplyProfile` | 温存（案） | DI未登録時フォールバック内・二重実行防止付き。AP側に不備あり（§3.2） |
| 5320 | U | `getProfileActions` | A | IProfileSettingsService.`ProfileActions` | 置換可 | `ProfileActions[i]`（List直参照）。PA.`GetProfileActionNames` は毎回 `ToArray()` のため不可 |
| 5324 | U | `GetProfileAction` | A | IProfileActionProvider.`GetProfileAction` | 要判断 | 実装が呼出ごとに Trace ログを出す（Trace有効時のみ）。ホットパスでのログ増加を要判断 |
| 5325 | U | `GetProfileActionIndexOf` | A | IProfileActionProvider.`GetProfileActionIndexOf` **(未定義)** | 契約追加要 | 契約に存在しない（プロファイル別の独自辞書。SA.GetActionIndex とは別物） |
| 5539 | Q | `outputKBMMapping` | H条件 | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6089 | U | `GetDS4CSetting` | H条件 | IProfileSettingsService.`GetDS4CSetting` | 置換可 |  |

### `ReleaseActionKeys`（H条件、3 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6122 | U | `GetDS4CSetting` | H条件 | IProfileSettingsService.`GetDS4CSetting` | 置換可 |  |
| 6127 | U | `outputKBMMapping` | H条件 | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6135 | U | `outputKBMMapping` | H条件 | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `PlayMacroCodeValue`（A、14 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6515 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6519 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6523 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6527 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6531 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6535 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6536 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6559 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6563 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6567 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6571 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6575 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6579 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6580 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `AltTabSwapping`（A、3 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6683 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6692 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6694 | U | `outputKBMMapping` | A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `AltTabSwappingRelease`（H条件/A、2 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6705 | U | `outputKBMMapping` | H条件/A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6707 | U | `outputKBMMapping` | H条件/A | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `GetMouseWheelMapping`（H、2 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6721 | Q | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |
| 6722 | Q | `outputKBMMapping` | H | IProfileSettingsService.`OutputKBMMapping` | 置換可 | インスタンス状態のため §3.1 の束縛ハザードに注意 |

### `GetAbsMouseMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 6752 | U | `ButtonAbsMouseInfos` | H | IProfileSettingsService.`ButtonAbsMouseInfos` | 置換可 |  |

### `getMouseMapping`（H、3 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7092 | U | `getLSDeadzone` | H | IProfileSettingsService.`LSModInfo` | 置換可 | `LSModInfo[i].deadZone` |
| 7094 | U | `getRSDeadzone` | H | IProfileSettingsService.`RSModInfo` | 置換可 | `RSModInfo[i].deadZone` |
| 7098 | U | `ButtonMouseInfos` | H | IProfileSettingsService.`ButtonMouseInfos` | 置換可 |  |

### `GetByteMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7506 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |

### `GetBoolMappingExternal`（H/外部呼出し、5 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7631 | U | `IsUsingSAForControls` | H/外部呼出し | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |
| 7635 | U | `SXSens` | H/外部呼出し | IProfileSettingsService.`SXSens` | 置換可 |  |
| 7636 | U | `SXSens` | H/外部呼出し | IProfileSettingsService.`SXSens` | 置換可 |  |
| 7637 | U | `SZSens` | H/外部呼出し | IProfileSettingsService.`SZSens` | 置換可 |  |
| 7638 | U | `SZSens` | H/外部呼出し | IProfileSettingsService.`SZSens` | 置換可 |  |

### `GetBoolMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7694 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |

### `getBoolSpecialActionMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7838 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |

### `GetBoolActionMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 7936 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |

### `GetXYAxisMapping`（H、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 8043 | U | `IsUsingSAForControls` | H | IProfileSettingsService.`GyroOutputMode` | 置換可 | `GyroOutputMode[i]==GyroOutMode.Controls`。契約追加不要（Mapping内private補助で1本化可） |

### `SAWheelEmulationCalibration`（H条件、1 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 8249 | Q | `SaveControllerConfigs` | H条件 | IProfileXmlStore.`SaveControllerConfigsForDevice` **(未定義)** | 契約追加要 | 契約に存在しない（Load 側のみ Step2-PR-2 で追加済み） |

### `Scale360degreeGyroAxis`（H、6 件）

| 行 | 形式 | メンバ | 区分 | 移行先 | 判定 | 備考 |
|---:|:-:|---|:-:|---|---|---|
| 8423 | Q | `LoadControllerConfigs` | H | IProfileXmlStore.`LoadControllerConfigsForDevice` | 置換可 | Step2-PR-2 で追加済み |
| 8456 | U | `SAWheelFuzzValues` | H | IProfileSettingsService.`SAWheelFuzzValues` | 置換可 |  |
| 8471 | U | `getSXDeadzone` | H | IProfileSettingsService.`SXDeadzone` | 置換可 |  |
| 8530 | U | `WheelSmoothInfo` | H | IProfileSettingsService.`WheelSmoothInfo` | 置換可 |  |
| 8543 | U | `getSXAntiDeadzone` | H | IProfileSettingsService.`SXAntiDeadzone` | 置換可 |  |
| 8546 | Q | `OutContType` | H | IProfileSettingsService.`OutContType` | 置換可 |  |

### 4.1 除外項目（純粋計算・定数）

| メンバ | 件数 | 行 |
|---|---:|---|
| `MAX_DS4_CONTROLLER_COUNT` | 23 | 173, 610, 611, 614, 637, 677, 733, 740, 956, 963, 973, 977, 982, 988, 1015, 1018, 1019, 1020, 1021, 1024, 1035, 1209, 2838 |
| `Clamp` | 23 | 1678, 1679, 1686, 1687, 1793, 1839, 1911, 1912, 1920, 1921, 2028, 2074, 2123, 2133, 2178, 2188, 2219, 2220, 2230, 2231, 2237, 2241, 8197 |
| `TEST_PROFILE_ITEM_COUNT` | 3 | 590, 603, 622 |
| `getTransitionedColor` | 2 | 5628, 5630 |

---

## 5. 決定をお願いする論点（提案は暫定）

本書 §1〜§3 を踏まえ、`Phase6-Step3-Plan.md` を改訂する前に、次の 4 点の決定が必要。詳細な選択肢（メリット・デメリット・推奨）はチャットで提示した内容を正とする。

| 論点 | 内容 | 暫定推奨 |
|---|---|---|
| 1 | Step3 の範囲（計画書どおり 10 箇所＋引き継ぎ／全 150 件を一括／段階的に解消） | 段階的に解消 |
| 2 | 静的 `Mapping` へのサービスの渡し方（既存の Service Locator 継続／明示的な静的注入口／引数渡し） | 明示的な静的注入口（モデル図 04 の追記が必要） |
| 3 | 契約追加の置き場（モニター座標系・`SaveControllerConfigsForDevice`・`GetProfileActionIndexOf`・`GetControlSettingsGroup`） | モニター座標系は `IEnvironmentService`（モデル図 04 が責務に明記済み） |
| 4 | 機械置換できない項目（`ApplyProfile`、フォールバック 2 件、`reverseX360ButtonMapping`、`GetProfileAction` のログ） | Step3 では温存し TODO コメントを付与、根拠のある項目のみ個別対応 |