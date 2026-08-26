# Phase 1: SpecialAction 判定・実行の分離 - 最新進捗状況 (2026-08-27 更新)

## 1. 全体進捗サマリー

| ステップ | 担当アクション / 項目 | 状態 | 完了日 | 備考 |
| :--- | :--- | :---: | :---: | :--- |
| **A** | Direct Callsites インベントリ作成 & テスト基盤 | **完了** | 2026-08-26 | `Direct-Callsites-Inventory.md`, `MockManagedActionManager.cs` 作成済 |
| **B** | `Mapping.cs` の DispatchTrigger 厳密化 | **完了** | 2026-08-26 | `Mapping.cs` の `DispatchInputEdge` / `DispatchOrSetBeingTriggered` を実装, フォールバック保持 |
| **C1** | Key send 系 (`KeyOutputAction`) | **完了** | 2026-08-26 | `KeyOutputAction.cs` 既存利用, 配線確認済 |
| **C2** | Mouse / Move 系 (`MouseOutputAction`) | **完了** | 2026-08-26 | `MouseOutputAction.cs` 新規作成 |
| **C3** | Macro 系 (`MacroAction` / `DefaultMacroPlayer`) | **進行中** | - | 設計書作成済, Step 1〜2完了 (`IMacroPlayer`, `DefaultMacroPlayer`, `Mapping.PlayMacroDirect` ビルド成功) |
| **C4** | Profile 切替 (`ProfileSwitchAction`) | **未着手** | - | `Global.ApplyProfile` 等の呼び出し集約 |
| **C5** | Launch program (`LaunchProcessAction`) | **完了** | 2026-08-27 | 4引数対応, Adapter新設, `Mapping.cs`置換, `MockProcessLauncher`改修, 単体テスト(T1〜T6)実装・ビルド成功 |
| **D** | フォールバック削除と整流化 | **未着手** | - | 全アクションの単体テスト・動作確認完了後に実施 |
| **E** | ドキュメントとロールアウト | **未着手** | - | 最終成果物の整理 |

---

## 2. 直近の完了作業詳細

### 2.1 C5: LaunchProcessAction の全面移行と単体テスト完了 (2026-08-27)
1. **`IProcessLauncher` インターフェースの拡張**:
   * 引数付き・ウィンドウ非表示起動に対応する 4 引数オーバーロードを追加。
     ```csharp
     void Launch(string fileName, string arguments, bool useShellExecute, bool hidden);
     ```
2. **`LaunchProcessAction` の全面改修**:
   * `$hidden` プレースホルダーのパース処理（文字列除去および `hidden = true` フラグ化）。
   * `.bat` / `.cmd` ファイルの `cmd.exe /c` 自動ラップ起動対応。
   * `DS4Windows.DI.ServiceProviderHolder.Provider` からの DI 解決とフォールバックの維持。
3. **`LaunchProcessActionAdapter` の新設**:
   * 旧 `specActionLaunchProc` からの移行アダプターを作成し、`ActionFactory` / `DefaultActionFactory` に配線。
4. **`Mapping.cs` (L5508-5569) のピンポイント置換**:
   * `specActionLaunchProc` 呼び出し箇所を `LaunchProcessActionAdapter` 経由に置換し、二重実行防止の `handled` フラグ捕捉を実装。
5. **単体テストの実装とビルド検証 (`DS4WindowsTests`)**:
   * `MockProcessLauncher.cs` を 4 引数版に対応修正。
   * `LaunchProcessActionTests.cs` (T1〜T6) を作成し、`ServiceProviderHolder` による DI 解決および各種フラグ処理のテストをパス。

### 2.2 C3: Macro 系アクション移行着手 (Step 1〜2 完了)
1. **設計書の作成**:
   * `docs-forDIMG/MadeByAgent/C3-MacroAction-Design.md` を作成。
2. **Step 1 (`IMacroPlayer.cs`)**:
   * マクロ再生・停止・状態管理の抽象インターフェース `DS4Windows/Actions/IMacroPlayer.cs` を新設。
3. **Step 2 (`DefaultMacroPlayer.cs` & `Mapping.cs`)**:
   * No Feature Drop 原則に基づき、`Mapping.cs` に委譲エントリーポイント `PlayMacroDirect` / `EndMacroDirect` を追加（L6447〜）。
   * `DS4Windows/Actions/DefaultMacroPlayer.cs` を実装し、既存のマクロ実行・キー解放ロジックを完全維持したまま DI 接続してビルド成功を確認。

---

## 3. 残存タスクと優先順位

1. **C3: Macro 系 (`MacroAction`) の移行完了 (最優先)**
   * Step 3: `MacroAction.cs` および `MacroActionAdapter.cs` の作成
   * Step 4: `ActionFactory.cs` / `DefaultActionFactory.cs` への `Macro` 型配線
   * Step 5: `Mapping.cs` のディスパッチ箇所の置換（`handled` 捕捉）
   * Step 6: `MockMacroPlayer` および `MacroActionTests` の実装・単体テスト検証
2. **C4: Profile 切替系 (`ProfileSwitchAction`) の設計と実装**
   * `Global.ApplyProfile` の直接呼び出しをアクション経由に移行。
3. **Step D: 移行用フォールバックの削除と整流化**
   * すべてのアクション（C1〜C5）のテスト通過後、旧直接呼び出しコードを整理。
4. **Step E: Phase 1 完了レビュー & 文書化**

---

## 4. 参照ドキュメント

* `docs-forDIMG/DI-App-Wide-Migration-Plan.md` (全体移行計画)
* `.github/copilot-instructions.md` (移行作業ガイドライン)
* `docs-forDIMG/MadeByAgent/Direct-Callsites-Inventory.md` (呼び出し箇所インベントリ)
* `docs-forDIMG/MadeByAgent/C5-LaunchProcessAction-Implementation.md` (C5 実装記録)
* `docs-forDIMG/MadeByAgent/C3-MacroAction-Design.md` (C3 設計書)