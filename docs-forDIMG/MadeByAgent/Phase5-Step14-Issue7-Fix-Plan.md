# Phase 5 - Step 14 個別計画書: Issue 7（Emulated Controller 誤表示）是正実装計画

作成日: 2026-09-11
改訂日: 2026-09-11（タスク4・5をPhase6へ移管し、本計画書のスコープ外とした旨を追記）
対象ブランチ: `For-DI-migration-work`
前提ドキュメント: `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（案1採用済み）
関連ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Plan.md`（フェーズC タスク(c)-1・(c)-2 を本計画書の内容に置き換える）
- `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Status.md`
- `docs-forDIMG/MadeByAgent/Phase6-Plan.md`（§0.2, §1, §2 Phase6-Step5／Phase6-Step6。タスク4・5の移管先）
- `docs-forDIMG/MadeByAgent/Phase6-Step5-Plan.md`（旧タスク4の個別計画書）
- `docs-forDIMG/MadeByAgent/Phase6-Step6-Plan.md`（旧タスク5の個別計画書）
- `.github/copilot-instructions.md`（3.1 Pure DI原則）

> **【2026-09-11改訂】スコープ変更のお知らせ**: 当初「任意スコープ」として本計画書に含めていた**タスク4（`MainWindow.xaml.cs` のUDP診断コマンド修正）およびタスク5（`ProfileEditor.xaml.cs` の `Reload()` 内マッピング一覧機種追従修正）は、Phase6のStep5・Step6としてそれぞれ移管され、本Phase5-Step14-Issue7の実施対象外（スコープ外）となった。** 本計画書の「必須スコープ」であるタスク1〜3・6・7には変更がなく、これらは引き続きPhase5-Step14で実施する。タスク4・5の詳細な実装内容は、移管先の個別計画書（`Phase6-Step5-Plan.md`／`Phase6-Step6-Plan.md`）を参照のこと。以下の本文中、タスク4・5に関する記述は当時の検討経緯としてそのまま残すが、実施主体はPhase6に変わっている点に留意すること。

---

## 1. 位置づけ

前回の調査報告書（`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`）にて、Issue 7 の真因は `IOutputSlotService.GetOutputDeviceType()` が `Global`/`BackingStore` と非連動の孤立配列 `_deviceTypes[]`（常に `OutContType.None`）を参照していることと特定した。対応方針として **案1（`IProfileSettingsService` への新規プロパティ追加による参照先の是正）** を採用する旨、gwin7ok 氏より承認を得た。

本計画書は、案1を実装可能なマイクロタスク単位に分解したものである。あわせて、gwin7ok 氏から提案のあったプロパティ名 `OutputDeviceType` について事前調査を行った結果を記録する。

---

## 2. プロパティ命名の調査結果

### 2.1 提案名 `OutputDeviceType` の調査

リポジトリ全文 `grep` により、`OutputDeviceType` という文字列を含む既存識別子を確認した。

```
IOutputSlotService.cs:      OutContType GetOutputDeviceType(int slotIndex);
IOutputSlotService.cs:      void SetOutputDeviceType(int slotIndex, OutContType deviceType);
OutputSlotService.cs:       public OutContType GetOutputDeviceType(int slotIndex) { ... }
OutputSlotService.cs:       public void SetOutputDeviceType(int slotIndex, OutContType deviceType) { ... }
```

### 2.2 判定: **問題あり（採用非推奨）**

提案名 `OutputDeviceType` は、まさに今回是正対象となっている **孤立バグ側のメソッド名 `GetOutputDeviceType`/`SetOutputDeviceType` とほぼ同一の文字列**である。

これを新規プロパティ名として採用すると、同一の `ProfileSettingsViewModel.cs` 内に、

- `profileSettings.OutputDeviceType`（今回新設する、正しい値を返すプロパティ）
- `outputSlotService.GetOutputDeviceType(device)`（孤立バグを抱えたまま残置されるメソッド）

という**名前が酷似した2つの別サービスのAPIが並存する**ことになる。

これはまさに、調査報告書§6.1で確認した `SpecialActionsListViewModel.cs` のコメント

```csharp
// Step13-5: IOutputSlotService.GetOutputDeviceType が既存の正規DIラッパー
```

のように、Phase5-Step13時点で「名前が近い＝正規のラッパーだろう」という誤認から孤立バグ側を採用してしまった経緯と同種の混同を、将来また誘発するリスクがある。せっかく是正しても、再発防止という調査報告書の目的（§9の推奨理由）に反する。

### 2.3 代替案: `OutContType`（採用推奨）

真の実体である `Global.OutContType`（`m_Config.outputDevType` への薄いラッパー、`ScpUtil.cs:3118`）と**全く同じ名前**を `IProfileSettingsService` 側にも採用する。

```csharp
// Global.cs（ScpUtil.cs）: 既存
public static OutContType[] OutContType => m_Config.outputDevType;
```

**採用理由**:
1. `IProfileSettingsService` の既存プロパティは概ね `Global`/`m_Config` 側のメンバ名をそのまま踏襲する命名規則になっている（例: `GyroOutputMode => _config.gyroOutMode`、`LSModInfo => _config.lsModInfo`）。`OutContType` はこの規則にそのまま合致する。
2. 委譲先（`Global.OutContType`）と委譲元（`IProfileSettingsService.OutContType`）が同一名称になることで、コード追跡性（「これはどこから来た値か」の即答性）が最大化される。
3. `IOutputSlotService` 側の孤立APIの名称（`GetOutputDeviceType`/`SetOutputDeviceType`）とは字面が明確に異なるため、§2.2の混同リスクを構造的に回避できる。

**懸念点の確認**: プロパティ名が型名 `OutContType`（enum）と同一になるが、これは `Global.OutContType`（`public static OutContType[] OutContType => ...`）で**既に採用されている既存パターン**であり、C#の言語仕様上も問題なくコンパイル可能であることをビルド実績（既存コード）から確認済み。

**本計画書では `OutContType` を採用する。** 別の名称を希望される場合は、実装着手前にご指示いただきたい。

---

## 3. 追加調査で判明した波及範囲（前回報告書からの追記）

前回報告書では `outputSlotService.GetOutputDeviceType(` の呼び出しを3箇所（`ProfileSettingsViewModel.ControllerTypeIndex`、`SpecialActionsListViewModel`、`MainWindow`）として報告したが、実装計画の詳細化にあたり `ProfileSettingsViewModel.cs` 内を再確認した結果、**同一ファイル内に2つ目の独立したプロパティ `ContType`（955行目付近）が存在し、これも同じ孤立バグを踏んでいる**ことを追加で確認した。

```csharp
public OutContType ContType
{
    get => outputSlotService.GetOutputDeviceType(device);
}
```

この `ContType` プロパティは `ProfileEditor.xaml.cs`（301行目、コンストラクタ内）で `MappingListViewModel` の初期化引数として使用されている。

```csharp
mappingListVM = new MappingListViewModel(deviceNum, profileSettingsVM.ContType);
```

したがって、本バグは「プロファイル編集画面の Emulated Controller コンボボックス表示」だけでなく、**マッピング一覧画面がエディタ起動時にどちらの機種向けボタン名で初期表示されるか**にも波及していた可能性がある（`ContType` を是正すれば、この初期化も自動的に正しい値を受け取るようになるため、追加の実装は不要）。

また、`MappingListViewModel` には `UpdateMappingDevType(OutContType)` という更新用メソッドが存在するが、これは `ProfileEditor.xaml.cs` 内のコンボボックス手動選択時のイベントハンドラ（`OutConTypeCombo_SelectionChanged`）からのみ呼ばれており、`Reload()`（同一エディタウィンドウ内で別プロファイルへ切り替える処理）では呼ばれていない。このため、**エディタを閉じずに別プロファイルへ切り替えた場合、マッピング一覧の内部状態が旧プロファイルの機種のまま残る**可能性がある。これは本Issue 7の直接の症状ではなく隣接する別経路の問題であるため、§4のタスク5として任意対応の形で提案する。

> **【2026-09-11改訂】** 本項で提案したタスク5は、後日Phase6-Step6として移管・実施することが決定した。本Phase5-Step14-Issue7としては実施対象外である。詳細は `Phase6-Step6-Plan.md` を参照。

---

## 4. 修正対象ファイル棚卸し

| # | ファイル | 変更内容 | 種別 |
| --- | --- | --- | --- |
| 1 | `DS4Windows/DI/IProfileSettingsService.cs` | `OutContType[] OutContType { get; }` を新規宣言 | インターフェース追加 |
| 2 | `DS4Windows/DS4Control/Services/ProfileSettingsService.cs` | `public OutContType[] OutContType => _config.outputDevType;` を実装 | サービス実装追加 |
| 3 | `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | `ControllerTypeIndex`・`ContType`・`UpdateLateProperties()` の3箇所を `outputSlotService.GetOutputDeviceType(device)` → `profileSettings.OutContType[device]` に置換 | 参照先修正 |
| 4 | `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs` | コンストラクタに `IProfileSettingsService profileSettings = null` を追加（Pure DI＋フォールバック方式）、253行目の参照を `profileSettings.OutContType[deviceNum]` に置換 | 参照先修正＋DI追加 |
| 5 | ~~`DS4Windows/DS4Forms/MainWindow.xaml.cs`~~ | ~~（任意・推奨）1412行目のUDP診断コマンド `outconttype` の参照を `profileSettingsService.OutContType[tdevice]` に置換~~ **【対象外・Phase6-Step5へ移管】** | 移管（Phase6-Step5） |
| 6 | ~~`DS4Windows/DS4Forms/ProfileEditor.xaml.cs`~~ | ~~（任意）`Reload()` 内、`profileSettingsVM.UpdateLateProperties();` の直後に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加し、同一エディタ内でのプロファイル切替時にもマッピング一覧の機種表示を追従させる~~ **【対象外・Phase6-Step6へ移管】** | 移管（Phase6-Step6） |
| 7 | `DS4Windows/DS4Control/Services/OutputSlotService.cs` / `IOutputSlotService.cs` | `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` に技術的負債コメント（`// TODO(Phase6候補): ...`）を付与。実装自体は変更しない | コメント追記のみ |
| 8 | `DS4WindowsTests/ProfileSettingsServiceTests.cs`（存在すれば）または新規テストファイル | 新規プロパティの単体テスト追加 | テスト追加 |
| 9 | `DS4WindowsTests/SpecialActionsListViewModelTests.cs` | 既存テストへの影響確認・必要に応じてモック調整 | テスト確認 |
| 10 | `docs-forDIMG/DI-App-Wide-Migration-Plan.md` または `docs-forDIMG/MadeByAgent/Phase6-Plan.md` | `IOutputSlotService` の4メンバ未接続を技術的負債として記録（既存の `IUdpServerService` 未接続の記載に準ずる形） | ドキュメント追記 |

> **No Feature Drop方針**: いずれの変更も「読み取り参照先の付け替え」のみであり、`BackingStore`（`m_Config`）の実データ格納先・XML構造・保存ロジック（`SetLateProperties()` 等）には一切手を加えない。

---

## 5. マイクロタスク breakdown

### タスク1（【2026-09-11実装済み】）: `IProfileSettingsService` / `ProfileSettingsService` へのプロパティ追加

- [x] `IProfileSettingsService.cs` に以下を追加:
  ```csharp
  /// <summary>
  /// プロファイルに永続化されているエミュレートコントローラー種別（&lt;OutputContDevice&gt;）。
  /// Global.OutContType（m_Config.outputDevType）への読み取り専用の薄い委譲。
  /// </summary>
  OutContType[] OutContType { get; }
  ```
- [x] `ProfileSettingsService.cs` に以下を追加:
  ```csharp
  public OutContType[] OutContType => _config.outputDevType;
  ```
- [x] 既存の配列プロパティ（`LSModInfo` 等）と同じ書式・配置箇所（プロパティ群の並び順）に揃える。（`EnableOutputDataToDS4` の直後、`UseDs3PitchRollSim` の直前に配置）

### タスク2（【2026-09-11実装済み】）: `ProfileSettingsViewModel.cs` の参照先修正（3箇所）

- [x] `ControllerTypeIndex` の switch 対象を修正:
  ```diff
  - switch (outputSlotService.GetOutputDeviceType(device))
  + switch (profileSettings.OutContType[device])
  ```
- [x] `ContType` プロパティの getter を修正:
  ```diff
  - get => outputSlotService.GetOutputDeviceType(device);
  + get => profileSettings.OutContType[device];
  ```
- [x] `UpdateLateProperties()` 内の読み取り側を修正（書き込み先の `OutDevTypeTemp[device]` はそのまま）:
  ```diff
  - outputSlotService.OutDevTypeTemp[device] = outputSlotService.GetOutputDeviceType(device);
  + outputSlotService.OutDevTypeTemp[device] = profileSettings.OutContType[device];
  ```
- [x] `outputSlotService` フィールド・コンストラクタ引数は、`OutDevTypeTemp`（Temp出力デバイス種別の一時保持）で引き続き使用するため削除しない。（`grep`で他の残存使用箇所を確認済み）

### タスク3（【2026-09-11実装済み】）: `SpecialActionsListViewModel.cs` へのDI追加と参照先修正

- [x] コンストラクタに `IProfileSettingsService profileSettings = null` を追加し、フォールバック込みで初期化:
  ```csharp
  this.profileSettings = profileSettings ?? DS4WinWPF.AppHost.GetService<IProfileSettingsService>() ?? Global.ProfileSettingsServiceInstance;
  ```
  （引数はコンストラクタの最後尾に追加し、既存の位置引数呼び出しとのソース互換を維持）
- [x] 253行目を修正:
  ```diff
  - OutContType outType = outputSlotService.GetOutputDeviceType(deviceNum);
  + OutContType outType = profileSettings.OutContType[deviceNum];
  ```
- [x] `outputSlotService` フィールドが本箇所以外で使われていないか確認し（`grep`で確認、本ファイル内では未使用となった）、フィールド自体の削除は行わず、削除要否検討用のTODOコメントを付与した（本タスクでは参照差し替えのみ）。
- [x] `SpecialActionsListViewModel` を `new` している呼び出し元（`ProfileEditor.xaml.cs` 302行目、`SpecialActionsListViewModelTests.cs` 47行目）を確認し、いずれも位置引数の数が変更前と同じか、それ以下であるため、コンストラクタ引数追加による影響がないことを確認した。

### タスク4（【2026-09-11改訂】対象外・Phase6-Step5へ移管）: `MainWindow.xaml.cs` のUDP診断コマンド修正

> **本タスクはPhase6-Step5として移管され、本Phase5-Step14-Issue7の実施対象外となった。** 以下は移管前の検討内容であり、実施は行わない（記録として残す）。詳細な実装計画は `Phase6-Step5-Plan.md` を参照。

- [ ] ~~`outconttype` クエリの参照先を `profileSettingsService.OutContType[tdevice]` に置換（`MainWindow.xaml.cs` 内で `IProfileSettingsService` が既に注入されているか確認し、未注入であればコンストラクタに追加）。~~
- [ ] ~~本タスクは影響範囲が外部UDP連携という限定的な箇所であるため、Step14本体のスコープに含めるか、次Stepへ回すかをgwin7ok氏に確認する。~~ → **次Stepへ回す方針が確定し、Phase6-Step5として移管された。**

### タスク5（【2026-09-11改訂】対象外・Phase6-Step6へ移管）: `ProfileEditor.xaml.cs` の `Reload()` 内、マッピング一覧の機種追従修正

> **本タスクはPhase6-Step6として移管され、本Phase5-Step14-Issue7の実施対象外となった。** 以下は移管前の検討内容であり、実施は行わない（記録として残す）。詳細な実装計画は `Phase6-Step6-Plan.md` を参照。

- [ ] ~~`Reload()` 内、`profileSettingsVM.UpdateLateProperties();` の直後に以下を追加:~~
  ```csharp
  // （参考・Phase6-Step6で実施予定）
  mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);
  ```
- [ ] ~~`RefreshEditorBindings()`（同様のリロード処理が存在する場合）にも同一の追加が必要か確認する。~~
- [ ] ~~本タスクは§3で新たに判明した隣接問題への対応であり、Issue 7そのものの是正には必須ではない。gwin7ok氏の判断で今回含めるか次回に回すかを決定する。~~ → **次回（Phase6-Step6）に回す方針が確定した。**

### タスク6（【2026-09-11実装済み】）: `IOutputSlotService` への技術的負債コメント付与

- [x] `IOutputSlotService.cs` の `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` 宣言部、および実装側（`OutputSlotService.cs`）に技術的負債コメントを付与した。当初案（本節記載の簡略版）から、`PluginSlot`/`UnplugSlot` が実際には `ControlService.AttachUnboundOutDev`/`DetachUnboundOutDev` を呼び出す実装を持つ（＝単純な未接続ではなく「呼出元0件の並行実装」である）という実地確認結果を反映し、4メソッドそれぞれの実態に即した内容に拡充した。
- [ ] `docs-forDIMG/DI-App-Wide-Migration-Plan.md` の技術的負債一覧（または `Phase6-Plan.md` の監査対象候補）への追記は、Phase6-Step1着手時にまとめて反映する方針とし、本タスクの範囲では未実施（コード中のコメントは付与済み）。

### タスク7（【2026-09-11一部実装・要確認】）: 単体テスト追加・全体確認

- [x] `ProfileSettingsService` の新規プロパティに対する単体テストを追加した（`OutContType_ShouldShareBackingStoreWithGlobalShim`、`DS4WindowsTests/ProfileSettingsServiceTests.cs`）。`Global.OutContType`との同一参照性、および双方向の書き込み反映を確認する内容とした。
- [x] **【2026-09-11追加実装】** `ProfileSettingsViewModel` に対する単体テストを新規作成した（`DS4WindowsTests/ProfileSettingsViewModelTests.cs`）。gwin7ok氏の指示により新規作成を実施。`PatternCViewModelTests.cs` に既存の `ViewModelFactory_ShouldCreate_ProfileSettingsViewModel`（Null チェックのみ）とは異なり、`OutContType`／`ControllerTypeIndex`／`ContType`／`UpdateLateProperties()` の連動という具体的な振る舞いを検証する3件のテストを追加した。WPFリソース読み込み（`BitmapImage`）に必要な `Application.Current` 初期化は `PatternCViewModelTests.cs` と同一のブートストラップパターンを踏襲した。
- [x] `SpecialActionsListViewModel` 関連の既存テスト（`SpecialActionsListViewModelTests.cs`）を確認した。コンストラクタ引数の追加は末尾への追加のみであり、既存テスト（`new SpecialActionsListViewModel(0, specialActionRepo, profileRepo, null, outputSlotService)`）の呼び出し箇所数は変更前と同じであるため、破壊されていないことを確認した（コード修正は不要）。
- [x] **【2026-09-11 gwin7ok氏確認済み】** `dotnet build` によるクリーンビルド確認: gwin7ok氏の環境（VS Codeウィンドウ再読み込み後）でビルド成功を確認済み。なお、最初の試行時に発生した `MC1000`（XAML生成物ファイルロック）エラーは、今回のコード変更とは無関係の環境要因（エディタのバックグラウンドプロセスによるファイルロック）であったことを確認済み。
- [x] **【2026-09-11 gwin7ok氏確認済み】** `dotnet test` による全件PASS確認: gwin7ok氏の環境でテスト実行・全件成功を確認済み。ただし、この時点では `ProfileSettingsViewModelTests.cs`（本節上部で追加実装）はまだ含まれていなかったため、同ファイル追加後の再実行結果の確認をお願いします。

---

## 6. リスクと回避策

1. **`SpecialActionsListViewModel` のコンストラクタ変更による既存呼び出し元への影響**: 新規引数はオプション引数（デフォルト`null`＋フォールバック）とするため、既存の `new SpecialActionsListViewModel(device)` 等の呼び出しはソース互換を維持する。
2. **`outputSlotService.OutDevTypeTemp` との役割混同の防止**: `OutDevTypeTemp`（プロファイルエディタ上の未確定選択値）と、今回追加する `OutContType`（永続化済みの確定値）は意味的に異なる。タスク1のXMLコメントで明確に区別を記載する。
3. **タスク4・5は任意項目 → Phase6へ移管**: Issue 7本体の是正（タスク1〜3）とは独立して着手可否を判断できるよう、当初明確に分離して記載していたが、【2026-09-11改訂】この分離を活かす形でタスク4・5はそれぞれPhase6-Step5／Phase6-Step6として移管することが確定した。これにより本Phase5-Step14-Issue7は「必須スコープ」（タスク1〜3・6・7）のみで完結する。

---

## 7. 完了条件

- [x] `IProfileSettingsService` / `ProfileSettingsService` に `OutContType` プロパティが追加され、`Global.OutContType`（`m_Config.outputDevType`）と同一の参照であることが単体テストで保証されている。
- [x] `ProfileSettingsViewModel.cs` の `ControllerTypeIndex`・`ContType`・`UpdateLateProperties()` が新プロパティ経由に置換されている。
- [x] `SpecialActionsListViewModel.cs` が `IProfileSettingsService` を注入され、253行目が新プロパティ経由に置換されている。
- [ ] **【要gwin7ok氏確認】** `<OutputContDevice>DS4</OutputContDevice>` を持つプロファイルを開いた際、`Emulated Controller` コンボボックスが正しく `DS4` を表示すること（実機での確認が必要。単体テストでは`OutContType_ShouldShareBackingStoreWithGlobalShim`によりデータ経路の同一性は保証済み）。
- [ ] **【要gwin7ok氏確認】** Special Action 一覧のボタン名表示が、DS4プロファイルで空欄にならず正しいDS4名称で表示されること（実機での確認が必要）。
- [ ] **【要gwin7ok氏再確認】** 全単体テストがクリーンにPASSし、ビルド警告・エラーが増加していないこと。ビルド成功・既存テスト全件PASSは確認済み（2026-09-11）。本節上部で追加した `ProfileSettingsViewModelTests.cs` を含めた再実行の確認をお願いします。
- [ ] ~~（タスク4・5を実施する場合）該当箇所の動作確認が完了していること。~~ **【2026-09-11改訂】タスク4・5はPhase6-Step5／Phase6-Step6へ移管されたため、本計画書の完了条件からは除外する。**それぞれの完了条件は移管先の個別計画書（`Phase6-Step5-Plan.md` §7、`Phase6-Step6-Plan.md` §8）で管理する。

---

## 8. 進行ルール

- 【2026-09-11改訂】本計画書のスコープはタスク1〜3・6・7の「必須スコープ」のみとする。当初「任意スコープ」としていたタスク4・5は、それぞれPhase6-Step5／Phase6-Step6として移管され、本Phase5-Step14-Issue7の実施対象外となった。着手前にこの移管内容についてもgwin7ok氏の最終確認を得る。
- 巨大ファイル編集時はピンポイント編集ルールを徹底し、不要な空白・改行差分を混入させない。
- 実装完了後、`Phase5-Step14-FormSettings-Unification-Plan.md` フェーズCのタスク(c)-1・(c)-2の記載を本計画書の内容に基づき改訂する。
- タスク4・5（Phase6移管分）の進捗管理は、本計画書ではなく `Phase6-Status.md` のStep5／Step6欄で行う。