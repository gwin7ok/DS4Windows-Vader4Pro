# フェーズ6計画書: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

作成日: 2026-09-09  
改定日: 2026-09-26（Step7b［プロファイル編集の即時反映廃止］の追加と完了）／2026-09-20（Step13［.cs ファイル配置の統一］、Step10b［1ファイル1型の全数是正］を新設）／2026-09-19（Step1再監査および全ステップ［Step2〜Step12］確定実装計画書に基づく全面改訂）  
対象ブランチ: `For-DI-migration-work`  
前フェーズ: `Phase5-Plan.md`（Step1〜15、ドメイン集約型・完了済み）  
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照調査・各ステップ計画書:  
  - `Phase6-Step1-Completion-Summary.md`（Step1詳細再監査・全762参照走査記録）  
  - `Phase6-Step1-Core-Reference-Evidence.md` / `Phase6-Step1-UI-Reference-Evidence.md`  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step13-Plan.md`（Step10b を含む各ステップ確定実装計画書）  
  - `Phase6-Unified-Deployment-Plan-and-Safe-Phased-Transition-Work-Plan.md`（Step13 の参照原案）  

---

## 0. 目的とスコープ

### 0.1 目的
**完全にDI化された4層構造（入力監視層／信号変換層／信号出力層／UI層）のアプリケーションへ移行し、`Global` 静的クラスへの直接依存を根絶して、保守性・テスタビリティ・新機能追加の容易性を恒久的に向上させること。**

Phase1〜5により、DIコンテナ基盤の構築、主要サービスの抽出、および `BackingStore` と連動した SSOT（信頼できる唯一の情報源）の確立（Issue 7 根絶）が完了した。  
Phase6 では、アプリケーション全域に残存する呼び出し元側の `Global.*` 直接参照（および `using static DS4Windows.Global;` 経由の無修飾参照）をすべてコンストラクタ経由の Pure DI サービス呼び出しへ完全置換し、呼出元0件となった不要な旧静的シムを安全に物理削除して `ScpUtil.cs` のスリム化を果たす。

### 0.2 スコープに含むもの（Step1再監査による確定値）
旧計画時の「暫定193件（`Global.` 修飾子のみの単純grep）」から、Step1 の精密再監査を経て、無修飾参照を含む**実利用参照式 約404箇所（除外定数やPhase7引き継ぎを含めると500箇所以上）**の全容が確定した。

| 対象ステップ | 対象ファイル | 実参照式数 | 採用設計方針・概要 |
| :--- | :--- | :---: | :--- |
| **Step 2** | `ControlService.cs` | 66箇所 | Pure DIコンストラクタ拡張、ホットパスゼロアロケーション維持、KBMライフサイクル集約、循環依存回避 |
| **Step 3** | `Mapping.cs` | 10箇所 | **案A採用**: 局所的引数渡し（10件）＋ 超高頻度ホットパス（101件）のPhase7完全引き継ぎ台帳化 |
| **Step 4** | `Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs` | 81行（2026-09-24 に現行コードから再集計。**完了**） | **採用**: Pure DIコンストラクタ引数注入（フォールバックなしの必須引数）＋ `MouseWheel.cs`（6行）正式統合、1000Hzホットパス保護。契約の追加なし |
| **Step 5** | `OutputSlotService.cs` / `MainWindow.xaml.cs` | 孤立配列系＋UDP診断1行（**完了**、2026-09-24） | **選択肢B採用**: 孤立配列 `_deviceTypes[]` と Get/SetOutputDeviceType を削除、出力デバイス三態SSOT台帳確立、UDP診断是正 |
| **Step 6** | `ProfileEditor.xaml.cs` / `IOutputSlotService` | 2行追加＋TODO書き換え（2026-09-24 実装完了） | **案1採用（2026-09-24 改訂）**: マッピング一覧機種表示追従バグ是正 ＋ 呼出元0件の未接続APIを温存し Step10 で接続 ＋ 実働ホットスワップ温存 |
| **Step 7** | `App.xaml.cs` | 39件（温存15、2026-09-24 再集計。**完了**、2026-09-25） | **選択肢1採用**: Pre-Host領域（12件）厳格保護 ＋ Post-Host Composition Root解決 ＋ 既存サービス集約 |
| **Step 7b** | `ProfileEditor.xaml.cs`, `MainWindow.xaml.cs`, `ProfileSettingsViewModel.cs` ほか | 編集スロット固定＋`targetDevice` 導入（**完了**、2026-09-26） | プロファイル編集の即時反映を廃止（ユーザー承認の機能廃止、K4-3）。編集は作業スロット、実機は `targetDevice`、Save／Apply は同じ再適用（違いは画面を閉じるかどうか） |
| **Step 8** | `ProfileEditor.xaml.cs` | 60箇所 | **案3-A採用**: 段階的MVVM移設（ロジックを `ProfileSettingsViewModel` へ集約）＋ `MainWindow` 解体推進 ＋ 契約差B8, B9, B10吸収 |
| **Step 9** | 主要4大ViewModel (`Settings`, `MainWindow`, `TrayIcon`, `ProfileSettings`) | 79箇所 | **選択肢1採用**: 中枢4大ViewModel集中 ＋ 機能カテゴリ別段階的移行 ＋ Pure DI徹底（残存小型VMはStep10へ引き継ぎ） |
| **Step 10** | 残存小型View (11) & 小型ViewModel (12) & `PresetOption` | 72箇所 | **選択肢A採用**: 機能ドメイン別3グループ分割 ＋ View/ViewModelペアPure DI化 ＋ 全域直参照ゼロ化 |
| **合計** | **ソリューション全域** | **約404箇所** | **全呼び出し元の Pure DI 完全移行** |

### 0.3 スコープに含まないもの（明示的除外・保護対象）
1. **仮想コントローラー出力バックエンド（ViGEm）の直接操作**: `ControlService.cs` や `ScpUtil.cs` 内の `Nefarius.ViGEm.Client` 直接依存、およびダウンキャスト配線は、仮想バス刷新イニシアチブ（`IVirtualControllerBackend` 構想）のスコープとし、Phase6 では動作安定性を最優先して温存する。
2. **`Mapping.cs` の完全インスタンス化（101箇所）**: 全体計画書 §5.5 および Step3 合意（案A）に基づき、8,500行に及ぶクラス全体のドメイン分割・インスタンス化は Phase7 として独立させる（Step3にて完全台帳化済み）。
3. **Pre-Host ブートストラップ処理（12箇所。2026-09-24 に Step7-0 で再集計）**: 明示的な `AppHost.CreateHost()`（`App.xaml.cs:298`）より前に実行される初期ログローテーション（`appdatapath`, `LogMaxArchiveFiles`, `LogMinLevel`）、多重起動判定、および `-driverinstall` 分岐は、ブートストラップコードとして静的のまま温存する。なお `-driverinstall` 分岐の `Global.Load()` は、`AppHost.GetService` の暗黙構築によりホストを作るため、「DIコンテナが存在しない」という旧記載は正確ではない（`Phase6-Step7-Plan.md` §0.4）。
4. **`BackingStore` の正本データ構造**: DI サービスが SSOT として参照しているため、Phase7 のドメインエンティティ化まで物理削除を禁止し完全保護する。
5. **真の定数（`const`）および純粋計算ユーティリティ**: `MAX_DS4_CONTROLLER_COUNT`, `TEST_PROFILE_INDEX`, `ASSEMBLY_RESOURCE_PREFIX` などの定数、および `Clamp`, `getTransitionedColor` などの状態非保持ユーティリティは共通インフラとして温存する。
6. **テストファイル内の比較用 `Global.*` 参照**: 新旧値の同一性検証用テストコードは移行対象外とする。

---

## 1. 実施ステップ一覧（全15ステップ（2026-09-26 に Step7b を追加）、ドメイン集約型）

```text
【Phase6 全15ステップ構成】
[完了] Phase6-Step1: 詳細監査と対象確定（全762参照走査、実参照約404箇所特定・分類確定）

── [ドメイン1: コア層（信号変換・入力監視境界）] ──
├─ Phase6-Step2: ControlService.cs のGlobal直参照解消（66箇所、最優先・最難関）
├─ Phase6-Step3: Mapping.cs の局所的引数渡し ＆ Phase7引き継ぎ台帳化（実装10箇所 / 引き継ぎ101箇所）

── [ドメイン2: 信号出力層] ──
├─ Phase6-Step4: Mouse系（Mouse / MouseCursor / MouseWheel）のPure DI化（81行、2026-09-24 完了）
├─ Phase6-Step5: OutputSlotService の孤立配列撤廃 ＆ UDP診断是正（2026-09-24 完了）
└─ Phase6-Step6: ProfileEditor 出力切替表示追従バグ是正 ＆ 未接続API整理（2026-09-24 実装完了）

── [ドメイン3: 起動・UI層] ──
├─ Phase6-Step7: App.xaml.cs Post-Host領域のPure DI化（39件、温存15件［Pre-Host12件を含む］。2026-09-25 完了）
├─ Phase6-Step7b: プロファイル編集画面の即時反映廃止（編集は作業スロット、保存・適用時に一括反映。2026-09-26 完了）
├─ Phase6-Step8: ProfileEditor.xaml.cs の段階的MVVM移設 ＆ MainWindow解体推進（60箇所）
├─ Phase6-Step9: 主要4大ViewModelのPure DI徹底 ＆ カテゴリ別移行（79箇所）
├─ Phase6-Step10: 残存小型UI・小型ViewModel・補助クラスのドメイン別ペアPure DI化（72箇所）
└─ Phase6-Step10b: 1ファイル1型（原則1）の全数是正（55ファイル・新設203ファイル。Step10 の後・Step11 の前。Step13 の後に実施）

── [ドメイン4: 検証・仕上げ] ──
├─ Phase6-Step11: 2層構造・階層化総合検証（自動回帰テスト全件 ＋ Vader 4 Pro/DS4/DualSense 実機検証）
└─ Phase6-Step12: 呼出元0件旧静的シムの安全防壁付き物理削除 ＆ Phase6最終完了判定

── [ドメイン5: 横断（構造整理）] ──
└─ Phase6-Step13: .cs ファイル配置の統一（git mv のみ・挙動変更なし。13-1 は Step2-PR-4 の前、13-2〜13-5 は Step2 完了後・Step3 着手前が推奨。番号は末尾だが実施時期は途中から始まる）
```

---

## 2. 各ステップの詳細内容と確定方針

### Phase6-Step1: 詳細監査と対象確定【完了】
- **実績**: ソリューション全体で 762 箇所の参照シンボルを完全走査。重複ゼロ・漏れゼロを数学的に証明。
- **成果物**: `Phase6-Step1-Completion-Summary.md`, `Phase6-Step1-Core-Reference-Evidence.md`, `Phase6-Step1-UI-Reference-Evidence.md`, `Phase6-Step1-ABC-Classification.md`, `Phase6-Step1-Unique-ID-Manifest.md` 等の10大監査文書を完備。

---

### 【ドメイン1: コア層】

#### Phase6-Step2: `ControlService.cs` のGlobal直参照解消（最優先・最難関）
- **対象**: 実参照式66箇所（除外8箇所）。
- **確定方針**: コンストラクタ引数注入の拡張（Pure DI）。
- **重点対策**:
  - **ホットパス性能維持**: `On_Report` 入力ループ内でのゼロアロケーション（GC Alloc = 0）および BackingStore 配列直接アクセスの徹底。
  - **循環依存の回避**: `ProfileApplicationService` を直接注入せず、イベント連動または低レベル適用APIで解決。
  - **6分割サブPR（マイクロステップ）**: PR-1（環境/パス）→ PR-2（出力スロット/プロファイル）→ PR-3（KBM初期化）→ PR-4（ホットパス条件分岐）→ PR-5（毎レポート最頻度経路）→ PR-6（`using static` 削除・最終確認）。

#### Phase6-Step3: `Mapping.cs` の局所的引数渡し ＆ Phase7引き継ぎ台帳化
- **対象**: 実装対象10箇所、Phase7引き継ぎ対象101箇所、除外4箇所。
- **確定方針（案A）**:
  - 非ホットパス（画面座標、設定保存・ロード等）の10箇所のみ、メソッド引数渡し方式で安全に解消。
  - `SetCurveAndDeadzone`（34件）や `outputKBMMapping`（29件）等のホットパス101箇所は、シグネチャ肥大化とスタックオーバーヘッドを防ぐため、全件ID付きでカタログ化し Phase7 へ正式引き継ぎ。

---

### 【ドメイン2: 信号出力層】

#### Phase6-Step4: マウスエミュレーション系（`Mouse`, `MouseCursor`, `MouseWheel`）のPure DI化
- **対象**: 実参照式73箇所（`Mouse`: 47, `MouseCursor`: 20, `MouseWheel`: 6、除外2）。
- **確定方針（推奨案）**:
  - `MouseWheel.cs`（6件）を正式統合し、マウスサブシステムを一挙に完全解決。
  - 各インスタンスクラスのコンストラクタで DI サービスを受け取り `readonly` フィールドに保持。1000Hz ポーリング時のオーバーヘッドを極小化。
  - `DS4Device.cs` からサービスインスタンスを直接伝搬。

#### Phase6-Step5: `OutputSlotService` 全面SSOT統合 ＆ 孤立配列完全撤廃
- **状態**: **完了（2026-09-24）**。詳細は `Phase6-Step5-Completion-Report.md`。
- **対象**: `MainWindow.xaml.cs:1432` のUDP診断コマンド、`OutputSlotService.cs`／`IOutputSlotService.cs` の孤立配列系。
- **確定方針（2026-09-24 改訂）**:
  - バグの温床であった孤立配列 `_deviceTypes[]` を物理的に完全削除。
  - `GetOutputDeviceType` / `SetOutputDeviceType` は**削除**（旧方針の「委譲＋`[Obsolete]`」は、`UnplugSlot` 経由で永続設定へ `None` を書き込む危険があるため不採用）。
  - 出力デバイス設定の「三態（永続設定 `OutContType` / 実行時接続 `ActiveOutDevType` / UI一時 `OutDevTypeTemp`）」のSSOT台帳を確立。`OutDevTypeTemp`／`ActiveOutDevType` の裏づけの `Global` 静的配列は温存し、所有権の移動は Phase7 へ引き継ぐ。

#### Phase6-Step6: ProfileEditor 出力切替表示追従バグ是正 ＆ 未接続API整理
- **状態**: **完了（2026-09-24）**。詳細は `Phase6-Step6-Completion-Report.md`。
- **確定方針（2026-09-24 改訂）**:
  - `ProfileEditor.xaml.cs` の `Reload()` と `RefreshEditorBindings()`（プリセット適用後）で `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を明示的に呼び、編集画面を開いたとき・プリセット適用後のマッピングボタン表記（A/B/X/Y ⇄ Cross/Circle）の食い違いを解消。
  - `IOutputSlotService` の呼出元0件API（`PluginSlot`, `UnplugSlot`）は、Phase5-Step12 で UI 接続のために作られたが未接続と判明したため、非推奨化せず温存し TODO を書き換え（決定1＝案R）。UI・UDP コマンドからの接続は `Phase6-Step10-Plan.md` §8 で実施。
  - `ScpUtil.cs:PostLoadSnippet` による実働ホットスワップ機構は安定性最優先で温存。

---

### 【ドメイン3: 起動・UI層】

#### Phase6-Step7: `App.xaml.cs` Post-Host領域のPure DI化
- **状態**: **完了（2026-09-25）**。Step7-0〜7-5。詳細は `Phase6-Step7-Plan.md`、`Phase6-Step7-Completion-Report.md`。Post-Host の置き換え38件、TODO 付き温存1件（P7-38）、`App.xaml.cs` の `Global.` 参照は54→16件。実機確認済み（3項目は Step11 へ先送り）。
- **対象（2026-09-24 再集計）**: 参照式54件 ＝ Post-Host の置き換え対象39件 ＋ 温存15件（Pre-Host 12、共用ヘルパー `ApplyLanguageSetting` 2、const 1）。旧記載の「32箇所（Pre-Host除外11、const除外1）」は陳腐化。
- **決定（2026-09-24〜25）**: 決定1＝A（`InitializePostHostServices()` でフィールドに1回だけ解決）、決定2＝L2（`SpecialActionRepository.LoadActions()` を `Global` と同じ動作に是正して使用）、決定3＝P1（`Global.appdatapath = null` は温存、`PathService.AppDataPath` のセッターは Step12 の削除候補）、決定4＝K（`ApplyLanguageSetting` の2件は温存）、決定5＝A（起動引数 `-virtualkbm` が 2026-09-02 以降無視されている回帰を Step7-4 で是正）、決定6＝N（`Program.rootHub` 等は本 Step の対象外とし、後の Step／Phase で Pure DI 化。`Phase6-Step12-Plan.md` §4 に登録）、決定7＝H（DI ホストが `Global` の静的初期化で起動引数なしに先に作られるため、起動引数の保持役 `IStartupArguments` を新設して `ControlService` へ渡す。根本原因は持ち越し K7-1）。
- **マイクロステップ**: Step7-1（契約追加・LoadActions 是正）→ 7-2（起動前半・初回起動補助）→ 7-3（起動後半・終了処理・TODO）→ 7-4（`-virtualkbm`）→ 7-5（文書・完了報告）。
- **確定方針（選択肢1、2026-09-18。改訂後も維持）**:
  - Pre-Host 領域（ログ初期化、多重起動判定、driverinstall等）を厳格に保護し、静的のまま温存。
  - `AppHost.CreateHost()` 直後に `InitializePostHostServices()` を実行し、WPF Composition Root パターンに則って安全にサービス解決。
  - 分類(c)の項目（保存場所選択、標準アクション等）を既存サービスへ薄いシムとして集約。

#### Phase6-Step7b: プロファイル編集画面の「編集内容の即時反映」廃止
- **状態**: **完了（2026-09-26）**。Step7b-0〜7b-4。詳細は `Phase6-Step7b-Plan.md`、`Phase6-Step7b-Completion-Report.md`。
- **経緯**: Step4 の実機確認で、コントローラー横の Edit ボタンで開いた編集画面の変更が保存前にコントローラーへ即時反映される既存仕様の問題（K4-3）が判明。ユーザー決定（2026-09-24）で、編集中のリアルタイム確認を廃止（copilot-instructions §2.2 の承認済みの機能廃止）し、ライトバー色のプレビューとランブルテストは残す。
- **決定（2026-09-26）**: 決定1＝A（実機に触れる処理をサブ画面まで `targetDevice` に付け替え）、決定2＝P1（画面は必須引数、VM・ファクトリは省略可能）、決定3＝M1（モデル図 03・04 に追記）、決定4＝I1（ランブルテストの左右反転は編集中の値）、決定5＝D1（`ProfileEditor.DeviceNum` 削除）、決定6＝L1（確認用ログ）、決定7＝H1（到達しなくなる即時フック 8 件を削除）、決定8（Save と Apply はどちらも保存後にそのプロファイルを使うスロットへ再適用し、違いは画面を閉じるかどうかだけ）。
- **マイクロステップ**: 7b-1（VM・ファクトリ拡張）→ 7b-2（サブ画面への受け渡し）→ 7b-3（編集スロット固定と保存・適用・キャンセル整理）→ 7b-4（文書・実機確認・完了報告）。
- **持ち越し**: K7b-1（実機確認中の異常終了。原因未特定、切り分けを含めて先送り。`Phase6-Status.md` §6.5）。ステアリングホイール校正の実機確認は Step11 へ先送り。

#### Phase6-Step8: `ProfileEditor.xaml.cs` の段階的MVVM移設 ＆ `MainWindow` 解体推進
- **対象**: 実参照式60箇所（const除外38箇所）。
- **確定方針（案3-A）**:
  - View に書かれていたプロファイル保存（`SaveProfile`）、ブランク読込（`LoadBlankDevProfile`）、スロット適用（`ApplyProfileToSlot`）等のバックエンド操作を **`ProfileSettingsViewModel` へ移設**（Dumb View 化）。
  - `MainWindow.xaml.cs` がサービスを横流しするバケツリレーを撤廃し、`MainWindow` の解体・スリム化を強力に推進。
  - Step1 で判明した重要契約差（B8: `GetActionOrPlaceholder`、B9: `ApplyProfileToSlot`、B10: `LoadBlankDevProfile`）をシムで完全吸収。

#### Phase6-Step9: 主要4大ViewModelのPure DI徹底 ＆ カテゴリ別移行
- **対象**: 実参照式79箇所（`Settings`: 55, `MainWindow`: 8, `TrayIcon`: 6, `ProfileSettings`: 10、const除外14）。
- **確定方針（選択肢1、2026-09-18。改訂後も維持）**:
  - アプリの中枢4大ViewModelにスコープを限定。コンストラクタ引数注入（Pure DI）を徹底。
  - `SettingsViewModel`（55箇所）を「①基本/テーマ/ログ」「②OSC/UDP/平滑化」「③環境/パス/外部」の3サブPRに分割。
  - `BatteryChanged`（B12シム）を `IDeviceStateService` に実装。残存小型ViewModel（12ファイル）は Step10 へ引き継ぎ。

#### Phase6-Step10: 残存小型UI・小型ViewModel・補助クラスのドメイン別ペアPure DI化
- **対象**: 小型View 11ファイル、小型ViewModel 12ファイル＋SpecialActions、`PresetOption.cs`（実参照式計72箇所、除外9箇所）。
- **確定方針（選択肢A）**:
  - 「グループ1: 起動・環境系」「グループ2: プロファイル・マッピング系」「グループ3: スペシャルアクション編集系」の3グループに分割。
  - **View と ViewModel をペアで同時に Pure DI 化**し、バインディング切れを完全防止。
  - 本ステップ完了をもって、ソリューション全域からの Global 直接参照根絶を達成。

---

### 【ドメイン4: 検証・仕上げ】

#### Phase6-Step11: 2層構造・階層化総合検証
- **確定方針（選択肢A）**:
  - **第1層**: 自動テストスイート全件実行（Actions, Standalone, Phase4/5新規, Phase6新規全件） ＋ 静的解析による未検知Global参照ゼロ完全証明。
  - **第2層**: `Phase6-Step11-RealDevice-Verification-Checklist.md` を作成し、**Flydigi Vader 4 Pro**（C/Z/M1〜M4パドル、1000Hz）、**DS4**、**DualSense** による 10大構造化シナリオ検証（30分〜1時間連続ストレステスト含む）を実施。
  - 全項目合格をもって Step12 の物理削除を正式認可（GOゲート）。

#### Phase6-Step12: 呼出元0件旧静的シムの安全防壁付き物理削除 ＆ Phase6最終完了判定
- **確定方針（選択肢2）**:
  - 【5大保護防壁】（BackingStore、Phase7引き継ぎ101件、Pre-Host11件、const定数、ViGEm依存）を厳格に保護。
  - 全リポジトリ走査で呼出元0件が完全に証明された旧静的ラッパーメソッド・プロパティを `ScpUtil.cs` から物理削除し、コードを安全にスリム化。
  - `dotnet test` 再実行で完全合格を証明し、`Phase6-Completion-Report.md` をもって Phase6 の完全成功を宣言。Phase7 への最終引き継ぎ台帳を締結。

---

### 【ドメイン5: 横断（構造整理）】

#### Phase6-Step10b: 1ファイル1型（原則1）の全数是正（Step10 と Step11 の間）
- **対象**: リポジトリ全体の複数型ファイル55件（`DS4Windows` 53、`DS4WindowsTests` 2）、分割で新設されるファイル203件。単一型でファイル名が型名と異なる3件は改名する。
- **位置の理由**: Step2〜Step10 の行番号台帳を壊さず、Step11 の総合検証が分割後のコードを1回で検証でき、Step12 は `Global` の単独ファイルに対する削除になる。
- **確定方針**: Roslyn ベースの機械的な分割と機械検証（型インベントリ・宣言テキスト・意味診断の一致、1ファイル1型・命名の確認）。ウェーブ W1（データ型・補助型）→ W2（DI 層）→ W3（UI 層）→ W4（デバイス・コア層）→ W5（`ScpUtil.cs`）の順。名前空間・フォルダは変更しない。
- **規約**: `copilot-instructions.md` §3.3 原則1 に過渡期ルールを追加（既存の複数型ファイルは本 Step で一括分割し、それまでは分割せず、新しい型は必ず新規ファイルに作る）。
- **実施順序**: Step13（配置統一）→ Step10b。
- **未決**: 提供方法（分割ツール＋検証レポート／パッチ／全ファイル）。着手時に決定。
- 詳細は `Phase6-Step10b-Plan.md` を正本とする。

---

#### Phase6-Step13: `.cs` ファイル配置の統一（段階実施・挙動変更なし）
- **対象**: `DS4Windows/` 配下の `.cs` 44ファイル（`DS4Control/` と `DS4Control/Services/` の配置基準の不統一、Actions サブシステムの二重配置、ルート直下の機能ファイル、`NotificationService` の孤立）。
- **確定方針**: 現 Step は**案2**（インターフェースを `DI/` に一元化、実装は `DS4Control/Services/`、Actions は `Actions/`）を適用し、将来は案4（層別再編・名前空間の一致）へ進む。移動は `git mv` のみで、ファイルの内容・名前空間は変更しない。配置ルールは `copilot-instructions.md` §3.4 に明文化した。
- **ステージ構成**:
  - 13-1（旧 PR-L、20件）: 契約7件を `DI/` へ、`ProfileSlotApplier` を `Services/` へ、直下の Actions 名前空間12件を `Actions/` へ。**Step2-PR-4 の前に実施（決定済み）**。
  - 13-2（8件）: 更新3件を `Updater/` へ、ログ5件を `Core/Logging/` へ。
  - 13-3（1件）: `NotificationService` を `DS4Control/Services/` へ。
  - 13-4（6件）: `ActionManager`、`ActionRegistry` と関連コントローラー4件を `Actions/` へ。
  - 13-5（9件）: プロファイル補助4件を `DS4Control/Profiles/` へ、ユーティリティ5件を `Core/Utilities/` へ。
  - 13-6: Phase6 範囲外への引き継ぎ（`DI/` の契約の最終配置、テスト構造の整理、名前空間とフォルダの一致）。
- **検証**: 各ステージで、`git diff --cached -M100% --name-status` の全行が `R100` であること（内容変更なし）、ビルド・テスト全件成功、起動スモーク。
- 詳細は `Phase6-Step13-Plan.md` を正本とする。

---

## 3. アーキテクチャ・ガードレール

1. **ホットパス性能維持（ゼロアロケーション）**:
   毎秒250〜1000回実行される入力ポーリングループ（`ControlService`, `Mapping`, `Mouse`）において、新規ヒープ割り当て（GC Alloc）、LINQ、仮想メソッド多段解決を厳禁とし、BackingStore 固定長配列への直接参照を維持する。
2. **BackingStore SSOT 保護**:
   各 DI サービスは `BackingStore` の正本データを直接参照・操作し、独自フィールドによる二重管理を絶対に再発させない。
3. **プロファイル適用時の Halt 停止保証**:
   プロファイル適用や出力デバイス変更を伴う操作では、入力ポーリングスレッドの停止（Halt）と排他制御を確実に維持する。
4. **Pre-Host ライフサイクル境界の保護**:
   DIコンテナ構築前に動作するログ初期化・多重起動判定・driverinstall分岐に手を加えず、起動クラッシュリスクをゼロとする。
5. **残置コードへの技術的負債コメント明記**:
   Phase7 へ引き継ぐメンバや非推奨化した API には、理由・正規参照先（SSOT）・対応予定フェーズを必ず明記する。

---

## 4. 完了条件

1. Step2〜Step10 に定義された全実利用箇所（約404箇所）が、すべてコンストラクタ注入された DI サービス経由の呼び出しへ置換されていること。
2. 明示的除外リスト（const定数、Pre-Hostブートストラップ11件、ViGEmバックエンド、Phase7引き継ぎ101件）以外の `Global.` 直接参照がソリューション全体から物理的に根絶されていること。
3. `ProfileEditor.xaml.cs` のバックエンド操作が `ProfileSettingsViewModel` へ移設され、`MainWindow.xaml.cs` のスリム化が達成されていること。
4. 全自動テスト（Actions / Standalone / Phase6新規テスト全件）が 100% 成功していること。
5. Flydigi Vader 4 Pro コントローラーを含む実機マトリクス検証（10大セクション）が全件「○（合格）」であること。
6. 呼出元0件となった旧静的ラッパーが安全防壁付きで物理削除され、`ScpUtil.cs` のスリム化が安全に達成されていること。
7. Step10b が完了し、対象の全 `.cs` ファイルがトップレベル型を1つだけ持ち、ファイル名が型名と一致していること（型インベントリ・宣言テキスト・意味診断が分割前後で一致していること）。
8. Step13（13-1〜13-5）が完了し、`.cs` ファイルの配置が `copilot-instructions.md` §3.4 の配置ルールに従っていること（全移動が内容変更なしの rename として検証済みであること）。

---

## 5. スケジュール見積りと全体進捗位置づけ

### 5.1 各ステップ作業見積り
- **Step 1**: 詳細監査と対象確定（完了実績: 1日）
- **Step 2〜3**: コア層（ControlService / Mapping）（3〜4日）
- **Step 4〜6**: 信号出力層・スロットSSOT・追従（3〜4日）
- **Step 7〜10**: 起動・UI層・MVVM移設・小型UI（4〜5日）
- **Step 11〜12**: 2層総合検証・実機テスト・物理削除・完了判定（2〜3日）
- **Step 10b**: 1ファイル1型の全数是正（W1〜W5、約2〜3日）
- **Step 13**: `.cs` ファイル配置の統一（13-1〜13-5、約1.5〜2日）
- **合計想定工数**: 約17〜22日（Step10b・Step13 を含む。順次マイクロステップPR運用）

### 5.2 全体プロジェクト（Phase 0 〜 Phase 7）における位置づけ
- **全体進捗率**: **約 60〜65%**（Phase0〜5完了 ＋ Phase6-Step1完了・全計画書確定）。
- **設計・リスク消化度**: **約 75〜80% 突破**（SSOT確立、全参照走査完了、1行単位での施工計画確定により、不確実性はほぼ消滅）。

---

## 6. 次のアクション

1. 本計画書（`Phase6-Plan.md`）および進捗管理表（`Phase6-Status.md`）の承認。
2. **Phase6-Step2（`ControlService.cs` のGlobal直参照解消、実参照66箇所）** の施工着手。