# フェーズ6進捗管理表: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

最終更新日: 2026-09-09（Phase6-Plan.md 承認・コミット後、Status新設時点）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase6計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`
前フェーズ進捗書: `docs-forDIMG/MadeByAgent/Phase5-Status.md`

---

## 0. 着手前提（重要）

**Phase6は、Phase5-Step14（自動テスト・実機検証）およびStep15（Legacy shim削除判断）の完了を前提とする。** 現時点（本Status作成時点）でPhase5-Step14の実機検証は未実施であり、Phase6の実装（Step1監査を含む）は開始していない。本ドキュメントはPhase6着手後の進捗記録用に先行して整備したものであり、進捗欄はすべて「未着手」である。

| 前提条件 | 状態 |
|---|---|
| Phase5-Step14: 自動テスト実行・実機検証チェックリスト完了 | 未実施 |
| Phase5-Step15: Legacy shim削除判断 | 未着手 |
| 実機CP4: Phase5総合E2E実機検証 | 未着手 |

上記が完了した時点で、Phase6-Step1（詳細監査）に着手する。

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
| :--- | :--- | :--- | :--- | :--- |
| Step 1 | 詳細監査と対象確定 | **未着手** | - | 前提: Phase5-Step14/15完了後に着手。成果物: `Phase6-Step1-Global-Usage-Classification-Report.md`（予定） |
| **【ドメイン1】** | **コア層（信号変換・入力監視境界）** | | | |
| Step 2 | `ControlService.cs` のGlobal直参照解消（39件） | **未着手** | - | 最優先・最難関。ホットパス性能維持ガードレール適用予定 |
| Step 3 | `Mapping.cs` 残存参照の再点検（12件） | **未着手** | - | `Mapping`完全instance化は対象外。既存方針の範囲内で対応 |
| **【ドメイン2】** | **信号出力層** | | | |
| Step 4 | `Mouse.cs` / `MouseCursor.cs` のGlobal直参照解消（41件） | **未着手** | - | マウス感度・加速度の実機体感確認を要する |
| **【ドメイン3】** | **起動・UI層** | | | |
| Step 5 | `App.xaml.cs` 起動シーケンスの整理（22件） | **未着手** | - | On-Demandパス評価原則（Phase5-Step10継承）を適用 |
| Step 6 | `ProfileEditor.xaml.cs` のGlobal直参照解消（18件） | **未着手** | - | Phase5-Step13の対象漏れ箇所。Step13と同一パターンで対応予定 |
| Step 7 | `SettingsViewModel.cs`他 主要ViewModel群の解消（39件） | **未着手** | - | `SettingsViewModel`/`MainWindowsViewModel`/`TrayIconViewModel`/`ProfileSettingsViewModel` |
| Step 8 | 残りの小型UIファイル群の解消（29件） | **未着手** | - | `WelcomeDialog`/`PresetOption`/`SaveWhere`/`BindingWindow` |
| **【ドメイン4】** | **検証・仕上げ** | | | |
| Step 9 | 自動テスト・実機検証 | **未着手** | - | Step2・Step4は実機での連続入力回帰確認が必須項目 |
| Step 10 | 呼出元0件シムの物理削除判断 | **未着手** | - | `[Obsolete]`付与、即時削除は行わない |

**全体進捗: 0/10ステップ完了（0%）**

---

## 2. 詳細ステータス

### Step 1: 詳細監査と対象確定【未着手】
- Phase6-Plan.md §0.2記載の193件（`ControlService.cs`39件、`Mouse.cs`27件、`SettingsViewModel.cs`23件、`App.xaml.cs`22件、`ProfileEditor.xaml.cs`18件、`MouseCursor.cs`14件、`Mapping.cs`12件、`PresetOption.cs`10件、`WelcomeDialog.xaml.cs`9件、`MainWindowsViewModel.cs`8件、`SaveWhere.xaml.cs`/`BindingWindow.xaml.cs`/`TrayIconViewModel.cs`各5件、`ProfileSettingsViewModel.cs`4件）について、(a)単純リダイレクト、(b)中程度作業、(c)要設計の3分類を確定する予定。
- 着手条件: Phase5-Step14/15完了後。

### 【ドメイン1】コア層（Step 2〜3）【未着手】
- **Step 2（ControlService.cs）**: 未着手。既存`IProfileSettingsService`/`IDeviceStateService`等への単純リダイレクトが主体になる見込みだが、Step1監査で確定させる。ホットパス（入力ポーリングループ、毎秒250〜1000回）に該当する参照は置換前後のベンチマーク比較を必須とする。
- **Step 3（Mapping.cs）**: 未着手。`Mapping`完全instance化は継続して対象外。既存の`IDeviceStateAccessor`委譲パターンの範囲内でのみ対応する。

### 【ドメイン2】信号出力層（Step 4）【未着手】
- **Step 4（Mouse.cs / MouseCursor.cs）**: 未着手。マウス出力設定（感度・加速度カーブ等）が中心と推測。完了判定にはマウスエミュレーションの実機体感確認を含む。

### 【ドメイン3】起動・UI層（Step 5〜8）【未着手】
- **Step 5（App.xaml.cs）**: 未着手。Composition Root構築前後のタイミング依存領域。On-Demandパス評価原則（Phase5-Step10継承）を適用。
- **Step 6（ProfileEditor.xaml.cs）**: 未着手。Phase5-Step13完了報告書で判明した「対象漏れ」箇所。Step13の`AppHost.GetService<T>() ?? Global.XxxInstance`パターンを踏襲予定。
- **Step 7（主要ViewModel群）**: 未着手。`SettingsViewModel.cs`, `MainWindowsViewModel.cs`, `TrayIconViewModel.cs`, `ProfileSettingsViewModel.cs`が対象。
- **Step 8（小型UIファイル群）**: 未着手。`WelcomeDialog.xaml.cs`, `PresetOption.cs`, `SaveWhere.xaml.cs`, `BindingWindow.xaml.cs`が対象。件数が少なく1PRでまとめて対応可能な見込み。

### 【ドメイン4】検証・仕上げ（Step 9〜10）【未着手】
- **Step 9（自動テスト・実機検証）**: 未着手。各Step完了ごとにビルド・既存自動テストを実行する運用とする。
- **Step 10（Legacy shim削除判断）**: 未着手。Phase6完了時点で呼出元0件が確定した`Global`メンバに`[Obsolete]`を付与し、削除候補リストを更新する（即時削除はしない）。

---

## 3. ガードレール対策の適用状況（Phase6-Plan.md §3準拠）

| # | ガードレール | 適用対象Step | 状態 |
|---|---|---|---|
| 1 | [新規] ホットパス性能維持 | Step2, Step4 | 未適用（未着手） |
| 2 | [継承] ファイルI/O排他ロック | 該当Stepなし（Phase6ではXML I/O自体は変更しない） | - |
| 3 | [継承] Halt保証 | Step2 | 未適用（未着手） |
| 4 | [継承] スレッド直列化 | 該当時に適用 | - |
| 5 | [継承] On-Demandパス評価 | Step5 | 未適用（未着手） |
| 6 | [継承] ViGEmドライバ破棄順序 | Phase6スコープ外（§0.3で明示的除外） | 対象外 |
| 7 | [継承] 切断時クリーンアップ | 該当時に適用 | - |
| 8 | [新規] ドキュメント記述の実地確認原則 | 全Step共通 | 本Status作成時点で継続適用中 |

---

## 4. 次のアクション

1. Phase5-Step14（自動テスト実行・実機検証）を完了させる。
2. Phase5-Step15（Legacy shim削除判断）を完了させる。
3. 上記完了後、Phase6-Step1（詳細監査）に着手し、`Phase6-Step1-Global-Usage-Classification-Report.md`を作成する。
4. Step1完了後、本Statusのステップ別ステータスを順次更新する。

---

## 5. Phase6完了判定基準（Phase6-Plan.md §4を転記）

- [ ] Step1で確定した対象（真の除外分を除く実質193件相当）の呼出元が、全てDIサービス経由の呼び出しに置換されていること。
- [ ] `ControlService.cs`, `Mapping.cs`, `Mouse.cs`, `MouseCursor.cs`のホットパスにおいて、置換前後で体感可能な性能劣化がないことを実機確認済みであること。
- [ ] 既存の全自動テスト（Actions／Standalone／Phase4・5で追加された単体テスト）が成功を維持していること。
- [ ] `ProfileEditor.xaml.cs`を含むUI層全体で、Phase5-Step13と同水準の静的参照撲滅が完了していること。
- [ ] Phase6完了時点で呼出元0件となった`Global`メンバに`[Obsolete]`が付与され、削除候補リストが更新されていること。
- [ ] ビルドエラーおよび警告の増加がないこと。