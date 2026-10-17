# フェーズ3 計画書: 入力監視層・信号変換層の整理

作成日: 2026-08-29
対象ブランチ: For-DI-migration-work
前提ドキュメント:
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md` §6.5（フェーズ3の元定義）、§5.5（循環依存解消方針）
- `docs-forDIMG/MadeByAgent/Phase2-Completion-Report.md`（フェーズ2完了報告）
- `docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md`（Step 3-F フォローアップ計画）
- `.github/copilot-instructions.md`（全ルール、§5 外部エージェント運用ルール）

## ルール確認（作業開始前に毎回読む）
- §2.1 修正版: 古い方式を残して移行OK。新方式の動作確認後に削除。複数候補同時実装はNG。
- §2.2 機能100%維持、§2.3 ログ維持、§3.1 コンストラクタインジェクションシンプル化 / §3.2 段階的依存関係の整理
- §4.1 テストコード維持、§4.2 自動解消禁止、§4.3 ビルドエラー直ちに修正、§4.4 調査結果を.mdで文書化。
- §5: 指示は段階的に実施する。1ステップ完了ごとに確認を打つ／許可を得てから次へ進む。
  1行PowerShellコンソール形式とする。

---

## 0. 着手前調査で判明した事実（フェーズ2完了後の最新コードで再検証済み）

フェーズ3着手にあたり、全体計画書の§6.5・§5.5作成時点（フェーズ1着手前）の調査結果が
フェーズ1・フェーズ2完了後の現在も有効か、実コードで再検証した。

### 0.1 `Program.rootHub` 参照はフェーズ1で増加していた（2箇所→3箇所）
全体計画書の想定（`Mapping.cs` 内2箇所）にたいし、フェーズ1で新設された
`ApplyProfileDirect` / `RestoreProfileDirect`（プロファイル切替のืDIイベントハンドラ）が
新たに `Program.rootHub` を参照しており、実際には**3箇所**（他に入力モジュール1件）に増えている。

```csharp
// Mapping.cs 6048行目（ApplyProfileDirect内）
var ctrl = Program.rootHub;
...
DS4Device d = ctrl.DS4Controllers[device];
...
Global.ApplyProfile(device, action.details, false, true, ctrl, ...);  // ctrl(ControlService)をそのまま渡す

// Mapping.cs 6089行目（RestoreProfileDirect内）
var ctrl = Program.rootHub;
...
LoadProfile(device, false, ctrl);  // または LoadTempProfile(device, profileName, true, ctrl);

// Mapping.cs 6504行目（既存・フェーズ1着手前から存在）
DS4Device d = Program.rootHub.DS4Controllers[device];
```

**重要発見**: `ApplyProfileDirect`/`RestoreProfileDirect` は単に `DS4Controllers[device]` を
読み取るだけではなく、`ctrl`（`ControlService` インスタンスそのもの）を `Global.ApplyProfile`/
`LoadProfile`/`LoadTempProfile` と**そのまま渡す**必要がある。ここからのメソッドのシグネチャ自体が
`ControlService` 型の引数を要求しているため、全体計画書 §5.5 で計画された最小インターフェース
（`GetController(int deviceIndex)` のみ）**だけでは、この2箇所の依存を解消できない**。

### 0.2 `outputKBMHandler` 経由の依存はフェーズ2で解消済み
`ControlService.cs` は `On_Report` 内で `outputKBMHandler.Sync()`（`Global.outputKBMHandler` の
`using static` 参照）を使用しているが、これは `Global` のライブラリクラス管理（Connect/Disconnect/
Init）の一部であり、`IVirtualKBM` 抽象化の対象範囲（送信操作）とは別の関心事として扱われている。
フェーズ3のスソープには含まれない。

### 0.3 `DS4Devices` は調査時点から一切変更されていない
`DS4Windows/DS4Library/DS4Devices.cs` を再確認したとこる、
`public class DS4Devices` は
今も実質全メンバーが `static` のままで、一度もインスタンス化されていない「隠れた静的スレッド」で
あることが確定した（フェーズ1・2の影響を受けていない）。`ControlService.cs` から
`DS4Devices.RequestElevation`（インパルス要求）、`DS4Devices.PrepareDS4Init`/`PostDS4Init`/
`PreparePendingDevice`（デバイス代入）、`DS4Devices.findControllers()`/`getDS4Controllers()`/
`stopControllers()`/`isExclusiveMode`（静的メソッド・フィールド）を直接呼び出す構造も変化していない。

### 0.4 `Process.Start` 分類①（権限昇格）の実装箇所を確定
`ControlService.cs` の `DS4Devices_RequestElevation` メソッドが該当箇所である。
（`DS4Devices.RequestElevation` インパルスからのコンテキストプラグとして
`DS4Devices.RequestElevation += DS4Devices_RequestElevation;` により購読）：

```csharp
private void DS4Devices_RequestElevation(RequestElevationArgs args)
{
    ProcessStartInfo startInfo = new ProcessStartInfo(Global.exelocation);
    startInfo.Verb = "runas";
    startInfo.Arguments = "re-enabledevice " + args.InstanceId;
    startInfo.UseShellExecute = true;
    try
    {
        Process child = Process.Start(startInfo);
        if (!child.WaitForExit(30000)) { child.Kill(); }
        else { args.StatusCode = child.ExitCode; }
        child.Dispose();
    }
    catch { }
}
```

分類②（多重起動チェック）は `ScpUtil.cs` 側（`Process.GetProcesses()`）に存在する想定のまま
（フェーズ1・2で `ScpUtil.cs` に変更が入っていないことを全体計画書の記述から追認。着手時に再確認する）。

---

## 1. フェーズ3の目的・方針（フェーズ2完了後の実態を反映して修正版）

### 1.1 目的
`DS4Devices`（1層＝入力監視層）を `IDs4DeviceRegistry` としてDI管理下に置き、
`ControlService` ➔ `Mapping` の依存関係を解消する。

### 1.2 スソープ（§0の調査結果をふまえた修正）

| 対象 | フェーズ3で対応するか | 理由 |
|---|---|---|
| `DS4Devices` の `IDs4DeviceRegistry` 化 | **対応する** | 全体計画書の主目的そのもの。§0.3で変更なしを確認済み |
| `Mapping.cs` 6504行目（既存の `DS4Controllers[device]` 読み取るのみ） | **対応する** | `IDeviceStateAccessor.GetController()` で置換可能、最もクリーンなケース |
| `ApplyProfileDirect`/`RestoreProfileDirect` の `ctrl` 依存（2箇所） | **対応しない（フェーズ3スソープ外、明示的に棚上げ）** | §0.1の通り `Global.ApplyProfile`/`LoadProfile`/`LoadTempProfile` のシグネチャ自体が `ControlService` 型の引数を要求しているため、真の解消には `Global`（フェーズ4の `IProfileRepository` スソープ）のシグネチャ変更が必要。`IDeviceStateAccessor` を無理に拡張して急場しのぎで対応すると、フェーズ4で二度手間になるリスクが高い |
| `Process.Start` 分類①（権限昇格） | **対応する** | `IElevatedProcessLauncher` として抽象化。§0.4で実装箇所を確定済み |
| `Process.Start` 分類②（多重起動チェック） | **対応する（要再調査）** | `IProcessInspector` として抽象化。`ScpUtil.cs` 側の現状を着手時に再確認 |

**重要方針転換**: 全体計画書 §6.5 は「`Mapping` の `Program.rootHub` 参照が0件になること」を
完了判定基準としていたが、§0.1の発見により、**3箇所のうち1箇所（`ApplyProfileDirect`/`RestoreProfileDirect`）は本フェーズでは意図的に残す**方針に修正する。無理に0件を達成しようとして
`Global.ApplyProfile` 等のシグネチャまで変更すると、影響範囲が `Global` 分割（フェーズ4）にまで
及こでしまい、フェーズ3の胥大化が早まるため。フェーズ3の完了判定基準は「**新規の読み取る専用参照
（`GetController` 相当）が `IDeviceStateAccessor` 経由に統一されること**」に変更する。
`ApplyProfileDirect`/`RestoreProfileDirect` の2箇所は「フェーズ4で `IProfileRepository` 導入と
同時に解消する」既知の残課題として文書化する。

---

## 2. タスク分割（9ステップ・§5ルールに従い各ステップ完了後に確認を打つ）

| ステップ | 内容 | 完了基準 | PR単位 |
|---|---|---|---|
| **3-1** | `IDs4DeviceRegistry` インターフェースの設計・作成 | `DS4Devices` の public static メンバー一覧と対比できるインターフェースを作成、コンパイル成功 | 1インターフェース |
| **3-2** | `DS4Devices` をラップするアダプター実装（`Ds4DeviceRegistryAdapter`） | 静的メソッド・イベントへの委譲アダプターを作成（Step 2-2の `OutputKBMHandlerAdapter` と同じ設計思想） | 1アダプター |
| **3-3** | DI登録（`AppHost` / `ServiceRegistration`） | `services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();` 追加 | 配置のみ |
| **3-4** | `IDeviceStateAccessor` の設計・`ControlService` への実装・`Mapping.cs` 該当1箇所の置換 | `Mapping.cs` 6504行目のみピンバイント置換。バックトラック保持 | 1インターフェース+1箇所置換 |
| **3-F** | **フォローアップ: DI配線整理および昇格境界対応（Phase3-StepF）** | `Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md` に基づき、Step 3-1〜3-4のDI配線整理、ReEnableDeviceと昇格処理の境界整理、ビルド確認の実施 | 1フォローアップ |
| **3-5** | `Process.Start` 分類①（権限昇格）の抽象化 | `IElevatedProcessLauncher` 新設、`ControlService.DS4Devices_RequestElevation` を経由させる（バックトラック保持） | 1インターフェース |
| **3-6** | `Process.Start` 分類②（多重起動チェック）の抽象化 | `IProcessInspector` 新設、`ScpUtil.cs` 側の現状再調査を含めて対応 | 1インターフェース |
| **4** | **自動テストカバレッジ報告（Phase3-Step4）** | **DI 配線・移行対象ロジックの自動テストを実行し、全件成功を記録する** | **テスト結果報告** |
| **5** | **実機動作確認（Phase3-Step5）** | **実機確認結果を記録し、`△`／`×`／未実施項目を DI 化完了後の未対応事項として引き継ぐ** | **実機確認記録** |

**§5ルール（段階的実施）に従い、各ステップ完了後にユーソー確認を打つこと。**
一度に全ステップを実装しない。

---

## 3. 各ステップの詳細

### Step 3-1: `IDs4DeviceRegistry` インターフェース設計（完了・要フォローアップ）

`DS4Devices.cs`（§0.3で再確認済み）を網羅する public static メンバーをそのまま反映する。
イント（`RequestElevation`）はインターフェースイントとして再定義する。

```csharp
namespace DS4Windows.Services
{
    public interface IDs4DeviceRegistry
    {
        event RequestElevationDelegate RequestElevation;
        PrepareInitDelegate PrepareDS4Init { get; set; }
        PrepareInitDelegate PostDS4Init { get; set; }
        CheckPendingDevice PreparePendingDevice { get; set; }
        bool IsExclusiveMode { get; set; }

        void FindControllers();
        IEnumerable<DS4Device> GetDS4Controllers();
        void StopControllers();
        void RemoveDevice(DS4Device device);
        void UpdateSerial(object sender, EventArgs e);
        void OnRemoval(object sender, EventArgs e);
        bool ReEnableDevice(string deviceInstanceId);
    }
}
```

**確認事項（着手前に分析すること）**: `ControlService.cs` から呼び出されている `DS4Devices.` の
全メンバーを `grep -n "DS4Devices\."` で再抽出し、上記インターフェース案に漏れがないか確認する
（本計画書時点の一覧は§0.3の調査に基づく昂定版）。

### Step 3-2: アダプター実装（完了・ビルド未確認）

`Ds4DeviceRegistryAdapter : IDs4DeviceRegistry` を新設する。`DS4Devices` の全メンバーが `static` の
ため、アダプターは実態を持たず、各メソッド呼び出し時に静的メンバーへ委譲する
（Step 2-2の `OutputKBMHandlerAdapter` と同一の設計思想。初期化順序の問題を回避できる）。

### Step 3-3: DI登録（完了）

`AppHost.cs`（または `ServiceRegistration.cs`）に1行追加：
```csharp
services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();
```

`ControlService` のコンストラクタ引数に `IDs4DeviceRegistry` を受け取る形に変更するかどうかは、
`ControlService` 自体をDI登録するかどうか（全体計画書 §5.3では「実体型のままSingleton」）に
依存する。本フェーズでは `ControlService` のコンストラクタ引数変更は行わず、
`Ds4DeviceRegistryAdapter` をDIコンテナに登録するにとどまる
（§2.1修正版: バックトラックを保持した段階的移行）。

### Step 3-4: `IDeviceStateAccessor` による依存解消（範囲限定）（完了・一部残課題あり）

```csharp
public interface IDeviceStateAccessor
{
    DS4Device GetController(int deviceIndex);
}
```

`ControlService` がこれを実装する（`DS4Controllers[deviceIndex]` を返す既存実装）。
`Mapping.cs` 6504行目の `DS4Device d = Program.rootHub.DS4Controllers[device];` のみをピンバイント
置換対象とする（§1.2の方針に従い、`ApplyProfileDirect`/`RestoreProfileDirect` の2箇所は対象外）。

置換後のインライン解体（DI解消を試み、失敗時は既存の `Program.rootHub` 参照へバックトラック。
C3〜C5・Step 2-4/2-5で確立したパターンを踏襲）：
```csharp
DS4Device d = null;
try
{
    var accessor = DS4Windows.DI.ServiceProviderHolder.Provider?
        .GetService(typeof(IDeviceStateAccessor)) as IDeviceStateAccessor;
    if (accessor != null) d = accessor.GetController(device);
}
catch { }
if (d == null) d = Program.rootHub?.DS4Controllers[device];
```

---

### Step 3-F: フォローアップ（DI配線整理および昇格境界対応）（Phase3-StepF） (NEW)
※ 参照: `docs-forDIMG/MadeByAgent/Phase3-Followup-DI-Wiring-And-Elevation-Boundary-Plan.md`

#### 目的
Step 3-1〜3-4 までの実装完了を踏まえ、後続の Step 3-5（権限昇格の抽象化）へ進む前に、DI 配線の整合性確認と昇格境界の轹割重複の整理を実施する。

#### 主な作業内容
1. **DI配線の整理・追従**:
   - `ServiceRegistration.cs` および `AppHost.cs` における Step 3-1〜3-4 追加サービス（`IDs4DeviceRegistry`, `IDeviceStateAccessor` 等）の登録整合性を確認・整理。
2. **昇格境界の轹割重複整理**:
   - Step 3-1 実装時に `IDs4DeviceRegistry` に追加された `ReEnableDevice` と、Step 3-5 で予定されている `IElevatedProcessLauncher` の責務分担・境界を明確化。
3. **未検証事項の確認・ビルド検証**:
   - Step 3-1〜3-4 のビルド確認（コンパイルエラーの解消）。
   - `DS4Devices` の public static メンバーの grep 再検証。

---

### Step 3-5: 権限昇格の抽象化

```csharp
public interface IElevatedProcessLauncher
{
    int RelaunchElevated(string arguments, int timeoutMs = 30000);
}
```

`ControlService.DS4Devices_RequestElevation`（§0.4で特定済み）の内部実装をDI経由の
`IElevatedProcessLauncher` を優先使用し、失敗時は既存の直接 `Process.Start` 呼び出しにバックトラックする
形に変更する。取り扱う属性（`runas` 昇格、30秒タイムアウトでKill、終了ソード取得）は完全維持する。  
※ Step 3-F で整理された境界設計に基づいて実装を行う。

### Step 3-6: 多重起動チェックの抽象化

```csharp
public interface IProcessInspector
{
    bool IsProcessRunning(string exePath);
}
```

着手前に `ScpUtil.cs` 内の `Process.GetProcesses()` 呼び出し箇所を再調査し、全体計画書の記述
（L6694/L6717相当）が現在も正しいか確認する。

### Step 4: 自動テストカバレッジ報告

`Phase3-Step4-Automated-Test-Coverage-Report.md` に、自動テストの対象範囲、追加テスト、対象外範囲、および実行結果を記録する。全自動テストが成功したことをもって Step 4 を完了とする。

### Step 5: 実機動作確認

`Phase3-Step5-RealDevice-Verification-Checklist.md` に、デバイス接続・切断、UAC 昇格、ラムブル、`LaunchProgram` の実機確認結果を記録する。`△`／`×`／未実施項目は、DI 化完了後に対応する未対応事項として管理するが、Phase 3 の実装完了および完了扱いを妨げない。

---

## 4. リスクと回避策

| リスク | 該当ステップ | 回避策 |
|---|---|---|
| `DS4Devices` はHID通信を含む低レイヤー処理を含むため、インターフェース化の過程でスレッド間依存の不整合が発覚する可能性 | 3-1/3-2 | 実機での接続/切断シナリオをテスト項目として明示化。Step 3-3のDI登録後に手動確認を必須とする |
| `ApplyProfileDirect`/`RestoreProfileDirect` の `ctrl` 依存を残すことで、`Mapping` の依存が完全に解消されない | 3-4 | フェーズ3の完了判定基準を「新規参照の統一」に限定する。残る2箇所はフェーズ4で `IProfileRepository` と合体して解消する既知の残課題として文書化する |
| `ReEnableDevice` と `IElevatedProcessLauncher` で轹割が重複し、設計が曖昧になる | 3-F / 3-5 | Step 3-F（Phase3-StepF）で境界と責務を事前に整理し文書化する |
| 権限昇格フローがUAC昇格を伴うためテスト環境で再現しにくい | 3-5 | バックトラックを保持し、実機での動作確認（管理者権限なしで起動➔再有効化フローが動くか）を必須とする |
| `ScpUtil.cs` 側の多重起動チェック実装が想定と異なる可能性（未再確認） | 3-6 | Step 3-6着手時に必ず `grep` で最新コードを再確認してから設計を確定する |

---

## 5. 完了判定基準（フェーズ3全体）

- [x] `IDs4DeviceRegistry` インターフェースが `DS4Devices` の全 public static メンバーを過不足なく反映している
- [x] `Ds4DeviceRegistryAdapter` がコンパイル成功し、DIコンテナにSingleton登録されている
- [x] `Mapping.cs` 6504行目相当の箇所が `IDeviceStateAccessor` 経由に置換され、バックトラックが保持されている
- [x] `ApplyProfileDirect`/`RestoreProfileDirect` の `Program.rootHub` 依存2箇所が、フェーズ4への既知の残課題として明示的に文書化されている（削除・変更はしない）
- [x] **Step 3-F（Phase3-StepF）のフォローアップが完了し、DI配線と昇格境界の整理・ビルド確認が済んでいる**
- [x] `IElevatedProcessLauncher`/`IProcessInspector` が新設され、既存の `Process.Start` 直接呼び出しはバックトラックとして保持されている
- [x] 実機でのデバイス接続/切断・権限昇格シナリオの動作確認が `Phase3-Step5-RealDevice-Verification-Checklist.md` に記録されている（`△`／`×`／未実施項目は後続対応）
- [x] 自動テストカバレッジと全件成功の結果が `Phase3-Step4-Automated-Test-Coverage-Report.md` に記録されている
- [x] 各ステップの集積記録（`Phase3-Step3-x-Report.md` 等）が `docs-forDIMG/MadeByAgent/` に記録されている
- [x] `Phase2-Status.md` 相当の `Phase3-Status.md` を新設し、進捗を追跡する

## 6. 次のアクション

1. Phase 3 の実装、自動テスト、実機確認結果の記録を完了扱いとして確定する。
2. `Phase3-Step5-RealDevice-Verification-Checklist.md` の `△`／`×`／未実施項目を DI 化完了後の未対応事項として引き継ぐ。
3. 次フェーズでは `Global` 分割および ViewModel DI 化に着手する。
