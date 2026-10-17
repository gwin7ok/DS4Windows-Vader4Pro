﻿# Step 3-5 完了報告書: IElevatedProcessLauncher（権限昇格の抽象化）

作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`（Step 3-5定義）
- `docs-forDIMG/MadeByAgent/Phase3-Step3-5-IElevatedProcessLauncher-Design.md`（本ステップの設計書）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`

## 1. 実施内容

設計書（`Phase3-Step3-5-IElevatedProcessLauncher-Design.md`）通りに、Step 3-5-1〜3-5-4を実施した。

| ステップ | 内容 | 結果 |
|---|---|---|
| 3-5-1 | `IElevatedProcessLauncher` インターフェース新設 | 完了。`DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs` |
| 3-5-2 | `DefaultElevatedProcessLauncher` 実装（既存ロジックの移設のみ） | 完了。`DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs` |
| 3-5-3 | DI登録 | 完了。`DS4Windows/DI/ServiceRegistration.cs` に1行追加 |
| 3-5-4 | `ControlService.DS4Devices_RequestElevation` のピンポイント置換 | 完了。新経路（`AppHost.GetService<IElevatedProcessLauncher>()`）優先＋フォールバック（既存直接`Process.Start`）の形に変更 |

## 2. 変更ファイル一覧

- 新規: `DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs`
- 新規: `DS4Windows/DS4Control/Services/DefaultElevatedProcessLauncher.cs`
- 変更: `DS4Windows/DI/ServiceRegistration.cs`（`IElevatedProcessLauncher` のSingleton登録を1行追加）
- 変更: `DS4Windows/DS4Control/ControlService.cs`（`DS4Devices_RequestElevation` メソッドをピンポイント置換）

`IDs4DeviceRegistry.ReEnableDevice`、`App.xaml.cs` の `parser.ReenableDevice` 枝（子プロセスエントリ）には設計書通り一切触れていない。

## 3. ビルド・テスト結果

ユーザーにより実施・確認済み。

- ビルド: 成功
- テストビルド: 成功
- テスト実行: 成功（全件）

## 4. 実装時の技術的な学び（作業ルールへの反映事項）

### 4.1 改行コード（CRLF/LF）差異によるピンポイント置換の不一致

Step 3-5-4の初回実行時、`ControlService.cs` に対する複数行ブロックのピンポイント置換が「対象文字列が見つからない」エラーで失敗した。原因は、GitHub API（`raw.githubusercontent.com`）経由で取得したコード片がLF改行であるのに対し、ローカルのWindowsチェックアウトはCRLF改行だったため、複数行にまたがる文字列の完全一致比較が不一致になったこと。
（`ServiceRegistration.cs` 側は挿入箇所が短く、たまたま一致したため問題が表面化しなかった）

**対応**: 比較・置換の直前に対象ファイル内容とold_str/new_strの両方を `\r\n` → `\n` に正規化し、置換後に元ファイルがCRLFだった場合のみ `\n` → `\r\n` に戻して書き戻す方式に修正し、再実行して解消した。

**今後の作業ルールへの反映**: 複数行にまたがるピンポイント置換を伴うPowerShellスクリプトでは、今後もこの正規化ロジック（比較前にLF統一、書き戻し前に元ファイルの改行コードへ復元）を標準的に組み込む。

## 5. §0.5で申し送りとした別件（本ステップでは対応していない）

設計書§0.5に記載した通り、`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md` の「F-1完了」報告と実コードの間に食い違いがあることを確認済みである（`ServiceRegistration.cs` に `IDeviceStateAccessor` の登録がなく、`Mapping.cs` も `ServiceProviderHolder.Provider`（未登録のため常に失敗）を参照したまま）。

本ステップの対象外のため変更していない。対応要否は別途判断が必要（Step 3-6着手前、または別セッションでの確認を推奨）。

## 6. 完了判定基準の充足状況

- [x] `IElevatedProcessLauncher` が新設され、`RelaunchElevated` のシグネチャが設計書通り
- [x] `DefaultElevatedProcessLauncher` が既存の `DS4Devices_RequestElevation` のロジックをそのまま移設したものである（ロジック変更なし）
- [x] `ServiceRegistration.cs` に `IElevatedProcessLauncher` のSingleton登録が追加されている
- [x] `ControlService.DS4Devices_RequestElevation` が新経路優先＋フォールバックの形になっており、両方が同時実行されることがない
- [x] `IDs4DeviceRegistry.ReEnableDevice` には一切触れていない
- [x] `App.xaml.cs` の `parser.ReenableDevice` 枝には一切触れていない
- [x] ビルドが通っている（DS4WinWPF, Actions.Tests, StandaloneTests）
- [x] `Phase3-Status.md` を更新し、§0.5の申し送り事項を記録した（本報告書と合わせて更新）

## 7. 未実施・今後の確認事項

- [ ] 実機でのUAC昇格シナリオ（管理者権限なし起動→再有効化フロー）の回帰テスト（Phase3-Plan §4のリスク表通り、実機確認が必要）
- [ ] §0.5の `IDeviceStateAccessor` 配線の食い違いへの対応要否の判断

## 8. 次のアクション

1. `Phase3-Status.md` を更新（3-5を完了扱いに変更、§0.5の申し送りを追記）。
2. Step 3-6（`IProcessInspector`、多重起動チェックの抽象化）の着手可否をユーザーに確認する。着手前に `ScpUtil.cs` 側の `Process.GetProcesses()` 呼び出し箇所の現状再調査が必要（Phase3-Plan §2 Step3-6の完了基準通り）。
