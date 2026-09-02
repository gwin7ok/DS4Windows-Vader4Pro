# フェーズ5-Step3 計画書: プロファイル適用・復帰の責務分離

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step3（Phase5詳細計画書。`IProfileSwitcher`の本Stepへの統合方針を含む）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果。本Stepの対象根拠）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（前Step。`IProfileXmlStore`によるXML I/O分離。本StepはXML層より上位の「適用」層を対象とする）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.ApplyProfile`等の古い経路は、新しい適用契約が完成し動作確認が取れるまで削除しない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - `isTemp`判定、一時プロファイルの復帰スナップショット、カスケードループ防止ガード（250ms デバウンス）、`touchpadActive`等の状態管理を100%維持する。特に本Stepで発見した**2種類の復帰追跡機構**（後述0.3）はどちらも安易に単純化・削除しない。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`／`AppLogger.LogDebug`、および`AppLogger.LogProfileChanged`を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は`ServiceRegistration.cs`に行う。既存の`IProfileApplicationService`／`IProfileSwitcher`のインターフェース名・登録ライフタイムは維持する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.ApplyProfile`／`CompleteProfileApplication`）はピンポイント置換のみ行う。

---

## 0. Step3の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2, §4-4 に基づき、以下の**2つのDIサービス**を対象とする。これらは同一の「プロファイル適用」という機能領域を別々の経路で実装しており、`Phase5-Plan.md` Step3は「SpecialAction経由の適用契約統一」としてこの重複解消をStep3スコープに含めることを定めている。

- `IProfileApplicationService`→`ProfileApplicationService`（#10）: `Global.ApplyProfile`／`Global.LoadTempProfile`／`Global.LoadProfile`／`Global.CompleteProfileApplication`、`Mapping.TakePendingRestoreProfileName`
- `IProfileSwitcher`→`DefaultProfileSwitcher`（#11）: `Global.ApplyProfile`（3箇所）、`Program.rootHub`（2箇所）

### 0.2 現状のコード構造（GitHub実コード確認済み）
`ProfileApplicationService`と`DefaultProfileSwitcher`は、いずれもSpecialAction経由のプロファイル切替を扱うが、**実装が完全に独立しており、互いの存在をほぼ意識していない**。

| 項目 | `ProfileApplicationService` | `DefaultProfileSwitcher` |
|---|---|---|
| 切替実行 | `ApplyFromAction`: `device.HaltReportingRunAction`でデバイス入力を一時停止した上で`Global.ApplyProfile`を実行し、`IProfileActionChainService.DispatchNextActions`で連鎖アクションを発火 | `SwitchProfile`: 250msデバウンスガード後、直接`Global.ApplyProfile`を`Program.rootHub`引数で実行（デバイス入力停止は行っていない） |
| 復帰追跡 | `Mapping.TakePendingRestoreProfileName`（`Mapping`静的クラスが管理するデバイス単位の保留復帰スタック） | インスタンスフィールド`_previousProfiles[4]`／`_temporaryProfiles[4]`（自前のバックアップ配列） |
| 復帰実行 | `RestoreFromAction`: 上記スタックからプロファイル名を取得し`Global.LoadTempProfile`または`Global.LoadProfile`＋`Global.CompleteProfileApplication`を実行 | `RestoreProfile`: まず`IProfileApplicationService.RestoreFromAction`を試み、失敗時のみ自前の`_previousProfiles`から`Global.ApplyProfile`を再実行（**フォールバックとして`ProfileApplicationService`に依存しているが、Global直呼び出しの独自経路も残存**） |
| 手動適用 | なし | `ApplyManualProfile`: `Global.ApplyProfile`への単純なパススルー |

**重要な発見**: `DefaultProfileSwitcher.RestoreProfile`は既に`IProfileApplicationService.RestoreFromAction`を優先的に呼び出す実装になっており、部分的な統合が行われている。しかし（a）`SwitchProfile`（切替の入口）と`ApplyManualProfile`は依然として`Global.ApplyProfile`を直接呼び出しており、（b）復帰追跡機構が2系統（`Mapping`の保留スタック／`DefaultProfileSwitcher`自前配列）並存している。

### 0.3 「通常GUI切替」経路の要確認事項
`Phase5-Plan.md` Step3は「通常GUI切替、編集画面Save／Apply、SpecialAction、AutoProfileのすべてが同じ適用契約を使用すること」を目標としているが、本Stepの事前調査では**通常のGUIプロファイル切替（コントローラー一覧のドロップダウン等）およびProfileEditor Save／Applyが、`IProfileApplicationService`と`IProfileSwitcher`のいずれを経由しているか未確認**である。これらは別のViewModel／View（`ControllersViewModel`、`ProfileEditor.xaml.cs`等）から直接`Global.ApplyProfile`を呼んでいる可能性がある。タスクStep3-1でこれを特定し、対象に含めるかを確定する。

### 0.4 全体4層モデルにおける位置づけ
`ProfileApplicationService`／`DefaultProfileSwitcher`はいずれも**第4層 4-c**（設定・プロファイル適用サービス）に属する。本Stepはこの層内で、プロファイル「適用」という単一機能に対する重複実装を統合する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IProfileApplicationService`を単一の適用契約として拡張
既存の`IProfileApplicationService`を「プロファイル適用の唯一の入口」と位置づけ、以下のメソッドを追加する。

```csharp
namespace DS4Windows.DI
{
    public interface IProfileApplicationService
    {
        // 既存メソッド（変更なし）
        void ApplyFromAction(int deviceIndex, SpecialAction action);
        bool RestoreFromAction(int deviceIndex);

        // 新規追加: DefaultProfileSwitcherのSwitchProfile/ApplyManualProfileを統合する汎用適用メソッド
        bool ApplyProfile(int deviceIndex, string profileName, bool isTemp, bool launchProgram,
            ControlService control, ProfileChangeSource source,
            string prolog = null, bool displayNotification = true);
    }
}
```

`ApplyProfile`の実装は、現時点では`Global.ApplyProfile`への薄い委譲とする（`Global.ApplyProfile`内部のXML再読込・状態更新ロジック自体の分解はStep4以降の対象）。本Stepの主眼は「呼び出し元の集約」であり、`Global.ApplyProfile`の内部実装分解は行わない。

### 1.2 `DefaultProfileSwitcher`のリファクタリング方針
`DefaultProfileSwitcher`から`Global.ApplyProfile`／`Program.rootHub`への直接依存を排除し、`IProfileApplicationService`をコンストラクタ注入して置き換える。

- `SwitchProfile`: `Global.ApplyProfile(...)`直接呼び出しを`_profileApplicationService.ApplyProfile(...)`へ置換。**250msデバウンスガードと`_previousProfiles`／`_temporaryProfiles`によるバックアップは維持する**（`Mapping`の保留復帰スタックとは異なる目的＝カスケードループ防止のためのローカル状態であり、統合の対象外と判断する）。
- `RestoreProfile`: 既存の「`IProfileApplicationService.RestoreFromAction`優先、フォールバックで自前配列から復帰」という構造を維持しつつ、フォールバック時の`Global.ApplyProfile`直接呼び出しを`_profileApplicationService.ApplyProfile`へ置換する。
- `ApplyManualProfile`: `Global.ApplyProfile`直接呼び出しを`_profileApplicationService.ApplyProfile`へ置換する。

### 1.3 2系統の復帰追跡機構に関する方針
`Mapping.TakePendingRestoreProfileName`（`ProfileApplicationService`が使用）と`DefaultProfileSwitcher`自前の`_previousProfiles`配列は、**用途が異なる可能性がある**（前者はマッピングエンジン側が管理する「保留中の復帰」、後者はSpecialAction切替時のUI/呼び出し側バックアップ）ため、本Stepでは安易に統合・削除しない。タスクStep3-2で両者の実際の呼び出しタイミングと用途を精査し、完全な重複であれば統合案を、異なる用途であれば併存の妥当性を`Phase5-Step3-Completion-Report.md`に記録する。

### 1.4 「通常GUI切替」の扱い
タスクStep3-1の調査結果に応じて、通常GUI切替が`Global.ApplyProfile`を直接呼んでいる場合は、可能な範囲で`IProfileApplicationService.ApplyProfile`経由に統一する。ただし影響範囲（呼び出し元ファイル数）によっては本Stepでは見送り、別Stepとして切り出す判断もありうる（判断基準・結果を完了報告書に記録する）。

---

## 2. 成果物一覧

| ファイルパス | 種別 | ライフサイクル | 内容 |
|---|---|---|---|
| `DS4Windows/DI/IProfileApplicationService.cs` | 更新 | **DI永続資産** | `ApplyProfile`メソッドの追加 |
| `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` | 更新 | **DI永続資産** | `ApplyProfile`実装の追加（`Global.ApplyProfile`への薄い委譲） |
| `DS4Windows/Actions/DefaultProfileSwitcher.cs` | 更新 | **DI永続資産** | `Global.ApplyProfile`／`Program.rootHub`直接呼び出しを`IProfileApplicationService`経由へ置換。コンストラクタ注入追加 |
| `DS4Windows/DI/ServiceRegistration.cs` | 確認・必要に応じ更新 | **DI永続資産** | `DefaultProfileSwitcher`への`IProfileApplicationService`注入経路の確認（登録順序に依存関係がある場合は調整） |
| （調査対象、タスクStep3-1で特定） | 更新（要確認） | **DI永続資産** | 通常GUI切替・ProfileEditor Save／Applyの呼び出し元。対象と判断された場合のみピンポイント修正 |
| `DS4WindowsTests/ProfileApplicationServiceTests.cs` | 新規 | **テスト資産** | `ApplyProfile`／`ApplyFromAction`／`RestoreFromAction`の単体テスト |
| `DS4WindowsTests/DefaultProfileSwitcherTests.cs` | 新規 | **テスト資産** | デバウンスガード、復帰フォールバック順序、`IProfileApplicationService`連携の単体テスト |
| `docs-forDIMG/MadeByAgent/Phase5-Step3-Plan.md` | 新規 | ドキュメント | 本計画書 |
| `docs-forDIMG/MadeByAgent/Phase5-Step3-Completion-Report.md` | 新規 | ドキュメント | Step3完了報告書（0.3・1.3の調査結果を含む） |
| `docs-forDIMG/MadeByAgent/Phase5-Status.md` | 更新 | ドキュメント | Step3進捗ステータス更新 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step3-1: 「通常GUI切替」「ProfileEditor Save／Apply」呼び出し元の調査
- `Global.ApplyProfile`の全呼び出し箇所をリポジトリ全体で検索し、`IProfileApplicationService`／`IProfileSwitcher`経由でないもの（View／ViewModelからの直接呼び出し）を洗い出す。
- 調査結果を`Phase5-Step3-Completion-Report.md`に一覧化し、本Stepで統一する範囲を確定する。

### タスク Step3-2: 復帰追跡機構（`Mapping`保留スタック vs `DefaultProfileSwitcher`自前配列）の用途精査
- `Mapping.TakePendingRestoreProfileName`／対となる登録処理の呼び出しタイミングを確認する。
- `DefaultProfileSwitcher`の`_previousProfiles`／`_temporaryProfiles`が更新・参照されるタイミングを確認する。
- 完全重複か別用途かを判定し、統合方針（統合する／併存を維持する）を決定・記録する。

### タスク Step3-3: `IProfileApplicationService.ApplyProfile`の追加
- インターフェースおよび実装クラスに`ApplyProfile`メソッドを追加する（1.1節のシグネチャ）。
- 内部実装は`Global.ApplyProfile`への薄い委譲とし、既存の戻り値・ログ出力を維持する。

### タスク Step3-4: `DefaultProfileSwitcher`のリファクタリング
- コンストラクタに`IProfileApplicationService`を追加注入する。
- `SwitchProfile`／`RestoreProfile`（フォールバック部分）／`ApplyManualProfile`内の`Global.ApplyProfile`直接呼び出しを`_profileApplicationService.ApplyProfile`へピンポイント置換する。
- `Program.rootHub`への直接参照を、注入済み`ControlService`（既存コンストラクタに追加が必要な場合は追加）経由に置換する。

### タスク Step3-5: Step3-1調査結果に基づく追加対象の修正（対象がある場合のみ）
- タスクStep3-1で特定した通常GUI切替／ProfileEditor Save／Apply呼び出し元を、範囲・リスクに応じて`IProfileApplicationService.ApplyProfile`経由へ置換する。

### タスク Step3-6: 単体テスト作成と自動テスト実行
- `ProfileApplicationServiceTests.cs`／`DefaultProfileSwitcherTests.cs`を作成する。
- 特にデバウンスガード（250ms以内の連続切替を無視すること）、復帰フォールバック順序（`IProfileApplicationService`優先→自前配列）を回帰的に検証する。
- 既存回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が全件通過することを確認する。

### タスク Step3-7: ビルド検証、進捗更新、完了報告書の作成
- `dotnet build DS4WindowsWPF.sln --nologo` を実行し警告0・エラー0を確認する。
- `Phase5-Status.md`のStep3欄を更新し、`Phase5-Step3-Completion-Report.md`を作成する（0.3・1.3の調査結果を含める）。

---

## 4. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| 2系統の復帰追跡機構を誤って統合し、いずれかのシナリオ（SpecialAction経由の一時切替 vs マッピングエンジン側の保留復帰）で復帰が失敗する | Step3-2, Step3-4 | タスクStep3-2で用途が異なると判明した場合は統合せず併存を維持する。統合する場合も、既存の全呼び出しパターンを単体テストで網羅してから実施する。 |
| `DefaultProfileSwitcher`のカスケードループ防止ガード（250msデバウンス）が、`IProfileApplicationService.ApplyProfile`への置換時に意図せず失われる | Step3-4 | デバウンス判定は`SwitchProfile`の入口でこれまで通り維持し、`ApplyProfile`呼び出しはガード通過後にのみ行う設計とする。単体テストでガード動作を検証する。 |
| 通常GUI切替の呼び出し元修正が想定より広範囲（多数のView/ViewModel）に及び、他機能への影響が読みきれない | Step3-1, Step3-5 | 影響範囲が大きいと判断した場合は本Stepでの修正を見送り、独立したStepとして切り出す（Phase5-Plan.md・Phase5-Status.mdへの追記が必要になる場合は事前に確認を取る）。 |
| `Global.ApplyProfile`薄い委譲化により、既存の75ファイルからの直接呼び出し元に予期せぬ影響が出る | Step3-3 | `Global.ApplyProfile`自体のシグネチャ・戻り値は変更せず、`IProfileApplicationService.ApplyProfile`はあくまで新しい呼び出し経路の追加として扱う（既存経路の削除はしない）。 |

---

## 5. 完了判定基準

- [ ] `IProfileApplicationService`に`ApplyProfile`メソッドが追加され、`Global.ApplyProfile`への薄い委譲として実装されている。
- [ ] `DefaultProfileSwitcher`が`Global.ApplyProfile`／`Program.rootHub`を直接呼び出さず、`IProfileApplicationService`経由に置き換わっている（`SwitchProfile`／`RestoreProfile`／`ApplyManualProfile`の3箇所）。
- [ ] カスケードループ防止ガード（250msデバウンス）の動作が単体テストで確認されている。
- [ ] 2系統の復帰追跡機構（`Mapping`保留スタック／`DefaultProfileSwitcher`自前配列）の用途精査結果が完了報告書に記録され、統合方針（統合する／併存を維持する）が明確になっている。
- [ ] 通常GUI切替・ProfileEditor Save／Applyの呼び出し元調査結果が完了報告書に記録され、本Stepでの対応範囲が明確になっている。
- [ ] 新設した`ProfileApplicationServiceTests`／`DefaultProfileSwitcherTests`および既存の全回帰テスト（`DS4WindowsTests`／`StandaloneTests`）が成功する。
- [ ] ソリューションビルドが警告0・エラー0で成功する。
- [ ] `Phase5-Status.md`が更新され、`Phase5-Step3-Completion-Report.md`が作成されている。