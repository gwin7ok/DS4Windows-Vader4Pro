# フェーズ4 進捗管理表: Global 分割と ViewModel DI 化

最終更新日: 2026-08-31
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
| Step 9 | ViewModel DI 移行 (Pattern C) | **未着手 (次)** | - | 実行時引数付き ViewModel（ProfileEditViewModel, **RecordBoxViewModel**, SpecialActionsViewModel, KBMEditorViewModel 等）の Factory 移行 |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | 未着手 (計画) | - | 全画面 UI 結合・ViewModel 直接 new 全廃実機検証（Step9完了時） |
| Step 10 | Phase3 引継ぎ再確認・シム整理 | 未着手 | - | 残存シムの監査と全体健全性確認 |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | 未着手 (計画) | - | 残存シム整理後・フェーズ4完了総合実機検証（Step10完了時） |

---

## 2. 詳細ステータス

### Step 8: ViewModel DI 移行 (Pattern B: 共有依存 ViewModel) (完了)
- **ViewModel DI 化 (永続資産)**:
  - `ControllersViewModel`: 全画面 MVVM 対称性向上のため新設（`IDeviceStateService`, `IProfileSettingsService`, `IProfileRepository` をコンストラクタ注入）。
  - `MainWindowsViewModel`: アプリケーション全体共有 ViewModel として `ServiceRegistration.cs` に Singleton 登録。
- **View DataContext DI 解決 (永続資産)**:
  - `MainWindow.xaml.cs`: `mainWinVM = new MainWindowsViewModel()` を全廃し、`DS4WinWPF.AppHost.GetService<MainWindowsViewModel>()` 経由の DI 解決へ移行。
- **単体テスト**: `DS4WindowsTests/PatternBViewModelTests.cs`（ControllersViewModel, MainWindowsViewModel の解決および Singleton 検証、全件通過）。
- **ビルド・テスト検証**: 全プロジェクトビルド警告0・エラー0、既存テスト全件成功（77/77件, 13/13件）。
