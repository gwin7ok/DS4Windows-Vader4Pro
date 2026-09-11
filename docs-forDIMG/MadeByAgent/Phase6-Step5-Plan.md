# フェーズ6 - ステップ5 個別計画書: OutputSlot / 横断設定の `Global` 直参照・孤立設定の是正

作成日: 2026-09-11
対象ブランチ: `For-DI-migration-work`
前フェーズ/前ステップ: Phase6-Step1（詳細監査、Phase5-Step14/15完了後に着手予定・本計画書作成時点では未実施）
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase6計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`（§1, §0.2, §2 Phase6-Step5）
移管元ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（§5, §6.2, §8.1, §8.2）
- `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-Fix-Plan.md`（タスク4）
- `.github/copilot-instructions.md`（§3.1 Pure DI原則、§6.11 実地確認の原則）

---

## 1. 位置づけ

Phase6-Plan.md §0.2・§1では、本ステップを「旧Phase5-Step14 タスク4」の移管として位置づけている。Phase5-Step14では、Issue 7（`Emulated Controller` コンボボックス誤表示）の真因調査の過程で、`IOutputSlotService.GetOutputDeviceType(int)` が `Global`/`BackingStore` と非連動の孤立配列 `_deviceTypes[]`（常に `OutContType.None`）を参照する「孤立バグ」であることが判明した（`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md` §5）。

この是正（`IProfileSettingsService` への `OutContType` プロパティ新設、および `ProfileSettingsViewModel.cs`・`SpecialActionsListViewModel.cs` の参照先修正）自体は `Phase5-Step14-Issue7-Fix-Plan.md` のタスク1〜3・6・7として「必須スコープ」に位置づけられている。**このうち、影響範囲が外部UDP連携という限定的な箇所であるため優先度が低いとして「任意スコープ」に分類されたタスク4（`MainWindow.xaml.cs` のUDP診断コマンド `outconttype` の修正）が、Phase6-Step5として本計画書の対象である。**

---

## 2. 実地確認結果（2026-09-11、`git clone --depth 1 --branch For-DI-migration-work` によるリポジトリ全文 `grep`）

着手前に、Phase6-Plan.md本文の記述が現在のコードと一致しているかを確認した（`copilot-instructions.md` の「ドキュメント記述は実装前に必ず実地確認を経ること」の原則を適用）。

### 2.1 Phase6-Plan.md記載の「OutputSlotViewModel」は実在しない

Phase6-Plan.md §2 Step5の記述は「`OutputSlotViewModel` や関連コンポーネントが保持する出力デバイス設定の参照先を…」「スロット設定画面等で直接 `Global.Instance.Config` を読み書きしている二重管理を根絶する」としているが、実地 `grep` の結果：

```
$ grep -rln "OutputSlotViewModel" DS4Windows/
（0件）
$ grep -rn "Global.Instance.Config" DS4Windows/
（0件）
```

`OutputSlotViewModel` という名前のクラスはリポジトリ中に存在せず、`Global.Instance.Config` という参照パターンも存在しない。この記述はPhase6-Plan.md策定時点での一般論的な想定であり、実装の裏付けを伴っていなかったと判断する。**本計画書ではこの記述を採用せず、実際に存在が確認された対象のみをスコープとする。**

### 2.2 実在する対象: `MainWindow.xaml.cs` のUDP診断コマンド

```
DS4Windows/DS4Forms/MainWindow.xaml.cs:1412:
    else if (propName == "outconttype")
        propValue = outputSlotService.GetOutputDeviceType(tdevice).ToString();
```

`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md` §6.2で報告された通り、外部プログラムがUDP経由で `query.<device>.outconttype` を問い合わせた場合、プロファイルの実際の設定に関わらず常に文字列 `"None"` が返却される孤立バグが現存することを確認した。

### 2.3 前提タスク（Fix-Planタスク1〜3）の未着手を確認

```
$ grep -n "OutContType" DS4Windows/DI/IProfileSettingsService.cs DS4Windows/DS4Control/Services/ProfileSettingsService.cs
（0件）
```

本ステップが参照先として使う予定の `IProfileSettingsService.OutContType`（`Phase5-Step14-Issue7-Fix-Plan.md` タスク1で新設予定）は、本計画書作成時点では**未実装**であることを確認した。Phase6-Status.mdの前提（Phase5-Step14完了後にPhase6着手）に照らすと、Phase6-Step5の実装着手時点ではタスク1〜3は完了済みである見込みだが、**万一未完了のままPhase6-Step5に着手する場合は、本計画書のタスク0として先に実施する**（§5参照）。

### 2.4 MainWindow.xaml.cs は既に `IProfileSettingsService` を保持済み

```
DS4Windows/DS4Forms/MainWindow.xaml.cs:74:
    private readonly DS4Windows.DI.IProfileSettingsService profileSettingsService;
DS4Windows/DS4Forms/MainWindow.xaml.cs:109:
    profileSettingsService = DS4WinWPF.AppHost.GetService<...IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
```

同ファイル内で `profileSettingsService` フィールドが既にPure DI／フォールバック方式で初期化済みであり、コンストラクタ変更や新規DI配線は不要である。1412行目の修正のみで完結する。

### 2.5 他の「横断設定」孤立バグは是正済み（再確認のみ）

`Phase5-Step14-FormSettings-Unification-Plan.md` フェーズAにより、`IEnvironmentService`／`IProfileSettingsService`（カラム幅5プロパティ）／`AppNotificationService` の孤立フィールドは既に是正済みであることを確認した（該当コードが現存しないことをgrepで確認）。本ステップではこれらの再修正は不要である。

---

## 3. スコープ確定

### 3.1 スコープに含むもの

1. `MainWindow.xaml.cs` 1412行目、UDP診断コマンド `outconttype` の参照先を `Global.OutContType[device]`（永続化実体）へ是正する（実装は `IProfileSettingsService.OutContType` 経由）。
2. §2.3の前提タスク（`IProfileSettingsService.OutContType` 新設）が未完了の場合、これを本ステップのタスク0として先に実施する。
3. `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType` を参照する箇所が、Phase5-Step14フェーズC完了後・Phase6-Step1監査後の時点で他に残存していないかの最終横断確認（§2.1の是正漏れ防止）。

### 3.2 スコープに含まないもの

1. `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` 自体の実装変更・削除・`[Obsolete]`化（Phase6-Step6のスコープ）。
2. `ProfileEditor.xaml.cs` の `Reload()`/`RefreshEditorBindings()` におけるマッピング一覧の機種追従修正（Phase6-Step6のスコープ）。
3. ViGEm出力バックエンドの抽象化（全体計画書§4.5により恒久的に対象外＝カテゴリE隣接の別イニシアチブ）。

---

## 4. 修正対象ファイル棚卸し

| # | ファイル | 変更内容 | 種別 |
|---|---|---|---|
| 1 | `DS4Windows/DI/IProfileSettingsService.cs` | （タスク0・条件付き）`OutContType[] OutContType { get; }` の宣言追加 | インターフェース追加（前提未完了時のみ） |
| 2 | `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | （タスク0・条件付き）`public OutContType[] OutContType => _config.outputDevType;` の実装追加 | サービス実装追加（前提未完了時のみ） |
| 3 | `DS4Windows/DS4Forms/MainWindow.xaml.cs` | 1412行目、`outputSlotService.GetOutputDeviceType(tdevice)` → `profileSettingsService.OutContType[tdevice]` に置換 | 参照先修正 |
| 4 | `DS4WindowsTests/ProfileSettingsServiceTests.cs`（存在すれば） | （タスク0・条件付き）新規プロパティの単体テスト追加 | テスト追加（前提未完了時のみ） |

> **No Feature Drop方針**: 本ステップの変更は「読み取り参照先の付け替え」のみであり、`BackingStore`（`m_Config`）の実データ格納先・XML構造・保存ロジックには一切手を加えない。

---

## 5. マイクロタスク breakdown

### タスク0（条件付き・実施要否は着手時に確認）: `IProfileSettingsService.OutContType` の新設

- [ ] Phase6-Step5着手時点で `IProfileSettingsService.OutContType` が未実装の場合のみ実施する。
- [ ] `IProfileSettingsService.cs` に以下を追加:
  ```csharp
  /// <summary>
  /// プロファイルに永続化されているエミュレートコントローラー種別（&lt;OutputContDevice&gt;）。
  /// Global.OutContType（m_Config.outputDevType）への読み取り専用の薄い委譲。
  /// </summary>
  OutContType[] OutContType { get; }
  ```
- [ ] `ProfileSettingsService.cs` に以下を追加:
  ```csharp
  public OutContType[] OutContType => _config.outputDevType;
  ```
- [ ] 既に `Phase5-Step14-Issue7-Fix-Plan.md` タスク1〜3として実装済みの場合は、本タスクをスキップし、その旨をStatus文書に記録する。

### タスク1: `MainWindow.xaml.cs` のUDP診断コマンド修正

- [ ] 1412行目を以下のように修正する:
  ```diff
  -    else if (propName == "outconttype")
  -        propValue = outputSlotService.GetOutputDeviceType(tdevice).ToString();
  +    else if (propName == "outconttype")
  +        propValue = profileSettingsService.OutContType[tdevice].ToString();
  ```
- [ ] `outputSlotService` フィールドが本箇所以外（`activeoutdevtype` 分岐等）で引き続き使用されているため、フィールド自体は削除しない。

### タスク2: 残存参照の最終横断確認

- [ ] `grep -rn "outputSlotService.GetOutputDeviceType\|outputSlotService.SetOutputDeviceType" DS4Windows/` を実行し、本ステップ完了時点で `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType` の呼び出し元がアプリ本体コード中に0件（テストファイルを除く）であることを確認する。
- [ ] 0件でなかった場合、Phase6-Step1の監査対象に追加漏れがなかったかを確認し、必要に応じて本ステップまたは後続ステップへの追加を提案する。

### タスク3: 単体テスト・ビルド確認

- [ ] `dotnet build` によるクリーンビルド確認（警告0・エラー0）。
- [ ] `dotnet test` による全件PASS確認。
- [ ] （任意）UDP `query.<device>.outconttype` コマンドの実機／統合テストでの動作確認（外部連携機能のため必須ではない）。

---

## 6. リスクと回避策

| リスク | 回避策 |
|---|---|
| タスク0の要否判定を誤り、`IProfileSettingsService.OutContType` を重複定義してしまう | 着手直前に `grep -n "OutContType" IProfileSettingsService.cs ProfileSettingsService.cs` を再実行し、存在確認をしてから着手する |
| `outputSlotService` フィールドが本ステップの修正により未使用になり、コンパイル警告が発生する | `activeoutdevtype` 分岐で引き続き使用されていることを確認済みのため、フィールド自体の削除は行わない |
| UDP外部連携の仕様変更と誤認される | 戻り値の型・意味（`OutContType` の文字列表現）は変更せず、参照元（常に`None`だった孤立実体）を正しい実体に差し替えるのみであることをコミットメッセージに明記する |

---

## 7. 完了条件

1. `MainWindow.xaml.cs` の UDP診断コマンド `outconttype` が、`Global.OutContType[device]`（プロファイルの永続化実体）と連動した正しい値を返すこと。
2. `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType` の呼び出し元がアプリ本体コード中（テストを除く）に0件であることを確認済みであること。
3. 全自動テストがクリーンにPASSし、ビルド警告・エラーが増加していないこと。
4. タスク0を実施した場合、`Phase5-Step14-Issue7-Fix-Plan.md` タスク1〜3の完了記録と重複しないよう、実施経緯をStatus文書に記録すること。

---

## 8. 進行ルール

- 本計画書の内容について、着手前にgwin7ok氏の確認・承認を得る。
- 巨大ファイル編集時はピンポイント編集ルールを徹底する。
- 実装完了後、`Phase6-Status.md` のStep5欄を更新する。
- 成果物は `present_files` による直接ダウンロード形式で提供する（PowerShellスクリプト形式は使用しない）。

---

## 9. 次のアクション

1. gwin7ok氏より本計画書の承認を得る。
2. タスク0の要否をPhase5-Step14の完了状況（`Phase5-Step14-Issue7-Fix-Plan.md` タスク1〜3の実施有無）で確定する。
3. タスク1〜3を実施し、`dotnet build`／`dotnet test` の結果を確認のうえ完了報告書を作成する。