# Phase6-Step7 計画書: `SettingsViewModel.cs`他 主要ViewModel群の解消

作成日: 2026-09-09
対象ブランチ: `For-DI-migration-work`
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`
着手前提: Phase6-Step1（詳細監査と対象確定）の完了、および`Phase6-Step1-Global-Usage-Classification-Report.md`の承認

---

## 0. 前提の明記

本計画書は、Phase6-Plan.md §0.2記載の暫定値（`SettingsViewModel.cs`23件、`MainWindowsViewModel.cs`8件、
`TrayIconViewModel.cs`5件、`ProfileSettingsViewModel.cs`4件、合計39件）を基に、**作業方針を先行して
定義**するものである。対象の確定件数・個別のメンバ名・移行先サービス名は、Step1の成果物を待って
本計画書に反映する。

### 重要な前提の区別

全体計画書§4.3（2026-09-09注記）で明記した通り、**「ViewModelがDI/Factory化されていること」と
「ViewModel内部のロジックがGlobalを直接呼んでいないこと」は別の問題**である。対象4ファイルは
いずれもPhase4-Step9でDI/Factory化が完了済みだが、コンストラクタ注入とは別に、メソッド内部で
`Global.*`を直接呼んでいる箇所が残存している。Step7はこの内部ロジックの残存参照を対象とする
（ViewModelの生成方式自体には変更を加えない）。

---

## 1. 目的

`SettingsViewModel.cs`, `MainWindowsViewModel.cs`, `TrayIconViewModel.cs`, `ProfileSettingsViewModel.cs`
内の`Global.*`直接参照（Step1確定値、暫定39件）を、コンストラクタで既に注入されているDIサービス、
または軽微拡張したDIサービス経由の呼び出しへ置換する。

---

## 2. 作業方針

### 2.1 ファイルごとの優先順位

件数の多い順に着手する（Phase4/5の実績上、件数が多いファイルほど機械的な単純リダイレクトの割合が
高い傾向があるため）。

1. `SettingsViewModel.cs`（23件）: アプリ全体設定画面。多岐にわたる設定項目のため、Step1での
   分類がとりわけ重要。
2. `MainWindowsViewModel.cs`（8件）: メインウィンドウの画面状態。
3. `TrayIconViewModel.cs`（5件）: トレイアイコン関連。`MainWindow.xaml.cs`のトレイアイコンCustomName
   対応（Phase5-Step14前クリーンアップPR-C）と重複領域がある可能性がある。
4. `ProfileSettingsViewModel.cs`（4件）: パターンC（ファクトリ生成、実行時パラメータ`device`を受け取る）。
   件数は最も少ないが、ファクトリ経由で注入されるサービスとの整合を個別に確認する。

### 2.2 既存コンストラクタ注入サービスの活用確認

各ViewModelは既にPhase4-Step9でDI/Factory化されており、コンストラクタで複数のDIサービスを受け取って
いる可能性が高い。Step7の作業の多くは、**新たな依存を注入するのではなく、既に注入済みのサービスの
未使用メンバへのアクセスを追加する、または既存の`Global.*`呼び出しを既存フィールド経由に差し替える**
だけで完了する可能性がある。着手前に各ViewModelのコンストラクタ・既存フィールド一覧を確認し、
重複投資を避ける。

### 2.3 `TrayIconViewModel.cs`と`MainWindow.xaml.cs`の重複確認

Phase5-Step14前クリーンアップのPR-C・PR-Eで、`MainWindow.xaml.cs`側のトレイアイコン関連
（`ExecutablePath`, `IAppearanceSettingsService.GetIconResourcePath`等）は既に解消済みである。
`TrayIconViewModel.cs`側の5件が同様の内容であれば、新規実装は不要でMainWindow側の既存サービスを
再利用するだけで済む可能性が高い。

---

## 3. PR粒度・実施順序

| PR | 内容 |
|---|---|
| PR-1 | `SettingsViewModel.cs`の分類(a)（単純リダイレクト） |
| PR-2 | `SettingsViewModel.cs`の分類(b)（既存サービス拡張を伴うもの） |
| PR-3 | `MainWindowsViewModel.cs`の全件 |
| PR-4 | `TrayIconViewModel.cs`の全件（`MainWindow.xaml.cs`との重複確認込み） |
| PR-5 | `ProfileSettingsViewModel.cs`の全件（ファクトリ注入サービスとの整合確認込み） |

---

## 4. 完了判定基準

- [ ] 対象4ファイル内の`Global.`直接参照（Step1確定分）が0件になっていること。
- [ ] 各ViewModelの既存コンストラクタ注入パターンに変更が加えられていないこと（新規依存の追加は
      既存サービスの拡張で対応し、コンストラクタシグネチャ自体の変更は最小限にとどめる）。
- [ ] 設定画面・メインウィンドウ・トレイアイコン・プロファイル編集画面の操作が、置換前と同等に動作すること。
- [ ] 既存自動テストが全件成功を維持していること。

---

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| `SettingsViewModel.cs`の23件が多岐にわたる設定項目のため、分類作業が煩雑になる | Step1で機能グループ別（言語/テーマ、更新チェック、通知、デバイス関連等）に整理してもらい、PR-1/2をさらにサブPRへ分割してもよい |
| `TrayIconViewModel.cs`と`MainWindow.xaml.cs`の重複箇所で実装がずれる | PR-4着手前に`MainWindow.xaml.cs`の該当実装を再確認し、同一のサービス呼び出しに揃える |
| `ProfileSettingsViewModel.cs`はパターンC（ファクトリ生成）のため、ファクトリのシグネチャ変更が必要になる可能性 | ファクトリシグネチャの変更が必要と判明した場合は、着手前にユーザーへ影響範囲を提示し承認を得る |

---

## 6. 次のアクション

1. Phase6-Step1の完了後、対象4ファイル分の確定件数・分類・既存コンストラクタ注入サービス一覧を確認する。
2. 承認後、PR-1（`SettingsViewModel.cs`の単純リダイレクト）から着手する。
3. 完了後、`Phase6-Status.md`のStep7欄を更新し、Step8（残りの小型UIファイル群の解消）の計画書作成へ進む。