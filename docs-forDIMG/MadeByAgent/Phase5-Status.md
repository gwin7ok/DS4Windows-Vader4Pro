# フェーズ5進捗管理表: DIサービス内部 Legacy 経路監査と責務分離

最終更新日: 2026-09-04（Step 6: AppSettings 永続化・状態管理のDI化 完了 / ドメイン1全完了）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5全体計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 全体進捗サマリ（ドメイン1全完了・ドメイン2着手へ）

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
| :--- | :--- | :--- | :--- | :--- |
| Step 1 | DIサービス内部 Legacy 経路の詳細監査と優先度付け | **完了** | 2026-09-03 | `Phase5-Step1-legacy-delegation-audit-report.md`。DI登録23サービスおよびコード全体（4大ブラインドスポット）の棚卸し完了。 |
| **【ドメイン1】** | **プロファイル・設定系（全完了）** | | | |
| Step 2 | プロファイル XML 読込・保存の責務分離 | **完了** | 2026-09-04 | `Phase5-Step2-Completion-Report.md`。`IProfileXmlStore`・`ProfileXmlStore`新設、排他ロック実装、`ProfileRepository`責務分離・シム化完了。 |
| Step 3 | プロファイル適用・復帰の一本化 | **完了** | 2026-09-04 | `Phase5-Step3-Completion-Report.md`。`IProfileApplicationService` 一本化、Halt保護内包、`Program.rootHub` 直参照排除、切断時クリア実装完了。 |
| Step 4 | Save／Apply の結果伝播と通知の統一 | **完了** | 2026-09-04 | `Phase5-Step4-Completion-Report.md`。保存成否伝播・GUI通知、`bool?` による通知自動解決、`[DI]` ログ標準化完了。 |
| Step 5 | AutoProfile（自動プロファイル切替）の自律実行系DI化 | **完了** | 2026-09-04 | `Phase5-Step5-Completion-Report.md`。`IAutoProfileService`新設、`IProcessInspector`集約、スレッド直列化、適用一本化・シム化完了。 |
| Step 6 | アプリ全体設定（AppSettings）の永続化・状態管理のDI化 | **完了** | 2026-09-04 | `Phase5-Step6-Completion-Report.md`。`IAppSettingsService`新設、同一XML排他ロック(`XmlIoLock`)統合、`Global.Save`/`Load`シム化完了。【ドメイン1全完了】 |
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

## 2. 詳細ステータス

### Step 1: 詳細監査と優先度付け【完了】
- 登録済み23サービスおよびコード全体の4大ブラインドスポットを特定・分類完了。
- 成果物: `Phase5-Step1-legacy-delegation-audit-report.md`。

### 【ドメイン1】プロファイル・設定系（Step 2 〜 Step 6）【全完了】
- **Step 2（プロファイルXML）**: **完了**（2026-09-04）。`IProfileXmlStore` 新設、同一XML排他ロック（`XmlIoLock`）実装、`ProfileRepository` 状態調整集約、`Global.LoadProfile` / `SaveProfile` シム化完了。成果物: `Phase5-Step2-Completion-Report.md`。
- **Step 3（プロファイル適用）**: **完了**（2026-09-04）。`IProfileApplicationService` 一本化、Halt保護内包、`DefaultProfileSwitcher` からの `Program.rootHub` 直参照完全排除、切断時スタッククリア実装完了。成果物: `Phase5-Step3-Completion-Report.md`。
- **Step 4（結果と通知）**: **完了**（2026-09-04）。保存成否伝播・GUIエラー通知、`bool?` による通知自動解決（通知オフ設定無視バグ解消）、`[DI]` ログ標準化完了。成果物: `Phase5-Step4-Completion-Report.md`。
- **Step 5（AutoProfile）**: **完了**（2026-09-04）。`IAutoProfileService` 新設、`IProcessInspector` へのネイティブ API 集約、スレッド直列化（§5.3）、適用一本化・シム化完了。成果物: `Phase5-Step5-Completion-Report.md`。
- **Step 6（AppSettings）**: **完了**（2026-09-04）。`IAppSettingsService` 新設、`ProfileXmlStore.XmlIoLock` へのファイル排他ロック共有統合（ロストアップデート防止 §5.1）、`Global.Save` / `Global.Load` シム化完了。成果物: `Phase5-Step6-Completion-Report.md`。

### 【ドメイン2】アクション系（Step 7 〜 Step 9）【次期着手】
- **Step 7（SpecialAction永続化）**: `BackingStore.actions` との二重管理・非同期バグ解消、`XmlIoLock` との排他整合。個別計画書完成。
- **Step 8（アクション連鎖）**: 巨大ファイル `Mapping.cs` を解体せず `IMappingActionDispatcher` で境界化しテスト容易性を確立済み。個別計画書完成。
- **Step 9（Actions基盤＆マクロ）**: `DefaultActionManager` への `IActionFactory` 注入、トグル状態内包、`DefaultMacroPlayer` への `IVirtualKBM` 注入組み込み済み。個別計画書完成。

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

以下の 6 大アーキテクチャ・ガードレールが、該当するすべての個別計画書に事前設計として組み込まれ、実装適用が進められています。

1. **[同一XML排他ロック・ロストアップデート防止]**: Step 2 実装完了、Step 6 にて `SaveAppSettingsXml` との完全な共有統合を達成（`ProfileXmlStore.XmlIoLock`）。
2. **[プロファイル適用時のHalt停止保証]**: Step 3 実装完了（`ProfileApplicationService.ApplyProfile` に内包）、Step 4 / Step 5 にて適用完了。
3. **[AutoProfileスレッド直列化]**: Step 5 実装完了（`AutoProfileService.CheckProfiles` の `_syncLock` 排他直列化保護）。
4. **[On-Demandパス評価]**: Step 10 の計画書に反映済み（起動順序逆転によるパス固定化防止）。
5. **[ViGEmネイティブドライバ保護]**: Step 12 の計画書に反映済み（PnP遅延・破棄順序の温存）。
6. **[物理切断時の復帰スタック残留防止]**: Step 3 実装完了（`ClearPendingRestore` / `ClearState` 実装済み）。

---

## 4. 次のアクション（【ドメイン2】アクション系 Step 7 への着手）

1. 【ドメイン2】アクション系の第 1 ステップである **「Step 7: SpecialAction 永続化の責務分離（`Phase5-Step7-Plan.md`）」の実コード改修作業に着手**する。
2. `ISpecialActionRepository` の `BackingStore.actions` 二重管理・非同期バグ解消、および `ProfileXmlStore.XmlIoLock` との排他整合を進める。

---

## 5. Phase5完了判定基準

- [x] DIサービス内部およびコード全体の Legacy 経路が詳細監査され、分類・記録されている（Step1完了）。
- [x] Step 2（プロファイル XML 読込・保存）の責務分離が完了している。
- [x] Step 3（プロファイル適用・復帰の一本化）の責務分離が完了している。
- [x] Step 4（Save／Apply の結果伝播と通知の統一）の責務分離が完了している。
- [x] Step 5（AutoProfile の自律実行系DI化）の責務分離が完了している。
- [x] Step 6（AppSettings の永続化・状態管理のDI化）の責務分離が完了している。
- [ ] 各ドメイン（プロファイル【完】、アクション、デバイス、UI）の責務分離が個別計画書通りに完了している。
- [ ] DI サービス内部から `Global` / `Program.rootHub` / `BackingStore` への不正な委譲・再委譲が排除されている。
- [ ] 6 大アーキテクチャ・ガードレールが実コードに正しく組み込まれ、競合・クラッシュ・リークが防止されている。
- [ ] 全自動テスト（Actions 85件、Standalone 13件、新規単体テスト）が常時グリーン（合格）を維持している。
- [ ] 実機コントローラーを用いた E2E 実機検証（実機CP4）が合格している。
- [ ] 未使用となった Legacy shim の安全な削除判断が行われ、移行が完了している。
