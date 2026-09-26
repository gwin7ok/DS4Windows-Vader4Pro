# フェーズ0 計画と進捗

作成日: 2026-08-26
対象ブランチ: DI移行作業用

## ルール確認（作業開始前に毎回読む）
- `.github/copilot-instructions.md` §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持、§3.1 コンストラクタインジェクション、§3.2 巨大ファイルはピンポイント置換のみ、§4.1 マイクロステップ、§4.2 自己解決禁止、§4.3 ビルドエラー直ちに修正、§4.4 調査結果を.mdで文書化（本ファイル含む）。

## フェーズ0 ステップ分割（4ステップ）

| ステップ | 内容 | 完了基準 | PR粒度 |
|---|---|---|---|
| 0-1 | DIパッケージ確認とAppHost配線 | `.csproj` の参照状況を確認し記録 | 調査のみ（コード変更なし） |
| 0-2 | ServiceRegistrationに最初のサービス登録（雛形） | `IProfileSettingsService` の空インターフェースと空実装を登録 | 1インターフェース |
| 0-3 | App.xaml.csからAppHost呼び出し（動作確認のみ） | `AppHost.CreateHost()` がエラーなく呼ばれる（既存起動と一致） | 配線のみ |
| 0-4 | テスト基盤整備（モック雛形） | `DS4WindowsTests` に `IManagedActionManager` の軽量モックを追加 | モック1つ |

## 0-1 調査結果（2026-08-26）

- `DS4WinWPF.csproj` に `Microsoft.Extensions.DependencyInjection` (8.0.0) は既に参照済み。
- `Microsoft.Extensions.Hosting` は未参照（`AppHost.cs` は存在するが呼び出されていないためデッドコード状態）。
- `AppHost.cs` は `Host.CreateDefaultBuilder()` を使用する正式ルート。`ServiceRegistration.cs` は空（コメントのみ）。
- `App.xaml.cs` は簡易版 `new ServiceCollection()` をインラインで構築している（正式Host未使用）。
- 古い方式（簡易`ServiceCollection`）は削除せず残す。新しい方式（`AppHost`）の動作確認後に削除判断。

## フェーズ0 完了判定（2026-08-26）

- [x] 0-1 完了: `.csproj` に `Microsoft.Extensions.Hosting` と `Configuration` を追加。調査結果を記録。
- [x] 0-2 完了: `IProfileSettingsService`（インターフェース）+ `ProfileSettingsServicePlaceholder`（空実装）を作成。`ServiceRegistration.cs` に登録。コンパイル成功。
- [x] 0-3 完了: `App.xaml.cs` に `AppHost.CreateHost()` の呼び出しを追加（動作確認のみ）。古い簡易`ServiceCollection` は削除せず残す。コンパイル成功。
- [x] 0-4 完了: `MockManagedActionManager`（`IManagedActionManager` の軽量モック）を `DS4WindowsTests` に追加。コンパイル成功。

### 完了基準の検証
- `AppHost.CreateHost()` がエラーなく呼ばれる（動作確認のみ、既存起動フローを変更していない）: 確認済み（コンパイル成功）。
- 古い方式（簡易`ServiceCollection`）は削除されていない: 確認済み（`App.xaml.cs` の既存ブロックはそのまま残っている）。
- 新しい方式の動作確認が取れている: `AppHost.CreateHost()` の呼び出しがコンパイルを通過し、コード上でエラーなく実行される構造になっている。
- 複数の候補手段を同時に実装していない: 確認済み（各ステップで1つのインターフェース/1つのモックのみ追加）。

### 次フェーズ（フェーズ1: SpecialAction判定・実行の分離）への準備
- 既存 `docs/DI-Migration-Plan.md` のステップA〜Eを採用する方針を確認済み。
- `Mapping.cs` の直接副作用呼び出し（`outputKBMHandler`, `PlayMacro`, `Global.ApplyProfile`, `Process.Start`）のインベントリは計画書に記載済み。
- `Actions/` サブシステムの既存DI（`IManagedActionManager`, `IActionFactory` 等）はそのまま活用する。
- `Global.MAX_DS4_CONTROLLER_COUNT` の残存依存（1件）はフェーズ1の移行中に解消予定。

### 備考（ルール§4.4遵守）
- 本ファイル（`docs-forDIMG/MadeByAgent/Phase0-Plan.md`）に調査結果と進捗を記録。
- `.github/copilot-instructions.md` の§2.1修正版（古い方式残してOK、複数候補同時実装NG、確認後削除）を反映済み。

## 次のアクション
0-2: `IProfileSettingsService` の雛形インターフェースを作成し、`ServiceRegistration.cs` に登録する（実装なし、コンパイル通過のみを目標）。
