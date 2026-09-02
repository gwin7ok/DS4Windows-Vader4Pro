# フェーズ5進捗管理表: DIサービス内部 Legacy 経路監査と責務分離

最終更新日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase4-Status.md`

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
|---|---|---|---|---|
| Step 1 | DIサービス内部 Legacy 経路の詳細監査と優先度付け | **未着手** | - | Phase4で作成した `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md` を基準に、全DIサービスの内部参照を分類・優先順位付けする。 |
| Step 2 | プロファイル XML 読込・保存の責務分離 | **未着手（計画）** | - | `ProfileRepository`／`BackingStore.LoadProfile`／`SaveProfile` の再委譲を分離。既存のロード順、既定値、欠落設定、カルチャ、例外、ログを維持する。 |
| Step 3 | プロファイル適用・復帰の責務分離 | **未着手（計画）** | - | 通常GUI切替、編集画面 Save／Apply、SpecialAction、AutoProfile の適用経路を統一する。 `IProfileApplicationService` を実体の所有者とする。 |
| Step 4 | Save／Apply の操作結果と通知の統一 | **未着手（計画）** | - | 保存成否、再適用成否、操作ログ、`ProfileChanged` 通知を明示的かつテスト可能にする。 |
| Step 5 | SpecialAction 永続化の責務分離 | **未着手（計画）** | - | `SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions` 再委譲を分離し、Actions XML CRUD と runtime再構築の境界を整理する。 |
| Step 6 | 残存DIサービス境界の整理 | **未着手（計画）** | - | `PathService`、`ProfileSettingsService`、デバイス状態、KBMアダプター等の残存 `Global`／`rootHub` 参照を整理する。 |
| Step 7 | 自動テストと実機検証 | **基準検証完了・Phase5検証未着手** | - | Debugビルド成功、Actions 85/85件、Standalone 13/13件を確認済み。責務分離後の追加テストと実機CP4相当の検証は未実施。 |
| Step 8 | Legacy shim の削除判断 | **未着手（計画）** | - | 全呼び出し元、内部再委譲、実機経路、フォールバック使用実績を確認後、shimごとに存置／削除を判断する。 |
| **実機CP4** | **Phase5 総合E2E実機検証** | **未着手（計画）** | - | XML読込・保存、通常／一時プロファイル適用、Save／Apply、通知、SpecialAction、AutoProfile、接続・切断、長時間安定性を検証する。 |

---

## 2. 詳細ステータス

### Step 1: DIサービス内部 Legacy 経路の詳細監査と優先度付け（未着手）
- **Phase4からの監査結果を引継ぎ**:
  - `ProfileRepository`: `Global.LoadProfile`／`Global.SaveProfile` へ再委譲。
  - `ProfileApplicationService`: `Global.ApplyProfile`／`Global.LoadProfile`／`Global.LoadTempProfile` へ再委譲。
  - `SpecialActionRepository`: `Global.LoadActions`／`Global.SaveActions` へ再委譲。
  - `PathService`: `Global.appdatapath` を参照。
  - `ProfileSettingsService`: `Program.rootHub` を参照。
- **基準成果物**: `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md`。
- **次の作業**: DI登録済みサービスを対象に、参照を設定データ、XML永続化、プロファイル適用、副作用、通知、パス、デバイス状態、KBMへ分類し、移行順序とテスト方法を確定する。

### Step 2: プロファイル XML 読込・保存の責務分離（未着手）
- 現在は `ProfileRepository` がDIの入口である一方、XMLの実処理は `BackingStore` と `Global` のLegacy境界に残っている。
- `BackingStore.LoadProfile`／`SaveProfile` の大規模処理を一括置換せず、XML reader／writer、設定値反映、デバイス副作用、Action再構築の責務を段階的に分ける。
- ProfileEditorのロード不具合に対する `ControlService` 解決処理は修正済み。今後も `control` null時の挙動と既存ログを回帰確認する。

### Step 3: プロファイル適用・復帰の責務分離（未着手）
- 通常GUI切替とProfileEditor Save／Applyは、現在も最終的に `Global` のプロファイル適用処理へ到達する。
- 一時プロファイル、通常プロファイル、復帰スナップショット、`touchpadActive` 等の状態、入力停止・出力デバイス操作を欠落させずに整理する。
- 二重ロード、`isTemp`、`SelectedProfile`／`OlderProfilePath` の更新順、Action連鎖を重点的に検証する。

### Step 4: Save／Apply の操作結果と通知の統一（未着手）
- 現在はSave処理の成否、既存プロファイルの再適用成否、ログ、通知が一つの結果として扱われていない。
- 保存結果と再適用結果を戻り値または結果型で扱い、失敗箇所を操作ログから判別できるようにする。
- `ProfileChangedNotification` による通知抑制と、常時必要なログ出力を分離し、通常切替とSave／Applyの通知経路を統一する。

### Step 5: SpecialAction 永続化の責務分離（未着手）
- `SpecialActionRepository` はDI登録済みだが、Actions XMLのロード／保存実体は `Global`／`BackingStore` に残っている。
- Actions XMLのCRUD、インメモリ一覧、`ActionManager` のruntime状態再構築を分離する。
- 既存の重複防止、Invalid actionログ、runtime synthetic stateのリセットを維持する。

### Step 6: 残存DIサービス境界の整理（未着手）
- `PathService`、`ProfileSettingsService`、デバイス状態、KBMアダプターに残るLegacy参照を、Step 1の分類結果に基づき整理する。
- `BackingStore` を当面共有する場合は、共有理由、影響範囲、終了条件を文書化する。
- Phase4で導入済みの互換シムは、新経路の動作確認前には削除しない。

### Step 7: 自動テストと実機検証（基準検証完了・追加検証未着手）
- **完了済みの基準検証**:
  - Debugアプリビルド: 成功、警告・エラーなし。
  - Actionsテスト: 85/85件パス。
  - Standaloneテスト: 13/13件パス。
  - Phase4のCP1〜CP3: 完了済み。
- **Phase5で必要な検証**:
  - XML読込・保存の互換性と欠落設定処理。
  - 通常／一時プロファイルの適用・復帰。
  - ProfileEditor Save／Applyの保存結果、再適用、ログ、通知。
  - SpecialAction、AutoProfile、接続・切断、出力デバイス状態。
  - HID、WPF、ドライバ依存、長時間安定性の実機確認。

### Step 8: Legacy shim の削除判断（未着手）
- shim削除は責務分離と自動／実機検証の完了後に実施可否を判断する。
- 判断単位はshimごととし、呼び出し元、内部再委譲、フォールバック使用実績、ロールバック方法を記録する。
- 削除作業は責務分離の変更と分け、既存機能の維持を確認してから実施する。

---

## 3. 現在の既知課題

- プロファイルXMLの読込・保存実体は、DIサービス経由で呼ばれていても `Global`／`BackingStore` に残っている。
- プロファイル適用実体は `Global.ApplyProfile` に残っており、通常GUI切替とProfileEditor Save／Applyの完全なDI一本化は未完了。
- Save／Applyの保存成否、再適用成否、ログ、通知の統一は未完了。
- `SpecialActionRepository`、`PathService`、`ProfileSettingsService` に内部Legacy参照が残っている。
- 最新変更は未コミット・未プッシュであり、実機でのPhase5検証は未実施。

---

## 4. 次のアクション

1. Step 1として、既存監査報告を起点にDIサービス内部のLegacy参照を再分類し、移行優先度を確定する。
2. Step 2のXML reader／writer責務分離について、既存ロード順・保存形式・副作用境界の設計文書を作成する。
3. 最小単位の責務分離を実装し、対象スライスのビルドとテストを直ちに実行する。
4. Step 3〜Step 6の完了後、Phase5固有の自動テスト、実機CP4検証、shim削除判断へ進む。
5. 全検証完了後に変更をコミットし、リモート反映を行う。

---

## 5. Phase5完了判定

- [ ] DIサービス内部の `Global`／`rootHub`／Legacy実体への再委譲が分類・記録されている。
- [ ] プロファイルXML読込・保存・適用・復帰の責務境界が明確になっている。
- [ ] 通常GUI切替とProfileEditor Save／Applyが同一のDI適用契約を使用している。
- [ ] Save／Applyの保存結果、再適用結果、ログ、通知が検証可能になっている。
- [ ] SpecialAction永続化と残存DIサービス境界が整理されている。
- [ ] 自動テスト、必要な実機検証、長時間安定性確認が完了している。
- [ ] Legacy shimごとの存置または削除判断が根拠付きで記録されている。
