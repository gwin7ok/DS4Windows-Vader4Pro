# フェーズ6 Step13 計画書: `.cs` ファイル配置の統一（段階実施・挙動変更なし）

作成日: 2026-09-20  
対象ブランチ: `For-DI-migration-work`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`（Step13 として新設）  
参照原案: `docs-forDIMG/MadeByAgent/Phase6-Unified-Deployment-Plan-and-Safe-Phased-Transition-Work-Plan.md`（以下「原案」）  
関連: `Phase6-Step2-Plan.md`（13-1 は Step2-PR-4 の前に実施）  
実地確認の基準: HEAD `06251e0`（2026-09-20）

---

## 0. 位置づけと目的

### 0.1 背景
`DS4Windows/DS4Control/` と `DS4Windows/DS4Control/Services/`、および `DS4Windows/` 直下・`Notifications/` などで、`.cs` ファイルの配置基準が統一されていない。実地調査（2026-09-20）で確認した主な不統一は次のとおり。

1. **契約（インターフェース）の置き場が2つ**: `DI/` に21件、`DS4Control/Services/` に7件。
2. **DI 登録される実装が直下に混在**: `ProfileSlotApplier`、`Default*Factory`／`Creator` など。
3. **Actions サブシステムの二重配置**: 名前空間 `DS4Windows.Actions` が `DS4Control/` 直下（12件）と `Actions/`（31件）に分かれている。旧来のマネージャー・コントローラー（`ActionManager` 等）も直下に残っている。
4. **`DS4Windows/` 直下の機能ファイルの平置き**: ログ5件、更新3件、プロファイル補助4件、ユーティリティ5件。
5. **`NotificationService` の孤立**: `Notifications/` に単独で存在。
6. **規約の空白**: `copilot-instructions.md` には名前空間の原則しかなく、フォルダ配置のルールがなかった。

### 0.2 目的
配置ルールを明文化（`copilot-instructions.md` §3.4）し、既存ファイルを**挙動を変えずに**段階的に新ルールへ移す。本 Step は `git mv` のみを行い、ファイルの内容・名前空間・型名は一切変更しない。

### 0.3 番号と実施時期について
本 Step は Phase6 の末尾番号（Step13）だが、各ステージの実施時期は §3 のとおり Step2 の途中から始まる（番号順の実施ではない）。

---

## 1. 決定事項（2026-09-20）

| # | 論点 | 決定 |
|---|---|---|
| 1 | 配置基準 | **現 Step は案2（インターフェースを `DI/` に一元化）、将来は案4（原案の To-Be／層別再編）** |
| 2 | 13-1（旧称 PR-L）の実施時期 | Step2-PR-4 の前 |
| 3 | 移動の提供方法 | `git mv` のコマンド一覧（本書 §4） |
| 4 | 原案の扱い | 原案を参照し、ステージ1〜3 を 13-2〜13-4 に、原案の To-Be のうちルート直下の残り（プロファイル補助・ユーティリティ）を 13-5 に取り込む。原案のステージ4（`DI/` の契約の再配置、テスト構造の整理）は 13-6（Phase6 範囲外への引き継ぎ）とする |

### 1.1 原案との整合（差異の解決）

| 差異 | 原案 | 本 Step での扱い |
|---|---|---|
| `DI/` の役割 | 配線専用（`AppHost`／`ServiceRegistration`／`ServiceProviderHolder` のみ） | 現 Step では契約も `DI/` に集約（案2）。最終形は 13-6 で原案どおり配線専用にする。**13-1 で `DI/` に移す7件の契約は、13-6 で再度移動する（二重移動）**。git は内容が同一の移動を rename として追跡するため履歴は保たれる |
| ログの移動先 | ステージ1の本文は `DS4Control/Logging/`、To-Be の図は `Core/Logging/` | 最終形の図に合わせ **`Core/Logging/`** とする（二重移動の回避） |
| `DS4Control/Log.cs`（`AppLogger` ほか3型） | 言及なし | 移動しない。複数型ファイルのため、1ファイル1型の是正 Step で分割してから配置を再検討する |
| Action 系の対象 | `Action`、`ActionFactory`、`ActionManager`、`ActionRegistry`、`DefaultActionFactory`、`DefaultActionManager`、`IActionFactory` | 13-1 で名前空間 `DS4Windows.Actions` の12件を移動（原案の対象のうち5件を含む）。13-4 で `ActionManager`、`ActionRegistry` と、関連するコントローラー4件を移動 |
| `Actions/` 内のサブフォルダ（`Abstractions`／`Controllers`／`Implementations`） | 提案 | 13-6（名前空間の統一と同時）。Phase6 では `Actions/` を平置きのまま集約する |
| `MacroManager`／`MacroParser` | 言及なし | 移動しない（マクロエンジンとして `DS4Control/` に残す。13-6 で再検討） |
| `KeyboardSettings.cs`（`DS4Windows/` 直下） | 言及なし | 移動しない（13-6 で再検討） |

---

## 2. 配置ルール（現 Step で適用する基準）

| ID | ルール |
|---|---|
| R1 | `ServiceRegistration` に登録される型のインターフェースは `DI/` に置く |
| R2 | その実装（アダプタ・`NotificationService` を含む）は `DS4Control/Services/` に置く |
| R3 | Actions サブシステム（名前空間 `DS4Windows.Actions`、`Action*`、`*ActionController`、`KeyAction`）は、インターフェースも実装も `Actions/` に集約する |
| R4 | `DS4Control/` 直下は、DI 登録しないレガシー本体・ドメイン部品だけを置く。DI 登録される型は置かない |
| R5 | `DS4Windows/` 直下に置くのは、アプリの起点（`App.xaml.cs`、`StartupMethods.cs`、`GlobalSuppressions.cs`）と、`KeyboardSettings.cs` だけとする。機能ファイルは機能フォルダ（`Updater/`、`Core/Logging/`、`Core/Utilities/`、`DS4Control/Profiles/`）に置く |
| R6 | 名前空間は変更しない（`copilot-instructions.md` §3.3 原則3の過渡期ルール）。新規の実装は `DS4Windows.Services`、契約は `DS4Windows.DI` を用いる |
| R7 | 移動は `git mv` のみで行い、ファイルの内容を変更しない |

---

## 3. ステージ構成

| ステージ | 内容 | 件数 | 実施時期 | リスク |
|---|---|---:|---|---|
| **13-1**（旧 PR-L） | 案2 の適用: `DS4Control/Services/` の契約7件を `DI/` へ、`ProfileSlotApplier` を `Services/` へ、直下の Actions 名前空間の12件を `Actions/` へ | 20 | **Step2-PR-4 の前（決定済み）** | 低 |
| **13-2** | ルート直下の更新3件を `Updater/` へ、ログ5件を `Core/Logging/` へ（原案ステージ1） | 8 | Step2 完了後・Step3 着手前（推奨。13-1 の直後でも可） | 低 |
| **13-3** | `Notifications/NotificationService.cs` を `DS4Control/Services/` へ（原案ステージ2） | 1 | 同上 | 低 |
| **13-4** | `ActionManager`／`ActionRegistry` と関連コントローラー4件を `Actions/` へ（原案ステージ3） | 6 | 同上 | 低〜中 |
| **13-5** | ルート直下のプロファイル補助4件を `DS4Control/Profiles/` へ、ユーティリティ5件を `Core/Utilities/` へ | 9 | 同上 | 低 |
| **13-6** | 引き継ぎ（Phase6 範囲外）: 原案ステージ4（契約の最終配置＝`DI/` を配線専用に）、`DS4WindowsTests/` の構造整理、`Actions/` 内のサブフォルダ化、名前空間とフォルダの一致（案4） | - | 最終クリーンアップフェーズ | 中〜高 |

- Phase6 内で移動するファイルは合計 **44 件**。
- 1ステージ = 1 PR = 1コミット。ステージごとにビルド・テストを通してから次へ進む。
- 各ステージは独立しており、順序の入れ替えは可能。ただし 13-1 を最初に行う（R1〜R3 の基準が確立するため）。

---

## 4. 各ステージの移動対象と `git mv` コマンド

リポジトリのルート（`G:\Cursor_Folder\DS4Windows-Vader4Pro`）で実行する。コマンドは PowerShell と bash のどちらでも動作する。フォルダの作成のみ PowerShell の書式で示す（bash では `mkdir -p` に読み替える）。

### 13-1: 案2 の適用（20件）

| 移動元 | 移動先 |
|---|---|
| `DS4Windows/DS4Control/Services/IDeviceStateAccessor.cs` | `DS4Windows/DI/IDeviceStateAccessor.cs` |
| `DS4Windows/DS4Control/Services/IDs4DeviceRegistry.cs` | `DS4Windows/DI/IDs4DeviceRegistry.cs` |
| `DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs` | `DS4Windows/DI/IElevatedProcessLauncher.cs` |
| `DS4Windows/DS4Control/Services/IProcessInspector.cs` | `DS4Windows/DI/IProcessInspector.cs` |
| `DS4Windows/DS4Control/Services/IProfileSlotApplier.cs` | `DS4Windows/DI/IProfileSlotApplier.cs` |
| `DS4Windows/DS4Control/Services/IVirtualKBM.cs` | `DS4Windows/DI/IVirtualKBM.cs` |
| `DS4Windows/DS4Control/Services/IVirtualKBMLifecycle.cs` | `DS4Windows/DI/IVirtualKBMLifecycle.cs` |
| `DS4Windows/DS4Control/ProfileSlotApplier.cs` | `DS4Windows/DS4Control/Services/ProfileSlotApplier.cs` |
| `DS4Windows/DS4Control/Action.cs` | `DS4Windows/Actions/Action.cs` |
| `DS4Windows/DS4Control/ActionFactory.cs` | `DS4Windows/Actions/ActionFactory.cs` |
| `DS4Windows/DS4Control/DefaultActionFactory.cs` | `DS4Windows/Actions/DefaultActionFactory.cs` |
| `DS4Windows/DS4Control/DefaultActionManager.cs` | `DS4Windows/Actions/DefaultActionManager.cs` |
| `DS4Windows/DS4Control/DefaultKeyActionCreator.cs` | `DS4Windows/Actions/DefaultKeyActionCreator.cs` |
| `DS4Windows/DS4Control/DefaultKeyButtonActionControllerFactory.cs` | `DS4Windows/Actions/DefaultKeyButtonActionControllerFactory.cs` |
| `DS4Windows/DS4Control/IActionFactory.cs` | `DS4Windows/Actions/IActionFactory.cs` |
| `DS4Windows/DS4Control/IKeyActionCreator.cs` | `DS4Windows/Actions/IKeyActionCreator.cs` |
| `DS4Windows/DS4Control/IKeyButtonActionControllerFactory.cs` | `DS4Windows/Actions/IKeyButtonActionControllerFactory.cs` |
| `DS4Windows/DS4Control/IManagedActionManager.cs` | `DS4Windows/Actions/IManagedActionManager.cs` |
| `DS4Windows/DS4Control/KeyActionAdapter.cs` | `DS4Windows/Actions/KeyActionAdapter.cs` |
| `DS4Windows/DS4Control/SpecialActionBase.cs` | `DS4Windows/Actions/SpecialActionBase.cs` |

```powershell
# 移動（内容変更なし）
git mv "DS4Windows/DS4Control/Services/IDeviceStateAccessor.cs" "DS4Windows/DI/IDeviceStateAccessor.cs"
git mv "DS4Windows/DS4Control/Services/IDs4DeviceRegistry.cs" "DS4Windows/DI/IDs4DeviceRegistry.cs"
git mv "DS4Windows/DS4Control/Services/IElevatedProcessLauncher.cs" "DS4Windows/DI/IElevatedProcessLauncher.cs"
git mv "DS4Windows/DS4Control/Services/IProcessInspector.cs" "DS4Windows/DI/IProcessInspector.cs"
git mv "DS4Windows/DS4Control/Services/IProfileSlotApplier.cs" "DS4Windows/DI/IProfileSlotApplier.cs"
git mv "DS4Windows/DS4Control/Services/IVirtualKBM.cs" "DS4Windows/DI/IVirtualKBM.cs"
git mv "DS4Windows/DS4Control/Services/IVirtualKBMLifecycle.cs" "DS4Windows/DI/IVirtualKBMLifecycle.cs"
git mv "DS4Windows/DS4Control/ProfileSlotApplier.cs" "DS4Windows/DS4Control/Services/ProfileSlotApplier.cs"
git mv "DS4Windows/DS4Control/Action.cs" "DS4Windows/Actions/Action.cs"
git mv "DS4Windows/DS4Control/ActionFactory.cs" "DS4Windows/Actions/ActionFactory.cs"
git mv "DS4Windows/DS4Control/DefaultActionFactory.cs" "DS4Windows/Actions/DefaultActionFactory.cs"
git mv "DS4Windows/DS4Control/DefaultActionManager.cs" "DS4Windows/Actions/DefaultActionManager.cs"
git mv "DS4Windows/DS4Control/DefaultKeyActionCreator.cs" "DS4Windows/Actions/DefaultKeyActionCreator.cs"
git mv "DS4Windows/DS4Control/DefaultKeyButtonActionControllerFactory.cs" "DS4Windows/Actions/DefaultKeyButtonActionControllerFactory.cs"
git mv "DS4Windows/DS4Control/IActionFactory.cs" "DS4Windows/Actions/IActionFactory.cs"
git mv "DS4Windows/DS4Control/IKeyActionCreator.cs" "DS4Windows/Actions/IKeyActionCreator.cs"
git mv "DS4Windows/DS4Control/IKeyButtonActionControllerFactory.cs" "DS4Windows/Actions/IKeyButtonActionControllerFactory.cs"
git mv "DS4Windows/DS4Control/IManagedActionManager.cs" "DS4Windows/Actions/IManagedActionManager.cs"
git mv "DS4Windows/DS4Control/KeyActionAdapter.cs" "DS4Windows/Actions/KeyActionAdapter.cs"
git mv "DS4Windows/DS4Control/SpecialActionBase.cs" "DS4Windows/Actions/SpecialActionBase.cs"
```

### 13-2: ルート直下の更新・ログ（8件）

| 移動元 | 移動先 |
|---|---|
| `DS4Windows/UpdaterElevatedHelper.cs` | `DS4Windows/Updater/UpdaterElevatedHelper.cs` |
| `DS4Windows/UpdaterInstallTrace.cs` | `DS4Windows/Updater/UpdaterInstallTrace.cs` |
| `DS4Windows/UpdaterInstaller.cs` | `DS4Windows/Updater/UpdaterInstaller.cs` |
| `DS4Windows/LogItem.cs` | `DS4Windows/Core/Logging/LogItem.cs` |
| `DS4Windows/LogRotator.cs` | `DS4Windows/Core/Logging/LogRotator.cs` |
| `DS4Windows/LogWriter.cs` | `DS4Windows/Core/Logging/LogWriter.cs` |
| `DS4Windows/LoggerHolder.cs` | `DS4Windows/Core/Logging/LoggerHolder.cs` |
| `DS4Windows/StatusLogMsg.cs` | `DS4Windows/Core/Logging/StatusLogMsg.cs` |

```powershell
# 移動先フォルダの作成（git mv は移動先フォルダが無いと失敗する）
New-Item -ItemType Directory -Force -Path "DS4Windows/Core/Logging" | Out-Null
New-Item -ItemType Directory -Force -Path "DS4Windows/Updater" | Out-Null

# 移動（内容変更なし）
git mv "DS4Windows/UpdaterElevatedHelper.cs" "DS4Windows/Updater/UpdaterElevatedHelper.cs"
git mv "DS4Windows/UpdaterInstallTrace.cs" "DS4Windows/Updater/UpdaterInstallTrace.cs"
git mv "DS4Windows/UpdaterInstaller.cs" "DS4Windows/Updater/UpdaterInstaller.cs"
git mv "DS4Windows/LogItem.cs" "DS4Windows/Core/Logging/LogItem.cs"
git mv "DS4Windows/LogRotator.cs" "DS4Windows/Core/Logging/LogRotator.cs"
git mv "DS4Windows/LogWriter.cs" "DS4Windows/Core/Logging/LogWriter.cs"
git mv "DS4Windows/LoggerHolder.cs" "DS4Windows/Core/Logging/LoggerHolder.cs"
git mv "DS4Windows/StatusLogMsg.cs" "DS4Windows/Core/Logging/StatusLogMsg.cs"
```

追加作業: `.editorconfig` の名前空間×フォルダ規約の適用外リスト `[DS4Windows/{DS4Control,DI,Actions}/**.cs]` を、新設フォルダを含む `[DS4Windows/{DS4Control,DI,Actions,Core,Updater}/**.cs]` へ拡張する（このコミットに含める。現状は Style カテゴリが `none` のためビルドへの影響はないが、意図を明示する）。**手順の注意**: `.editorconfig` の変更は `M`（変更）であり、§5.3 の「全行が `R100`」の検証に混ざる。移動の検証（`git add -A` → `R100` 以外の行がないこと）を通した**後**に `.editorconfig` を反映し、`git add .editorconfig` してから同じコミットに含める。

### 13-3: NotificationService の集約（1件）

| 移動元 | 移動先 |
|---|---|
| `DS4Windows/Notifications/NotificationService.cs` | `DS4Windows/DS4Control/Services/NotificationService.cs` |

```powershell
# 移動（内容変更なし）
git mv "DS4Windows/Notifications/NotificationService.cs" "DS4Windows/DS4Control/Services/NotificationService.cs"
```

追加作業: 手元に残る空フォルダ `DS4Windows/Notifications/` を削除する（Git は空フォルダを管理しないため、コミット内容には影響しない）。`NotificationService` の名前空間は `DS4WinWPF` のままで変更しないため、`using` の修正は不要。

### 13-4: Action 系クラスの集約（6件）

| 移動元 | 移動先 |
|---|---|
| `DS4Windows/DS4Control/ActionManager.cs` | `DS4Windows/Actions/ActionManager.cs` |
| `DS4Windows/DS4Control/ActionRegistry.cs` | `DS4Windows/Actions/ActionRegistry.cs` |
| `DS4Windows/DS4Control/KeyAction.cs` | `DS4Windows/Actions/KeyAction.cs` |
| `DS4Windows/DS4Control/KeyButtonActionController.cs` | `DS4Windows/Actions/KeyButtonActionController.cs` |
| `DS4Windows/DS4Control/PressActionController.cs` | `DS4Windows/Actions/PressActionController.cs` |
| `DS4Windows/DS4Control/ToggleActionController.cs` | `DS4Windows/Actions/ToggleActionController.cs` |

```powershell
# 移動（内容変更なし）
git mv "DS4Windows/DS4Control/ActionManager.cs" "DS4Windows/Actions/ActionManager.cs"
git mv "DS4Windows/DS4Control/ActionRegistry.cs" "DS4Windows/Actions/ActionRegistry.cs"
git mv "DS4Windows/DS4Control/KeyAction.cs" "DS4Windows/Actions/KeyAction.cs"
git mv "DS4Windows/DS4Control/KeyButtonActionController.cs" "DS4Windows/Actions/KeyButtonActionController.cs"
git mv "DS4Windows/DS4Control/PressActionController.cs" "DS4Windows/Actions/PressActionController.cs"
git mv "DS4Windows/DS4Control/ToggleActionController.cs" "DS4Windows/Actions/ToggleActionController.cs"
```

### 13-5: ルート直下の残り（9件）

| 移動元 | 移動先 |
|---|---|
| `DS4Windows/ProfileEntity.cs` | `DS4Windows/DS4Control/Profiles/ProfileEntity.cs` |
| `DS4Windows/ProfileList.cs` | `DS4Windows/DS4Control/Profiles/ProfileList.cs` |
| `DS4Windows/AutoProfileChecker.cs` | `DS4Windows/DS4Control/Profiles/AutoProfileChecker.cs` |
| `DS4Windows/AutoProfileHolder.cs` | `DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs` |
| `DS4Windows/ArgumentParser.cs` | `DS4Windows/Core/Utilities/ArgumentParser.cs` |
| `DS4Windows/DateTimeJsonConverter.cs` | `DS4Windows/Core/Utilities/DateTimeJsonConverter.cs` |
| `DS4Windows/OneEuroFilter.cs` | `DS4Windows/Core/Utilities/OneEuroFilter.cs` |
| `DS4Windows/ReaderWriteLockSlimWrappers.cs` | `DS4Windows/Core/Utilities/ReaderWriteLockSlimWrappers.cs` |
| `DS4Windows/WindowLayoutDefaults.cs` | `DS4Windows/Core/Utilities/WindowLayoutDefaults.cs` |

```powershell
# 移動先フォルダの作成（git mv は移動先フォルダが無いと失敗する）
New-Item -ItemType Directory -Force -Path "DS4Windows/Core/Utilities" | Out-Null
New-Item -ItemType Directory -Force -Path "DS4Windows/DS4Control/Profiles" | Out-Null

# 移動（内容変更なし）
git mv "DS4Windows/ProfileEntity.cs" "DS4Windows/DS4Control/Profiles/ProfileEntity.cs"
git mv "DS4Windows/ProfileList.cs" "DS4Windows/DS4Control/Profiles/ProfileList.cs"
git mv "DS4Windows/AutoProfileChecker.cs" "DS4Windows/DS4Control/Profiles/AutoProfileChecker.cs"
git mv "DS4Windows/AutoProfileHolder.cs" "DS4Windows/DS4Control/Profiles/AutoProfileHolder.cs"
git mv "DS4Windows/ArgumentParser.cs" "DS4Windows/Core/Utilities/ArgumentParser.cs"
git mv "DS4Windows/DateTimeJsonConverter.cs" "DS4Windows/Core/Utilities/DateTimeJsonConverter.cs"
git mv "DS4Windows/OneEuroFilter.cs" "DS4Windows/Core/Utilities/OneEuroFilter.cs"
git mv "DS4Windows/ReaderWriteLockSlimWrappers.cs" "DS4Windows/Core/Utilities/ReaderWriteLockSlimWrappers.cs"
git mv "DS4Windows/WindowLayoutDefaults.cs" "DS4Windows/Core/Utilities/WindowLayoutDefaults.cs"
```

追加作業: `.editorconfig` の適用外リスト（13-2 で拡張済み）に、13-5 で新設する `DS4Control/Profiles/`（`DS4Control` 配下のため対象済み）と `Core/Utilities/`（`Core` 配下のため対象済み）が含まれることを確認する。

---

## 5. 各ステージ共通の手順

### 5.1 事前確認
1. 作業ツリーがクリーンであること（`git status`）。
2. 直前のコミット時点で、ビルド・テストビルド・テスト実行が全件成功していること。
3. 進行中の別ブランチや未マージの変更がないこと（ある場合は、対象ファイルを触っていないか確認する）。

### 5.2 実行
1. §4 の該当ステージのコマンドを上から順に実行する。
2. `urls.txt`（ファイルのURL一覧）は、利用者がファイル構成の変更後にバッチ処理で更新する。移動の作業には含めない。

### 5.3 検証
1. **全件が rename であること（内容変更がないこと）**:

```powershell
git add -A
git diff --cached -M100% --name-status
```

   出力の全行が `R100` で始まること。`R100` 以外（`A`／`D`／`M` や `R9x`）が1行でもあれば、コミットせずに原因を調べる。PowerShell では次のコマンドで、`R100` 以外の行だけを抽出できる（出力が空なら合格）。

```powershell
git diff --cached -M100% --name-status | Select-String -NotMatch '^R100'
```

2. `dotnet build -c Release`（警告数が移動前と同じであること）。
3. テストビルド、`dotnet test`（全件成功）。
4. 起動スモーク: アプリを起動 → コントローラーを接続 → 入力が反映されることを確認 → 終了。ステージごとの追加確認:
   - 13-2: 起動と終了、ログ出力（ログウィンドウに出力されること）。更新機能そのものの実行は不要（内容が変わらないため）。
   - 13-3: 通知（トースト等）が1件表示されること。
   - 13-4: KBM 出力・マクロ・スペシャルアクションのいずれか1つが動くこと。

### 5.4 コミットとロールバック
- コミットメッセージ例: `refactor(layout): Phase6-Step13-1 契約を DI/ へ集約し Actions 名前空間を Actions/ へ移動（git mv のみ）`
- 問題があった場合は `git revert <コミット>` で全体を元に戻せる（移動のみのため副作用がない）。

---

## 6. 実地確認済みの事項（HEAD `06251e0`）

| 項目 | 結果 |
|---|---|
| csproj のソース指定 | `DS4WinWPF.csproj`／`DS4Windows.Actions.Tests.csproj`／`StandaloneTests.csproj` のいずれにも、`.cs` の明示的な `Compile Include`／`Remove` はない（SDK 既定のグロブで収集される）。移動でビルド定義の修正は不要 |
| 移動先の衝突 | 全44件で、移動先に同名ファイルなし。移動元も全件存在 |
| 名前空間×フォルダの規約 | `.editorconfig` で Style カテゴリは `none`、`TreatWarningsAsErrors=false`。新フォルダへの移動でビルドは影響を受けない（`.editorconfig` の適用外リストは 13-2 で拡張） |
| CI | `.github/workflows/release.yml` は csproj のパスのみ参照。影響なし |
| 旧パスを文字列で参照するファイル | 履歴文書（Phase1〜6 の報告書・計画書）と `Phase6-Step1-Audit/manifest.json` は過去時点の記録のため更新しない。現行の運用文書は該当ステージで更新する。`urls.txt` は利用者のバッチ処理で更新する |
| `DS4WindowsTests/` | 直下に52件（サブフォルダは `TestData`、`ActionControllerTests`）。本 Step では移動しない（13-6） |

---

## 7. リスクと対策

| リスク | 対策 |
|---|---|
| 13-1 で `DI/` へ移す7件が 13-6 で再移動になる（二重移動） | 内容が同一の移動は git が rename として追跡する。決定済み（案2 → 案4）のため許容 |
| 移動後に「名前空間とフォルダの不一致」が増える | 意図的（§3.3 原則3の過渡期ルール）。13-6 で解消する |
| 他の作業との競合 | 1ステージ = 1 PR を直列に実施する。移動前に §5.1 で未マージの変更を確認する |
| 移動と内容変更の混在による履歴の追跡不能 | R7（`git mv` のみ）と §5.3 の `R100` 検証で防ぐ |
| 同名ファイルの後発追加 | ステージ実施の直前に、移動先の衝突を再確認する（§5.1） |

---

## 8. 完了条件

1. 13-1〜13-5 が完了し、§5.3 の検証（全件 `R100`、ビルド、テスト、起動スモーク）が全て合格していること。
2. `copilot-instructions.md` §3.4（配置ルール）が反映されていること。
3. `DS4Windows/` 直下の `.cs` が R5 で定めたファイルのみであること。
4. `DS4Control/` 直下に、DI 登録される型が残っていないこと。
5. 13-6 の引き継ぎ内容が、最終クリーンアップフェーズの計画に記載されていること。

---

## 9. 他の Step との関係

| Step | 関係 |
|---|---|
| Step2（`ControlService`） | 13-1 は Step2-PR-4 の前に実施する。13-2〜13-5 は Step2 完了後（PR-6 の後）・Step3 着手前が推奨 |
| Step3〜Step10 | 各 Step の計画書に書かれたファイルパスは、13-1〜13-5 の対象ファイルについて変わる。各 Step の着手時に、対象ファイルの現在のパスを確認する（各 PR で grep 台帳を再作成する既存ルールで足りる） |
| Step12（`ScpUtil.cs` の旧シム削除） | 本 Step では `ScpUtil.cs` を移動しない。影響なし |
| Step10b（1ファイル1型の全数是正、Step10 と Step11 の間） | 本 Step（13-1〜13-5）の**後**に実施する。移動してから分割することで、新設する型のファイルが最終の配置先に作られる |
| Phase7（`Mapping.cs` の instance 化） | 影響なし |

---

## 付録: 実施記録

### 13-1（旧 PR-L）: 2026-09-20 実施済み

- **実施内容**: §4 の 13-1 の `git mv` 一覧（20件）を実行し、リモートリポジトリへ反映済み（HEAD `6bcfde9` で、`DI/` 29ファイル、`Actions/` 43ファイル、`DS4Control/Services/` 26ファイル、`DS4Control/` 直下 48ファイルの構成になっていることを確認した）。
- **自動検証**: ビルド、テストビルド、テスト実行の全てが成功（開発環境での実施結果）。
- **起動スモーク**（ユーザー実施）: DS4Windows の起動、コントローラーの接続、プロファイルの適用、プロファイル切替のスペシャルアクションの実行 — すべて問題なし。
- **残り**: 13-2〜13-5 は Step2 の完了後（Step3 の着手前）に実施する。

### 13-2〜13-5: 事前検証（HEAD `5785af9`、2026-09-20）

- 4ステージの移動 24 件（13-2: 8、13-3: 1、13-4: 6、13-5: 9）について、移動元がすべて存在し、移動先に同名ファイルがないことを再確認した（問題なし）。
- 移動後、`DS4Windows/` 直下に残る `.cs` は `App.xaml.cs`、`StartupMethods.cs`、`GlobalSuppressions.cs`、`KeyboardSettings.cs` の4件になる（R5 のとおり）。
- 13-2 に含める `.editorconfig` の変更（適用外リストへ `Core`、`Updater` を追加）は、`.editorconfig` の更新版として提供した。
- **実施の進め方（任意）**: 4ステージはいずれも `git mv` のみで、ステージごとの `R100` 検証を通すため、ビルド・テストは最後に1回でもよい（コミットはステージごとに分ける。失敗した場合は、コミット単位で原因を特定できる）。

### 13-2〜13-5: 2026-09-20 実施済み

- **実施内容**: §4 の 13-2〜13-5 の `git mv` 一覧（24件）を適用し、`.editorconfig` の更新（適用外リストへ `Core`、`Updater` を追加）を反映して、リモートリポジトリへ反映済み（HEAD `c28381f`）。
- **配置の確認**（HEAD `c28381f`）: `DS4Windows/` 直下の `.cs` は `App.xaml.cs`、`GlobalSuppressions.cs`、`KeyboardSettings.cs`、`StartupMethods.cs` の4件のみ（R5 のとおり）。`Updater/` 3件、`Core/Logging/` 5件、`Core/Utilities/` 5件、`DS4Control/Profiles/` 4件、`Actions/` 49件、`DS4Control/Services/` 27件、`DI/` 29件。`Notifications/` は消滅した。
- **自動検証**: ビルド、テストビルドは成功。テスト実行で2件のエラー。**原因は移動そのものではなく、静的初期化の循環という潜在的な欠陥**（ファイル移動でコンパイル順序が変わり、テストの実行順序が変わって表に出た）。`Phase6-Step2-Plan.md` 付録 B.13 に原因と是正を記録した。
- **Step13 の状況**: 13-1〜13-5 の移動（合計44件）が完了した。残りは 13-6（Phase6 範囲外への引き継ぎ）のみ。テスト是正版の確認後に、Step13（13-1〜13-5）の完了を確定する。

### 完了確定: 2026-09-21

13-1〜13-5（`.cs` ファイル44件の配置整理）の完了を確定した。§8 の完了条件の確認結果は次のとおり。

| # | 完了条件 | 結果 |
|---|---|---|
| 1 | 13-1〜13-5 が完了し、検証（全件 `R100`、ビルド、テスト、起動スモーク）が合格している | 済。ビルド、テストビルド、テスト実行の全件成功、Release ビルドはエラー・警告なし。13-1 は起動スモーク（起動、接続、プロファイル適用、プロファイル切替）も問題なし。`R100` 検証の結果は個別の報告がなく、配置が想定どおりであること（HEAD `c68329f`）を確認した |
| 2 | `copilot-instructions.md` §3.4 が反映されている | 済 |
| 3 | `DS4Windows/` 直下の `.cs` が R5 のファイルのみ | 済（`App.xaml.cs`、`StartupMethods.cs`、`GlobalSuppressions.cs`、`KeyboardSettings.cs` の4件） |
| 4 | `DS4Control/` 直下に DI 登録される型が残っていない | 済。`ServiceRegistration` に登録された実装クラス31件は、`DS4Control/Services/` に24件、`Actions/` に7件で、`DS4Control/` 直下には0件（機械的に確認） |
| 5 | 13-6 の引き継ぎ内容が最終クリーンアップフェーズの計画に記載されている | 一部。引き継ぎ内容は本書 §3 と §1.1 に記載済み。最終クリーンアップフェーズの計画書は未作成のため、作成時に取り込む |

- 13-6 の引き継ぎ内容: `DI/` の契約の最終配置（`DI/` を配線専用にする）、`DS4WindowsTests/` の構造整理、`Actions/` 内のサブフォルダ化、名前空間とフォルダの一致（案4）。
- 副産物: 13-2〜13-5 の適用後に見つかった静的初期化の循環（`Phase6-Step2-Plan.md` 付録 B.13）を是正した。