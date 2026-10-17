# C5 LaunchProcessAction — 動作テストリスト

作成: Agent（DI移行作業用ブランチ）
参照元: docs/DI-Migration-Plan.md (C5), docs-forDIMG/MadeByAgent/LaunchProcessAction-Design.md, .github/copilot-instructions.md §2.1修正版

## 制約（§2.1修正版再確認）
- 古い方式（直接 `Process.Start` 呼び出し）は削除せず残す（新しい `LaunchProcessAction` の動作確認が取れるまで）。
- 新しい機能に複数の候補手段を同時に実装しない（`LaunchProcessAction` 経由の単一路線を目指すが、フォールバックを残す）。
- ログ出力（`AppLogger.LogTrace` / `LogDebug` 等）は維持（削除・新設しない）。
- `Global.cs` の静的メンバは薄いデリゲート（シム）として残す（75ファイルの呼び出し元を一度に壊さない）。

---

## テスト項目（C5 実装の検証）

### 単体テスト（モックベース、`DS4WindowsTests` で実施可能）

| # | テスト名 | 対象 | 検証内容 | 完了基準 |
|---|---|---|---|---|
| T1 | `LaunchProcessAction` コンストラクタ | `LaunchProcessAction` | `SpecialAction` を受け取り `Id` が正しく設定される | `Id == sa?.name ?? "LaunchProcess"` |
| T2 | `Execute` — DI経由（`IProcessLauncher` あり） | `LaunchProcessAction` | `ServiceProviderHolder.Provider` から `IProcessLauncher` を解決し `Launch()` を呼ぶ | `IProcessLauncher.Launch` が呼ばれた（モックで確認）、`Process.Start` は呼ばれない |
| T3 | `Execute` — フォールバック（`IProcessLauncher` なし） | `LaunchProcessAction` | `IProcessLauncher` が解決できない場合、直接 `Process.Start` が呼ばれる（フォールバック維持） | `Process.Start` が呼ばれた（モックで確認）、ログに `fallback` が記録される |
| T4 | `Execute` — `SpecialAction` が `null` | `LaunchProcessAction` | `sa == null` の場合、例外を投げずに早期リターン | 例外なし、ログなし（または `LogTrace` が維持されている） |
| T5 | `Stop` — ログ維持 | `LaunchProcessAction` | `Stop` が呼ばれても例外を投げず、`AppLogger.LogTrace` が維持される | ログ出力が削除されていない（コードレビューで確認） |
| T6 | `IProcessLauncher` インターフェース定義 | `IProcessLauncher` | `Launch(string filePath)` メソッドが存在し、コンパイルを通過する | `dotnet build` 通過 |

### 統合テスト（`Mapping.cs` との接続、手動または自動で限定的に実施）

| # | テスト名 | 対象 | 検証内容 | 完了基準 |
|---|---|---|---|---|
| T7 | `Mapping.cs` の `specActionLaunchProc` 呼び出し経路 | `Mapping.cs` + `LaunchProcessAction` | `SpecialAction` の `typeID` が `LaunchProcess` 相当の場合、`LaunchProcessAction` が `ActionManager` 経由で呼ばれる（または直接呼ばれる経路が存在する） | `LaunchProcessAction.Execute` が呼ばれる（ログで確認） |
| T8 | `LaunchProcessAction` のログ出力維持 | `LaunchProcessAction` | `AppLogger.LogTrace` が `Execute` / `Stop` の両方で維持されている（削除されていない） | `grep` で `AppLogger.LogTrace` が `LaunchProcessAction.cs` に存在する |
| T9 | フォールバック経路の存在確認 | `LaunchProcessAction` | `Process.Start` の直接呼び出しがコード内に残っている（フォールバックとして削除されていない） | `grep` で `Process.Start` が `LaunchProcessAction.cs` に存在する |

### 回帰テスト（実機で手動確認、限定的に実施）

| # | テスト名 | 対象 | 検証内容 | 完了基準 |
|---|---|---|---|---|
| T10 | 既存プロファイルの `specActionLaunchProc` 動作 | 実機 | `specActionLaunchProc` を含むプロファイルで、指定された外部プログラムが起動される（新経路またはフォールバック経由） | プログラムが起動される（手動確認） |
| T11 | ログファイルの確認 | 実機 | `AppLogger.LogTrace` の出力がログファイル（`ds4windows_log_*.txt` 等）に記録されている | ログに `LaunchProcessAction` のエントリが存在する |

---

## 検証手順（推奨順序）

1. **コードレビュー（自動）**: `LaunchProcessAction.cs` と `IProcessLauncher.cs` の内容を確認（§2.1修正版の原則を満たしているか）。
2. **コンパイル確認（自動）**: `dotnet build` が通過する（既に確認済み）。
3. **単体テスト（自動）**: T1〜T6 を `DS4WindowsTests` に追加し実行（モックベース）。
4. **統合テスト（自動/手動）**: T7〜T9 を実施（`Mapping.cs` の呼び出し経路を確認）。
5. **回帰テスト（手動）**: T10〜T11 を実機で限定的に確認（`specActionLaunchProc` を含むプロファイルを使用）。

---

## 完了判定基準（C5 の完了条件）

- [ ] `LaunchProcessAction.cs` が `IOutputAction` を実装し、コンパイルを通過する（確認済み）。
- [ ] `IProcessLauncher.cs` が定義され、コンパイルを通過する（確認済み）。
- [ ] `LaunchProcessAction` が `SpecialAction` を受け取り、`Execute` で `IProcessLauncher.Launch` を優先的に呼び、フォールバックとして `Process.Start` を残している（コードレビューで確認）。
- [ ] `AppLogger.LogTrace` が `LaunchProcessAction` 内で維持されている（削除されていない、コードレビューで確認）。
- [ ] `Mapping.cs` の `specActionLaunchProc` 相当の呼び出し経路が存在する（または別途確認されている）。
- [ ] 単体テスト T1〜T6 が `DS4WindowsTests` で実行可能（モックが存在する、または追加されている）。

---

## 備考（§2.1修正版と §3.1 の遵守状況）

- **§2.1修正版（フォールバック維持）**: `LaunchProcessAction` は `IProcessLauncher` 経由を優先し、解決できない場合に `Process.Start` を直接呼ぶフォールバックを残している（削除されていない）。
- **§2.1修正版（複数候補同時実装NG）**: `LaunchProcessAction` は単一路線（`IProcessLauncher` → `Process.Start` フォールバック）のみを持つ。同時に別の実装経路（例: `ProfileSwitchAction` のような別クラスでの起動）は存在しない。
- **§2.2（機能100%維持）**: `specActionLaunchProc` の機能（外部プログラム起動）は削除されていない（`LaunchProcessAction` がその役割を引き継ぐ）。
- **§2.3（ログ維持）**: `AppLogger.LogTrace` は `LaunchProcessAction` の `Execute` / `Stop` の両方で維持されている（削除されていない）。
- **§3.1（コンストラクタインジェクション）**: `LaunchProcessAction` はコンストラクタで `SpecialAction` を受け取る（コンストラクタインジェクションの原則に準拠）。`IProcessLauncher` は `ServiceProviderHolder.Provider` 経由で解決される（将来的にコンストラクタインジェクションに移行可能）。
- **§3.2（巨大ファイルのピンポイント置換）**: `Mapping.cs` の `specActionLaunchProc` 呼び出し箇所はまだ直接置換されていない（C5-2 のステップとして別途実施予定）。本設計文書ではその置換方針を明記している。
- **§4.1（マイクロステップ）**: C5 は `LaunchProcessAction` の作成（C5-1）と `Mapping.cs` の置換（C5-2）を別ステップに分割している（1機能 = 1ステップ）。
- **§4.4（調査結果の文書化）**: 本ファイル（`LaunchProcessAction-Design.md`）に設計案とテストリストを記録している。
