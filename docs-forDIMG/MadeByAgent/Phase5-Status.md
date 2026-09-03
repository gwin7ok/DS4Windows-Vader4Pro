# フェーズ5進捗管理表: DIサービス内部 Legacy 経路監査と責務分離

最終更新日: 2026-09-04（Step 2: プロファイル XML 読込・保存の責務分離 完了）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5全体計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 全体進捗サマリ（全個別計画書 完成・実装フェーズ進行中）

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
| :--- | :--- | :--- | :--- | :--- |
| Step 1 | DIサービス内部 Legacy 経路の詳細監査と優先度付け | **完了** | 2026-09-03 | `Phase5-Step1-legacy-delegation-audit-report.md`。DI登録23サービスおよびコード全体（4大ブラインドスポット）の棚卸し完了。 |
| **【ドメイン1】** | **プロファイル・設定系** | | | |
| Step 2 | プロファイル XML 読込・保存の責務分離 | **完了** | 2026-09-04 | `Phase5-Step2-Completion-Report.md`。`IProfileXmlStore`・`ProfileXmlStore`新設、排他ロック実装、`ProfileRepository`責務分離・シム化完了。 |
| Step 3 | プロファイル適用・復帰の一本化 | **個別計画書完成** | - | `Phase5-Step3-Plan.md`。`IProfileApplicationService` 一本化、Halt停止ガード、切断時リセット追記。 |
| Step 4 | Save／Apply の結果伝播と通知の統一 | **個別計画書完成** | - | `Phase5-Step4-Plan.md`。通知自動解決（`bool?`）、保存成否伝播、`[DI]` ログ統一。 |
| Step 5 | AutoProfile（自動プロファイル切替）の自律実行系DI化 | **個別計画書完成** | - | `Phase5-Step5-Plan.md`。`IAutoProfileService` 新設、`IProcessInspector` 活用、直列化保証。 |
| Step 6 | アプリ全体設定（AppSettings）の永続化・状態管理のDI化 | **個別計画書完成** | - | `Phase5-Step6-Plan.md`。`IAppSettingsService` 新設、`Profiles.xml` ロストアップデート防止排他統合。 |
| **【ドメイン2】** | **アクション系** | | | |
| Step 7 | SpecialAction 永続化の責務分離 | **個別計画書完成** | - | `Phase5-Step7-Plan.md`。`BackingStore.actions` 二重管理解消、排他ロック整合。 |
| Step 8 | アクション連鎖処理の責務分離 | **個別計画書完成** | - | `Phase5-Step8-Plan.md`。`IMappingActionDispatcher` 新設による `Mapping.cs` 境界化。 |
| Step 9 | Actions基盤とMacroPlayerの整理 | **個別計画書完成** | - | `Phase5-Step9-Plan.md`。`DefaultActionManager` 整理、`DefaultMacroPlayer` への `IVirtualKBM` 注入。 |
| **【ドメイン3】** | **デバイス・インフラ系** | | | |
| Step 10 | 残存サービス境界の整理 | **個別計画書完成** | - | `Phase5-Step10-Plan.md`。`PathService` キャッシュ完全撤廃（On-Demand化）、`IDeviceStateAccessor` 活用。 |
| Step 11 | デバイス検出・列挙の静的委譲分離 | **個別計画書完成** | - | `Phase5-Step11-Plan.md`。`IDs4DeviceRegistry` 契約強化と `DS4Devices` 段階的シム化。 |
| Step 12 | 出力スロット層（OutputSlot）の整理 | **個別計画書完成** | - | `Phase5-Step12-Plan.md`。`IOutputSlotStore` 新設、ViGEm ネイティブドライバ保護（PnP遅延維持）。 |
| **【ドメイン4】** | **UI統合・検証・クリーンアップ** | | | |
| Step 13 | UI層（ViewModels）のDIサービス接続・残存静的参照撲滅 | **個別計画書完成** | - | `Phase5-Step13-Plan.md`。Pure DI 堅持、コア4大ViewModel優先順次改修による静的直参照一掃。 |
| Step 14 | 自動テストと実機検証 | **基準検証完了・Phase5検証未着手** | - | Debugビルド、Actions(85件)、Standalone(13件)成功済み。実装完了後の全件テスト。 |
| Step 15 | Legacy shim の削除判断 | **未着手** | - | 全呼び出し元のDI移行完了後に不要shimの削除・非推奨化を判断。 |
| **実機CP4** | **Phase5 総合E2E実機検証** | **未着手** | - | 実機コントローラーを用いた全系統（XML、切替、通知、Action、AutoProfile、安定性）検証。 |

---

## 2. 詳細ステータス（全ドメインの計画が完了・実装進行中）

### Step 1: 詳細監査と優先度付け【完了】
- 登録済み23サービスおよびコード全体の4大ブラインドスポットを特定・分類完了。
- 成果物: `Phase5-Step1-legacy-delegation-audit-report.md`。

### 【ドメイン1】プロファイル・設定系（Step 2 〜 Step 6）【実装進行中】
- **Step 2（プロファイルXML）**: **完了**（2026-09-04）。`IProfileXmlStore` 新設、`ProfileXmlStore` による同一XML排他ロック（`_fileLock`）実装、`ProfileRepository` への状態調整集約、`Global.LoadProfile` / `SaveProfile` シム化完了。成果物: `Phase5-Step2-Completion-Report.md`。
- **Step 3（プロファイル適用）**: `IProfileApplicationService` 一本化、入力ポーリング停止（`Halt`）保証、切断時スタッククリア組み込み済み。個別計画書完成。
- **Step 4（結果と通知）**: 通知自動解決（`bool?`）、保存成否伝播、Halt下成否ログ、`[DI]` ログ統一組み込み済み。個別計画書完成。
- **Step 5（AutoProfile）**: `IAutoProfileService` 新設、`IProcessInspector` 活用によるテスト自動化、スレッド直列化組み込み済み。個別計画書完成。
- **Step 6（AppSettings）**: `IAppSettingsService` 新設、`IProfileXmlStore` とのファイル排他ロック共有（ロストアップデート防止）組み込み済み。個別計画書完成。

### 【ドメイン2】アクション系（Step 7 〜 Step 9）【全計画書 策定・承認完了】
- **Step 7（SpecialAction永続化）**: `BackingStore.actions` との二重管理・非同期バグ解消、調査先行タスク組み込み済み。
- **Step 8（アクション連鎖）**: 巨大ファイル `Mapping.cs` を解体せず `IMappingActionDispatcher` で境界化しテスト容易性を確立済み。
- **Step 9（Actions基盤＆マクロ）**: `DefaultActionManager` への `IActionFactory` 注入、トグル状態内包、`DefaultMacroPlayer` への `IVirtualKBM` 注入組み込み済み。

### 【ドメイン3】デバイス・インフラ系（Step 10 〜 Step 12）【全計画書 策定・承認完了】
- **Step 10（残存サービス）**: `PathService` のキャッシュ完全撤廃（On-Demand評価）、`ProfileSettingsService` の `IDeviceStateAccessor` 活用組み込み済み。
- **Step 11（デバイス検出）**: `IDs4DeviceRegistry` 契約強化と `DS4Devices` 段階的シム化（生デバイス列挙に専念）組み込み済み。
- **Step 12（出力スロット）**: `IOutputSlotStore` 新設（永続化抽象化）、ViGEm ネイティブドライバ保護（PnP遅延・破棄順序の温存）組み込み済み。

### 【ドメイン4】UI統合・検証・クリーンアップ（Step 13 〜 Step 15）
- **Step 13（UI層DI接続）**: **計画書策定完了**。Pure DI（`ViewModelFactory` 経由の手渡し注入）堅持、コア4大ViewModel優先順次改修による静的直参照一掃。
- **Step 14（自動テスト・実機検証）**: 実装完了後の全自動テスト（ユニット・統合テスト）および実機CP4検証。
- **Step 15（Legacy shim削除判断）**: 全経路のDI移行完了後に不要となった静的シムの削除。

---

## 3. ガードレール対策の確立状況（Phase5-Plan §5 準拠）

以下の 6 大アーキテクチャ・ガードレールが、該当するすべての個別計画書に事前設計として組み込まれ、Step 2 より実装適用が開始されました。

1. **[同一XML排他ロック・ロストアップデート防止]**: Step 2 実装完了（`_fileLock` 実装済み）、Step 6 にて共有統合予定。
2. **[プロファイル適用時のHalt停止保証]**: Step 3, Step 4, Step 5 の計画書に反映済み（コレクション変更クラッシュ防止）。
3. **[AutoProfileスレッド直列化]**: Step 5 の計画書に反映済み（UI Dispatcher マーシャリング）。
4. **[On-Demandパス評価]**: Step 10 の計画書に反映済み（起動順序逆転によるパス固定化防止）。
5. **[ViGEmネイティブドライバ保護]**: Step 12 の計画書に反映済み（PnP遅延・破棄順序の温存）。
6. **[物理切断時の復帰スタック残留防止]**: Step 3, Step 4, Step 5 の計画書に反映済み（状態リーク防止）。

---

## 4. 次のアクション（Step 3 への着手）

1. ドメイン1 の第2ステップである **「Step 3: プロファイル適用・復帰の一本化（`Phase5-Step3-Plan.md`）」の実コード改修作業に着手**する。
2. `IProfileApplicationService` の適用処理一本化（`DefaultProfileSwitcher` 統合）、および入力ポーリング停止（`Halt`）ガードの実装を進める。

---

## 5. Phase5完了判定基準

- [x] DIサービス内部およびコード全体の Legacy 経路が詳細監査され、分類・記録されている（Step1完了）。
- [x] Step 2（プロファイル XML 読込・保存）の責務分離が完了している。
- [ ] 各ドメイン（プロファイル、アクション、デバイス、UI）の責務分離が個別計画書通りに完了している。
- [ ] DI サービス内部から `Global` / `Program.rootHub` / `BackingStore` への不正な委譲・再委譲が排除されている。
- [ ] 6 大アーキテクチャ・ガードレールが実コードに正しく組み込まれ、競合・クラッシュ・リークが防止されている。
- [ ] 全自動テスト（Actions 85件、Standalone 13件、新規単体テスト）が常時グリーン（合格）を維持している。
- [ ] 実機コントローラーを用いた E2E 実機検証（実機CP4）が合格している。
- [ ] 未使用となった Legacy shim の安全な削除判断が行われ、移行が完了している。
