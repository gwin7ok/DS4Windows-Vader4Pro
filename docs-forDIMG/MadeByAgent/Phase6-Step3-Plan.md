# Phase6-Step3 計画書: `Mapping.cs` の段階的引数渡しによる `Global` 直接参照の解消

作成日: 2026-09-09  
改訂日: 2026-09-21（実地突き合わせに基づく全面改訂。案A［局所10件＋Phase7一括引き継ぎ］を撤回し、**全150件を段階的に解消する方針（論点1〜4の決定）へ変更**）  
状態: 計画確定・実装未着手（Step3-1 から着手可能）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §5.5, §6.9  
参照エビデンス:

- `Phase6-Step3-Reality-Check-Ledger.md`（2026-09-21、現行コードとの突き合わせ・grep 台帳。**本計画書の一次資料**）
- `Phase6-Step1-Core-Reference-Evidence.md` §2（初期調査。行番号は現行 HEAD と一部不一致、Ledger で補正済み）

---

## 0. 改訂の経緯と決定事項

### 0.1 旧計画（案A）を撤回した理由

2026-09-18 時点の計画（案A: 「非ホットパス10件のみ実装し、残り101件はPhase7へ一括引き継ぎ」）は、2026-09-21 の実地突き合わせ（`Phase6-Step3-Reality-Check-Ledger.md`）により、次の理由で前提が崩れていることが判明した。

1. 実装対象10件の行番号・所属メソッド・ホットパス区分が全件、現行コードと一致しなかった。
2. 残り101件超（実際には140件）の大半（135件）は、既存の DI 契約に同名メンバが既に存在し、`using static` を外すだけで機械的に置換できることが判明した。Phase7（`Mapping` の完全 instance 化）を待たずに解消できる。
3. 全件を一括で「引き継ぎ台帳化してコメントを付けるだけ」に留めると、`using static DS4Windows.Global;`（31行目）が Step12（旧シム削除判定）まで残り続け、`Global` の呼出元0件化が Phase7 完了までブロックされる。

### 0.2 決定事項（2026-09-21、4論点）

| 論点 | 決定 |
|---|---|
| **1. Step3 の範囲** | **C: 段階的に解消**。実参照150件（後述 §1）を、本計画書 §3 のバッチ構成で複数の Step3-N（PR）に分けて全件解消する。Phase7 へ丸ごと引き継ぐ方式は取らない。 |
| **2. 引数渡しの方式** | **S3: 引数渡し**（旧計画の設計をそのまま採用）。`Mapping` に新たな静的注入口・Service Locator は追加しない。呼び出し元（`ControlService` 等、Step2 で DI 済み）が保持するサービスインスタンスを、`Mapping` の対象メソッドへ引数として渡す。 |
| **3. 未定義メンバの移行先** | ・モニター座標系（`absUseAllMonitors`, `TranslateCoorToAbsDisplay`）→ **新設 `IDisplayCoordinateService`**（Step1 提案採用）。既存 `IEnvironmentService.PrepareAbsMonitorBounds` と状態を共有するため、実装時に集約方針を確定する（§2.3）。<br>・`SaveControllerConfigs` → `IProfileXmlStore.SaveControllerConfigsForDevice`<br>・`GetProfileActionIndexOf` → `IProfileActionProvider.GetProfileActionIndexOf`<br>・`GetControlSettingsGroup` → `IProfileSettingsService.GetControlSettingsGroup`<br>モデル図 03・04 は本決定に合わせて更新済み（`IProfileXmlStore` の実シグネチャ反映、`IDisplayCoordinateService` 追加）。 |
| **4. 機械置換できない4項目** | §2.4 のとおり、3項目（`getProfileActions`/`GetProfileAction`/`GetActions` 系）は挙動を変えずに早期解消できると判定し、Step3-2 で対応する。残り2項目（`reverseX360ButtonMapping` の Clone、`Global.ApplyProfile` フォールバック）は挙動差・既存不具合（K1）を伴うため、選択肢を提示し（§2.4.2〜§2.4.3）、実装 Step 直前に個別確認する。 |

---

## 1. 対象件数（`Phase6-Step3-Reality-Check-Ledger.md` §1 に基づく）

| 区分 | 件数 |
|---|---:|
| 実参照（Step3 で解消する対象） | **150** |
| 除外（純粋計算 `Clamp`/`getTransitionedColor`、真の定数） | 51 |
| 合計走査対象 | 201 |

実参照150件の判定内訳:

| 判定 | 件数 | Step3 での扱い |
|---|---:|---|
| 置換可（OK） | 135 | §3 のバッチで機械的に置換 |
| 契約追加要（ADD） | 8 | Step3-1 で契約を追加してから置換（対象メンバ5種: `absUseAllMonitors`, `TranslateCoorToAbsDisplay`, `SaveControllerConfigs`, `GetProfileActionIndexOf`, `GetControlSettingsGroup`） |
| 要判断（JUDGE） | 4 | §2.4 のとおり個別対応（対象メンバ2種: `GetProfileAction`, `reverseX360ButtonMapping`） |
| 温存（KEEP） | 3 | 防御用フォールバックとして温存し、TODO コメントのみ付与（`ProfileSettingsServiceInstance`, `outputKBMHandler`, `Global.ApplyProfile`） |

行番号・所属メソッド・移行先の全件詳細は `Phase6-Step3-Reality-Check-Ledger.md` §4 の grep 台帳を正とする（本書では重複記載しない）。

---

## 2. アーキテクチャ設計

### 2.1 引数渡し（S3）の設計方針

- `Mapping` の各 `static` メソッドは、必要とするサービスをそのまま引数として受け取る。`Mapping` 自身が `AppHost.GetService<T>()` を新たに呼ぶことはしない（既存の `profileSettings`/`profileApplication`/`VirtualKBM` の3つの静的束縛は Step3 では変更しない。これらは Step2 で確立済みの過渡的な Service Locator であり、置き換えは本 Step の対象外とする）。
- 呼び出し元は主に `ControlService`（Step2 で `IProfileSettingsService` 等をコンストラクタ注入済み）と `MainWindow`/`ControllerReadingsControl` 等の UI 呼び出し箇所である。`SetCurveAndDeadzone` 等は UI からも直接呼ばれるため、呼び出し元ごとに引数の調達経路（DI 済みフィールドを渡す／`AppHost.GetService<T>()` で都度解決する）を個別に確認する。
- **シグネチャの肥大化対策**: 1メソッドが複数サービス（`IProfileSettingsService`, `IProfileActionProvider`, `IProfileXmlStore` 等）を必要とする場合、個々の引数を並べるのではなく、Step3 で使うサービスをまとめた読み取り専用のコンテキスト構造体（例: `MappingServiceContext { IProfileSettingsService ProfileSettings; IProfileActionProvider ProfileActions; IProfileXmlStore ProfileXmlStore; IDisplayCoordinateService DisplayCoordinates; IProfileRepository ProfileRepository; }`）を1引数として渡す。これも Pure DI の引数渡し（S3）の一形態であり、Service Locator ではない。呼び出し元は既存の DI 済みフィールドから、このコンテキストを1回組み立てて渡すのみで済む。
- ホットパスでの新規ヒープ割り当てを避けるため、コンテキスト構造体は `readonly struct`（またはクラスをホットパス外で1回だけ生成しキャッシュ）とする。Step3-2 着手時に具体案（struct か、`ControlService` にキャッシュ済みシングルトンを持たせるか）を確定する。

### 2.2 バッチ分割の考え方

`Phase6-Step3-Reality-Check-Ledger.md` §1.1 のホットパス区分（H / H条件 / A / N）と、既存契約への適合度（OK / ADD / JUDGE / KEEP）の両軸で、**リスクが低い順**にバッチを組む（§3）。

- 契約整備（新設・追加）を先に済ませ、コード置換とインターフェース設計を同一 PR に混在させない（3.3 原則1の分割禁止規定と同じ考え方を、機能追加と呼び出し元差し替えの分離にも適用する）。
- 温存（KEEP）・要判断（JUDGE）の対象は最後のバッチに回し、それまでのバッチで着実に `using static` の依存を減らす。
- 各バッチの完了ごとに `dotnet build` / `dotnet test` のグリーンを確認してからコミットする（マイクロステップの原則）。

### 2.3 `IDisplayCoordinateService` と `IEnvironmentService.PrepareAbsMonitorBounds` の関係（Step3-1 で確定する設計判断）

`PrepareAbsMonitorBounds` は Phase6-Step2 で既に `IEnvironmentService` に追加され、`ControlService` から `_environmentService.PrepareAbsMonitorBounds(edid)` として呼ばれている（`Global.PrepareAbsMonitorBounds` への薄い委譲）。この処理は `absUseAllMonitors` / `absDisplayBounds` / `fullDesktopBounds` という、新設 `IDisplayCoordinateService` が扱うのと同じ状態を更新する。Step3-1 の実装時に以下のいずれかを選び、モデル図の注釈どおり確定する。

| 案 | 内容 | メリット | デメリット |
|---|---|---|---|
| **D1（推奨）** | `PrepareAbsMonitorBounds` を `IDisplayCoordinateService` に移し、`IEnvironmentService` 側は薄い委譲として残す（`Global` と同じ Strangler Fig パターン） | 状態と更新処理が同じサービスに閉じる。責務が一貫する | Step2 で完了済みの `ControlService` の依存先を1箇所変更する（`_environmentService.PrepareAbsMonitorBounds` → `_displayCoordinateService.PrepareAbsMonitorBounds`）。ただし振る舞いは変わらない |
| D2 | `PrepareAbsMonitorBounds` は `IEnvironmentService` に残し、`IDisplayCoordinateService` は読み取り専用（`UseAllMonitors` の get と `TranslateCoorToAbsDisplay`）のみを持つ | Step2 完了分に触れない | 状態の更新者（`IEnvironmentService`）と参照者（`IDisplayCoordinateService`）が分かれ、`Global` 越しに間接的に同期する構造がもう1段階増える |

Step3-1 の実装 PR 内で D1 を既定として進めるが、`ControlService` 側の依存変更を伴うため、実装直前に一度確認を取る。

### 2.4 機械置換できない4項目の対応（論点4）

#### 2.4.1 早期解消する3項目（Step3-2 で対応）

| 項目 | 対応方針 | 根拠 |
|---|---|---|
| `getProfileActions(device)` → `PA.GetProfileActionNames(...)` | 使わず、`IProfileActionProvider.ProfileActions[device]`（`List<string>` の実体）を直接参照する形に置換する | `GetProfileActionNames` 相当は毎回 `ToArray()` する実装だが、`ProfileActions[device]` は元の `Global.getProfileActions` と同じ実体参照であり、割り当てが増えない |
| `GetActions()` → `SA.Actions` | 使わず、`ISpecialActionRepository.ActionList`（実体）を参照する | `Actions` はコピーを返す実装のため。`ActionList` は元の `Global.GetActions()` と同じ実体参照 |
| `GetProfileAction(device, name)` → `PA.GetProfileAction(...)` | そのまま置換する。**ただし2026-09-23の実機確認で判明した理由により、`ProfileActionProvider.GetProfileAction`側の`[DI]` Traceログは削除した（§3.1追記参照）** | 割り当てコストの面では問題なし（`AppLogger.IsTraceEnabled`ガードによりTrace無効時はbool判定1回のみ）。ただし、このメソッドは`MapCustom`のStage3・`MapCustomAction`のアクション走査から**毎レポート・プロファイル内アクション数分（実測最大約40回）**呼ばれるホットパスであり、Trace有効時にログが大量に流れ続けて他のログが読めなくなる実害が実機確認で判明したため、ログ自体を削除した |

#### 2.4.2 選択肢の提示が必要な項目 A: `reverseX360ButtonMapping`

`IProfileSettingsService.GetReverseX360ButtonMapping()` は呼び出しごとに配列を `Clone()` する（`ProfileSettingsService.cs` 611行）。`Global.reverseX360ButtonMapping` は素の `public static` 配列（クローンなし）であり、`ProcessControlSettingAction`（ホットパス）内での置換は、置換前後で1回のヒープ割り当てが新たに発生する差分になる。なお `LSModInfo` 等、同じ `IProfileSettingsService` の大多数の配列プロパティはクローンなしの直接参照であり、このメンバのみが例外的にクローンしている。

| 選択肢 | 内容 | メリット | デメリット |
|---|---|---|---|
| **R1（推奨）** | `IProfileSettingsService` にクローンしない直接参照プロパティ（例: `ReverseX360ButtonMapping` の配列そのものを返す）を追加し、既存の `GetReverseX360ButtonMapping()`（クローン版、UI 等の防御が必要な呼び出し元向け）とは別に用意する | 割り当てゼロを維持でき、`LSModInfo` 等の既存パターンと一貫する | 呼び出し元がこの配列を変更しないという前提に依存する（従来の `Global.reverseX360ButtonMapping` も同じ前提だったため、実質的にリスク増加はない） |
| R2 | クローン版をそのまま使い、1回のヒープ割り当てを許容する | 変更が最小 | Phase6-Status.md §9完了判定基準「ホットパスで新たなヒープ割り当てが発生しないこと」に抵触する。ベンチマーク比較が必須になる（`DI-App-Wide-Migration-Plan.md` §8 リスク表） |
| R3 | `Mapping` 側で1回だけ取得しプロファイル切替時のみ再取得するキャッシュを持つ | 割り当てを抑えつつ既存 API に触れない | キャッシュ無効化のタイミング管理が増え、Step3 の対象範囲（呼び出し元置換）を超える設計変更になる |

Step3 の対象バッチ（§3 Step3-6）着手前に確認する。

#### 2.4.3 選択肢の提示が必要な項目 B: `Global.ApplyProfile`（5303〜5333行のフォールバックブロック）

このフォールバックは、`IProfileApplicationService` が DI 未登録の場合にのみ通る経路であり、外側で `HaltReportingRunAction` を呼び出したうえで `Global.ApplyProfile` を実行している。`IProfileApplicationService.ApplyProfile` に置き換える場合、同サービスの既知の不具合（K1: `deviceIndex >= 4` のスロットを拒否する、戻り値を捨てて成功扱いにする、Halt を自身で行わない）の影響を受ける。K1 は Step2 完了報告の持ち越し事項として既に記録されている（`Phase6-Status.md` §6.5）。

| 選択肢 | 内容 | メリット | デメリット |
|---|---|---|---|
| **K-1（推奨・既定）** | Step3 では `Global.ApplyProfile` フォールバックをそのまま温存し、TODO コメント（K1 是正後に `AP.ApplyProfile` へ置換予定、と明記）を付与するに留める | K1 の是正という Step3 の範囲外の作業を持ち込まない。持ち越し事項の管理と矛盾しない | `using static` 依存が1件残る（他の149件が解消されれば影響は限定的） |
| K-2 | Step3 の中で K1 を先に是正してから `AP.ApplyProfile` に置換する | `using static` 依存を完全にゼロにできる | `ProfileApplicationService`（`Mapping.cs` 以外のファイル）に手を入れることになり、Step3 の対象ファイルの原則（`Mapping.cs` 局所の引数渡し）を超える |

Step3 の最終バッチ（§3 Step3-7）着手前に確認する。既定は K-1 とする。

---

## 3. バッチ構成（Step3-1 〜 Step3-7）

各バッチは 1 PR とし、実装後に `dotnet build` / `dotnet test` のグリーンを確認してからコミットする。バッチの対象・行番号の正本は `Phase6-Step3-Reality-Check-Ledger.md` §4 とする。

| バッチ | 内容 | 対象（Ledger 上の名称） | 件数 | 備考 |
|---|---|---|---:|---|
| **Step3-1** | 契約整備（新設・拡張のみ、`Mapping.cs` 不変更） | `IDisplayCoordinateService` 新設＋登録、`IProfileXmlStore.SaveControllerConfigsForDevice` 追加、`IProfileActionProvider.GetProfileActionIndexOf` 追加、`IProfileSettingsService.GetControlSettingsGroup` 追加、各モックテスト追加 | - | §2.3 の D1/D2 をここで確定 |
| **Step3-2** | 非ホット・条件付き経路の引数渡し化 | `ProfilePath`（5257）、`SaveControllerConfigs`/`LoadControllerConfigs`/`OutContType`（8249/8423/8546、`Scale360degreeGyroAxis`＝SA操舵輪エミュレーション有効時のみ到達）、`getProfileActions`/`GetProfileAction`/`GetActions` 系（3274/3285/4887/4938/4946/4978/5320/5324、§2.4.1 の方針で解消） | 12 | **実施済み（2026-09-23、ビルド未検証）。実施記録は §3.1 参照** |
| **Step3-3** | 画面座標変換の引数渡し化 | `absUseAllMonitors`/`TranslateCoorToAbsDisplay`/`ButtonAbsMouseInfos`（3640, 3658, 3668, 3674, 3676、絶対マウス出力使用時のみ到達） | 5 | `IDisplayCoordinateService` を実際に消費する最初のバッチ |
| **Step3-4** | `SetCurveAndDeadzone` のスティック・トリガー系（前半） | ローテーション・アンチスナップバック・デッドゾーン・感度・ベジェ曲線（1595〜2227 台の約20件） | 約20 | 純粋な配列引き当てが中心。`IProfileSettingsService` 1個の引数追加で足りる |
| **Step3-5** | `SetCurveAndDeadzone` の残り＋`Commit`/`ApplyStickCalibration` | スクエアスティック・ジャイロ・カーブモード・ドリフト補正・`outputKBMMapping`（`Commit` 24件） | 約44 | 本 Step で最大のバッチ。`Commit` は毎レポート出力確定処理のため実機回帰確認を重視する |
| **Step3-6** | 残りのホットパス（ボタン・マウス・アクション設定・6軸） | `reverseX360ButtonMapping`（§2.4.2 の選択後）、`GetControlSettingsGroup`、`ButtonMouseInfos`、`SXSens`/`SZSens` 等、`PlayMacroCodeValue` 内の `outputKBMMapping` | 約35 | 着手前に §2.4.2 を確認 |
| **Step3-7** | 温存・要判断の最終処理 | `Global.ApplyProfile` フォールバック（§2.4.3 の選択、既定 K-1）、`ProfileSettingsServiceInstance`/`outputKBMHandler` の `??` フォールバック（現状維持、TODO コメント確認のみ）、`using static DS4Windows.Global;` の削除可否判定 | 3 | 全バッチ完了後、`Global.` 参照が0件（またはコメント記載どおりの温存のみ）になっていることを確認する |

バッチの粒度・順序は、Step3-1 完了後の実地確認（各バッチ着手前に該当範囲を再 grep する。`DI-App-Wide-Migration-Plan.md` §6.11）で必要に応じて見直す。

### 3.1 Step3-2 実施記録（2026-09-23）

**実地再確認の結果**: 着手前の再 grep で12件（`GetControlSettingsGroup`1、`getProfileActions`系3、`GetProfileAction`系3、`GetProfileActionIndexOf`系2、`GetActions()`系2、`ProfilePath`1）を確認し、Ledger記載の行番号からのズレ（Step3-1のコード追加により約+25行）を補正した。

**§2.1で想定していたコンテキスト構造体は不要と判明**: `MapCustom`／`MapCustomAction`／`Scale360degreeGyroAxis`／`SAWheelEmulationCalibration` の4メソッドは、いずれも**既に `ControlService ctrl` を引数として受け取っていた**（Step2以前からの既存シグネチャ）。そのため、新しいサービスを渡すための追加パラメータやコンテキスト構造体は不要で、`ControlService` に読み取り専用の `internal` プロパティ（`ProfileActionProvider`／`ProfileXmlStore`／`DisplayCoordinateService`／`ProfileRepository`／`SpecialActionRepository`）を追加し、`ctrl.ProfileActionProvider.GetProfileAction(...)` のように既存の `ctrl` 経由でアクセスする形で解消した。これはS3（引数渡し）の趣旨に合致しつつ、シグネチャ変更を最小化できる実装方法として、Step3-3以降でも同様に「対象メソッドが既に`ctrl`を持っているか」を最初に確認する運用とする。

**新規DI依存の追加**: `ControlService` は `ISpecialActionRepository`（`GetActions()`／`GetActions().Count` の置換に必要）を保持していなかったため、新規のコンストラクタ必須引数として追加した（`ServiceRegistration.cs` も追随）。

**シグネチャ変更が必要だった唯一の例**: `LogActionDoneCountOnTrigger`（`Mapping.cs` の診断ログ専用 private ヘルパー、`ctrl` 引数を持たない）には `ControlService ctrl` を新規パラメータとして追加し、11箇所の呼び出し元すべてを更新した。

**テスト**: `ControlServiceStep3Step2DiWiringTests.cs`（新規DI配線・5プロパティの実体一致を検証）、`MappingLogActionDoneCountOnTriggerTests.cs`（新規）を追加。`Mapping.cs` の巨大メソッド自体（`MapCustom`/`MapCustomAction`等）を直接駆動するテストは、既存の `MappingSpecialActionSuppressionTests.cs` 冒頭コメントが指摘する通り本プロジェクトでも困難と判断し、今回は追加していない。挙動保持の確認はビルド・実機確認に委ねる。

**2026-09-23 実機確認で判明したログ問題の修正**: 実機ログで、コントローラー接続直後から `[DI] ProfileActionProvider.GetProfileAction` が全プロファイルアクション分（約40件）× 継続的に流れ続ける状態を確認。原因は、`MapCustom` の Stage3（ボタン型SA判定）と `MapCustomAction` のアクション走査が、**元々（Step3-2着手前から）毎入力レポートごとにプロファイル内の全アクションをループしてGetProfileActionを呼んでいた**ため（この頻度自体はStep3-2が作り込んだものではない）。従来は `Global.GetProfileAction` にログが無かったためこの高頻度呼び出しが可視化されていなかったが、Step3-2で `ProfileActionProvider.GetProfileAction`（`[DI]` Traceログ付き）に置き換えたことで、Trace有効時に大量のログが流れ続けるようになった。`ProfileActionProvider.GetProfileAction` の `[DI]` Traceログを削除して対応（§2.4.1の記述も訂正済み）。なお、同じ実機ログ中に発生した `0002_DS4WDisconnect Controller` によるコントローラー切断は、ユーザーが意図して実行したものであり本件とは無関係（正常動作）。

---

## 4. テスト・回帰検証計画

### 4.1 自動単体テスト

- **契約追加分**（Step3-1）: `IDisplayCoordinateService`, `IProfileXmlStore.SaveControllerConfigsForDevice`, `IProfileActionProvider.GetProfileActionIndexOf`, `IProfileSettingsService.GetControlSettingsGroup` のそれぞれにモックベースの単体テストを追加する（1ファイル1型を厳守）。
- **`MappingArgumentPassThroughTests.cs`**: Step3-2 以降、引数渡しに変更した `Mapping` の対象メソッドにモックサービスを渡し、期待したメンバが呼び出されることを検証する。
- **`MappingHotPathAllocationTests.cs`**: `SetCurveAndDeadzone` / `Commit` / `ApplyStickCalibration` について、Step3-4〜3-6 の各バッチ前後でヒープ割り当てが増えていないことを検証する（Step2 の `ControlServiceHotPathAllocationTests` と同種の手法）。
- 既存の `Mapping.` 参照テスト4本（`ControlTabAndSpecialActionKeyTests`, `DefaultActionManagerTests`, `MappingSpecialActionSuppressionTests`, `MappingControllerLifecycleTests`）は、ホットパスの回帰を担保しないため、Step3-4〜3-6 着手前に特性化テストを別途追加する。

### 4.2 回帰テスト基準

- `dotnet build -c Release` で警告・エラー0件。
- `DS4WindowsTests` / `StandaloneTests` 全件成功を各バッチで維持。
- Step3-3（絶対マウス出力）、Step3-5（`Commit` の出力確定）は実機回帰確認の対象とする（`Phase6-Step11-Plan.md` §3.3 の先送り台帳に倣う）。

---

## 5. ロールバック方針

- 各バッチ（PR）単位で `git revert` する。`Global.*` の実体（シム）はコードベースに残るため、引数渡し前の直接呼び出しに戻せば即座に復旧できる。
- `using static DS4Windows.Global;` の削除は Step3-7（全バッチ完了後）にのみ行う。途中のバッチでは明示修飾（`Global.xxx`）と非修飾（`using static` 経由）が混在してよい。

---

## 6. 完了判定チェックリスト

- [ ] §1 の実参照150件が、§3 の各バッチにより解消されていること（KEEP 3件は TODO コメント付き温存、JUDGE 2件は §2.4 の選択後に解消または温存判断済みであること）。
- [ ] 新設・拡張した契約（`IDisplayCoordinateService` 等4件）にモックベースの単体テストがあること。
- [ ] `SetCurveAndDeadzone` 等のホットパスで新たなヒープ割り当てが発生していないこと（§2.4.2 の選択結果を反映）。
- [ ] `ControlService`（またはその他の呼び出し元）から `Mapping` への引数渡しが Pure DI の原則に準拠していること（新たな Service Locator を `Mapping` に持ち込んでいないこと）。
- [ ] モデル図 03・04 の `IDisplayCoordinateService` / `IProfileXmlStore` の記載が、実装後のコードと一致していること。
- [ ] `dotnet build -c Release` でエラー・警告0件。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功。
- [ ] `using static DS4Windows.Global;`（31行目）の削除可否が判定され、記録されていること（削除する場合は本 Step 内で実施、見送る場合は理由と残存参照を記録）。