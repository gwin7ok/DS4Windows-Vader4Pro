# フェーズ5進捗管理表: DIサービス内部 Legacy 経路監査と責務分離

最終更新日: 2026-09-03（全15ステップ再編・Step1完了・最新状況を反映）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5全体計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
|---|---|---|---|---|
| Step 1 | DIサービス内部 Legacy 経路の詳細監査と優先度付け | **完了** | 2026-09-03 | `Phase5-Step1-legacy-delegation-audit-report.md`。DI登録23サービスおよびコード全体（4大ブラインドスポット）の棚卸し完了。 |
| Step 2 | プロファイル XML 読込・保存の責務分離 | **計画書承認済・未着手** | - | `Phase5-Step2-Plan.md`。`IProfileXmlStore` 新設、`bool SaveProfileXml` 統一済み。 |
| Step 3 | プロファイル適用・復帰の責務分離 | **計画書承認済・未着手** | - | `Phase5-Step3-Plan.md`。`IProfileApplicationService` 一本化、`ControlService` 引数排除、スロット先行更新注記。 |
| Step 4 | Save／Apply の操作結果と通知の統一 | **計画書承認済・未着手** | - | `Phase5-Step4-Plan.md`。通知自動解決（`bool?`）、保存成否伝播、`[DI]` ログ統一。 |
| Step 5 | SpecialAction 永続化の責務分離 | **計画書承認済・未着手** | - | `Phase5-Step5-Plan.md`。`BackingStore.actions` との二重管理・非同期バグ特定、実データ一本化。 |
| Step 6 | 残存DIサービス境界の整理 | **計画書承認済・未着手** | - | `Phase5-Step6-Plan.md`。`PathService` キャッシュ完全撤廃、`IDeviceStateAccessor` 活用、KBM調査。 |
| Step 7 | プロファイルアクション解決・連鎖処理の責務分離 | **計画書承認済・未着手** | - | `Phase5-Step7-Plan.md`。`IMappingActionDispatcher` 新設による `Mapping.cs` 境界化。 |
| Step 8 | Actions基盤（ActionManager）の静的委譲分離 | **未着手（個別計画書未作成）** | - | `DefaultActionManager` が依存する静的3系統（ActionManager/Factory/Registry）の整理。 |
| Step 9 | デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離 | **未着手（個別計画書未作成）** | - | 静的 `DS4Devices` への全委譲解消とデバイス列挙抽象化。 |
| Step 10 | AutoProfile（自動プロファイル切替）の自律実行系DI化 | **未着手（個別計画書未作成）** | - | 【新設】`AutoProfileChecker` を `IAutoProfileService` としてDI化し適用経路を統一。 |
| Step 11 | アプリ全体設定（AppSettings）の永続化・状態管理のDI化 | **未着手（個別計画書未作成）** | - | 【新設】`Profiles.xml` 内 `<AppSettings>` を扱う `IAppSettingsService` の新設。 |
| Step 12 | アクション実行・出力スロット層の内部委譲整理 | **未着手（個別計画書未作成）** | - | 【新設】`DefaultMacroPlayer` の KBM 直結および `OutputSlotService` の `rootHub` 直結整理。 |
| Step 13 | UI層（ViewModels）のDIサービス接続・残存静的参照撲滅 | **未着手（個別計画書未作成）** | - | 【新設】各 ViewModel 内部の `Global` / `rootHub` 直参照を各DIサービス経由に差し替え。 |
| Step 14 | 自動テストと実機検証 | **基準検証完了・Phase5検証未着手** | - | 旧Step10。Debugビルド、Actions(85件)、Standalone(13件)成功済み。責務分離後の追加検証。 |
| Step 15 | Legacy shim の削除判断 | **未着手** | - | 旧Step11。全呼び出し元のDI移行完了後に不要shimの削除・非推奨化を判断。 |
| **実機CP4** | **Phase5 総合E2E実機検証** | **未着手** | - | 実機コントローラーを用いた全系統（XML、切替、通知、Action、AutoProfile、安定性）検証。 |

---

## 2. 詳細ステータス

### Step 1: DIサービス内部 Legacy 経路の詳細監査と優先度付け【完了】
- **実績**:
  - 第1次監査: `ServiceRegistration.cs` 登録済みの全23サービスを精査し、Legacy委譲・参照を分類。
  - 第2次監査（追加調査）: バックグラウンド実行系（AutoProfile/UdpServer）、AppSettings永続化、出力スロット、ViewModels内部の4大ブラインドスポットを特定。
  - 成果物 `Phase5-Step1-legacy-delegation-audit-report.md` を作成・反映完了。全15ステップへの再編を確定。

### Step 2 〜 Step 7: 個別計画書の作成と承認【承認済・実装待機中】
- **Step 2（プロファイルXML）**: `IProfileXmlStore` を新設し、戻り値 `bool SaveProfileXml` を先行統一した計画書を承認済み。
- **Step 3（プロファイル適用）**: `IProfileApplicationService` への適用一本化、`ControlService` 引数排除、スロット先行更新を盛り込んだ計画書を承認済み。
- **Step 4（結果と通知）**: 通知自動解決（`bool? displayNotification = null`）により Switcher への不要な結合を防ぐ設計を承認済み。
- **Step 5（SpecialAction）**: `SpecialActionRepository` の二重管理・非同期バグを是正する方針を承認済み。
- **Step 6（残存サービス）**: `PathService` のキャッシュ撤廃、`ProfileSettingsService` の `IDeviceStateAccessor` 活用方針を承認済み。
- **Step 7（アクション連鎖）**: `IMappingActionDispatcher` による `Mapping.cs` 境界化方針を承認済み。

### Step 8 〜 Step 13: 新設ステップ【個別計画書未作成】
- 全体計画書 `Phase5-Plan.md` にて概要・対象範囲・責務を定義済み。Step2〜Step7の実装進行に合わせて順次個別計画書を作成する。

### Step 14: 自動テストと実機検証（基準検証完了・Phase5検証未着手）
- **完了済みの基準検証**:
  - Debugアプリビルド: 成功、警告・エラーなし。
  - Actionsテスト: 85/85件パス。
  - Standaloneテスト: 13/13件パス。
- **Phase5で必要な検証**:
  - XML読込・保存成否、通常／一時プロファイルの適用・復帰、通知抑制、SpecialAction永続化、AutoProfile自律実行、HID実機確認。

### Step 15: Legacy shim の削除判断（未着手）
- 責務分離および Step14 の自動／実機検証完了後に、残存 shim ごとに存置／削除を判定する。

---

## 3. 現在の既知課題

- プロファイル XML の読込・保存実体が依然として `Global` / `BackingStore` に依存（Step2で解消予定）。
- プロファイル切替経路が二重化し、通知抑制設定が無視されるバグが存在（Step3, Step4で解消予定）。
- `SpecialActionRepository` が実データ `BackingStore.actions` と非同期になっている（Step5で解消予定）。
- `AutoProfileChecker` が DI 管理外で自律実行され、`Global.ApplyProfile` を直呼び出ししている（Step10で解消予定）。
- アプリ全体設定（`AppSettings`）の専用 DI サービスが存在しない（Step11で解消予定）。
- 各 ViewModel 内部に直接の `Global` / `Program.rootHub` 参照が残存している（Step13で解消予定）。

---

## 4. 次のアクション

1. 承認済みの `Phase5-Step2-Plan.md` に基づき、**Step2（プロファイル XML 読込・保存の責務分離）** の実コード改修作業（マイクロタスク Step2-1 の実装）に着手する。
2. Step2 の完了後、順次 Step3〜Step7 の実装・単体テストを進行させる。
3. 実装の進捗に合わせて Step8 以降の個別計画書を順次策定する。

---

## 5. Phase5完了判定基準

- [x] DIサービス内部およびコード全体の Legacy 経路が詳細監査され、分類・記録されている（Step1完了）。
- [ ] Step2〜Step7 の各サービス内部の Legacy 再委譲が解消され、単体テストが整備されていること。
- [ ] Step8〜Step9（Actions基盤・デバイス列挙）の静的委譲が整理されていること。
- [ ] Step10〜Step12（AutoProfile、AppSettings、出力スロット）がDI化されていること。
- [ ] Step13 により、全 ViewModel 内部から静的直アクセスが排除され、DIサービス経由に統一されていること。
- [ ] 全自動テスト（ユニットテスト・統合テスト）が成功し、リグレッションがないこと。
- [ ] 実機CP4検証チェックリストにより、コントローラーの全機能が正常動作すること。
- [ ] Legacy shim の存置／削除判断が完了していること。
