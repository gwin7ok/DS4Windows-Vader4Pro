# Phase6-Step10 計画書: 残りの小型UIファイル群の解消

作成日: 2026-09-09
改定日: 2026-09-11（Phase6全12ステップ再編に伴うステップ番号変更）
対象ブランチ: `For-DI-migration-work`
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`
着手前提: Phase6-Step1（詳細監査と対象確定）の完了、および`Phase6-Step1-Global-Usage-Classification-Report.md`の承認

---

## 0. 前提の明記

本計画書は、Phase6-Plan.md §0.2記載の暫定値（`WelcomeDialog.xaml.cs`9件、`PresetOption.cs`10件、
`SaveWhere.xaml.cs`5件、`BindingWindow.xaml.cs`5件、合計29件）を基に、**作業方針を先行して定義**する
ものである。対象の確定件数・個別のメンバ名・移行先サービス名は、Step1の成果物を待って本計画書に反映する。

ドメイン3（起動・UI層）の最終Stepであり、Phase6-Plan.mdでも「件数が少なく個別ファイルの複雑度も低いため、
1PRでまとめて対応可能な見込み」と位置づけられている。Step2〜7で確立された置換パターンの総仕上げとして、
機械的に処理することを想定する。

### `BindingWindow.xaml.cs`に関する既知の前提

`BindingWindow.xaml.cs`は、Phase5-Step14前クリーンアップ（PR-A）にて`Program.rootHub`の無条件直接代入
（`controlService`取得部分）を`AppHost.GetService<ControlService>() ?? Program.rootHub`形式へ**既に
統一済み**である。本Step10で扱う5件は、それとは別の`Global.*`メンバ参照（`ControlService`取得とは無関係の
設定値等）であることを確認した上で着手する。

---

## 1. 目的

`WelcomeDialog.xaml.cs`, `PresetOption.cs`, `SaveWhere.xaml.cs`, `BindingWindow.xaml.cs`内の
`Global.*`直接参照（Step1確定値、暫定29件）を、既存または軽微拡張したDIサービス経由の呼び出しへ置換する。

---

## 2. 各ファイルの性質と想定される移行先

| ファイル | 想定される参照内容 | 想定移行先 |
|---|---|---|
| `WelcomeDialog.xaml.cs` | 初回起動時のウェルカム画面。`firstRun`関連、言語/テーマ初期表示 | `IAppSettingsService`（`FirstRun`、Phase5-Step14前クリーンアップで既に追加済み）、`IAppearanceSettingsService` |
| `PresetOption.cs` | プリセット選択UIの補助クラス。プロファイル関連の定数・設定値 | `IProfileSettingsService`または`IProfileRepository` |
| `SaveWhere.xaml.cs` | 保存先選択ダイアログ。パス関連 | `IPathService` |
| `BindingWindow.xaml.cs` | ボタン割り当て画面。`ControlService`取得は解消済み（上記参照）、残りは設定値参照と推測 | `IProfileSettingsService`等 |

Step1監査で上記推測が正しいか確定させ、実際の移行先を確定する。

---

## 3. 作業方針

### 3.1 まとめて1PRで対応する方針の妥当性確認

Phase6-Plan.mdの想定通り、4ファイル合計29件・個別ファイルあたり平均7件程度と少数であるため、
Step1監査の結果、各ファイルの参照が全て分類(a)（単純リダイレクト）であることが確認できれば、
1PRでまとめて対応する。ただし、いずれかのファイルで分類(b)(c)に該当する項目が見つかった場合は、
該当ファイルのみ別PRに切り出す。

### 3.2 `PresetOption.cs`の性質確認

`PresetOption.cs`はUI要素そのもの（Window/UserControl）ではなく、ヘルパークラスである可能性がある。
全体計画書§4.5カテゴリDまたはEに該当する「データの入れ物」「動的ドメインオブジェクト」でないかを
Step1監査で確認し、該当する場合はカテゴリ除外対象として扱う（無理にDI化しない）。

---

## 4. PR粒度・実施順序

| PR | 内容 |
|---|---|
| PR-1（基本方針） | 4ファイル全ての分類(a)をまとめて1PRで対応 |
| PR-2（該当時のみ） | 分類(b)(c)に該当する項目がある場合、該当ファイルのみ個別対応 |

---

## 5. 完了判定基準

- [ ] 4ファイル内の`Global.`直接参照（Step1確定分）が0件になっていること。
- [ ] `PresetOption.cs`がUI要素かヘルパークラスかの性質確認が完了し、カテゴリ除外の要否が判断されていること。
- [ ] 初回起動時のウェルカム画面、プリセット選択、保存先選択、ボタン割り当て画面が、置換前と同等に動作すること。
- [ ] 既存自動テストが全件成功を維持していること。

---

## 6. リスクと対応

| リスク | 対応 |
|---|---|
| `WelcomeDialog.xaml.cs`は初回起動時のみ表示されるため、通常のテスト手順では見落とされやすい | Step11の実機検証項目に「設定ファイル削除後の初回起動確認」を明示的に含める（Phase5-Step14前クリーンアップでも同様の考慮を実施済み） |
| `PresetOption.cs`がカテゴリD/Eに該当し、実質的な対応不要と判明する可能性 | その場合は「対応不要」を正式な結論として記録し、無理に対応を作らない |
| `BindingWindow.xaml.cs`の残り5件が、Phase5-Step14前クリーンアップで解消済みの`Program.rootHub`関連と誤って重複対応される | 着手前に該当ファイルの現在のコードを確認し、`controlService`取得部分には触れないことを確認する |

---

## 7. 次のアクション

1. Phase6-Step1の完了後、4ファイル分の確定件数・分類・`PresetOption.cs`の性質を確認する。
2. 承認後、PR-1（4ファイル一括対応）から着手する。
3. 完了後、`Phase6-Status.md`のStep10欄を更新し、Step11（自動テスト・実機検証）の計画書作成へ進む。
