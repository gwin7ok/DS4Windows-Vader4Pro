# Phase6-Step11 計画書: 2層構造・階層化総合検証（自動回帰テスト ＋ 実機シナリオマトリクス）

作成日: 2026-09-09  
改訂日: 2026-09-18（Step2〜Step10確定スコープ約400箇所統合・推奨案［2層構造・階層化総合検証］採用・全面拡充改訂）  
状態: **完了（2026-10-17。判定 GO。`Phase6-Step11-Completion-Report.md`）**。Step11-0 の台帳は §0A  
**2026-10-14 注記（文書整合）**: §0.1 の Step2〜Step10 の件数は 2026-09-18 時点の記載で、現行の正と異なる。Step3 は「10箇所実装＋101箇所引き継ぎ」ではなく、実参照150件の全件解消（完了、2026-09-24）。Step7 は 39件（温存15）、Step8 は 58件、Step9 は 114件、Step4 は 81行。ほかに Step7b（編集スロット固定）と Step9b（Copilot レビュー指摘の検証と是正）が加わった。実機確認の先送り項目は §3.3 の台帳が正本（各 Step の完了報告書から随時追加）。第1層の静的解析は、現行コードの台帳で再計画すること。  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md`（Core層・Mapping層・Mouse層 監査記録）  
  - `Phase6-Step1-UI-Reference-Evidence.md`（UI層・ViewModel層 監査記録）  
  - `Phase6-Step2-Plan.md` 〜 `Phase6-Step10-Plan.md`（各ステップ確定実装計画書）  
  - `Phase5-Step14-RealDevice-Verification-Checklist.md`（Phase5 実機検証成功モデル）  

---

## 0A. Step11-0 台帳（2026-10-15 作成。現行コードの走査による再集計。§0〜§6 の記述は 2026-09-18 版のため、失効している点は本節を正とする）

### 0A.1 現行の事実（HEAD は Step10b 完了時点）
- **自動テスト**
  - `DS4WindowsTests`（`DS4Windows.Actions.Tests.csproj`）: 134 ファイル、`[Fact]` 552、`[Theory]` 10（静的な数。`dotnet test` の実行件数は、ユーザーの実行結果で確定する）。
  - `StandaloneTests`: 11 ファイル、MSTest（`[TestMethod]` 13）。
  - 監査ツール: `scripts/audit/GlobalInventory`（Roslyn による `Global` 参照の台帳。第 1 層の静的解析に使える）。
- **全域の直接参照の現行値**（`Step10FourGlobalRatchetGuardTests` の基準値と一致。コメントを除く）
  - `Program.rootHub`: 25 件・6 ファイル（`App.xaml.cs` 12、`Global.cs` 9、`ServiceRegistration.cs` 1、`Mapping.cs` 1、`ProfileEditor.xaml.cs` 1、`DS4SixAxis.cs` 1）。
  - `AppHost.GetService|Services`: 48 件・9 ファイル（`MainWindow.xaml.cs` 17、`Global.cs` 11、`Mapping.cs` 6、`App.xaml.cs` 4、`ProfileEditor.xaml.cs` 4、`AppHost.cs` 2、`Compat/AppHost.cs` 2、`ControlService.cs` 1、`BackingStore.cs` 1）。
  - `Global.*`: 780 件・75 ファイル（上位: `AppSettingsService.cs` 185、`BackingStore.cs` 182、`Mapping.cs` 53、`Global.cs` 47、`ProfileEditor.xaml.cs` 32、`ProfileRepository.cs` 26、`EnvironmentService.cs` 21、`OutputKBMHandlerAdapter.cs` 21。過半は、サービス実装の薄いシム［§2.1］と DTO、`BackingStore` 自身）。
- **対応デバイス**: `DS4`、`DualSense`、`DS3`、`SwitchPro`、`JoyConL`／`JoyConR`／`JoyConGrip`。**Flydigi Vader 4 Pro のサポートは削除済み**（2026-10-14、`Phase6-Step10-V4P-Removal-Plan.md`）。
- **直近の変更**（実機確認の対象）: Step10-3d（ボタン SA の Toggle）、Step10-5（出力スロットの抜き差し）、名前変更の伝播（リンク・自動プロファイル・プロファイル切替 SA）、`Mouse.cs` の 3 件（Release モード、タップのずれ許容 100、タップのクリック保持 50ms）、`ProfileEditor` の Left Touch のホバー、Step10b（型の分割。静的初期化の順序）。

### 0A.2 計画書の記述のうち、失効している点と、改訂方針（案）
- **Vader 4 Pro**: §0.2、§1（目的 3）、§3.1（機種マトリクス）、§3.2（セクション 2）、§6（完了判定）が、Vader 4 Pro を前提にしている。削除済みのため、対応デバイス（DS4 を主、DualSense・SwitchPro・JoyCon・DS3 を機材があれば）へ置換する。
- **§2.1 のテスト名**: 一覧のうち、実在するのは `ControlServiceDiWiringTests`、`ControlServiceHotPathAllocationTests`、`MouseDiWiringTests`、`MouseWheelShimTests`、`MouseCursorMovementTests`、`OutputSlotServiceSsotTests` のみ（19 件中 6 件）。残りは名前が変わった、または作られていない。現行のテストの全件（上記）を対象に書き換える。
- **§2.2「Global 直接参照ゼロ」**: 実態は、薄いシムの温存（§2.1）と、Phase7／Step12 の持ち越しにより、ゼロではない。ゲートを、「(1) ラチェットの基準値（`Global.*` 780、`AppHost` 48、`Program.rootHub` 25）が守られている、(2) 残存を、温存の根拠［定数、薄いシム、Pre-Host、DTO、`BackingStore`、Phase7 持ち越し］別に分類した台帳がある」へ置き換える。
- **§3.2 の各セクション**: Step9b〜Step10b の内容（Fresh-press ガード、名前変更の伝播、ボタン SA の Toggle、出力スロット、型の分割）が含まれていない。§0A.3 の台帳を、各セクションへ割り当てる。
- **完了判定（§6）**: 「全項目○」は、機材が無い項目で満たせない。機材が無い項目の扱い（§0A.4 の決定 4）が必要。
- **Step12 計画書**: `Phase6-Step12-Plan.md` も、Vader 4 Pro と 2026-09-18 時点の数を前提にしている。Step12-0 で、現行コードから改訂する（本 Step の範囲外）。

### 0A.3 先送り台帳（§3.3）の再分類（案。確定は Step11-1）
- **機材・環境が不要、または手元で可能（実施する）**
  - 起動・保存場所・言語・多重起動・`-m`・`-driverinstall`（Step7）、初回起動。
  - 通常操作、トリガー、DS4 のモード、バッテリー表示、遅延警告（Step2-PR-5）。
  - `LaunchProgram`（Step2-PR-4 ②）、スペシャルアクションの動作（Step2-PR-5、Step10-3-2）。
  - タッチパッドの出力モード、タッチボタンとクリックパススルー、2 本指スクロール（Step4）、Release モード・タップ（ずれ許容、クリックの保持）・ホバー（Step10）。
  - 出力種別（X360／DS4）の切り替え（Step5 ②）、出力スロットの Plug／Unplug、Permanent（Step10-5）。
  - Actions.xml のエクスポート（Step8）、設定タブ・トレイの残り（Step9）。
  - 静的初期化の順序（起動ログの確認。Step10b）。
- **別アプリ・特殊環境が必要**
  - OSC／UDP の起動・停止と、UDP クライアントからのコマンド（`query.<n>.outconttype`、`outputslot.<n>.unplug`／`plugds4`／`plugx360`）。Step2-PR-1、Step5 ①、Step10-5。
  - vJoy: ステアリングホイールエミュレーション（Step2-PR-4 ③、Step3-6a、Step7b の校正）。
  - 更新通知（新しいバージョンの検出）: Step10-1-3。
- **追加の機材が必要**
  - Joy-Con の結合デバイスのジャイロ（Step2-PR-4 ⑤）。
  - 振動するデバイスと DualSense（Step10-4-2c）。
  - 5 台以上のコントローラー（スロット 5〜8。Step9b-3 F05、Step10-4-2f）。
- **参考・任意**: Step6 のトースト通知の有無、Step3-6c の Alt+Tab マクロ。
- **判断待ち**: `Mouse.cs` の `[TAP]` Trace の残置（実機確認は不要）。

### 0A.4 決定事項（要承認）
- **決定 1（機種の前提）**: Vader 4 Pro を前提にした記述を削除し、対応デバイスへ置換する（A）／計画書の記述を残す（B）。推奨: A。
- **決定 2（静的解析のゲート）**: 「Global 直接参照ゼロ」を、「ラチェット基準値の維持＋残存の分類台帳」へ置き換える（A）／ゼロを維持する（B。Phase7・Step12 の範囲まで広がるため非推奨）。推奨: A。分類台帳は、`GlobalInventory` の出力と、基準値のファイル別の件数から作る。
- **決定 3（自動テストの実施）**: ユーザーが `dotnet test`（Release x64、全件）を実行し、件数と失敗 0 を確認する（A）。警告数も記録する。推奨: A。
- **決定 4（機材が無い項目の扱い）**: 機材・環境が無い項目は、「確認不能（リスク受容）」として、(a) 単体テストなどの代替の根拠、(b) 影響範囲、(c) 確認できる時期、を台帳に記録し、完了判定の対象外とする（A）／すべて実施するまで完了としない（B）。推奨: A（ユーザーの環境［DS4 の BT 1 台など］で確認できない項目が、すでに 8 件ある）。
- **決定 5（チェックリストの作り方）**: `Phase6-Step11-RealDevice-Verification-Checklist.md` を新規に作り、§3.2 の 10 セクションに、§0A.3 の台帳を割り当てて統合する（A）／§3.2 と §3.3 を別々に運用する（B）。推奨: A。項目ごとに、実施手順、期待結果、確認方法（ログ、画面）を書き、ユーザーが 1 項目ずつ結果を返す。
- **決定 6（`[TAP]` Trace）**: 外す（A）／Trace レベルのため残す（B）。推奨: A（Step11 の完了前に、調査用のコードを外す。台帳に記録）。
- **決定 7（連続操作テストの時間）**: 30 分（A）／1 時間（B）。推奨: A。

### 0A.5 決定の記録（2026-10-15。決定 1〜5・7 は推奨案 A で承認、決定 6 は B）
- **決定 1＝A**: Vader 4 Pro の記述を削除し、対応デバイスへ置換した（§0.2、§1、§3.1、§3.2 のセクション 2、§4、§6）。
- **決定 2＝A**: 静的解析のゲートを、ラチェット基準値の維持と、残存の分類台帳へ置き換えた（§2.2。台帳は `Phase6-Step11-Global-Residual-Ledger.md`）。
- **決定 3＝A**: 自動テストは、ユーザーが `dotnet build -c Release`／`dotnet test` を実行し、件数・失敗 0・警告数を記録する（§2.1）。
- **決定 4＝A**: 機材・環境が無い項目は、「確認不能（リスク受容）」として、台帳に、代替の根拠・影響範囲・確認時期を記録し、完了判定の対象外とする（§6、チェックリスト §11）。
- **決定 5＝A**: `Phase6-Step11-RealDevice-Verification-Checklist.md` を新規に作成した。§3.2 の 10 セクションに、§3.3 の先送り台帳と、Step9b〜Step10b の変更を割り当てた。
- **決定 6＝B**: `Mouse.cs` の `[TAP]` Trace（Trace レベルのみ）は、ユーザーの判断で、そのまま残す。調査用のコメント（「確認後に外す」）を、残置の記載に改める。
- **決定 7＝A**: 連続操作テストは 30 分。

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
   `Phase6-Step11-RealDevice-Verification-Checklist.md` を作成し、本フォークの主役である **DS4**（主）と、機材があれば **DualSense** など を用いた実機検証を実施。10大検証セクションにより、ホットパス性能、入力追従性、三態SSOT、表示追従、MVVM保存、長時間の安定稼働を保証する。
3. **Step12（旧シム物理削除判断）への確実なゲート機能**:  
   本ステップの完全合格（全テスト Green ＋ 全実機チェック項目 ○）をもって、呼出元0件となった旧静的メンバの安全な物理削除を正式に認可（GO / NO-GO 判定）する。

---

## 1. 目的

1. Step2〜Step10 で実施された全改修（約404実参照）に対して、機能欠落（Feature Drop）および動作リグレッションが一切発生していないことを、自動テストと実機テストの両輪で証明する。
2. 毎秒250〜1000回実行される入力ポーリング・ホットパス（`ControlService`, `Mapping`, `Mouse`）において、DI化に伴う入力遅延、GC アロケーション、CPU負荷の悪化がゼロであることを実証する。
3. 対応デバイス（DS4 を主とし、機材があれば DualSense・SwitchPro・JoyCon・DS3）の入力・出力が完全に動作することを保証する。（Flydigi Vader 4 Pro は 2026-10-14 にサポートを削除済み。）
4. Step12（旧静的シムの物理削除判断）に引き渡す客観的かつ反論不能な合格エビデンス（検証報告書・チェックリスト）を完成させる。

---

## 2. 第1層: 自動回帰テストスイート ＆ 静的解析完全証明

### 2.1 自動テストスイートの全件実行（2026-10-15 改訂。決定 3）
ユーザーが、Release x64 で、次を実行し、全件成功（失敗 0）と、件数・警告数を記録する。

1. **`DS4WindowsTests`**（`DS4Windows.Actions.Tests.csproj`）: 134 ファイル、`[Fact]` 552、`[Theory]` 10。Phase2〜Step10b で追加された単体テスト・ソース走査ガードテスト（`*GlobalReferenceGuardTests`、`Step10FourGlobalRatchetGuardTests` など）をすべて含む。
2. **`StandaloneTests`**（MSTest）: 11 ファイル、`[TestMethod]` 13。
3. **ビルド**: `dotnet build -c Release`（x64）。警告数を、Step10b 完了時と比較する。
4. 実行後、テスト名の一覧と件数を、完了報告書に添付する（件数は、`dotnet test` の出力で確定する）。

### 2.2 静的解析によるゲート（2026-10-15 改訂。決定 2。「ゼロ」から「基準値の維持と分類」へ）
- ゲートの定義、集計、分類別の台帳、温存の根拠は、`Phase6-Step11-Global-Residual-Ledger.md` に記録した。
- 合格基準: (1) `Step10FourGlobalRatchetGuardTests` が成功（`Global.*` 780、`AppHost` 48、`Program.rootHub` 25 を超えない）、(2) 残存の全件が、台帳の分類（A〜H）に属し、温存の根拠がある、(3) 定数・不変データ・純粋関数以外で、第 4 層の UI・ViewModel が `Global` を直接参照していない。
- 旧「ゼロ」の記述（明示的除外リスト）は、Step2〜Step10 の計画時のもので、現行は上記の台帳が正。


---

## 3. 第2層: 実機デバイスマトリクス ＆ 10大構造化シナリオ検証

### 3.1 対象実機デバイスマトリクス
本リポジトリ（`DS4Windows-Vader4Pro`）の責務に基づき、以下の実機マトリクスを対象とする：

| コントローラー機種 | 接続方式 | 主な検証重点項目 |
|---|---|---|
| **Sony DualShock 4 v.2（主）** | Bluetooth | BT 接続認識、スロット割当、切断・再接続（確認済みの環境） |
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

#### セクション2: デバイス接続・認識・ホットパス性能検証 (Step2, Step4)
- [ ] コントローラー接続時、スロットに即時認識され仮想デバイスがプラグインされること。
- [ ] 入力ポーリング時に入力遅延・カクつきが一切ないこと（体感ラグゼロ）。
- [ ] ボタン連打・スティック全開旋回時にメモリリークや GC スパイク（カクつき）が発生しないこと。
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
| Step8 | `ProfileEditor` の ViewModel 移設・Global 参照解消後の、Special Actions タブの Actions.xml エクスポート（`SpecialActionsListViewModel.ActionsFilePath`＝`ISpecialActionRepository.ActionsPath`）。AppData とプログラムフォルダのどちらにも設定がある環境での、コピー元が現在の保存先の `Actions.xml` であること（保存場所が 1 つの環境では確認済み相当。2026-10-13）。そのほかの Step8 の実機確認項目（レイアウト保存・復元、New Profile、Save／Apply、スペシャルアクションの保存）は問題なし | セクション1、9 | 未確認（保存場所が 2 つある環境がない場合のみ） |
| Step9 | 主要4大 ViewModel の Pure DI 化（Step9-2〜9-4）の実機確認のうち、メイン画面と設定タブ（各設定の維持、OSC 送信のオン／オフ、偽装 exe 名、Steam フォルダ、ログ設定）、トレイ（バッテリー表示、ツールチップ、メニュー）、プロファイル編集（Edit・保存・Bezier カーブエディタ・プリセットの割り当て内容）は 2026-10-14 に確認済み（問題なし）。次の項目を先送り: ①ViGEmBus が未導入または古い環境での、起動時のドライバー確認（`MainWindowsViewModel.CheckDrivers` の `-driverinstall` による昇格起動。導入済みの環境でしか確認していない）、②プリセット「WASD」「Arrow Keys」を割り当てた後の、実際のキー出力（割り当ての表示と保存のみ確認。`RefreshActionAlias` の別名が出力方式［SendInput／FakerInput］で正しく使われること）、③OSC 受信側のアプリを接続した状態での送受信（送信のオン／オフでクラッシュしないことのみ確認。Step2-PR-1 の先送りと同じ）、④K9-3: プリセットのコンテキストメニューのサブメニューが、単一モニター・同一 DPI の環境でマウス操作で選べるか。ベースライン（`f13d5c7c` 以前）との差の有無 | セクション1、2、7 | 未確認（2026-10-14 に先送りを確定） |
| Step9b-3 | F05: スロット数を 8 に拡張した環境で、デバイス 5 台目以降（インデックス 4〜7）のスペシャルアクションのマクロが再生されること（`DefaultMacroPlayer`／`Mapping.PlayMacroDirect`／`EndMacroDirect` の 4 スロット固定を `Global.MAX_DS4_CONTROLLER_COUNT` へ変更。単体テスト `DefaultMacroPlayer_HandlesAllControllerSlots_NotJustFour` で固定済み。5 台目以降の実機がないため先送り。既定の 4 スロットでは影響なし） | セクション9（スペシャルアクション・プロファイル） | 未確認 |
| Step10-1-3 | 更新通知（`UpdaterWindow`）の実機確認: 新しいバージョンが検出される状況が必要で実施できなかった。「Skip Version」を押したバージョンが再通知されないこと（`IAppSettingsService.LastVersionChecked` への置換）、「Install Latest」を押したときの起動モード（admin／user。`IEnvironmentService.IsAdministrator` への置換）が置換前と同じであること。自動テストは `Phase6Step10GroupOneViewModelTests` で値の共有まで確認済み | セクション1（起動・環境・更新） | 未確認 |
| Step10-3-2 | スペシャルアクションの「キー押下（Press Key）」の割り当てで、ボタン割り当て画面にコントローラーの図は表示されるが、キーボードとマウスしか割り当てられない（コントローラーのボタン［Xbox 360／DS4 の表記］を割り当てられない）。DI 化の範囲外の機能の問題として別 Step で扱う（`Phase6-Step10-3-Plan.md` §12）。確認項目: 割り当てたボタン名が出力種別に合った表記で表示されること | セクション9（スペシャルアクション・プロファイル） | 一部確認済み（2026-10-15: ボタンの割り当て、表示、Toggle の保存・読込）。ボタンの Toggle の実行時の動作は、実装後（Step10-4・10-5 の後。`Phase6-Step10-3c-Plan.md` §8）に確認 |
| Step10-4-2c | 振動（DualSense の補正を含む）と、振動の自動停止時間の変更が接続中のコントローラーに反映されること。`ProfileSettingsService` の `Program.rootHub` 参照を `Func<ControlService>` に置換したため（`GetRumbleBoost`、`SetRumbleAutostopTime`）。振動するデバイスが無く確認できなかった | 振動・ライトバー系のセクション | 未確認（先送り。2026-10-15） |
| Step10-4-2f | K1 是正（`ProfileApplicationService` のスロット上限を `ControllerSlotLimit` に変更）: 5〜8 番目のコントローラーで、プロファイル切替アクション、復帰、自動プロファイルが動作すること。5 台以上のコントローラーが必要で確認できない見込み | プロファイル切替・自動プロファイル | 未確認（先送り。2026-10-15。1〜4 番目の回帰確認は実施） |
| Step10-5 | UDP コマンド（`outputslot.<n>.unplug`／`plugds4`／`plugx360`）が、`IOutputSlotService` 経由（事前条件の確認と UI スレッドへの切り替え）で動作すること。未接続のスロットへの `unplug` で例外にならないこと。UDP を送る環境が無く確認できなかった | 出力スロット管理・UDP | 未確認（先送り。2026-10-15。画面の Plug／Unplug は確認済み） |
| 既存不具合の是正（Mouse.cs） | Touchpad Button Activation Mode が Release のとき、Left Touch／Right Touch 等に割り当てたキーが、指を離した後に出ること（`releaseButtonActive` の 100ms 判定の比較反転を `>=` に是正。上流 7b2a69c5 由来で移行による退行ではない。ユーザー承認済み 2026-10-15） | タッチパッド | 実機確認待ち（Click／Touch は従来どおり動作すること） |
| 既存仕様の変更（Mouse.cs タップ判定） | Mouse モードでタップ（触れて 感度×2 ms 以内に離す）したとき左クリックが出ること。タップ判定の位置ずれ許容を上流の 10 から 100 に拡大（ユーザー承認 2026-10-15。実機で dx=26/63 の速いタップが不成立だったため。調査用 `[TAP]` Trace を `touchesBegan`／`touchesEnded` に追加。Trace レベルのため、そのまま残す［決定 6＝B］） | タッチパッド | 実機確認待ち（Double Tap ON／OFF の両方。Unbound でもタップが出ること） |
| 既存不具合の是正（ProfileEditor.xaml.cs ホバー） | プロファイル編集画面のコントローラー画像で、Left Touch の領域にホバーすると「Left Touch」と表示されること（`6f04ecf6`〔2025-11-11〕で `hoverIndexes[leftTouchConBtn]` が誤って欠落していたものを復元。同コミットの不要な `brpConBtn = 44` を削除。移行による退行ではない。ユーザー承認 2026-10-15） | プロファイル編集 | 実機確認待ち（Right Touch／Multitouch／Upper Touch／BRP のホバーも従来どおりであること） |
| Step10 からの申し送り（Mouse.cs の調査用 Trace） | `Mouse.cs` の `touchesBegan`／`touchesEnded` に、調査用の `[TAP]` Trace（Trace レベルのみ）が残っている。ユーザーの判断（2026-10-15）で残置。決定（2026-10-15）: Trace レベルのみのため、そのまま残す（決定 6＝B） | タッチパッド | 決定済み（残置。実機確認は不要） |
| Step10b からの申し送り（静的初期化の順序の確認） | `ScpUtil.cs` を型ごとに分割した（`Global`／`BackingStore` は別ファイル）。宣言は変更していないため、静的初期化の順序は変わらない想定。起動のログ（Logger の初期化、設定・プロファイルの読み込み、サービスの開始）が分割前と同じ順序・内容であること | 起動・設定 | 実機確認待ち（Step11 の起動シナリオ。Step10b の各ウェーブの起動スモークは問題なし） |
| 既存仕様の変更（Mouse.cs タップクリック保持） | タップの左クリックが、フォーカスされていないウィンドウでもフォーカスを切り替えること（上流は Down/Up がほぼ同時の瞬間クリックで効かなかった。`TapClickHoldMs`=50ms 保持。Double Tap ON の保留クリックも同様に保持し、指を離した後も `touchUnchanged` から処理が続くよう条件に `tappedOnce`／保持中を追加。ユーザー承認 2026-10-15） | タッチパッド | 実機確認待ち（Double Tap ON／OFF。タップ＆ホールドのドラッグ、押し込みクリック、連続タップが従来どおりであること） |

---

## 4. 検証手順とスケジュール

```text
【Phase6-Step11 実行フロー】
├─ 1. 自動テストスイートの完全実行 ＆ 静的解析スクリプト実行（第1層）
├─ 2. Checklist.md の展開と実機テスト環境（USB/BT。DS4 を主とする）の準備
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

- [x] 第1層: 自動テスト（既存 Actions / Standalone ＋ Phase6 新規追加単体テスト全件）が 100% 成功していること。
- [x] 第1層: 静的解析のゲート（ラチェット基準値の維持と、残存の分類台帳。§2.2）が満たされていること。
- [x] 第2層: `Phase6-Step11-RealDevice-Verification-Checklist.md` の全 10 セクションが実施され、実施可能な必須項目がすべて「○（合格）」であること。機材・環境が無い項目は、「確認不能（リスク受容）」として、代替の根拠・影響範囲・確認時期が記録されていること（決定 4）。
- [x] 利用できる実機（DS4 を主とする）で、入力追従・出力が確認されていること（対応デバイスは DS4・DualSense・DS3・SwitchPro・JoyCon。Vader 4 Pro は削除済み）。
- [x] 入力遅延・マウス追従性・ホットスワップ瞬断において体感上の劣化が一切ないこと。
- [x] 30分以上の連続操作でクラッシュ・メモリリークが皆無であること。
- [x] `Phase6-Step11-Completion-Report.md` が作成され、Step12（旧静的シム物理削除）の正式認可（GO）が記録されていること。
