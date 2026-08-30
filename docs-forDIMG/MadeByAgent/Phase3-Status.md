﻿# フェーズ3 進捗状況（Phase3-Status.md）

作成日: 2026-08-30
最終更新: 2026-08-30（Step 3-5 完了・ビルド／テストビルド／テスト実行 全て成功確認時点）
参照: docs-forDIMG/MadeByAgent/Phase3-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Step3-1to3-4-Completion-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Followup-StepF0-Member-Audit-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md, docs-forDIMG/MadeByAgent/Phase3-Step3-5-IElevatedProcessLauncher-Design.md, docs-forDIMG/MadeByAgent/Phase3-Step3-5-Completion-Report.md

## 1. ステップ別進捗

| ステップ | 内容 | 状況 | 完了日 | 備考 |
|---|---|---|---|---|
| 3-1 | IDs4DeviceRegistry インターフェース設計 | 完了 | 2026-08-30 | namespace実態がDS4Windows.Servicesに変更。ReEnableDevice追加。F-0で12件の全メンバー突合完了 |
| 3-2 | Ds4DeviceRegistryAdapter 実装 | 完了（ビルド・テスト確認済） | 2026-08-30 | Step 2-2と同一設計思想で実装 |
| 3-3 | DI登録（ServiceRegistration.cs） | 完了 | 2026-08-30 | ControlServiceのコンストラクタ変更はStep 3-Fで適用完了 |
| 3-4 | IDeviceStateAccessor設計・ControlService実装・Mapping.cs 6504行目置換 | 完了（一部残課題あり） | 2026-08-30 | ApplyProfileDirect/RestoreProfileDirectの2箇所は意図的にスコープ外のまま |
| 3-F | フォローアップ: DI配線整理および昇格境界の整理（Phase3-StepF） | 完了（ビルド・テスト全件確認済） | 2026-08-30 | F-0〜F-3完了。IDeviceStateAccessor配線、ControlServiceコンストラクタ注入、全37件の単体テスト100%通過確認済。詳細は`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md`。※ただしStep 3-5着手時の実コード再確認でF-1の一部内容と実コードに食い違いが判明（本Status §5参照） |
| **3-5** | **Process.Start分類（権限昇格）の抽象化（IElevatedProcessLauncher）** | **完了（ビルド／テストビルド／テスト実行 全て成功確認済）** | **2026-08-30** | `ControlService.DS4Devices_RequestElevation` のProcess.Start部分のみを新経路（AppHost.GetService）優先＋フォールバックに変更。`IDs4DeviceRegistry.ReEnableDevice` には未着手。詳細は`Phase3-Step3-5-Completion-Report.md` |
| 3-6 | Process.Start分類（多重起動チェック）の抽象化（IProcessInspector） | 未着手（次期着手） | - | 着手前にScpUtil.cs側の現状再調査が必要 |

全体進捗: 7ステップ中6ステップ完了（ビルド・テスト実行確認済み）

## 2. 既知の残課題（フェーズ3スコープ外として明示的に棚上げ中のもの）

| 残課題 | 発生元 | 解消予定 |
|---|---|---|
| Mapping.cs内 ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub参照2箇所（ctrl=ControlServiceそのものをGlobal.ApplyProfile等へ渡す必要があるため、IDeviceStateAccessorだけでは解消不可） | Phase3-Plan.md §0.1、§1.2 | フェーズ4（IProfileRepository導入時）に合わせて解消 |
| IDs4DeviceRegistryのReEnableDeviceとIElevatedProcessLauncher（Step 3-5完了）の役割重複整理 | Step 3-1実装時の追加 | Step 3-Fで境界を文書確定済み（`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md` §2.4）。Step 3-5はProcess.Start部分のみを対象とし、ReEnableDeviceには触れずに完了 |

## 3. 検証・確認事項（Step 3-5時点）

- [x] Step 3-1〜3-4、Step 3-F、Step 3-5 分のビルド確認（コンパイルエラー 0 件）
- [x] IDs4DeviceRegistryがDS4Devicesの全public staticメンバー（12件）を過不足なく反映しているかのgrep突合（欠員なし確認済）
- [x] DS4Windows.Actions.Tests および StandaloneTests の自動テスト通過確認（Step 3-5時点で全件成功、ユーザー確認済）
- [ ] 実機でのデバイス接続/切断シナリオの回帰テスト
- [ ] 実機でのUAC昇格シナリオ（管理者権限なし起動→再有効化フロー）の回帰テスト（Step 3-5で新設した`IElevatedProcessLauncher`経路の実機確認）
- [ ] Mapping.cs側のDI解決失敗時フォールバック経路の実機動作確認

## 4. 次に着手すべきステップ

Step 3-6（`IProcessInspector`、多重起動チェックの抽象化）。
着手前に `ScpUtil.cs` 内の `Process.GetProcesses()` 呼び出し箇所を再調査し、Phase3-Plan.md記載の内容（L6694/L6717相当）が現在も正しいか確認すること（Phase3-Plan §2 Step3-6の完了基準通り）。

## 5. Step 3-5着手時に判明した申し送り事項（要判断）

Step 3-5着手前の実コード確認（`Phase3-Step3-5-IElevatedProcessLauncher-Design.md` §0.5）で、`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` のF-1完了報告と実コードに食い違いがあることが判明した。

- 報告内容: 「`ServiceRegistration.cs` に `AddSingleton<IDeviceStateAccessor>` を追加」「`Mapping.cs` の解決口を `AppHost.GetService` に変更」
- 実コード（Step 3-5着手時点で確認）: `ServiceRegistration.cs` に `IDeviceStateAccessor` の登録行なし。`Mapping.cs` 6504行目付近は今も `ServiceProviderHolder.Provider` を参照しており、DI未登録のため常にフォールバック（`Program.rootHub`直接参照）のみが動作している状態。

Step 3-5の対象外のため今回は変更していない。**対応要否（`ServiceRegistration.cs`への追加登録／`Mapping.cs`側の解決口修正）をユーザーに確認し、Step 3-6着手前または別途対応すること。**
