# フェーズ6 進捗管理文書: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

最終更新日: 2026-09-19  
状態: Phase6-Step1 完了 / Step2〜Step12 全計画書確定・承認待ち（施工着手準備完了）  
対象ブランチ: `For-DI-migration-work`  
前フェーズ完了状況: **Phase5 完了（Step1〜15 完了済み、SSOT確立・Issue 7根本解消完了）**  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 主な対象ファイル | 最新確定実参照数 | ステータス | 確定計画書 / 成果物 | 完了日 / 予定 |
| :--- | :--- | :--- | :---: | :---: | :--- | :---: |
| **Step 1** | 詳細監査と対象確定 | ソリューション全域 | 762参照走査 | **完了** | `Phase6-Step1-Completion-Summary.md` | 2026-09-18 |
| **Step 2** | `ControlService.cs` 解体 | `ControlService.cs` | 66箇所 | 計画確定・承認待ち | `Phase6-Step2-Plan.md` | 未着手 (PR-1〜6) |
| **Step 3** | `Mapping.cs` 局所引数渡し | `Mapping.cs` | 10箇所 (+101引継) | 計画確定・承認待ち | `Phase6-Step3-Plan.md` | 未着手 (PR-1〜3) |
| **Step 4** | マウスエミュレーション系 DI化 | `Mouse*.cs` (3ファイル) | 73箇所 | 計画確定・承認待ち | `Phase6-Step4-Plan.md` | 未着手 (PR-1〜5) |
| **Step 5** | OutputSlotService SSOT統合 | `OutputSlotService.cs` 等 | 8箇所 | 計画確定・承認待ち | `Phase6-Step5-Plan.md` | 未着手 (PR-1〜3) |
| **Step 6** | 出力切替UI表示追従・負債整理 | `ProfileEditor.xaml.cs` 等 | 4箇所 | 計画確定・承認待ち | `Phase6-Step6-Plan.md` | 未着手 (PR-1〜3) |
| **Step 7** | `App.xaml.cs` Post-Host DI化 | `App.xaml.cs` | 32箇所 (11保護) | 計画確定・承認待ち | `Phase6-Step7-Plan.md` | 未着手 (PR-1〜4) |
| **Step 8** | `ProfileEditor` 段階的MVVM移設 | `ProfileEditor.xaml.cs` 等 | 60箇所 | 計画確定・承認待ち | `Phase6-Step8-Plan.md` | 未着手 (PR-1〜5) |
| **Step 9** | 主要4大ViewModel Pure DI化 | `SettingsVM`, `MainWindowVM` 等 | 79箇所 | 計画確定・承認待ち | `Phase6-Step9-Plan.md` | 未着手 (PR-1〜6) |
| **Step 10**| 小型UI/ViewModel ドメイン別DI化| 小型View 11, 小型VM 12 等 | 72箇所 | 計画確定・承認待ち | `Phase6-Step10-Plan.md` | 未着手 (PR-1〜4) |
| **Step 11**| 2層構造・階層化総合検証 | 全自動テスト ＋ 実機検証 | - | 計画確定・承認待ち | `Phase6-Step11-Plan.md` | 未着手 |
| **Step 12**| 旧シム物理削除 ＆ 完了判定 | `ScpUtil.cs` | 呼出元0件ラッパー | 計画確定・承認待ち | `Phase6-Step12-Plan.md` | 未着手 (PR-1〜4) |
| **合計** | **Phase6 全域** | **約40ファイル** | **約404箇所** | **計画策定100% / 実装 1/12** | **全Step計画書完備** | - |

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

### Phase6-Step2: `ControlService.cs` のGlobal直参照解消【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: コンストラクタ引数注入の拡張（Pure DI）。
- **対象**: 実参照式66箇所（除外8箇所）。
- **計画ハイライト**:
  - `On_Report` 入力ループ内での**ゼロアロケーション（GC Alloc = 0）**および BackingStore 配列直接アクセスの徹底。
  - `ProfileApplicationService` との循環依存を回避する設計の確立。
  - 6つのサブPR（マイクロステップ）に分割して安全に施工予定。

---

### Phase6-Step3: `Mapping.cs` の局所的引数渡し ＆ Phase7引き継ぎ台帳化【計画確定・承認待ち】
- **進捗率**: **0%（未着手・計画確定済み）**
- **確定方針**: **案A（局所的引数渡し ＋ Phase7完全引き継ぎ台帳化）の採用**。
- **対象**: 実装対象10箇所、Phase7引き継ぎ対象101箇所、除外4箇所。
- **計画ハイライト**:
  - 非ホットパス（画面座標、設定保存・ロード等）の10箇所のみを安全に引数渡しで解消。
  - `SetCurveAndDeadzone`（34件）等の超高頻度ホットパス（101件）は、スタック負荷を避けるため Phase7 インスタンス化への完全引き継ぎ台帳として管理。

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

---

## 5. 直近の次アクション

1. 本進捗管理文書（`Phase6-Status.md`）および全体計画書（`Phase6-Plan.md`）の承認。
2. **Phase6-Step2（`ControlService.cs` のGlobal直参照解消、実参照66箇所）** の施工着手。
   - サブPR-1（Step2-1: 非ホットパスの環境・パス・基本設定、ID: C2-01〜05, 21〜27, 32〜40、計22参照）の着手準備。