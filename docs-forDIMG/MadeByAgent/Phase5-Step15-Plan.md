# Phase 5 - Step 15 個別計画書：Legacy shim 棚卸し・削除判断

作成日: 2026-09-18
対象ブランチ: `For-DI-migration-work`
承認前提: 本計画書の内容確認・承認後に実装に着手する（copilot-instructions §4 マイクロステップの原則）
前提となる調査: Step15-1 詳細監査（本計画書内§1に集約）

---

## 1. Step15-1 監査結果のまとめ

Step13完了報告書が引き継いだ3項目について、実コードを調査した。

### ①`FindValidProfile`相当（プロファイル存在確認ロジック）
→ **対応不要（解消済み）**。該当機能は`MainWindow.xaml.cs`1341行（UDPコマンド経由のプロファイル読込）にあり、既に`pathService.AppDataPath`（`IPathService`、DI）を使用している。`Global`への直接参照は残っていない。

### ②`ProfileListHolder`（`ProfileList`クラス）
→ `MainWindow`・`ProfileEditor`・`ControllerListViewModel`・`TrayIconViewModel`・`AutoProfileControls`から実際に使われている、UI層に必要な`ObservableCollection`ラッパーであり、クラス自体の`[Obsolete]`化・削除は不適切と判断した。唯一の問題は`ProfileList.Refresh()`内部の`DS4Windows.Global.appdatapath`直接参照であり、同種の参照が`AutoProfiles.xaml.cs`にも1箇所ある。

### ③呼出元0件シムの物理削除
→ `Global`全体（約500メンバー）の網羅監査はPhase6-Step1の管轄と重複するため、Step15は**UI層でStep13の対象から漏れた`Program.rootHub`直接参照**に絞って調査した。UI層に9ファイル・19箇所が残存しているが、詳細確認の結果、内訳は以下の通り明確に二分できた。

| 分類 | 該当箇所 | 判断 |
|---|---|---|
| **意図的な安全策（対応不要）** | `BindingWindow.xaml.cs`, `MainWindow.xaml.cs`, `ProfileEditor.xaml.cs`(281行), `StickCalibrationWindow.xaml.cs`, `AutoProfilesViewModel.cs`, `ProfileSettingsViewModel.cs`, `RecordBoxViewModel.cs`（計7箇所） | いずれも`DS4WinWPF.AppHost.GetService<ControlService>() ?? Program.rootHub`という、本プロジェクト全体で確立済みのDIフォールバックパターン（初期化時1箇所のみ）。他の`Global.XxxServiceInstance`と同じ設計であり、意図的な残存として問題ない。 |
| **単純な移行漏れ（要対応）** | `ControllerReadingsControl.xaml.cs`（9箇所）、`RecordBox.xaml.cs`（2箇所） | いずれも`controlService`フィールド自体が存在せず、クラス内で`Program.rootHub`を直接使い続けている。Step13のUI層DI化から単純に漏れていたと判断される。 |
| **フィールドがあるのに未使用（要対応）** | `ProfileEditor.xaml.cs`（1984行） | 同ファイル281行で`controlService`フィールドを既にDIフォールバックパターンで解決済みであるにもかかわらず、1984行だけ`Program.rootHub.GetActiveInputControl(...)`を直接呼んでいる。単純な置換漏れ。 |

---

## 2. 対応方針

### 2.1 `ProfileList.Refresh()` / `AutoProfiles.xaml.cs`のGlobal参照置換
`ProfileList`クラスに`IPathService`をコンストラクタ経由で受け取らせ（`?? Global.PathServiceInstance`のフォールバック付き、既存の他クラスと同じパターン）、`Refresh()`内の`DS4Windows.Global.appdatapath`を`pathService.AppDataPath`に置き換える。`AutoProfiles.xaml.cs`も同様に修正する。

### 2.2 `ControllerReadingsControl.xaml.cs` / `RecordBox.xaml.cs`へのDIフォールバック導入
両ファイルに、他の多くのView/ViewModelと同じ形の`controlService`フィールドを追加する。

```
controlService = DS4WinWPF.AppHost.GetService<DS4Windows.ControlService>() ?? DS4Windows.Program.rootHub;
```

追加後、ファイル内の`Program.rootHub.X`という直接呼び出し（`ControllerReadingsControl.xaml.cs`9箇所、`RecordBox.xaml.cs`2箇所）を、すべて`controlService.X`に置き換える。動作は完全に同一（フォールバック先が同じ`Program.rootHub`のため）で、コンパイル後の実行時の挙動に変化はない。

### 2.3 `ProfileEditor.xaml.cs`の置換漏れ修正
1984行の`Program.rootHub.GetActiveInputControl(tempDeviceNum)`を、既に281行で解決済みの`controlService.GetActiveInputControl(tempDeviceNum)`に置き換える。1行のみの変更。

---

## 3. マイクロステップ分割（PR粒度案）

1. **Step15-2-a**: `ProfileList.cs` / `AutoProfiles.xaml.cs`のGlobal参照置換（§2.1）。
2. **Step15-2-b**: `ControllerReadingsControl.xaml.cs`へのDIフォールバック導入と置換（§2.2前半）。
3. **Step15-2-c**: `RecordBox.xaml.cs`へのDIフォールバック導入と置換（§2.2後半）。
4. **Step15-2-d**: `ProfileEditor.xaml.cs`の1行置換（§2.3）。
5. **Step15-3**: 上記すべて完了後、`dotnet build`/`dotnet test`のグリーン確認、および対象4画面（プロファイル一覧・コントローラー状態表示・マクロ記録・プロファイル編集の入力状態表示）の実機での簡易動作確認。
6. **Step15-4**: 完了報告書（`Phase5-Step15-Completion-Report.md`）を作成し、`Phase5-Status.md`を更新してPhase5を正式にクローズする。

各ステップは変更が1〜2ファイルに閉じており、既存のDIフォールバックパターンをそのまま複製するだけの機械的な置換のため、個々のリスクは低い。

---

## 4. 完了判定基準

- `ProfileList.cs`・`AutoProfiles.xaml.cs`から`DS4Windows.Global.appdatapath`への直接参照が0件になっていること。
- `ControllerReadingsControl.xaml.cs`・`RecordBox.xaml.cs`・`ProfileEditor.xaml.cs`の`Program.rootHub`直接呼び出しが、意図的なDIフォールバック初期化行（`?? Program.rootHub`）を除いて0件になっていること。
- 全自動テストが成功していること。
- 対象4画面の簡易実機確認で回帰がないこと。
- `Phase5-Status.md`がStep15完了として更新され、Phase5全体が正式にクローズされていること。

---

## 5. リスクと回避策

- **`ProfileList`のコンストラクタ変更による既存呼び出し元への影響**: `MainWindow.xaml.cs`・`ProfileEditor.xaml.cs`の2箇所が`new ProfileList()`を呼んでいる可能性があるため、コンストラクタに`IPathService`引数を追加する際はデフォルト値（`IPathService pathService = null`）を設定し、既存呼び出し元の変更を不要にする。
- **`ControllerReadingsControl` / `RecordBox`の`controlService`フィールド名の衝突**: 導入前に、両ファイルに同名の別フィールド・プロパティが既に存在しないことを確認する（Step15-1監査では未検出だが、実装直前に再確認する）。
- **意図的残存（DIフォールバックパターンの7箇所）には一切手を加えない**: これらはStep13時点で既に正しい設計として確立されているため、Step15の対象外として明確に区別する。

---

## 6. 本計画書のスコープ外

- `Global`全体（約500メンバー）を対象とした網羅的な呼出元0件シムの棚卸しは、Phase6-Step1の監査ツールが既に着手しているため、Step15では扱わない。
- `ControlService.cs`等コア層に残る`Global`直接参照の解体は、Phase6の正式なスコープであり、Step15（UI層クリーンアップ）には含めない。