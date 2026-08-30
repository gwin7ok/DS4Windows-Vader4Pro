# フェーズ3 進捗状況（Phase3-Status.md）

作成日: 2026-08-30
最終更新: 2026-08-30（Step 3-F: F-0〜F-3完了時点）
参照: docs-forDIMG/MadeByAgent/Phase3-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Step3-1to3-4-Completion-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md, docs-forDIMG/MadeByAgent/Phase3-Followup-StepF0-Member-Audit-Report.md, docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md

## 1. ステップ別進捗

| ステップ | 内容 | 状況 | 完了日 | 備考 |
|---|---|---|---|---|
| 3-1 | IDs4DeviceRegistry インターフアース設計 | 完了（要フォローアップ） | 2026-08-30 | namespace実態がDS4Windows.Servicesに変更。ReEnableDevice追加。grepによる全メンバー再抽出は未実施 |
| 3-2 | Ds4DeviceRegistryAdapter 実装 | 完了（ビルド未確認） | 2026-08-30 | Step 2-2と同一設計思想で実装 |
| 3-3 | DI登録（ServiceRegistration.cs） | 完了 | 2026-08-30 | ControlServiceのコンストラクタ変更は意図的に見送り |
| 3-4 | IDeviceStateAccessor設計・ControlService実装・Mapping.cs 6504行目置換 | 完了（一部残課題あり） | 2026-08-30 | ApplyProfileDirect/RestoreProfileDirectの2箇所は意図的にスソープ外のまま |
| 3-F | フォローアップ: DI配線整理および昇格境界の整理（Phase3-StepF） | 完了（コード生成ベース、ビルド未確認） | 2026-08-30 | F-0（メンバー突合、欠員なし）／F-1（IDeviceStateAccessorをAppHost遅延ファクトリで登録、Mapping.cs解消口をAppHost.GetServiceに統一）／F-2（ControlServiceへIDs4DeviceRegistryをコンストラクタ注入、実行時DS4Devices.呼び出し14箇所をフィールド経由に置換）／F-3（本ファイルおよび完了報告書を更新）まで完了。詳細は`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` |
| 3-5 | Process.Start分類（権限昇格）の抽象化（IElevatedProcessLauncher） | 未着手 | - | ControlService.DS4Devices_RequestElevationのProcess.Start部分のみが対象。IDs4DeviceRegistry.ReEnableDeviceには触れない（境界はStep 3-Fで確定済み、本Statusの§2参照） |
| 3-6 | Process.Start分類（多重起動チェック）の抽象化（IProcessInspector） | 未着手 | - | 着手前にScpUtil.cs側の現状再調査が必要 |

全体進捗: 7ステップ中5ステップ完了（コード生成ベース）／うちビルド確認・実機確認済みは0ステップ

## 2. 既知の残課題（フェーズ3スソープ外として明示的に棚上げ中のもの）

| 残課題 | 発生元 | 解消予定 |
|---|---|---|
| Mapping.cs内 ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub参照2箇所（ctrl=ControlServiceそのものをGlobal.ApplyProfile等へ渡す必要があるため、IDeviceStateAccessorだけでは解消不可） | Phase3-Plan.md §0.1、§1.2 | フェーズ4（IProfileRepository導入時）に合わせて解消 |
| IDs4DeviceRegistryのReEnableDeviceとIElevatedProcessLauncher（Step 3-5予定）の轹割重複整理 | Step 3-1実装時の追加 | Step 3-F（Phase3-StepF）にて整理・対応 |

## 3. 未検証・未確認事項（要ユーソー対応）

- [ ] Step 3-1〜3-4分のビルド確認（コンパイルエラーの有無）
- [ ] IDs4DeviceRegistryがDS4Devicesの全public staticメンバーを過不足なく反映しているかのgrep再検証
- [ ] 実機でのデバイス接続/切断シナリオの回帰テスト
- [ ] Mapping.cs側のDI解消失敗時フォールバック経路の動作確認
- [ ] Step F-1〜F-2分のビルド確認（`IDeviceStateAccessor`ファクトリ登録・`ControlService`コンストラクタ変更を含む）
- [ ] 実機でのUAC昇格シナリオ（管理者権限なし起動→再有効化フロー）の回帰テスト

## 4. 次に着手すべきステップ

Step 3-F（Phase3-StepF：DI配線整理および昇格境界対応）。
`docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md` に基づき、DI配線の整合性確認、`IDs4DeviceRegistry.ReEnableDevice` と昇格処理の境界整理、およびビルド確認を実施した上で、Step 3-5（IElevatedProcessLauncher）へ進む。
