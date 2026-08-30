﻿# フェーズ3 進捗状況（Phase3-Status.md）

作成日: 2026-08-30
最終更新: 2026-08-30（Phase3-Step3-6 完了・ビルド／テストビルド／テスト実行 全て成功確認時点）
参照: docs-forDIMG/MadeByAgent/Phase3-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Step3-1to3-4-Completion-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Followup-StepF0-Member-Audit-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md, docs-forDIMG/MadeByAgent/Phase3-Step3-5-IElevatedProcessLauncher-Design.md, docs-forDIMG/MadeByAgent/Phase3-Step3-5-Completion-Report.md, docs-forDIMG/MadeByAgent/Phase3-Step3-6-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Step3-6-Completion-Report.md

## 1. ステップ別進捗

| ステップ | 内容 | 状況 | 完了日 | 備考 |
|---|---|---|---|---|
| Phase3-Step3-1 | IDs4DeviceRegistry インターフェース設計 | 完了 | 2026-08-30 | namespace実態がDS4Windows.Servicesに変更。ReEnableDevice追加。F-0で12件の全メンバー突合完了 |
| Phase3-Step3-2 | Ds4DeviceRegistryAdapter 実装 | 完了（ビルド・テスト確認済） | 2026-08-30 | Step 2-2と同一設計思想で実装 |
| Phase3-Step3-3 | DI登録（ServiceRegistration.cs） | 完了 | 2026-08-30 | ControlServiceのコンストラクタ変更はPhase3-StepFで適用完了 |
| Phase3-Step3-4 | IDeviceStateAccessor設計・ControlService実装・Mapping.cs 6504行目置換 | 完了（一部残課題あり） | 2026-08-30 | ApplyProfileDirect/RestoreProfileDirectの2箇所は意図的にスコープ外のまま |
| Phase3-StepF | フォローアップ: DI配線整理および昇格境界の整理 | 完了（ビルド・テスト全件確認済） | 2026-08-30 | F-0〜F-3完了。全37件の単体テスト100%通過確認済。詳細は`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md`。※Phase3-Step3-5着手時の実コード再確認でF-1の一部内容と実コードに食い違いが判明していたが、Phase3-Step3-6-Aにて解消済み（本Status §5参照） |
| Phase3-Step3-5 | Process.Start分類（権限昇格）の抽象化（IElevatedProcessLauncher） | 完了（ビルド／テストビルド／テスト実行 全て成功確認済） | 2026-08-30 | `ControlService.DS4Devices_RequestElevation` のProcess.Start部分のみを新経路（AppHost.GetService）優先＋フォールバックに変更。`IDs4DeviceRegistry.ReEnableDevice` には未着手。詳細は`Phase3-Step3-5-Completion-Report.md` |
| **Phase3-Step3-6** | **Process.Start分類（多重起動チェック）の抽象化（IProcessInspector）＋ IDeviceStateAccessor配線修正** | **完了（ビルド／テストビルド／テスト実行 全て成功確認済）** | **2026-08-30** | Phase3-Step3-6-A（配線修正）／Phase3-Step3-6-B（IProcessInspector）の2部構成で実施。詳細は`Phase3-Step3-6-Completion-Report.md` |

全体進捗: 7ステップ中7ステップ完了（ビルド・テスト実行確認済み）。**フェーズ3のステップ別実装は全完了**。

## 2. 既知の残課題（フェーズ3スコープ外として明示的に棚上げ中のもの）

| 残課題 | 発生元 | 解消予定 |
|---|---|---|
| Mapping.cs内 ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub参照2箇所（ctrl=ControlServiceそのものをGlobal.ApplyProfile等へ渡す必要があるため、IDeviceStateAccessorだけでは解消不可） | Phase3-Plan.md §0.1、§1.2 | フェーズ4（IProfileRepository導入時）に合わせて解消 |
| IDs4DeviceRegistryのReEnableDeviceとIElevatedProcessLauncher（Phase3-Step3-5完了）の役割重複整理 | Phase3-Step3-1実装時の追加 | Phase3-StepFで境界を文書確定済み（`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md` §2.4）。Phase3-Step3-5はProcess.Start部分のみを対象とし、ReEnableDeviceには触れずに完了 |
| DS4Windows自身の多重起動防止機構（IPC/Mutex）は未抽象化のまま | Phase3-Step3-6-Plan.md §0.1、§1.1 | `Process.GetProcesses()`を使わない別の仕組みのためフェーズ3の対象外。抽象化の要否はフェーズ4以降で改めて判断 |

## 3. 検証・確認事項（Phase3-Step3-6時点）

- [x] Phase3-Step3-1〜3-4、Phase3-StepF、Phase3-Step3-5、Phase3-Step3-6 分のビルド確認（コンパイルエラー 0 件）
- [x] IDs4DeviceRegistryがDS4Devicesの全public staticメンバー（12件）を過不足なく反映しているかのgrep突合（欠員なし確認済）
- [x] DS4Windows.Actions.Tests および StandaloneTests の自動テスト通過確認（Phase3-Step3-6時点で全件成功、ユーザー確認済）
- [ ] 実機でのデバイス接続/切断シナリオの回帰テスト
- [ ] 実機でのUAC昇格シナリオ（管理者権限なし起動→再有効化フロー）の回帰テスト（Phase3-Step3-5で新設した`IElevatedProcessLauncher`経路の実機確認）
- [ ] 実機でのラムブル動作確認（Phase3-Step3-6-Aで新設した`IDeviceStateAccessor`経路の実機確認）
- [ ] 実機での`LaunchProgram`多重起動チェック動作確認（Phase3-Step3-6-Bで新設した`IProcessInspector`経路の実機確認）

## 4. 次に着手すべきステップ

フェーズ3のステップ別実装（Phase3-Step3-1〜Phase3-Step3-6）は全て完了した。次は以下のいずれかを判断する必要がある。

1. フェーズ3の完了判定（`Phase3-Plan.md` §5の完了判定基準）を正式に確認し、フェーズ3を完了扱いとする。
2. §3に記載の実機確認事項（デバイス接続/切断、UAC昇格、ラムブル、LaunchProgram多重起動チェック）を実施する。
3. フェーズ4（Global分割＋ViewModel DI化）の計画策定に着手する。

いずれもユーザーの判断・指示を待って着手する。

## 5. Phase3-Step3-5着手時に判明した申し送り事項（解消済み）

Phase3-Step3-5着手前の実コード確認（`Phase3-Step3-5-IElevatedProcessLauncher-Design.md` §0.5）で、`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` のF-1完了報告と実コードに食い違いがあることが判明していた。

- 報告内容: 「`ServiceRegistration.cs` に `AddSingleton<IDeviceStateAccessor>` を追加」「`Mapping.cs` の解決口を `AppHost.GetService` に変更」
- 実コード（Phase3-Step3-5着手時点で確認）: `ServiceRegistration.cs` に `IDeviceStateAccessor` の登録行なし。`Mapping.cs` 6504行目付近は今も `ServiceProviderHolder.Provider` を参照しており、DI未登録のため常にフォールバック（`Program.rootHub`直接参照）のみが動作している状態。

**対応状況**: ユーザー指示によりPhase3-Step3-6-Aとして対応し、解消済み（詳細は`Phase3-Step3-6-Completion-Report.md` §4）。根本原因はDIコンテナが`ServiceProviderHolder`（旧・簡易版）と`AppHost`（正式ルート）の2つ並存しており、かつ`IDeviceStateAccessor`がどちらにも未登録だったこと。`ControlService`（`Program.rootHub`）を指すファクトリ委譲での登録、および`Mapping.cs`側の参照先修正により解消した。
