# Phase 5 Step14前クリーンアップ 完了報告書

作成日: 2026-09-09
対象計画書: `docs-forDIMG/MadeByAgent/Phase5-Step14-pretest-cleanup-plan.md`
対象ブランチ: `For-DI-migration-work`
実装コミット: `ecb03191970c1508570a5b0d5b92389f9478fcf4`
（先行コミット `f5b92c10e46910854da570d0322b6e377922b724`＝計画書追加）
ビルド・テスト結果: **ビルド成功／テストビルド成功／テスト実行 全件成功**（実施者確認済み）

---

## 1. 完了サマリ

`Phase5-Step14-pretest-cleanup-plan.md`で計画したPR-A〜PR-Eの全項目を実装し、ビルド・テストビルド・
テスト実行の全件成功、および`For-DI-migration-work`ブランチへのコミット・反映が完了した。

| PR | 内容 | 状態 |
|---|---|---|
| PR-A | `Program.rootHub`無条件直接代入の統一（4ファイル） | **完了** |
| PR-B | `IAppSettingsService`拡張（8プロパティ） | **完了** |
| PR-C | `IPathService`拡張（`ExecutablePath`） | **完了** |
| PR-D | `IEnvironmentService`拡張（4メンバ） | **完了** |
| PR-E | `IAppearanceSettingsService`新設 | **完了** |

---

## 2. 実装内容の詳細

### 2.1 PR-A: `Program.rootHub`無条件直接代入の統一

以下4ファイルの`ControlService`取得処理を、他のDIサービスと同一の
`AppHost.GetService<T>() ?? フォールバック`形式に統一した。

- `DS4Windows/DS4Forms/MainWindow.xaml.cs`
- `DS4Windows/DS4Forms/StickCalibrationWindow.xaml.cs`
- `DS4Windows/DS4Forms/BindingWindow.xaml.cs`
- `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`

`ServiceRegistration.cs`のDIファクトリが`Program.rootHub`と同一インスタンスを返す設計のため、
本変更による機能的な挙動変化はない（スタイル統一のみ）。

### 2.2 PR-B: `IAppSettingsService`拡張

`LogMinLevel`, `CheckUpdateStartupEnabled`, `CheckEveryValue`, `CheckEveryUnit`, `LastChecked`,
`LastVersionCheckedNum`（読み取り専用）, `FirstRun`, `RunHotPlug`の8プロパティを追加。
いずれも既存の`Notifications`等と同一の、`Global`への薄いシムパターン（独立フィールドを持たない）
で実装し、過去に発覚した「孤立プロパティバグ」の再発を防止する設計とした。

`MainWindow.xaml.cs`側の該当8箇所を`appSettingsService`経由の呼び出しに置換した。

### 2.3 PR-C: `IPathService`拡張

`ExecutablePath`プロパティを新設し、Scoop等のジャンクションシンボリックリンク解決ロジックを含む
`Global.exelocation`への薄い委譲として実装した。`MainWindow.xaml.cs`側の`exelocation`直接参照2箇所、
および既存メンバ`ExecutableDirectory`/`AppDataPath`で代替可能だった`exedirpath`/`appDataPpath`参照
計4箇所（`ImportProfBtn_Click`内2箇所、`XinputCheckerBtn_Click`内1箇所を含む）を置換した。

### 2.4 PR-D: `IEnvironmentService`拡張

`IsAdministrator()`, `ApplicationVersion`（`exeversion`相当）, `RefreshHidHideInfo()`,
`RefreshFakerInputInfo()`の4メンバを追加。`MainWindow.xaml.cs`に`environmentService`フィールドを
新設し（既存の`Global.EnvironmentServiceInstance`フォールバックパターンを踏襲）、該当5箇所
（`IsAdministrator()`×2、`exeversion`×1、`RefreshHidHideInfo`/`RefreshFakerInputInfo`各1）を置換した。

### 2.5 PR-E: `IAppearanceSettingsService`新設

全体移行計画書§5.4 #9で計画されていたが、Phase4・Phase5のいずれのStepでも未実装であったことが
今回の調査で判明したため、新規に実装した。

- `DS4Windows/DI/IAppearanceSettingsService.cs`（新規）
- `DS4Windows/DS4Control/Services/AppearanceSettingsService.cs`（新規、`Global`への薄いシム）
- `ServiceRegistration.cs`にSingleton登録を追加

`MainWindow.xaml.cs`に`appearanceSettingsService`フィールドを新設（新設サービスのため
`?? new AppearanceSettingsService()`フォールバック）し、`UseCurrentTheme`・
`iconChoiceResources[UseIconChoice]`の2箇所を置換した。

---

## 3. 検証結果

### 3.1 静的検証（実装中に実施）

| 検証項目 | 結果 |
|---|---|
| `MainWindow.xaml.cs`の`Global.*`残存参照 | 真の定数2件（`TEST_PROFILE_INDEX`×3, `RESOURCES_PREFIX`×6）と、受入れ済みフォールバックパターン5件（`?? Global.XxxInstance`）のみに縮小したことをgrepで確認 |
| `Program.rootHub`無条件直接代入 | リポジトリ全体で **0件** になったことをgrepで確認 |
| 全変更・新規ファイルの中括弧バランス | 全13ファイルで開閉一致を確認 |

### 3.2 ビルド・テスト（実施者確認）

- ビルド: **成功**
- テストビルド: **成功**
- テスト実行: **全件成功**

### 3.3 コミット・反映

- コミットメッセージ: `feat: Phase5-Step14前クリーンアップを実施し、Global参照の解消と新サービスの追加を行った ＊ビルド、テストビルド、テスト実行全て成功`
- コミットハッシュ: `ecb03191970c1508570a5b0d5b92389f9478fcf4`
- `For-DI-migration-work`ブランチへの反映を確認済み（差分13ファイル、+163/-31行、`git show --stat`で本報告書作成時に再確認済み）

---

## 4. 完了判定基準の充足状況（計画書§5対応）

| 完了判定基準 | 状態 |
|---|---|
| `MainWindow.xaml.cs`から`Global.*`直接参照が、分類①（真の定数2件）を除きゼロ | ✅ 達成 |
| `Program.rootHub`の無条件直接代入が4ファイルとも統一形式に | ✅ 達成 |
| 既存の自動テストが全件成功を維持 | ✅ 達成（実施者確認） |
| PR-Eのテーマ切替・トレイアイコン表示の動作確認 | **未確認**（下記§5参照） |

---

## 5. 残課題・申し送り事項

- **PR-Eの実機確認**: 計画書§4「PR粒度と実施順序」で「PR-Eのみ新規サービスのため、最小限の実機確認
  （テーマ変更・トレイアイコン表示）を推奨する」としていたが、本報告書作成時点でこの目視確認の実施有無は
  未確認である。Phase5-Step14の実機テスト実施時に、テーマ切替（Default/Light/Dark）およびトレイアイコン
  種別変更（Default/Colored/White/Black/Battery）が従来通り反映されることを確認項目に加えることを推奨する。
- **既知の軽微な挙動差異**: `ImportProfBtn_Click`のプロファイルインポート初期ディレクトリ判定を
  `pathService.ExecutableDirectory`（内部的に`AppContext.BaseDirectory`）に統一した。旧`Global.exedirpath`
  はScoopインストール時のジャンクションシンボリックリンクを解決するロジックを持っていたため、
  **Scoopでジャンクション配置された環境でのみ**、インポートダイアログの初期ディレクトリが従来と
  異なる可能性がある。該当環境がある場合はStep14実機確認で確認することを推奨する（通常インストール
  環境では影響なし）。

---

## 6. 総合結論

Phase5-Step14の実機テスト着手前に解消しておくべきとされた2項目（`MainWindow.xaml.cs`の`Global.*`
残存参照、および`Program.rootHub`無条件直接代入の不統一）は、計画書通りPR-A〜PR-Eとして実装・
ビルド・テスト・コミットが完了した。副次的な発見として、全体移行計画書で計画されながら未実装のまま
残っていた`IAppearanceSettingsService`もあわせて新設した。

残る作業は、上記§5の実機確認2点（テーマ・トレイアイコン表示、Scoop環境でのインポートダイアログ挙動）
を、Phase5-Step14の実機検証チェックリストの一部として組み込むことである。これ以外に本クリーンアップ
作業に起因する新たな未解決事項はない。