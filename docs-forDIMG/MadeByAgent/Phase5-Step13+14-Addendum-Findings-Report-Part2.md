# Phase 5 (Step13〜14) ドキュメント×実コード 突き合わせ監査レポート（Part 2）

作成日: 2026-09-09
対象: `Phase5-Step13+14-Addendum-Findings-Report.md`（Part 1）で「未確認」とした事項の実地調査
調査方法: `git clone --depth 1 --branch For-DI-migration-work` によるリポジトリ全体の取得と `grep` による全文検索
（GitHub Code Search はデフォルトブランチしか索引しないため、本調査ではローカルクローンして直接検索した）

---

## 1. 調査対象と結論サマリ

| # | Part1での指摘 | 結論 |
|---|---|---|
| 3.4 | `Global.ProfileListHolder` 静的メンバの有無 | **リポジトリ全体に存在しない**。Watchpoint3の前提が誤り |
| 3.2 | `Global.blMacro` / `CONFIG_VERSION` / `STEAM_APPROVED_APP_ID` / `FindValidProfile` / `ProfileExists` の実在性 | `CONFIG_VERSION`以外は**リポジトリ全体に一件も存在しない**（大文字小文字区別なしでも0件） |
| 3.1 | `Program.rootHub` 直接代入の範囲 | `MainWindow`以外に3ファイルで同型の無条件直接代入を確認。ただし機能的な二重実体化リスクはない |
| （新規） | Phase3積み残し「`ApplyProfileDirect`/`RestoreProfileDirect`のProgram.rootHub参照2箇所」の現状 | **解消済みを確認**。Phase3-Status.mdの残課題欄が更新されていない |

---

## 2. `Global.ProfileListHolder` 静的メンバの調査結果（Part1 §3.4 の解消）

```bash
grep -rn "ProfileListHolder" --include=*.cs .
```

結果、ヒットしたのは以下の2ファイル・3箇所のみ：

```
./DS4Windows/DS4Forms/MainWindow.xaml.cs:90:  public ProfileList ProfileListHolder { get => profileListHolder; }
./DS4Windows/DS4Forms/ProfileEditor.xaml.cs:1920: ProfileList profList = (Application.Current.MainWindow as MainWindow).ProfileListHolder;
./DS4Windows/DS4Forms/ProfileEditor.xaml.cs:1957: ProfileList profList = (Application.Current.MainWindow as MainWindow).ProfileListHolder;
```

`ScpUtil.cs`（`Global`クラスの実体）を含め、**`Global.ProfileListHolder`という静的メンバはコードベース中に一件も存在しない。**

### 2.1 評価

`Phase5-Watchpoints-Investigation-Report.md` §4.1 は「`Global.ProfileListHolder` は `MainWindow` 等で
まだ参照されている」「`IProfileRepository`側のコレクションと`ProfileListHolder`が二重管理されている
場合、画面上のリストと実際のデータに不一致が生じる恐れがある」としていたが、**この前提（静的ホルダーの存在）
自体が事実と異なる。**

実際に存在するのは、`MainWindow`が保持する**インスタンスフィールド**`profileListHolder`（`ProfileList`型）
であり、`ProfileEditor`（別ウィンドウ）はこれを`Application.Current.MainWindow as MainWindow`という
キャストを介して直接参照している。

これはWatchpoint3が想定した「Global静的ホルダー ⇄ DIサービスの二重実体化」とは**別種の問題**である：

- **旧Watchpoint3の想定リスク（該当なし）**: 静的な`Global.ProfileListHolder`とDIサービス
  `IProfileRepository`が同じデータを別々に保持し、状態が乖離する。→ 該当する静的メンバが存在しないため
  発生し得ない。
- **実際に存在する設計上の懸念（新規指摘）**: `ProfileEditor`が`Application.Current.MainWindow`という
  グローバルなView階層越しにMainWindowの内部状態（`ProfileListHolder`）へ直接アクセスしている。
  これはDIの観点では「View間の強結合」であり、`IProfileRepository`（DIサービス）を経由しないUIデータ
  アクセス経路が1本残っている、という意味では軽微な設計負債と言える。ただし「二重管理による不整合」の
  リスクではなく、`profileListHolder`は`MainWindow`側の単一実体をそのまま参照しているだけなので、
  **データの不整合は起きない**（参照の共有であり複製ではないため）。

### 2.2 推奨

- `Phase5-Watchpoints-Investigation-Report.md` §4.1 の表における「Profiles」行の記述
  （`Global.ProfileListHolder` が残存、として「要注意」評価）を、上記の実態に基づいて訂正する。
- Step15で対応するなら、対象は「静的シムの透過化」ではなく「`ProfileEditor`が`MainWindow`型へ直接
  キャストする代わりに、コンストラクタ引数または`IProfileRepository`経由でプロファイル一覧を受け取る」
  というリファクタリングになる（ただし優先度は低く、Phase5のスコープ外としても実害はない）。

---

## 3. Watchpoints報告書の具体例シンボルの実在性調査（Part1 §3.2 の追加検証）

```bash
grep -rin "blmacro" --include=*.cs .          # 0件
grep -rin "steam_approved\|steamapprovedapp" --include=*.cs .  # 0件
grep -rin "findvalidprofile" --include=*.cs . # 0件
grep -rn  "CONFIG_VERSION" --include=*.cs .   # Global.CONFIG_VERSION は存在するが、MainWindow等UI層からの参照はゼロ
```

`Global.blMacro`、`Global.STEAM_APPROVED_APP_ID`、`Global.FindValidProfile` は、大文字小文字を問わず
**コードベース全体を通じて一度も出現しない。** `Global.CONFIG_VERSION`は実在する定数（`ScpUtil.cs:1097`）
だが、参照元は`ScpUtil.cs`自身の2箇所のみで、`MainWindow.xaml.cs`等のUI層からは一切参照されていない。

### 3.1 評価

`Phase5-Watchpoints-Investigation-Report.md`のWatchpoint1・Watchpoint3で例示された固有シンボル名は、
**実際のリポジトリに存在した形跡がない。** 当該報告書の分類テーブル自体（"約7件"、"約4件"といった件数感）
は概ね実態と近いものの、**個別の例示コードは実地確認（grep等）に基づかず記述された可能性が高い**、
という所見をここに記録する。

### 3.2 推奨

- Step15の個別計画書（`Phase5-Step15-Plan.md`）を新規作成する際は、Watchpoints報告書の具体例をそのまま
  転記せず、Part1 §3.2に記載した実際の`Global.*`参照一覧（`LogMinLevel`, `exelocation`, `IsAdministrator`,
  `iconChoiceResources`, `UseCurrentTheme`, `runHotPlug`, `firstRun`, `exeversion`,
  `LastVersionCheckedNum`, `CheckUpdateStartupEnabled`/`CheckEveryValue`/`CheckEveryUnit`/`LastChecked`,
  `exedirpath`/`appDataPpath`, `TEST_PROFILE_INDEX`, `RefreshHidHideInfo`/`RefreshFakerInputInfo`,
  `RESOURCES_PREFIX`）を一次情報として使う。
- 今後、実装状況の記述にサンプルコードを引用する場合は、grep等で実在確認したうえで記載する運用ルールを
  ドキュメント作成ガイドラインに加えることを推奨する。

---

## 4. `Program.rootHub` 直接代入の全リポジトリ調査（Part1 §3.1 の深掘り）

`Program.rootHub`はリポジトリ全体で **20ファイル** から参照されている。パターン別に分類すると：

| パターン | 該当ファイル数 | 評価 |
|---|---|---|
| DIファクトリ登録・起動シーケンス（正規経路） | `App.xaml.cs`, `DI/ServiceRegistration.cs` | 問題なし。`ServiceRegistration.cs:82`で`return Program.rootHub ?? new ControlService(...)`となっており、`AppHost.GetService<ControlService>()`は`Program.rootHub`と同一インスタンスを返す |
| `?? Program.rootHub` フォールバック（受入れ済みパターン） | `AutoProfilesViewModel.cs`, `RecordBoxViewModel.cs`, `ProfileSettingsViewModel.cs`, `OutputSlotService.cs`, `PresetOption.cs`, `DS4Sixaxis.cs`, `Mapping.cs`(6471行目) | 過渡期の許容パターン（コンストラクタ引数優先、未指定時のみ`Program.rootHub`） |
| **無条件の直接代入（フォールバックなし）** | `MainWindow.xaml.cs:113`, `StickCalibrationWindow.xaml.cs:20`, `BindingWindow.xaml.cs:67`, `ProfileEditor.xaml.cs:279` | **4ファイルで同型のパターン**。他のDI注入済みサービスと統一感がない |
| 内部ロジックでの直接参照 | `ScpUtil.cs`（コントローラー設定保存/読込）, `ProfileSettingsService.cs`, `AutoProfileService.cs`, `ControllerReadingsControl.xaml.cs`, `RecordBox.xaml.cs` | `Program.rootHub`を「デバイスレジストリの実体」として直接読みに行くパターン。`IDs4DeviceRegistry`（Step11で境界化済み）の対象外の残存箇所とみられる |

### 4.1 Phase3積み残し事項の解消確認（新規のポジティブな発見）

`Phase3-Status.md` §2 には以下の残課題が記載されている：

> Mapping.cs内 ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub参照2箇所
> （ctrl=ControlServiceそのものをGlobal.ApplyProfile等へ渡す必要があるため、IDeviceStateAccessorだけでは
> 解消不可）｜フェーズ4（IProfileRepository導入時）に合わせて解消

実コードを確認したところ、該当する`Mapping.cs`の`ApplyProfileDirect`/`RestoreProfileDirect`は：

```csharp
internal static void ApplyProfileDirect(int device, SpecialAction action)
{
    profileApplication?.ApplyFromAction(device, action);
}

/// <summary>
/// DI/IProfileSwitcher 用の一時プロファイル復帰エントリーポイント
/// </summary>
internal static void RestoreProfileDirect(int device)
{
    profileApplication?.RestoreFromAction(device);
}
```

となっており、**`Program.rootHub`参照は完全に除去され、`IProfileApplicationService`（`profileApplication`）
へ委譲する形に置き換わっている。** これはPhase5-Status.mdのStep3「`DefaultProfileSwitcher`からの
`Program.rootHub`直参照完全排除」に対応する成果と考えられ、**Phase3の積み残し事項はPhase5で実質的に解消済み**
であることを確認した。

### 4.2 推奨

- `Phase3-Status.md` §2「既知の残課題」の該当行に、Phase5 Step3で解消済みである旨の注記を追加する
  （ドキュメントの整合性維持のため）。
- §4冒頭の「無条件の直接代入」4ファイルについては、機能的な不具合はないが、`MainWindow`と同様の
  パターンが3ファイルに複製されている状態である。Step15でまとめて`AppHost.GetService<ControlService>()`
  経由に統一するか、あるいは「サブウィンドウ生成時の`ControlService`取得は`Program.rootHub`直接参照を
  正式に許容する」という例外規定を`copilot-instructions.md`に明記するか、方針を決めることを推奨する。

---

## 5. 総合結論

- Part1で「ScpUtil.cs側の確認が必要」としていた`Global.ProfileListHolder`は、**実在しないことを確認**した
  （Watchpoint3の前提を修正する必要あり）。
- Watchpoints報告書がWatchpoint1/3で挙げた具体的なシンボル例（`blMacro`, `STEAM_APPROVED_APP_ID`,
  `FindValidProfile`等）も**実在しないことを確認**した。Step15計画はPart1・Part2で実地確認した一覧を
  一次情報として使うことを推奨する。
- 一方で、Phase3の積み残し事項（`Mapping.cs`の`Program.rootHub`参照2箇所）は**Phase5で既に解消済み**
  であることが確認できた。これはドキュメント更新漏れであり、実装遅延ではない。
- `Program.rootHub`の無条件直接代入は`MainWindow`含め4ファイルに存在するが、DIファクトリ自体が
  `Program.rootHub`と同一インスタンスを返す設計になっているため、**機能的な二重実体化リスクはない**。
  統一性の観点での軽微な指摘にとどまる。

以上より、Phase5コア実装（Step1〜13）の健全性という結論は変わらないが、**Step15着手前に
Watchpoints報告書とPhase3-Status.mdの記述精度を実地確認結果に基づいて更新しておく**ことを重ねて推奨する。