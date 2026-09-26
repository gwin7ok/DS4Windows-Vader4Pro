# Phase6-Step3 計画書: `Mapping.cs` の段階的引数渡しによる `Global` 直接参照の解消

作成日: 2026-09-09  
改訂日: 2026-09-21（実地突き合わせに基づく全面改訂。案A［局所10件＋Phase7一括引き継ぎ］を撤回し、**全150件を段階的に解消する方針（論点1〜4の決定）へ変更**）  
<<<<<<< HEAD
状態: 計画確定・**Step3-1・Step3-2 完了（2026-09-22〜23、ともにビルド・テスト・実機確認済み）**。**Step3-3 は対象消滅（2026-09-23、Abs Mouse機能削除に伴い対象コード自体を撤去。詳細は`Phase6-Status.md` §6.8）**。**Step3-4 は完了確定（2026-09-24、`SetCurveAndDeadzone` のスティック・トリガー系27件を解消。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.3）**。**Step3-5 は完了確定（2026-09-24、ジャイロ系13件・`ApplyStickCalibration` 8件・`Commit` 24件の計45件を解消。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.4）**。**Step3-6 は 3-6a／3-6b／3-6c の3バッチに分割（2026-09-24決定。§3.5）。Step3-6a は完了確定（2026-09-24、グループ A の24件＋契約追加 R1。ビルド・テストビルド・テスト実行成功、実機確認済み［ステアリングホイールエミュレーションのみ先送り］。§3.5）。Step3-6b は完了確定（2026-09-24、グループ B の12件。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.6）。Step3-6c も完了確定（2026-09-24、グループ C の19件。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.7）。これで Step3-6（3-6a／3-6b／3-6c）はすべて完了した**。次は Step3-7（別セッションで再開。着手前の確認事項は §3.8）。残実参照数は、Step3-5 完了後の再集計（2026-09-24）で **58件**と判明した（従来の記載「118」「73」は、Step3-2 で解消した12件の引き算漏れによる過大）。Step3-6a 完了後は34件、Step3-6b 完了後は22件、Step3-6c 完了後は3件（Step3-7 の温存3のみ）。  
=======
状態: 計画確定・**Step3-1・Step3-2 完了（2026-09-22〜23、ともにビルド・テスト・実機確認済み）**。**Step3-3 は対象消滅（2026-09-23、Abs Mouse機能削除に伴い対象コード自体を撤去。詳細は`Phase6-Status.md` §6.8）**。**Step3-4 は完了確定（2026-09-24、`SetCurveAndDeadzone` のスティック・トリガー系27件を解消。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.3）**。**Step3-5 は完了確定（2026-09-24、ジャイロ系13件・`ApplyStickCalibration` 8件・`Commit` 24件の計45件を解消。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.4）**。**Step3-6 は 3-6a／3-6b／3-6c の3バッチに分割（2026-09-24決定。§3.5）。Step3-6a は完了確定（2026-09-24、グループ A の24件＋契約追加 R1。ビルド・テストビルド・テスト実行成功、実機確認済み［ステアリングホイールエミュレーションのみ先送り］。§3.5）。Step3-6b は完了確定（2026-09-24、グループ B の12件。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.6）。Step3-6c は完了確定（2026-09-24、グループ C の19件。ビルド・テストビルド・テスト実行成功、実機確認済み。§3.7）**。**Step3-7 は実装済み（2026-09-24、温存3件へのTODOコメント付与、§2.4.3 の選択は既定K-1を採用、`using static DS4Windows.Global;` の削除可否判定は見送りで確定。ビルド・テスト確認待ち。§3.8）**。残実参照数は、Step3-6c 完了後の集計で **3件（Step3-7 の温存3件のみ）**、Step3-7 完了後は全件が解消・温存判断済み（温存3件はTODO付きで意図的に残置）。  
>>>>>>> 2ac27857341a8f984f8d9972a8a7394f1d2ac0ea
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
| 実参照（Step3 で解消する対象） | **150**（→145、2026-09-23 Abs Mouse機能削除によりStep3-3分5件が消滅。詳細は §3.2） |
| 除外（純粋計算 `Clamp`/`getTransitionedColor`、真の定数） | 51 |
| 合計走査対象 | 201 |

実参照150件（策定時点。2026-09-23以降は145件）の判定内訳:

| 判定 | 件数 | Step3 での扱い |
|---|---:|---|
| 置換可（OK） | 135（→130） | §3 のバッチで機械的に置換 |
| 契約追加要（ADD） | 8 | Step3-1 で契約を追加してから置換（対象メンバ5種: `absUseAllMonitors`, `TranslateCoorToAbsDisplay`, `SaveControllerConfigs`, `GetProfileActionIndexOf`, `GetControlSettingsGroup`。うち`absUseAllMonitors`/`TranslateCoorToAbsDisplay`の契約＝`IDisplayCoordinateService`自体もAbs Mouse機能削除で撤去済み） |
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

**決定（2026-09-24）: R1 を採用**。`IProfileSettingsService` にコピーしない読み取り専用プロパティ `ReverseX360ButtonMapping`（`Global.reverseX360ButtonMapping` の参照をそのまま返す）を追加し、`GetReverseX360ButtonMapping()`（コピー版）はそのまま残す。モデル図 03 は `IProfileSettingsService` の個別メンバーを記載していないため変更不要。実装は Step3-6a（§3.5）。

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
| **Step3-2** | 非ホット・条件付き経路の引数渡し化 | `ProfilePath`（5257）、`SaveControllerConfigs`/`LoadControllerConfigs`/`OutContType`（8249/8423/8546、`Scale360degreeGyroAxis`＝SA操舵輪エミュレーション有効時のみ到達）、`getProfileActions`/`GetProfileAction`/`GetActions` 系（3274/3285/4887/4938/4946/4978/5320/5324、§2.4.1 の方針で解消） | 12 | **完了（2026-09-23、ビルド・テスト・実機確認済み）。実施記録は §3.1 参照** |
| **Step3-3** | ~~画面座標変換の引数渡し化~~ | ~~`absUseAllMonitors`/`TranslateCoorToAbsDisplay`/`ButtonAbsMouseInfos`（3640, 3658, 3668, 3674, 3676、絶対マウス出力使用時のみ到達）~~ | ~~5~~ | **対象消滅（2026-09-23）。Abs Mouse機能削除によりMapCustomの該当ブロック自体を撤去。詳細は §3.2、`Phase6-Status.md` §6.8参照** |
| **Step3-4** | `SetCurveAndDeadzone` のスティック・トリガー系（前半） | LS/RS/L2/R2 のローテーション・アンチスナップバック・デッドゾーン・感度・スクエアスティック・カーブモード・ベジェ曲線 | 27（計画時の概算「約20」を実地確認で確定） | **完了確定（2026-09-24、ビルド・テスト・実機確認済み）。実施記録は §3.3 参照**。純粋な配列引き当てが中心。`IProfileSettingsService` 1個の引数追加で足りる |
| **Step3-5** | `SetCurveAndDeadzone` の残り＋`Commit`/`ApplyStickCalibration` | ジャイロ系（SX/SZ のデッドゾーン・感度・カーブモード・ベジェ曲線、`IsUsingSAForControls`）13件・ドリフト補正（`ApplyStickCalibration`）8件・`outputKBMMapping`（`Commit` 24件）（Step3-4 の実地確認でスクエアスティック・カーブモードは Step3-4 側へ移動。合計は計画の約44件と整合） | 約45 | 本 Step で最大のバッチ。`Commit` は毎レポート出力確定処理のため実機回帰確認を重視する。**完了確定（2026-09-24、ビルド・テスト・実機確認済み）。実施記録・`Commit` の扱いの決定（C1）は §3.4 参照** |
| **Step3-6a** | 残りのホットパス: `ctrl` から読める経路（グループ A） | `MapCustom`／`ProcessControlSettingAction`／`MapCustomAction`／`getMouseMapping`／`Scale360degreeGyroAxis`（いずれも `ControlService ctrl` を持つ）の `getProfileActionCount`／`GetSASteeringWheelEmulationAxis`／`ButtonMouseInfos`／`outputKBMMapping`／`GetDS4CSetting`／`getLSDeadzone`／`getRSDeadzone`／`SAWheelFuzzValues`／`getSXDeadzone`／`WheelSmoothInfo`／`getSXAntiDeadzone`、`reverseX360ButtonMapping`（決定 R1）、および `ctrl` を持たない `ReleaseActionKeys`／`IfAxisIsNotModified`（`settings` 引数を追加） | 24 | **完了確定（2026-09-24、ビルド・テスト・実機確認済み。ステアリングホイールエミュレーションのみ Step11 へ先送り）。実施記録は §3.5 参照** |
| **Step3-6b** | 判定補助メソッド群（グループ B） | `GetByteMapping`／`GetBoolMappingExternal`／`GetBoolMapping`／`getBoolSpecialActionMapping`／`GetBoolActionMapping`／`GetXYAxisMapping` の `IsUsingSAForControls`（6）・`SXSens`／`SZSens`（4）、`GetMouseWheelMapping` の `Global.outputKBMMapping`（2）。**方針 P2（`Mapping` の静的 `profileSettings` を利用。決定 2026-09-24、§3.4.1）** | 12 | 呼び出し元が多く（約50箇所）`ctrl` を持たないため引数渡しは行わない。TODO コメント（Phase7 の instance 化で解消）を付ける。**完了確定（2026-09-24、ビルド・テスト・実機確認済み）。実施記録は §3.6 参照** |
| **Step3-6c** | 非同期マクロ経路（グループ C） | `PlayMacroCodeValue`（14）・`AltTabSwapping`（3）・`AltTabSwappingRelease`（2）の `outputKBMMapping`。**方針 P2（決定 2026-09-24、§3.4.1）** | 19 | モデル図 04 の変更は不要。K3（マクロ二重ガード）は Phase7 で再設計するため触れない。**完了確定（2026-09-24、ビルド・テスト・実機確認済み。Alt+Tab マクロの観察事項あり）。実施記録は §3.7 参照** |
| **Step3-7** | 温存・要判断の最終処理 | `Global.ApplyProfile` フォールバック（§2.4.3 の選択、既定 K-1）、`ProfileSettingsServiceInstance`/`outputKBMHandler` の `??` フォールバック（現状維持、TODO コメント確認のみ）、`using static DS4Windows.Global;` の削除可否判定 | 3 | 全バッチ完了後、`Global.` 参照が0件（またはコメント記載どおりの温存のみ）になっていることを確認する。**次セッションで着手。事前調査の結果と確認事項は §3.8 参照** |

バッチの粒度・順序は、Step3-1 完了後の実地確認（各バッチ着手前に該当範囲を再 grep する。`DI-App-Wide-Migration-Plan.md` §6.11）で必要に応じて見直す。

### 3.1 Step3-2 実施記録（2026-09-23）

**実地再確認の結果**: 着手前の再 grep で12件（`GetControlSettingsGroup`1、`getProfileActions`系3、`GetProfileAction`系3、`GetProfileActionIndexOf`系2、`GetActions()`系2、`ProfilePath`1）を確認し、Ledger記載の行番号からのズレ（Step3-1のコード追加により約+25行）を補正した。

**§2.1で想定していたコンテキスト構造体は不要と判明**: `MapCustom`／`MapCustomAction`／`Scale360degreeGyroAxis`／`SAWheelEmulationCalibration` の4メソッドは、いずれも**既に `ControlService ctrl` を引数として受け取っていた**（Step2以前からの既存シグネチャ）。そのため、新しいサービスを渡すための追加パラメータやコンテキスト構造体は不要で、`ControlService` に読み取り専用の `internal` プロパティ（`ProfileActionProvider`／`ProfileXmlStore`／`DisplayCoordinateService`／`ProfileRepository`／`SpecialActionRepository`）を追加し、`ctrl.ProfileActionProvider.GetProfileAction(...)` のように既存の `ctrl` 経由でアクセスする形で解消した。これはS3（引数渡し）の趣旨に合致しつつ、シグネチャ変更を最小化できる実装方法として、Step3-3以降でも同様に「対象メソッドが既に`ctrl`を持っているか」を最初に確認する運用とする。

**新規DI依存の追加**: `ControlService` は `ISpecialActionRepository`（`GetActions()`／`GetActions().Count` の置換に必要）を保持していなかったため、新規のコンストラクタ必須引数として追加した（`ServiceRegistration.cs` も追随）。

**シグネチャ変更が必要だった唯一の例**: `LogActionDoneCountOnTrigger`（`Mapping.cs` の診断ログ専用 private ヘルパー、`ctrl` 引数を持たない）には `ControlService ctrl` を新規パラメータとして追加し、11箇所の呼び出し元すべてを更新した。

**テスト**: `ControlServiceStep3Step2DiWiringTests.cs`（新規DI配線・5プロパティの実体一致を検証）、`MappingLogActionDoneCountOnTriggerTests.cs`（新規）を追加。`Mapping.cs` の巨大メソッド自体（`MapCustom`/`MapCustomAction`等）を直接駆動するテストは、既存の `MappingSpecialActionSuppressionTests.cs` 冒頭コメントが指摘する通り本プロジェクトでも困難と判断し、今回は追加していない。挙動保持の確認はビルド・実機確認に委ねる。

**2026-09-23 実機確認で判明したログ問題の修正**: 実機ログで、コントローラー接続直後から `[DI] ProfileActionProvider.GetProfileAction` が全プロファイルアクション分（約40件）× 継続的に流れ続ける状態を確認。原因は、`MapCustom` の Stage3（ボタン型SA判定）と `MapCustomAction` のアクション走査が、**元々（Step3-2着手前から）毎入力レポートごとにプロファイル内の全アクションをループしてGetProfileActionを呼んでいた**ため（この頻度自体はStep3-2が作り込んだものではない）。従来は `Global.GetProfileAction` にログが無かったためこの高頻度呼び出しが可視化されていなかったが、Step3-2で `ProfileActionProvider.GetProfileAction`（`[DI]` Traceログ付き）に置き換えたことで、Trace有効時に大量のログが流れ続けるようになった。`ProfileActionProvider.GetProfileAction` の `[DI]` Traceログを削除して対応（§2.4.1の記述も訂正済み）。なお、同じ実機ログ中に発生した `0002_DS4WDisconnect Controller` によるコントローラー切断は、ユーザーが意図して実行したものであり本件とは無関係（正常動作）。

### 3.2 Step3-3 実施記録（2026-09-23実装 → 同日、機能削除に伴い対象消滅）

**対象**: `MapCustom`（`Mapping.cs`）内の絶対マウス出力（`absMouseOut`）座標変換ロジック、5箇所（`absUseAllMonitors` 参照2、`TranslateCoorToAbsDisplay` 呼出2、`ButtonAbsMouseInfos` 参照1）。`MapCustom` は Step3-2 と同様に既に `ControlService ctrl` を引数として受け取っているため、コンテキスト構造体や新規パラメータは不要だった。

**実装内容（2026-09-23、ビルド・テスト成功・コミット済み）**:
- `absUseAllMonitors`（`using static DS4Windows.Global;` 経由のbare参照）と `Global.absUseAllMonitors`（明示修飾）を、いずれも `ctrl.DisplayCoordinateService.UseAllMonitors`（Step3-1で新設済みの `IDisplayCoordinateService`）に置換した。
- `Global.TranslateCoorToAbsDisplay(...)` の呼び出し2箇所を `ctrl.DisplayCoordinateService.TranslateCoorToAbsDisplay(...)` に置換した。
- `ButtonAbsMouseInfos[device]`（bare参照、`Global.ButtonAbsMouseInfos` 経由）を `ctrl.ProfileSettingsService.ButtonAbsMouseInfos[device]` に置換した。`IProfileSettingsService.ButtonAbsMouseInfos` は既存の契約メンバー（追加不要）だが、`ControlService` にはこれを公開する `internal` アクセサが無かったため、既存の `_profileSettings` フィールドをそのまま返す読み取り専用プロパティ `ProfileSettingsService` を新設した（Step3-2で追加した5プロパティと同じパターン。新規サービス・新規フィールドは追加していない）。
- `GetAbsMouseMapping`（旧Ledger行6752相当、現在行6814）の `ButtonAbsMouseInfos[device]` は Step3-3の対象外（`MapCustom` 以外のメソッドのため）。Step3-6以降で扱う予定だった。

**テスト**: `ControlServiceStep3Step3DiWiringTests.cs`（新規。新設した `ProfileSettingsService` プロパティが Composition Root の実体をそのまま返すことを検証）。

**その後の展開（2026-09-23、同日中）**: ビルド・テスト・実機確認・コミットが完了した直後、ユーザーより「Abs Mouse機能自体の使用頻度が極めて低いため削除したい」との提案があり、調査・承認を経て、ボタン割当のAbs Mouse機能・タッチパッドのAbsoluteMouseモード・共有基盤`IDisplayCoordinateService`が丸ごと削除された（`copilot-instructions.md` §2.2 例外規定に基づく機能廃止、詳細は`Phase6-Status.md` §6.8）。これに伴い、上記5箇所を含む`MapCustom`の該当ブロックと`GetAbsMouseMapping`メソッド自体が削除され、**Step3-3は対象が消滅**した。新設した`ProfileSettingsService`プロパティのみ、Step3-6（`GetControlSettingsGroup`等）での再利用を見込んで存置している。`ControlServiceStep3Step3DiWiringTests.cs`はこのプロパティのDI配線検証として引き続き有効。

**ビルド確認（確定・2026-09-23）**: ビルド・テストビルド・テスト実行とも成功。実機確認も完了（既存プロファイルのフォールバック動作、タッチパッド他モードの正常動作、UI上のAbs Mouse関連項目の非表示を確認）。コミットしてリモートリポジトリに反映済み。詳細は`Phase6-Status.md` §6.8参照。

### 3.3 Step3-4 実施記録（2026-09-23）

**実地再確認の結果**: 着手前の再 grep で、`SetCurveAndDeadzone` 内の対象は Ledger と同じ40件（`Mapping.cs` 1524〜2715 行。Step3-3 の Abs Mouse 削除で Ledger 記載の行番号から約70行前へずれている）。うち LS/RS/L2/R2・スクエアスティックの系統（カーブモード・ベジェ曲線を含む）が27件、ジャイロ系（SX/SZ）が13件だった。§3 の表の「約20件」は概算で、ローテーション〜ベジェ曲線までを一体で扱うと27件になる。ジャイロ系13件は Step3-5 へ回す（Step3-5 は 13＋`ApplyStickCalibration` 8＋`Commit` 24 ＝ 45件で、計画の約44件と整合する）。**Step3 の残実参照数は 145 → 118**。

**方針**: `SetCurveAndDeadzone` は `ControlService ctrl` を引数に持たず、必要なサービスは `IProfileSettingsService` の1個だけである。§2.1 に従い、このインターフェースを引数（`settings`）として追加した（コンテキスト構造体・`ctrl` 渡しは不要）。引数名を `profileSettings` にしなかったのは、`Mapping` の静的フィールド `profileSettings` を隠さず、どちらを参照しているか読み取れるようにするため。

**変更内容**:

| 対象 | 変更 |
|---|---|
| `Mapping.SetCurveAndDeadzone` | シグネチャに `DS4Windows.DI.IProfileSettingsService settings` を追加（唯一のシグネチャ変更）。27件を `settings.XXX` に置換（下表） |
| `ControlService.cs`（呼び出し元・入力ループ） | `Mapping.SetCurveAndDeadzone(ind, cState, TempState[ind], _profileSettings)`。既存のコンストラクタ注入済みフィールドを渡すのみで、コンストラクタ引数は変更なし |
| `ControllerReadingsControl.xaml.cs`（呼び出し元・UI プレビュー） | `controlService.ProfileSettingsService` を渡す（Step3-3 で新設し存置していた `internal` プロパティを再利用。新規プロパティなし） |
| 契約（`DS4Windows/DI/`）・`ServiceRegistration.cs` | 変更なし（対応メンバーはすべて既存） |
| `ApplyStickCalibration`、ジャイロ系（SX/SZ） | 変更なし（Step3-5 の対象。`using static DS4Windows.Global;` 経由のまま） |

置換の対応（すべて `[device]` は元のまま）:

| 置換前（Global） | 置換後 | 件数 |
|---|---|---:|
| `getLSRotation`／`getRSRotation` | `settings.LSRotation`／`settings.RSRotation` | 2 |
| `GetLSAntiSnapbackInfo`／`GetRSAntiSnapbackInfo` | `settings.LSAntiSnapbackInfo`／`settings.RSAntiSnapbackInfo` | 2 |
| `GetLSDeadInfo`／`GetRSDeadInfo` | `settings.LSModInfo`／`settings.RSModInfo` | 2 |
| `GetL2ModInfo`／`GetR2ModInfo` | `settings.L2ModInfo`／`settings.R2ModInfo` | 2 |
| `getLSSens`／`getRSSens`／`getL2Sens`／`getR2Sens` | `settings.LSSens`／`RSSens`／`L2Sens`／`R2Sens` | 4 |
| `GetSquareStickInfo` | `settings.SquStickInfo` | 1 |
| `getLsOutCurveMode`／`getRsOutCurveMode`／`getL2OutCurveMode`／`getR2OutCurveMode` | `settings.GetLsOutCurveMode(device)` 等 | 4 |
| `lsOutBezierCurveObj`／`rsOutBezierCurveObj`（各4）、`l2OutBezierCurveObj`／`r2OutBezierCurveObj`（各1） | `settings.LsOutBezierCurveObj` 等 | 10 |
| 合計 | | **27** |

**挙動の同一性（実装前に確認）**: 置換前の `Global.getXxx(index)` は `m_Config.xxx[index]`（`Global.store` の配列）を返し、置換後の `IProfileSettingsService.Xxx`（`ProfileSettingsService`）は `SafeConfig?.xxx`（`_config ?? Global.store`、Composition Root の実体は `Global.store`）を返す。同じ `BackingStore` の同じ配列インスタンスを指すため値・参照とも一致する。カーブモード4種も、`Global.getXxxOutCurveMode` は `ProfileSettingsServiceInstance.GetXxxOutCurveMode` へ委譲済みで、サービス側は `SafeConfig.getXxxOutCurveMode` を呼ぶ。同じ経路である。ベジェ曲線オブジェクト4種も、`Global` 側が `ProfileSettingsServiceInstance` の同名プロパティへ委譲済みである。いずれも配列の参照を返すだけで、割り当て（`Clone()`）・ログ・キャッシュはない（ホットパスの制約を満たす。§2.4.2 の `GetReverseX360ButtonMapping` のような例外メンバーは本バッチに含まれない）。

**テスト**（新規3ファイル。§4.1 で予定していた `MappingArgumentPassThroughTests.cs`／`MappingHotPathAllocationTests.cs` を作成し、Step3-5・3-6 でも同じファイルへ追加していく）:
- `MappingArgumentPassThroughTests.cs`: 互いに独立した `BackingStore` を持つ2つの `ProfileSettingsService` を用意し、片方だけ設定を変えて同じ入力を与え、出力が変わることを確認する（LS/RS 回転、LS デッドゾーン、LS/L2/R2 感度の6件）。メソッドが `Global` の設定を読んでいれば2つの出力が一致するため、引数の値が使われていることを検出できる。ゴールデン値は固定せず相対比較のみ。
- `MappingHotPathAllocationTests.cs`: `SetCurveAndDeadzone` を2万回呼び、ウォームアップ後の割り当てが0バイトであることを確認する（新設した引数の取得経路がヒープ割り当てを追加していないことの固定）。
- `MappingSetCurveAndDeadzoneGlobalReferenceGuardTests.cs`: `Mapping.cs` は Step3-7 まで `using static DS4Windows.Global;` を残すため、非修飾の Global メンバ呼び出しが再導入されてもコンパイルは通る。このため、`SetCurveAndDeadzone` 本体のソースを走査し、置換済み21種のメンバー名（`Global.` 修飾・非修飾の両方）が残っていないこと、および `IProfileSettingsService settings` の引数があることを確認する回帰ガード（`ControlServiceGlobalReferenceGuardTests` と同じ方式）。

**検証結果（確定・2026-09-24）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功、実機でも LS/RS/L2/R2 のデッドゾーン・感度・出力カーブ・スクエアスティック・ローテーション、および ControllerReadings 画面の入力値プレビューが変更前と同じ挙動であることを確認済み。

**（実装時点の記録）未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。実機確認は、コントローラー接続時に LS/RS/L2/R2 の入力（デッドゾーン・感度・出力カーブ・スクエアスティック・ローテーション）が変更前と同じ挙動であること、および ControllerReadings（入力値のプレビュー画面）の表示が引き続き正常であること（UI 側の呼び出し元の確認）。

### 3.4 Step3-5 実施記録（2026-09-24）

**実地再確認の結果（HEAD `b0e70b8`）**: 対象は Ledger／§3 の表と一致する45件（`SetCurveAndDeadzone` のジャイロ系 SX/SZ 13件、`ApplyStickCalibration` 8件、`Commit` の `outputKBMMapping` 24件）。**Step3 の残実参照数は 118 → 73**。

**着手前の確認と決定（2026-09-24）**: `ApplyStickCalibration` は呼び出し元が `SetCurveAndDeadzone` 内の1箇所のみ（外部呼び出しなし）、ジャイロ系は `settings` が既に引数にあるため、選択肢は不要（Step3-4 と同じ形）。`Commit(int device)` は `ctrl` を持たないため選択肢を提示し、**案 C1 を採用**した。

| 案 | 内容 | 判断 |
|---|---|---|
| **C1（採用）** | `Commit(int device, IProfileSettingsService settings)` に引数追加。呼び出し2箇所（`ControlService` の切断時 `Task.Run` 内と入力ループ末尾）は既存の `_profileSettings` を渡す。24箇所は `outputKBMMapping.` を `settings.OutputKBMMapping.` に置換（使用のたびに読む。ローカルへのキャッシュはしない） | S3（引数渡し）に合致、モデル図の変更なし、割り当てゼロ |
| C2 | `ControlService ctrl` を渡す | 不採用: `Commit` は `ctrl` の他の機能を使わず、過剰な結合になる |
| C3 | `Mapping` の static `profileSettings` を使う | 不採用: 型初期化時に1回だけ束縛されるため Ledger §3.1 の束縛ハザードが `OutputKBMMapping`（インスタンス自身が状態を持つ）で顕在化する。S3 の決定にも反する |

**変更内容**:

| 対象 | 変更 |
|---|---|
| `Mapping.Commit` | シグネチャに `DS4Windows.DI.IProfileSettingsService settings` を追加。`outputKBMMapping.` 24箇所を `settings.OutputKBMMapping.` に置換 |
| `Mapping.ApplyStickCalibration` | シグネチャに `settings` を追加（`public`。外部呼び出しなしを確認済み）。ドリフト補正 `RightStickDriftXAxis` 等 8箇所を `settings.XXX` に置換 |
| `Mapping.SetCurveAndDeadzone`（ジャイロ系） | 13箇所を置換: `IsUsingSAForControls(device)` → `settings.GyroOutputMode[device] == GyroOutMode.Controls`、`getSXDeadzone`/`getSZDeadzone`/`getSXMaxzone`/`getSZMaxzone`/`getSXAntiDeadzone`/`getSZAntiDeadzone`/`getSXSens`/`getSZSens` → `settings.SXDeadzone[device]` 等、`getSXOutCurveMode`/`getSZOutCurveMode` → `settings.GetSxOutCurveMode(device)`/`GetSzOutCurveMode(device)`、`sxOutBezierCurveObj`/`szOutBezierCurveObj` → `settings.SxOutBezierCurveObj`/`SzOutBezierCurveObj`。`ApplyStickCalibration` の呼び出しは `settings` を渡す形に更新 |
| `ControlService.cs`（呼び出し元） | `Mapping.Commit(ind, _profileSettings)`（2箇所）。コンストラクタ引数・プロパティの追加なし |
| 契約（`DS4Windows/DI/`）・`ServiceRegistration.cs` | 変更なし（対応メンバーはすべて既存） |

**挙動の同一性（実装前に確認）**:
- ジャイロ系・ドリフト補正: 置換前の `Global` 側の getter は `ProfileSettingsServiceInstance` または `m_Config`（`Global.store`）経由、置換後の `IProfileSettingsService` 側は `SafeConfig`（`_config ?? Global.store`）経由で、同じ `BackingStore` の同じ配列・同じ BezierCurve インスタンスを返す。`IsUsingSAForControls(i)`（`m_Config.gyroOutMode[i] == Controls`）と `settings.GyroOutputMode[i] == Controls` も同じ配列を見る。`Clone()`・ログ・キャッシュはない（ホットパスの制約を満たす）。
- `Commit`: 置換前の `Global.outputKBMMapping` は `Global.ProfileSettingsServiceInstance.OutputKBMMapping`（DI の Singleton）を返す。`ControlService` は `InitOutputKBMHandler` で同じ `_profileSettings` の `OutputKBMMapping` を初期化・`PopulateConstants`／`PopulateMappings` しており、`Commit` が読むインスタンスは初期化されたものと同一になる（`RefreshOutputKBMHandler` による null 化・再構築のタイミングも従来どおり。読み取りは使用時）。Ledger §3.1 の束縛ハザード（static 束縛のずれ）は、静的フィールドではなく呼び出し元の DI 済みインスタンスを渡すため発生しない。置換前は getter 2段（`Global.outputKBMMapping` → `ProfileSettingsServiceInstance`）、置換後は1回のプロパティ読み取りで、割り当ては増えない。
- `Commit` が使う出力先 `VirtualKBM`（`AppHost.GetService<IVirtualKBM>() ?? Global.outputKBMHandler`）は Step3 の対象外（Ledger §3.3）であり、変更していない。

**テスト**:
- `MappingArgumentPassThroughTests.cs`（更新）: ジャイロ系（出力モード・SX/SZ 感度・SX/SZ デッドゾーン・SX/SZ 出力カーブ、8件）と `ApplyStickCalibration`（4軸のドリフト補正の値、0〜255 への丸め、2件）を追加。Step3-4 と同じく、互いに独立した `BackingStore` を持つ2つの `ProfileSettingsService` の出力比較で、引数の値が使われていることを検証する。
- `MappingHotPathAllocationTests.cs`（更新）: ジャイロ→コントロール変換の分岐を通した `SetCurveAndDeadzone`、および `ApplyStickCalibration` が割り当て0バイトであることを固定。
- `MappingSetCurveAndDeadzoneGlobalReferenceGuardTests.cs`（更新）: 回帰ガードの対象メンバーにジャイロ系13種を追加。
- `MappingCommitAndCalibrationGlobalReferenceGuardTests.cs`（新規）: `ApplyStickCalibration` と `Commit` のソース走査ガード（`settings` 引数の存在、`Global` メンバー［ドリフト補正4種・`outputKBMMapping`］の再混入なし、`settings.OutputKBMMapping.` 経由の参照の存在）。
- **`Commit` の挙動テストは追加していない**: 出力先の `IVirtualKBM` を実 DI ホスト（`AppHost`）から解決しており、テストから実行すると実際にマウス・キー入力を送出してしまうため。ソース走査ガードと実機確認で担保する。

**検証結果（確定・2026-09-24）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。実機確認は下記3項目とも問題なし: (1) キーボード・マウス出力（ボタン割り当てのキー・マウスクリック、トグル、ホイール、ホイールの押しっぱなしリピート）、(2) ジャイロをスティック（Controls）出力に割り当てた場合の感度・デッドゾーン・出力カーブ、(3) スティックのドリフト補正（X/Y Axis Offset）。

**（実装時点の記録）未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。実機確認は次の項目: (1) キーボード・マウス出力（ボタン割り当てのキー・マウスクリック、トグル、ホイール、ホイールの押しっぱなしリピート）が変更前と同じ挙動であること、(2) ジャイロをスティック（Controls）出力に割り当てた場合の感度・デッドゾーン・出力カーブが変更前と同じであること、(3) スティックのドリフト補正が設定どおりに効くこと。

#### 3.4.1 Step3-6 の事前整理: `ctrl` を持たない連鎖（非同期マクロ経路・判定補助メソッド群）の扱い（2026-09-24 決定: P2）

Step3-6 の `PlayMacroCodeValue`（14件）・`AltTabSwapping`／`AltTabSwappingRelease`（計5件）は、マクロを別スレッドで再生する経路にあり、`ctrl` を持たない。実コード（HEAD `b0e70b8`）で連鎖の規模を確認した: `PlayMacro`（内部呼び出し7箇所、`PlayMacroDirect`、リフレクションで呼ぶ `MappingPlayMacroDispatchGuardTests`）→ `PlayMacroTask`（2箇所）→ `PlayMacroCodeValue`（6箇所）・`EndMacro` 3オーバーロード（約10箇所）・`AltTabSwapping`（1箇所）・`AltTabSwappingRelease`（3箇所）。合計は約9メソッド・約35呼び出し箇所。入口は2つあり、`MapCustomAction`／`ProcessControlSettingAction` は `ctrl` を持つが、`DefaultMacroPlayer`（DI 登録の `IMacroPlayer`）経由の `PlayMacroDirect`／`EndMacroDirect` は持たない。`DefaultMacroPlayer` は `MacroAction` のフォールバックとテスト2件が `new DefaultMacroPlayer()` で生成している。

| 案 | 内容 | 評価 |
|---|---|---|
| **M1（推奨）** | `IProfileSettingsService` を引数で末端まで通す。`ctrl` を持つ入口は `ctrl.ProfileSettingsService` を渡し、`DefaultMacroPlayer` には `IProfileSettingsService` を追加する（既存の `IVirtualKBM virtualKBM = null` と同じ任意引数方式で `new DefaultMacroPlayer()` を壊さない） | S3 に合致。束縛ハザードなし。**モデル図 04 の `IMacroPlayer` の依存列に `IProfileSettingsService` を追加する変更が必要（ユーザーの指示待ち）** |
| M2 | 非同期経路だけ、既存の static `profileSettings` を暫定利用し Phase7 へ引き継ぐ | 変更は最小だが、束縛ハザードが実害になり得る。S3 の決定と矛盾 |
| M3 | マクロ再生を instance クラス（`MacroRunner` 等）へ切り出す | Phase7 規模。モデル図 02・03 の変更も必要。Step3 の範囲外 |

**決定（2026-09-24）: P2 を採用し、M1 は採用しない**（ユーザー承認。Step3-6 の再集計で連鎖がさらに大きいと判明したため、前回の推奨 M1 から変更した）。
- 再集計で、非同期マクロ経路（グループ C、19件）に加え、`ctrl` を持たない判定補助メソッド群（グループ B、12件。呼び出し元は `getBoolSpecialActionMapping` 17・`GetBoolActionMapping` 11 など約50箇所）にも同じ連鎖があると分かった。引数渡し（P1）では約85箇所の変更が必要になり、`DefaultMacroPlayer` への依存追加（モデル図 04 の変更）も要る。しかもこれらは Phase7 で `Mapping` を instance 化すれば不要になる作業である。
- P2 は、`Mapping` に元からある静的フィールド `profileSettings`（`AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance`）から読む。`Global` 参照は完全に消え、Step3 の目的（`Global` 直接参照の根絶）を達成できる。`AppHost.GetService` は、ホスト未構築なら構築してから返す実装のため、通常は他の箇所と同じ Singleton に束縛される（Ledger §3.1 の懸念は理論上のもの。ただし静的結合が Phase7 まで残ることは変わらない）。
- 適用範囲: グループ B（Step3-6b）とグループ C（Step3-6c）のみ。`ctrl` を持つ経路（グループ A）は従来どおり S3（`ctrl.ProfileSettingsService` 等）で置換する。
- 実施時の約束: 置換箇所ごとに、Phase7 の instance 化で解消する旨の TODO/技術的負債コメント（`copilot-instructions.md` §3.3 原則4）を付ける。モデル図の変更は不要（`IMacroPlayer` の依存は変えない）。
- 注意点（テスト）: 静的 `profileSettings` は型初期化時に1回だけ束縛されるため、テストで `Global.ProfileSettingsServiceInstance` を差し替えても追随しない（Phase6-Status.md §6.4 の教訓1）。この経路の検証はソース走査ガードと実機確認で担保する。


### 3.5 Step3-6a 実施記録（2026-09-24）

**着手前の確認と決定（2026-09-24）**: Step3-6 の再集計（HEAD `236e76f`）で、残る `Global` 参照は58件（従来記載の73件は Step3-2 の12件の引き算漏れによる過大）、3グループに分かれると判明した。ユーザーの決定: **確認1＝R1 採用**（§2.4.2）、**確認2＝P2 採用**（§3.4.1）、**確認3＝Step3-6 を 3-6a／3-6b／3-6c の3バッチに分割**。

| グループ | 内容 | 件数 | バッチ |
|---|---|---:|---|
| A | `ControlService ctrl` を持つ経路（`MapCustom`／`ProcessControlSettingAction`／`MapCustomAction`／`getMouseMapping`／`Scale360degreeGyroAxis`）と、その呼び出し先の `ReleaseActionKeys`／`IfAxisIsNotModified` | 24 | **3-6a（本節）** |
| B | 判定補助メソッド群（`GetBoolMapping` 系、`GetMouseWheelMapping`） | 12 | 3-6b（P2） |
| C | 非同期マクロ経路（`PlayMacroCodeValue`／`AltTabSwapping`／`AltTabSwappingRelease`） | 19 | 3-6c（P2） |
| （Step3-7） | 温存: `Global.ApplyProfile` フォールバック、`ProfileSettingsServiceInstance`／`outputKBMHandler` の `??` フォールバック | 3 | Step3-7 |

**変更内容（グループ A、24件）**:

| 対象 | 変更 |
|---|---|
| `MapCustom` | `getProfileActionCount(device)` → `ctrl.ProfileActionProvider.GetProfileActionCount(device)`、`GetSASteeringWheelEmulationAxis(device)` → `ctrl.ProfileSettingsService.GetSASteeringWheelEmulationAxis(device)`（2件） |
| `ProcessControlSettingAction` | `ButtonMouseInfos[device]` 2件、`outputKBMMapping.GetRealEventKey` 1件、`reverseX360ButtonMapping[...]` 1件 → いずれも `ctrl.ProfileSettingsService` 経由（4件） |
| `MapCustomAction` | `GetDS4CSetting` 3件、`outputKBMMapping`（`Global.` 修飾の1件を含む）3件 → `ctrl.ProfileSettingsService` 経由。`ReleaseActionKeys` の呼び出し2件に `ctrl.ProfileSettingsService` を渡す（6件。`Global.ApplyProfile` は Step3-7 まで温存） |
| `ReleaseActionKeys` | シグネチャに `IProfileSettingsService settings` を追加（呼び出し元は `MapCustomAction` の2件のみ）。`GetDS4CSetting`・`outputKBMMapping` 3件を `settings` 経由に置換（3件） |
| `IfAxisIsNotModified` | 同様に `settings` 引数を追加し `GetDS4CSetting` を置換（1件）。**呼び出し元のない private メソッド**（確認済み）のため、注記コメントを付けて温存（削除は Phase7 の整理で判断） |
| `getMouseMapping` | `getLSDeadzone`／`getRSDeadzone` → `ctrl.ProfileSettingsService.LSModInfo[device].deadZone`／`RSModInfo[device].deadZone`、`ButtonMouseInfos[device]` → `ctrl.ProfileSettingsService.ButtonMouseInfos[device]`（3件） |
| `Scale360degreeGyroAxis` | `SAWheelFuzzValues`／`getSXDeadzone`／`WheelSmoothInfo`／`getSXAntiDeadzone` → `ctrl.ProfileSettingsService` 経由（4件。既にあった `profileSettings.OutContType[device]` は静的フィールド経由のまま変更なし） |
| 契約（R1） | `IProfileSettingsService` に `ReverseX360ButtonMapping { get; }` を追加。`ProfileSettingsService` は `Global.reverseX360ButtonMapping` の参照をそのまま返す（コピー版 `GetReverseX360ButtonMapping()` は残す） |
| `ServiceRegistration.cs`・`ControlService` | 変更なし（`ControlService` の `ProfileActionProvider`／`ProfileSettingsService` の internal プロパティは Step3-2・3-3 で追加済み） |

**挙動の同一性（実装前に確認）**: 置換前の `Global` 側の getter は `ProfileSettingsServiceInstance` または `m_Config`（`Global.store`）経由、置換後の `IProfileSettingsService`／`IProfileActionProvider` 側は `SafeConfig`（`_config ?? Global.store`）または同じ BackingStore 経由で、同じ配列・同じインスタンスを返す（`GetDS4CSetting` は `Global` が `ProfileSettingsServiceInstance` へ委譲済み）。`reverseX360ButtonMapping` は、元の `Global` の素の配列と同じ参照を返すため、割り当て・挙動とも変わらない。ホットパスで `Clone()`・ログ・キャッシュは追加していない。

**テスト**:
- `ProfileSettingsServiceReverseX360ButtonMappingTests.cs`（新規）: `ReverseX360ButtonMapping` が `Global.reverseX360ButtonMapping` と同一参照であること、コピー版が独立したコピーを返し続けること、読み取りの割り当てが0バイトであること。
- `MappingControlPathGlobalReferenceGuardTests.cs`（新規）: グループ A の7メソッドのソース走査ガード（移行済み `Global` メンバー12種の非修飾・`Global.` 修飾の参照が残っていないこと、`ReleaseActionKeys`／`IfAxisIsNotModified` が `settings` 引数を持つこと）。走査の方式（コメント除去＋波括弧の対応）は、実ファイルに対して事前に検証済み。
- これらのメソッド自体（`MapCustom`／`MapCustomAction` 等）を直接駆動するテストは、既存 `MappingSpecialActionSuppressionTests.cs` 冒頭コメントのとおり困難なため追加していない。挙動保持はビルドと実機確認で担保する。

**検証結果（確定・2026-09-24）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。実機確認は下記の項目1〜5がすべて問題なし（ボタン→別のゲームパッドボタン、ボタン→キー／マクロとキー解放、スペシャルアクション、ボタンによるマウス移動と Change Mouse Sensitivity、デッドゾーン0のときのマウス移動）。項目6（ステアリングホイールエミュレーション）は vJoy 環境がなく実施できないため、`Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録した。

**（実装時点の記録）未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。実機確認は次の項目: (1) ボタンを別のゲームパッドボタン（Xbox→DS4 の対応表を引く経路）へ割り当てた場合の動作、(2) ボタン→キー／マクロ割り当て（`GetRealEventKey`）の動作と、押した状態で離したときのキー解放、(3) スペシャルアクション（トリガーの押し離し、トリガーに割り当てたキー／マクロの解放、タップ判定）、(4) ボタンによるマウス移動と、Extras の「Change Mouse Sensitivity」（`ButtonMouseInfos`）、(5) スティックのデッドゾーンが 0 のときのマウス移動の補正（`getMouseMapping`）。ステアリングホイールエミュレーション（`Scale360degreeGyroAxis`、`GetSASteeringWheelEmulationAxis`）は、対象の vJoy 環境がなければ `Phase6-Step11-Plan.md` §3.3 の先送り台帳へ登録する。

### 3.6 Step3-6b 実施記録（2026-09-24）

**対象（グループ B、12件）**: `ctrl` を持たない判定補助メソッド群。方針は決定 P2（§3.4.1）で、引数渡しは行わず、`Mapping` の既存の静的フィールド `profileSettings` から読む。着手前に再確認した結果は、Step3-6 の再集計（§3.5）と一致した。

| 対象 | 変更 |
|---|---|
| `IsUsingSAForControls(device)`（6件: `GetByteMapping`／`GetBoolMappingExternal`／`GetBoolMapping`／`getBoolSpecialActionMapping`／`GetBoolActionMapping`／`GetXYAxisMapping`） | `Mapping` に private ヘルパー `IsUsingGyroForControls(int device)`（`profileSettings.GyroOutputMode[device] == GyroOutMode.Controls`）を新設し、6箇所の呼び出しを置換。`Global.IsUsingSAForControls`（`m_Config.gyroOutMode[i] == Controls`）と同じ BackingStore の同じ配列を見る。ヘルパー名を `IsUsingSAForControls` にしなかったのは、`using static DS4Windows.Global;` 経由の `Global` メソッドと取り違えないため |
| `SXSens`／`SZSens`（4件: `GetBoolMappingExternal`） | `profileSettings.SXSens[device]`／`profileSettings.SZSens[device]` に置換（`GetBoolMappingExternal` は `public` で外部呼び出し2箇所あり。シグネチャは変更なし） |
| `Global.outputKBMMapping`（2件: `GetMouseWheelMapping`） | `profileSettings.OutputKBMMapping.WHEEL_TICK_DOWN`／`WHEEL_TICK_UP` に置換 |
| TODO コメント | ヘルパー、`GetMouseWheelMapping`、`GetBoolMappingExternal` の `SXSens`／`SZSens` ブロックに、Phase7 の instance 化（コンストラクタ注入）で解消する旨の TODO を付与（`copilot-instructions.md` §3.3 原則4） |

契約（`DS4Windows/DI/`）・`ServiceRegistration.cs`・`ControlService`・モデル図の変更はなし。

**挙動の同一性（実装前に確認）**: `profileSettings` は `AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance` で束縛され、DI の Singleton（`ProfileSettingsService`。コンストラクタで `Global.store` を使う）を指す。`Global.IsUsingSAForControls`／`Global.SXSens`／`Global.outputKBMMapping` はいずれも `Global.store`（`m_Config`）または `ProfileSettingsServiceInstance`（同じ Singleton）へ到達するため、同じ配列・同じインスタンスを返す。`Clone()`・ログ・キャッシュは追加していない。

**テスト**:
- `MappingHelperMethodsGlobalReferenceGuardTests.cs`（新規）: グループ B の7メソッドのソース走査ガード（移行済み `Global` メンバー4種の非修飾・`Global.` 修飾の参照が残っていないこと、ヘルパーが `profileSettings.GyroOutputMode[device] == GyroOutMode.Controls` を読むこと）。走査の方式は実ファイルに対して事前に検証済み。
- `MappingGyroControlsHelperTests.cs`（新規）: リフレクションで `IsUsingGyroForControls` を呼び、出力モード（Controls／None／Mouse）を切り替えたときに `Global.IsUsingSAForControls` と同じ結果を返すことを検証（検証後は元の値へ戻す）。
- 静的 `profileSettings` を読む経路は、テストでサービスを差し替えても追随しない（型初期化時に1回だけ束縛。`Phase6-Status.md` §6.4 の教訓1）ため、引数渡しの検証（`MappingArgumentPassThroughTests`）と同じ方式のテストは追加していない。

**検証結果（確定・2026-09-24）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。実機確認は下記の項目1〜3がすべて問題なし（ジャイロによるボタン判定、ジャイロ出力モードがコントロール以外のときの非干渉、スティック／トリガーによるマウスホイール上下）。

**（実装時点の記録）未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。実機確認は次の項目: (1) ジャイロをコントロール（スティック・ボタン）に割り当てた場合の、ジャイロによるボタン判定（`GyroXPos`／`GyroXNeg`／`GyroZPos`／`GyroZNeg` を割り当てたボタンの反応）、(2) ジャイロ出力モードをコントロール以外（マウス等）にしたとき、ジャイロがボタン判定に影響しないこと、(3) スティックやトリガーをマウスホイールの上下に割り当てた場合のホイール回転（`GetMouseWheelMapping`）。(1)(2) はジャイロを使えるコントローラー（DS4／DualSense／Vader 4 Pro のジャイロ）が必要。

### 3.7 Step3-6c 実施記録（2026-09-24）

**対象（グループ C、19件）**: 非同期マクロ再生経路の `outputKBMMapping`（`PlayMacroCodeValue` 14件、`AltTabSwapping` 3件、`AltTabSwappingRelease` 2件）。マクロ再生は別スレッドで動き、入口が `ctrl` を持たない `DefaultMacroPlayer`（`PlayMacroDirect`／`EndMacroDirect`）経由にもあるため、決定 P2（§3.4.1）に従い引数渡しは行わず、`Mapping` の静的 `profileSettings` から読む。

| 対象 | 変更 |
|---|---|
| `PlayMacroCodeValue` | `outputKBMMapping.` 14箇所（マウスボタンの押下・解放、X ボタン、`macroKeyTranslate`／`GetRealEventKey` によるキー変換）を `profileSettings.OutputKBMMapping.` に置換 |
| `AltTabSwapping` | `outputKBMMapping.KEY_TAB` 3箇所を置換 |
| `AltTabSwappingRelease` | `outputKBMMapping.KEY_TAB`／`KEY_LALT` 2箇所を置換 |
| TODO コメント | 3メソッドの冒頭に、Phase7 の instance 化（コンストラクタ注入）で解消する旨の TODO を付与（`copilot-instructions.md` §3.3 原則4） |

シグネチャの変更・契約の変更・`ControlService`・`DefaultMacroPlayer`・モデル図の変更はなし（M1 のようなバケツリレーは行っていない）。K3（マクロ二重ガード `macroDispatchInFlight`）にも触れていない。これで `Mapping.cs` 全体から `outputKBMMapping` への `Global` 参照（非修飾・`Global.` 修飾とも）がなくなった。

**挙動の同一性（実装前に確認）**: 置換前の `outputKBMMapping` は `Global.ProfileSettingsServiceInstance.OutputKBMMapping`（DI の Singleton。呼び出しのたびに解決）を返す。置換後の静的 `profileSettings` は `AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance` で束縛され、通常は同じ Singleton を指す。読み取りは使用のたびに行い（キャッシュしない）、`RefreshOutputKBMHandler` でマッピングが再構築されたときも従来どおり新しいものを読む。割り当て・ログ・キャッシュの追加はない。相違点: 静的 `profileSettings` は型初期化時に1回だけ束縛されるため、テストで `Global.ProfileSettingsServiceInstance` を差し替えても追随しない（`Phase6-Status.md` §6.4 の教訓1）。

**テスト**: `MappingMacroPathGlobalReferenceGuardTests.cs`（新規）: 3メソッドのソース走査ガード（`outputKBMMapping` の非修飾・`Global.` 修飾の参照が残っていないこと、`profileSettings.OutputKBMMapping.` 経由であること）と、`Mapping.cs` 全体に `outputKBMMapping` への `Global` 参照がないことの固定。走査の方式は実ファイルに対して事前に検証済み。マクロ再生自体は実際のキー・マウス入力を送出するため、挙動テストは追加していない（ガードと実機確認で担保）。既存の `MappingPlayMacroDispatchGuardTests`（`PlayMacro` をリフレクションで呼ぶ）は、`PlayMacro` のシグネチャを変更していないため影響を受けない。

**検証結果（確定・2026-09-24）**: ユーザー側でビルド・テストビルド・テスト実行がすべて成功し、コミットしてリモートリポジトリに反映済み。実機確認は下記の項目1・2・4・5が問題なし。項目3（Alt+Tab マクロ）は、押している間 Tab が繰り返し送られて2つのウィンドウ間でフォーカスが切り替わり続け、離した時点でフォーカスされていたウィンドウで確定する挙動だった。これは Alt+Tab マクロの設計（押している間、一定間隔で Tab を送り続け、離すと確定する）と、Windows の Alt+Tab 設定・開いているウィンドウ数に依存する見え方であり、ユーザーも環境依存と判断した。Step3-6c の変更は `KEY_TAB`／`KEY_LALT` の読み取り元の置換のみで送出内容を変えていないため、回帰とは判断していない（ただし変更前後の比較はしていない）。念のため `Phase6-Step11-Plan.md` §3.3 に参考項目として登録した。Bug1（Alt+Tab 誤爆）の調査の参考にもなる。

**（実装時点の記録）未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。実機確認は次の項目: (1) Controls タブでボタンにキー入力のマクロ（「Record A Macro」で記録）を割り当てて動作すること、(2) マウスクリック（左・右・中・X ボタン）を含むマクロが動作すること、(3) Alt+Tab のウィンドウ切り替えマクロ（押している間 Tab で切り替え、離すと確定）が動作すること、(4) スペシャルアクションのマクロ（`DefaultMacroPlayer` 経由の入口を含む）が動作すること。あわせて、押し続けの繰り返しマクロ（Repeat while held）が二重に走らないこと（K3 のガードの回帰確認）も見る。

### 3.8 Step3-7 の事前調査と着手前の確認事項（2026-09-24、別セッションで再開）

**現状（HEAD `74e344f`、Step3-6c 反映後）**: `Mapping.cs` に残る `Global` 直接参照は次の3件のみ（Step3-6 の再集計と一致）。

| 行（概数） | 参照 | 内容 | 既定の扱い |
|---:|---|---|---|
| 75 | `Global.ProfileSettingsServiceInstance` | 静的 `profileSettings` の初期化子の `??` フォールバック（`AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance`） | 温存（防御コード。Step2 の C2-02 と同扱い）。TODO コメントの確認のみ |
| 78 | `Global.outputKBMHandler` | `VirtualKBM` getter の `??` フォールバック（`AppHost.GetService<IVirtualKBM>() ?? Global.outputKBMHandler`） | 温存（防御コード）。TODO コメントの確認のみ |
| 5139 | `Global.ApplyProfile` | `MapCustomAction`（`Task.Run` 内）の、`IProfileApplicationService` 未登録時のフォールバック | §2.4.3 の選択（既定 K-1: 温存＋TODO。K-2: K1 を先に是正してから `AP.ApplyProfile` へ置換）。**ユーザーへ選択肢を提示して確認する** |

**`using static DS4Windows.Global;`（31行）の削除可否**: 事前調査（字句レベル）では、除外項目としていた純粋計算・定数のうち `Clamp`（23件）・`MAX_DS4_CONTROLLER_COUNT`（22件）・`TEST_PROFILE_ITEM_COUNT`（3件）はすでに `Global.` 修飾済みで、非修飾のまま残るのは `getTransitionedColor`（2件）のみ。したがって、削除には `getTransitionedColor` の修飾（2件）が必要になる見込み。ただしこの環境ではコンパイルできないため、ほかにも非修飾の `Global` メンバー（`using static` 経由の型・定数など）が残っていないかは、削除後のビルドで最終確認する必要がある。削除する場合は、ビルドエラーが出たものを1件ずつ修飾する手順を Step3-7 の計画に入れる。あわせて、`using static` を外すと `Mapping` 内の他の `Global` 参照（上表の3件）は `Global.` 修飾のままコンパイルできる。

**Step3-7 着手時の作業案**: (1) 上表3件の最終確認と、K-1／K-2 の選択（メリット・デメリット・推奨の形で提示して確認）、(2) `getTransitionedColor` の修飾と `using static DS4Windows.Global;` の削除、(3) ビルド・テストビルド・テスト実行の確認（ビルドエラーが出た箇所の修飾）、(4) 回帰ガードテスト（`Global` 直接参照が `Mapping.cs` に温存3件以外に残っていないこと）の追加、(5) 実機確認（起動・接続・プロファイル適用・スペシャルアクション、K-2 を選んだ場合は `ApplyProfile` の経路）、(6) 本計画書 §6 の完了判定チェックリストの更新。

**Step3-6c 完了確定（2026-09-24、ユーザーのビルド・テスト・実機確認完了）**: 上記4項目および繰り返しマクロの二重実行がないことを含め、すべて問題なしと確認された。Step3 の残実参照数は3（Step3-7 の温存3件のみ）で確定。

### 3.8 Step3-7 実施内容（2026-09-24実装）

**対象（温存3件の最終処理、§1・§2.4.3）**: `Global.ProfileSettingsServiceInstance`（`profileSettings` フィールドのフォールバック）、`Global.outputKBMHandler`（`VirtualKBM` プロパティのフォールバック）、`Global.ApplyProfile`（`MapCustomAction` 内、`ActionManager` 未処理時のフォールバック）。加えて `using static DS4Windows.Global;`（31行目付近）の削除可否判定。

**§2.4.3 の選択（着手前確認）**: **既定の K-1 を採用**（K-2 は不採用）。`IProfileApplicationService.ApplyProfile` の既知の不具合 K1（`Phase6-Status.md` §6.5: `deviceIndex >= 4` のスロットを拒否・戻り値を捨てて成功扱い・`Halt` を自身で行わない）が是正されるまで、K1 是正という Step3 の対象外の作業を持ち込まないため、`Global.ApplyProfile` フォールバックはそのまま温存する。K-2（Step3 内で K1 を先に是正）は、`Mapping.cs` 以外のファイル（`ProfileApplicationService` の実装）への変更が必要になり、Step3 の対象ファイル原則を超えるため不採用。

**変更内容**:

| 対象 | 変更 |
|---|---|
| `profileSettings`（`Mapping.cs` 79〜81行） | TODOコメントを追加。`Global.ProfileSettingsServiceInstance` へのフォールバックはコード上変更なし（温存） |
| `VirtualKBM`（`Mapping.cs` 84〜86行） | TODOコメントを追加。`Global.outputKBMHandler` へのフォールバックはコード上変更なし（温存） |
| `MapCustomAction` 内 `Global.ApplyProfile` フォールバック（K-1採用） | TODOコメントを追加（K1 是正後は本ファイル冒頭で解決済みの静的フィールド `profileApplication`（`IProfileApplicationService`）の `ApplyProfile` へ置換予定、と明記）。呼び出し自体はコード上変更なし（温存） |
| `using static DS4Windows.Global;`（31行目） | **削除は見送り**（理由は下記）。ディレクティブの直前に、見送り理由と残存参照を記録するコメントを追加 |

**`using static DS4Windows.Global;` の削除可否判定（完了判定チェックリスト最終項目）**:

- 判定方法: `Global` クラス（`ScpUtil.cs`）が宣言する `public`/`internal static` メンバー（フィールド・プロパティ・メソッド・イベント・定数、計456件を抽出）それぞれについて、`Mapping.cs` 内で「`.` を伴わない裸の参照」が実際のコード（コメント除去後）に残っているかを機械的に走査した。
- **結果**: `getTransitionedColor`（`Mapping.cs` 5465・5467行付近、`BatteryCheck` スペシャルアクションのライトバー色遷移計算）の2箇所が、非修飾のまま `Global.getTransitionedColor` を呼び出していることを確認した。この関数は §1 の対象件数表における「除外（純粋計算 `Clamp`/`getTransitionedColor`、真の定数）51件」の1つであり、そもそも Step3 の解消対象（実参照150件）に含まれていない。既存の `Global.Clamp` 呼び出し（複数箇所、いずれも `Global.` 修飾済み）も同じ除外区分に属し、コード上は既に明示修飾されているため `using static` には依存していない。
- それ以外の456候補について、`OutContType`（enum型名。`Global` の同名メンバーではない）、`getL2AntiDeadzone`/`getL2Maxzone`/`getR2AntiDeadzone`/`getR2Maxzone`（`SetCurveAndDeadzone` 内のブロックコメント `/* ... */` 内の死んだコードのみで、実行されるコードでは Step3-4 で `settings.L2ModInfo`/`R2ModInfo` に置換済み）、`useTempProfile`（ログの文字列補間のラベル文字列であり実際のメンバー参照ではない）を検出したが、いずれも偽陽性（実際の `Global` 参照ではない）と確認した。
- **判定**: **見送り**。`getTransitionedColor` が非修飾で呼ばれている限り `using static DS4Windows.Global;` を削除するとビルドが通らなくなるため、本 Step では削除しない。`getTransitionedColor` はそもそも Step3 の対象外（除外51件）であり、これを置換すること自体が Step3 の範囲を超える（`Clamp` 同様、単純な値変換のみで状態を持たないため、Global 直接参照根絶の実害は小さいと判断されている）。除外51件の要否判断は、Step3 の範囲外として記録する。
- **残存参照の記録**: `Mapping.cs` に残る `Global.` 実利用箇所は、温存3件（`ProfileSettingsServiceInstance`／`outputKBMHandler`／`ApplyProfile`、各1箇所、TODOコメント付き）と、除外区分の `Global.Clamp`（複数箇所、明示修飾）のみ。`getTransitionedColor` は非修飾（`using static` 経由）で2箇所。これ以外の `Global` 実利用（150件のうち145件相当。Step3-3の5件は対象消滅済み）はすべて Step3-1〜3-6c で解消済み。

**挙動の同一性**: いずれもコメント追加のみで、実行コード（フォールバックの条件・`ApplyProfile` の呼び出し引数）は一切変更していない。

**テスト**: `MappingStep3FinalGlobalReferenceGuardTests.cs`（新規）: (1) `using static DS4Windows.Global;` が引き続き存在し、`getTransitionedColor` の非修飾呼び出しが存在すること（using static 存置の前提を固定）、(2) 温存3件（`Global.ProfileSettingsServiceInstance`／`Global.outputKBMHandler`／`Global.ApplyProfile(`）がそれぞれ厳密に1箇所ずつ存在すること（再増加の検出）、(3) 温存3件それぞれに Step3-7 の TODO コメントが付与されていること。

**未検証事項**: この環境には dotnet がなく、`dotnet build`／`dotnet test` は実行できていない。ユーザー側でビルド・テストビルド・テスト実行を確認してからコミットすること。今回はコメント追加のみで実行コードの変更がないため、実機での挙動確認は不要と判断するが、念のため通常のプロファイル切替（Controls タブの Load Profile スペシャルアクション）が従来どおり動作することを確認することが望ましい。

**完了判定チェックリスト（§6）との対応**: §1 の実参照150件（Step3-3消滅分5件を除く145件）はすべて解消済み。温存3件は TODO コメント付きで温存、JUDGE 2件（`reverseX360ButtonMapping`→Step3-6a で R1 採用済み、`Global.ApplyProfile`→本節で K-1 採用済み）はいずれも解消・温存判断済み。`using static DS4Windows.Global;` の削除可否は本節で判定・記録済み（見送り）。Step3 は本節をもって完了確定候補とする（ユーザーのビルド・テスト確認待ち）。

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

- [x] §1 の実参照150件が、§3 の各バッチにより解消されていること（KEEP 3件は TODO コメント付き温存、JUDGE 2件は §2.4 の選択後に解消または温存判断済みであること）。**2026-09-24、Step3-7 §3.8 で確定。Step3-3消滅分5件を除く145件を解消。KEEP3件はTODO付き温存、JUDGE2件はR1・K-1採用で決着。**
- [x] 新設・拡張した契約（`IDisplayCoordinateService` 等4件）にモックベースの単体テストがあること。**Step3-1で対応（`IDisplayCoordinateService`自体はAbs Mouse機能削除［§6.8］に伴い撤去済み）。**
- [x] `SetCurveAndDeadzone` 等のホットパスで新たなヒープ割り当てが発生していないこと（§2.4.2 の選択結果を反映）。**`MappingHotPathAllocationTests.cs`で固定済み。**
- [x] `ControlService`（またはその他の呼び出し元）から `Mapping` への引数渡しが Pure DI の原則に準拠していること（新たな Service Locator を `Mapping` に持ち込んでいないこと）。
- [x] モデル図 03・04 の `IDisplayCoordinateService` / `IProfileXmlStore` の記載が、実装後のコードと一致していること。**`IDisplayCoordinateService`は§6.8の削除に伴い撤去反映済み。**
- [ ] `dotnet build -c Release` でエラー・警告0件。**この環境にdotnetがないため未実行。ユーザー側での確認待ち。**
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功。**同上、ユーザー側での確認待ち。**
- [x] `using static DS4Windows.Global;`（31行目）の削除可否が判定され、記録されていること（削除する場合は本 Step 内で実施、見送る場合は理由と残存参照を記録）。**2026-09-24、Step3-7 §3.8 で判定・記録済み。見送り。理由: `getTransitionedColor`［除外51件の1つ、Step3対象外］が非修飾で残存するため。**