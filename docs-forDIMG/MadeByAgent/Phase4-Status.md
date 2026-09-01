# フェーズ4 進捗管理表: Global 分割と ViewModel DI 化

最終更新日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase4計画書: `docs-forDIMG/MadeByAgent/Phase4-Plan.md`

---

## 1. 全体進捗サマリ

| ステップ | 名称 | 状態 | 完了日 | 成果物・備考 |
|---|---|---|---|---|
| Step 0 | 現状棚卸し・基準テスト | **完了** | 2026-08-31 | `Phase4-Step0-Plan.md`, `Phase4-Step0-Completion-Report.md`, Globalメンバー442件/ViewModel生成29件棚卸し |
| Step 1 | IProfileSettingsService 実装化 | **完了** | 2026-08-31 | `IProfileSettingsService.cs`, `ProfileSettingsService.cs`, DI登録, Globalシム, `ProfileSettingsServiceTests.cs` |
| Step 2 | IProfileRepository 分離 | **完了** | 2026-08-31 | `IProfileRepository.cs`, `ProfileRepository.cs`, DI登録, Globalシム, `ProfileRepositoryTests.cs` |
| Step 3 | ISpecialActionRepository 分離 | **完了** | 2026-08-31 | `ISpecialActionRepository.cs`, `SpecialActionRepository.cs`, DI登録, Globalシム, `SpecialActionRepositoryTests.cs`, **実機検証CP1全件合格** |
| **実機CP1** | **データ中核層 実機検証** | **完了** | 2026-08-31 | `Phase4-Step3-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格) |
| Step 4 | 入力・出力・デバイス状態サービス | **完了** | 2026-08-31 | `IDeviceStateService.cs`, `IOutputSlotService.cs`, `DeviceStateService.cs`, `OutputSlotService.cs`, DI登録, Globalシム, 各単体テスト |
| Step 5 | 環境・UI・通知サービス | **完了** | 2026-08-31 | `IPathService.cs`, `IEnvironmentService.cs`, `INotificationService.cs`, `PathService.cs`, `EnvironmentService.cs`, `AppNotificationService.cs`, DI登録, Globalシム, 各単体テスト |
| Step 6 | Composition Root 一本化 | **完了** | 2026-08-31 | `AppHost.cs`, `ServiceRegistration.cs`, 全13バックエンドサービス集約, `CompositionRootTests.cs`, **実機検証CP2実施完了** |
| **実機CP2** | **全バックエンドDI＋Root一本化 実機検証** | **完了** | 2026-08-31 | `Phase4-Step6-RealDevice-Verification-Checklist.md` (実施完了。一部要調査項目はDI完了後に対応) |
| Step 7 | ViewModel DI 移行 (Pattern A) | **完了** | 2026-08-31 | `SettingsViewModel`, `LogViewModel`, `AboutViewModel` の DI 化完了、`PatternAViewModelTests.cs`（**RecordBoxViewModel は Step 9 Pattern C に正式引継ぎ**） |
| Step 8 | ViewModel DI 移行 (Pattern B) | **完了** | 2026-08-31 | `ControllersViewModel.cs` 新設、`MainWindowsViewModel` DI化、`MainWindow.xaml.cs` DI解決化、`PatternBViewModelTests.cs` |
| Step 9 | ViewModel DI 移行 (Pattern C) | **完了** | 2026-09-01 | `IViewModelFactory.cs`, `ViewModelFactory.cs`, DI登録, View直接new全廃, `PatternCViewModelTests.cs`, **Step9-4-α監査合格**, **実機検証CP3全件合格** |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | **完了** | 2026-09-01 | `Phase4-Step9-RealDevice-Verification-Checklist.md` (全12項目 ○ 合格) |
| Step 10 | Phase3 引継ぎ再確認・シム整理・[DI]ログ整備 | **一部進行中** | - | [DI]/[Legacy] Trace ログ整備は着手済み。呼び出し元DI直接参照化（Step10-2）を前倒しで追加実施中のため、実機検証CP4はStep10-2完了後に実施 |
| Step 10-2 | 呼び出し元の実稼働DIサービス直接参照化（フェーズ5前倒し・先行着手） | **Stage2 監査完了・追加変更の検証待ち** | 2026-09-02 | `ProfileSettingsViewModel`、`ProfileEditor`、`ControlService`、`Mapping` の対象設定系 `Global` 参照を DI 経由へ移行済み。Mapping の高頻度ループ内で毎回解決なし。今回の `ControlService` 追加移行と監査結果はユーザー側テスト・コミット待ち。次は Stage2 後実機検証と `△`・未実施項目の再評価。 |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | 未着手 (計画) | - | 残存シム整理後・フェーズ4完了総合実機検証（Step10・Step10-2完了時） |

---

## 2. 詳細ステータス

### Step 9: ViewModel DI 移行 (Pattern C: Factory DI) & 実機検証CP3 (完了)
- **Factory 契約・実装 (永続資産)**:
  - `DS4Windows/DI/IViewModelFactory.cs` (第4層 4-c Factory 契約)
  - `DS4Windows/DS4Control/Services/ViewModelFactory.cs` (DI永続Factoryサービス実装)
  - `ServiceRegistration.cs` にて `IViewModelFactory` を Singleton 登録。
- **View 直接 new の全廃 (第4層 4-a / 4-b 結合)**:
  - `ProfileEditor.xaml.cs`: `IViewModelFactory.CreateProfileSettingsViewModel`
  - `RecordBox.xaml.cs`: `IViewModelFactory.CreateRecordBoxViewModel`
  - `SpecialActionEditor.xaml.cs`: `IViewModelFactory.CreateSpecialActEditorViewModel`
  - `AutoProfiles.xaml.cs`: `IViewModelFactory.CreateAutoProfilesViewModel`
  - 全 29 箇所の ViewModel 直接 new 生成を完全全廃。
- **Step 9-4-α 全体監査**: `Phase4-Step9-Audit-Report.md`（全 4 層モデル・全 13 サービス・全 ViewModel の移行漏れゼロを確認・合格）。
- **単体テスト**: `DS4WindowsTests/PatternCViewModelTests.cs`（全件通過、回帰ゼロ、83/83件パス）。
- **実機動作検証 (Checkpoint 3)**: `Phase4-Step9-RealDevice-Verification-Checklist.md` に基づき全画面 UI 結合検証を実施。全 12 項目すべて合格（○）。
