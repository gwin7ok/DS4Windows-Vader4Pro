# フェーズ5計画書: DIサービス内部 Legacy 経路監査と責務分離

作成日: 2026-09-02
最終更新日: 2026-09-03（A案ドメイン集約型順序および6大アーキテクチャ・ガードレールを反映）
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase4-Plan.md`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`

---

## 1. 位置づけ

Phase4 では DI 基盤、Composition Root、主要呼び出し元、ViewModel Factory を整備した。Phase5 では、DI サービスの内部実装が `Global`／`Program.rootHub`／Legacy 実体へ再委譲している経路を監査し、影響範囲と優先度を確定したうえで責務分離を進める。

DI インターフェース経由で呼ばれているだけでは、内部実体の DI 化完了とは判定しない。

---

## 2. Phase4 からの引継ぎと最適化された移行順序（A案）

Step1監査およびコード全体の詳細調査に基づき、従来の「個別クラスの発見順」から、依存関係・ファイルI/Oの共通性・テストの連続性を考慮した**「ドメイン集約型の最適順序（A案）」**へと再編した。

1. **プロファイル・設定ドメイン（Step 2〜6）**: `Profiles.xml` の XML I/O、適用、通知、自動切替（AutoProfile）、全体設定（AppSettings）を一気通貫で完成させる。
2. **アクションドメイン（Step 7〜9）**: SpecialAction 永続化からアクション連鎖、ActionManager、MacroPlayer まで入力アクション系を完全に集約。
3. **デバイス・インフラドメイン（Step 10〜12）**: PathService のキャッシュ撤廃、DS4Devices の抽象化、OutputSlot（仮想スロット）を整理。
4. **UI統合・検証ドメイン（Step 13〜15）**: 全サービスが揃った状態で ViewModel を接続し、自動/実機検証を経て Legacy shim を削除する。

---

## 3. 実施ステップ一覧（A案: 全15ステップ）

```
【Phase5 全15ステップ構成（ドメイン集約型）】
Phase5-Step1: 詳細監査と優先度付け【完了】

── [ドメイン1: プロファイル・設定系] ──
├─ Phase5-Step2: プロファイル XML 読込・保存（IProfileXmlStore新設・bool成否伝播）
├─ Phase5-Step3: プロファイル適用・復帰の一本化（IProfileApplicationService新設・Switcher統合）
├─ Phase5-Step4: Save／Apply の結果伝播と通知統一（通知自動解決・[DI]ログ標準化）
├─ Phase5-Step5: AutoProfile（自動プロファイル切替）の自律実行系DI化（旧Step 10）
├─ Phase5-Step6: アプリ全体設定（AppSettings）の永続化・状態管理のDI化（旧Step 11）

── [ドメイン2: アクション系] ──
├─ Phase5-Step7: SpecialAction 永続化の責務分離（旧Step 5: BackingStore.actions二重管理解消）
├─ Phase5-Step8: アクション連鎖処理の責務分離（旧Step 7: IMappingActionDispatcher新設）
├─ Phase5-Step9: Actions基盤とMacroPlayerの整理（旧Step 8+12: ActionManager/Factory整理 + DefaultMacroPlayer）

── [ドメイン3: デバイス・インフラ系] ──
├─ Phase5-Step10: 残存サービス境界の整理（旧Step 6: PathServiceキャッシュ撤廃・IDeviceStateAccessor活用）
├─ Phase5-Step11: デバイス検出・列挙の静的委譲分離（旧Step 9: Ds4DeviceRegistry / DS4Devicesの整理）
├─ Phase5-Step12: 出力スロット層（OutputSlot）の整理（旧Step 12: OutputSlotService / OutputSlotManager整理）

── [ドメイン4: UI統合・検証・クリーンアップ] ──
├─ Phase5-Step13: UI層（ViewModels）のDIサービス接続・残存静的参照の撲滅
├─ Phase5-Step14: 自動テストと実機検証（Actions/Standalone単体テスト + CP4実機検証）
└─ Phase5-Step15: Legacy shim の削除判断（残存静的シムの廃止判定）
```

---

## 4. 各ステップの詳細内容

### Phase5-Step1: 詳細監査と優先度付け【完了】
DI登録済み23サービスおよびコード全体を精査し、全残存Legacy経路を特定・分類した。成果物: `Phase5-Step1-legacy-delegation-audit-report.md`。

---

### 【ドメイン1: プロファイル・設定系】

#### Phase5-Step2: プロファイル XML 読込・保存
- **対象**: `IProfileRepository` / `ProfileRepository.cs`
- **内容**: `Global.LoadProfile`／`Global.SaveProfile` を対象とし、純粋なXML I/Oを担う `IProfileXmlStore` を新設して責務を分離する。保存成否の伝播のため最初から `bool SaveProfileXml` として定義する。

#### Phase5-Step3: プロファイル適用・復帰の一本化
- **対象**: `IProfileApplicationService` / `ProfileApplicationService.cs`, `DefaultProfileSwitcher.cs`
- **内容**: `ProfileApplicationService` をプロファイル適用の唯一の契約とし、`DefaultProfileSwitcher` で重複していた適用経路を統合する。呼び出し元からの `ControlService` / `Program.rootHub` 引き回しを排除し、スロット先行更新（`Global.ProfilePath`）の順序を保証する。

#### Phase5-Step4: Save／Apply の結果伝播と通知統一
- **対象**: `IProfileApplicationService.cs`, `ProfileApplicationService.cs`, `ProfileRepository.cs`
- **内容**: 保存成否の `bool` 伝播とエラーハンドリングを確定する。`IProfileApplicationService.ApplyProfile` の通知引数を `bool? displayNotification = null` とし、サービス内部でユーザー設定を自動解決することで、呼び出し元への不要な結合を防ぎつつ通知抑制バグを是正する。操作ログ（`[DI]`）を標準化する。

#### Phase5-Step5（旧Step10）: AutoProfile（自動プロファイル切替）の自律実行系DI化
- **対象**: `AutoProfileChecker.cs` / `AutoProfileHolder.cs`
- **内容**: バックグラウンドでアクティブウィンドウを監視しプロファイルを自動切替する処理を `IAutoProfileService`（仮）としてDI管理下に置く。切替実行を Step3・Step4 で完成した `IProfileApplicationService.ApplyProfile` 経由に統一し、`Global.ApplyProfile` / `Program.rootHub` 直参照を排除する。

#### Phase5-Step6（旧Step11）: アプリ全体設定（AppSettings）の永続化・状態管理のDI化
- **対象**: `Global.SaveSettings` / `Global.LoadSettings`、`Profiles.xml` 内 `<AppSettings>` セクション
- **内容**: コントローラー個別プロファイルとは独立したアプリ本体全般設定（スタートアップ起動、トレイ最小化、通知設定、UDPサーバー設定等）を扱う `IAppSettingsService` / `IAppSettingsRepository` を新設し、UIや起動処理からの静的直呼び出しを分離する。

---

### 【ドメイン2: アクション系】

#### Phase5-Step7（旧Step5）: SpecialAction 永続化の責務分離
- **対象**: `ISpecialActionRepository` / `SpecialActionRepository.cs`
- **内容**: `SpecialActionRepository` が独自リストを保持して `BackingStore.actions` と非同期になっていたサイレントバグを是正し、実データへの操作に一本化する。UIおよびActionManagerの呼び出し元調査を先行して実施する。

#### Phase5-Step8（旧Step7）: アクション連鎖処理の責務分離
- **対象**: `IProfileActionProvider` / `ProfileActionProvider.cs`, `IProfileActionChainService` / `ProfileActionChainService.cs`
- **内容**: `ProfileActionProvider` の `Global` 迂回路を解消し `BackingStore` 直接参照とする。巨大ファイル `Mapping.cs` の `DispatchProfileActionEdge` を薄いインターフェース `IMappingActionDispatcher` で境界化し、単体テストを完全自動化可能にする。

#### Phase5-Step9（旧Step8 + 旧Step12マクロ）: Actions基盤とMacroPlayerの整理
- **対象**: `IManagedActionManager` / `DefaultActionManager.cs`, `ActionManager.cs`, `ActionFactory.cs`, `ActionRegistry.cs`, `DefaultMacroPlayer.cs`
- **内容**: `DefaultActionManager` が依存している静的3系統（ActionManager/Factory/Registry）を整理・インスタンス化し、アクションのトグル状態管理および生成処理をDI境界へ引き戻す。また、`DefaultMacroPlayer` のキー入力送出を注入された `IVirtualKBM` 経由に切り替える。

---

### 【ドメイン3: デバイス・インフラ系】

#### Phase5-Step10（旧Step6）: 残存サービス境界の整理（Path / ProfileSettings / KBM）
- **対象**: `IPathService` / `PathService.cs`, `IProfileSettingsService` / `ProfileSettingsService.cs`, `IVirtualKBM`
- **内容**: `PathService` の初期化順序依存（起動時キャッシュ競合）をキャッシュ撤廃により根本解消する。`ProfileSettingsService` が参照する `Program.rootHub` を `IDeviceStateAccessor` に置換する。KBMアダプター（`IVirtualKBM`）の登録状況を精査・確定する。

#### Phase5-Step11（旧Step9）: デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離
- **対象**: `IDs4DeviceRegistry` / `Ds4DeviceRegistryAdapter.cs`, `DS4Devices.cs`
- **内容**: `IDs4DeviceRegistry` が静的 `DS4Devices` に全委譲している構造を整理し、デバイス列挙・検出の抽象化を進める。

#### Phase5-Step12（旧Step12スロット）: 出力スロット層（OutputSlot）の整理
- **対象**: `IOutputSlotService` / `OutputSlotService.cs`, `OutputSlotManager.cs`, `OutputSlotPersist.cs`
- **内容**: `OutputSlotService` が `Program.rootHub.outputSlotManager` に直結している構造およびスロット永続化（`OutputSlotPersist.cs`）の静的ファイルI/Oを整理・境界化する。

---

### 【ドメイン4: UI統合・検証・クリーンアップ】

#### Phase5-Step13: UI層（ViewModels）のDIサービス接続・残存静的参照の撲滅
- **対象**: `ControllersViewModel`, `SettingsViewModel`, `ProfileSettingsViewModel`, `SpecialActionsListViewModel`, `RecordBoxViewModel` 等
- **内容**: Step2〜Step12 で構築された各DIサービスをViewModelに注入・接続し、ViewModel内部に残存している `Global.ProfilePath[...]` や `Program.rootHub.DS4Controllers` の直接読み書きをDIサービス呼び出しに置換する。

#### Phase5-Step14: 自動テストと実機検証
- **内容**: 各ステップ完了時にビルド、Actions／Standalone 単体テスト、結合テストを実行する。自動テストで代替できない HID、ドライバ、長時間安定性は実機検証チェックリストで確認する。

#### Phase5-Step15: Legacy shim の削除判断
- **内容**: すべてのDIサービスおよびViewModelが新契約へ移行したことを確認したうえで、残存する `Global` / `Mapping` の不要となった静的シムメソッドの削除・非推奨化を判断する。

---

## 5. 実装における潜在的懸念点とアーキテクチャ・ガードレール

DI 化によりモジュール間の結合を切り離したことで、従来「同一スレッドで動いていた」「同じ順序で初期化されていた」という暗黙の前提に依存していた処理が潜在的な不具合として表面化するリスクがある。各 Step の実装および計画書策定時は、以下の **6大ガードレール** を厳格に順守する。

### 5.1 [ファイルI/O] 同一設定XMLに対する並行ファイルI/O競合とロストアップデート防止（Step 2, Step 6）
- **リスク**: `Profiles.xml` に `<Profiles>`（Step 2）と `<AppSettings>`（Step 6）が同居している。別々のDIサービスが非同期または並行して `XmlDocument` で上書き保存すると、ファイルロック競合（`IOException`）や、一方の変更が丸ごと消えるロストアップデートが発生する。
- **ガードレール**: ファイルI/O層（`IProfileXmlStore` / `BackingStore`）において、同一XMLファイルへのアクセスを保護するプロセス内排他ロック（`ReaderWriterLockSlim` または静的同期オブジェクト）を確立し、書き込みの直列化を保証する。

### 5.2 [入力スレッド] プロファイル適用時の「入力ポーリング停止（Halt）」保証（Step 3, Step 5）
- **リスク**: コントローラー入力ループ（毎秒250〜1000回）が稼働している最中に `Global.ApplyProfile`（マッピングテーブルやアクション辞書の再構築）を実行すると、走査スレッド側で `InvalidOperationException: コレクションが変更されました` が発生し、アプリがサイレントクラッシュする。
- **ガードレール**: `IProfileApplicationService.ApplyProfile` 内部において、対象スロットのデバイスがアクティブな場合は、必ず `device.HaltReportingRunAction(() => { ... })` により入力ポーリングを一時停止させた安全な状態でマッピング更新を実行する。

### 5.3 [マルチスレッド] AutoProfileタイマー vs UIスレッドのデータ競合防止（Step 5）
- **リスク**: `AutoProfileChecker` はバックグラウンドタイマースレッドで動作し、UI（`ControllersViewModel` 等）は WPF UI スレッドで動作する。`BackingStore` 内部のコレクションはスレッドセーフではないため、並行アクセスで競合状態に陥る。
- **ガードレール**: `AutoProfile` によるプロファイル適用処理を発火する際は、UI スレッド（WPF Dispatcher）へ適切にマーシャリングして直列化するか、BackingStore 操作を保護する排他制御を設ける。

### 5.4 [起動順序] On-Demand パス評価による初期化順序の逆転防止（Step 10）
- **リスク**: `AppHost.Initialize()`（DIコンテナ構築）は起動シーケンスの早期に実行されるが、`Global.FindConfigLocation()` による実行パス確定はその後に行われる。Singleton サービスのコンストラクタ内でパスをキャッシュすると、未確定の空パスやフォールバック先で固定化されてしまう。
- **ガードレール**: `PathService` のキャッシュフィールド（`_appDataPath`）を完全撤廃し、プロパティ getter で常に `Global.appdatapath` を直接返す設計とする。DI サービスはコンストラクタ内で設定パスを先読み・キャッシュせず、呼び出し時のオンデマンド評価を徹底する。

### 5.5 [ネイティブドライバ] OutputSlot（ViGEm）のPnP非同期遅延とリソース破棄順序の維持（Step 12）
- **リスク**: 仮想コントローラーのプラグイン／アンプラグは Windows カーネルドライバ（`ViGEmBus.sys`）との非同期 PnP 処理を伴う。DI コンテナ破棄時等にドライバハンドルの破棄順序や完了待機が崩れると、ドライバハングや OS レベルのデバイス認識スタック（最悪の場合はBSoD）を引き起こす。
- **ガードレール**: `OutputSlotManager` のドライバ通信キューおよび既存の `ViGEmClient` 破棄順序を崩さず、薄いアダプター境界に留めて物理層の挙動を完全に温存する。

### 5.6 [状態管理] コントローラー物理切断時の一時プロファイル（TempProfile）残留防止（Step 3, Step 5）
- **リスク**: 一時プロファイル適用中に USB 抜去や Bluetooth 切断が起きると、復帰アクション（ボタンを離すイベント）が発火しないため、復帰スタックに古いプロファイルが残留し、次回接続時に一時プロファイルから復帰できなくなる。
- **ガードレール**: `ControlService` のデバイス切断イベント（`DeviceRemoved`）と連動し、物理切断検知時に該当スロットの一時プロファイル保留スタックおよびフラグを強制クリアするクリーンアップを組み込む。

---

## 6. 完了条件

- [ ] Step2〜Step12 の各領域において、DIサービス実装内部の `Global`／`Program.rootHub`／静的実体への再委譲が解消されていること。
- [ ] バックグラウンド自律実行系（AutoProfile）およびアプリ全体設定（AppSettings）がDIコンテナ経由で管理されていること。
- [ ] 各 ViewModel 内部から静的直アクセスが排除され、DIサービス経由で動作していること（Step13）。
- [ ] 第5章に規定された **6大アーキテクチャ・ガードレール**（ファイル排他、Halt停止、スレッド直列化、オンデマンドパス評価、ドライバ破棄順、切断時クリーンアップ）が遵守されていること。
- [ ] 既存の全単体テストが成功し、新設された単体テストのカバレッジが確保されていること。
- [ ] ビルドエラーおよび警告の増加がないこと。
- [ ] 実機検証チェックリストにより、コントローラー入力・プロファイル切替・SpecialAction発火が正常動作すること。

---

## 7. 進行ルール

- **マイクロステップの原則**: 各Stepは必ず個別計画書を作成し、承認を得てからマイクロタスク単位で実装する。
- **No Feature Drop**: リファクタリングによる機能削減、設定値欠落、通知抑制不全を絶対に発生させない。
- **ガードレール順守の原則**: 第5章の潜在リスクに対する予防策を、該当する各Stepの個別計画書に明記した上で着手する。
- **スクリプト提供ルール**: 作業成果物の保存・更新は必ず `.github/PowerShell-script-generation-rules-for-deliverables.md` に従ったPowerShellスクリプトで行う。
