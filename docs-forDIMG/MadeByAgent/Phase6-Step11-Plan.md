# Phase6-Step11 計画書: 2層構造・階層化総合検証（自動回帰テスト ＋ 実機シナリオマトリクス）

作成日: 2026-09-09  
改訂日: 2026-09-18（Step2〜Step10確定スコープ約400箇所統合・推奨案［2層構造・階層化総合検証］採用・全面拡充改訂）  
状態: 計画書改訂・承認待ち（実装未着手）  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（Core層・Mapping層・Mouse層 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md`（UI層・ViewModel層 監査記録）  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step10-Plan.md`（各ステップ確定実装計画書）  
  - `Phase5-Step14-RealDevice-Verification-Checklist.md`（Phase5 実機検証成功モデル）  

---

## 0. 背景と改訂の経緯

### 0.1 暫定193件から全域約400実参照への精査と統合
旧計画書（2026-09-11）では、対象範囲を「Step2〜8（暫定193件）」と表記していた。  
しかし、Step1 精密再監査（`Phase6-Step1-*`）および Step2〜Step10 で策定された実装計画により、Phase6 で実施される改修の全容は**実参照式約404箇所（除外定数や引き継ぎ等を含めると500箇所以上）**に上る大規模なものであることが確定した：

- **Step2 (`ControlService.cs`)**: 実参照66箇所（ホットパス、KBMライフサイクル、UDP/OSC、Pure DIコンストラクタ）
- **Step3 (`Mapping.cs`)**: 実装10箇所引数渡し ＋ 101箇所Phase7引き継ぎ台帳化
- **Step4 (`Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs`)**: 実参照73箇所（Pure DI引数注入、マウス・ジャイロ・ホイールホットパス保護）
- **Step5 (`OutputSlotService.cs`)**: 孤立配列 `_deviceTypes[]` 完全撤廃、三態SSOT確立、UDP診断コマンド是正、実参照8箇所
- **Step6 (`ProfileEditor.xaml.cs` / `IOutputSlotService`)**: マッピング表示追従バグ是正、負債API非推奨化、実参照4箇所
- **Step7 (`App.xaml.cs`)**: Pre-Host領域厳格保護（11箇所温存）、Post-Host 32箇所（Composition Root解決）
- **Step8 (`ProfileEditor.xaml.cs`)**: 案3-A 段階的MVVM移設（`ProfileSettingsViewModel` へロジック移設、Fat View解体、MainWindow解体推進）、実参照60箇所
- **Step9 (主要4大ViewModel)**: Pure DI徹底、カテゴリ別移行（`Settings`, `MainWindow`, `TrayIcon`, `ProfileSettings`、計79箇所）
- **Step10 (残存小型View & 小型ViewModel & 補助クラス)**: 機能ドメイン別3グループペアPure DI化、実参照72箇所

### 0.2 方針の確定: 【推奨案: 選択肢A（2層構造・階層化総合検証アプローチ）の採用】
Phase5-Step14 で大成功を収めた検証モデル（自動回帰テストスイートの完全合格 ＋ 実機検証チェックリストの厳格運用）を完全踏襲し、以下の2層構造で Phase6 全域の成果を横断的かつ徹底的に検証する：

1. **第1層: 自動回帰テストスイートの全件実行 ＋ 静的解析による未検知参照ゼロ証明**:  
   既存テスト（Actions / Standalone）に加え、Step2〜Step10 で新たに追加された全単体テストを完全実行。Roslyn/grep 静的解析により、明示的除外（const定数、Pre-Hostブートストラップ、ViGEmバックエンド直接依存）以外の `Global.` 直参照がコードベース全体から完全に根絶されたことを客観的に証明する。
2. **第2層: 実機デバイスマトリクスによる構造化シナリオ検証**:  
   `Phase6-Step11-RealDevice-Verification-Checklist.md` を作成し、本フォークの主役である **Flydigi Vader 4 Pro**（追加ボタン C/Z/M1〜M4、ジャイロ、有線/ドングル/BT）、**DS4**、**DualSense** を用いた実機検証を実施。10大検証セクションにより、ホットパス性能、入力追従性、三態SSOT、表示追従、MVVM保存、長時間の安定稼働を保証する。
3. **Step12（旧シム物理削除判断）への確実なゲート機能**:  
   本ステップの完全合格（全テスト Green ＋ 全実機チェック項目 ○）をもって、呼出元0件となった旧静的メンバの安全な物理削除を正式に認可（GO / NO-GO 判定）する。

---

## 1. 目的

1. Step2〜Step10 で実施された全改修（約404実参照）に対して、機能欠落（Feature Drop）および動作リグレッションが一切発生していないことを、自動テストと実機テストの両輪で証明する。
2. 毎秒250〜1000回実行される入力ポーリング・ホットパス（`ControlService`, `Mapping`, `Mouse`）において、DI化に伴う入力遅延、GC アロケーション、CPU負荷の悪化がゼロであることを実証する。
3. Flydigi Vader 4 Pro コントローラーの固有機能（C/Z ボタン、背面 M1〜M4 パドル、ポーリングレート等）が完全に動作することを保証する。
4. Step12（旧静的シムの物理削除判断）に引き渡す客観的かつ反論不能な合格エビデンス（検証報告書・チェックリスト）を完成させる。

---

## 2. 第1層: 自動回帰テストスイート ＆ 静的解析完全証明

### 2.1 自動テストスイートの全件実行
以下のすべてのテストプロジェクトを Release x64 で実行し、警告ゼロ・合格率 100% を確認する。

1. **既存基盤テスト**:
   - `DS4Windows.Actions.Tests.csproj`（156件超）
   - `StandaloneTests.csproj`（13件超）
2. **Phase4 / Phase5 既存サービステスト**:
   - `AppSettingsServiceTests`, `AutoProfileServiceTests`, `DeviceStateServiceTests`, `EnvironmentServiceTests`, `NotificationServiceTests`, `OutputSlotServiceTests`, `PathServiceTests`, `ProfileRepositoryTests`, `SpecialActionRepositoryTests` 等
3. **Phase6（Step2〜Step10）で追加された全新規単体テスト**:
   - `ControlServiceDiWiringTests.cs`, `ControlServiceHotPathAllocationTests.cs` (Step2)
   - `MappingSettingsPassThroughTests.cs`, `MappingHotPathIntegrityTests.cs` (Step3)
   - `MouseDiWiringTests.cs`, `MouseWheelShimTests.cs`, `MouseCursorMovementTests.cs` (Step4)
   - `OutputSlotServiceSsotTests.cs`, `MainWindowUdpQueryTests.cs` (Step5)
   - `AppCompositionRootTests.cs`, `AppShimIntegrationTests.cs` (Step7)
   - `ProfileSettingsViewModelMvvmTests.cs`, `ProfileEditorShimEquivalenceTests.cs` (Step8)
   - `SettingsViewModelDiTests.cs`, `TrayIconViewModelBatteryTests.cs`, `MainWindowsViewModelViGEmTests.cs` (Step9)
   - `SmallViewModelsDiWiringTests.cs`, `SpecialActionEditorDiTests.cs`, `SaveWherePathServiceTests.cs` (Step10)

### 2.2 静的解析による「未検知 Global 参照ゼロ」の完全証明
PowerShell または Python スクリプトにより、ソリューション内の全 C# ファイルを走査し、以下の「明示的除外リスト」に登録された行以外の `Global.` 直接参照が**完全に 0 件であること**を機械的に検証・出力する。

#### 明示的除外リスト（許容される正当な Global 参照）:
- **真の定数（`const`）**: `MAX_DS4_CONTROLLER_COUNT`, `OLD_XINPUT_CONTROLLER_COUNT`, `TEST_PROFILE_INDEX`, `ASSEMBLY_RESOURCE_PREFIX`, `RESOURCES_PREFIX` 等
- **純粋計算ユーティリティ（状態非保持）**: `Clamp`, `getTransitionedColor`
- **Pre-Host ブートストラップ処理（カテゴリA）**: `App.xaml.cs` 内のコンテナ構築前ログ初期化・多重起動判定・driverinstall分岐（計11箇所）
- **ViGEm クライアント直接依存（Phase6対象外）**: `RefreshViGEmBusInfo`, `IsRunningSupportedViGEmBus`, `vigembusVersion`, `vigemInstalled`

---

## 3. 第2層: 実機デバイスマトリクス ＆ 10大構造化シナリオ検証

### 3.1 対象実機デバイスマトリクス
本リポジトリ（`DS4Windows-Vader4Pro`）の責務に基づき、以下の実機マトリクスを対象とする：

| コントローラー機種 | 接続方式 | 主な検証重点項目 |
|---|---|---|
| **Flydigi Vader 4 Pro** | 有線 USB | 1000Hz ポーリング、C/Z ボタン、背面 M1〜M4 パドル、ジャイロ |
| **Flydigi Vader 4 Pro** | 2.4G ワイヤレスドングル | 無線接続時の安定性、独自レポート解析、遅延体感 |
| **Flydigi Vader 4 Pro** | Bluetooth | BT 接続認識、スロット割当、切断・再接続 |
| **Sony DualShock 4** | 有線 USB / BT | タッチパッドマウス、ライトバー、6軸ジャイロ、排他モード |
| **Sony DualSense (PS5)** | 有線 USB / BT | アダプティブトリガー設定、触覚フィードバック、オーディオ |
| **Nintendo Switch Pro** (任意) | Bluetooth | ジャイロ操作、ボタンリマップ |

---

### 3.2 10大検証セクション（`Checklist.md` 構成仕様）

`Phase6-Step11-RealDevice-Verification-Checklist.md` に以下の 10大セクションを定義し、各項目に対して `[○: 合格 / △: 軽微課題 / ×: 失敗 / 未: 未実施]` を記録する。

#### セクション1: 起動・初期化・Pre-Host 整合性検証 (Step7, Step10 G1)
- [ ] 通常起動時にスプラッシュ・UI が正常表示されること。
- [ ] `-m`（最小化起動）でトレイアイコンのみ起動すること。
- [ ] `-driverinstall` 起動時に特殊インストーラ画面がクラッシュせず表示されること。
- [ ] 初回起動ダイアログ（`SaveWhere`）での設定保存先選択が正常に反映されること。
- [ ] 言語設定（英語 ⇄ 日本語）の変更が即時・再起動後に反映されること。

#### セクション2: デバイス接続・認識・ホットパス性能検証 (Step2, Step4, Vader 4 Pro)
- [ ] コントローラー接続時、スロットに即時認識され仮想デバイスがプラグインされること。
- [ ] 1000Hz 入力ポーリング時に入力遅延・カクつきが一切ないこと（体感ラグゼロ）。
- [ ] ボタン連打・スティック全開旋回時にメモリリークや GC スパイク（カクつき）が発生しないこと。
- [ ] **Vader 4 Pro 固有ボタン（C, Z, M1, M2, M3, M4）が正しく認識・マッピングされること**。
- [ ] コントローラー切断（ケーブル抜去／BT電源オフ）時、仮想デバイスが安全にアンプラグされること。

#### セクション3: マウスエミュレーション・ジャイロ・ホイール検証 (Step4)
- [ ] タッチパッドによるマウスカーソル移動が滑らかで、ジッター補正が効いていること。
- [ ] タッチパッドタップ、左右クリック、マルチタッチが意図通り動作すること。
- [ ] ジャイロマウス操作（傾き検知）が滑らかに追従し、トグル切替・反転が動作すること。
- [ ] スティックまたはタッチによるホイールスクロール（上下）が正確に入力されること。

#### セクション4: 出力スロット・三態SSOT・ViGEm ホットスワップ検証 (Step5, Step6)
- [ ] 出力デバイス種別の変更（Xbox 360 ⇄ DS4）時、ViGEm バス上で仮想デバイスが即座に再接続されること（`joy.cpl` で機種変更を確認）。
- [ ] UDP 診断コマンド `query.<device>.outconttype` が、孤立値 `None` ではなく正しい設定値（`Xbox360` / `DS4`）を応答すること。
- [ ] `IOutputSlotService` の負債API（`PluginSlot`, `UnplugSlot`）が呼出元0件で非推奨化されていること。

#### セクション5: ProfileEditor 表示追従・設定変更検証 (Step6, Step8)
- [ ] ProfileEditor を開いた状態で別プロファイルをロード（Reload）した際、マッピング一覧のボタン表記（Xbox「A/B/X/Y」⇄ DS4「Cross/Circle」）が即座に切り替わること。
- [ ] エディタの左右スプリッター幅および各カラム幅が再起動後も維持されること。

#### セクション6: プロファイル保存・適用・ブランク作成（MVVM移設）検証 (Step8)
- [ ] エディタで設定を変更し「Save」をクリックした際、XML が正常保存され、接続中のコントローラーに即時適用されること。
- [ ] 「New」操作でブランクプロファイルが生成され、安全にデフォルトリセットされること。
- [ ] アクションの XML エクスポート機能が正常に動作すること。

#### セクション7: 主要設定（OSC/UDP/平滑化/テーマ/ログ）検証 (Step9)
- [ ] OSC 送信・受信設定（ポート・アドレス）の変更が保存され、通信が機能すること。
- [ ] UDP サーバーおよび 1Euro フィルタ平滑化（Mincutoff, Beta）の変更が即時反映されること。
- [ ] ダークテーマ ⇄ デフォルトテーマの切り替えが即座に全画面に適用されること。
- [ ] ログレベル変更（Info, Debug）およびログローテーションが正常に機能すること。

#### セクション8: タスクトレイ・バックグラウンド常駐・通知検証 (Step9)
- [ ] 最小化時にタスクトレイに格納され、右クリックメニュー（プロファイル切替、Stop/Start、Exit）が動作すること。
- [ ] コントローラーのバッテリー残量変化時に、トレイアイコンおよびツールチップが正確に更新されること（B12シム検証）。

#### セクション9: 小型ダイアログ・自動プロファイル・スペシャルアクション検証 (Step10)
- [ ] `BindingWindow` でボタンをクリックし、キーボード・マウス・コントローラーボタンの再マッピングが正常に保存されること。
- [ ] `AutoProfiles` で特定アプリ（例: メモ帳やゲーム exe）にプロファイルを紐付け、ウィンドウフォーカス連動でプロファイルが自動切替されること。
- [ ] `SpecialActionEditor` でマクロ、キーコンボ、バッテリー読み上げ等のアクションを作成・実行できること。

#### セクション10: 連続ストレステスト・終了永続化・総合判定
- [ ] 30分〜1時間の連続ゲームプレイまたは入力ストレステストにおいて、異常終了・フリーズ・入力抜けがゼロであること。
- [ ] アプリ終了（タスクトレイ「Exit」またはウィンドウ「閉じる」）時、全設定・プロファイルが破損なく完全に保存されること。
- [ ] **Step12（旧シム物理削除）への引き渡し判定: 【GO（認可）】 であること**。

---

### 3.3 各 Step から先送りされた実機確認項目（先送り台帳）
各 Step の PR で実機確認を先送りした項目を、本 Step の実機マトリクスで確認する。項目が増えた場合は、この台帳に追記する。

| 元の PR | 先送りした項目 | 対応するセクション | 状態 |
|---|---|---|---|
| Step2-PR-1 | OSC／UDP の起動・停止（対応するサードパーティアプリを所持していないため実施できず）。UDP 平滑化イベント、OSC 送受信設定の反映 | セクション7 | 未確認 |
| Step2-PR-4 | ②プロファイルの「起動プログラム」設定（`LaunchProgram`） | セクション9（スペシャルアクション・プロファイル） | 未確認 |
| Step2-PR-4 | ③ステアリング軸のスペシャルアクション | セクション3 | 未確認 |
| Step2-PR-4 | ⑤Joy-Con の結合デバイスがある場合のジャイロの動作 | セクション3 | 未確認 |
| Step2-PR-5 | 実機確認の全項目（実施した場合は不要）: 通常操作、タッチパッドのトグル、KBM 出力、スペシャルアクション・エクストラの動作、バッテリー表示のトレイアイコン、遅延警告時の点滅、DS4 トリガーモード、OSC／UDP。あわせて On_Report の処理時間（アプリの入力遅延・出力遅延の最大値表示）を PR-4 の状態と比較 | セクション2、3、7、8、9 | 未確認（2026-09-20 に先送りを確定） |
| Step3-6a | ステアリングホイールエミュレーション（`Scale360degreeGyroAxis`／`GetSASteeringWheelEmulationAxis`。SA 軸のデッドゾーン・アンチデッドゾーン・スムージング・ファズ）。vJoy 環境がなく実施できないため先送り。Step3-6a で `ctrl.ProfileSettingsService` 経由へ置換済みの経路 | セクション3 | 未確認（2026-09-24 に先送りを確定） |
| Step4 | `Mouse`／`MouseCursor`／`MouseWheel` の Pure DI 化（81行）の実機確認のうち、タッチパッドのマウス移動・タップ・クリック、ジャイロマウス、2 本指スクロールは 2026-09-24 に確認済み（問題なし）。次の項目を先送り: ①タッチパッドのスティック・ホイール等の出力モード（`TouchpadOutMode` の各モード）、②タッチボタン（左・右・上・マルチ）とクリックパススルー、③ジャイロ→スティック（`MouseJoystick`）・ジャイロのスワイプ・コントロール出力、ジャイロのトリガー条件（`SATriggers`）、④コントローラーの接続・切断の繰り返し（`Mouse` の再生成経路） | セクション3 | 未確認（2026-09-24 に先送りを確定） |
| Step5 | ①UDP 診断コマンド `query.<device>.outconttype` がプロファイルの出力種別（`X360`／`DS4`）を返し、`None` を返さないこと、`activeoutdevtype` が従来どおり動くこと（UDP クライアントの環境がないため先送り。ソース走査ガード `MainWindowUdpOutContTypeGuardTests` で参照先を固定済み）。②コントローラー接続時に、出力種別（X360／DS4）がプロファイルどおりに切り替わること（Step5 はこの経路に触れていないため確認は念のため） | セクション7、セクション2 | 未確認（2026-09-24 に先送りを確定） |
| Step6 | （参考・別途調査）プロファイル切り替えで仮想コントローラーの種別（X360⇄DS4）が変わったとき、以前は表示されていた「DS4／X360 コントローラーが接続された」旨の Windows トースト通知が表示されない。切り替え自体は正常。調査結果（2026-09-24）: ①DS4Windows には仮想コントローラー接続のトーストを出すコードがない（`ControlService.PluginOutDev` の接続ログは 2019 年からコメントアウト）、②仮想デバイスの抜き差し経路（`ScpUtil.cs` の `PostLoadSnippet`、`ControlService.PluginOutDev`／`UnplugOutDev`、`OutputSlotManager`）は Step4〜6 で未変更、③トーストが出ていた頃のブランチをビルドして実機確認しても表示されなかった。以上から Windows 側（デバイス接続通知の仕様・通知設定・集中モード・Xbox Game Bar 等）が原因と判断し、アプリの回帰ではない。別途調査の観点: Windows の通知履歴での送信元、出力スロットが Permanent で仮想デバイスが再利用されていないか（`FindExistUnboundSlotType`）、Windows の通知・Game Bar の設定 | セクション2 | 参考（2026-09-24 に記録。アプリの回帰ではないため Step11 の合否判定には含めない） |
| Step3-6c | （参考・任意）Alt+Tab マクロの挙動: 押している間 Tab が繰り返し送られ、2つのウィンドウ間でフォーカスが切り替わり続け、離した時点でフォーカスされていたウィンドウで確定する。Windows の Alt+Tab 設定（ウィンドウ一覧の表示など）や開いているウィンドウ数への依存を切り分ける。Step3-6c の変更は `KEY_TAB`／`KEY_LALT` の読み取り元の置換のみで送出内容を変えていないため、回帰とは判断していない。Bug1（Alt+Tab 誤爆）の調査の参考にもなる | セクション3 | 参考（2026-09-24 に記録。確認は任意） |
| Step7 | `App.xaml.cs` の Post-Host DI 化（Step7-2・7-3）の実機確認のうち、通常起動・終了、`-m`、多重起動、`-driverinstall`、初回起動（言語選択〜既定プロファイル自動作成、`Actions.xml` の既定アクション作成と既存プロファイルへの非追記）は 2026-09-25 に確認済み（問題なし）。次の項目を先送り: ①プログラムフォルダと AppData の両方に設定がある状態での起動（保存場所の選択画面に「両方にある」旨の選択肢が出ること。`IPathService.HasMultipleSaveLocations`）、②書き込みできない場所に設定がある初回起動での `AttemptSave` のコピー処理（AppData へのコピーと終了。`RoamingAppDataPath`／実行ファイルのフォルダの参照先を置換済み）、③Windows のサインアウト・シャットダウン時の設定保存（`Application_SessionEnding` → `CleanShutdown`）。詳細は `Phase6-Step7-Plan.md` §6.5・§6.6 | セクション1 | 未確認（2026-09-25 に先送りを確定） |
| Step7b | プロファイル編集画面の即時反映廃止（編集は作業スロット、実機は targetDevice）後の、ステアリングホイール校正（ジャイロタブで Output Mode を Controls にしたときの「360 Steering Wheel」枠の校正ボタン、`SteeringWheelEmulationCalibrateBtn_Click`。Edit ボタンのコントローラー［一覧経由ではコントローラー0］が対象になること、校正中のライトバー点滅、OK／キャンセルでの確定・取り消し）。vJoy 環境がなく実施できず（2026-09-26） | セクション3 | 未確認 |

---

## 4. 検証手順とスケジュール

```text
【Phase6-Step11 実行フロー】
├─ 1. 自動テストスイートの完全実行 ＆ 静的解析スクリプト実行（第1層）
├─ 2. Checklist.md の展開と実機テスト環境（USB/BT/Vader4Pro）の準備
├─ 3. セクション1〜9 の個別機能マトリクス検証（第2層）
├─ 4. セクション10 の長時間連続ストレステスト（30分〜1時間）
├─ 5. チェックリスト結果集計と判定レビュー
└─ 6. Phase6-Step11-Completion-Report.md 作成 ＆ Step12 承認ゲート開放
```

---

## 5. ロールバックおよび是正方針

万が一、いずれかの項目で「×（失敗）」または重大な「△（性能劣化・カクつき）」が検出された場合は、以下の手順で直ちに対処する：
1. **問題箇所の特定**: Step2〜Step10 のどのサブPRが原因であるかを単離する。
2. **局所的ホットフィックスまたは revert**: 
   - 単純なシム連動漏れであれば、該当ステップの修正パッチを即時適用する。
   - ホットパスの性能劣化やアーキテクチャ競合である場合は、該当サブPRを `git revert` して再設計する。
3. **再テスト**: 修正後、第1層自動テストおよび該当実機セクションを再実行し、全項目「○」を確認する。

---

## 6. 完了判定チェックリスト（Step12 移行ゲート）

- [ ] 第1層: 自動テスト（既存 Actions / Standalone ＋ Phase6 新規追加単体テスト全件）が 100% 成功していること。
- [ ] 第1層: 静的解析により、明示的除外以外の Global 直接参照がソリューション全体から完全に 0 件であることが証明されていること。
- [ ] 第2層: `Phase6-Step11-RealDevice-Verification-Checklist.md` の全 10 セクションが実施され、すべての必須項目が「○（合格）」であること。
- [ ] Flydigi Vader 4 Pro コントローラーの独自機能（C/Z/M1〜M4、1000Hz 入力追従）が実機で確認されていること。
- [ ] 入力遅延・マウス追従性・ホットスワップ瞬断において体感上の劣化が一切ないこと。
- [ ] 30分以上の連続操作でクラッシュ・メモリリークが皆無であること。
- [ ] `Phase6-Step11-Completion-Report.md` が作成され、Step12（旧静的シム物理削除）の正式認可（GO）が記録されていること。
