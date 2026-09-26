# フェーズ5-Step3 計画書: プロファイル適用・復帰の責務分離

作成日: 2026-09-02（改訂日: 2026-09-03・アーキテクチャガードレール反映）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step3, §5.2, §5.6（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（前Step）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - `Global.ApplyProfile` 等の古い経路は、新しい適用契約が完成し動作確認が取れるまで削除しない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - `isTemp` 判定、一時プロファイルの復帰スナップショット、カスケードループ防止ガード（250ms デバウンス）、`touchpadActive` 等の状態管理を100%維持する。
  - 特に本Stepで確認された **2種類の復帰追跡機構**（後述0.2）はどちらも安易に単純化・削除しない。
- **§2.3 ログ出力の厳格な維持**:
  - `AppLogger.LogToGui`／`AppLogger.LogTrace`／`AppLogger.LogDebug`、および `AppLogger.LogProfileChanged` を維持する。
- **§3.1 DI (Dependency Injection) の実装**:
  - コンテナ登録は `ServiceRegistration.cs` に行う。既存の `IProfileApplicationService`／`IProfileSwitcher` のインターフェース名・登録ライフタイムは維持する。
  - **DI 原則の徹底**: 呼び出し元（Switcher や ViewModel）にインフラストラクチャ層のインスタンス（`ControlService` / `Program.rootHub`）を引き回させず、サービス内部で解決・カプセル化する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.ApplyProfile`／`CompleteProfileApplication`）はピンポイント置換のみ行う。

---

## 0. Step3の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2, §4-4 に基づき、以下の**2つのDIサービス**を対象とする。これらは同一の「プロファイル適用」という機能領域を別々の経路で実装しており、重複の解消と `Program.rootHub` への直接依存排除を行う。

- `IProfileApplicationService` → `ProfileApplicationService`（#10）: `Global.ApplyProfile`／`Global.LoadTempProfile`／`Global.LoadProfile`／`Global.CompleteProfileApplication`、`Mapping.TakePendingRestoreProfileName`
- `IProfileSwitcher` → `DefaultProfileSwitcher`（#11）: `Global.ApplyProfile`（3箇所）、`Program.rootHub`（2箇所）

### 0.2 現状のコード構造（GitHub実コード確認済み）
`ProfileApplicationService` と `DefaultProfileSwitcher` は、いずれもプロファイル切替を扱うが、実装が独立して並存している。

| 項目 | `ProfileApplicationService` | `DefaultProfileSwitcher` |
|---|---|---|
| 切替実行 | `ApplyFromAction`: `device.HaltReportingRunAction` で入力停止した上で `Global.ApplyProfile` を実行し、連鎖アクションを発火 | `SwitchProfile`: 250msデバウンスガード後、直接 `Global.ApplyProfile` を `Program.rootHub` 引数で実行（入力停止なし） |
| 復帰追跡 | `Mapping.TakePendingRestoreProfileName`（`Mapping` 静的クラスが管理する保留復帰スタック） | インスタンスフィールド `_previousProfiles[4]`／`_temporaryProfiles[4]`（自前のバックアップ配列） |
| 復帰実行 | `RestoreFromAction`: 上記スタックから取得し、`Global.LoadTempProfile` 等を実行 | `RestoreProfile`: まず `IProfileApplicationService.RestoreFromAction` を試み、失敗時のみ自前の `_previousProfiles` から `Global.ApplyProfile` を再実行（`Program.rootHub` 直接参照） |
| 手動適用 | なし | `ApplyManualProfile`: `Global.ApplyProfile`（`Program.rootHub` 引数）へのパススルー |

### 0.3 「通常GUI切替」経路の要確認事項
通常のGUIプロファイル切替（コントローラー一覧のドロップダウン等）および `ProfileEditor` Save／Apply が、`IProfileApplicationService` を経由しているか、あるいは View / ViewModel から直接 `Global.ApplyProfile` を呼んでいるかをタスク Step3-1 で特定する。

### 0.4 全体4層モデルにおける位置づけ
いずれも**第4層 4-c**（設定・プロファイル適用サービス）に属する。本層内でプロファイル「適用」契約を一本化し、下位または上位への不正な静的シングルトンアクセスを断ち切る。

---

## 1. 設計方針とアーキテクチャ

### 1.1 `IProfileApplicationService` を単一の適用契約として拡張
既存の `IProfileApplicationService` を「プロファイル適用の唯一の契約」と位置づけ、汎用の `ApplyProfile` メソッドを追加する。

#### 【設計の要点: ControlService 引数の排除】
旧設計案では `ControlService control` を引数として要求していたが、これでは呼び出し元（`DefaultProfileSwitcher` や UI）が `Program.rootHub` を直接知る必要が生じ、DI化の趣旨に反する。  
したがって、**引数から `ControlService` を完全に排除**し、`ProfileApplicationService` の内部で解決（内部で `Program.rootHub` を渡す、または将来的に注入されたサービスを利用する）する設計とする。

```csharp
namespace DS4Windows.DI
{
    public interface IProfileApplicationService
    {
        // 既存メソッド（変更なし）
        void ApplyFromAction(int deviceIndex, SpecialAction action);
        bool RestoreFromAction(int deviceIndex);

        // 新規追加: SwitchProfile / ApplyManualProfile / GUI切替を統合する汎用適用メソッド
        // ※ ControlService 引数は外部に公開せず、実装内部でカプセル化する
        bool ApplyProfile(int deviceIndex, string profileName, bool isTemp, bool launchProgram,
            ProfileChangeSource source = ProfileChangeSource.ProfileChange,
            string prolog = null, bool displayNotification = true);
    }
}
```

---

### 1.2 【重要注記: `Global.ProfilePath[deviceIndex]` の更新タイミング】
`Global.ApplyProfile` の内部挙動として、事前にスロット配列 `Global.ProfilePath[deviceIndex] = profileName;` が更新されていることを前提としている処理が存在する。  
一時プロファイル（`isTemp == true`）の場合はスロットパスを上書きしてはならないが、通常プロファイルの適用時は必須となる。

したがって、`ProfileApplicationService.ApplyProfile` の実装内では、**`Global.ApplyProfile` を呼び出す直前に以下のスロット更新処理を確実に実行**する（`DefaultProfileSwitcher.SwitchProfile` line 41 と厳格に整合させる）。

```csharp
if (!isTemp)
{
    Global.ProfilePath[deviceIndex] = profileName;
}
```

---

### 1.3 `DefaultProfileSwitcher` のリファクタリング方針
`DefaultProfileSwitcher` から `Global.ApplyProfile` および `Program.rootHub` への直接依存を完全に排除し、注入された `IProfileApplicationService` へ処理を委譲する。

- **`SwitchProfile`**:
  - 250msデバウンスガードおよび自前バックアップ配列 `_previousProfiles` の退避処理は**そのまま維持**する（カスケード防止のため必須）。
  - その後の適用呼び出しを `_profileApplicationService.ApplyProfile(deviceIndex, profileName, false, false, ...)` に置換する。
- **`RestoreProfile`**:
  - 既存の「`IProfileApplicationService.RestoreFromAction` 優先、失敗時に自前バックアップ配列から復帰」というフォールバック順序を維持する。
  - フォールバック時の `Global.ApplyProfile` 呼び出しを `_profileApplicationService.ApplyProfile(...)` に置換する。
- **`ApplyManualProfile`**:
  - `_profileApplicationService.ApplyProfile(...)` への委譲に置換する。
- **効果**:
  - `DefaultProfileSwitcher.cs` 内の **`Program.rootHub` 参照（2箇所）および `Global.ApplyProfile` 参照（3箇所）がすべて消滅し、依存が 0 件**になる。

---

### 1.4 2系統の復帰追跡機構に関する方針
- `Mapping.TakePendingRestoreProfileName`（`Mapping.cs` 内部の静的スタック）: SpecialAction 由来の一時プロファイル復帰用。
- `DefaultProfileSwitcher._previousProfiles`（自前配列）: カスケードループ防止および Switcher 単体での直前復帰用。

この2系統は用途とライフサイクルが異なるため、本Stepでは安易に1つに統合せず、それぞれの責任範囲を維持したまま適用経路（`IProfileApplicationService`）のみを統一する。

---

### 1.5 アーキテクチャ・ガードレール: プロファイル適用時と切断時の安全性保証（Phase5-Plan §5.2, §5.6準拠）

#### 1.5.1 [入力スレッド保護] プロファイル適用時における「入力ポーリング停止（Halt）」保証（§5.2）
- **【問題の実態】**:
  `ProfileApplicationService.ApplyFromAction` では `device.HaltReportingRunAction` を呼び、コントローラーの高速入力ポーリングループ（毎秒250〜1000回）を一時停止させた状態で `Global.ApplyProfile` を安全に実行している。
  しかし、`DefaultProfileSwitcher` や通常の GUI 切替ではこれを行っておらず、入力ループが稼働している最中にマッピングテーブルやアクション辞書の再構築が走っている。
  この状態でプロファイルが切り替わると、走査スレッド側で **`InvalidOperationException: コレクションが変更されました`** が発生し、アプリがサイレントクラッシュする重大なリスクがある。
- **【推奨対策】**:
  新設する `ProfileApplicationService.ApplyProfile` 実装内において、対象スロットに接続されているデバイス（`DS4Device`）が存在し稼働中の場合は、**必ず `device.HaltReportingRunAction(() => { ... })` により安全に入力レポートを一時停止させた状態で `Global.ApplyProfile` を実行するガード**を標準実装する。

#### 1.5.2 [状態管理] コントローラー物理切断時の一時プロファイル（TempProfile）残留防止（§5.6）
- **【問題の実態】**:
  一時プロファイル（特定ボタンを押している間だけ適用される設定）が適用されている最中に、ユーザーが USB ケーブルを抜去したり Bluetooth 接続が切断された場合、ボタンを離すイベント（復帰アクション）が発火しない。
  そのため、`Mapping.TakePendingRestoreProfileName` の復帰スタックに古いプロファイル情報が残留したままになり、次回コントローラーを再接続した際に「一時プロファイルが適用されたまま復帰不能になる」という状態リークが発生する。
- **【推奨対策】**:
  `ControlService` のデバイス切断イベント（`DeviceRemoved` / `Hotplug`）または `IProfileApplicationService` にクリーンアップメソッド（`ClearPendingRestoreProfile(int deviceIndex)`）を設け、**物理切断を検知した際に該当スロットの一時プロファイル保留スタックおよびフラグを強制クリアする安全機構**を確立する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IProfileApplicationService.cs` | `ApplyProfile` メソッドの追加（`ControlService` 引数なし） |
| サービス実装 | `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` | `ApplyProfile` の実装（スロット先行更新、`HaltReportingRunAction` 入力停止ガード組み込み、切断時クリーンアップ対応） |
| スイッチャー | `DS4Windows/Actions/DefaultProfileSwitcher.cs` | `Global.ApplyProfile` および `Program.rootHub` 直接参照の排除、`IProfileApplicationService` 経由への統一 |
| 単体テスト | `DS4WindowsTests/ProfileApplicationServiceTests.cs`（新設または拡充） | 新設 `ApplyProfile` の動作検証、通知抑制・スロット更新・Halt実行確認 |
| 単体テスト | `DS4WindowsTests/ProfileSwitchActionTests.cs` | `DefaultProfileSwitcher` のモック検証 |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step3-1: 「通常GUI切替」「ProfileEditor Save／Apply」呼び出し元の調査【前提】
1. `ControllersViewModel.cs` 等で、プロファイルドロップダウン選択時にどのメソッドが呼ばれているかを grep 調査。
2. `ProfileEditor.xaml.cs` の「Apply」「Save」ボタン押下時の適用呼び出し経路を調査。
3. 調査結果に基づき、Step3-5 でこれらを `IProfileApplicationService` 経由に切り替える対象とするかを確定する。

### タスク Step3-2: 復帰追跡機構の用途精査
1. `Mapping.TakePendingRestoreProfileName` の参照箇所を全件洗い出し、`DefaultProfileSwitcher._previousProfiles` との競合がないかを再確認する。

### タスク Step3-3: `IProfileApplicationService.ApplyProfile` の追加（Haltガード内包）
1. `DS4Windows/DI/IProfileApplicationService.cs` に `ApplyProfile` を定義。
2. `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` に実装を追加。
   - スロット更新注記（§1.2）に基づき `if (!isTemp) Global.ProfilePath[deviceIndex] = profileName;` を配置。
   - ガードレール（§1.5.1）に基づき、デバイス稼働時は `device.HaltReportingRunAction` 経由で `Global.ApplyProfile` を呼び出す。

```csharp
// ProfileApplicationService.cs 実装イメージ
public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp, bool launchProgram,
    ProfileChangeSource source = ProfileChangeSource.ProfileChange,
    string prolog = null, bool displayNotification = true)
{
    if (deviceIndex < 0 || deviceIndex >= ControlService.MAX_NUM_CONTROLLERS)
        return false;

    // 重要: 通常適用の場合はスロットパスを先行更新
    if (!isTemp && !string.IsNullOrEmpty(profileName))
    {
        Global.ProfilePath[deviceIndex] = profileName;
    }

    bool success = false;
    DS4Device device = _control?.DS4Controllers[deviceIndex] ?? Program.rootHub?.DS4Controllers[deviceIndex];

    // ガードレール: デバイス稼働時は入力ループを安全に一時停止して適用
    Action applyAction = () =>
    {
        success = Global.ApplyProfile(
            deviceIndex,
            launchProgram,
            Program.rootHub,
            load: true,
            source: source,
            prolog: prolog,
            displayNotification: displayNotification);
    };

    if (device != null && device.IsAlive())
    {
        device.HaltReportingRunAction(applyAction);
    }
    else
    {
        applyAction();
    }

    return success;
}
```

### タスク Step3-4: `DefaultProfileSwitcher` のリファクタリング
1. `DS4Windows/Actions/DefaultProfileSwitcher.cs` の `SwitchProfile`, `RestoreProfile`, `ApplyManualProfile` を修正。
2. `Program.rootHub` の参照（line 44, line 78 等）および `Global.ApplyProfile` の直接呼び出しを、`_profileApplicationService.ApplyProfile` に置き換える。
3. `using` から不要となった参照を整理。

### タスク Step3-5: Step3-1調査結果に基づく追加対象の修正（対象がある場合のみ）
1. Step3-1 で特定された GUI 側の呼び出し元が容易に DI 注入可能な構成であれば、`IProfileApplicationService.ApplyProfile` を使用するように変更。
2. 結合度が高くリスクが大きい場合は、本Stepではスコープ外とし Step13（UI統合）へ送る判断を明記する。

### タスク Step3-6: 単体テスト作成と自動テスト実行
1. `ProfileApplicationServiceTests` に `ApplyProfile` のテストを追加（Halt呼び出し検証含む）。
2. `DefaultProfileSwitcher` のテストを実行し、`IProfileApplicationService` への委譲が正しく機能していることを検証。
3. 全テスト実行（`dotnet test`）で既存機能にリグレッションがないことを確認。

### タスク Step3-7: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルドの成功を確認。
2. `Phase5-Status.md` の Step3 進捗を「完了」に更新。
3. `Phase5-Step3-Completion-Report.md` を作成。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **コレクション変更クラッシュ** | 高 | プロファイル適用時に入力ポーリングを `device.HaltReportingRunAction` で一時停止して辞書を再構築する（§1.5.1）。 |
| **物理切断時の状態リーク** | 中 | コントローラー物理切断時に該当スロットの一時プロファイル保留スタックをリセットする安全機構を設ける（§1.5.2）。 |
| **スロット配列の不整合** | 高 | `Global.ApplyProfile` 呼び出し前に `if (!isTemp) Global.ProfilePath[deviceIndex] = profileName;` を必ず実行する（§1.2）。 |
| **カスケードループ再発** | 高 | `DefaultProfileSwitcher` 内の 250ms デバウンスガード（`_lastSwitchedTimes`）および `_previousProfiles` 退避機構には一切手を触れずそのまま維持する。 |
| **GUI通知の重複・抑制不全** | 中 | `displayNotification` 引数を正しく伝播させ、Step4 の「通知統一」計画と整合させる。 |

---

## 5. 完了判定基準

- [ ] `IProfileApplicationService` に `ControlService` 引数を持たないクリーンな `ApplyProfile` が定義されていること。
- [ ] `ProfileApplicationService.ApplyProfile` 内でスロット更新（`Global.ProfilePath`）とプロファイル適用が正しく行われていること。
- [ ] プロファイル適用時に `device.HaltReportingRunAction` による入力停止が行われ、マルチスレッド下での `InvalidOperationException` が防止されていること（§1.5.1）。
- [ ] コントローラー切断時に一時プロファイル保留スタックが残留しないクリーンアップ設計が担保されていること（§1.5.2）。
- [ ] `DefaultProfileSwitcher.cs` 内から `Program.rootHub` への参照が 0 件になっていること。
- [ ] `DefaultProfileSwitcher.cs` 内から `Global.ApplyProfile` への直接参照が 0 件になり、`_profileApplicationService` 経由になっていること。
- [ ] 250ms デバウンスおよびカスケードガードが機能していること。
- [ ] 単体テストがすべて成功し、ビルドエラー・警告増がないこと。
