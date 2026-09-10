# Phase6-Step6 計画書: `ProfileEditor.xaml.cs` のGlobal直参照解消

作成日: 2026-09-09
対象ブランチ: `For-DI-migration-work`
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`
着手前提: Phase6-Step1（詳細監査と対象確定）の完了、および`Phase6-Step1-Global-Usage-Classification-Report.md`の承認

---

## 0. 前提の明記

本計画書は、Phase6-Plan.md §0.2記載の暫定値（`ProfileEditor.xaml.cs`内18件）を基に、**作業方針・
既存パターンの再利用方針を先行して定義**するものである。対象の確定件数・個別のメンバ名・移行先サービス名は、
Step1の成果物を待って本計画書に反映する。

`ProfileEditor.xaml.cs`は、Phase5-Step13（`MainWindow.xaml.cs`のUI層DI接続）完了時点で**対象漏れに
なっていたファイル**である（Phase5-Step13+14-Addendum-Findings-Report、2026-09-09実地調査で判明）。
Step6は、Phase5-Step13および同Step14前クリーンアップで確立された置換パターンを、本ファイルへ
横展開する作業と位置づけられる。

---

## 1. 目的

`ProfileEditor.xaml.cs`内の`Global.*`直接参照（Step1確定値、暫定18件）を、既存のDIサービス経由の
呼び出しへ置換し、UI層（プロファイル編集画面）のGlobal依存を解消する。

---

## 2. 作業方針

### 2.1 既存パターンの踏襲（新規設計を避ける）

`MainWindow.xaml.cs`は既にPhase5-Step13およびStep14前クリーンアップ（PR-A〜E）で同種の作業を完了している。
`ProfileEditor.xaml.cs`の18件は、性質上`MainWindow.xaml.cs`で既に解消済みの参照と重複・類似している
可能性が高い（例: パス関連、環境情報、アプリ設定関連）。Step6では新規設計を最小限にとどめ、以下の
既存パターンをそのまま適用することを基本方針とする。

```csharp
// 既存パターン（MainWindow.xaml.cs, Phase5-Step13/Step14前クリーンアップで確立）
private readonly DS4Windows.DI.IPathService pathService;
// ...
pathService = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IPathService>() ?? Global.PathServiceInstance;
```

`ProfileEditor.xaml.cs`にコンストラクタでDIサービスを受け取るフィールドが未整備の場合は、上記と同一の
命名・フォールバックパターンで新設する。

### 2.2 `ProfileListHolder`への参照に関する特記事項

Phase5-Step13+14-Addendum-Findings-Report-Part2（2026-09-09）の調査により、`ProfileEditor.xaml.cs`は
`(Application.Current.MainWindow as MainWindow).ProfileListHolder`という形で`MainWindow`のインスタンス
プロパティへ直接アクセスしていることが判明している。これは`Global.*`静的参照ではなくView間の直接結合
であり、**本Stepのスコープ（Global直接参照の解消）には含まれない**。当該箇所の解消要否は、Step6の
成果物内で「別種の技術的負債」として記録するにとどめ、対応するかどうかはPhase6完了後の判断とする。

### 2.3 分類ごとの対応方針

Step2〜5と同様、既存DIサービスへの単純リダイレクト(a)を優先し、軽微拡張(b)、新規設計(c)の順で
対応する。`MainWindow.xaml.cs`側で既に同名メンバがDIサービスに存在する場合は、(a)に分類される
可能性が高い。

---

## 3. PR粒度・実施順序

| PR | 内容 |
|---|---|
| PR-1 | `MainWindow.xaml.cs`で既に解消済みのメンバと重複するもの（分類(a)、最低リスク） |
| PR-2 | `ProfileEditor.xaml.cs`固有の分類(a)(b) |
| PR-3（該当時のみ） | 分類(c)（新規インターフェース設計・実装・配線） |

---

## 4. 完了判定基準

- [ ] `ProfileEditor.xaml.cs`内の`Global.`直接参照（Step1確定分）が0件になっていること。
- [ ] 新設したフィールド・初期化コードが、`MainWindow.xaml.cs`の既存パターンと命名・構造ともに一貫していること。
- [ ] プロファイル編集画面（新規作成・複製・削除・各種設定タブ）の操作が、置換前と同等に動作すること。
- [ ] `ProfileListHolder`へのView間直接参照について、対応要否の判断が記録されていること（対応自体は任意）。
- [ ] 既存自動テストが全件成功を維持していること。

---

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| `ProfileEditor.xaml.cs`はUIタブ数が多く、影響範囲の見落としが発生しやすい | Step1監査で全18件の行番号・呼び出し箇所を事前に一覧化し、タブ単位で網羅的に確認する |
| `MainWindow.xaml.cs`との重複パターンに気づかず、独自の異なる実装をしてしまう | PR-1で明示的に「MainWindow.xaml.cs重複分」を切り出し、実装を先に確認してからコードを揃える |
| `ProfileListHolder`のView間結合に誤って手を出し、スコープが拡大する | §2.2の通り本Stepのスコープ外と明記し、着手しない |

---

## 6. 次のアクション

1. Phase6-Step1の完了後、`ProfileEditor.xaml.cs`分の確定件数・分類・`MainWindow.xaml.cs`との重複有無を確認する。
2. 承認後、PR-1（`MainWindow.xaml.cs`重複分）から着手する。
3. 完了後、`Phase6-Status.md`のStep6欄を更新し、Step7（`SettingsViewModel.cs`他 主要ViewModel群の解消）の
   計画書作成へ進む。