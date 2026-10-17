﻿# フェーズ3 自動テスト追加報告書（Phase3-Automated-Test-Coverage-Report.md）

正式名称: Phase3-Step4-Automated-Test-Coverage-Report.md
作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase3-Plan.md` §5（完了判定基準）
- `docs-forDIMG/MadeByAgent/Phase3-Status.md` §3
- `docs-forDIMG/MadeByAgent/Phase3-Step5-RealDevice-Verification-Checklist.md`（本報告書と対をなす実機確認リスト）

## 1. 目的

`Phase3-Plan.md` §5 項目7「実機でのデバイス接続/切断・権限昇格シナリオの動作確認が記録されている」への対応にあたり、

- **自動テストで検証可能な範囲**はテストコードとして実装し、CI／`dotnet test`で継続的に検証できるようにする
- **実機（実コントローラ・UAC・実際の外部プログラム）がなければ検証できない範囲**は`Phase3-Step5-RealDevice-Verification-Checklist.md`にチェックリストとして切り出す

という方針で切り分けを行った。本書は前者（自動テストで担保した範囲）の内容と、その判断根拠を記録する。

## 2. 追加した自動テスト

### 2.1 `ProcessInspectorTests.cs`（`DS4WindowsTests`、xUnit）

`DefaultProcessInspector.IsProcessRunning` の単体テスト。実行中の自プロセス（テストランナー自身）を「既知の起動中プロセス」として利用することで、UACや実機を必要とせずに検証する。

| テストケース | 検証内容 |
|---|---|
| `IsProcessRunning_CurrentProcess_ReturnsTrue` | 自プロセスの実行ファイルパスを渡すと `true` を返す |
| `IsProcessRunning_NonExistentPath_ReturnsFalse` | 実在しないパスを渡すと `false` を返す |
| `IsProcessRunning_EmptyPath_ReturnsFalse` | 空文字列を渡すと `false` を返す（例外を投げない） |

**移設元ロジックとの整合性**: `DefaultProcessInspector`は`Global.LoadProfile`（`ScpUtil.cs`）にあった既存の`procFound`判定ループをそのまま移設したものであり、このテストは移設後もロジックが変わっていないことの回帰検証を兼ねる。

### 2.2 `Phase3ServiceRegistrationTests.cs`（`DS4WindowsTests`、xUnit）

Phase3-Step3-5／Phase3-Step3-6でAppHost（正式DIルート）に追加登録したサービスが、実際に解決可能であることを検証する**DI配線の回帰テスト**。

| テストケース | 検証内容 |
|---|---|
| `AppHost_ResolvesIElevatedProcessLauncher_AsDefaultImplementation` | `AppHost.GetService<IElevatedProcessLauncher>()` が `DefaultElevatedProcessLauncher` を返す |
| `AppHost_ResolvesIProcessInspector_AsDefaultImplementation` | `AppHost.GetService<IProcessInspector>()` が `DefaultProcessInspector` を返す |
| `AppHost_ResolvesIDs4DeviceRegistry_AsAdapterImplementation` | `AppHost.GetService<IDs4DeviceRegistry>()` が `Ds4DeviceRegistryAdapter` を返す |
| `AppHost_ResolvesIDeviceStateAccessor_WithoutThrowing_WhenRootHubNotSet` | `Program.rootHub`が未設定の状態で`AppHost.GetService<IDeviceStateAccessor>()`を呼んでも例外を投げず、`null`を返す |

**このテストを追加した理由（最重要）**: Phase3-Step3-5着手前の実コード確認で、`Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Report.md`が「`IDeviceStateAccessor`のDI登録・配線を完了した」と報告していたにもかかわらず、実際には`ServiceRegistration.cs`に登録されておらず、常にフォールバック経路のみが動作していたという不具合が発覚した（詳細: `Phase3-Step3-5-IElevatedProcessLauncher-Design.md` §0.5、`Phase3-Step3-6-Completion-Report.md` §4）。

この種の不具合（「登録したつもりが実際には登録されておらず、ビルドは通るため誰も気づかない」）は、通常の単体テスト（ロジック単体のテスト）では検出できない。**DIコンテナから実際に解決できるかを機械的に検証するテストを用意することで、今後同種の不具合が再発してもビルド・テスト実行の段階で検知できるようにした。**

## 3. 自動テストで担保しなかった範囲とその理由

以下は検討した上で自動テスト化を見送った範囲である。理由とともに記録する。

| 対象 | 見送った理由 |
|---|---|
| `DefaultElevatedProcessLauncher.RelaunchElevated` の実行結果（実際の`runas`昇格・子プロセス起動） | 実行すると本物のUACダイアログが表示され、CI／自動テスト環境では停止してしまう。ユーザー操作が必須のため自動化不可能 |
| `ControlService.DS4Devices_RequestElevation` 内の `handled` 分岐ロジック（新経路優先→フォールバックの切り替え） | `AppHost.GetService<IElevatedProcessLauncher>()` を直接呼び出す実装であり、`LaunchProcessAction`（Phase1 C5）のようなコンストラクタ注入によるモック差し替えの余地がない。テスト可能にするにはコンストラクタ変更等の追加リファクタリングが必要だが、Phase3-Step3-5設計書§2で「Step 3-Fで`IDs4DeviceRegistry`を注入したばかりであり、同じPRで立て続けにコンストラクタを変更するのはリスクが高い」との判断でコンストラクタ注入を見送った経緯があり、本ステップでも同じ判断を踏襲する |
| `Global.LoadProfile`（`ScpUtil.cs`）内の `handled` 分岐ロジック（同上） | 同上（469メンバーの`Global`クラス内の巨大メソッドであり、テスト容易性を上げるための構造変更は`copilot-instructions.md` §3.2「巨大ファイルの編集方針」の範囲を超える） |
| `IDeviceStateAccessor.GetController` が実際に正しい`DS4Device`を返すか | `Program.rootHub`が実際のコントローラ接続情報を保持していることが前提であり、実機接続なしには意味のある検証ができない |
| `IDs4DeviceRegistry`経由の実HID通信（接続・切断検知） | 実際のUSB/Bluetooth通信に依存 |
| `LaunchProgram`のエンドツーエンド動作（プロファイル設定→実際の外部アプリ起動制御） | 実在する外部プログラムパスとプロファイル設定を用いた統合的な動作確認が必要 |

これらは `Phase3-Step5-RealDevice-Verification-Checklist.md` に実機確認項目として記載した。

**今後の課題（フェーズ4以降で検討）**: `ControlService.DS4Devices_RequestElevation`および`Global.LoadProfile`の`handled`分岐ロジック自体をテスト可能にするには、`LaunchProcessAction`と同様のコンストラクタ注入パターン（`IElevatedProcessLauncher`/`IProcessInspector`をオプション引数として受け取れるようにする）へのリファクタリングが有効と考えられる。ただし`ControlService`のコンストラクタ変更は影響範囲が大きく、`Global`クラスの構造変更は§3.2の制約に触れるため、フェーズ4（`Global`分割）と合わせて検討することを推奨する。

## 4. ファイル一覧

- 新規: `DS4WindowsTests/ProcessInspectorTests.cs`
- 新規: `DS4WindowsTests/Phase3ServiceRegistrationTests.cs`
- `docs-forDIMG/MadeByAgent/Phase3-Step5-RealDevice-Verification-Checklist.md`（対をなす実機確認リスト）

## 5. 次のアクション

1. `dotnet test` でビルド・テスト実行し、全件成功することを確認した。
2. `Phase3-Step5-RealDevice-Verification-Checklist.md` に基づく実機確認結果を記録した。
3. 実機確認で `×` または未実施となった項目は、DI 化完了後に対応する未対応事項として引き継ぐ。

## 6. Step 4 完了結果

自動テストは全件成功した。これにより、Step 4 の自動テスト実装・実行および DI 配線の回帰確認を完了とする。
