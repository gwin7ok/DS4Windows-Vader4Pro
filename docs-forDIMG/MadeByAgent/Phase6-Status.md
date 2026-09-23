# フェーズ6 進捗管理文書: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

最終更新日: 2026-09-23  
状態: Phase6-Step1 完了 / **Step2 完了（2026-09-21 確定）** / **Step13（配置整理 44件）完了（2026-09-21 確定）** / **Step3-1・Step3-2 完了確定（ともにビルド・テスト・実機確認済み、2026-09-22〜23）** / **Step3-3 対象消滅（2026-09-23、Abs Mouse機能削除に伴い対象コード自体を撤去。詳細は §6.8）** / Step10b（1ファイル1型の全数是正）新設 / **Phase8（Controls/SpecialActions統合ディスパッチ）新設（2026-09-23、Phase7完了後に独立フェーズとして着手。詳細は §6.7）** / Step3-4〜Step12 計画確定・承認待ち（**次は Step3-4**）  
対象ブランチ: `For-DI-migration-work`  
前フェーズ完了状況: **Phase5 完了（Step1〜15 完了済み、SSOT確立・Issue 7根本解消完了）**  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 主な対象ファイル | 最新確定実参照数 | ステータス | 確定計画書 / 成果物 | 完了日 / 予定 |
| :--- | :--- | :--- | :---: | :---: | :--- | :---: |
| **Step 1** | 詳細監査と対象確定 | ソリューション全域 | 762参照走査 | **完了** | `Phase6-Step1-Completion-Summary.md` | 2026-09-18 |
| **Step 2** | `ControlService.cs` 解体 | `ControlService.cs` | 66箇所 | **完了（66/66 ID）** | `Phase6-Step2-Plan.md` | PR-1〜6 完了（2026-09-20）。完了確認は計画書 付録C・B.12 |
| **Step 3** | `Mapping.cs` 段階的引数渡し | `Mapping.cs` | 145箇所（Step3-1・3-2完了、Step3-3は対象消滅、残Step3-4〜3-7） | Step3-1・Step3-2完了（ビルド・テスト・実機確認済み）、**Step3-3は対象消滅（§6.8）** | `Phase6-Step3-Plan.md`、`Phase6-Step3-Reality-Check-Ledger.md` | Step3-1・3-2完了（Step3-4〜3-7 未着手） |
| **Step 4** | マウスエミュレーション系 DI化 | `Mouse*.cs` (3ファイル) | 73箇所 | 計画確定・承認待ち | `Phase6-Step4-Plan.md` | 未着手 (PR-1〜5) |
| **Step 5** | OutputSlotService SSOT統合 | `OutputSlotService.cs` 等 | 8箇所 | 計画確定・承認待ち | `Phase6-Step5-Plan.md` | 未着手 (PR-1〜3) |
| **Step 6** | 出力切替UI表示追従・負債整理 | `ProfileEditor.xaml.cs` 等 | 4箇所 | 計画確定・承認待ち | `Phase6-Step6-Plan.md` | 未着手 (PR-1〜3) |
| **Step 7** | `App.xaml.cs` Post-Host DI化 | `App.xaml.cs` | 32箇所 (11保護) | 計画確定・承認待ち | `Phase6-Step7-Plan.md` | 未着手 (PR-1〜4) |
| **Step 8** | `ProfileEditor` 段階的MVVM移設 | `ProfileEditor.xaml.cs` 等 | 60箇所 | 計画確定・承認待ち | `Phase6-Step8-Plan.md` | 未着手 (PR-1〜5) |
| **Step 9** | 主要4大ViewModel Pure DI化 | `SettingsVM`, `MainWindowVM` 等 | 79箇所 | 計画確定・承認待ち | `Phase6-Step9-Plan.md` | 未着手 (PR-1〜6) |
| **Step 10**| 小型UI/ViewModel ドメイン別DI化| 小型View 11, 小型VM 12 等 | 72箇所 | 計画確定・承認待ち | `Phase6-Step10-Plan.md` | 未着手 (PR-1〜4) |
| **Step 10b**| 1ファイル1型の全数是正 | 複数型55ファイル（新設203） | 分割のみ | 計画確定・承認待ち | `Phase6-Step10b-Plan.md` | Step10 の後・Step11 の前（Step13 の後に実施） |
| **Step 11**| 2層構造・階層化総合検証 | 全自動テスト ＋ 実機検証 | - | 計画確定・承認待ち | `Phase6-Step11-Plan.md` | 未着手 |
| **Step 12**| 旧シム物理削除 ＆ 完了判定 | `ScpUtil.cs` | 呼出元0件ラッパー | 計画確定・承認待ち | `Phase6-Step12-Plan.md` | 未着手 (PR-1〜4) |
| **Step 13**| `.cs` ファイル配置の統一 | `DS4Windows/` 配下の44ファイル | git mv のみ | **完了（44/44、2026-09-21 確定）** | `Phase6-Step13-Plan.md` | 13-6 は Phase6 範囲外（最終クリーンアップ） |
| **合計** | **Phase6 全域** | **約40ファイル ＋ 配置統一44ファイル ＋ 分割55ファイル** | **約404箇所** | **計画策定100% / 実装: Step1・Step2・Step3-1・Step3-2・Step13 完了** | **全Step計画書完備（Step13 を含む）** | - |

---

## 2. 各ステップの詳細進捗状況

### Phase6-Step1: 詳細監査と対象確定【完了】
- **進捗率**: **100%（完了）**
- **完了日**: 2026-09-18
- **成果サマリ**:
  - ソリューション全域で 762 箇所の参照シンボルを完全走査。
  - 重複ゼロ・漏れゼロを数学的に証明（全件一意ID台帳 `Phase6-Step1-Unique-ID-Manifest.md` 完備）。
  - 各参照を 分類(a) 既存契約直結、分類(b) 既存サービス軽微拡張、分類(c) 要設計・集約、除外（const / Pre-Host / ViGEm）に分類確定。
  - 成果物: `Phase6-Step1-Completion-Summary.md`, `Phase6-Step1-Core-Reference-Evidence.md`, `Phase6-Step1-UI-Reference-Evidence.md` ほか全10文書を格納。

---

### Phase6-Step2: `ControlService.cs` のGlobal直参照解消【完了】
- **進捗率**: **100%（66 ID 中 66 ID 実装済み。C2-02 は決定D2 により防御コードとして温存。全 PR がビルド・テスト成功、反映済み）**
- **確定方針**: コンストラクタ引数注入の拡張（Pure DI）。決定 D1（`Func<IOutputSlotService>` の遅延解決）、D2（新規引数はすべて必須）、D3（`IVirtualKBMLifecycle` の新設）、O1=B／O4=B-2（`IProfileSlotApplier` の新設）、O2=C／O3=A（スロット上限のサービス化と段階移行）。
- **完了した PR**（いずれもビルド・テストビルド・テスト実行が成功し、リモート反映済み）:
  - PR-1（環境・パス・基本設定）／PR-1b（スロット上限のサービス化）／PR-1c（DI サービス2件のスロット上限注入）: 実機確認は OSC/UDP のみ未実施（対応するサードパーティアプリを所持していないため）、他は問題なし。
  - PR-2（出力スロット・プロファイル状態・接続イベント）: 実機確認（接続時のプロファイル適用・ライトバー色・出力種別切替・DInput のみ）問題なし。
  - PR-3（KBM ハンドラ初期化・ライフサイクル）: 実機確認 1〜3 すべて問題なし。
  - Step13-1（配置整理 20件、PR-4 の前に実施）: ビルド・テスト成功。起動、コントローラー接続、プロファイル適用、プロファイル切替のスペシャルアクション — すべて問題なし。
  - PR-4（ID: C2-51〜54, 56, 58, 64, 65）: ビルド・テスト成功。実機確認は接続時のオプション適用と接続・切断の繰り返しを実施（問題なし）。起動プログラム設定・ステアリング軸のスペシャルアクション・Joy-Con 結合ジャイロは Step11 へ先送り。
- **完了**: PR-5（ID: C2-50, 55, 57, 59〜63, 66。修正版のテストで全件成功、リモート反映済み。実機確認は全項目を Step11 へ先送り。入力ループ最頻度経路。`Global.` 修飾行は 11 → 8 行。On_Report 相当の取得16個がヒープ割り当て0であることをテストで固定）。
- **完了**: PR-6（修正版のテストで全件成功、反映済み。`using static DS4Windows.Global;` の削除と `Global.vigemInstalled` への修飾。差分2行。`ControlService.cs` の `Global.` 参照は9箇所、すべて除外項目と C2-02。回帰ガードテスト `ControlServiceGlobalReferenceGuardTests` を追加）。
- **完了確認**: 計画書 付録C・B.12・B.14 を締結（2026-09-21）。Release ビルドはエラー・警告なし。持ち越し事項6件を記録済み。
- **計画ハイライト**:
  - `On_Report` 入力ループ内での**ゼロアロケーション（GC Alloc = 0）**および BackingStore 配列直接アクセスの徹底。
  - `ProfileApplicationService`／`OutputSlotService` との循環依存の回避（`Func<>` の遅延解決と出力ポートによる依存の逆転）。
  - 修飾 `Global.` 行（コメント除く）は 75 → 14 行（PR-3 完了時点）。

---

### Phase6-Step3: `Mapping.cs` の段階的引数渡し【Step3-1・Step3-2 完了、Step3-3 対象消滅】
- **進捗率**: **Step3-1・Step3-2 完了（ともにビルド・テスト・実機確認済み）、Step3-3 対象消滅（§6.8）、Step3-4〜3-7 未着手**
- **Step3-1 実装内容（2026-09-21実装、2026-09-22 ビルド・テスト・実機確認完了）**:
  - 新設 `IDisplayCoordinateService`／`DisplayCoordinateService`（`UseAllMonitors`、`PrepareAbsMonitorBounds`、`TranslateCoorToAbsDisplay`）。`ServiceRegistration.cs` に登録済み。
  - **決定D1を確認済みの単一呼び出し元（`ControlService.cs`のみ）で実現**: `IEnvironmentService.PrepareAbsMonitorBounds` を削除し（実装1件・モック0件を確認済みのため、委譲シムを残さず完全移設）、`ControlService` のコンストラクタに `IDisplayCoordinateService` を追加（必須引数、Pure DI）。`SystemEvents_DisplaySettingsChanged` の呼び出し元を新サービス経由に変更。
  - `IProfileXmlStore.SaveControllerConfigsForDevice` を追加（`Global.SaveControllerConfigs` へ委譲。対の `LoadControllerConfigsForDevice` と同型）。
  - `IProfileActionProvider.GetProfileActionIndexOf` を追加（`Global.GetProfileActionIndexOf` へ委譲）。
  - `IProfileActionProvider.GetProfileActionsRaw`（**論点4の早期解消に必要な追加契約。当初4件の外**）を新設。`GetProfileActionNames` の `ToArray()` によるホットパス割り当てを避けるため、コピーなしで実体の `List<string>` を返す。
  - `IProfileSettingsService.GetControlSettingsGroup` を追加（`SafeConfig.ds4controlSettings[deviceIndex]` への委譲。`GetDS4CSetting` と同型）。
  - 既存の `IProfileXmlStore`/`IProfileActionProvider` 実装クラス（テスト用スタブ含む、`AppSettingsServiceTests.cs`／`ProfileRepositoryTests.cs`／`ProfileActionChainServiceTests.cs`の3ファイル）を契約拡張に追随させ、コンパイル断絶を防止済み。
  - 単体テスト `Phase6Step3ContractExtensionTests.cs` を新設（Global との状態・参照共有を検証。`SaveControllerConfigsForDevice` のみ、実DS4Device依存のため契約実装の存在確認に留め、値の等価性は `LoadControllerConfigsForDevice` と同様に実機確認へ委ねる）。ControlService の新DI配線検証テストも追加。
  - **未検証事項**: この環境には dotnet がなく、`dotnet build`/`dotnet test` を実行できていない。開発者側でのビルド・テスト実行を経てからコミットすること。
  - **2026-09-22 テスト失敗の修正**: ビルド・テストビルドは成功したが、`GetProfileActionIndexOf_SharesStateWithGlobal` が失敗（期待値 `-1` に対し実際は `0`）。原因はテスト側の誤った期待値で、本番コード（`ProfileActionProvider.GetProfileActionIndexOf`）は `Global.GetProfileActionIndexOf`（ScpUtil.cs 3494〜3499行）と完全に同一実装であることを確認済み。`Dictionary.TryGetValue` は未検出時に `out` 引数を `default(int)`（＝0）で上書きするため、`Global` 側も本来 `-1` ではなく `0` を返す既存の挙動である（Step3-1では変更しない）。テストの期待値を `0` に修正し、`Global` との直接比較アサーションも追加した。この挙動自体の修正要否は持ち越し事項 K2（§6.5）として記録した。
- **2026-09-21 実地突き合わせによる方針転換**: 旧案A（非ホット10件のみ実装しホット101件はPhase7へ一括引き継ぎ）は、行番号・分類が現行コードと一致しないことが判明し撤回。`Phase6-Step3-Reality-Check-Ledger.md` を作成し、実参照150件（うち135件は既存契約で機械的に置換可能）と確定した。
- **確定方針（4論点）**:
  - 論点1（範囲）: **C＝段階的に解消**。150件全件をStep3-1〜3-7の7バッチに分けて解消し、Phase7への丸ごと引き継ぎは行わない。
  - 論点2（方式）: **S3＝引数渡し**。`Mapping` に新規 Service Locator は追加しない。
  - 論点3（未定義メンバの移行先）: モニター座標系→新設 `IDisplayCoordinateService`（モデル図03・04に反映済み）、`SaveControllerConfigsForDevice`→`IProfileXmlStore`、`GetProfileActionIndexOf`→`IProfileActionProvider`、`GetControlSettingsGroup`→`IProfileSettingsService`。
  - 論点4（機械置換できない4項目）: 3項目（`getProfileActions`/`GetProfileAction`/`GetActions`系）はStep3-2で早期解消。残り2項目（`reverseX360ButtonMapping`のClone、`Global.ApplyProfile`フォールバック）は選択肢を提示し実装Step直前に個別確認（既定案は計画書§2.4に明記）。
- **対象**: 実参照150件（置換可135、契約追加要8、要判断4、温存3）。バッチ構成はStep3-1（契約整備）〜Step3-7（温存・要判断の最終処理）。
- **Step3-2 実装内容（2026-09-23実装・ビルド・テスト・実機確認済み）**: 対象12件（`ProfilePath`、`SaveControllerConfigs`/`LoadControllerConfigs`/`OutContType`、`getProfileActions`/`GetProfileAction`/`GetActions`系）を全て解消。
  - **重要な発見**: `MapCustom`/`MapCustomAction`/`Scale360degreeGyroAxis`/`SAWheelEmulationCalibration` はいずれも既に `ControlService ctrl` を引数として受け取っていたため、想定していたコンテキスト構造体（Phase6-Step3-Plan.md §2.1）は不要だった。`ControlService` に読み取り専用の `internal` プロパティ5件（`ProfileActionProvider`/`ProfileXmlStore`/`DisplayCoordinateService`/`ProfileRepository`/`SpecialActionRepository`）を追加し、`ctrl.XxxService.Method(...)` の形でアクセスする方式で解消（S3方針に合致、新規 static Service Locator は追加していない）。
  - `ControlService` に新規必須コンストラクタ引数 `ISpecialActionRepository` を追加（`GetActions()`置換に必要。`ServiceRegistration.cs` も追随）。
  - `Mapping.cs` 内の診断ログ専用ヘルパー `LogActionDoneCountOnTrigger` にのみ `ControlService ctrl` パラメータを新規追加（呼び出し元11箇所を更新）。これが本バッチで唯一のメソッドシグネチャ変更。
  - テスト: `ControlServiceStep3Step2DiWiringTests.cs`（新規DI配線・5プロパティの実体一致検証）、`MappingLogActionDoneCountOnTriggerTests.cs`（シグネチャ変更・null安全性の回帰防止）を新設。`MapCustom`/`MapCustomAction` 自体を直接駆動するテストは、既存 `MappingSpecialActionSuppressionTests.cs` 冒頭コメントの通り本プロジェクトでも困難なため見送り、挙動保持の確認はビルド・実機確認に委ねる。
  - **検証結果（確定・2026-09-23）**: ビルド・テストとも成功。テスト失敗1件（`EnvironmentServiceInjectionTests`、`Global.appdatapath` 未設定）は新規テスト追加でxUnit実行順序が変わり既存の脆弱性（他テストの副作用への暗黙依存、Step2教訓 §6.4-1 該当）が表面化したもので、冪等ガード追加により修正済み。実機確認も完了（コントローラー接続・プロファイル適用・切替のスペシャルアクション等、問題なし）。実機確認中に判明した `[DI]` ログ過多の問題は解消済み（詳細は §6.5 K4 参照）。
- **Step3-3（対象消滅、2026-09-23）**: 当初実装した内容（`MapCustom` の絶対マウス出力座標変換5件を`ctrl.DisplayCoordinateService`/`ctrl.ProfileSettingsService`経由に置換）は、ビルド・テスト・実機確認・コミットまで完了していたが、その後Abs Mouse機能自体（ボタン割当機能・タッチパッドAbsoluteMouseモード・共有基盤`IDisplayCoordinateService`）がユーザー承認のもと削除されたことに伴い、対象コードごと撤去された。**Step3の残実参照数は150→145に更新**（Step3-3が担っていた5件が消滅）。詳細な経緯・削除範囲・`copilot-instructions.md`改訂は §6.8 参照。

---

### Phase6-Step4: マウスエミュレーション系のPure DI化【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **推奨案（Pure DI引数注入 ＋ `MouseWheel.cs` 正式統合）の採用**。
- **対象**: 実参照式73箇所（`Mouse`: 47, `MouseCursor`: 20, `MouseWheel`: 6、除外2）。
- **計画ハイライト**:
  - `MouseWheel.cs`（6件）を統合し、マウスサブシステムの Global 参照を一挙に完全根絶。
  - `DS4Device.cs` からサービスインスタンスを直接伝搬し、1000Hz 入力ループでの実行時オーバーヘッドを極小化。

---

### Phase6-Step5: `OutputSlotService` 全面SSOT統合 ＆ 孤立配列完全撤廃【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢B（孤立配列完全撤廃 ＋ 出力デバイス三態SSOT確立）の採用**。
- **対象**: 実参照式8箇所（`MainWindow.xaml.cs:1412` のUDP診断コマンド含む）。
- **計画ハイライト**:
  - バグの温床であった孤立配列 `_deviceTypes[]` を物理削除し、二重管理の根本原因を抹消。
  - 「永続設定（`OutContType`）」「実行時接続（`ActiveOutDevType`）」「UI一時状態（`OutDevTypeTemp`）」の三態台帳を確立。

---

### Phase6-Step6: ProfileEditor 出力切替表示追従バグ是正 ＆ 負債API安全整理【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **案1（保守的・安定性最優先アプローチ）の採用**。
- **対象**: 実参照式4箇所。
- **計画ハイライト**:
  - `ProfileEditor.xaml.cs` の `Reload()` に `mappingListVM.UpdateMappingDevType` を追加し、プロファイル再読込時のボタン名表記不整合バグを解消。
  - `IOutputSlotService` の呼出元0件API（`PluginSlot`, `UnplugSlot`）を安全に非推奨化。
  - `ScpUtil.cs:PostLoadSnippet` の実働ホットスワップ機構は安定性最優先で温存。

---

### Phase6-Step7: `App.xaml.cs` Post-Host領域のPure DI化【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢1（Pre-Host保護 ＋ Post-Host Composition Root解決）の採用**。
- **対象**: 実参照式32箇所（Pre-Host除外11箇所、const除外1箇所）。
- **計画ハイライト**:
  - 初期ログローテーションや driverinstall などの Pre-Host 領域（11箇所）を厳格に保護。
  - `InitializePostHostServices()` により、Host 構築直後に必要なサービスを一度だけ解決してキャッシュする正統な Composition Root パターンを適用。

---

### Phase6-Step8: `ProfileEditor.xaml.cs` 段階的MVVM移設 ＆ `MainWindow` 解体推進【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **案3-A（段階的MVVM移設 ＋ MainWindow解体推進）の採用**。
- **対象**: 実参照式60箇所（const除外38箇所）。
- **計画ハイライト**:
  - View に書かれていたプロファイル保存・適用・ブランク読込ロジックを **`ProfileSettingsViewModel` へ移設**（Dumb View 化）。
  - `MainWindow.xaml.cs` のサービスバケツリレーを撤廃し、MainWindow のスリム化を強力に前進。
  - 重要契約差（B8: `GetActionOrPlaceholder`、B9: `ApplyProfileToSlot`、B10: `LoadBlankDevProfile`）を薄いシムで完全吸収。

---

### Phase6-Step9: 主要4大ViewModelのPure DI徹底 ＆ カテゴリ別移行【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢1（主要4大ViewModel集中 ＋ カテゴリ別移行・Pure DI徹底）の採用**。
- **対象**: 実参照式79箇所（`Settings`: 55, `MainWindow`: 8, `TrayIcon`: 6, `ProfileSettings`: 10、const除外14）。
- **計画ハイライト**:
  - `SettingsViewModel`（55箇所）を機能カテゴリ別に3つのサブPRで安全に置換。
  - クラス内部でのサービスロケータ都度解決を完全排除し、コンストラクタ注入（Pure DI）を徹底。
  - 残存する小型ViewModel（12ファイル）は Step10 で対応する View とペアで Pure DI 化する方針を確定。

---

### Phase6-Step10: 残存小型UI・小型ViewModel・補助クラスのドメイン別ペアPure DI化【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢A（機能ドメイン別 3グループ分割・View/ViewModelペアPure DI化）の採用**。
- **対象**: 小型View 11ファイル、小型ViewModel 12ファイル＋SpecialActions、`PresetOption.cs`（実参照式計72箇所、除外9箇所）。
- **計画ハイライト**:
  - 「起動・環境系」「プロファイル・マッピング系」「スペシャルアクション編集系」の3グループに分割。
  - View と ViewModel をペアで同時に Pure DI 化し、バインディング切れを完全防止。
  - 本ステップの完了をもって、ソリューション全域からの Global 直接参照根絶を達成。

---

### Phase6-Step10b: 1ファイル1型（原則1）の全数是正【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: 位置は Step10 と Step11 の間（案C）。規約の過渡期ルールを追加（既存の複数型ファイルは本 Step で一括分割し、それまでは分割しない）。
- **対象**: 複数型ファイル55件、新設203ファイル（class 150、enum 34、struct 12、delegate 6、interface 1）。単一型でファイル名が型名と異なる3件は改名。
- **ウェーブ**: W1 データ型・補助型（19件）→ W2 DI 層（5件）→ W3 UI 層（20件）→ W4 デバイス・コア層（10件）→ W5 `ScpUtil.cs`（1件・29型）。
- **検証**: 型インベントリ・宣言テキスト・意味診断の一致（Roslyn）、1ファイル1型・命名、ビルド・テスト、起動スモーク。
- **未決**: 提供方法（分割ツール＋検証レポート／パッチ／全ファイル）。着手時に決定。

---

### Phase6-Step11: 2層構造・階層化総合検証【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢A（自動回帰テスト ＋ 実機シナリオマトリクス）の採用**。
- **計画ハイライト**:
  - **第1層**: Actions / Standalone / Phase6新規単体テスト全件実行 ＋ 静的解析による未検知Global参照ゼロ完全証明。
  - **第2層**: `Checklist.md` を運用し、**Flydigi Vader 4 Pro**（C/Z/M1〜M4パドル、1000Hz）、**DS4**、**DualSense** による 10大構造化シナリオ検証（30分〜1時間連続ストレステスト含む）を実施。
  - 全項目合格をもって Step12 の物理削除を正式認可。

---

### Phase6-Step12: 呼出元0件旧静的シムの安全防壁付き物理削除 ＆ 完了判定【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **選択肢2（安全防壁付きの物理削除）の採用**。
- **計画ハイライト**:
  - 【5大保護防壁】（BackingStore、Phase7引き継ぎ101件、Pre-Host11件、const定数、ViGEm依存）を厳格に保護。
  - 全リポジトリ走査で呼出元0件が完全に証明された旧静的ラッパーのみを行削除し、`ScpUtil.cs` を安全にスリム化。
  - 全自動テスト再実行で完全合格を証明し、`Phase6-Completion-Report.md` をもって Phase6 の完全成功を宣言。

---

### Phase6-Step13: `.cs` ファイル配置の統一【完了】
- **進捗率**: **100%（13-1〜13-5、44/44 ファイル。2026-09-21 確定）。ビルド・テスト・Release ビルドとも成功。13-6 は Phase6 範囲外への引き継ぎ**
- **確定方針**: 現 Step は**案2**（インターフェースを `DI/` に一元化）、将来は案4（層別再編）。移動は `git mv` のみ・内容変更なし。配置ルール（R1〜R7）は `copilot-instructions.md` §3.4 に明文化。
- **ステージ**: 13-1（20件、Step2-PR-4 の前）／13-2（8件）／13-3（1件）／13-4（6件）／13-5（9件）／13-6（Phase6 範囲外への引き継ぎ）。
- **検証**: 各ステージで `git diff --cached -M100% --name-status` が全行 `R100`、ビルド・テスト全件成功、起動スモーク。

---

## 3. ガードレール対策の適用状況

| ガードレール項目 | 対象ステップ | 対策設計の内容 | 適用状況 |
| :--- | :--- | :--- | :---: |
| **ホットパス性能維持** | Step 2, 3, 4 | ゼロアロケーション（GC Alloc = 0）、BackingStore 配列直接アクセス維持、ログ出力禁止 | 計画書に仕様明記済み |
| **SSOT・BackingStore保護** | Step 2, 5, 8, 12 | 各DIサービスは独自フィールドを持たず BackingStore を正本として参照。物理削除を禁止 | 計画書に仕様明記済み |
| **Halt 停止・排他制御保証** | Step 2, 6, 8 | プロファイル適用・出力デバイス切替時のポーリングスレッド停止・直列化を維持 | 計画書に仕様明記済み |
| **Pre-Host 境界保護** | Step 7, 10, 12 | DIコンテナ構築前の初期ログ・多重起動・driverinstall分岐（11件）に手を加えず温存 | 計画書に仕様明記済み |
| **循環依存の回避** | Step 2, 8 | `ProfileApplicationService` ⇄ `ControlService` の双方向注入を禁止しイベント・委譲解決 | 計画書に仕様明記済み |
| **技術的負債コメント明記** | Step 3, 5, 6, 12 | Phase7引き継ぎ分（101件）や非推奨APIに正規参照先・理由・予定フェーズを付与 | 計画書に仕様明記済み |

---

## 4. 全体プロジェクト（Phase 0 〜 Phase 7）における進捗位置づけ

```text
【全体進捗マップ: Phase 0 〜 Phase 7】

[完了] Phase 0: 準備・DIコンテナ選定・ベースライン確立        ( 5%) ───┐
[完了] Phase 1: ActionサブシステムのDI化                       ( 8%)    │
[完了] Phase 2: 入力変換層シム・フォールバック解消             ( 7%)    ├─ 【約50% 完了済み】
[完了] Phase 3: 基盤サービス層（Settings/Env/Path等）のDI化    (10%)    │   基盤・サービス・
[完了] Phase 4: UI・ViewModel層のDI化（Pattern A/B/C）         (10%)    │   SSOTの土台が完成
[完了] Phase 5: プロファイル設定同期・SSOT確立・Issue 7根絶   (10%) ───┘
───────────────────────────────────────────────────────────────────────────
[現在地] Phase 6: 全域 Global 直参照根絶 ＆ Pure DI 化
   ├─ [完了] Step 1: 全域精密監査（約404箇所の実参照を1行単位で完全特定） ( 5%) ── ★現在ここ！
   └─ [計画完了・施工待ち] Step 2〜12:
        - Step 2〜10: 約404箇所の安全な Pure DI 置換施工      (10%) ───┐
        - Step 11: 2層構造総合検証（自動テスト ＋ 実機検証）    ( 5%)    ├─ 【残り約35〜40%】
        - Step 12: 呼出元0件旧シムの物理削除                    ( 2%)    │   確立された設計図に
────────────────────────────────────────────────────┤   沿った施工と、
[未着手] Phase 7: 最終本丸の解体                                        │   最後の本丸解体
   ├─ `Mapping.cs`（8,500行）のインスタンス化・ドメイン分割    (13%)    │
   └─ `ScpUtil.cs`（Global）の物理的消滅（プロジェクトから完全削除）( 5%) ───┘
```

- **全体進捗率（工数・作業ボリューム）**: **約 60〜65% 到達**
- **設計・不確実性・リスク消化度**: **約 75〜80% 突破**  
  （最大の難所であった Issue 7 / SSOT 確立をクリアし、Phase6 の全約404箇所の移行先が 1 行単位で確定したため、手戻り・未知のリスクは実質ゼロ）
- **Phase8の新設（2026-09-23）**: 上記マップは Phase 0〜7 時点のものだが、Phase7 完了後の独立フェーズとして「Phase8: Controls/SpecialActions統合ディスパッチ」を新設した（詳細は §6.7、計画書 `Phase8-Unified-Trigger-Dispatch-Plan.md`）。工数比率は Phase7 完了後に確定するため、本マップには未反映。

---

## 5. 直近の次アクション

1. **Step3-4**（`SetCurveAndDeadzone` のスティック・トリガー系前半）に着手する。`Phase6-Step3-Plan.md` §3 のバッチ構成に従う。Step3-3は対象コード自体がAbs Mouse機能削除に伴い消滅したため、コミット・確認作業は不要（詳細は §6.8）。
2. Step3 以降は、確定済みの順序（Step3 → Step4 → Step5 → ... → Step10 → Step10b → Step11 → Step12 → Phase7 → Phase8）で進める。Phase8（Controls/SpecialActions統合ディスパッチ）は Phase7 完了後の独立フェーズとして新設済み（詳細は §6.7）。

---

## 6. 次セッションへの引き継ぎ（2026-09-23、Step3-4 着手前）

### 6.1 現在地
- 完了: Step1（詳細監査）、Step2（`ControlService.cs`、66 ID）、Step3-1・Step3-2（`Mapping.cs`、契約整備＋非ホット12件。ともにビルド・テスト・実機確認済み）、Step13（配置整理 44件）。
- Step3-3は対象消滅（Abs Mouse機能削除、§6.8参照）。Step3の残実参照数は150→145に更新。
- 次: Step3-4（`SetCurveAndDeadzone` のスティック・トリガー系前半、約20件）。その後 Step3-5〜3-7、Step4〜Step10、Step10b（1ファイル1型）、Step11（総合検証）、Step12（旧シム削除）。Phase6完了後は Phase7、その後 Phase8（新設、§6.7）。

### 6.2 確定済みの決定事項（要約）
- **Step2**: D1（`Func<IOutputSlotService>` の遅延解決）、D2（新規コンストラクタ引数はすべて必須の Pure DI）、D3（`IVirtualKBMLifecycle`）、O1=B／O4=B-2（`IProfileSlotApplier`）、O2=C／O3=A（スロット上限のサービス化と段階移行）。詳細は `Phase6-Step2-Plan.md` §0.3.2。
- **配置**: 現 Step は案2（契約は `DI/`、実装は `DS4Control/Services/`、Actions は `Actions/`）、将来は案4。`copilot-instructions.md` §3.4（R1〜R7）。
- **1ファイル1型**: Step10b（Step10 と Step11 の間、Step13 の後）。規約 §3.3 原則1 に過渡期ルール（既存の複数型ファイルは Step10b まで分割せず、新しい型は必ず新規ファイルに作る）。

### 6.3 作業の進め方（確立した手順）
- **開始時**: 計画書の行番号・件数は古くなっている。各 PR の着手時に、`git` ベースの grep 台帳を再作成する（`DI-App-Wide-Migration-Plan.md` §6.11）。
- **選択肢がある場合**: メリット・デメリット・推奨の形で提示し、モデル図（`docs-forDIMG/Model-Diagram/` の01〜04）を基準に判断する。モデル図の変更が必要になった場合は、指摘して指示を仰ぐ。
- **1 PR = 1 機能**: 実装 → 静的検証（Roslyn の意味診断を変更前後で比較し、新規エラー 0 件を確認）→ ユーザーがビルド・テスト・（実機）を確認してコミット。
- **成果物の提供**: 変更ファイルをファイル本体で提供し、ファイル名の直前に `[新規]`／`[更新]` を付けて列挙する。ファイル移動は `git mv` のコマンド一覧で提供する。`urls.txt` はユーザーのバッチ処理で更新するため、更新しない。
- **各 PR で更新する文書**: 対象 Step の計画書（実施内容と付録の記録）、`Phase6-Status.md`。実機確認を先送りする場合は `Phase6-Step11-Plan.md` §3.3 の先送り台帳に登録する。

### 6.4 テスト作成・コード作成で守るルール（Step2 で得た教訓）
1. **`Global` の static 状態に依存するテスト**: `Global.ProfileSettingsServiceInstance` を差し替えたまま戻さない既存テストがあるため、参照先に依存するテストは明示的に配線し、終了後に復元する（`Phase6-Step2-Plan.md` 付録 B.11）。
2. **インスタンスが保持する状態**（例: `TouchpadActiveArray`）は、`new` した別インスタンスではなく DI の Singleton で検証する（同 B.9）。
3. **静的初期化の循環**: `static` の初期化子（静的コンストラクタを含む）から、静的コンストラクタを持つ型（`Global` など）のメソッドや非 const メンバを呼ばない。初期化の順序によって、途中の値（0 や null）が確定する恐れがある（同 B.13）。
4. **インターフェースにメンバを追加したとき**: 実装するテスト用のモックがないか、全テストファイルを含めて確認する（PR-2・PR-5 で見つかった）。
5. **`ControlService` のテスト生成**: `ControlServiceTestFactory`（引数を型から解決）を使う。引数が増えてもテスト側の修正が不要。
6. **ホットパス**: 新しい取得系のシムは、割り当て・ログ・キャッシュを行わない（`ControlServiceHotPathAllocationTests`）。

### 6.5 持ち越し事項（Step2 付録 B.12）
外部呼び出し元54箇所の移行（Step5/8/9/10、Step12 で互換シム削除）、K1（`IProfileApplicationService.ApplyProfile` の `deviceIndex >= 4`）、C2-02（Step12 で削除判断）、`OutputSlotService` などの逆依存（Step5）、後始末のない既存テスト3ファイル、処理時間比較と先送りの実機確認（Step11 §3.3）、**K2（新規、2026-09-22）**: `Global.GetProfileActionIndexOf`（`ScpUtil.cs` 3494〜3499行）およびその移植先 `ProfileActionProvider.GetProfileActionIndexOf`（Phase6-Step3-1で新設）は、`Dictionary.TryGetValue` が未検出時に `out` 引数を `default(int)`（＝0）で上書きする仕様により、アクション名が未検出の場合も `-1` ではなく `0` を返す（インデックス0の定義済みアクションと区別がつかない）。Step3-1では既存挙動の忠実な移植として温存し、修正の要否はテスト（`Phase6Step3ContractExtensionTests.GetProfileActionIndexOf_SharesStateWithGlobal`）作成時に判明した。呼び出し元（`Mapping.cs` 等）が本当に `-1` を「未検出」判定に使っているか、Step3 のいずれかのバッチ着手時に確認し、対応要否を判断する。

**K3（新規、2026-09-22、Mapping.cs のバグ調査で判明・恒久対応が必要）**: `PlayMacro`（`Mapping.cs`）の二重実行防止ガードは、本来 `IsMacroRunning`（`ActionInstanceState`）一本で「トリガー成立後、その処理が実行中かどうか」を判定すべきところ、SpecialAction 経由（`action != null`）にしか効いておらず、Controls タブの直接マクロ割り当て（`action == null`）は素通りしていた（入力ポーリングのたびに新しい `Task` を生成しようとし、216ms 間隔で約64回もの無駄な `Task` 生成が発生することを実機ログで確認）。暫定対策として、`control` 単位の独立したフラグ `macroDispatchInFlight`（+`macroDispatchLock`）を新設し、Task 生成前に同期的にチェック・設定する形で二重実行を防止した（`MappingPlayMacroDispatchGuardTests.cs` で検証）。

**恒久対応の申し送り**: これは対症療法であり、根本原因は「トリガー判定層」と「マクロ実行層」が `Mapping.cs` 内で分離できていないことにある。`docs-forDIMG/Model-Diagram/02-Layer-Architecture-Diagram.md`・`03-Class-Interface-Diagram.md` が示す理想構造（§3.3 の 2-d マクロの分解／3-b KBM出力）では、マクロの「実行」は Controls 由来か SpecialActions 由来かに関わらず単一の実行層（`IVirtualKBM` 経由の逐次送出）に一本化され、二重実行防止もその単一の実行層で一元的に行われる想定である。Phase7（`Mapping.cs` 完全 instance 化、`DI-App-Wide-Migration-Plan.md` §6.9）で、今回の `macroDispatchInFlight` を含めて再設計し、`action`（SpecialAction）の有無で分岐する現在の二重ガード構造を解消すること。

**K4（新規、2026-09-23、対応不要と判断・記録のみ）**: `Mapping.cs` の `MapCustom`（Stage3）・`MapCustomAction` は、毎入力レポートごとに、プロファイルで有効なアクション名一覧をループし、各名前について `IProfileActionProvider.GetProfileAction` で辞書（`profileActionDict`）を都度検索して `SpecialAction` 定義を取り直している（プロファイルが変わらない限り毎回同じ結果になる、本質的に無駄な再検索）。この呼び出しに付いていた `[DI]` Trace ログが実機ログを埋め尽くす問題は既に解消済み（`ProfileActionProvider.GetProfileAction` からログを削除、`Phase6-Step3-Plan.md` §2.4.1・§3.1参照）。残る「辞書検索そのものの無駄」については、Step3内で暫定キャッシュ対応を行うか、Phase8まで先送りするかを比較検討した結果、**先送りを決定した**。理由: (1) 暫定対応には新規キャッシュ設計・無効化ロジック（プロファイル読込時の `CalculateProfileActionDicts` へのフック等）・専用テストが新たに必要になる、(2) この暫定コードは Phase7 のinstance化変換対象が1つ増えるだけで、Phase7側の作業量そのものは減らない、(3) Phase8（`Phase8-Unified-Trigger-Dispatch-Plan.md` §2 の `TriggerBinding`、プロファイル読込時に1回だけ解決してリストを作る設計）で最終的に置き換えられ、暫定実装は使い捨てになる、(4) 残存する実害はO(1)辞書検索を毎レポート約40回行うだけの小さなCPUコストのみで、ログ問題（実害の本体）は既に解消済み。Step3-2で対応済みの `GetProfileAction` 呼び出し箇所（`ctrl.ProfileActionProvider.GetProfileAction(...)`）は、このK4決定により現状のまま変更しない。

**K5（新規、2026-09-23、解決済み）**: Step3-2の実機確認と並行して発見された `SpecialActionsListViewModel.SortActions`（Controls/SpecialActionsタブの一覧ソート順）のバグを修正した。決定: B1採用（View側が個別に持っていた二重のソート状態を完全に削除し、ViewModel側に一元化）、G1（Trigger列の第2キーは直前の実際の方向を維持する）、アクション名を最終タイブレークとして追加。この対応中に、`GetColumnComparison` のTrigger/Action列判定で使われる case ラベルの不一致という別バグも発見し、あわせて修正した。ビルド・テスト・実機確認とも問題なし。

### 6.6 Step3 着手時の確認事項
- （着手前の想定。実地確認で150件に更新済み。下段参照）対象は `Mapping.cs` の Global 直接参照。Phase7（`Mapping.cs` の完全 instance 化）の下地として、静的結合を段階的に減らす方針（`DI-App-Wide-Migration-Plan.md` §5.5・§6.9）。
- `Mapping.cs` は `Global` 以外の静的結合（`Program.rootHub` 直接参照1件、Service Locator 6件）を含む。いずれも Step3 の対象外（`Phase6-Step3-Reality-Check-Ledger.md` §3.3）。
- **2026-09-21 実地突き合わせ完了**: `Phase6-Step3-Reality-Check-Ledger.md` を作成・コミット済み。実参照は150件（旧計画の「12件」「115件」から確定値に更新）。`Phase6-Step3-Plan.md` を全面改訂し、論点1〜4の決定（C／S3／新設`IDisplayCoordinateService`ほか／機械置換不可2項目の選択肢）を反映済み。
- **2026-09-23 Step3-1・Step3-2 完了**: いずれもビルド・テスト・実機確認済み（コミット済み）。
- **2026-09-23 Step3-3 → 対象消滅、Abs Mouse機能削除（確定）**: Step3-3実装・ビルド・テスト・実機確認・コミット完了直後、Abs Mouse機能（ボタン割当・タッチパッドAbsoluteMouseモード・共有基盤`IDisplayCoordinateService`）をユーザー承認のもと削除。削除後のビルド・テスト・実機確認・コミットも完了済み（詳細は §6.8）。次はStep3-4。
- ファイルの配置は Step13 で変わっている（`ActionManager` など、Action 系は `Actions/` へ移動済み）。パスは現行の HEAD で確認する。

### 6.7 Phase8の新設（Controls/SpecialActions統合ディスパッチ）

**経緯（2026-09-23）**: ユーザーから、Controls/SpecialActionsタブの「トリガー成立→処理実行」ディスパッチを統合できないかとの提案・調査依頼があった。調査の結果、コードベースには未配線の `IActionBinding` 系（系統A）と、実際に稼働中の `Actions.Action` 系（系統B）という2系統が並存していることが判明した。統合は実現可能と判断し、Phase7完了後の独立フェーズ「Phase8」として新設した。

**成果物**: `docs-forDIMG/MadeByAgent/Phase8-Unified-Trigger-Dispatch-Plan.md` を新規作成。`DI-App-Wide-Migration-Plan.md` にも §6.13 等で組み込み済み。

**決定事項（D1〜D5、2026-09-23確定・実装着手はPhase7完了後）**:
- D1: A2採用（構造は系統A、中身は系統Bから移植。Macro/プロファイル切替/プログラム起動の3種類は既に `IOutputAction` 実装済みと判明し移植ほぼ不要、Keyタイプのみ委譲に書き直す方針をStep2で確定）。
- D2: B1（汎用 `ComboTriggerInputAction` で統合）。
- D3: C1（Strangler Fig パターンで段階的に置き換え）。
- D4: D4-1（独立フェーズとし、Phase7完了後に着手）。
- D5: E2の変形（系統Aは削除せず本採用、系統Bを段階的に削除）。

**モデル図への影響**: 現時点ではモデル図（`docs-forDIMG/Model-Diagram/` 01〜04）の変更は未実施。変更が必要になった場合はStep2着手時にユーザーへ指摘し指示を仰ぐ。

### 6.8 Abs Mouse機能の削除（copilot-instructions.md §2.2 改訂を伴う機能廃止）

**経緯（2026-09-23）**: Step3-3完了・実機テスト検討の過程で、ユーザーから「ボタン割当のAbs Mouse機能（十字キー等に絶対座標マウス移動を割り当てる機能）は使用頻度が低く、構造をシンプルにするため削除したい」との提案があった。調査の結果、以下が判明した。

- 座標変換基盤（`absUseAllMonitors`／`TranslateCoorToAbsDisplay`／`PrepareAbsMonitorBounds`、Step3-1で`IDisplayCoordinateService`へ集約済み）は、ボタン機能だけでなく**タッチパッドのAbsolute Mouseモード**（`TouchpadOutMode.AbsoluteMouse`、`TouchpadAbsMouseSettings`）とも共用されていた。
- ユーザーはタッチパッド側のAbsolute Mouseモードも含めて削除する判断を下し、結果として共有基盤の`IDisplayCoordinateService`自体、および出力側の`IVirtualKBM.MoveAbsoluteMouse`（両機能の唯一の消費者）も丸ごと不要になった。
- DS4Windowsアプリ自身のウィンドウDPIスケール処理（`WindowPlacementHelper.cs`）とは無関係であることを確認済み。

**`copilot-instructions.md` §2.2の改訂（ユーザー承認・2026-09-23）**: 「DI移行そのものでは機能を削除しない」という原則に、「ユーザーが明示的に承認した機能廃止（別途Issue化）は対象外。エージェント自身の判断のみでの削除は不可。ただし実装中に使用頻度が極めて低い機能を見つけ、削除に構造単純化等の明確なメリットが見込める場合は削除を提案してよい」という例外規定を追加した。今回のAbs Mouse機能削除はこの規定に基づく第一号案件。

**削除範囲**:
- ボタン機能: `X360Controls.AbsMouseUp/Down/Left/Right`、`ButtonAbsMouseInfo`、`Mapping.cs`の`AbsMouseOutput`構造体・`GetAbsMouseMapping`メソッド・`MapCustom`内の該当ブロック（**Step3-3で修正した箇所そのもの**）、`BindingWindow`の「Abs Mouse」タブ、`ProfileEditor`の「Absolute Mouse Options」設定UI
- タッチパッド機能: `TouchpadOutMode.AbsoluteMouse`、`TouchpadAbsMouseSettings`、`MouseCursor.cs`の`TouchesMovedAbsolute`/`TouchCenterAbsolute`、`ProfileEditor`のタッチパッド「Absolute Mouse」タブ
- 共有基盤: `IDisplayCoordinateService`／`DisplayCoordinateService`（インターフェース・実装ごと削除）、`IVirtualKBM.MoveAbsoluteMouse`（全実装：`FakerInputHandler`/`SendInputHandler`/`OutputKBMHandlerAdapter`/`InputMethods`）
- 未使用だったUI: `MainWindow`の「Abs Display Monitor」設定（`AbsMonitorSettingEDID`、実際にはUI露出していたことが調査で判明）、`AppSettingsDTO.AbsRegionDisplay`
- プロファイルXML側の別系統の永続化（`ProfileDTO.cs`の`TouchpadAbsMouseSettingsSerialize`/`AbsMouseRegionSettingsSerializer`、DOM解析とは別のDTO高速パス）
- 関連テスト（DI配線検証・サブ設定バブリング検証）、ローカライズ文字列4言語分

**既存プロファイルへの影響（ユーザー承認済み）**: 該当のボタン割当は名前ベースのXML解析が失敗し自動的に`Unbound`にフォールバックする（追加の移行コードは実装していない）。`AbsMouseRegionSettings`/`TouchpadAbsMouseSettings`のXML要素は対応する読込コードごと削除したため、存在しても無視される。新バージョンで一度でも保存すると、これらの設定はファイルから消える（不可逆）。

**モデル図への反映**: `03-Class-Interface-Diagram.md`（`IDisplayCoordinateService`クラス定義・実装関係を削除、注釈追記）、`04-Service-Lifecycle-Spec.md`（登録サービス一覧表から該当行を削除、注釈追記）を更新済み。`01`/`02`図には元々記載がなく変更なし。

**ビルド確認（確定・2026-09-23）**: ビルド・テストビルド・テスト実行とも成功。実機確認も完了（コントローラー接続確認済み）：(1) Abs Mouse機能を使用していたプロファイルの読込・保存で該当設定が無警告でUnbound/初期値にフォールバックすることを確認、(2) タッチパッドの他の出力モード（Mouse／Controls／MouseJoystick／Passthru、インデックス詰め後）が引き続き正常動作することを確認、(3) Controlsタブのバインディングウィンドウから「Abs Mouse」タブが消えていること、Otherタブから「Absolute Mouse Options」「Abs Display Monitor」設定が消えていることを確認。コミットしてリモートリポジトリに反映済み。

**削除漏れの修正（2026-09-23、ユーザーのビルドエラー報告により発覚）**: `ControlService.PreLoadReset(int ind)` 内に `Mapping.absMouseOutputState[ind].Reset();` の呼び出しが1箇所残存しており、`absMouseOutputState`フィールド削除に伴いビルドエラー（CS0117）となっていた。該当呼び出しを削除して解消。この修正を反映した上で上記のビルド・テスト・実機確認が完了している。