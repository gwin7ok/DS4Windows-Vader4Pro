# Phase 1: SpecialAction 判定・実行の分離 - 進捗状況

**最終更新日**: 2026-08-27  
**ステータス**: 進行中 (C1, C2, C5 完了 / C3, C4 未着手)

---

## 1. 全体進捗概要

Phase 1 では、`Mapping.cs` に埋め込まれている `SpecialAction` の副作用直接実行ロジックを、Actions サブシステム（`IOutputAction` / DI 経由）へ分離・移行する作業を進めています。

| ステップ | 対象機能 / アクション | 状態 | 備考 |
| :--- | :--- | :---: | :--- |
| **A** | インベントリ作成 & テスト基盤整備 | **完了** | Direct-Callsites-Inventory.md, MockManagedActionManager 作成済 |
| **B** | `Mapping.cs` の DispatchTrigger 厳密化 | **完了** | `DispatchInputEdge` / `DispatchOrSetBeingTriggered` 実装、フォールバック維持 |
| **C1** | Key send 系 (`KeyOutputAction`) | **完了** | 既存実装利用・配線確認済 |
| **C2** | Mouse / Move 系 (`MouseOutputAction`) | **完了** | `MouseOutputAction.cs` 新設済 |
| **C3** | Macro 系 (`MacroAction` / `MacroController`) | **未着手** | `PlayMacro` / `PlayMacroTask` の非同期・並列性設計が必要 |
| **C4** | Profile 切替系 (`ProfileSwitchAction`) | **未着手** | `Global.ApplyProfile` の抽象化が必要 |
| **C5** | 外部プロセス起動 (`LaunchProcessAction`) | **完了** | 4引数対応、Adapter新設、`Mapping.cs`置換、単体テスト(T1〜T6)ビルド成功 |
| **D** | フォールバック削除と整流化 | **未着手** | 全出力アクション移行・動作確認後に実施 |
| **E** | Phase 1 完了レビュー & 文書化 | **未着手** | ロールアウト前最終チェック |

---

## 2. 直近の実施内容詳細 (C5: LaunchProcessAction 移行 & 単体テスト)

### 2.1 実装内容
1. **`IProcessLauncher` インターフェースの拡張**:
   * 引数付き・ウィンドウ非表示起動に対応するオーバーロードを追加。
     ```csharp
     void Launch(string fileName, string arguments, bool useShellExecute, bool hidden);
     ```
2. **`LaunchProcessAction` の全面改修**:
   * `$hidden` プレースホルダー除去と `hidden = true` フラグ化。
   * `.bat` / `.cmd` ファイルの `cmd.exe /c` ラップ起動対応。
   * `DS4Windows.DI.ServiceProviderHolder.Provider` からの DI 解決と、フォールバック処理の維持。
3. **`LaunchProcessActionAdapter` の新設**:
   * 旧 `specActionLaunchProc` からの移行アダプターを作成し、`ActionFactory` に配線。
4. **`Mapping.cs` のピンポイント置換**:
   * `specActionLaunchProc` 呼び出し箇所を `LaunchProcessActionAdapter` 経由に置換し、二重実行防止の `handled` フラグ捕捉を実装。

### 2.2 単体テスト実装 (`DS4WindowsTests`)
* **`MockProcessLauncher.cs`**:
   * 4引数オーバーロードに対応し、呼び出し履歴（`Calls`）および各種引数の記録プロパティを実装。
* **`LaunchProcessActionTests.cs` (新規追加)**:
   * **T1**: 単純な実行可能ファイル（`notepad.exe`）の通常起動検証
   * **T2**: 引数付き実行ファイルの引数分離検証
   * **T3**: `$hidden` 指定時の非表示フラグおよび文字列除去検証
   * **T4**: バッチファイル（`.bat`）の `cmd.exe` ラップ起動検証
   * **T5**: 不正・空パス指定時の安全性検証（例外非スロー）
   * **T6**: 複数回実行・履歴記録・リセット検証
   * **結果**: `DS4Windows.Actions.Tests.csproj` においてビルド成功を確認。

---

## 3. 次のステップ・残存タスク

1. **C3: Macro 系 (`MacroAction`) の設計と実装**
   * `Mapping.cs` 内の `PlayMacro` / `PlayMacroTask` の非同期実行・キーシーケンス制御を Actions サブシステムへ移行。
2. **C4: Profile 切替系 (`ProfileSwitchAction`) の設計と実装**
   * プロファイル切り替えロジック（`Global.ApplyProfile`）を抽象化し、アクション経由で安全にディスパッチする仕組みを構築。
3. **Step D: 移行フォールバックの段階的廃止**
   * 全アクションの安定稼働確認後、旧直接呼び出しロジックを削除。