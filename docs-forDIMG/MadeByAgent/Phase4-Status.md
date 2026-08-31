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
| Step 7 | ViewModel DI 移行 (Pattern A) | **未着手 (次)** | - | 引数なし ViewModel（Settings, Log, About 等）の DI 登録・移行 |
| Step 8 | ViewModel DI 移行 (Pattern B) | 未着手 | - | 共有依存 ViewModel の DI 登録・移行 |
| Step 9 | ViewModel DI 移行 (Pattern C) | 未着手 | - | 実行時引数付き ViewModel の Factory 移行 |
| **実機CP3** | **全ViewModel DI移行完了 実機検証** | 未着手 (計画) | - | 全画面 UI 結合・ViewModel 直接 new 全廃実機検証（Step9完了時） |
| Step 10 | Phase3 引継ぎ再確認・シム整理 | 未着手 | - | 残存シムの監査と全体健全性確認 |
| **実機CP4** | **Phase4 最終総合 E2E 実機検証** | 未着手 (計画) | - | 残存シム整理後・フェーズ4完了総合実機検証（Step10完了時） |

---

## 2. 詳細ステータス

### Step 1〜5: バックエンド各層 DI サービス分離 (完了)
- 第1層（入力監視層）: `IDeviceStateService`, `IDs4DeviceRegistry`
- 第3層（信号出力層）: `IOutputSlotService`, `IVirtualKBM`, `IElevatedProcessLauncher`, `IProcessInspector`
- 第4層 4-c（設定／状態サービス）: `IProfileSettingsService`, `IProfileRepository`, `ISpecialActionRepository`, `IPathService`, `IEnvironmentService`, `INotificationService`

### Step 6: Composition Root 一本化 & 実機検証CP2 (完了)
- **Composition Root (永続資産)**: `DS4Windows/DI/AppHost.cs` を唯一の DI エントリポイントとして一本化（`DS4WinWPF` / `DS4Windows` 両空間完全対応、`CreateHost` オーバーロード完備）。
- **サービス登録 (永続資産)**: `DS4Windows/DI/ServiceRegistration.cs` にて全 13 バックエンドサービスの登録を集約。
- **単体テスト**: `DS4WindowsTests/CompositionRootTests.cs`（全サービス一括解決・Singleton検証、全件通過）。
- **実機動作検証 (Checkpoint 2)**: `Phase4-Step6-RealDevice-Verification-Checklist.md` に基づき実機検証を実施・記録。一部○でない項目については、全 ViewModel DI 移行完了後に原因調査・対処を行う方針で合意。
