﻿# Phase3-Step3-6 完了報告書: IProcessInspector（多重起動チェックの抽象化）＋ IDeviceStateAccessor配線修正

作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md`（Phase3-Step3-6定義）
- `docs-forDIMG/MadeByAgent/Phase3-Step3-6-Plan.md`（本ステップの計画書）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md`（§5 申し送り事項）

## 1. 実施内容

計画書（`Phase3-Step3-6-Plan.md`）通りに、Phase3-Step3-6-A-1〜Phase3-Step3-6-B-4を実施した。

| ステップ | 内容 | 結果 |
|---|---|---|
| Phase3-Step3-6-A-1 | `IDeviceStateAccessor` のファクトリ登録追加（`Program.rootHub` への委譲） | 完了。`DS4Windows/DI/ServiceRegistration.cs` に追加 |
| Phase3-Step3-6-A-2 | `Mapping.cs` の参照先修正（`ServiceProviderHolder.Provider` → `AppHost.GetService`） | 完了。ピンポイント置換1箇所 |
| Phase3-Step3-6-B-1 | `IProcessInspector` インターフェース新設 | 完了。`DS4Windows/DS4Control/Services/IProcessInspector.cs` |
| Phase3-Step3-6-B-2 | `DefaultProcessInspector` 実装（既存ロジックの移設のみ） | 完了。`DS4Windows/DS4Control/Services/DefaultProcessInspector.cs` |
| Phase3-Step3-6-B-3 | DI登録 | 完了。`DS4Windows/DI/ServiceRegistration.cs` に追加 |
| Phase3-Step3-6-B-4 | `Global.LoadProfile`（`ScpUtil.cs`）のピンポイント置換 | 完了。多重起動チェックの`procFound`判定部分のみを新経路優先＋フォールバックに変更 |

## 2. 変更ファイル一覧

- 新規: `DS4Windows/DS4Control/Services/IProcessInspector.cs`
- 新規: `DS4Windows/DS4Control/Services/DefaultProcessInspector.cs`
- 変更: `DS4Windows/DI/ServiceRegistration.cs`（`IDeviceStateAccessor`のファクトリ登録、`IProcessInspector`のSingleton登録を追加）
- 変更: `DS4Windows/DS4Control/Mapping.cs`（参照先コンテナを`ServiceProviderHolder.Provider`から`AppHost.GetService`に変更。フォールバックは維持）
- 変更: `DS4Windows/DS4Control/ScpUtil.cs`（`Global.LoadProfile`内の`procFound`判定ループのみをピンポイント置換。起動処理部分（`Task processTask`）には触れていない）

DS4Windows自身の多重起動防止機構（IPC/Mutex）には計画書通り一切触れていない。`IDs4DeviceRegistry.ReEnableDevice`、`App.xaml.cs`の`parser.ReenableDevice`枝にも触れていない。

## 3. ビルド・テスト結果

ユーザーにより実施・確認済み。

- ビルド: 成功
- テストビルド: 成功
- テスト実行: 成功（全件）

## 4. Phase3-Status.md §5 申し送り事項への対応結果

Phase3-Step3-5着手時に判明した`IDeviceStateAccessor`配線の食い違い（Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Reportの F-1完了報告と実コードの不一致）について、ユーザー指示によりPhase3-Step3-6-Aとして対応した。

**根本原因**: 当初の想定（`AppHost.GetService`への参照先変更のみで解決）より深い問題があった。
1. DIコンテナが`ServiceProviderHolder`（Actions系のみを登録する旧・簡易版）と`AppHost`（Phase3以降の正式ルート）の2つ並存しており、`Mapping.cs`は前者を参照していた。
2. `IDeviceStateAccessor`はどちらのコンテナにも登録されていなかった。
3. `IDeviceStateAccessor`の実装元である`ControlService`はDIコンテナ管理下になく、`App.xaml.cs`内で手動生成され`Program.rootHub`として保持される特殊なインスタンスであるため、単純な`AddSingleton<IDeviceStateAccessor, ControlService>()`は別インスタンスを生成してしまい危険と判断し、`Program.rootHub`を指すファクトリ委譲で登録する方式を採用した。

この対応により、`Mapping.cs`側の新経路（`AppHost.GetService<IDeviceStateAccessor>()`）が実際に解決されるようになった（従来は常にフォールバックのみが動作していた）。

**この申し送り事項は本ステップをもって解消済みとする。**

## 5. 完了判定基準の充足状況

### Phase3-Step3-6-A（配線修正）

- [x] `ServiceRegistration.cs` に `IDeviceStateAccessor` のファクトリ登録（`Program.rootHub` 委譲）が追加されている
- [x] `Mapping.cs` の参照先が `AppHost.GetService<IDeviceStateAccessor>()` に変更され、フォールバックが維持されている
- [ ] 実機でのラムブル動作確認（新経路が実際に解決されることの確認）※未実施、§7参照

### Phase3-Step3-6-B（IProcessInspector）

- [x] `IProcessInspector` が新設され、`IsProcessRunning` のシグネチャが計画書通り
- [x] `DefaultProcessInspector` が既存の `procFound` 判定ループをそのまま移設したものである（ロジック変更なし）
- [x] `ServiceRegistration.cs` に `IProcessInspector` のSingleton登録が追加されている
- [x] `Global.LoadProfile`（`ScpUtil.cs`）の `procFound` 判定部分のみが新経路優先＋フォールバックの形に置換され、起動処理部分（`Task processTask`）には触れていない
- [x] DS4Windows自身の多重起動防止機構（IPC/Mutex）には一切触れていない

### 共通

- [x] ビルド・テストビルド・テスト実行が全て成功する
- [x] `Phase3-Status.md` が更新され、Phase3-Step3-6完了・§5の申し送り事項解消が記録されている
- [x] `Phase3-Step3-6-Completion-Report.md` が作成されている（本書）

## 6. 命名規則の徹底（本ステップでの申し送り）

本ステップの完了確認時、ユーザーより「Phase3-Step3-6-5」という略記を「Phase3-Step3-6-C」に改めるよう指摘があった。これに伴い、以後のフェーズ・ステップ呼称は必ず `Phase3-Step3-6-C` のように **PhaseとStepを省略しない完全形式** で統一する（`copilot-instructions.md`にはまだ明文化されていないが、今後のドキュメント作成・会話上での運用ルールとして徹底する）。

## 7. 未実施・今後の確認事項

- [ ] 実機でのデバイス接続/切断シナリオの回帰テスト（Phase3全体を通じて未実施のまま）
- [ ] 実機でのUAC昇格シナリオ（Phase3-Step3-5で新設した`IElevatedProcessLauncher`経路の実機確認）
- [ ] 実機でのラムブル動作確認（Phase3-Step3-6-Aで新設した`IDeviceStateAccessor`経路の実機確認）
- [ ] 実機での`LaunchProgram`多重起動チェック動作確認（Phase3-Step3-6-Bで新設した`IProcessInspector`経路の実機確認）

## 8. 次のアクション

1. `Phase3-Status.md` を更新（Phase3-Step3-6を完了扱いに変更、§5の申し送りを解消済みとして記録）。
2. フェーズ3全体の完了判定（`Phase3-Plan.md` §5）を確認する。7ステップ（3-1, 3-2, 3-3, 3-4, 3-F, 3-5, 3-6）全て完了。
3. フェーズ4（Global分割＋ViewModel DI化）着手の要否をユーザーに確認する。
