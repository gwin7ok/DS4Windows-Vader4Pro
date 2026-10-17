# Phase5-Step14-Issue7 実装完了報告書（必須スコープ: タスク1・2・3・6・7）

作成日: 2026-09-11
対象ブランチ: `For-DI-migration-work`
対応計画書: `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-Fix-Plan.md`（2026-09-11改訂版、タスク4・5はPhase6へ移管済み）
実装方式: `git clone --depth 1 --branch For-DI-migration-work` によるローカルクローン上でピンポイント編集（`copilot-instructions.md` §3.2準拠）

---

## 1. 実装概要

`Phase5-Step14-Issue7-Fix-Plan.md` の必須スコープ（タスク1・2・3・6・7）を実装した。`IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType` が参照していたGlobal非連動の孤立配列を根本原因とする表示バグを、新設した `IProfileSettingsService.OutContType`（`Global.OutContType`と同一実体への委譲）を正本（SSOT）とする形に是正した。

## 2. 変更ファイル一覧（7ファイル）

| # | ファイル（リポジトリ内パス） | 対応タスク | 変更内容 |
|---|---|---|---|
| 1 | `DS4Windows/DI/IProfileSettingsService.cs` | タスク1 | `OutContType[] OutContType { get; }` を新設 |
| 2 | `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | タスク1 | `OutContType => _config.outputDevType` を実装 |
| 3 | `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | タスク2 | `ControllerTypeIndex`／`ContType`／`UpdateLateProperties()` の3箇所を`profileSettings.OutContType`経由に修正 |
| 4 | `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | タスク3 | `IProfileSettingsService`をコンストラクタ末尾にオプション引数として追加、253行目の参照先を修正、未使用となった`outputSlotService`フィールドに経緯コメントを付与 |
| 5 | `DS4Windows/DI/IOutputSlotService.cs` | タスク6 | `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` に技術的負債コメントを付与 |
| 6 | `DS4Windows/DS4Control/Services/OutputSlotService.cs` | タスク6 | 上記4メソッドの実装側にも要約コメントを付与 |
| 7 | `DS4WindowsTests/ProfileSettingsServiceTests.cs` | タスク7 | `OutContType_ShouldShareBackingStoreWithGlobalShim` テストを新規追加 |

各ファイルの差分は同梱の `Phase5-Step14-Issue7-implementation.diff`（`git diff`形式）でも確認できる。

## 3. タスク6実装時の設計判断（計画書からの拡張）

計画書のタスク6は「4メソッドに同一の簡略コメントを付与する」想定だったが、実装前に `OutputSlotService.cs` の実装を確認したところ、`PluginSlot`/`UnplugSlot` は `GetOutputDeviceType`/`SetOutputDeviceType`（孤立配列の読み書きのみ）とは異なり、実際に `ControlService.AttachUnboundOutDev`/`DetachUnboundOutDev` を呼び出す実装を持つことを確認した。そのため、コメント内容を以下のように区別して記述した。

- `GetOutputDeviceType`/`SetOutputDeviceType`: 「Global非連動の孤立配列であり、正しい参照先は`IProfileSettingsService.OutContType`」という孤立バグの説明。
- `PluginSlot`/`UnplugSlot`: 「呼出元0件（実地確認済み）の並行実装であり、実際のホットスワップは`Global.ApplyProfile`→`ScpUtil.cs`の`PostLoadSnippet`という別経路で行われている」という実態の説明。

この整理は、別途作成した `Phase6-Step6-Plan.md` の実地確認結果（§2.2, §2.3）と整合する内容である。

## 4. タスク7の実施状況と未実施項目（要確認）

| 項目 | 状況 |
|---|---|
| `ProfileSettingsService`の新規プロパティに対する単体テスト | **実施済み**（`OutContType_ShouldShareBackingStoreWithGlobalShim`） |
| `ProfileSettingsViewModel`に対する単体テスト | **未実施（要確認）**。既存の同クラスへの単体テストファイル自体が存在しないことを確認した。計画書の「テストファイルが存在しない場合は新規作成の要否をgwin7ok氏に確認する」という条件に従い、新規作成は見送った。新規作成を希望される場合はお申し付けください。 |
| `SpecialActionsListViewModelTests.cs`への影響確認 | **確認済み・修正不要**。コンストラクタへの追加引数は末尾のオプション引数であり、既存テストの呼び出し（4引数）はそのままソース互換。 |
| `dotnet build` | **未実施（要gwin7ok氏実施）**。本作業はLinuxベースのサンドボックス環境で行っており、.NET 8 / WPF（`net8.0-windows`）のビルドツールチェーンが存在しないため、Claude側では実行不可能である。 |
| `dotnet test` | **未実施（要gwin7ok氏実施）**。理由は上記と同じ。 |

### 4.1 Claude側で実施した代替検証

`dotnet build`/`dotnet test`が実行できない制約下で、以下の静的検証を実施した。

1. 各変更ファイルの中括弧`{}`・丸括弧`()`の対応数が変更前後で崩れていないことをスクリプトで確認済み（全7ファイルで対応数一致）。
2. 変更箇所ごとに、変更前後のコード片を個別に`view`で目視確認済み。
3. `OutContType`型・`IProfileSettingsService`型・`Global.ProfileSettingsServiceInstance`等、既存コードで使われている型・メンバの実在を`grep`で確認したうえで参照している。
4. 変更した2つのコンストラクタ（`SpecialActionsListViewModel`）について、リポジトリ全体を`grep`し、既存の呼び出し箇所（`ProfileEditor.xaml.cs`、テストファイル）が新しい引数追加の影響を受けないことを確認済み。

**上記はあくまで静的な確認であり、実際のコンパイル可否・テスト結果の保証にはならない。gwin7ok氏の手元（Windows環境）での`dotnet build`／`dotnet test`の実行を強く推奨する。**

## 5. 実機確認が必要な項目（§7完了条件より）

以下はコードレビューでは確認できず、実機（または実際にビルドしたアプリ）での確認が必要である。

1. `<OutputContDevice>DS4</OutputContDevice>`を持つプロファイルを開いた際、`Emulated Controller`コンボボックスが正しく`DS4`を表示すること。
2. Special Action一覧のボタン名表示が、DS4プロファイルで空欄にならず正しいDS4名称で表示されること。

## 6. Fix-Plan.md への反映

`Phase5-Step14-Issue7-Fix-Plan.md`のタスク1・2・3・6・7のチェックボックスおよび§7完了条件を、本実装の進捗に合わせて更新した（`[x]`化、および未実施項目への「要gwin7ok氏確認/実施」の明記）。更新版を同梱する。

## 7. 次のアクション

1. 本報告書とともに提供する7つの`.cs`ファイルを、リポジトリの元パスに配置（上書き）する。
2. Windows環境で `dotnet build` / `dotnet test` を実行し、結果を確認する。
3. 実機（またはビルド済みアプリ）で、§5に記載の2項目を確認する。
4. 問題がなければコミットし、Phase5-Step14の残タスク（自動テスト・実機検証のチェックリスト、Step15）へ進む。
5. `ProfileSettingsViewModel`向け単体テストの新規作成要否について、ご判断をお願いします。