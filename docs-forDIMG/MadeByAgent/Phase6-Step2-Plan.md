# Phase6-Step2 計画書: `ControlService.cs` のGlobal直参照解消

作成日: 2026-09-09  
改訂日: 2026-09-19（決定D1〜D3の反映・実地再確認による是正・モデル図連動更新／PR-1 実装に伴う対象ID・型の是正／PR-1 検証記録・決定O2=C・PR-1b 実装の追記）  
前回改訂: 2026-09-18（Phase6-Step1 コア参照エビデンスに基づく全74箇所網羅・Pure DI設計・ホットパス性能保護の全面拡充改訂）  
状態: PR-1 完了（ビルド・テスト成功、コミット済み `da16de4`、実機確認は OSC/UDP のみ未実施）・決定O2=C 確定／PR-1b 実装済み（レビュー待ち）・O1／O3 決定待ち  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（コア層実地調査エビデンス）  
  - `Phase6-Step1-ABC-Classification.md`（a/b/c分類・契約差検証記録）  
  - `Phase6-Step1-Unique-ID-Manifest.md`（一意ID台帳）  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（横断設定・孤立プロパティ監査）  
  - `docs-forDIMG/Model-Diagram/01〜04`（2026-09-19 連動更新済み。IVirtualKBMLifecycle 追加、ControlService 依存の更新、過渡期の逆依存の明記）

---

## 0. 背景と改訂の経緯

### 0.1 暫定39件から全74実参照式への昇華
Phase6上位計画（`Phase6-Plan.md` §0.2）では、`ControlService.cs` の対象件数を「暫定39件」としていた。これは主に `Global.` 修飾子による単純 grep 抽出に由来する値であった。  
しかし、2026-09-18に実施された Step1 再監査（`Phase6-Step1-Core-Reference-Evidence.md`）において、同ファイル冒頭の `using static DS4Windows.Global;`（36行目）による**無修飾参照（U: Unqualified）**が多数実利用されていることが判明した。  
実コードの精密走査の結果、修飾参照（Q: Qualified）84行・93出現に加え、無修飾参照を含めた実質的な Global 依存式は**計74箇所（除外8箇所を除く実移行対象66箇所）**に上ることが確定した。

本計画書は、先行して記録されていた候補（C2-01〜C2-03）の枠組みを全74箇所へ全面的に拡張し、すべての参照箇所に対する移行先サービス、分類（a: 既存契約 / b: 軽微拡張 / c: 要設計 / 除外）、ホットパス区分、依存性注入（Pure DI）構造、PR分割計画、および検証基準を完全網羅した実装計画書として改訂したものである。

### 0.2 `ControlService.cs` の重要性と課題
`ControlService.cs`（約3,400行）は、4層アーキテクチャにおいて**第1層（入力読み取り層）と第2層（信号変換層）の境界**に位置し、コントローラーのライフサイクル管理、ViGEm仮想バスへの出力割り当て、UDP/OSCモーションサーバー連携、および毎秒250〜1000回実行される入力ポーリング（`On_Report`）を統括する最重要コンポーネントである。  
本改修における最大のリスクは以下の3点である：
1. **ホットパス性能の維持**: 入力ポーリングループ内で仮想メソッド呼び出しや余分なヒープ割り当て・クローン処理が発生し、遅延やGCスパイクを引き起こすリスク。
2. **循環依存の防止**: `ProfileApplicationService` が既に `ControlService` に依存しているため、逆向きにコンストラクタ注入を行うことによる循環参照リスク。
3. **二重管理・孤立設定の再発防止**: DIサービス側が独自フィールドを持たず、正本（`BackingStore` / `Global`）と完全に同期する薄いシム（SSOT）を維持する原則。

### 0.3 2026-09-19 実地再確認による是正と確定決定事項

#### 0.3.1 実地再確認の結果（HEAD `00080d6`、`For-DI-migration-work`、作業ツリー差分なし）

| 項目 | 計画書（2026-09-18版）の記載 | 実コード（HEAD `00080d6`） | 対応 |
|---|---|---|---|
| ファイル規模 | 約3,400行 | 3,350行 | 記載のみ更新 |
| `Global.` を含む行 | 修飾84行 | 84行（コメント行を除くと75行。const宣言行を含む） | 付録A参照 |
| 行番号 | 表1〜3の各行番号 | 多くが +10 行ずれ（例: コンストラクタ 204→214、KBM 478→488、`ApplyProfileToSlot` 2168→2178）。一部は +14〜+21（`GetSASteeringWheelEmulationAxis` 2890→2911 等） | §2.1 注記・付録A。各PR着手時に grep 台帳を再作成（§6） |
| 循環依存 | `ProfileApplicationService ⇄ ControlService` のみ | 加えて `OutputSlotService` が `ControlService` に依存（`_control = control ?? Program.rootHub`）。`ProfileRepository` も `AppHost.GetService<ControlService>()` を実行時に呼ぶ | 決定D1（§3.2） |
| `ControlService` 生成経路 | 未記載 | `ServiceRegistration` のファクトリ（`Program.rootHub ?? new ControlService(...)`）のみ。`Program.rootHub` への代入は生成後（`App.xaml.cs:724`）。`new ControlService(` は全ソリューションでこの1箇所のみ | 決定D2（§3.1） |
| KBM ハンドラ管理 | 「`_virtualKBM` アダプタ経由で更新」 | `OutputKBMHandlerAdapter` は `Global.outputKBMHandler`（`VirtualKBMBase` の static フィールド）への読み取り専用委譲。`Version` は get のみで、ハンドラ生成・差し替え・フォールバック切替の口が無い | 決定D3（§4.2） |
| `IVirtualKBM` の名前空間 | `DI.IVirtualKBM`（§3.1 のコード例） | `DS4Windows.Services.IVirtualKBM`（`DS4Control/Services/IVirtualKBM.cs`） | §3.1 のコード例を修正 |
| `Global.activeOutDevType` | 未記載 | `public static OutContType[]`。配列参照の再代入はリポジトリ全体で宣言時の初期化のみ（要素書込みのみ） | D1 の配列参照キャッシュの根拠 |

※ この環境には `dotnet` が無く、ビルド・テスト・ベンチマークは実行していない。上表は grep による静的確認のみに基づく。ビルド／テスト／実機は各PRの検証手順（§6・§7）に従い手元で実施する。

#### 0.3.2 確定した決定事項（2026-09-19。D1〜D3 は推奨案を承認、O2 は推奨（B）と異なる C を選択）

| ID | 論点 | 確定内容 |
|---|---|---|
| **D1** | `ControlService` → `IOutputSlotService` の循環（`OutputSlotService` が `ControlService` に依存） | `ControlService` には `IOutputSlotService` を直接注入せず、**`Func<IOutputSlotService>`（遅延解決）**を注入する。初回使用時に解決し、`ActiveOutDevType` 配列参照を1回だけキャッシュしてホットパスは配列直接参照とする（詳細は §3.2）。`OutputSlotService` 側の逆依存除去は Phase6-Step5 で実施し、その時点で `Func` を直接注入へ戻す |
| **D2** | コンストラクタの `AppHost.GetService` フォールバック | **新規引数はすべて必須引数（Pure DI）**とし、`?? AppHost.GetService<T>()` フォールバックは新設しない。`ServiceRegistration` のファクトリで全て解決する。既存の `profileSettings ?? Global.ProfileSettingsServiceInstance` のみ、過渡期の防御コードとして本文内に残す（Step12 で削除判断） |
| **D3** | KBM ハンドラの生成・差し替え | **新規 `IVirtualKBMLifecycle`（1ファイル1型）**を追加し、実装 `OutputKBMHandlerLifecycle` は `Global` への薄い委譲とする。`IVirtualKBM`（送出専用）は変更しない（詳細は §4.2） |
| **O2** | C2-01（`CURRENT_DS4_CONTROLLER_LIMIT` の静的初期化 `Global.IsWin8OrGreater()`） | **C（インスタンス化）を採用**（推奨案 B の「静的ヘルパーへ移設」ではなく、上限を DI サービスの値として扱う）。実装が大きいため、実施方式（段階／一括）は O3 で決定する。PR-1b で `IEnvironmentService.ControllerSlotLimit` / `UsingMaxControllers` を新設し、`ControlService` 内部を注入値へ移行した（§6 Step2-1b） |

#### 0.3.3 未決事項

| ID | 論点 | 決定時期 |
|---|---|---|
| **O3** | O2=C の実施方式。外部の未移行呼び出し元が54箇所（15ファイル）あり、その多くは Step5／8／9／10／12 の対象ファイルである。(A) 段階実施: 各 Step が対象ファイルを Pure DI 化する際に同時に置換し、`ControlService` の static 互換シムは Step12 で削除する。(B) 一括実施: 54箇所を Step2 内で全置換し、互換シムを即時に撤去する | PR-1b レビュー時に、メリット・デメリット・推奨（A）を提示して決定 |
| **O1** | C2-47（`Global.ApplyProfileToSlot`）の扱い。`IProfileApplicationService.ApplyProfile` は `Global.ApplyProfileToSlot` と等価ではない（§0.3.5 K1）ため、(A) `Func<IProfileApplicationService>` への置換は、先に K1 の是正と等価性確認が必要。(B) イベント経由の委譲は過剰設計。(C) `Global.ApplyProfileToSlot` を温存（同メソッドの XML コメントが既に「フェーズGで `IProfileApplicationService` へ完全委譲」と予告済み） | Step2-2（PR-2）着手時に、メリット・デメリット・推奨（C）を提示して決定 |

#### 0.3.4 モデル図の連動更新

`docs-forDIMG/Model-Diagram/` の 01〜04 を本改訂と同時に更新した（`IVirtualKBMLifecycle` の追加、`ControlService` の依存一覧の更新、過渡期の逆依存の注記）。図が示す目標構造（`OutputSlotService` は `IOutputSlotStore` のみに依存、`ProfileApplicationService` は `ControlService` に依存しない）は変更していない。


#### 0.3.5 実施中に判明した既知の課題

| ID | 内容 | 影響 | 対応 |
|---|---|---|---|
| **K1** | `ProfileApplicationService.ApplyProfile`（`DS4Control/Services/ProfileApplicationService.cs`）は `deviceIndex >= 4` をハードコードで拒否する（`Global.ApplyProfileToSlot` は `CURRENT_DS4_CONTROLLER_LIMIT` = 最大8 まで許可）。また `source == MappingAction` 以外では `device.HaltReportingRunAction` の内側で適用するが、`ControlService` の接続処理（C2-47）から呼ぶと Halt が入れ子になる可能性がある | C2-47 を `IProfileApplicationService` へ置換すると、スロット5〜8で適用が失敗し、接続時の挙動も変わり得る。現行の呼び出し元（`Global.ApplyProfileToSlot` → `Global.ApplyProfile`）は当該サービスを経由しないため、現時点では顕在化していない | O1 の決定に合わせて扱う。是正する場合はスロット上限の参照を `IEnvironmentService.ControllerSlotLimit` へ替えることが自然（O2 と連動） |

---

## 1. 目的

`ControlService.cs` 内のすべての `Global.*` 直接参照および `using static DS4Windows.Global;` 経由の無修飾静的参照を解消し、コンストラクタ経由で注入された DI サービス経由の呼び出しへ完全移行する。これにより、信号変換層・入力監視層の境界における静的依存を根絶し、テスト容易性と保守性を恒久的に向上させる。

---

## 2. 全件参照台帳（全74箇所・完全カタログ）

`Phase6-Step1-Core-Reference-Evidence.md` の実コード検証結果に基づく全参照の一覧である。

### 2.1 区分表記の定義
> **行番号に関する注意（2026-09-19）**: 表1〜表3の行番号は前回改訂時点のコードに対する値で、現行HEAD（`00080d6`）とは多くが +10 行程度ずれている。実装時の正本は付録A（現行HEADの修飾参照台帳）と、各PR着手時に作成する grep 台帳とする。ID・移行先・分類は本表が正本である。

- **形式**: `Q` = `Global.` 明示修飾 / `U` = `using static DS4Windows.Global;` による無修飾
- **分類**:
  - `(a)` 既存DIサービスに同一または同等の契約・実装が既に存在（単純・確実な置換）
  - `(b)` 既存DIサービスへの軽微なプロパティ／メソッド追加が必要（薄いシムの追加）
  - `(c)` ライフサイクル管理や複数責務の集約など、軽微な設計整理が必要
  - `除外` 真の定数（`const`）、純粋計算、または Phase6 明示的対象外（ViGEm クライアント直接依存）
- **経路（ホットパス区分）**:
  - `H`: 毎レポート実行される超高頻度ホットパス（毎秒250〜1000回）
  - `H条件`: 入力ポーリング経路上だが、初回・状態変化時・特定モード時のみ実行
  - `N`: 初期化・設定変更・デバイス接続・切断等の非ホットパス
  - `N†`: 通常は設定変更経路だが、プロファイル動的切替等の入力起点からも到達可能
  - `A`: 入力から起動される非同期ワーカー・マクロ処理

---

### 表1: 初期化・KBM・環境・サーバー系（ID: C2-01 〜 C2-27、計31参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-01 | 51 Q | 静的初期化 | `IsWin8OrGreater()` | (b) | N | **O2=C（確定）→ PR-1b で実装済み**: `IEnvironmentService.ControllerSlotLimit` / `UsingMaxControllers` を新設。`ControlService` 内部は注入値 `_controllerSlotLimit` を使用し、static は互換シム（外部54箇所の移行は O3 に従う）。`ControlService.cs` から `Global.IsWin8OrGreater()` 参照は消える |
| C2-02 | 204 Q | コンストラクタ | `ProfileSettingsServiceInstance` | (a) | N | **D2**: 過渡期の防御コードとして温存（`profileSettings ?? Global.ProfileSettingsServiceInstance`）。技術的負債コメント付与済み。Phase6-Step12 で削除判断 |
| C2-03 | 242 Q | コンストラクタ | `DeviceOptions` | (b) | N | `IAppSettingsService.DeviceOptions`（`BackingStore.deviceOptions` 委譲） |
| C2-04 | 250 Q | コンストラクタ | `UDPServerSmoothingMincutoffChanged` | (b) | N | `IAppSettingsService.UDPServerSmoothingMincutoffChanged` イベント購読 |
| C2-05 | 251 Q | コンストラクタ | `UDPServerSmoothingBetaChanged` | (b) | N | `IAppSettingsService.UDPServerSmoothingBetaChanged` イベント購読 |
| C2-06 | 262 Q | `SystemEvents_DisplaySettingsChanged` | `PrepareAbsMonitorBounds` | (c) | N | PR-1 で `IEnvironmentService.PrepareAbsMonitorBounds(string)`（`Global` への薄い委譲）を新設して置換。将来 `IDisplayCoordinateService`（Phase6-Step4 と連動して再評価） |
| C2-07 | 332 Q | `CreateOSCCallback` | `isInterpretingOscMonitoring()` | (b) | N/受信時 | `IAppSettingsService.InterpretingOscMonitoring` |
| C2-08 | 356 U | `CreateOSCCallback` | `isUsingOSCSender()` | (b) | N/受信時 | `IAppSettingsService.UseOscSender` |
| C2-09 | 478, 481 Q | `RefreshOutputKBMHandler` | `outputKBMHandler` (null判定・代入) | (c) | N | **D3**: `_kbmLifecycle.ReleaseHandler()`（null判定・Disconnect・null代入を実装側に内包） |
| C2-10 | 480 Q | `RefreshOutputKBMHandler` | `outputKBMHandler.Disconnect()` | (a) | N | `_virtualKBM.Disconnect()` |
| C2-11 | 484, 486 Q | `RefreshOutputKBMHandler` | `outputKBMMapping` (null判定・代入) | (a) | N | `_profileSettings.OutputKBMMapping = null` |
| C2-12 | 495 Q | `InitOutputKBMHandler` | `InitOutputKBMHandler(...)` | (c) | N | **D3**: `_kbmLifecycle.DetermineHandler(identifier)`（実装は `Global.InitOutputKBMHandler` への委譲） |
| C2-13 | 500 Q | `InitOutputKBMHandler` | `outputKBMHandler.Connect()` | (a) | N | `_virtualKBM.Connect()` |
| C2-14 | 507 Q | `InitOutputKBMHandler` | `outputKBMHandler` (代入) | (c) | N | **D3**: `_kbmLifecycle.SwitchToFallbackHandler()` |
| C2-15 | 512 U, 518 Q | `InitOutputKBMHandler` | `outputKBMHandler.GetIdentifier()` | (a) | N | `_virtualKBM.GetIdentifier()` |
| C2-16 | 514 Q | `InitOutputKBMHandler` | `outputKBMHandler.Version = ...` | (c) | N | **D3**: `_kbmLifecycle.ApplyFakerInputVersion()`（`IVirtualKBM.Version` は get 専用のまま変更しない） |
| C2-17 | 514 Q | `InitOutputKBMHandler` | `fakerInputVersion` | (b) | N | `IEnvironmentService.FakerInputVersion`（**D3**: 利用者は `OutputKBMHandlerLifecycle`。`ControlService` からの直接参照は無くなる） |
| C2-18 | 518 Q | `InitOutputKBMHandler` | `InitOutputKBMMapping(...)` | (c) | N | **D3**: `_kbmLifecycle.InitializeMapping(identifier)`（実装は `Global.InitOutputKBMMapping` への委譲） |
| C2-19 | 519 Q | `InitOutputKBMHandler` | `outputKBMMapping.PopulateConstants()` | (a) | N | `_profileSettings.OutputKBMMapping.PopulateConstants()` |
| C2-20 | 520 Q | `InitOutputKBMHandler` | `outputKBMMapping.PopulateMappings()` | (a) | N | `_profileSettings.OutputKBMMapping.PopulateMappings()` |
| C2-21 | 663, 698 Q | `RequestElevation`, `CheckHidHide` | `exelocation` | (a) | N | `_pathService.ExecutablePath` |
| C2-22 | 687, 764, 792, 804, 858 Q | HidHide 判定・属性更新 | `hidHideInstalled` | (b) | N | `IEnvironmentService.HidHideInstalled` |
| C2-23 | 803 Q | `CheckAffected` | `GetInstanceIdFromDevicePath(...)` | (b) | N | `IEnvironmentService.GetInstanceIdFromDevicePath(...)` |
| C2-24 | 806 Q | `CheckAffected` | `CheckHidHideAffectedStatus(...)` | (b) | N | `IEnvironmentService.CheckHidHideAffectedStatus(...)` |
| C2-25 | 886, 1012, 1736 Q | UDP ポート取得 | `getUDPServerPortNum()` | (a) | N | `_appSettings.UdpServerPort` |
| C2-26 | 887, 1013, 1737 Q | UDP アドレス取得 | `getUDPServerListenAddress()` | (a) | N | `_appSettings.UdpServerListenAddress` |
| C2-27 | 929, 931, 945, 946 Q | OSC ポート・アドレス取得 | `getOSCServerPortNum()`, `getOSCSender*` | (b) | N | `_appSettings.OscServerPort`, `_appSettings.OscSenderAddress`, `_appSettings.OscSenderPort` |

---

### 表2: サービス起動・停止・プロファイル・出力状態系（ID: C2-28 〜 C2-49、計30参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-28 | 1416 Q | `PluginOutDev` | `OutContType[index]` | (a) | N† | `_profileSettings.OutContType[index]`（旧C2-01）。`IOutputSlotService` は経由しない（D1 の対象外） |
| C2-29 | 1419, 2176, 2569 U | `PluginOutDev`, `PrepConn`, `SyncChange` | `getDInputOnly(index)` | (a) | N† | `_profileSettings.GetDInputOnly(index)`（永続設定） |
| C2-30 | 1424, 1567, 1574, 1604, 1904, 2197, 2561, 2608, 2670 U | プラグイン／アンプラグ／停止／同期 | `useDInputOnly[index]` | (a) | N† | `_profileSettings.UseDInputOnlyArray[index]`（実行時状態） |
| C2-31 | 1429, 1495, 1584, 1586, 2188, 2191, 2198, 2563, 2671 Q/U | スロット割当・解放・同期 | `activeOutDevType[index]` | (a) | N† | **D1**: `ActiveOutDevType[index]`（`Func<IOutputSlotService>` の遅延解決結果からキャッシュした配列参照。§3.2） |
| C2-32 | 1623 Q | `Start` | `IsAdministrator()` | (a) | N | `_environmentService.IsAdministrator()` |
| C2-33 | 1624, 1630 Q | `Start` | `outputKBMHandler.GetIdentifier()`, `.GetFullDisplayName()` | (a) | N | `_virtualKBM.GetIdentifier()`, `_virtualKBM.GetFullDisplayName()` |
| C2-34 | 1633 U | `Start` | `getUseExclusiveMode()` | (a) | N | `_appSettings.UseExclusiveMode` |
| C2-35 | 1643, 1648, 1653 U | `Start` | `isUsingOSCServer()`, `isUsingOSCSender()`, `isUsingUDPServer()` | (a)/(b) | N | `_appSettings.UseOscServer`, `_appSettings.UseOscSender`, `_appSettings.UseUdpServer` |
| C2-36 | 1774, 1848, 1957 U | `Start`, `Stop` | `runHotPlug` (書込み) | (a) | N | `_appSettings.RunHotPlug = ...` |
| C2-37 | 1778 Q | `Start` | `ProcessPriority` | (b) | N | `_appSettings.ProcessPriority` |
| C2-38 | 1822 U | `CheckQuickCharge` | `getQuickCharge()` | (b) | N | `_appSettings.QuickCharge` |
| C2-39 | 1863 U | `Stop` | `DCBTatStop` | (b) | N | `_appSettings.DCBTatStop` |
| C2-40 | 1951, 1952 U | `Stop` | `outputKBMHandler.GetDisplayName()`, `.Disconnect()` | (a) | N | `_virtualKBM.GetDisplayName()`, `_virtualKBM.Disconnect()` |
| C2-41 | 2095 Q | `PrepareConnectedInput...` | `RefreshExtrasButtons(index, buttons)` | (b) | N | `_profileSettings.RefreshExtrasButtons(index, buttons)` |
| C2-42 | 2096 Q | `PrepareConnectedInput...` | `LoadControllerConfigs(device)` | (b) | N | `_profileXmlStore.LoadControllerConfigsForDevice(device)` |
| C2-43 | 2101, 2661 U | `PrepareConnectedInput...`, `On_DS4Removal` | `isUsingOSCSender()` | (b) | N | `_appSettings.UseOscSender` |
| C2-44 | 2116, 2123, 2141 Q | `PrepareConnectedInput...` | `IsFirstConnection(i)`, `MarkConnected(i)` | (b) | N | `_deviceStateService.IsFirstConnection(i)`, `_deviceStateService.MarkConnected(i)` |
| C2-45 | 2128, 2131, 2154, 2156 U | `PrepareConnectedInput...` | `containsLinkedProfile(s)`, `getLinkedProfile(s)` | (b) | N | `_profileRepository.ContainsLinkedProfile(s)`, `_profileRepository.GetLinkedProfile(s)` |
| C2-46 | 2137, 2146, 2151, 2157, 2162 Q/U | `PrepareConnectedInput...` | `OlderProfilePath[i]`, `SelectedProfile[i]`, `LinkedProfileUI[i]` | (a) | N | `_profileRepository.OlderProfilePath[i]`, `SelectedProfile[i]`, `LinkedProfileUI[i]` |
| C2-47 | 2168 Q | `PrepareConnectedInput...` | `ApplyProfileToSlot(i, ...)` | (b) | N | **O1（未決）**: Step2-2 着手時に方式を決定（§0.3.3）。それまで `Global.ApplyProfileToSlot` は温存 |
| C2-48 | 2174, 3128 U | `PrepareConnectedInput...`, `LagFlashWarning` | `getMainColor(index)` | (a) | N/H条件 | `_profileSettings.GetMainColor(index)` |
| C2-49 | 2257, 2260, 2269, 2275 Q | UDP 平滑化設定 | `UDPServerSmoothingMincutoff`, `UDPServerSmoothingBeta` | (b) | N | `_appSettings.UDPServerSmoothingMincutoff`, `_appSettings.UDPServerSmoothingBeta` |

---

### 表3: 入力ポーリング・ホットパス・出力フィードバック系（ID: C2-50 〜 C2-66、計25参照）

| ID | 行番号・形式 | メソッド／経路 | 参照メンバ | 分類 | ホットパス | 移行先サービス・メンバ／方針 |
|---|---|---|---|---|---|---|
| C2-50 | 1792 Q | `PrepareDevUDPMotion` コールバック | `IsUsingUDPServerSmoothing` | (b) | **H** | `_appSettings.UseUdpServerSmoothing`（Report毎に評価されるためキャッシュ考慮） |
| C2-51 | 2281, 2282, 2285, 2286, 2288, 2302, 2309, 2325 U | `CheckProfileOptions` | `getEnableOutputDataToDS4`, `getIdleDisconnectTimeout`, `getBTPollRate`, `getTrackballFriction`, `getRumbleAutostopTime`, `DualSense*` | (a) | N† | `_profileSettings.GetEnableOutputDataToDS4(i)`, `GetIdleDisconnectTimeout(i)`, `GetBTPollRate(i)`, `GetTrackballFriction(i)`, `GetRumbleAutostopTime(i)`, `DualSense*` |
| C2-52 | 2336 U | `CheckLauchProfileOption` | `LaunchProgram[index]` | (a) | N† | `_profileSettings.LaunchProgram[index]` |
| C2-53 | 2385, 2408, 2417, 2433, 2442 U | フック・設定変更イベント | `WheelSmoothInfo`, `L2OutputSettings`, `R2OutputSettings` | (a) | N | `_profileSettings.WheelSmoothInfo[i]`, `L2OutputSettings[i]`, `R2OutputSettings[i]` |
| C2-54 | 2540 U | `On_SerialChange` | `OnDeviceSerialChange` | (b) | N | `_deviceStateService.OnDeviceSerialChange(sender, i, serial)` |
| C2-55 | 2701 U | `On_Report` | `getFlashWhenLateAt()` | (b) | **H** | `_appSettings.FlashWhenLateAt` |
| C2-56 | 2749, 2753 Q/U | `On_Report`（初回Report時） | `appdatapath`, `ProfilePath[i]`, `store.EmitMissingActionLogsForDevice` | (a) | H条件 | `_pathService.AppDataPath`, `_profileRepository.ProfilePath[i]`, `_profileRepository.EmitMissingActionLogsForDevice(i, false)` |
| C2-57 | 2762, 2764 Q/U | `On_Report` | `UseIconChoice`, `InvokeBatteryChanged` | (a)/(b) | **H** / H条件 | `_appearanceSettings.UseIconChoice`, `_appearanceSettings.InvokeBatteryChanged(byte)` |
| C2-58 | 2775 Q | `On_Report`（非Primary結合時） | `GetGyroOutMode(JointDeviceSlotNumber)` | (a) | H条件 | `_profileSettings.GetGyroOutMode(...)`（旧C2-02） |
| C2-59 | 2780 U | `On_Report` | `outputKBMHandler.Sync()` | (a) | **H** | `_virtualKBM.Sync()` |
| C2-60 | 2788, 2846 U | `On_Report` | `useDInputOnly[index]` | (a) | **H** | `_profileSettings.UseDInputOnlyArray[index]` |
| C2-61 | 2805, 2851 Q/U | `On_Report` | `getEnableTouchToggle(i)`, `GetOutputDS4TriggerMode(i)` | (a) | **H** | `_profileSettings.GetEnableTouchToggle(i)`, `_profileSettings.OutputDS4TriggerMode[i]` |
| C2-62 | 2815, 2816 U | `On_Report` | `containsCustomAction(i)`, `containsCustomExtras(i)`, `getProfileActionCount(i)` | (b) | **H** | `_profileSettings.ContainsCustomAction(i)`, `_profileSettings.ContainsCustomExtras(i)`, `_profileActionProvider.GetProfileActionCount(i)` |
| C2-63 | 2821, 2837, 2849 U | `On_Report` | `isUsingOSCSender()`, `isUsingOSCServer()`, `activeOutDevType[i]` | (a)/(b) | **H** | `_appSettings.UseOscSender`, `_appSettings.UseOscServer`, **D1**: キャッシュ済み `ActiveOutDevType[i]`（毎レポートで `Func` を呼ばない） |
| C2-64 | 2890 Q | `On_Report`（useDInputOnly時） | `GetSASteeringWheelEmulationAxis(index)` | (a) | H条件 | `_profileSettings.GetSASteeringWheelEmulationAxis(index)`（旧C2-03） |
| C2-65 | 3114 U | `LagFlashWarning` | `getFlashWhenLate()` | (b) | H条件 | `_appSettings.FlashWhenLate` |
| C2-66 | 3212, 3214, 3216, 3223, 3237, 3301 U | タッチトグル・ランブルブースト | `IsUsingTouchpadForControls`, `GetTouchActive`, `TouchActive[i]`, `getRumbleBoost` | (a) | **H** / N† | `_profileSettings.TouchOutMode[i] == TouchpadOutMode.Controls`, `_profileSettings.TouchpadActiveArray[i]`, `_profileSettings.GetRumbleBoost(i)` |

---

### 表4: 明示的除外項目（ID: C2-EX01 〜 C2-EX07、計8参照）

| ID | 行番号 | 参照メンバ | 除外理由（根拠） |
|---|---|---|---|
| C2-EX01 | 47 Q | `Global.MAX_DS4_CONTROLLER_COUNT` | 真の定数（`const int`、4層モデル共通値） |
| C2-EX02 | 49, 51 Q | `Global.OLD_XINPUT_CONTROLLER_COUNT` | 真の定数（`const int`） |
| C2-EX03 | 2462 Q | `Global.TEST_PROFILE_INDEX` | 真の定数（`const int`） |
| C2-EX04 | 1055 Q | `Global.RefreshViGEmBusInfo()` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX05 | 1056, 1760 Q | `Global.IsRunningSupportedViGEmBus()` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX06 | 1631, 1762 Q | `Global.vigembusVersion` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |
| C2-EX07 | 1756 U | `vigemInstalled` | Phase6-Plan.md §0.3.1 明示的除外（ViGEm クライアント直接依存） |

---

## 3. アーキテクチャ設計・依存性注入（Pure DI）

### 3.1 コンストラクタ注入の拡張設計（決定D2: 新規引数はすべて必須）
`ControlService` は現在、`ArgumentParser`, `IDs4DeviceRegistry`, `IProfileSettingsService`（省略可能）の3つのみをコンストラクタで受け取っている。  
`copilot-instructions.md` §3.1（サービスロケータを持ち込まない Pure DI）に従い、**新規に追加する依存はすべて必須引数**とし、`AppHost.GetService<T>()` によるフォールバックは設けない。`new ControlService(` の呼び出し元は `ServiceRegistration` のファクトリ1箇所のみであるため、既存呼び出し元への影響は無い。

```csharp
// 拡張後の ControlService コンストラクタ設計（namespace は実装時にコンパイルで確認する）
public ControlService(
    DS4WinWPF.ArgumentParser cmdParser,
    IDs4DeviceRegistry deviceRegistry,
    DI.IProfileSettingsService profileSettings,        // 既存。省略可能を外す（下記「引数順」参照）
    DI.IAppSettingsService appSettings,
    DI.IEnvironmentService environmentService,
    DI.IPathService pathService,
    Func<DI.IOutputSlotService> outputSlotServiceFactory, // D1: 遅延解決（§3.2）
    Services.IVirtualKBM virtualKBM,                       // DS4Windows.Services（DI ではない）
    Services.IVirtualKBMLifecycle kbmLifecycle,            // D3: 新規（§4.2）
    DI.IProfileRepository profileRepository,
    DI.IDeviceStateService deviceStateService,
    DI.IAppearanceSettingsService appearanceSettings,
    DI.IProfileXmlStore profileXmlStore,
    DI.IProfileActionProvider profileActionProvider)
{
    this.cmdParser = cmdParser;
    this._deviceRegistry = deviceRegistry;
    // 過渡期の防御コード（TODO: Phase6-Step12 で削除判断。DI 経由の生成では常に非null）
    this._profileSettings = profileSettings ?? Global.ProfileSettingsServiceInstance;
    this._appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    this._environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
    this._pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    this._outputSlotServiceFactory = outputSlotServiceFactory ?? throw new ArgumentNullException(nameof(outputSlotServiceFactory));
    this._virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
    this._kbmLifecycle = kbmLifecycle ?? throw new ArgumentNullException(nameof(kbmLifecycle));
    // 以下同様（profileRepository, deviceStateService, appearanceSettings, profileXmlStore, profileActionProvider）
    ...
}
```

- **引数順**: C# では省略可能引数を必須引数より前に置けない。既存の `profileSettings = null` は省略可能を外して必須にし（本文内の `?? Global.ProfileSettingsServiceInstance` は残す）、新規引数を後ろに追加する。
- **段階導入**: 引数は各PRで必要になった分だけ追加する（PR-1: `appSettings`, `environmentService`, `pathService`／PR-2: `outputSlotServiceFactory`, `profileRepository`, `deviceStateService`, `profileXmlStore`／PR-3: `virtualKBM`, `kbmLifecycle`／PR-5: `appearanceSettings`, `profileActionProvider`）。各PRで `ServiceRegistration` のファクトリも同時に更新する。
- **`ServiceRegistration` の更新**: ファクトリ内で `sp.GetRequiredService<T>()` により全引数を解決する。`Func<IOutputSlotService>` は `() => sp.GetRequiredService<IOutputSlotService>()` を渡す。新規 `IVirtualKBMLifecycle` は `IVirtualKBM` の登録の隣に `AddSingleton<IVirtualKBMLifecycle, OutputKBMHandlerLifecycle>()` として登録する。
- **利点**: 循環依存や未登録サービスが、実行時の分岐ではなくコンテナ構築（起動）時に顕在化する。

### 3.2 循環依存の回避設計（決定D1 / 未決O1）
- **現状の課題（2026-09-19 実地確認）**:
  - `OutputSlotService` のコンストラクタは `_control = control ?? Program.rootHub` で `ControlService` に依存し、`_slotManager` も `_control?.OutputslotMan` から取得する。`ControlService` の生成中に `IOutputSlotService` を解決すると、その時点で `Program.rootHub` は未代入（代入は生成後の `App.xaml.cs:724`）のため、`_control` が null のまま Singleton が固定され、別の `OutputSlotManager` が作られる（二重実体化）恐れがある。
  - `ProfileApplicationService` も `ControlService` に依存し、`control ?? AppHost.GetService<ControlService>()` を実行するため、`ControlService` の生成中に解決すると再帰生成の恐れがある。
  - モデル図（03 / 04）では、`IOutputSlotService` は `IOutputSlotStore` のみ、`IProfileApplicationService` は `IProfileRepository` と `IOutputSlotService` のみに依存する。現状の逆依存は**過渡期の技術的負債**であり、モデル図の目標構造は変更しない。
- **決定D1（`IOutputSlotService`）**:
  - `ControlService` は `Func<IOutputSlotService> _outputSlotServiceFactory` を保持し、`IOutputSlotService` を初回使用時に解決する。**コンストラクタ内で `Func` を呼ばない**（単体テストで検証する）。
  - ホットパス（C2-63）では `Func` を毎回呼ばない。`ActiveOutDevType` の配列参照を初回に1回だけフィールドへキャッシュし、以降は配列を直接参照する（`Global.activeOutDevType` は再代入されないことを実地確認済み）。`null` 合体による1回の分岐のみでアロケーションは発生しない。

```csharp
private OutContType[] _activeOutDevType;
private OutContType[] ActiveOutDevType
    => _activeOutDevType ?? (_activeOutDevType = _outputSlotServiceFactory().ActiveOutDevType);
```

  - 技術的負債コメント（copilot-instructions §3.3 原則4）を `_outputSlotServiceFactory` に付与する: 「OutputSlotService → ControlService の逆依存を回避する過渡期措置。Phase6-Step5 で逆依存を除去し、直接注入へ戻す」。
- **未決O1（`IProfileApplicationService`、C2-47）**: `ControlService` には `IProfileApplicationService` を直接注入しない。方式は Step2-2 着手時に、メリット・デメリット・推奨を提示して決定する。候補は (A) `Func<IProfileApplicationService>` の遅延解決、(B) 適用要求イベントによる上位への委譲、(C) `IProfileRepository.LoadProfileToSlot` 等の低レベルAPI（`Global.ApplyProfileToSlot` が担うログ組立・`ProfileChangeSource`・Halt 保護との等価性の確認が必要）。決定までは `Global.ApplyProfileToSlot` を温存する。

---

## 4. 分類(b)・分類(c) のサービス拡張・設計方針

### 4.1 分類(b) 既存サービスへの軽微拡張一覧
各DIサービスに、以下の薄いシム（`BackingStore` / `Global` 委譲）を追加する。独自フィールドは一切保持せず、SSOT（信頼できる唯一の情報源）を徹底する。

1. **`IAppSettingsService` / `AppSettingsService.cs`**:
   - `bool FlashWhenLate { get; set; }` → `Global.getFlashWhenLate()` / `Global.setFlashWhenLate(value)`
   - `int FlashWhenLateAt { get; set; }` → `Global.getFlashWhenLateAt()` / `Global.setFlashWhenLateAt(value)`
   - `bool QuickCharge { get; set; }` → `Global.getQuickCharge()` / `Global.setQuickCharge(value)`
   - `bool DCBTatStop { get; set; }` → `Global.DCBTatStop`
   - `int ProcessPriority { get; set; }` → `Global.ProcessPriority`（`MainWindow.ProcessPriorityClasses` の添字。旧記載の `ProcessPriorityClass` 型は誤り）
   - `int OscServerPort { get; set; }` → `Global.getOSCServerPortNum()`
   - `string OscSenderAddress { get; set; }` → `Global.getOSCSenderAddress()`
   - `int OscSenderPort { get; set; }` → `Global.getOSCSenderPortNum()`
   - `bool UseOscServer { get; set; }` → `Global.isUsingOSCServer()`
   - `bool UseOscSender { get; set; }` → `Global.isUsingOSCSender()`
   - `bool InterpretingOscMonitoring { get; set; }` → `Global.isInterpretingOscMonitoring()`
   - `bool UseUdpServerSmoothing { get; set; }` → `Global.IsUsingUDPServerSmoothing`
   - `double UDPServerSmoothingMincutoff { get; set; }` → `Global.UDPServerSmoothingMincutoff`（旧記載の `float` は誤り。実型は `double`）
   - `double UDPServerSmoothingBeta { get; set; }` → `Global.UDPServerSmoothingBeta`（同上）
   - `event EventHandler UDPServerSmoothingMincutoffChanged;`
   - `event EventHandler UDPServerSmoothingBetaChanged;`
   - `ControlServiceDeviceOptions DeviceOptions { get; }` → `Global.DeviceOptions`

2. **`IEnvironmentService` / `EnvironmentService.cs`**:
   - `bool IsWin8OrGreater()` → `Global.IsWin8OrGreater()`（O2 決定後に追加）
   - `void PrepareAbsMonitorBounds(string edid)` → `Global.PrepareAbsMonitorBounds(edid)`（PR-1 で追加、C2-06）
   - `bool HidHideInstalled { get; }` → `Global.hidHideInstalled`
   - `string GetInstanceIdFromDevicePath(string devicePath)` → `Global.GetInstanceIdFromDevicePath(devicePath)`
   - `bool CheckHidHideAffectedStatus(string instanceId, HashSet<string> affected, HashSet<string> exempted, bool forced)` → `Global.CheckHidHideAffectedStatus(...)`
   - `string FakerInputVersion { get; }` → `Global.fakerInputVersion`

3. **`IDeviceStateService` / `DeviceStateService.cs`**:
   - `bool IsFirstConnection(int deviceIndex)` → `Global.IsFirstConnection(deviceIndex)`
   - `void MarkConnected(int deviceIndex)` → `Global.MarkConnected(deviceIndex)`
   - `void OnDeviceSerialChange(DS4Device sender, int index, string serial)` → `Global.OnDeviceSerialChange(sender, index, serial)`

4. **`IProfileRepository` / `ProfileRepository.cs`**:
   - `bool ContainsLinkedProfile(string serial)` → `Global.containsLinkedProfile(serial)`
   - `string GetLinkedProfile(string serial)` → `Global.getLinkedProfile(serial)`

5. **`IProfileSettingsService` / `ProfileSettingsService.cs`**:
   - `void RefreshExtrasButtons(int deviceIndex, List<DS4Controls> buttons)` → `Global.RefreshExtrasButtons(deviceIndex, buttons)`
   - `bool ContainsCustomAction(int deviceIndex)` → `Global.containsCustomAction(deviceIndex)`
   - `bool ContainsCustomExtras(int deviceIndex)` → `Global.containsCustomExtras(deviceIndex)`

6. **`IProfileXmlStore` / `ProfileXmlStore.cs`**:
   - `void LoadControllerConfigsForDevice(DS4Device device)` → `Global.LoadControllerConfigs(device)`

7. **`IAppearanceSettingsService` / `AppearanceSettingsService.cs`**:
   - `void InvokeBatteryChanged(byte batteryLevel)` → `Global.InvokeBatteryChanged(batteryLevel)`

8. **`IProfileActionProvider` / `ProfileActionProvider.cs`**:
   - `int GetProfileActionCount(int deviceIndex)` → `Global.getProfileActionCount(deviceIndex)`

### 4.2 分類(c) ライフサイクル・座標変換の整理
- **KBM ハンドラ初期化（ID: C2-09, C2-12, C2-14, C2-16, C2-18）— 決定D3**:
  `RefreshOutputKBMHandler` / `InitOutputKBMHandler` は、ハンドラの生成・破棄・フォールバック交換・バージョン設定・マッピング初期化が密結合している。一方 `OutputKBMHandlerAdapter`（`IVirtualKBM`）は `Global.outputKBMHandler` への読み取り専用委譲であり、ハンドラの生成・差し替えの口を持たない。
  そこで、送出責務（`IVirtualKBM`、変更しない）とライフサイクル責務を分離し、**新規 `IVirtualKBMLifecycle`** を追加する（`DS4Windows/DS4Control/Services/IVirtualKBMLifecycle.cs`、namespace `DS4Windows.Services`、1ファイル1型）。実装 `OutputKBMHandlerLifecycle`（コンストラクタ引数: `IEnvironmentService`）は、現行の `Global` 側処理へ薄く委譲し、挙動・ログ（`Global.InitOutputKBMHandler` 内の `AppLogger.LogDebug`）を100%維持する。

```csharp
namespace DS4Windows.Services
{
    /// <summary>KBM出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化（送出は IVirtualKBM の責務）。</summary>
    public interface IVirtualKBMLifecycle
    {
        void ReleaseHandler();                     // null でなければ Disconnect し、ハンドラを null にする
        void DetermineHandler(string identifier);  // Global.InitOutputKBMHandler(identifier) へ委譲
        void SwitchToFallbackHandler();            // ハンドラを VirtualKBMFactory.GetFallbackHandler() へ差し替え
        void ApplyFakerInputVersion();             // ハンドラの Version へ IEnvironmentService.FakerInputVersion を設定
        void InitializeMapping(string identifier); // Global.InitOutputKBMMapping(identifier) へ委譲
    }
}
```

  `ControlService` 側の対応（旧 → 新）:

| 旧（現行HEADの行） | 新 |
|---|---|
| `if (Global.outputKBMHandler != null) { Disconnect(); = null; }`（488〜491） | `_kbmLifecycle.ReleaseHandler();` |
| `Global.outputKBMMapping = null`（494〜496） | `_profileSettings.OutputKBMMapping = null;`（null 判定は維持） |
| `Global.InitOutputKBMHandler(id)`（505） | `_kbmLifecycle.DetermineHandler(id);` |
| `try { Global.outputKBMHandler.Connect() } catch { }`（510） | `try { handlerConnected = _virtualKBM.Connect(); } catch { }`（try/catch は維持） |
| `Global.outputKBMHandler = VirtualKBMFactory.GetFallbackHandler()`（517） | `_kbmLifecycle.SwitchToFallbackHandler();`（分岐条件は不変） |
| `outputKBMHandler.GetIdentifier() == FakerInputHandler.IDENTIFIER`（522、無修飾参照） | `_virtualKBM.GetIdentifier() == FakerInputHandler.IDENTIFIER` |
| `Global.outputKBMHandler.Version = Global.fakerInputVersion`（524） | `_kbmLifecycle.ApplyFakerInputVersion();` |
| `Global.InitOutputKBMMapping(Global.outputKBMHandler.GetIdentifier())`（528） | `_kbmLifecycle.InitializeMapping(_virtualKBM.GetIdentifier());` |
| `Global.outputKBMMapping.PopulateConstants()/PopulateMappings()`（529〜530） | `_profileSettings.OutputKBMMapping.PopulateConstants()/PopulateMappings()` |

  - **技術的負債コメント**: `OutputKBMHandlerLifecycle` 内の `Global.outputKBMHandler` への直接アクセスには、copilot-instructions §3.3 原則4 に従い、TODO コメント（理由: ハンドラ実体が `Global` の static フィールドに残っているため／最終対応: `Global` 撤去フェーズ〈Phase7〉）を付与する。
  - **既知の挙動差**: `Start()` 内（旧1634, 1640行）の `Global.outputKBMHandler.GetIdentifier()` / `GetFullDisplayName()` を `_virtualKBM` 経由に替えると、ハンドラが null の場合の結果が「NullReferenceException」から「空文字」に変わる。`InitOutputKBMHandler` が `Start()` より前に必ず実行されるため通常は発生しないが、PR-3 の報告書に明記する。
- **画面解像度変更時の座標系更新（ID: C2-06）**:
  `SystemEvents_DisplaySettingsChanged` から呼ばれる `Global.PrepareAbsMonitorBounds(string.Empty)` は、マウス絶対座標計算のためのWin32モニター列挙処理である。過渡期は薄いシムメソッドを経由させ、Phase6-Step4（Mouse系移行）と連動して最終的なサービス抽象化を行う。

---

## 5. ホットパス性能維持とアーキテクチャ・ガードレール

### 5.1 ホットパス（毎秒250〜1000回）の性能保護原則
1. **ゼロアロケーション（Zero Allocation）**:
   `On_Report`、`LagFlashWarning`、`CheckForTouchToggle` 等の高頻度実行経路上では、GC プレッシャーを避けるため、配列の `.Clone()`、`new` によるオブジェクト生成、および LINQ の使用を厳禁とする。
2. **BackingStore 直列配列参照の維持**:
   `_profileSettings.UseDInputOnlyArray[index]` や `_profileSettings.LSModInfo[index]` などの配列アクセスは、内部キャッシュを経由せず、BackingStore の固定配列を直接参照する getter を介して行われる。余分なディクショナリ検索や動的解決を挟まない。
3. **ホットパス内のログ出力厳禁**:
   高頻度経路上に常時出力される `[DI]` 追跡ログ、`Debug.WriteLine`、例外スローを持ち込まない。
4. **設定値の都度サービス解決の排除**:
   コンストラクタで受け取った readonly フィールド（`_profileSettings`, `_virtualKBM` 等）を直接参照し、`AppHost.GetService<T>()` をホットパス内で絶対に呼ばない。

### 5.2 アーキテクチャ・ガードレールの順守（Phase5 §5 継承）
1. **プロファイル適用時の Halt 停止保証**:
   プロファイル適用や出力デバイス変更を伴う操作では、入力ポーリングスレッドの停止（Halt）と排他制御を確実に維持する。
2. **スレッド直列化**:
   `eventDispatchThread` および `eventDispatcher` によるイベント直列化キューの順序を乱さない。
3. **切断時クリーンアップ**:
   コントローラー切断時（`On_DS4Removal`）およびサービス停止時（`Stop`, `ShutDown`）における、仮想デバイスアンプラグおよび状態リセットを完全に維持する。

---

## 6. PR分割計画とマイクロステップ

変更規模（実移行対象66箇所）と安全性を考慮し、以下の**6つのサブPR（マイクロステップ）**に分割して段階的に実装を進める。各PRごとに `dotnet test`（Actions / Standalone）を実行し、全件グリーンを確認してから次へ進む。

```text
【Phase6-Step2 マイクロステップ構成】
├─ Step2-0: 着手前提検証・ベースライン記録（実装なし）
├─ Step2-1 (PR-1): 非ホットパスの環境・パス・基本設定（ID: C2-03〜08, 21〜27, 32, 34〜39）※C2-01=O2決定待ち、C2-02=D2温存、C2-33/40=PR-3
├─ Step2-2 (PR-2): 出力スロット・プロファイル状態・接続イベント（ID: C2-28〜31, 41〜49）[20参照]
├─ Step2-3 (PR-3): KBMハンドラ初期化・ライフサイクル整理（ID: C2-09〜20, 33, 40）
├─ Step2-4 (PR-4): 入力処理ホットパス（条件付き・低頻度分岐）（ID: C2-51〜54, 56, 58, 64〜65）[14参照]
├─ Step2-5 (PR-5): 入力処理ホットパス（毎レポート最頻度経路）（ID: C2-50, 55, 57, 59〜63, 66）[18参照]
└─ Step2-6 (PR-6): using static DS4Windows.Global; 削除・未修飾参照ゼロ検証・最終クリーンアップ
```

---

### Step2-0: 着手前提検証・ベースライン記録（実装なし）
1. 作業ツリーの状態、HEADコミットハッシュ、未コミット差分ゼロを確認する。**（2026-09-19 実施済み: HEAD `00080d6`、差分なし。§0.3.1）**
2. 現行コードと計画書の突き合わせ（実地確認の原則、`DI-App-Wide-Migration-Plan.md` §6.11）を行い、結果を §0.3.1 と付録A に記録する。**（2026-09-19 実施済み）**
3. ソリューション全体のビルド（`dotnet build -c Release`）および既存テスト（`dotnet test`）を実行し、ベースライン結果（全テストパス、警告ゼロ）を記録する。**（未実施: 開発環境で実施し、結果を報告する）**
4. 各PR着手時の手順: 対象メンバを grep して現行HEADの行番号台帳を作成し、PR報告書に添付する。無修飾参照（`using static` 由来）の網羅は PR-6 でコンパイラ（`using static` 削除後の CS0103 一覧）により最終証明する。

---

### Step2-1 (PR-1): 非ホットパスの環境・パス・基本設定
- **対象**: ID: C2-03〜C2-08, C2-21〜C2-27, C2-32, C2-34〜C2-39（初期化、OSC/UDP設定、管理者権限、HidHide、Start/Stop）
- **対象外（2026-09-19 是正）**: C2-01（O2: 静的初期化のため決定待ち）、C2-02（D2: 防御コードとして温存）、C2-33・C2-40（`IVirtualKBM` を使うため、注入を行う PR-3 で実施）。旧計画では C2-06〜C2-08 がどの PR にも割り当てられていなかったため、PR-1 に含めた
- **作業内容**:
  1. `IAppSettingsService` および `IEnvironmentService` に必要なプロパティ・メソッドを追加（§4.1）。
  2. `ControlService` コンストラクタに `IAppSettingsService`, `IEnvironmentService`, `IPathService` を**必須引数として**追加（D2）。あわせて `profileSettings` の省略可能を外し、`ServiceRegistration` のファクトリを更新する。
  3. 対象箇所の `Global.*` を注入インスタンス参照へ置換。
- **検証**:
  - 新規単体テスト: 追加したシムプロパティが `Global` / `BackingStore` と同期していることを検証。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-1b (PR-1b): C2-01 コントローラースロット上限のサービス化（決定O2=C）
- **上限の意味**: `CURRENT_DS4_CONTROLLER_LIMIT` は「同時に扱えるスロット数の**上限**」（Windows 8 以上で 8、未満で 4。ビルド定義 `FORCE_4_INPUT` で 4 固定）であり、現在接続中のコントローラー台数ではない。`USING_MAX_CONTROLLERS` は上限が 8 かどうかの派生値。旧実装は `public static` フィールドの初期化子で `Global.IsWin8OrGreater()` を呼んでいたため、インスタンスを注入できなかった。
- **作業内容（実装済み）**:
  1. `IEnvironmentService` に `int ControllerSlotLimit { get; }` と `bool UsingMaxControllers { get; }` を追加。計算（OS 判定＋`FORCE_4_INPUT`）は `EnvironmentService` へ移設し、値はプロセス内で不変のため `internal static readonly` で1回だけ確定する。
  2. `ControlService` 内部の5箇所（`Start`／`HotPlug`／`On_SyncChange`／`StartTPOff`／`setRumble`）は、コンストラクタで取得した `_controllerSlotLimit`（int フィールド）を使用する。`setRumble` は頻繁に呼ばれるため、インターフェース呼び出しを避けてキャッシュ値を使う。
  3. `CURRENT_DS4_CONTROLLER_LIMIT` / `USING_MAX_CONTROLLERS` は、外部54箇所が未移行のため static プロパティの互換シムとして残す。値は `EnvironmentService` と同一の静的値を共有し、技術的負債コメント（理由・正規参照先・撤去予定 Step12）を付与した。
- **互換シムを `Global.EnvironmentServiceInstance` 経由にしなかった理由**: 同プロパティは DI が未構築の場面でフォールバック生成と GUI ログ（`[Legacy] ... Fallback instance used`）を毎回出す。上限は `for` ループの条件式で毎回評価されるため、テストや起動初期にログが大量に出る恐れがある。また `ControlService` の型初期化中に `AppHost` を呼ぶと、DI 構築中の再入になる。
- **検証**: `ControllerSlotLimitTests`（旧計算式との一致、範囲、`UsingMaxControllers` の判定、`EXPANDED_CONTROLLER_COUNT` と `Global.MAX_DS4_CONTROLLER_COUNT` の一致、互換シムとの値共有）。
- **残る移行対象（外部54箇所・15ファイル）と担当案（O3 承認後に各 Step 計画書へ反映）**:

| 呼び出し元 | 箇所数 | 担当案 | 置換方法 |
|---|---:|---|---|
| `OutputSlotManager.cs` | 3 | Step5 | コンストラクタで上限を受け取る（引数なしのコンストラクタは互換のため残す） |
| `Services/ProfileSettingsService.cs` | 4 | PR-1c（提案） | `IEnvironmentService` をコンストラクタ注入（既存テストの `new ProfileSettingsService()` の修正を伴う） |
| `Services/AutoProfileService.cs` | 2 | PR-1c（提案） | 同上（`AutoProfileChecker` と `AutoProfileServiceTests` の生成箇所の修正を伴う） |
| `ScpUtil.cs`（`Global` 内部の static メソッド） | 8 | Step12 | `Global.EnvironmentServiceInstance` 経由へ（Global 内部のためシム扱い） |
| `DTOXml/AutoProfilesDTO.cs` | 2 | Step10 | 引数渡し |
| `ProfileSettingsViewModel.cs` | 10 | Step9 | コンストラクタ注入（`IViewModelFactory` 経由） |
| `ProfileEditor.xaml.cs` | 8 | Step8 | `ProfileSettingsViewModel` へ移設するロジックと同時に置換 |
| `MainWindow.xaml.cs` | 2 | Step8 | 同上（MainWindow 解体推進の一環） |
| `AutoProfilesViewModel.cs`／`AutoProfiles.xaml.cs` | 2／2 | Step10 | コンストラクタ注入／`AppHost` 経由 |
| `RecordBoxViewModel.cs`／`BindingWindowViewModel.cs`／`BindingWindow.xaml.cs` | 3／3／1 | Step10 | 同上 |
| `SpecialActions/CheckBatteryViewModel.cs`／`CurrentOutDeviceViewModel.cs` | 3／1 | Step10 | 同上 |

---

### Step2-2 (PR-2): 出力スロット・プロファイル状態・接続イベント
- **対象**: ID: C2-28〜C2-31, C2-41〜C2-49（`PluginOutDev`, `useDInputOnly`, `activeOutDevType`, `PrepareConnectedInputControllerSettingEvents`）
- **着手時に決定**: O1（C2-47 `ApplyProfileToSlot` の方式）。メリット・デメリット・推奨を提示して承認を得てから実装する。
- **作業内容**:
  1. `IDeviceStateService`, `IProfileRepository`, `IProfileXmlStore`, `IProfileSettingsService` に必要なシムを追加（§4.1）。
  2. `ControlService` コンストラクタに `Func<IOutputSlotService>`（D1）, `IProfileRepository`, `IDeviceStateService`, `IProfileXmlStore` を必須引数として追加。`ActiveOutDevType` 配列参照のキャッシュを実装する（§3.2）。
  3. C2-28（旧C2-01: `Global.OutContType` → `_profileSettings.OutContType`）を含む対象箇所を置換。
  4. C2-47 は O1 の決定に従う。
- **検証**:
  - 単体テスト: スロット別の `OutContType`, `useDInputOnly`, `activeOutDevType` の読み書き分離を検証。
  - 単体テスト（D1）: コンストラクタ内で `Func<IOutputSlotService>` が呼ばれないこと。実コンテナで `ControlService` を解決しても `OutputSlotService` の `_control` が非nullで、`OutputSlotManager` が二重に作られないこと。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-3 (PR-3): KBMハンドラ初期化・ライフサイクル整理
- **対象**: ID: C2-09〜C2-20（`RefreshOutputKBMHandler`, `InitOutputKBMHandler`）＋ C2-33, C2-40（`Start` / `Stop` 内の KBM 識別子・表示名参照）
- **作業内容**（決定D3）:
  1. 新規 `IVirtualKBMLifecycle.cs` / `OutputKBMHandlerLifecycle.cs` を追加（1ファイル1型、§4.2）し、`ServiceRegistration` に登録。
  2. `ControlService` コンストラクタに `IVirtualKBM`, `IVirtualKBMLifecycle` を必須引数として追加。
  3. §4.2 の対応表に従い `RefreshOutputKBMHandler` / `InitOutputKBMHandler` を置換。`Global.outputKBMMapping` は `_profileSettings.OutputKBMMapping` へ置換。
  4. ハンドラ交換時の例外保護（`try/catch`）・フォールバック切替の分岐条件・処理順序を維持。
- **検証**:
  - 新規単体テスト: `VirtualKBMLifecycleTests.cs`（`ReleaseHandler` の null 安全性、フォールバック切替、`ApplyFakerInputVersion` が Faker ハンドラのみで呼ばれること、マッピング初期化）。
  - 回帰テスト: `VirtualKBMTests.cs` および `dotnet test` 全件合格を確認。
  - 実機: KBM 出力の接続・フォールバック（FakerInput 未導入環境）・再初期化（`RefreshOutputKBMHandler`）の動作確認。

---

### Step2-4 (PR-4): 入力処理ホットパス（条件付き・低頻度分岐）
- **対象**: ID: C2-51〜C2-54, C2-56, C2-58, C2-64〜C2-65（初回Report時ログ、結合ジャイロ、ステアリング軸、オプションチェック）
- **作業内容**:
  1. C2-58（旧C2-02: `Global.GetGyroOutMode` → `_profileSettings.GetGyroOutMode`）を置換。
  2. C2-64（旧C2-03: `Global.GetSASteeringWheelEmulationAxis` → `_profileSettings.GetSASteeringWheelEmulationAxis`）を置換。
  3. 初回Report時のプロファイル欠落ログ出力（`EmitMissingActionLogsForDevice`）を置換。
- **検証**:
  - 単体テスト: 結合スロットでのジャイロモード取得、ステアリング軸取得の独立性を検証。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-5 (PR-5): 入力処理ホットパス（毎レポート最頻度経路）
- **対象**: ID: C2-50, C2-55, C2-57, C2-59〜C2-63, C2-66（`On_Report` 最頻度経路、タッチトグル、KBM Sync、遅延警告）
- **作業内容**:
  1. `IAppearanceSettingsService`, `IProfileActionProvider` をコンストラクタへ必須引数として追加（D2）し、`ServiceRegistration` のファクトリを更新。
  2. `On_Report` 内の残存 `Global` 参照（`useDInputOnly`, `Sync()`, `TouchActive`, `containsCustomAction` 等）を一挙に置換。
  3. 新規オブジェクト割り当て・クローンが一切発生していないことをコードレビューで厳格確認。
- **検証**:
  - ベンチマーク: `On_Report` 処理時間のプロファイル計測（置換前後の処理時間比較）。
  - 回帰テスト: `dotnet test` 全件合格を確認。

---

### Step2-6 (PR-6): `using static DS4Windows.Global;` 削除・最終クリーンアップ
- **対象**: `ControlService.cs` 冒頭の `using static DS4Windows.Global;` ディレクティブ
- **作業内容**:
  1. `using static DS4Windows.Global;` を削除する。
  2. コンパイルを通し、意図しない無修飾 Global 参照が残存していないことを Roslyn コンパイラで完全に検出・解消する。
  3. 表4の明示的除外（`MAX_DS4_CONTROLLER_COUNT` 等の const、ViGEmバックエンド）のみが `Global.` 修飾で残っていることを確認する。
  4. `ServiceRegistration.cs` の `ControlService` 解決ファクトリが、全引数（D1の `Func<IOutputSlotService>`、D3の `IVirtualKBMLifecycle` を含む）を解決していることを最終確認する（更新自体は各PRで実施済み）。
- **検証**:
  - 静的解析: `ControlService.cs` 内の `Global.` 出現箇所が除外リスト（表4の8件）と完全一致することを確認。
  - 回帰テスト: 全テストプロジェクトの完全ビルド・実行でグリーンを確認。

---

## 7. テスト・性能・実機検証計画

### 7.1 自動単体テスト計画
`DS4WindowsTests` に以下のテストケースを追加する（1ファイル1型を厳守）：
1. **`ControlServiceDiWiringTests.cs`**:
   - `ControlService` が全依存サービスを受け取って正常にインスタンス化されること。
   - `ServiceRegistration` 経由の DI 解決で `ControlService` が Singleton として正しく解決され、循環依存・未登録サービスによる例外が発生しないこと（D2）。
2. **`ControlServiceShimEquivalenceTests.cs`**:
   - 各サービスに追加された新規シム（`FlashWhenLate`, `QuickCharge`, `IsFirstConnection` 等）が、`Global` / `BackingStore` と完全に値を共有していること。
   - スロットインデックス（0〜3）の分離が正しく機能していること。
3. **`ControlServiceHotPathAllocationTests.cs`**:
   - `On_Report` で呼び出される `_profileSettings` の各 getter がヒープ割り当て（GC Alloc）を行わないことの検証。
4. **`VirtualKBMLifecycleTests.cs`**（D3、PR-3）:
   - `ReleaseHandler` / `SwitchToFallbackHandler` / `ApplyFakerInputVersion` / `InitializeMapping` の挙動（null 安全性、Faker ハンドラ限定のバージョン設定）が現行の `RefreshOutputKBMHandler` / `InitOutputKBMHandler` と等価であること。
5. **`ControlServiceOutputSlotResolutionTests.cs`**（D1、PR-2）:
   - `ControlService` のコンストラクタ内で `Func<IOutputSlotService>` が呼ばれないこと。
   - 実コンテナ経由で解決した `OutputSlotService` の `_control` が非nullで、`ControlService.OutputslotMan` と同一の `OutputSlotManager` を共有していること。

### 7.2 ホットパス性能比較（マイクロベンチマーク）
- **測定対象**: `On_Report` の1回あたりの平均処理時間（μs）および p95/p99 レイテンシ。
- **測定条件**:
  - Release x64 ビルド。
  - 同一PC・同一コントローラー（有線USB接続、1000Hz ポーリング）。
  - ウォームアップ後、同一操作シナリオ（左右スティック全開旋回＋ボタン連打）で 100,000 レポート分の処理時間を計測。
- **合否判定基準**:
  - 置換前後の平均処理時間差が ±5% 以内（統計的有意差なし）であること。
  - レポート処理中の Gen0 GC 発生回数が 0 回（完全ゼロアロケーション）であること。

### 7.3 実機動作検証項目（Phase6-Step11連携）
1. **コントローラー接続・切断・再接続**:
   - DS4 / DualSense / Vader 4 Pro コントローラーを有線および Bluetooth で接続し、正常に認識・仮想デバイスプラグインされること。
2. **プロファイル動的切替**:
   - プロファイル切替時に出力デバイス種別（Xbox 360 ⇄ DS4）が正しく反映され、入力遅延・フリーズが発生しないこと。
3. **タッチパッド・ジャイロ・ステアリング操作**:
   - タッチパッドによるマウス操作・トグル切り替えが正常に動作すること。
   - ジャイロによるマウス操作およびステアリングホイールエミュレーションが滑らかに動作すること。

---

## 8. ロールバック方針

万が一、特定のサブPRで入力遅延の悪化、デッドロック、または既存機能の破損が確認された場合は、以下の手順で直ちにロールバックを行う：
1. **サブPR単位の即時差し戻し**: 影響を受けたPRのみを `git revert` し、直前の健全な状態に復旧する。
2. **シムの温存**: 既存の静的メンバ（`Global.*`）はすべて温存されているため、読み取り式を旧参照に戻すだけで完全に元の動作に戻すことが可能。
3. **原因の単離と再設計**: 性能劣化の原因（仮想呼び出しオーバーヘッドか、意図せぬアロケーションか）をベンチマークログから特定し、計画書を補正した上で再適用する。

---

## 9. 完了判定チェックリスト

- [ ] 表1〜表3に定義された全66箇所の実移行対象が、DIサービス経由の呼び出しへ置換されていること。
- [ ] 表4に定義された明示的除外項目（const定数3件、ViGEmバックエンド5件、計8参照）以外の `Global.` 直接参照が `ControlService.cs` から完全に根絶されていること。
- [ ] `ControlService.cs` 冒頭の `using static DS4Windows.Global;` ディレクティブが安全に削除されていること。
- [ ] `ControlService.cs` に例外として残る `Global` 参照が、C2-02（D2: 防御コード）のみであり、技術的負債コメントで理由が記録されていること。
- [ ] `CURRENT_DS4_CONTROLLER_LIMIT` / `USING_MAX_CONTROLLERS` の外部呼び出し元が、O3 で決定した方式に従い移行され、Phase6-Step12 で互換シムが削除されること。
- [ ] コンストラクタ注入において、`ProfileApplicationService` および `OutputSlotService` との循環依存が発生していないこと（D1、O1 の決定内容に従うこと）。
- [ ] `ControlService` のコンストラクタに `AppHost.GetService` フォールバックが新設されておらず、新規引数がすべて必須であること（D2）。
- [ ] `IVirtualKBMLifecycle` / `OutputKBMHandlerLifecycle` が1ファイル1型で追加され、`Global.outputKBMHandler` への直接アクセスに技術的負債コメントが付与されていること（D3）。
- [ ] `Model-Diagram/01〜04` が実装結果と一致していること（差異が生じた場合は変更を指摘して指示を仰ぐ）。
- [ ] 追加されたすべての新規DIシムメンバが、正本（`BackingStore` / `Global`）と連動する薄い委譲として実装されていること。
- [ ] ホットパスにおいて、ゼロアロケーション（GC Alloc = 0）および遅延劣化なしがベンチマークで確認されていること。
- [ ] `dotnet build -c Release` で警告・エラーが0件であること。
- [ ] `DS4WindowsTests` および `StandaloneTests` の全自動テストが100%成功を維持していること。
- [ ] 実機コントローラーによる接続・切断・入力追従・プロファイル切替が正常に動作すること。

---

## 付録A: 現行HEAD（`00080d6`）の修飾 `Global.` 参照台帳（`ControlService.cs`、コメント行を除く）

grep による機械抽出（メンバ別・行番号）。無修飾参照（`using static` 由来）は含まない（PR-6 のコンパイラ検出で網羅を証明する）。**行番号は本書時点の値**で、PR着手時に再抽出すること。

| メンバ | 行番号 | 備考 |
|---|---|---|
| `MAX_DS4_CONTROLLER_COUNT` | 47 | 除外（C2-EX01、const） |
| `OLD_XINPUT_CONTROLLER_COUNT` | 49, 51 | 除外（C2-EX02、const） |
| `IsWin8OrGreater` | 51 | C2-01 |
| `ProfileSettingsServiceInstance` | 214 | C2-02（コンストラクタの防御コードとして残す） |
| `DeviceOptions` | 252 | C2-03 |
| `UDPServerSmoothingMincutoffChanged` / `UDPServerSmoothingBetaChanged` | 260 / 261 | C2-04 / C2-05 |
| `PrepareAbsMonitorBounds` | 272 | C2-06 |
| `isInterpretingOscMonitoring` | 342 | C2-07 |
| `outputKBMHandler` | 488, 490, 491, 510, 517, 524, 528, 1634, 1640 | C2-09/10/13/14/15/16/33（D3） |
| `outputKBMMapping` | 494, 496, 529, 530 | C2-11/19/20 |
| `InitOutputKBMHandler` | 505 | C2-12（D3） |
| `fakerInputVersion` | 524 | C2-17 |
| `InitOutputKBMMapping` | 528 | C2-18（D3） |
| `exelocation` | 673, 708 | C2-21 |
| `hidHideInstalled` | 697, 774, 802, 814, 868 | C2-22 |
| `GetInstanceIdFromDevicePath` / `CheckHidHideAffectedStatus` | 813 / 816 | C2-23 / C2-24 |
| `getUDPServerPortNum` / `getUDPServerListenAddress` | 896, 1022, 1746 / 897, 1023, 1747 | C2-25 / C2-26 |
| `getOSCServerPortNum` | 939, 941 | C2-27 |
| `getOSCSenderAddress` / `getOSCSenderPortNum` | 955, 956 | C2-27 |
| `RefreshViGEmBusInfo` | 1065 | 除外（C2-EX04、ViGEm） |
| `IsRunningSupportedViGEmBus` | 1066, 1770 | 除外（C2-EX05、ViGEm） |
| `vigembusVersion` | 1641, 1772 | 除外（C2-EX06、ViGEm） |
| `OutContType` | 1426 | C2-28 |
| `IsAdministrator` | 1633 | C2-32 |
| `ProcessPriority` | 1788 | C2-37 |
| `IsUsingUDPServerSmoothing` | 1802 | C2-50（H） |
| `RefreshExtrasButtons` / `LoadControllerConfigs` | 2105 / 2106 | C2-41 / C2-42 |
| `IsFirstConnection` / `MarkConnected` | 2126, 2133 / 2151 | C2-44 |
| `SelectedProfile` / `LinkedProfileUI` | 2156, 2161 / 2167, 2172 | C2-46 |
| `ApplyProfileToSlot` | 2178 | C2-47（O1） |
| `activeOutDevType` | 2201, 2208, 2573, 2681 | C2-31（D1） |
| `UDPServerSmoothingMincutoff` / `UDPServerSmoothingBeta` | 2267, 2270, 2279, 2285 | C2-49 |
| `TEST_PROFILE_INDEX` | 2472 | 除外（C2-EX03、const） |
| `store`（`Global.store.EmitMissingActionLogsForDevice`） | 2763 | C2-56 |
| `UseIconChoice` | 2772 | C2-57 |
| `GetGyroOutMode` | 2785 | C2-58 |
| `GetOutputDS4TriggerMode` | 2861 | C2-61 |
| `GetSASteeringWheelEmulationAxis` | 2911 | C2-64 |

---

## 付録B: 実施・検証記録

### B.1 PR-1（Step2-1）: 2026-09-19

- **コミット**: `da16de4`（`For-DI-migration-work`）
- **自動検証**: ビルド、テストビルド、テスト実行の全てが成功（開発環境での実施結果）
- **実機確認**（ユーザー実施）:

| # | 項目 | 結果 | 確認内容 |
|---|---|---|---|
| 1 | OSC / UDP の起動・停止 | **未実施** | 対応するサードパーティアプリを所持していないため実施できず |
| 2 | HidHide 有効時のデバイス隠蔽 | OK | 正常に隠蔽されることを確認 |
| 3 | 排他モード（Hide DS4 Controller） | OK | HidHide 側の隠蔽を解除した状態で確認。ON: `joy.cpl` では2つのコントローラーが見えるが、パッドテストサイトでは仮想コントローラーの出力のみが反映される。OFF: パッドテストサイトでデフォルトのボタン出力と仮想コントローラーの出力の両方が反映される |
| 4 | プロセス優先度 | OK | 設定に応じてタスクマネージャの優先度が変わることを確認 |
| 5 | QuickCharge | OK | ON: Bluetooth 接続中に USB ケーブルを挿すと、Bluetooth が切断されると同時に USB 接続のコントローラーが接続され、仮想コントローラーも有効になる。OFF: Bluetooth 接続は維持され、USB 接続のコントローラーは接続状態にならない |
| 6 | 停止時の BT 切断（DCBT） | OK | ON: 機能停止や DS4Windows 終了時に Bluetooth 接続も切断される。OFF: Bluetooth 接続は維持される |

- **未検証項目**: OSC / UDP の実機送受信。PR-1 の変更（有効フラグ・ポート・アドレスの読み出しを `IAppSettingsService` 経由へ変更、UDP 平滑化イベントの購読を転送に変更）は、単体テスト（`ControlServiceShimEquivalenceTests`）で Global との状態共有とイベント転送を検証済みだが、実際の送受信は確認できていない。Step11 の実機検証マトリクスへ持ち越し、受信側のツールを用意できた時点で確認する。

### B.2 PR-1b（Step2-1b）: 2026-09-19

- **実装**: §6 Step2-1b のとおり（`IEnvironmentService`／`EnvironmentService`／`ControlService`、新規テスト `ControllerSlotLimitTests`）。
- **静的検証**: Roslyn の意味診断を変更前後で比較し、DS4Windows 全ソース（既存診断 1,265 件）で新規エラー 0 件を確認。テストファイルも xUnit の簡易スタブで検証した。
- **未実施**: `dotnet build` / `dotnet test` / 実機確認（レビュー後に開発環境で実施）。