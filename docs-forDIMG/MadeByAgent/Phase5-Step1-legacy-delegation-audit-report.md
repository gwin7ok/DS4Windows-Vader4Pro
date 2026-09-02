# Phase5-Step1調査レポート: DIサービス内部 Legacy 経路の詳細監査

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Phase5進捗書: `docs-forDIMG/MadeByAgent/Phase5-Status.md`
基準となった前回監査: `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md`

---

## 1. 目的と対象範囲

Phase4完了時点で、DIの「入口」（インターフェース経由の呼び出し）は整備されたが、サービス実装の「内部」が依然として `Global` 静的クラス、`Program.rootHub`、あるいはそれに準ずる他の静的クラス（`Mapping`、`ActionManager`、`ActionFactory`、`ActionRegistry`、`DS4Devices` 等）へ再委譲している箇所が残っていることが判明した。

本レポートは、`DS4Windows/DI/ServiceRegistration.cs` に登録されている**全DIサービス（ViewModel群を除く23項目）**を対象に、GitHub上の実コード（`refs/heads/For-DI-migration-work`）を直接読み込み、Legacy委譲の有無・内容を棚卸しした結果をまとめる。

**調査方法**: `Github MCP:get_file_contents` を用い、各サービスの実装ファイルを個別に取得し、`Global.`／`Program.rootHub`／その他の静的クラス（`Mapping.`／`ActionManager.`／`ActionFactory.`／`ActionRegistry.`／`DS4Devices.`）への参照有無を目視で確認した。

**対象外としたもの**: ViewModel群（`SettingsViewModel` 等、Phase4のPattern A/B/C整理で既に扱われているため）、`DS4WinWPF.ArgumentParser`（POCO）、`ControlService` 自体（`Program.rootHub` の後継実体そのものであり、監査対象である「`Global`／`rootHub`への再委譲」には該当しないため）。

---

## 2. 監査結果一覧

`services.AddSingleton` / `AddTransient` で登録された23サービスを全数確認した。

| # | インターフェース | 実装クラス | 判定 | Legacy参照の内容 |
|---|---|---|---|---|
| 1 | `IActionFactory` | `DefaultActionFactory` | クリーン | Adapter生成のみ。Legacy参照なし。 |
| 2 | `IManagedActionManager` | `DefaultActionManager` | **Legacy委譲** | 静的 `ActionManager.SetToggledOn`／`FireToggledOnChanged`、静的 `ActionFactory.CreateFrom`（DI版 `IActionFactory` とは別の同名静的クラス）、`Mapping.ClearKeyButtonControllersForDevice`、`ActionRegistry.AllActions`／`GetByIndex`／`Count` への依存。トグル状態管理とアクションディスパッチの中核ロジックが静的クラス群に残存。 |
| 3 | `IKeyActionCreator` | `DefaultKeyActionCreator` | クリーン | `new KeyAction(...)` のみ。 |
| 4 | `IKeyButtonActionControllerFactory` | `DefaultKeyButtonActionControllerFactory` | クリーン | `ServiceProviderHolder`（DIコンテナ自体）経由で `IControllerRegistry` を解決しているのみ。`Global`直参照なし。 |
| 5 | `IControllerRegistry` | `DefaultControllerRegistry` | クリーン | `ConcurrentDictionary` によるインメモリ管理のみ。 |
| 6 | `IProfileSettingsService` | `ProfileSettingsService` | **Legacy委譲** | `Program.rootHub.DS4Controllers[]` 参照（`GetRumbleBoost`／`SetRumbleAutostopTime`）、`_config = Global.store`（`BackingStore` を共有）、`Global.defaultButtonMapping`／`Global.reverseX360ButtonMapping`。（Phase4引継ぎ済み・Step6想定） |
| 7 | `IProfileRepository` | `ProfileRepository` | **Legacy委譲** | `Global.LoadProfile`／`Global.SaveProfile`／`Global.ProfilePath`／`Global.appdatapath`。（Phase4引継ぎ済み・Step2想定） |
| 8 | `IProfileActionProvider` | `ProfileActionProvider` | **Legacy委譲（新規）** | `Global.getProfileActions`／`Global.GetProfileAction`。プロファイルに紐づくSpecialAction名の解決処理。 |
| 9 | `IProfileActionChainService` | `ProfileActionChainService` | **Legacy委譲（新規）** | 静的 `Mapping.DispatchProfileActionEdge` へ委譲。SpecialAction発火後の連鎖アクション処理。`IProfileActionProvider` に依存するため8と一体的。 |
| 10 | `IProfileApplicationService` | `ProfileApplicationService` | **Legacy委譲** | `Global.ApplyProfile`／`Global.LoadTempProfile`／`Global.LoadProfile`／`Global.CompleteProfileApplication`／`Global.ProfilePath`、`Mapping.TakePendingRestoreProfileName`。（Phase4引継ぎ済み・Step3想定） |
| 11 | `IProfileSwitcher` | `DefaultProfileSwitcher` | **Legacy委譲（新規）** | `Global.ApplyProfile`（3箇所）、`Program.rootHub`（2箇所）、`Global.ProfilePath`。SpecialAction経由のプロファイル切替・復帰を担当し、`ProfileApplicationService`（#10）と機能領域が重複。 |
| 12 | `ISpecialActionRepository` | `SpecialActionRepository` | **Legacy委譲** | `Global.LoadActions`／`Global.SaveActions`。（Phase4引継ぎ済み・Step5想定） |
| 13 | `IPathService` | `PathService` | Legacy参照（軽微） | `Global.appdatapath` の読み取りのみ（初期値取得であり書き込みや複雑な再委譲はない）。（Step6想定） |
| 14 | `IEnvironmentService` | `EnvironmentService` | クリーン（要確認） | Legacy参照なし。ただし設定値の永続化・復元を行う経路がコード上に見当たらず、実際に機能しているか別途確認が必要。 |
| 15 | `INotificationService` | `AppNotificationService` | クリーン（要確認） | Legacy参照なし。`NotificationTriggered` イベントを発火するのみで、実際に購読してトースト等を表示する側の実装が今回の調査範囲内では確認できなかった。 |
| 16 | `IDeviceStateService` | `DeviceStateService` | クリーン | インメモリ配列管理のみ。 |
| 17 | `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Legacy委譲（新規）** | 全メソッド・プロパティが静的 `DS4Devices` クラスへ完全委譲。`Global` ではないが同型のLegacy委譲パターン。デバイス検出・列挙・削除処理。 |
| 18 | `ControlService`（実体） | `ControlService` | 対象外 | `Program.rootHub` の後継実体そのもの。監査対象（Global/rootHubへの委譲）には該当しない。 |
| 19 | `DS4Windows.Services.IDeviceStateAccessor` | `ControlService`へ委譲 | 仕様通り | Phase3で確認済みのFactory-delegate登録パターン。 |
| 20 | `IOutputSlotService` | `OutputSlotService` | クリーン | インメモリ配列管理のみ。 |
| 21 | `IElevatedProcessLauncher` | `DefaultElevatedProcessLauncher` | Legacy参照（軽微） | `Global.exelocation` の読み取りのみ。Phase3 Step3-5で意図的に移設されたロジック。 |
| 22 | `IProcessInspector` | `DefaultProcessInspector` | クリーン | Phase3 Step3-6で移設済み。Legacy参照なし。 |
| 23 | `IViewModelFactory` | `ViewModelFactory` | クリーン | DI登録済みサービス経由でViewModelを生成するのみ。 |

---

## 3. Phase5既存Step（Step2〜6）との対応関係

| Step | 対象サービス | 本調査での確認結果 |
|---|---|---|
| Step2（プロファイルXML読込・保存） | `IProfileRepository` | 一致（#7）。追加発見なし。 |
| Step3（プロファイル適用・復帰） | `IProfileApplicationService` | 一致（#10）。**ただし `IProfileSwitcher`（#11）が同一機能領域で別経路のLegacy委譲を行っており、Step3のスコープに含めて統一すべきと判断**（詳細は4章）。 |
| Step4（Save/Apply操作結果と通知） | （Step2・Step3の実装後に着手） | 現時点でコード変更なし。既存整理方針のまま。 |
| Step5（SpecialAction永続化） | `ISpecialActionRepository` | 一致（#12）。追加発見なし。 |
| Step6（残存サービス境界） | `IPathService`／`IProfileSettingsService`／デバイス状態／KBMアダプター | `IPathService`（#13）・`IProfileSettingsService`（#6）は一致。「デバイス状態」は `IDeviceStateService`（#16、クリーン）であり対象外。「KBMアダプター」に該当する実装は今回のDI登録一覧からは確認できなかった（`OutputKBMHandlerAdapter.cs` は存在するがDI未登録の可能性。要確認）。 |

---

## 4. 新規発見事項（Step2〜6でカバーされない機能）

以下は既存Step2〜6のいずれにも該当しない、独立したLegacy委譲経路である。

### 4-1. プロファイルアクション解決・連鎖処理（#8, #9）
- `IProfileActionProvider`（`Global.getProfileActions`／`Global.GetProfileAction`）
- `IProfileActionChainService`（静的 `Mapping.DispatchProfileActionEdge`）
- 両者は依存関係が密（ChainServiceがProviderを利用）であり、1つの責務分離単位として扱うのが妥当。

### 4-2. Actions基盤の静的委譲（#2）
- `IManagedActionManager`（`DefaultActionManager`）が、トグル状態管理（`ActionManager.SetToggledOn`／`FireToggledOnChanged`）、アクション生成（静的`ActionFactory.CreateFrom`）、アクション一覧管理（`ActionRegistry`）という3つの異なる静的クラス群に委譲している。
- 特に「静的`ActionFactory`」と「DI登録された`IActionFactory`→`DefaultActionFactory`」が別物として共存している点は、命名の紛らわしさもあり要注意。

### 4-3. デバイス検出・列挙の静的委譲（#17）
- `IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）が、デバイス検出・取得・削除・シリアル更新等の全操作を静的`DS4Devices`クラスへ委譲している。
- `Global`ではないが、構造的には同型のLegacy委譲パターン。

### 4-4. IProfileSwitcherとProfileApplicationServiceの機能重複（#11）
- `IProfileSwitcher`（`DefaultProfileSwitcher`）は、SpecialAction経由のプロファイル切替・復帰時に独自に`Global.ApplyProfile`／`Program.rootHub`を直接呼び出している。
- Phase5-Plan.md Step3は「通常GUI切替、編集画面Save／Apply、SpecialAction、AutoProfileのすべてが同じ適用契約を使用することを目標とする」と明記しており、`IProfileSwitcher`のSpecialAction切替経路はまさにここに含まれるべき対象。**新規Stepとしてではなく、Step3のスコープに含めることを提案する。**

---

## 5. 既知課題（Legacy委譲ではないが要確認）

- `INotificationService`（#15）: 通知発火イベントの実際の購読・表示先が本調査範囲では未確認。機能していない可能性、または別ファイル（未調査範囲）で購読されている可能性がある。
- `IEnvironmentService`（#14）: 設定値の永続化・復元経路が見当たらない。同様に別ファイルでの補完実装がある可能性。
- Step6で想定されていた「KBMアダプター」が、現在のDI登録一覧（`ServiceRegistration.cs`）には見当たらない。`OutputKBMHandlerAdapter.cs` は存在するため、DI未登録のまま使用されている可能性がある（Step6実施時に要確認）。

これらはLegacy委譲問題とは性質が異なるため、Step再構成の対象にはせず、該当Step実施時の確認事項として記録するに留める。

---

## 6. Step再構成案（本レポートに基づく提案）

「Step2〜6のいずれにも該当しない機能」として、以下3つの新規Stepを提案する。

| Step | 名称 | 対象 |
|---|---|---|
| Step7（新規） | プロファイルアクション解決・連鎖処理の責務分離 | `IProfileActionProvider`／`IProfileActionChainService`（4-1） |
| Step8（新規） | Actions基盤（ActionManager）の静的委譲分離 | `IManagedActionManager`（4-2） |
| Step9（新規） | デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離 | `IDs4DeviceRegistry`（4-3） |
| Step10（旧Step7） | 自動テストと実機検証 | 変更なし、番号のみ繰り下げ |
| Step11（旧Step8） | Legacy shimの削除判断 | 変更なし、番号のみ繰り下げ |

`IProfileSwitcher`（4-4）は新規Stepとせず、既存Step3のスコープに統合することを提案する。

---

## 7. 次のアクション

1. 本レポートの内容についてご確認・ご承認をいただく。
2. 承認後、`Phase5-Plan.md`（Step構成の更新：Step7〜9の追加、Step3範囲の拡張、旧Step7・Step8の番号繰り下げ）を生成する。
3. 続けて `Phase5-Status.md` を同様に更新する。
4. 新規Step7・Step8・Step9それぞれについて、個別の詳細計画書（`Phase5-Step7-Plan.md` 等）を1つずつ生成し、都度確認をいただきながら進める。