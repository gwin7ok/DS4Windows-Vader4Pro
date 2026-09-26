# Phase5-Step1追補監査レポート: 個別計画書12文書の対象外にある3つの未把握問題

作成日: 2026-09-03
対象ブランチ: `For-DI-migration-work`
関連ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md` 〜 `Phase5-Step13-Plan.md`（全12文書、本レポート作成にあたり全件通読済み）
- `.github/copilot-instructions.md`

---

## 0. 本レポートの位置づけ

Phase5-Step1監査レポートおよびStep2〜Step13の個別計画書（全12文書）と、GitHub上の実コード（`ControlService.cs`、`App.xaml.cs`、`MainWindow.xaml.cs`、`OutputSlotService.cs`、`OutputSlotManagerControl.xaml.cs`、`OutputKBMHandlerAdapter.cs`、`AutoProfileChecker.cs`、`ServiceRegistration.cs` 等）を突き合わせた結果、既存の計画書のどこにも記載されていない3つの問題を発見した。

いずれも「実装済みの計画に反する」ものではなく、「Step1監査の把握漏れにより、そもそも計画のスコープに入っていない」種類の問題である。着手前に対処方針の確認を得るため、詳細と選択肢を本レポートにまとめる。

---

## 1. 発見1: `App.rootHub` という第二の静的シングルトン（最重要）

### 1.1 詳細

`Program.cs` の `Program.rootHub` は、これまでの全監査（Phase4含む）で「静的シングルトン」として一貫して追跡されてきた。しかし実際には、`App.xaml.cs` にも全く同型・同名の独立した静的フィールドが存在する。

対象コード（`DS4Windows/App.xaml.cs`）:
- `public static DS4Windows.ControlService rootHub;`

起動シーケンス（`CreateControlService()`）では、以下のように同一インスタンスを2つの独立した静的フィールドへ手動でコピーしている。
- `rootHub = AppHost.GetService<DS4Windows.ControlService>();`
- `DS4Windows.Program.rootHub = rootHub;`

この結果、`MainWindow.xaml.cs`（コードビハインド、約1,000行超）では、同一メソッド内で `App.rootHub.running` / `App.rootHub.Start()` / `App.rootHub.DS4Controllers[...]` / `App.rootHub.OutputslotMan` と、`Program.rootHub.xxx` が**混在・並存**して数十箇所使用されている。具体例として以下がある。
- 電源イベント処理（`PowerEventArrive`）
- IPCコマンド処理（`WndProc` 内の `outputslot` / `query` コマンド）
- サービス開始・停止ボタン（`ChangeService`）
- UDP/OSCサーバー制御チェックボックス（`UseUdpServerCk_Click` 等）
- プロファイル切替コンボボックス（`SelectProfCombo_SelectionChanged`）

`AutoProfileChecker.cs` の `SetAndWaitServiceStatus` メソッドも `App.rootHub.running` を直接参照している。

### 1.2 Step2〜13との整合性確認

Step13計画書（UI層の静的参照撲滅）の完了判定基準は「**ViewModel** 内部から `Global`／`Program.rootHub` 直参照が0件になっていること」であり、対象ファイル一覧も `ControllersViewModel.cs` 等6つのViewModelのみである。コードビハインドである `MainWindow.xaml.cs` はスコープに含まれておらず、`App.rootHub` という語自体が全12文書中どこにも一度も登場しない。

なお、Step3計画書のタスク3-1「通常GUI切替の呼び出し元調査」を実施すれば、`MainWindow.xaml.cs` の `SelectProfCombo_SelectionChanged` が `App.rootHub` を直接使っていることに必然的に行き着く設計になっており、既存計画との接点はある。

### 1.3 対処の選択肢

| 選択肢 | 内容 | メリット | デメリット |
|---|---|---|---|
| **A. 即時統合（`App.rootHub` を撤廃し `Program.rootHub` に一本化）** | `App.xaml.cs` の静的フィールドを削除し、`App.rootHub` を参照している全箇所を `Program.rootHub` に置換するピンポイント修正をPhase5内の独立タスクとして今すぐ実施する。 | ・二重管理の実害（将来的な同期漏れバグ）を根本から排除できる。<br>・以降のStep13等での「静的参照0件」判定が単一の対象で完結し、判定の信頼性が上がる。 | ・`MainWindow.xaml.cs` は数十箇所の置換が必要で、影響範囲がコードビハインド全体に及ぶ。<br>・Phase5の既存ステップ順序（ドメイン1→2→3→4）を一時中断させることになる。<br>・Step3-1等、他Stepの調査タスクと重複作業になる可能性がある。 |
| **B. Step13スコープ拡張（追加サブタスクとして計画書に追記）** | Step13計画書の対象に `MainWindow.xaml.cs` と `AutoProfileChecker.cs` を明示的に追加し、既存のPure DI方針（`ViewModelFactory` 経由）に準じて対応する。 | ・既存のドメイン順序（1→2→3→4）を崩さずに済む。<br>・Step2〜12のバックエンド完成後にまとめて対応でき、手戻りが最小。<br>・Step13は元々「UI層の静的参照撲滅の総仕上げ」であり、性質的に最も自然な置き場所。 | ・Step13の作業量が当初想定より大幅に増える（`MainWindow.xaml.cs` は1,000行超のコードビハインドで、単純なViewModel注入とは勝手が異なる）。<br>・Step13完了までの間、`App.rootHub`/`Program.rootHub` 二重管理は放置される（実害が顕在化するリスクはStep13まで残る）。 |
| **C. 静観（現状シムとして許容、Phase5対象外とする）** | §2.1「フォールバック実装・シム維持の原則」を根拠に、`App.rootHub` は `Program.rootHub` と同一インスタンスを指す限り実害はないと判断し、Phase5では触れず将来のクリーンアップフェーズ（Step15）に委ねる。 | ・作業ゼロで済み、Phase5の進行を一切妨げない。 | ・二重シングルトンという設計上の欠陥そのものは残り続ける。<br>・将来、片方だけが更新される変更が紛れ込むと（例: 再起動処理やテストコードでの部分的な差し替え等）、サイレントな不整合バグを生む潜在リスクが残る。<br>・Step1監査レポート自体の「Program.rootHub」件数カウントが不正確なままになる。 |

### 1.4 推奨

**選択肢B（Step13スコープ拡張）を推奨する。**
理由: `App.rootHub` は現状 `Program.rootHub` と同一インスタンスを指しており、今すぐ緊急対応が必要な機能不全ではない（選択肢Cの「実害なし」という前提は短期的には成立する）。一方で、Phase5全体の「Legacy経路の完全撲滅」という目的に照らせば放置は許容できない（選択肢Cは不採用）。`MainWindow.xaml.cs` の大規模改修はStep2〜12で完成する全バックエンドサービスに依存する性質上、Step13（UI統合の総仕上げ）で他の静的参照撲滅作業とまとめて行うのが最も手戻りが少ない。Phase5進行を中断させる選択肢Aは、緊急性に見合わないコストと判断する。

---

## 2. 発見2: `IOutputSlotService` が実運用から切り離された並行実装である疑い

### 2.1 詳細

Step12計画書は「`OutputSlotService` が `Program.rootHub.outputSlotManager` に直結している」という前提のもと、`Program.rootHub` 参照0件化を目標としている。しかし実際の `OutputSlotService.cs`（`DS4Windows/DS4Control/Services/OutputSlotService.cs`）を確認したところ、以下の事実が判明した。

- `OutputSlotManager` にも `Program.rootHub` にも一切参照がなく、`_outputDevices` / `_deviceTypes` という**独自のインメモリ配列**のみを保持する、完全に孤立した実装である。
- 実際にゲームパッドの抜き差し（ViGEm操作）を担っている実体は、`ControlService` が直接保持する private フィールド `outputslotMan`（生の `OutputSlotManager` インスタンス）である。
- `MainWindow.xaml.cs` の起動処理では `slotManControl.SetupDataContext(controlService: App.rootHub, App.rootHub.OutputslotMan)` のように、`IOutputSlotService` を一切経由せず `ControlService.OutputslotMan` プロパティ経由で生インスタンスを直接UIに渡している。
- IPCコマンド処理（`outputslot` / `query` コマンド）も同様に `Program.rootHub.OutputslotMan` を直接操作している。

つまり `IOutputSlotService` はDI登録済みで「クリーン」と判定されているが、**現在の実行パス上でどこからも呼ばれていない可能性が高い**（Phase4で発見された `AppHost` 死コード問題と同種のパターン）。

### 2.2 Step12計画書との齟齬

Step12計画書が示す実装イメージ（`OutputSlotManager slotManager = null` を受け取り `Program.rootHub?.outputSlotManager` にフォールバックする設計）は、「既存の `OutputSlotService` が `Program.rootHub` に依存している」という誤った前提の上に立てられている。実際には依存関係がゼロなので、Step12をこのまま実施しても「存在しない依存を除去する」形になり、本来解決すべき問題（`ControlService`／`MainWindow.xaml.cs`／IPCコマンド処理が生の `OutputSlotManager` を直接操作している実態）には触れられない。

### 2.3 対処の選択肢

| 選択肢 | 内容 | メリット | デメリット |
|---|---|---|---|
| **A. 実配線（`IOutputSlotService` を本来の唯一の経路にする）** | `ControlService` の `outputslotMan` フィールドを `IOutputSlotService` 経由の実装に置き換え、`MainWindow.xaml.cs` のUI配線・IPCコマンド処理もすべて `IOutputSlotService` 経由に書き換える。 | ・設計思想（DIサービスが単一の真実の情報源になる）が完全に実現する。<br>・Step12の当初目的（`Program.rootHub` 排除）が名実ともに達成される。 | ・`ControlService.cs`（2,900行超）のホットパス（`PluginOutDev`／`UnplugOutDev`／`Start`／`Stop` 等、ViGEmネイティブドライバと直結する箇所）への変更が必要で、§5.5ガードレール（ViGEm破棄順序）に抵触するリスクが最も高い。<br>・作業量・検証コストがStep12の当初想定を大幅に超える。 |
| **B. 設計の見直し（`IOutputSlotService` の役割を再定義）** | `IOutputSlotService` を「実運用の中核」ではなく、UIやIPC層が状態を**読み取る**ための補助的な射影（read-only view）として再定義し、書き込み系操作（プラグイン／アンプラグ）は当面 `ControlService.outputslotMan` 経由のまま残す設計にStep12計画書を修正する。 | ・ViGEmホットパスに手を入れずに済み、§5.5ガードレールのリスクを最小化できる。<br>・Step12の作業量を現実的な範囲に抑えられる。 | ・「DIサービスが単一の真実の情報源」という設計原則からは後退する。<br>・読み取り専用射影と書き込み系の二重構造が新たに生まれ、将来の保守性に別の負債を残す。 |
| **C. 現状維持＋注記追加のみ（Step12着手を保留）** | `IOutputSlotService` の実配線状況を「未確定の既知課題」としてStep1監査レポートおよびStep12計画書に注記するだけに留め、実装作業はPhase5の他ドメイン完了後、または個別のミニStepとして仕切り直す。 | ・誤った前提のままStep12に着手して手戻りが発生するリスクを事前に防げる。<br>・判断を急がず、選択肢A/Bのどちらが適切か十分検討する時間を確保できる。 | ・ドメイン3（Step10〜12）の完了が遅延する。<br>・「出力スロット層の整理」というPhase5の目標達成が先送りになる。 |

### 2.4 推奨

**選択肢C（現状維持＋注記追加、Step12着手前に方針確定）を暫定推奨する。**
理由: 発見2はViGEmネイティブドライバ（§5.5ガードレール、最悪の場合BSoDのリスク）に関わる領域であり、誤った前提のまま実装に着手することは他のどの発見よりも実害リスクが大きい。まず選択肢AとBのどちらの設計思想を採るかをこの場でご判断いただいた上で、Step12計画書を正確な前提に修正してから着手するのが安全である。長期的な設計方針としては選択肢Aが理想だが、ホットパスへの影響が大きいため、着手前に実機検証チェックリストの拡充を含めた追加のリスク評価を行うことを推奨する。

---

## 3. 発見3: UDP(DSU)/OSCサーバーのライフサイクル管理がどのStepにも未割当

### 3.1 詳細

Step1監査レポート §4-5-1 は「バックグラウンド自律実行系」として `AutoProfileChecker`／`AutoProfileHolder` と `UdpServer.cs` を同一のブラインドスポットとして扱っていたが、実際は性質が異なる。

- `UdpServer.cs` 自体は静的参照フリーである（コンストラクタでデリゲート `GetPadDetail` を受け取るだけの健全な設計）。
- 真の静的結合は `ControlService.cs` 内のライフサイクル管理コードにある。`ChangeUDPStatus`／`UseUDPPort`／`ChangeOSCListenerStatus`／`ChangeOSCSenderStatus`／`ChangeUdpSmoothingAttrs` 等が、`Global.getUDPServerPortNum()`／`Global.UDPServerSmoothingMincutoffChanged` イベント／`Global.getOSCServerPortNum()` 等を多数直接参照している。

Step2〜13の全12文書を検索したが、`UdpServer`／`OSC`／`ChangeUDPStatus`／`ChangeOSCListenerStatus` 等の語は一度も登場しない。Step10（残存サービス境界）・Step11（デバイス検出）のいずれにも含まれておらず、Step1の「再構成案（全15ステップ）」でも脱落している。

### 3.2 対処の選択肢

| 選択肢 | 内容 | メリット | デメリット |
|---|---|---|---|
| **A. Phase5に新規Stepとして追加（Step14, 15を後ろにずらす）** | 「UDP/OSCサーバーのライフサイクル管理DI化」を新しい個別Stepとして起票し、Phase5-Plan.mdのロードマップに正式追加する。 | ・Step1監査で発見された全ブラインドスポットが漏れなくカバーされ、Phase5完了時点でのLegacy経路撲滅の網羅性が保証される。 | ・Phase5全体のステップ数・作業量が増加し、スケジュールが延びる。<br>・`ControlService.cs` 自体への変更を伴うため、Step11（デバイス検出）等、同じく `ControlService.cs` を触る他Stepとの競合・手戻りリスクがある。 |
| **B. Phase6以降（`ControlService.cs` 分割フェーズ）に先送り** | UDP/OSC関連は `ControlService.cs` という巨大ファイル自体の分割・責務分離という、Phase5のドメイン単位の粒度を超えた大きな課題の一部と位置づけ、明示的にPhase5のスコープ外として次フェーズに送る。 | ・Phase5のドメイン単位（プロファイル／アクション／デバイス／UI）という設計方針と整合する。<br>・`ControlService.cs` への変更を1つのフェーズに集約でき、影響範囲の見積もりがしやすくなる。 | ・Phase5完了時点でも `Global` 静的参照がUDP/OSC領域に残存し続け、「Phase5完了 = Legacy経路撲滅」という達成感に穴が残る。 |
| **C. 対象外として明示的に記録するのみ（何もしない）** | Phase5-Status.mdの「既知の対象外事項」として一文だけ記録し、対応時期は未定のまま据え置く。 | ・作業ゼロで即決できる。 | ・「未定」のまま放置されると、将来的に忘却されるリスクがある（実際、Step1で一度発見されていたにもかかわらず12文書のどこにも反映されなかったのと同じ経緯を辿る）。 |

### 3.3 推奨

**選択肢B（Phase6以降＝`ControlService.cs` 分割フェーズへ明示的に先送り）を推奨する。**
理由: UDP/OSCのライフサイクル管理は、Step8計画書が `Mapping.cs` に対して採用した「巨大ファイルの内部には深入りせず、境界のみを薄いインターフェースで囲む」という考え方が本来当てはまる領域だが、対象が `ControlService.cs`（DIコンテナのComposition Root寄りの中核クラス）であるため、Phase5の他Stepと並行して行うと競合リスクが高い。選択肢Cの「何もしない」は今回と同じ見落としの再発を招くため不採用とし、少なくとも選択肢Aと同水準の「明示的な記録」は必須とした上で、実施時期はPhase6に回すB案を推奨する。ただし選択肢Aとの優劣は僅差であり、Phase5のスケジュール上の余裕次第では選択肢Aも十分に妥当である。

---

## 4. まとめ表

| # | 発見 | 実害の緊急度 | 推奨選択肢 | 備考 |
|---|---|---|---|---|
| 1 | `App.rootHub` 二重静的シングルトン | 中（現状は実害なしだが将来的な不整合リスクあり） | B: Step13スコープ拡張 | Step3-1調査で自然に発覚する経路と重なる |
| 2 | `IOutputSlotService` 未配線（並行実装疑い） | 高（ViGEmネイティブドライバ領域、§5.5ガードレール対象） | C: Step12着手前に方針確定（暫定） | 選択肢A/Bのどちらを採るかの意思決定が必須 |
| 3 | UDP/OSCサーバーのライフサイクル管理未対応 | 低〜中（実害は限定的だが監査の網羅性に影響） | B: Phase6（ControlService.cs分割）へ先送り | 選択肢Aとの優劣僅差 |

---

## 5. 次のアクション（承認待ち）

1. 発見1について、選択肢Bで進めてよいか確認。承認が得られ次第、Step13計画書に `MainWindow.xaml.cs` と `AutoProfileChecker.cs` を追加対象として明記する改訂を行う。
2. 発見2について、選択肢A（実配線）と選択肢B（役割再定義）のどちらの設計思想を採るか意思決定を仰ぐ。決定後、Step12計画書を正確な前提に修正してから着手する。
3. 発見3について、選択肢Bで進めてよいか確認。承認が得られ次第、Phase5-Plan.md・Phase5-Status.mdに「Phase6以降で対応予定」の注記を追加する。
