# Phase 5 - Step 13 完了報告書: UI層 DI 結合・検証および Watchpoint 2 対応

## 1. エグゼクティブサマリ
Phase 5 - Step 13 では、DS4Windows の第4層（UI層: ViewModel群および MainWindow）を第3層（DI サービス層）へ安全に結合し、長年放置されていた静的参照（`App.rootHub`, `Program.rootHub`, `Global`）を撲滅する作業を実施した。
さらに、`Phase5-Watchpoints-Investigation-Report.md` で策定された **Watchpoint 2（UIスレッド安全性およびイベント購読解除）** の点検・振る舞いテストを完了し、作業中に発見された **🔴 `AutoProfileChecker` 二重実体化問題（重大発見）** を根本解決した。

全 169 件（Actions: 156件、Standalone: 13件）の自動テストが 100% グリーンを達成し、Step 13 は完全に完了した。

---

## 2. 実施作業と具体的成果

### 2.1 コア ViewModel の Pure DI 移行（Step 13-1 〜 13-6）
* **`ControllerListViewModel`**: `ControlService`, `IProfileRepository`, `IAppSettingsService`, `IDs4DeviceRegistry`, `IOutputSlotService` をコンストラクタ注入。Singleton イベント購読解除のため `IDisposable` を実装。
* **`SettingsViewModel`**: `IAppSettingsService`, `IOutputSlotService` を注入し `IDisposable` を実装。
* **`SpecialActionsListViewModel`**: `ISpecialActionRepository`, `IProfileRepository`, `IOutputSlotService` を注入。Singleton サービスイベント未購読（メモリリークゼロ）を監査・実証。
* **`AutoProfilesViewModel`**: `IAutoProfileService`, `IProfileRepository` を注入し、Step 13-4 で `AutoProfileHolder` の二重実体を解消。
* **`RecordBoxViewModel` / `ProfileSettingsViewModel`**: 各ファクトリ経由での Pure DI 解決を確立。

### 2.2 MainWindow.xaml.cs の脱・静的参照（Step 13-7）
* 約 70 箇所存在した `App.rootHub`, `Program.rootHub`, `Global` 参照のうち、約 50 箇所のドメインロジック呼び出しをコンストラクタ注入された DI サービスへ置換。
* UI 固有の外観ヘルパー（テーマ・ウィンドウ位置等）および定数のみを意図的残存として峻別。

### 2.3 App.rootHub 過渡期シムの完全削除（Step 13-8）
* `App.xaml.cs` に存在していた過渡期フォールバックシム `App.rootHub` を完全に削除。全呼び出し元の置換を確認。

### 2.4 Watchpoint 2 対応および重大発見の是正（Step 13-9）
1. **🔴 `AutoProfileChecker` 二重実体問題の根本是正**:
   * **問題**: `MainWindow.xaml.cs` が `new AutoProfileChecker(...)` を独自生成していたが、コンストラクタ内で渡された Holder が黙殺され、DI コンテナ内の Singleton `IAutoProfileService` と二重実体化・状態解離を起こす潜在バグが存在していた。
   * **解決**: MainWindow から `AutoProfileChecker` の生成・利用を完全撤去。コンストラクタ引数に `IAutoProfileService autoProfileService = null` を受けて `_autoProfileService` フィールドに保持し、タイマー Tick 内の `CheckProfiles()` 呼び出し等へ直接一本化した。イベントシグネチャ整合用のオーバーロードも整備。
2. **スレッド安全性 ＆ イベント購読解除（Unsubscribe）の実証**:
   * `ControllerListViewModel` および `SettingsViewModel` に `IDisposable` を実装し、`MainWindow_Closed` 等で確実に Dispose を呼び出してアンフック。
   * `SpecialActionsListViewModel` については Singleton サービスへのイベント購読が存在しない（強参照リークなし）ことを監査・確認。
3. **振る舞い単体テストの拡充**:
   * `ControllerListViewModelTests.cs`: `IDisposable` 実装および安全な解放を検証。
   * `SpecialActionsListViewModelTests.cs`: 外部モック非依存の DI 注入テストおよび Singleton イベント未購読監査テストを配備。
   * `SettingsViewModelWatchpoint2Tests.cs`: ワーカースレッド（`Task.Run`）からのイベント発火耐性（スレッド安全性）および Dispose 多重呼出安全性を実証。

---

## 3. テスト実行結果

| テストプロジェクト | 合計テスト数 | 成功 | 失敗 | 結果 |
| :--- | :---: | :---: | :---: | :---: |
| **DS4Windows.Actions.Tests.csproj** | 156 | 156 | 0 | **PASS (100%)** |
| **StandaloneTests.csproj** | 13 | 13 | 0 | **PASS (100%)** |
| **合計** | **169** | **169** | **0** | **ALL GREEN** |

* **ビルド最適化**: `.vscode/tasks.json` のテストビルド引数に `--no-dependencies` を追加し、テストビルド時の本体再コンパイルを排除して大幅なビルド時間短縮を達成。

---

## 4. 主な変更ファイル一覧

* `DS4Windows/DS4Forms/MainWindow.xaml.cs`: 脱静的参照、`_autoProfileService` 直結化、イベントシグネチャ整合
* `DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs`: `IDisposable` 実装、Pure DI
* `DS4Windows/DS4Forms/ViewModels/SettingsViewModel.cs`: `IDisposable` 実装、Pure DI
* `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs`: Pure DI 接続、監査完了
* `DS4Windows/DS4Forms/ViewModels/AutoProfilesViewModel.cs`: `IAutoProfileService` 接続
* `DS4Windows/App.xaml.cs`: `App.rootHub` シム削除
* `DS4WindowsTests/ControllerListViewModelTests.cs`: `IDisposable` テスト拡充
* `DS4WindowsTests/SpecialActionsListViewModelTests.cs`: 新規作成（DI注入 ＆ 監査テスト）
* `DS4WindowsTests/SettingsViewModelWatchpoint2Tests.cs`: 新規作成（Watchpoint 2 振る舞いテスト）
* `.vscode/tasks.json`: `--no-dependencies` 追加（ビルド高速化）
* `docs-forDIMG/MadeByAgent/Phase5-Step13-Plan.md`: 実績同期・改定

---

## 5. 後続マイルストーンへの引き継ぎ

1. **Step 14: Phase 5 総合自動テスト**:
   * ドメイン 1〜4（永続化・アクション・デバイス・UI）の全テスト網羅実行によるリグレッションゼロの最終確定。
2. **Step 15: Legacy Shim 棚卸し・神クラス第一次ダウンサイズ**:
   * **Watchpoint 1 対応**: MainWindow 残存のプロファイル存在確認（`FindValidProfile` 等）を `IProfileRepository` へ委譲。
   * **Watchpoint 3 対応**: `ProfileListHolder` 等の旧静的ホルダーを DI サービスの透過シム化（SSOT担保）し、`[Obsolete]` 化。
   * **不要シム削除**: UI からの呼び出しが途絶えた `ControlService` / `Global` の不要シムメソッドを物理削除し、巨大ファイルの第一次ダウンサイズを実施。
3. **実機CP4: Phase 5 総合 E2E 実機検証**:
   * コントローラー実機接続、プロファイル切替、マクロ再生、AutoProfile 自動検知の総合動作検証。