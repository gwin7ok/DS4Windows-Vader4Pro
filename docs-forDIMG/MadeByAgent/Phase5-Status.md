# フェーズ5進捗管理表: DIサービス内部 Legacy 経路監査と責務分離

最終更新日: 2026-09-03（A案: ドメイン集約型全15ステップ再編・最新状況を反映）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5全体計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 全体進捗サマリ（A案: ドメイン集約型）

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
|---|---|---|---|---|
| Step 1 | DIサービス内部 Legacy 経路の詳細監査と優先度付け | **完了** | 2026-09-03 | `Phase5-Step1-legacy-delegation-audit-report.md`。DI登録23サービスおよびコード全体（4大ブラインドスポット）の棚卸し完了。 |
| **【ドメイン1】** | **プロファイル・設定系** | | | |
| Step 2 | プロファイル XML 読込・保存の責務分離 | **計画書承認済・未着手** | - | `Phase5-Step2-Plan.md`。`IProfileXmlStore` 新設、`bool SaveProfileXml` 統一済み。 |
| Step 3 | プロファイル適用・復帰の一本化 | **計画書承認済・未着手** | - | `Phase5-Step3-Plan.md`。`IProfileApplicationService` 一本化、`ControlService` 引数排除、スロット先行更新注記。 |
| Step 4 | Save／Apply の結果伝播と通知の統一 | **計画書承認済・未着手** | - | `Phase5-Step4-Plan.md`。通知自動解決（`bool?`）、保存成否伝播、`[DI]` ログ統一。 |
| Step 5 | AutoProfile（自動プロファイル切替）の自律実行系DI化 | **未着手（個別計画書未作成）** | - | 【新設・旧Step10】`AutoProfileChecker` を `ApplyProfile` に接続しDI化。 |
| Step 6 | アプリ全体設定（AppSettings）の永続化・状態管理のDI化 | **未着手（個別計画書未作成）** | - | 【新設・旧Step11】`Profiles.xml` 内 `<AppSettings>` を扱う `IAppSettingsService` の新設。 |
| **【ドメイン2】** | **アクション系** | | | |
| Step 7 | SpecialAction 永続化の責務分離 | **計画書承認済・未着手** | - | 【旧Step5】`SpecialActionRepository` 二重管理・非同期バグ解消（後でリネーム予定）。 |
| Step 8 | アクション連鎖処理の責務分離 | **計画書承認済・未着手** | - | 【旧Step7】`IMappingActionDispatcher` 新設、`Mapping.cs` 境界化（後でリネーム予定）。 |
| Step 9 | Actions基盤とMacroPlayerの整理 | **未着手（個別計画書未作成）** | - | 【旧Step8+12】`ActionManager`/`Factory` 整理 + `DefaultMacroPlayer` の KBM 注入。 |
| **【ドメイン3】** | **デバイス・インフラ系** | | | |
| Step 10 | 残存サービス境界の整理 | **計画書承認済・未着手** | - | 【旧Step6】`PathService` キャッシュ完全撤廃、`IDeviceStateAccessor` 活用（後でリネーム予定）。 |
| Step 11 | デバイス検出・列挙の静的委譲分離 | **未着手（個別計画書未作成）** | - | 【旧Step9】静的 `DS4Devices` への全委譲解消とデバイス列挙抽象化。 |
| Step 12 | 出力スロット層（OutputSlot）の整理 | **未着手（個別計画書未作成）** | - | 【旧Step12スロット】`OutputSlotService` と ViGEm 永続スロットの整理。 |
| **【ドメイン4】** | **UI統合・検証・クリーンアップ** | | | |
| Step 13 | UI層（ViewModels）のDIサービス接続・残存静的参照撲滅 | **未着手（個別計画書未作成）** | - | 【新設】各 ViewModel 内部の `Global` / `rootHub` 直参照を各DIサービス経由に差し替え。 |
| Step 14 | 自動テストと実機検証 | **基準検証完了・Phase5検証未着手** | - | Debugビルド、Actions(85件)、Standalone(13件)成功済み。責務分離後の追加検証。 |
| Step 15 | Legacy shim の削除判断 | **未着手** | - | 全呼び出し元のDI移行完了後に不要shimの削除・非推奨化を判断。 |
| **実機CP4** | **Phase5 総合E2E実機検証** | **未着手** | - | 実機コントローラーを用いた全系統（XML、切替、通知、Action、AutoProfile、安定性）検証。 |

---

## 2. 詳細ステータス（ドメイン別）

### Step 1: DIサービス内部 Legacy 経路の詳細監査と優先度付け【完了】
- 第1次監査（登録済み23サービス）および第2次追加調査（コード全体・4大ブラインドスポット）が完了。
- `Phase5-Step1-legacy-delegation-audit-report.md` を作成・反映完了。

### 【ドメイン1】プロファイル・設定系（Step 2 〜 Step 6）
- **Step 2（プロファイルXML）**: `IProfileXmlStore` 新設、`bool SaveProfileXml` 統一済みの計画書承認完了。
- **Step 3（プロファイル適用）**: `IProfileApplicationService` への適用一本化、`ControlService` 引数排除、スロット先行更新注記の計画書承認完了。
- **Step 4（結果と通知）**: 通知自動解決（`bool? displayNotification = null`）による結合排除、`[DI]` ログ統一の計画書承認完了。
- **Step 5（AutoProfile）**: `AutoProfileChecker` を `ApplyProfile` に接続する新規ステップ（個別計画書未作成）。
- **Step 6（AppSettings）**: アプリ本体全般設定の永続化を担う `IAppSettingsService` 新設ステップ（個別計画書未作成）。

### 【ドメイン2】アクション系（Step 7 〜 Step 9）
- **Step 7（SpecialAction永続化・旧Step5）**: 二重管理バグ是正の計画書承認完了（計画書ファイル名リネーム待機）。
- **Step 8（アクション連鎖・旧Step7）**: `IMappingActionDispatcher` 新設による境界化の計画書承認完了（計画書ファイル名リネーム待機）。
- **Step 9（Actions基盤＆マクロ・旧Step8+12）**: `DefaultActionManager` および `DefaultMacroPlayer` を整理するステップ（個別計画書未作成）。

### 【ドメイン3】デバイス・インフラ系（Step 10 〜 Step 12）
- **Step 10（残存サービス・旧Step6）**: `PathService` キャッシュ撤廃、`ProfileSettingsService` 改善の計画書承認完了（計画書ファイル名リネーム待機）。
- **Step 11（デバイス検出・旧Step9）**: 静的 `DS4Devices` 抽象化ステップ（個別計画書未作成）。
- **Step 12（出力スロット・旧Step12）**: ViGEm 仮想スロット永続化整理ステップ（個別計画書未作成）。

### 【ドメイン4】UI統合・検証・クリーンアップ（Step 13 〜 Step 15）
- **Step 13（UI層DI接続）**: Step2〜12で完成した各DIサービスを全ViewModelへ接続する総仕上げステップ。
- **Step 14（自動テスト・実機検証）**: 責務分離完了後の自動テスト全件パスおよび実機CP4検証。
- **Step 15（Legacy shim削除判断）**: 全経路のDI移行完了後に不要となった静的シムの削除。

---

## 3. 現在の既知課題

- プロファイル XML の読込・保存実体が依然として `Global` / `BackingStore` に依存（Step2で解消予定）。
- プロファイル切替経路が二重化し、通知抑制設定が無視されるバグが存在（Step3, Step4で解消予定）。
- `AutoProfileChecker` がバックグラウンドで `Global.ApplyProfile` を直呼び出ししている（Step5で解消予定）。
- アプリ全体設定（`AppSettings`）の専用 DI サービスが存在しない（Step6で解消予定）。
- `SpecialActionRepository` が実データ `BackingStore.actions` と非同期になっている（Step7で解消予定）。
- 各 ViewModel 内部に直接の `Global` / `Program.rootHub` 参照が残存している（Step13で解消予定）。

---

## 4. 次のアクション

1. **個別計画書のファイル名・見出しリネーム（旧Step 5, 6, 7）**:
   - `Phase5-Step5-Plan.md` → `Phase5-Step7-Plan.md`
   - `Phase5-Step7-Plan.md` → `Phase5-Step8-Plan.md`
   - `Phase5-Step6-Plan.md` → `Phase5-Step10-Plan.md`
2. 承認済みの `Phase5-Step2-Plan.md` に基づき、**Step2（プロファイル XML 読込・保存の責務分離）** の実コード改修作業（マイクロタスク Step2-1 の実装）に着手する。
3. プロファイルドメインの進行に合わせて、新設された Step5（AutoProfile）および Step6（AppSettings）の個別計画書を順次策定する。

---

## 5. Phase5完了判定基準

- [x] DIサービス内部およびコード全体の Legacy 経路が詳細監査され、分類・記録されている（Step1完了）。
- [ ] ドメイン1（Step 2〜6: プロファイル・設定系）の Legacy 再委譲が解消されていること。
- [ ] ドメイン2（Step 7〜9: アクション系）の Legacy 再委譲が解消されていること。
- [ ] ドメイン3（Step 10〜12: デバイス・インフラ系）の Legacy 再委譲が解消されていること。
- [ ] ドメイン4（Step 13: UI層）により全 ViewModel から静的直参照が排除され、DI接続されていること。
- [ ] 全自動テスト（ユニットテスト・統合テスト）が成功し、リグレッションがないこと（Step 14）。
- [ ] 実機CP4検証チェックリストにより、コントローラーの全機能が正常動作すること（Step 14）。
- [ ] Legacy shim の存置／削除判断が完了していること（Step 15）。
