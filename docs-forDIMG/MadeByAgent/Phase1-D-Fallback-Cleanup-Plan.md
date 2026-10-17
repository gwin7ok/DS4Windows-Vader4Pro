# Step D: フォールバック整理 & DI登録整流化 計画書

**作成日**: 2026-08-27  
**ステータス**: 計画策定完了 / 着手待ち  
**対象領域**: Phase 1 の総仕上げ（DI サービス登録、ディスパッチ整流化、レガシーコード整理）

---

## 1. 概要と目的

Step C1〜C5 において、全 5 種類の出力アクション（Key, Mouse, Macro, Profile, Program）の DI 分離・単体テストがすべて完了しました。

本ステップ（Step D）では、これらの新設サービスをアプリケーション本体の DI コンテナ（`AppHost.cs`）へ正式に統合し、`Mapping.cs` 内に残存している旧直接呼び出しロジック（インライン `Process.Start` 等の重複コード）を安全に整理・整流化します。

---

## 2. 作業分割（4つのマイクロ・ステップ）

```
[ Step D-1: DI登録正式化 ]
        │
        ▼
[ Step D-2: Mapping.cs レガシーコード整理 ]
        │
        ▼
[ Step D-3: ソリューション統合ビルド & 全テスト回帰検証 ]
        │
        ▼
[ Step D-4: Phase 1 完了実績の記録 & Phase 2 引き継ぎ準備 ]
```

---

### Step D-1: DI コンテナ登録の正式化 (`AppHost.cs`)
* **目的**: アプリ起動時に各アクションサービスが自動的に DI コンテナへ登録され、`ServiceProviderHolder` 経由で解決可能にする。
* **対象ファイル**: `DS4Windows/AppHost.cs`（または DI 初期化設定ファイル）
* **登録サービス一覧**:
  1. `IProcessLauncher` -> `DefaultProcessLauncher` (Singleton)
  2. `IMacroPlayer` -> `DefaultMacroPlayer` (Singleton)
  3. `IProfileSwitcher` -> `DefaultProfileSwitcher` (Singleton)
  4. `IActionFactory` -> `DefaultActionFactory` (Singleton)

---

### Step D-2: `Mapping.cs` のレガシー直接呼び出しコード整理
* **目的**: `DispatchInputEdge` 経由で確実に `handled = true` になることを担保した上で、古い重複インラインコードを整理。
* **対象ファイル**: `DS4Windows/DS4Control/Mapping.cs`
* **作業内容**:
  * `SpecialAction.ActionTypeId.Program`: 古い 30 行以上のインライン `Process.Start` を削除し、`LaunchProcessAction` への一本化を確認。
  * `SpecialAction.ActionTypeId.Macro`: 重複した直接 `PlayMacro` 呼び出しの整理。
  * `SpecialAction.ActionTypeId.Profile`: 重複した直接 `ApplyProfile` 呼び出しの整理。
  * ※安全弁としての委譲メソッド（`PlayMacroDirect`, `ApplyProfileDirect` 等）は維持（No Feature Drop）。

---

### Step D-3: ソリューション統合ビルド & 全テスト回帰検証
* **目的**: 本体ビルドとテストプロジェクトの全テスト（T1〜Tn）が一括でグリーンになることを検証。
* **実行コマンド**:
  ```bash
  # 1. 本体リリースターゲットのビルド
  dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 -o ./DS4Windows/bin/x64/Release

  # 2. 全単体テストの一括実行
  dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj
  ```

---

### Step D-4: Phase 1 完了ドキュメント整備
* **目的**: Phase 1 の成果を確定し、Phase 2（KBM出力の抽象化: `IVirtualKBM`）への移行準備を完了する。
* **更新・作成ファイル**:
  * `docs-forDIMG/MadeByAgent/Phase1-Status_updated.md`（Step D 完了反映）
  * `docs-forDIMG/MadeByAgent/Phase1-Completion-Report.md`（Phase 1 完了報告書）

---

## 3. No Feature Drop (機能完全維持) チェックリスト

- [ ] アプリ起動時およびプロファイル切り替え時にすべての SpecialAction が正常にトリガーされること。
- [ ] ログレベル（`AppLogger.LogTrace`, `AppLogger.LogDebug`, `AppLogger.LogToGui`）が既存と同一に維持されていること。
- [ ] DI 未登録のスタンドアロン環境（テスト等）でも安全にフォールバックが動作すること。

---

## 4. 完了基準
1. `AppHost.cs` で全サービスがシングルトン登録されていること。
2. `Mapping.cs` 内の肥大化したインライン直接呼び出しコードが整理され、コードの可読性・保守性が向上していること。
3. `DS4Windows.Actions.Tests` の全テストが 100% 合格すること。