# フェーズ5-Step4 計画書: Save／Apply の操作結果と通知の統一

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step4（Phase5詳細計画書）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（`IProfileXmlStore`によるXML I/O分離。本Stepで戻り値仕様を補正）
- `docs-forDIMG/MadeByAgent/Phase5-Step3-Plan.md`（`IProfileApplicationService.ApplyProfile`統一。本Stepはその戻り値・通知の扱いを対象とする）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.SaveProfile`等のシグネチャは、既存呼び出し元（75ファイル中の該当箇所）を壊さない範囲でのみ変更する。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 既存のログ出力、`ProfileChangedNotification`設定によるUI通知の抑制挙動を100%維持したまま、抜け漏れ（後述0.3）のみを是正する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存の`AppLogger.LogToGui`／`AppLogger.LogDebug`／`AppLogger.LogTrace`／`AppLogger.LogProfileChanged`をすべて維持し、削除・置換しない。新設する`[DI]`ログは追加であり、既存ログの代替ではない。
- **§3.1 DI (Dependency Injection) の実装**:
  - 新設・変更するメソッドはすべて`ServiceRegistration.cs`に登録済みの既存インターフェース（`IProfileRepository`／`IProfileApplicationService`／`IProfileXmlStore`）の範囲内で行う。

---

## 0. Step4の位置づけと現状分析

### 0.1 Step1監査結果・Step2/Step3計画書に基づく対象範囲
`Phase5-Plan.md` Step4は「保存成否と再適用成否を戻り値で扱い、操作単位の`[DI]`ログを追加する」「`ProfileChanged`通知が通常切替と同じ経路で発生することを確認する」ことを目的とする。本Stepは、Step2で設計した`IProfileXmlStore`／`IProfileRepository`の保存処理と、Step3で設計した`IProfileApplicationService.ApplyProfile`／`DefaultProfileSwitcher`の適用処理を対象に、**戻り値の伝播漏れと通知抑制の不整合**を是正する。

### 0.2 現状のコード構造（GitHub実コード確認済み）で判明した2つの具体的な欠落

**(A) 保存成否が戻り値として伝播していない**
`BackingStore.SaveProfile(int device, string proName)` は `bool Saved` を返す実装だが、`Global.SaveProfile(int device, string proName)` は次の通り戻り値を破棄している。

```csharp
public static void SaveProfile(int device, string proName)
{
    m_Config.SaveProfile(device, proName);
}
```

呼び出し元（`ProfileRepository.SaveProfile`を含む）はこの`void`シグネチャを通じて保存失敗を一切検知できない。C#では`bool`を返すメソッドを式文として呼び出しても既存呼び出し元は壊れないため、`void`→`bool`への変更は非破壊的に行える。

**(B) SpecialAction経由の切替で通知抑制設定が無視されている**
`ProfileApplicationService.ApplyFromAction`は`_profileSettings.ProfileChangedNotification`を読み取り、`Global.ApplyProfile`の`displayNotification`引数に正しく渡している。

```csharp
bool display = _profileSettings.ProfileChangedNotification;
...
Global.ApplyProfile(deviceIndex, action.details, action.IsTemporaryProfileAction, true, _control,
    ProfileChangeSource.MappingAction, prolog, display);
```

一方、`DefaultProfileSwitcher.SwitchProfile`は`prolog`／`displayNotification`引数を省略して呼び出しており、`Global.ApplyProfile`の既定値（`prolog = null`、`displayNotification = true`）が常に適用される。

```csharp
Global.ApplyProfile(deviceIndex, targetProfile, isTemporaryProfile, false,
    Program.rootHub, ProfileChangeSource.MappingAction);
```

**この結果、SpecialActionの切替経路によって「ユーザーが通知をオフにしていても、`DefaultProfileSwitcher`経由の切替では常に通知が表示される」という不整合が生じている。** これはPhase5-Plan.mdが目標とする「通常切替とSave／Applyの通知経路の統一」に反する具体的な不具合であり、本Stepの主要な是正対象とする。

### 0.3 `CompleteProfileApplication`内のActions基盤静的委譲との関係（参考情報）
`Global.CompleteProfileApplication`は、プロファイル適用完了時に`DS4Windows.ActionManager.ClearAllEntries()`／`Mapping.ClearKeyButtonControllersForDevice`／`ActionManager.ClearDeviceState`／`ActionManager.PreallocateForProfileApply`を呼び出している。これはStep8（Actions基盤の静的委譲分離）の対象範囲と重なるが、**本Stepでは戻り値・通知の整流化のみを扱い、`ActionManager`静的委譲自体には手を入れない**（Step8のスコープを侵さないようにする）。

### 0.4 全体4層モデルにおける位置づけ
本Stepは第4層 4-cに属する`ProfileRepository`／`ProfileApplicationService`／`DefaultProfileSwitcher`の**戻り値契約とログ・通知の整合性**を扱う横断的な整流化Stepであり、新規サービスの追加は行わない。

---

## 1. 設計方針とアーキテクチャ

### 1.1 保存系の戻り値伝播（Step2成果物の補正を含む）
- `Global.SaveProfile`: `void` → `bool`（`m_Config.SaveProfile`の戻り値をそのまま返す）。
- `IProfileXmlStore.SaveProfileXml`（Step2設計）: `void` → `bool` に修正する。Step2が未実装の場合はStep2実装時点で本仕様を反映し、実装済みの場合は本Stepでピンポイント修正する。
- `IProfileRepository.SaveProfile`: 戻り値を`bool`にし、`IProfileXmlStore.SaveProfileXml`の結果をそのまま返す。

### 1.2 適用系の戻り値・ログの整流化
- `IProfileApplicationService.ApplyProfile`（Step3設計）は既に`bool`を返す設計になっているため、型定義自体の変更は不要。呼び出し元（`DefaultProfileSwitcher`の`SwitchProfile`／`RestoreProfile`／`ApplyManualProfile`）が戻り値を握りつぶさず、失敗時に`[DI]`ログを出すようにする。
- `ApplyFromAction`（`void`のまま。`Task.Run`内で非同期実行されるため戻り値を同期的に返せない既存設計を維持）は、`Task.Run`内部での失敗を`[DI]`ログとして記録する形で可視化する（戻り値の型変更はしない。既存の非同期実行構造を崩さない）。

### 1.3 通知抑制の統一（0.2(B)の是正）
`DefaultProfileSwitcher.SwitchProfile`が`IProfileApplicationService.ApplyProfile`（Step3で追加）経由に置き換わっていることを前提に、`SwitchProfile`内で明示的に`_profileSettings.ProfileChangedNotification`（またはコンストラクタ注入された`IProfileSettingsService`）を読み取り、`ApplyProfile`の`displayNotification`引数に渡す。Step3の実装が本Stepより先行する場合はStep3側で対応済みとし、本Stepでは動作検証のみを行う。Step3が未実装の場合は本Stepで`DefaultProfileSwitcher`のコンストラクタに`IProfileSettingsService`を追加注入し、是正する。

### 1.4 `[DI]`操作ログの標準化
以下の形式で、保存・適用の成功／失敗を明示するトレースログを追加する（既存ログを置き換えるのではなく追加する）。

```csharp
if (AppLogger.IsTraceEnabled)
    AppLogger.LogTrace($"[DI][Save] ProfileRepository.SaveProfile: device={deviceIndex}, profile='{profileName}', result={result}");
```

```csharp
if (AppLogger.IsTraceEnabled)
    AppLogger.LogTrace($"[DI][Apply] {caller}: device={deviceIndex}, profile='{profileName}', isTemp={isTemp}, result={result}, notify={displayNotification}");
```

対象箇所: `ProfileRepository.SaveProfile`、`ProfileApplicationService.ApplyProfile`／`ApplyFromAction`（Task.Run内）／`RestoreFromAction`、`DefaultProfileSwitcher.SwitchProfile`／`RestoreProfile`／`ApplyManualProfile`。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DS4Control/ScpUtil.cs`（`Global.SaveProfile`） | 更新（ピンポイント） | 過渡期シム | `void`→`bool`への変更（非破壊的） |
| `DS4Windows/DI/IProfileXmlStore.cs`（Step2成果物） | 更新 | **DI永続資産** | `SaveProfileXml`の戻り値を`bool`に補正 |
| `DS4Windows/DS4Control/Services/ProfileXmlStore.cs`（Step2成果物） | 更新 | **DI永続資産** | 戻り値伝播の実装補正 |
| `DS4Windows/DI/IProfileRepository.cs` | 更新 | **DI永続資産** | `SaveProfile`の戻り値を`bool`に変更 |
| `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 更新 | **DI永続資産** | 戻り値伝播、`[DI][Save]`ログ追加 |
| `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` | 更新 | **DI永続資産** | `[DI][Apply]`ログ追加（`ApplyProfile`／`ApplyFromAction`内Task／`RestoreFromAction`） |
| `DS4Windows/Actions/DefaultProfileSwitcher.cs` | 更新 | **DI永続資産** | 通知抑制設定の反映（1.3節）、`[DI][Apply]`ログ追加、戻り値ハンドリング |
| `DS4WindowsTests/ProfileSaveApplyResultTests.cs` | 新規 | **テスト資産** | 保存失敗時の戻り値伝播、通知抑制設定がSpecialAction経由でも尊重されることの単体テスト |
| `docs-forDIMG/MadeByAgent/Phase5-Step4-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step4-Completion-Report.md` | 新規 | ドキュメント | Step4完了報告書 |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step4進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step4-1: Step2／Step3の実装状況確認
- `IProfileXmlStore`／`IProfileRepository`（Step2）、`IProfileApplicationService.ApplyProfile`／`DefaultProfileSwitcher`のリファクタリング（Step3）が実装済みかを確認する。未実装の場合、本Step内で該当箇所を先に実装するか、Step2／Step3の完了を待つかを判断し記録する。

### タスク Step4-2: 保存系戻り値の伝播（0.2(A)の是正）
- `Global.SaveProfile`を`bool`返却に変更する。
- `IProfileXmlStore.SaveProfileXml`／`ProfileXmlStore`実装を`bool`返却に補正する。
- `IProfileRepository.SaveProfile`／`ProfileRepository`実装を`bool`返却に変更し、`[DI][Save]`ログを追加する。

### タスク Step4-3: 通知抑制の統一（0.2(B)の是正）
- `DefaultProfileSwitcher.SwitchProfile`が`IProfileSettingsService.ProfileChangedNotification`を読み取り、適用呼び出しの`displayNotification`引数に正しく渡すよう修正する。
- `RestoreProfile`のフォールバック経路（`_previousProfiles`から復帰する箇所）でも同様に通知抑制設定を反映する。

### タスク Step4-4: `[DI]`操作ログの追加
- 1.4節の形式に従い、`ProfileRepository.SaveProfile`、`ProfileApplicationService`の各メソッド、`DefaultProfileSwitcher`の各メソッドに`[DI][Save]`／`[DI][Apply]`ログを追加する。

### タスク Step4-5: 単体テスト作成と自動テスト実行
- `ProfileSaveApplyResultTests.cs`を作成し、以下を検証する。
  - 保存失敗（例: 不正なパス）時に`ProfileRepository.SaveProfile`が`false`を返すこと。
  - `ProfileChangedNotification = false`設定時、`DefaultProfileSwitcher.SwitchProfile`経由でも通知が抑制されること（通常GUI切替と同じ挙動になること）。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が全件通過することを確認する。

### タスク Step4-6: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md`のStep4欄を更新し、`Phase5-Step4-Completion-Report.md`を作成する。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `Global.SaveProfile`の戻り値変更（`void`→`bool`）が、想定外の呼び出し元（式文以外の文脈、例: デリゲート型として参照している箇所）に影響する | Step4-2 | 事前に`Global.SaveProfile`の全参照箇所をリポジトリ全体で検索し、デリゲート／イベントハンドラとして渡されている箇所がないことを確認してから変更する。 |
| 通知抑制の是正により、これまで「常に通知される」ことを前提に動作確認していたテストシナリオが変化し、想定していた通知が出なくなる | Step4-3 | 既存の実機検証チェックリスト・単体テストで、通常GUI切替・SpecialAction切替の両方について通知ON/OFF双方の挙動を回帰確認する。挙動変化はNo Feature Dropの例外的な「不具合修正」であることを完了報告書に明記する。 |
| `[DI]`ログの追加により、`AppLogger.IsTraceEnabled`が真の環境でログ出力量が増え、パフォーマンスに影響する | Step4-4 | 既存の`Phase4`実装と同様、必ず`if (AppLogger.IsTraceEnabled)`でガードしてから出力する。 |
| Step2／Step3が本Step着手時点で未実装の場合、本Stepの前提が崩れる | Step4-1 | タスクStep4-1で実装状況を確認し、未実装であれば作業順序をPhase5-Status.mdに明記した上で、本Step内で必要な前提部分のみ先行実装する。 |

---

## 5. 完了判定基準

- [ ] `Global.SaveProfile`が`bool`を返し、`ProfileRepository.SaveProfile`まで保存成否が伝播している。
- [ ] `DefaultProfileSwitcher`経由のSpecialAction切替でも、`ProfileChangedNotification`設定による通知抑制が正しく機能する（通常GUI切替と同一の挙動になる）。
- [ ] `ProfileRepository`／`ProfileApplicationService`／`DefaultProfileSwitcher`の各操作に`[DI][Save]`／`[DI][Apply]`ログが追加されている。
- [ ] 新設した`ProfileSaveApplyResultTests`および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md`が更新され、`Phase5-Step4-Completion-Report.md`が作成されている。