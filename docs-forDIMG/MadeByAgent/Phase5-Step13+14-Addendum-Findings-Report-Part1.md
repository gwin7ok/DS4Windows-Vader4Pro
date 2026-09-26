# Phase 5 (Step13〜14) ドキュメント×実コード 突き合わせ監査レポート

作成日: 2026-09-09
対象ブランチ: `For-DI-migration-work`
調査方法: GitHub上の実ファイル（`MainWindow.xaml.cs`, `IProfileRepository.cs`, `copilot-instructions.md` 等）と、
`docs-forDIMG/MadeByAgent/` 配下の各種計画書・完了報告書・Phase5-Status.md・Phase5-Watchpoints-Investigation-Report.md を突き合わせ。

---

## 1. 前提確認

- `Phase5-Step14-RealDevice-Verification-Checklist.md` は全項目 `[ ]`（未実施）、§13実施記録も「未実施」。
  → ユーザー認識（Step14実機テストチェックリスト作成済み・実施前）と一致。

---

## 2. ドキュメント記述と実コードが一致していた点（確認済み）

| 項目 | 確認内容 | 結果 |
|---|---|---|
| `App.rootHub` 完全削除（Step13-8） | `MainWindow.xaml.cs` 全文精査 | 参照ゼロ。記述と一致 |
| `AutoProfileChecker` 二重実体化是正（Step13-9） | コンストラクタで `_autoProfileService` を注入・直結 | `[Phase 5 Step 13-9]` コメント付きで実装確認 |
| Watchpoint2（Dispose） | `MainDS4Window_Closed` | `conLvViewModel?.Dispose(); settingsWrapVM?.Dispose();` を確認。Watchpoints報告書を参照するコメントあり |
| Watchpoint2（Dispatcher委譲） | 各イベントハンドラ | `Dispatcher.BeginInvoke` によるUIスレッド委譲パターンを随所で確認 |

---

## 3. 実コードとドキュメントに齟齬・要確認がある点

### 3.1 `Program.rootHub` の直接参照残存

```csharp
controlService = Program.rootHub;
```

`MainWindow` コンストラクタ内でこの1行のみ、他サービス（`profileRepo`, `pathService` 等）と異なり
`AppHost.GetService<ControlService>()` を経由していない。

Step13完了報告は「`App.rootHub`／`Program.rootHub`／`Global` 参照 約70箇所中50箇所を置換、残り約20箇所は
UI外観ヘルパー・定数として意図的残存」としているが、この`Program.rootHub`直接代入がその「意図的残存」に
含まれるのか、単なる見落としなのかが文書上判別できない。

**推奨**: 意図的な残存であれば計画書にその理由（`ControlService`は具象シングルトンのまま、が§5.3の方針）を
明記し、そうでなければ他サービスと同様に `AppHost.GetService<ControlService>()` 経由に統一する。

### 3.2 Watchpoints報告書のMainWindow残存例と実コードの不一致

`Phase5-Watchpoints-Investigation-Report.md` §2.1 の分類表は、MainWindow残存例として
`Global.blMacro`, `Global.CONFIG_VERSION`, `Global.STEAM_APPROVED_APP_ID`,
`Global.FindValidProfile(name)`, `Global.ProfileExists(name)` を挙げているが、
**現在の `MainWindow.xaml.cs` にこれらの呼び出しは一件も存在しない。**

実際に残存しているのは以下のような約20シンボル・30箇所超（`?? Global.XxxInstance` フォールバック4件を除く）：

- `Global.LogMinLevel`
- `Global.exelocation`
- `Global.IsAdministrator()`
- `Global.iconChoiceResources` / `Global.UseIconChoice`
- `Global.UseCurrentTheme`
- `Global.runHotPlug`
- `Global.firstRun`
- `Global.exeversion`
- `Global.LastVersionCheckedNum`
- `Global.CheckUpdateStartupEnabled` / `CheckEveryValue` / `CheckEveryUnit` / `LastChecked`
- `Global.exedirpath` / `Global.appDataPpath`
- `Global.TEST_PROFILE_INDEX`（×3）
- `Global.RefreshHidHideInfo()` / `Global.RefreshFakerInputInfo()`
- `Global.RESOURCES_PREFIX`（`ImageLocationPaths` 内、×6）

件数感（約20件）はおおむね合致するが、**中身が別物**。Step15の個別計画がWatchpoints報告書の具体例を
そのまま前提にすると、対象取り違えのリスクがある。

**推奨**: Step15着手前に、現在の `MainWindow.xaml.cs` から `Global.` 参照を再度洗い出し、
分類（定数／UI外観／業務ロジック）をやり直す。

### 3.3 `IProfileRepository.ProfileExists(string)` は既に実装済み

Watchpoints報告書は「`IProfileRepository` にプロファイル存在判定メソッドが必要」としているが、
実際には既に以下が存在する（`DS4Windows/DI/IProfileRepository.cs`、Step13-2時点で追加と推測）：

```csharp
bool ProfileExists(string profileName);
```

一方、`MainWindow.xaml.cs` 内にはプロファイル名重複チェックの呼び出しが見当たらない
（`DupProfBtn_Click` 等は `dupBox` コントロールに処理を委譲している）。

**推奨**: 実際の重複チェック処理がどこにあるか（`ProfileEditor.xaml.cs` / `DupBox` 等）を確認し、
Watchpoint1の前提を更新する。

### 3.4 `ProfileListHolder` の実体を要再確認

`MainWindow.xaml.cs` の `ProfileListHolder` は：

```csharp
private ProfileList profileListHolder = new ProfileList();
public ProfileList ProfileListHolder { get => profileListHolder; }
```

という**インスタンスフィールド**であり、Watchpoint3が懸念する「`Global.ProfileListHolder` という
静的ホルダーとDIサービスの二重管理」とは別物に見える。

`Global.ProfileListHolder` という静的メンバが `ScpUtil.cs` 側に別途存在し、二重管理状態にあるのか、
既に解消済みで報告書の記述が古いだけなのかは、`ScpUtil.cs`（11,000行超）側の確認が必要
（今回の監査では分量の都合で未確認）。

**推奨**: Step15着手時に `ScpUtil.cs` 内 `Global.ProfileListHolder` の有無・参照箇所を確認し、
Watchpoint3の評価表を更新する。

### 3.5 Pure DI原則とMainWindowのService Locator利用

`copilot-instructions.md` §3.1：

> Pure DI（純粋コンストラクタ注入）の堅持: ViewModelやサービスに `AppHost.Services`
> （Service Locator）を持ち込ませず、コンストラクタで明示的に依存を受け取る。

一方 `MainWindow` コンストラクタ内では `AppHost.GetService<T>()` が多数呼ばれている
（`profileSettingsService`, `appSettingsService`, `pathService`, `profileAppService`,
`outputSlotService`, `profileRepo`, `mainWinVM` 等）。

MainWindowは厳密には「View」であり、コンポジションルート直下の許容範囲という解釈は可能だが、
原則文にその例外が明文化されていない。

**推奨**: 現状のパターンを追認するのであれば、`copilot-instructions.md` §3.1 に
「View（`MainWindow.xaml.cs`等のComposition Root隣接コード）はDI解決の起点として
`AppHost.GetService` の利用を許容する」旨を明記し、原則と実態の乖離を解消する。

---

## 4. Step15着手前の推奨アクション（まとめ）

1. `Program.rootHub` 直接代入（§3.1）が意図的残存か見落としかを明確化し、理由を計画書に明記する。
2. Watchpoints報告書のMainWindow残存項目リスト（§3.2）を現在のソースに基づき再棚卸しする。
3. `IProfileRepository.ProfileExists` の既存実装を踏まえ、Watchpoint1の前提・タスクを更新する（§3.3）。
4. `ScpUtil.cs` 内 `Global.ProfileListHolder` 静的メンバの有無を確認し、Watchpoint3を更新する（§3.4）。
5. MainWindowでのService Locator利用について、`copilot-instructions.md` に例外規定を追記するか検討する（§3.5）。

いずれも「実装のやり直し」を要するものではなく、**Step15の個別計画書を書き起こす前段階での
前提再確認**として位置づけられる軽微〜中程度の指摘である。Phase5 Step1〜13のコア実装
（プロファイル・アクション・デバイス・UI DI化）自体は、ドキュメント記述と実コードの間に
重大な齟齬は見つからなかった。