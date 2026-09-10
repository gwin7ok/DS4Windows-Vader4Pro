# フェーズ6計画書: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

作成日: 2026-09-09
改定日: 2026-09-11（Phase5-Step14からのタスク4・5移管に伴う全12ステップ化改定）
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase5-Plan.md`（Step1〜15、ドメイン集約型）
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
参照調査:
  - `Phase5-Step13+14-Addendum-Findings-Report-Part1.md`／`-Part2.md`（`Global.*`残存件数の実地調査）
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（横断的設定監査レポート）
  - `Phase5-Step14-Issue7-Fix-Plan.md`（Step14改修計画書）

---

## 0. 目的とスコープ

### 0.1 目的
**完全にDI化された4層構造（入力監視層／信号変換層／信号出力層／UI層）のアプリへ移行し、その結果として新機能追加時の実装容易性を恒常的に向上させること。**

Phase1〜5により、`Global`静的クラスの主要機能はすでに10種以上のDIサービスへ再編され、各サービス内部のLegacy委譲（`Global`／`Program.rootHub`への再委譲）もPhase5で監査・是正済みである。しかし、**呼び出し元側**（`ControlService.cs`、`Mapping.cs`、UIビュー群等）には、DIサービスが存在するにもかかわらず旧来通り`Global.X`を直接呼び続けている箇所が実地調査の結果**193件**確認されている。Phase6は、この呼び出し元側の残存直接参照を解消し、既存の全レイヤーがDIサービス経由で一貫して動作する状態を完成させる。

### 0.2 スコープに含むもの
`git clone`による実地調査（2026-09-09）で判明した、`Global.`修飾で参照されている193件の実利用箇所のうち、以下を対象とする。

| 参照元ファイル | 対象件数 | 層 |
| :--- | :--- | :--- |
| `ControlService.cs` | 39件 | 信号変換層／入力監視層の境界（最優先） |
| `Mouse.cs` | 27件 | 信号出力層 |
| `SettingsViewModel.cs` | 23件 | UI層 |
| `App.xaml.cs` | 22件 | 起動シーケンス |
| `ProfileEditor.xaml.cs` | 18件 | UI層（Phase5-Step15の対象漏れ箇所） |
| `MouseCursor.cs` | 14件 | 信号出力層 |
| `Mapping.cs` | 12件 | 信号変換層（既存方針の範囲内で再点検） |
| `PresetOption.cs` | 10件 | UI補助 |
| `WelcomeDialog.xaml.cs` | 9件 | UI層 |
| `MainWindowsViewModel.cs` | 8件 | UI層 |
| `SaveWhere.xaml.cs` / `BindingWindow.xaml.cs` / `TrayIconViewModel.cs` | 各5件 | UI層 |
| `ProfileSettingsViewModel.cs` | 4件 | UI層 |
| **【Phase5-Step14より移管】OutputSlot / 横断設定** | - | 信号出力層（孤立設定・直接参照の是正、旧Step14 タスク4） |
| **【Phase5-Step14より移管】スロット動的再接続連動** | - | 信号出力層（ホットスワップ連動・負債コメントAPI昇華、旧Step14 タスク5・6） |

（`ProfileRepository.cs`等、DIサービス自身の内部実装からの参照16件は、Global＝実データ源であるStrangler Fig委譲として意図的なものであり対象外。詳細はStep1監査で最終確定する。）

### 0.3 スコープに含まないもの（明示的除外）
1. **仮想コントローラー出力バックエンド（ViGEm）の抽象化**: `ControlService.cs`内の`as Xbox360OutDevice`/`as DS4OutDevice`ダウンキャストによるフィードバック配線、および`Nefarius.ViGEm.Client`への直接依存は、`Global`とは無関係の別種の技術的負債であり、Phase6の対象外とする。将来必要になった場合は別イニシアチブ（`IVirtualControllerBackend`抽象化）として扱う。
2. **`Mapping.cs`の完全instance化**: 全体移行計画書§5.5の既存方針（「本プランでは見送る」）を継承する。Phase6では`Mapping.cs`の残存12件についても、既存の`IDeviceStateAccessor`等の委譲パターンの範囲内でのみ対応し、`Mapping`自体のDIコンテナ管理下への完全移行は行わない。
3. **テストファイル内の`Global.*`参照**（`ProfileSettingsServiceTests.cs`, `AppSettingsServiceTests.cs`, `ProfileRepositoryTests.cs`等、計28件相当）: これらは新旧比較による孤立プロパティバグの再発防止用の意図的な参照であり、移行対象としない。
4. **真の定数**（`Global.TEST_PROFILE_INDEX`, `Global.RESOURCES_PREFIX`等）: DI化不要（元プラン§4.1）。

---

## 1. 実施ステップ一覧（全12ステップ、ドメイン集約型）

```text
【Phase6 全12ステップ構成】
Phase6-Step1: 詳細監査と対象確定【最優先】

── [ドメイン1: コア層（信号変換・入力監視境界）] ──
├─ Phase6-Step2: ControlService.cs のGlobal直参照解消（39件、最難関）
├─ Phase6-Step3: Mapping.cs 残存参照の再点検（12件、既存方針踏襲）

── [ドメイン2: 信号出力層] ──
├─ Phase6-Step4: Mouse.cs / MouseCursor.cs のGlobal直参照解消（41件）
├─ Phase6-Step5: 【新規】OutputSlot / 横断設定のGlobal直参照・孤立設定の是正（旧Phase5-Step14 タスク4）
└─ Phase6-Step6: 【新規】出力デバイス切替時の実行時スロット動的再接続（ホットスワップ）連動（旧Phase5-Step14 タスク5）

── [ドメイン3: 起動・UI層] ──
├─ Phase6-Step7: App.xaml.cs 起動シーケンスの整理（22件、旧Step5）
├─ Phase6-Step8: ProfileEditor.xaml.cs のGlobal直参照解消（18件、旧Step6）
├─ Phase6-Step9: SettingsViewModel他 主要ViewModel群の解消（39件、旧Step7）
└─ Phase6-Step10: 残りの小型UIファイル群の解消（29件、旧Step8）

── [ドメイン4: 検証・仕上げ] ──
├─ Phase6-Step11: 自動テスト・実機検証（旧Step9）
└─ Phase6-Step12: 呼出元0件シムの物理削除判断（旧Step10）
```

---

## 2. 各ステップの詳細内容

### Phase6-Step1: 詳細監査と対象確定
- **内容**: 193件全件について、(a) 既存DIサービスに同名・同等メソッドが既にある「単純リダイレクト」、(b) 既存サービスへの軽微な拡張が必要な「中程度作業」、(c) 新規インターフェースが必要な「要設計」の3分類を確定する。Phase5-Step0/Step1と同様、`git clone`による全文grep突合をベースとし、「呼出元候補」の記述ではなく実地確認を必須とする（Phase5-Watchpoints報告書で判明した「grep未確認の記述はStep15計画の前提を誤らせる」という教訓を反映）。
- **成果物**: `Phase6-Step1-Global-Usage-Classification-Report.md`（193件の全件分類表）。

---

### 【ドメイン1: コア層】

#### Phase6-Step2: `ControlService.cs` のGlobal直参照解消（最優先・最難関）
- **対象**: 39件。多くは`IProfileSettingsService`（プロファイル設定値）、`IDeviceStateService`（デバイス状態）等、Phase4で既に用意されているサービスに同等メソッドが存在する見込みが高い（Step1監査で確定）。
- **【アーキテクチャ・ガードレール注記】**:
  - **[ホットパス性能維持]（新規ガードレール、§3.1参照）**: `ControlService.cs`内のGlobal参照の一部は、毎秒250〜1000回実行される入力ポーリングループから直接呼ばれる（例: `getLSDeadzone(index)`相当のデッドゾーン取得）。単純な配列アクセスをDIサービス経由の仮想メソッド呼び出しに置き換える際、キャッシュ無し・都度XML参照等のオーバーヘッドが混入しないよう、置換前後でのマイクロベンチマーク比較を必須とする。
  - **[Halt保証]（§5.2継承）**: 設定変更を伴う一部メンバ（`InitOutputKBMHandler`関連等）は、既存のHalt機構との整合を崩さないことを確認する。
- **完了判定基準**: `ControlService.cs`内の`Global.`直接参照が0件になり、かつ実機での連打・ホールド・同時押しの入力遅延が置換前と同等であることを確認する。

#### Phase6-Step3: `Mapping.cs` 残存参照の再点検
- **対象**: 12件。全体移行計画書§5.5の方針（`Mapping`完全instance化の見送り）を継続するため、既存の`IDeviceStateAccessor`等と同様の「メソッド引数として注入済みインターフェースを受け取る」形の部分的解消にとどめる。
- **完了判定基準**: 是正可能な範囲（フォールバックパターンでの引数渡し化等）を洗い出し、対応した件数と、意図的に残す件数（`Mapping`のstatic性質上不可避なもの）を明確に区分して記録する。

---

### 【ドメイン2: 信号出力層】

#### Phase6-Step4: `Mouse.cs` / `MouseCursor.cs` のGlobal直参照解消
- **対象**: 計41件（Mouse 27件、MouseCursor 14件）。マウス感度・加速度カーブ等の出力設定参照を `IAppSettingsService` 等へ付け替える。
- **【アーキテクチャ・ガードレール注記】**: マウス移動計算ループ内のホットパス呼び出しに対する性能配慮を徹底する。
- **完了判定基準**: マウスエミュレーションの実機体感確認を含め、入力遅延・挙動の劣化がないことを確認する。

#### Phase6-Step5: OutputSlot / 横断設定のGlobal直参照・孤立設定の是正（旧Phase5-Step14 タスク4）
- **対象**: 横断的設定監査レポート（`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`）で検出された、スロット設定（`OutputSlotPersist` / `OutSlotDevice` 等）および他画面における `Global` 直参照・二重管理箇所。
- **内容**:
  1. `OutputSlotViewModel` や関連コンポーネントが保持する出力デバイス設定の参照先を、`IProfileSettingsService` および `IOutputSlotStore` を正本（SSOT）とする形に付け替える。
  2. スロット設定画面等で直接 `Global.Instance.Config` を読み書きしている二重管理を根絶する。
- **完了判定基準**: スロット関連の `Global` 直参照が解消され、出力デバイスタイプおよび関連設定の正本が一元化されていること。

#### Phase6-Step6: 出力デバイス切替時の実行時スロット動的再接続（ホットスワップ）連動（旧Phase5-Step14 タスク5）
- **対象**: `IOutputSlotService` / `OutputSlotManager` / `ControlService`。
- **内容**:
  1. プロファイル設定（`IProfileSettingsService.OutContType`）の変更イベントを検知し、現在接続中の仮想スロットのコントローラー種別（Xbox 360 ⇄ DS4）を動的にアンプラグ＆再プラグインするホットスワップ連動を実装する。
  2. Phase5-Step14（タスク6）で `IOutputSlotService` に付与した `GetOutputDeviceType` / `SetOutputDeviceType` の技術的負債コメントを解消し、実行時ViGEm接続状態を正しく問い合わせ・制御する正式APIとして昇華・整理する。
- **【アーキテクチャ・ガードレール注記】**:
  - **[ViGEmドライバ保護]（§3.4）**: 仮想デバイスの即時再接続時にドライバ側でデッドロックやクラッシュが発生しないよう、排他ロックおよび適切な切断待機インターバルを確保する。
- **完了判定基準**: UI上で出力コントローラー種別を変更した際、実スロットの仮想デバイスが即座に切り替わり、ゲーム側への入力転送が正常に継続すること。

---

### 【ドメイン3: 起動・UI層】

#### Phase6-Step7: `App.xaml.cs` 起動シーケンスの整理（旧Step5）
- **対象**: 22件。Composition Root構築前後のタイミング依存領域。On-Demandパス評価原則（Phase5-Step10継承）を適用する。

#### Phase6-Step8: `ProfileEditor.xaml.cs` のGlobal直参照解消（旧Step6）
- **対象**: 18件。Phase5-Step13完了報告書で判明した「対象漏れ」箇所。Step13の`AppHost.GetService<T>() ?? Global.XxxInstance`パターンを踏襲予定。

#### Phase6-Step9: `SettingsViewModel.cs`他 主要ViewModel群の解消（旧Step7）
- **対象**: 計39件（SettingsViewModel 23件、MainWindowsViewModel 8件、ProfileSettingsViewModel 4件、TrayIconViewModel 4件）。UI ViewModelからの直接参照を完全排除する。

#### Phase6-Step10: 残りの小型UIファイル群の解消（旧Step8）
- **対象**: 計29件（WelcomeDialog 9件、PresetOption 10件、SaveWhere/BindingWindow 各5件）。件数が少なく1PRでまとめて対応可能な見込み。

---

### 【ドメイン4: 検証・仕上げ】

#### Phase6-Step11: 自動テスト・実機検証（旧Step9）
- **内容**: 全ユニットテスト（新規テスト含む）、統合テストの実行および実機コントローラーによる総合動作検証。各Step完了ごとにビルド・既存自動テストを実行する運用とし、Step2・Step4・Step6は実機での連続入力・再接続回帰確認を必須項目とする。

#### Phase6-Step12: 呼出元0件シムの物理削除判断（旧Step10）
- **内容**: Phase6完了時点で呼出元0件が確定した `Global` メンバに `[Obsolete]` を付与し、削除候補リストを更新する（即時削除はしない）。

---

## 3. 実装における潜在的懸念点とアーキテクチャ・ガードレール

### 3.1 [新規] ホットパス性能維持（Step2, Step4）
毎秒250〜1000回実行される入力ポーリングループ（例: `ControlService` のスティック計算）における仮想メソッド呼び出しのオーバーヘッドを避けるため、必要な設定値はイベント購読によるローカルキャッシュ化を行い、都度サービス問い合わせを発生させない。

### 3.2 [継承] 既存6大ガードレール（Phase5-Plan.md §5より）
1. 同一XML排他ロック（ロストアップデート防止）
2. プロファイル適用時のHalt停止保証
3. AutoProfileスレッド直列化
4. パス解決のOn-Demand化
5. ViGEmドライバ排他・クラッシュ保護
6. DI経由通知の到達保証

### 3.3 [新規] ドキュメント記述の実地確認原則
コードの存在確認や呼出元調査は、推測や古いドキュメントの記述に頼らず、必ず最新のソースコードに対する実地grep・構文解析を行って確定する。

### 3.4 [新規] スロット動的再接続時の ViGEm バス競合保護（Step6）
実行時ホットスワップにおいて、アンロード前の再プラグインによるバスパニックを防ぐため、スロットアンロードの完了検知と直列化を徹底する。

### 3.5 [新規] 残置クラス・メソッドへの経緯コメント明記（`copilot-instructions.md` §3.3.4準拠）
移行過程で非推奨化・使用停止となるが破壊的変更防止のために残置するクラスやメソッドには、**理由・正規の参照先（SSOT）・対応予定フェーズ** を明記した技術的負債コメントを必ず付与する。

---

## 4. 完了条件
1. Step1で確定した対象（真の除外分を除く実質193件相当）の呼出元が、全てDIサービス経由の呼び出しに置換されていること。
2. `ControlService.cs`, `Mapping.cs`, `Mouse.cs`, `MouseCursor.cs` のホットパスにおいて、置換前後で体感可能な性能劣化がないことを実機確認済みであること。
3. 信号出力層において、出力デバイス切り替え時の動的スロット再接続が正常に連動すること（Step6）。
4. 既存の全自動テスト（Actions／Standalone／Phase4・5で追加された単体テスト）が成功を維持していること。
5. `ProfileEditor.xaml.cs` を含むUI層全体で、Phase5-Step13と同水準の静的参照撲滅が完了していること。
6. 呼出元0件となった `Global` メンバに `[Obsolete]` が付与され、削除候補リストが更新されていること。

---

## 5. 進行ルール（Phase5-Plan.md §7を継承）
- 1ステップごとに計画・実装・テスト・コミットを完結させる。
- 成果物はすべて `.github/PowerShell-script-generation-rules-for-deliverables.md` に従って提示する。

---

## 6. スケジュール見積り（全12ステップ）
- Step 1: 監査・分類（1日）
- Step 2〜3: コア層（3〜4日）
- Step 4〜6: 信号出力層・スロット連動（3〜4日）
- Step 7〜10: 起動・UI層（4〜5日）
- Step 11〜12: 検証・シム削除（2〜3日）

---

## 7. 次のアクション
Phase 5（Step 14 残余および Step 15）の完了を待ち、Phase6-Step1 の詳細監査に着手する。
