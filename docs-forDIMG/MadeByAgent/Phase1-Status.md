# Phase 1: SpecialAction 判定・実行の分離 - 最新進捗状況 (2026-08-27 更新)

## 1. 全体進捗サマリー

| ステップ | 担当アクション / 項目 | 状態 | 完了日 | 備考 |
| :--- | :--- | :---: | :---: | :--- |
| **A** | Direct Callsites インベントリ作成 & テスト基盤 | **完了** | 2026-08-26 | `Direct-Callsites-Inventory.md`, `MockManagedActionManager.cs` 作成済 |
| **B** | `Mapping.cs` の DispatchTrigger 厳密化 | **完了** | 2026-08-26 | `Mapping.cs` の `DispatchInputEdge` / `DispatchOrSetBeingTriggered` を実装, フォールバック保持 |
| **C1** | Key send 系 (`KeyOutputAction`) | **完了** | 2026-08-26 | `KeyOutputAction.cs` 既存利用, 配線確認済 |
| **C2** | Mouse / Move 系 (`MouseOutputAction`) | **完了** | 2026-08-26 | `MouseOutputAction.cs` 新規作成 |
| **C3** | Macro 系 (`MacroAction` / `DefaultMacroPlayer`) | **完了** | 2026-08-27 | `IMacroPlayer`, `DefaultMacroPlayer`, `MacroAction`, `MacroActionAdapter`, `ActionFactory` 配線, `Mapping.cs` 置換, 単体テスト(T1〜T5)通過 |
| **C4** | Profile 切替 (`ProfileSwitchAction`) | **完了** | 2026-08-27 | `IProfileSwitcher`, `DefaultProfileSwitcher`, `ProfileSwitchAction`, `ProfileSwitchActionAdapter`, `ActionFactory` 配線, `Mapping.cs` 置換, 単体テスト(T1〜T5)通過 |
| **C5** | Launch program (`LaunchProcessAction`) | **完了** | 2026-08-27 | 4引数対応, Adapter新設, `Mapping.cs`置換, `MockProcessLauncher`改修, 単体テスト(T1〜T6)実装・ビルド成功 |
| **D** | フォールバック削除と整流化 | **完了** | 2026-08-27 | `AppHost.cs` 正式登録, `Mapping.cs` レガシーコード整理, 全テスト回帰検証完了 |
| **E** | ドキュメントとロールアウト | **完了** | 2026-08-27 | Phase 1 全成果物の整理・完了報告書作成 |

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

### 2.2 C3: Macro 系アクションの全面移行と単体テスト完了 (2026-08-27)
1. **`IMacroPlayer` インターフェースの新設**:
   * マクロ再生・停止・状態管理を抽象化する `IMacroPlayer.cs` を作成。
2. **`DefaultMacroPlayer` の実装 & `Mapping.cs` 委譲エントリーポイント追加**:
   * `Mapping.cs` に `PlayMacroDirect` / `EndMacroDirect` を新設し、800行を超える実績あるマクロ実行・キー解放ロジックを完全維持（No Feature Drop）。
3. **`MacroAction` & `MacroActionAdapter` の新設**:
   * `IOutputAction` および `Action` 基底クラスを実装し、`ServiceProviderHolder` による DI 解決とフォールバックを両立。
4. **`ActionFactory` / `DefaultActionFactory` への配線**:
   * `SpecialAction.ActionTypeId.Macro` 判定時に `MacroActionAdapter` を生成するように配線。
5. **`Mapping.cs` のピンポイント置換**:
   * `MapCustomAction` 内の `SpecialAction.ActionTypeId.Macro` 処理を `DispatchInputEdge` 経由に置換し、二重実行防止の `handled` フラグ捕捉を実装。
6. **単体テストの実装とパス (`DS4WindowsTests`)**:
   * `MockMacroPlayer.cs` および `MacroActionTests.cs` (T1〜T5) を作成し、テストプロジェクトのビルドおよび全テストパスを確認。

### 2.3 C4: Profile 切替系アクションの全面移行と単体テスト完了 (2026-08-27)
1. **`IProfileSwitcher` インターフェースの新設**:
   * プロファイル切り替えおよび一時プロファイルからの復帰を抽象化する `IProfileSwitcher.cs` を作成。
2. **`DefaultProfileSwitcher` の実装 & `Mapping.cs` 委譲エントリーポイント追加**:
   * `Mapping.cs` に `ApplyProfileDirect` / `RestoreProfileDirect` を新設し、`HaltReportingRunAction` やトースト通知等の既存ロジックを完全維持（No Feature Drop）。
3. **`ProfileSwitchAction` & `ProfileSwitchActionAdapter` の新設**:
   * `IOutputAction` および `Action` 基底クラスを実装し、`ServiceProviderHolder` による DI 解決とフォールバックを両立。
4. **`ActionFactory` / `DefaultActionFactory` への配線**:
   * `SpecialAction.ActionTypeId.Profile` 判定時に `ProfileSwitchActionAdapter` を生成するように配線。
5. **`Mapping.cs` のピンポイント置換**:
   * `MapCustomAction` 内の `SpecialAction.ActionTypeId.Profile` 処理を `DispatchInputEdge` 経由に置換し、二重実行防止の `handled` フラグ捕捉を実装。
6. **単体テストの実装とパス (`DS4WindowsTests`)**:
   * `MockProfileSwitcher.cs` および `ProfileSwitchActionTests.cs` (T1〜T5) を作成し、テストプロジェクトのビルドおよび全テストパスを確認。

### 2.4 Step D: フォールバック整理 & DI登録整流化 (2026-08-27)
1. **`DefaultProcessLauncher.cs` の新規作成**:
   * `IProcessLauncher` インターフェースに対する本体用具象クラスを実装。
2. **`AppHost.cs` の DI 正式登録**:
   * `IConfigurationRoot` オーバーロードに対応し、全 4 サービス（`DefaultProcessLauncher`, `DefaultMacroPlayer`, `DefaultProfileSwitcher`, `DefaultActionFactory`）をシングルトン登録。
3. **`Mapping.cs` レガシーコード整理**:
   * `SpecialAction.ActionTypeId.Program` 内の約 40 行に及ぶインライン重複 `Process.Start` コードを `LaunchProcessAction` に一本化。
4. **ソリューション全体の回帰検証**:
   * `DS4Windows.Actions.Tests.csproj` の全単体テスト（16件）が一括成功することを確認。

---

## 3. 残存タスクと優先順位

1. **Phase 1 完了（全タスク完了）**
2. **次期フェーズ着手**: **Phase 2: KBM出力の抽象化 (`IVirtualKBM`)**
   * `Mapping.cs` やアクション群が直接行っている `InputMethods` / Win32 `SendInput` を `IVirtualKBM` インターフェースで抽象化し、仮想 KBM のテスタビリティを確保。

---

## 4. 参照ドキュメント

* `docs-forDIMG/DI-App-Wide-Migration-Plan.md` (全体移行計画)
* `.github/copilot-instructions.md` (移行作業ガイドライン)
* `docs-forDIMG/MadeByAgent/Direct-Callsites-Inventory.md` (呼び出し箇所インベントリ)
* `docs-forDIMG/MadeByAgent/Phase1-C5-LaunchProcessAction-Design.md` (C5 設計書)
* `docs-forDIMG/MadeByAgent/Phase1-C5-LaunchProcessAction-Implementation.md` (C5 実装記録)
* `docs-forDIMG/MadeByAgent/Phase1-C5-LaunchProcessAction-Tests.md` (C5 テスト仕様書)
* `docs-forDIMG/MadeByAgent/Phase1-C3-MacroAction-Design.md` (C3 設計書)
* `docs-forDIMG/MadeByAgent/Phase1-C3-MacroAction-Implementation.md` (C3 実装記録)
* `docs-forDIMG/MadeByAgent/Phase1-C4-ProfileSwitchAction-Design.md` (C4 設計書)
* `docs-forDIMG/MadeByAgent/Phase1-C4-ProfileSwitchAction-Implementation.md` (C4 実装記録)
* `docs-forDIMG/MadeByAgent/Phase1-D-Fallback-Cleanup-Plan.md` (Step D 計画書)
* `docs-forDIMG/MadeByAgent/Phase1-D-Fallback-Cleanup-Implementation.md` (Step D 実装記録)
* `docs-forDIMG/MadeByAgent/Phase1-Completion-Report.md` (Phase 1 総合完了報告書)