# フェーズ5-Step4 計画書: Save／Applyの操作結果と通知の統一

作成日: 2026-09-02（改訂日: 2026-09-03・アーキテクチャガードレール反映）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（全体計画書・全体4層モデル定義）
- `docs-forDIMG/MadeByAgent/Phase5-Plan.md` §2, §3 Step4, §5.2, §5.6（Phase5詳細計画書・ガードレール）
- `docs-forDIMG/MadeByAgent/Phase5-Status.md`（Phase5進捗管理）
- `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`（Step1監査結果）
- `docs-forDIMG/MadeByAgent/Phase5-Step2-Plan.md`（Step2計画書。保存成否bool化を先行反映済み）
- `docs-forDIMG/MadeByAgent/Phase5-Step3-Plan.md`（Step3計画書。プロファイル適用契約の一本化）
- `.github/copilot-instructions.md`（エージェント作業ルール）

---

## ルール確認（作業開始前に毎回読む）

- **§2.1 フォールバック実装・シム維持の原則**:
  - 既存の呼び出し元シグネチャ互換性を維持する。C#言語仕様上、戻り値が `void` から `bool` に変更されても、戻り値を無視する既存の呼び出し元はコンパイル・動作ともに破壊されない。
- **§2.2 現在の機能の完全維持 (No Feature Drop)**:
  - 通知の表示可否ロジック（`ProfileChangedNotification` の尊重）を全経路で一致させ、既存の意図しない通知強制バグのみを是正する。
- **§2.3 ログ出力の厳格な維持**:
  - 既存の `AppLogger` 出力はすべて維持し、DI経由の成否を追跡する標準化ログ（`[DI]` 接頭辞）を追加する。
- **§3.1 DI (Dependency Injection) の実装**:
  - クラス間の不要な依存関係の結合を避ける。呼び出し元（Switcher）に `IProfileSettingsService` を追加注入させず、サービス内部（`ProfileApplicationService`）で自動解決（カプセル化）する。
- **§3.2 巨大ファイルの編集方針**:
  - `ScpUtil.cs`（`Global.SaveProfile`）等の編集は最小限のピンポイント置換に留める。

---

## 0. Step4の位置づけと現状分析

### 0.1 Step1監査結果に基づく対象範囲
`Phase5-Step1-legacy-delegation-audit-report.md` §2 表の#7（`ProfileRepository`）、#10（`ProfileApplicationService`）、#11（`DefaultProfileSwitcher`）に関連し、保存・適用の結果伝播および通知制御の横断的な整流化を対象とする。

### 0.2 現状の課題（GitHub実コード確認済み）
1. **保存系の成否握りつぶし**: `ProfileRepository.SaveProfile` が内部で `bool` を受け取りつつも呼び出し元へ成否を伝播できていなかった課題は、Step2 の先行反映によって `bool SaveProfileXml` として契約が統一された。本Stepでは上位への伝播とエラーハンドリングを確定させる。
2. **プロファイル切替時の通知抑制不整合**: `ProfileApplicationService.ApplyFromAction` ではユーザーの通知設定（`ProfileChangedNotification`）を尊重しているのに対し、`DefaultProfileSwitcher.SwitchProfile` では `Global.ApplyProfile` の既定引数（`displayNotification = true`）に依存していたため、通知オフ設定が無視される実害バグが存在する。
3. **操作ログの不足**: プロファイルの保存および適用において、DI境界を通過した操作の成否を一貫して追跡できる統一ログ（`[DI]`）が存在しない。

### 0.3 本Stepの非対象（Step9への委譲）
`Global.CompleteProfileApplication` 内部にある `ActionManager` の静的呼び出し（アクション再ロード）は、Action基盤全体の刷新を担う **Step9** のスコープとし、本Stepでは現状維持とする。

### 0.4 全体4層モデルにおける位置づけ
第4層 4-c（設定・プロファイルサービス）とアクション実行層（Actions/）の間で、操作結果の伝播と通知・ログの責務を整流化する。

---

## 1. 設計方針とアーキテクチャ

### 1.1 保存系の成否伝播（bool化の確定）
- `IProfileXmlStore.SaveProfileXml`（Step2）: 既に `bool` として先行反映済み。
- `IProfileRepository.SaveProfile` / `ProfileRepository.SaveProfile`: `bool` を呼び出し元へ伝播させ、失敗時には GUI ログとトレースログを出力する。

```csharp
// ProfileRepository.SaveProfile 実装イメージ
public bool SaveProfile(int deviceIndex, string profileName)
{
    bool success = _profileXmlStore.SaveProfileXml(deviceIndex, profileName);
    if (success)
    {
        AppLogger.LogTrace($"[DI] SaveProfile succeeded: Slot {deviceIndex}, Profile '{profileName}'");
    }
    else
    {
        AppLogger.LogToGui($"Failed to save profile '{profileName}' for device {deviceIndex + 1}", true);
        AppLogger.LogTrace($"[DI] SaveProfile failed: Slot {deviceIndex}, Profile '{profileName}'");
    }
    return success;
}
```

---

### 1.2 プロファイル切替時の通知抑制の統一（サービス内部での自動解決）
#### 【設計の要点: Switcher への依存追加を回避する洗練策】
`DefaultProfileSwitcher` に `IProfileSettingsService` を追加注入すると、コンストラクタ引数やDI登録の不要な結合が増大する。
実コードにおいて `ProfileApplicationService` は既に `IProfileSettingsService _profileSettings` を保持しているため、`IProfileApplicationService.ApplyProfile` の通知引数を **Nullable（`bool? displayNotification = null`）** とする。

- `displayNotification` が `null` の場合: サービス内部で `_profileSettings.ProfileChangedNotification` を自動適用する。
- 明示的に `true` / `false` が渡された場合: その指定を優先する。
- **効果**: `DefaultProfileSwitcher` は通知設定を意識する必要がなく、引数を省略（既定の null）して呼ぶだけで自動的にユーザー設定が反映される。

```csharp
// DS4Windows/DI/IProfileApplicationService.cs
bool ApplyProfile(int deviceIndex, string profileName, bool isTemp, bool launchProgram,
    ProfileChangeSource source = ProfileChangeSource.ProfileChange,
    string prolog = null,
    bool? displayNotification = null); // null の場合は内部の _profileSettings を自動適用
```

```csharp
// ProfileApplicationService.ApplyProfile 実装イメージ
public bool ApplyProfile(int deviceIndex, string profileName, bool isTemp, bool launchProgram,
    ProfileChangeSource source = ProfileChangeSource.ProfileChange,
    string prolog = null,
    bool? displayNotification = null)
{
    if (deviceIndex < 0 || deviceIndex >= ControlService.MAX_NUM_CONTROLLERS)
        return false;

    if (!isTemp && !string.IsNullOrEmpty(profileName))
    {
        Global.ProfilePath[deviceIndex] = profileName;
    }

    // null の場合は内部保持している _profileSettings から自動解決
    bool shouldDisplay = displayNotification ?? _profileSettings.ProfileChangedNotification;

    bool success = false;
    DS4Device device = _control?.DS4Controllers[deviceIndex] ?? Program.rootHub?.DS4Controllers[deviceIndex];

    Action applyAction = () =>
    {
        success = Global.ApplyProfile(
            deviceIndex,
            launchProgram,
            Program.rootHub,
            load: true,
            source: source,
            prolog: prolog,
            displayNotification: shouldDisplay);
    };

    if (device != null && device.IsAlive())
    {
        device.HaltReportingRunAction(applyAction);
    }
    else
    {
        applyAction();
    }

    if (success)
    {
        AppLogger.LogTrace($"[DI] ApplyProfile succeeded: Slot {deviceIndex}, Profile '{profileName}', Temp={isTemp}, Notify={shouldDisplay}");
    }
    else
    {
        AppLogger.LogToGui($"Failed to apply profile '{profileName}' for device {deviceIndex + 1}", true);
        AppLogger.LogTrace($"[DI] ApplyProfile failed: Slot {deviceIndex}, Profile '{profileName}'");
    }

    return success;
}
```

```csharp
// DefaultProfileSwitcher.SwitchProfile からの呼び出し
// 通知引数を省略（null）するだけで、ユーザー設定に応じた通知抑制が自動的に行われる
_profileApplicationService.ApplyProfile(
    deviceIndex,
    targetProfile,
    isTemporaryProfile,
    launchProgram: false,
    source: ProfileChangeSource.MappingAction);
```

---

### 1.3 操作ログの標準化
保存および適用操作のトレースログとして、以下の標準フォーマットを採用する。
- 適用成功: `[DI] ApplyProfile succeeded: Slot {index}, Profile '{name}', Temp={bool}, Notify={bool}`
- 適用失敗: `[DI] ApplyProfile failed: Slot {index}, Profile '{name}'`
- 保存成功: `[DI] SaveProfile succeeded: Slot {index}, Profile '{name}'`
- 保存失敗: `[DI] SaveProfile failed: Slot {index}, Profile '{name}'`

---

### 1.4 アーキテクチャ・ガードレール: Halt停止中の結果判定と切断クリーンアップのログ統一（Phase5-Plan §5.2, §5.6準拠）

#### 1.4.1 [入力スレッド保護] プロファイル適用時の「入力ポーリング停止（Halt）」と結果伝播（§5.2）
- **【問題の実態】**:
  Step3 で導入される `device.HaltReportingRunAction` の実行中において、`Global.ApplyProfile` の内部でマッピング再構築中に例外が発生したり成否判定が握りつぶされると、外側の呼び出し元は適用が成功したか失敗したかを検知できず、UI のプロファイル表示やログが不整合に陥る。最悪の場合、入力ループが一時停止した状態のまま例外で脱落し、コントローラーが反応しなくなる危険がある。
- **【推奨対策】**:
  `HaltReportingRunAction` デリゲート内部で実行された `Global.ApplyProfile` の戻り値 `bool` を外側のスコープ変数 `success` に確実に格納して返却する。失敗時には GUI エラーログおよび標準化ログ `[DI] ApplyProfile failed: Slot {deviceIndex}, Profile '{profileName}'` を出力し、例外発生時も入力ループの復帰（Halt 解除）が `finally` 等で確実に保証されることを検証する。

#### 1.4.2 [状態管理] コントローラー物理切断時の復帰スタックリセットとログ通知（§5.6）
- **【問題の実態】**:
  一時プロファイル適用中にコントローラーが物理切断された際、復帰スタックの古い情報が残留するだけでなく、UIトレイ通知やGUIログに「一時プロファイルが強制リセットされたのか維持されているのか」のフィードバックが記録されないため、次回接続時の不具合調査が困難になる。
- **【推奨対策】**:
  物理切断検知時のクリーンアップ処理（`ClearPendingRestoreProfile` 等）において、保留スタックがリセットされたことを示す標準化トレースログ `[DI] Cleared pending restore profile on disconnect: Slot {deviceIndex}` を出力し、状態管理の透明性を確保する。

---

## 2. 成果物一覧

| 種別 | ファイルパス | 変更内容 |
|---|---|---|
| インターフェース | `DS4Windows/DI/IProfileApplicationService.cs` | `ApplyProfile` の通知引数を `bool? displayNotification = null` に更新 |
| サービス実装 | `DS4Windows/DS4Control/Services/ProfileApplicationService.cs` | 通知設定自動解決ロジック、Halt下での成否伝播、切断ログ、標準化ログ（`[DI]`）の実装 |
| サービス実装 | `DS4Windows/DS4Control/Services/ProfileRepository.cs` | 保存成否の `bool` 伝播および標準化ログ（`[DI]`）の実装 |
| スイッチャー | `DS4Windows/Actions/DefaultProfileSwitcher.cs` | `ApplyProfile` 呼び出し（通知引数は既定 null を利用し依存追加を回避） |
| 単体テスト拡充 | `DS4WindowsTests/ProfileApplicationServiceTests.cs` | `displayNotification` が `null` / `true` / `false` それぞれの動作検証テスト、Halt下成否検証 |
| 単体テスト拡充 | `DS4WindowsTests/ProfileRepositoryTests.cs` | 保存成功・失敗時の戻り値およびログ検証テスト |

---

## 3. 作業手順（マイクロタスク分割）

### タスク Step4-1: Step2／Step3 の前提状況確認
1. Step2 の `IProfileXmlStore.SaveProfileXml` が `bool` を返していることを確認する。
2. Step3 の `ProfileApplicationService.ApplyProfile` の Halt 停止ガードおよび戻り値構造を確認する。

### タスク Step4-2: 保存系成否伝播とログの実装
1. `ProfileRepository.SaveProfile` の戻り値を `bool` とし、成否判定と `[DI]` ログを追加する。

### タスク Step4-3: 通知抑制自動解決の実装
1. `IProfileApplicationService.ApplyProfile` を `bool? displayNotification = null` に更新する。
2. `ProfileApplicationService.ApplyProfile` 内で `displayNotification ?? _profileSettings.ProfileChangedNotification` を評価して適用する。
3. `DefaultProfileSwitcher` が余分な依存を持たずに呼び出せていることを確認する。

### タスク Step4-4: 単体テスト作成と自動テスト実行
1. `ProfileApplicationServiceTests` に、通知設定が OFF のときに `displayNotification = null` で呼ぶと通知フラグが `false` として渡されることを検証するテストを追加する。
2. `ProfileRepositoryTests` に、保存失敗時の戻り値 `false` 検証テストを追加する。
3. `dotnet test` を実行し、全テストパスを確認する。

### タスク Step4-5: ビルド検証、進捗更新、完了報告書の作成
1. Debug / Release ビルドの成功を確認する。
2. `Phase5-Status.md` の Step4 進捗を「完了」に更新する。
3. `Phase5-Step4-Completion-Report.md` を作成する。

---

## 4. リスクと回避策

| リスク | 影響度 | 回避策 |
|---|---|---|
| **Halt停止下の適用失敗握りつぶし** | 高 | `HaltReportingRunAction` 内の成否を外側の戻り値に確実に格納し、失敗ログを出力する（§1.4.1）。 |
| **不要な結合の増加** | 中 | `DefaultProfileSwitcher` に `IProfileSettingsService` を注入せず、`ProfileApplicationService` 内部で自動解決する（§1.2）。 |
| **切断時状態の不可視性** | 低 | 物理切断時のスタックリセットを標準化ログ `[DI]` として明示的に記録する（§1.4.2）。 |
| **保存失敗の握りつぶし** | 低 | `SaveProfile` が `bool` を返すことで、呼び出し元が失敗を確実にハンドリング可能にする。 |

---

## 5. 完了判定基準

- [ ] `IProfileApplicationService.ApplyProfile` が `bool? displayNotification = null` を受け付け、`null` 時に内部の `_profileSettings.ProfileChangedNotification` を自動適用すること。
- [ ] `device.HaltReportingRunAction` 実行下でのプロファイル適用成否が正しく呼び出し元へ返却されること（§1.4.1）。
- [ ] コントローラー物理切断時の復帰スタック強制リセットが `[DI]` ログとして追跡可能であること（§1.4.2）。
- [ ] `DefaultProfileSwitcher` が `IProfileSettingsService` を追加注入されることなく、通知設定を尊重した切り替えを行えること。
- [ ] `ProfileRepository.SaveProfile` が `bool` を返し、失敗時にエラーログが出力されること。
- [ ] すべての適用・保存操作で標準化された `[DI]` トレースログが出力されること。
- [ ] 単体テストがすべてパスし、ビルドエラー・警告増がないこと。
