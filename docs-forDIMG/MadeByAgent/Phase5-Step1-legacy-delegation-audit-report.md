# Phase5-Step1調査レポート: DIサービス内部 Legacy 経路の詳細監査

作成日: 2026-09-02
最終更新日: 2026-09-03（コード全体追加調査結果を反映）
対象ブランチ: `For-DI-migration-work`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Phase5計画書: `docs-forDIMG/MadeByAgent/Phase5-Plan.md`
Phase5進捗書: `docs-forDIMG/MadeByAgent/Phase5-Status.md`
基準となった前回監査: `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md`

---

## 1. 目的と対象範囲

Phase4完了時点で、DIの「入口」（インターフェース経由の呼び出し）は整備されたが、サービス実装の「内部」が依然として `Global` 静的クラス、`Program.rootHub`、あるいはそれに準ずる他の静的クラス（`Mapping`、`ActionManager`、`ActionFactory`、`ActionRegistry`、`DS4Devices` 等）へ再委譲している箇所が残っていることが判明した。

本レポートは、以下の2段階で網羅的な監査を実施した結果をまとめる。
1. **第1次監査**: `DS4Windows/DI/ServiceRegistration.cs` に登録されている**全DIサービス（ViewModel群を除く23項目）**を対象に、実装クラス内部のLegacy再委譲を棚卸し。
2. **第2次監査（追加調査）**: DI登録済みサービスの枠を超え、**アプリケーション全体（バックグラウンド自律実行系、全体設定永続化、UI層のViewModel内部業務ロジック）**に潜む静的アクセス・Singleton直参照のブラインドスポットを棚卸し。

---

## 2. 監査結果一覧（第1次監査: DI登録済み23サービス）

`services.AddSingleton` / `AddTransient` で登録された23サービスを全数確認した。

| # | インターフェース | 実装クラス | 判定 | Legacy参照の内容 |
|---|---|---|---|---|
| 1 | `IActionFactory` | `DefaultActionFactory` | クリーン | Adapter生成のみ。Legacy参照なし。 |
| 2 | `IManagedActionManager` | `DefaultActionManager` | **Legacy委譲** | 静的 `ActionManager.SetToggledOn`／`FireToggledOnChanged`、静的 `ActionFactory.CreateFrom`、`Mapping.ClearKeyButtonControllersForDevice`、`ActionRegistry.AllActions` への依存。 |
| 3 | `IKeyActionCreator` | `DefaultKeyActionCreator` | クリーン | `new KeyAction(...)` のみ。 |
| 4 | `IKeyButtonActionControllerFactory` | `DefaultKeyButtonActionControllerFactory` | クリーン | DIコンテナ経由で `IControllerRegistry` を解決しているのみ。 |
| 5 | `IControllerRegistry` | `DefaultControllerRegistry` | クリーン | `ConcurrentDictionary` によるインメモリ管理のみ。 |
| 6 | `IProfileSettingsService` | `ProfileSettingsService` | **Legacy委譲** | `Program.rootHub.DS4Controllers[]` 参照（`GetRumbleBoost`／`SetRumbleAutostopTime`）、`_config = Global.store`。 |
| 7 | `IProfileRepository` | `ProfileRepository` | **Legacy委譲** | `Global.LoadProfile`／`Global.SaveProfile`／`Global.ProfilePath`／`Global.appdatapath`。 |
| 8 | `IProfileActionProvider` | `ProfileActionProvider` | **Legacy委譲（新規）** | `Global.getProfileActions`／`Global.GetProfileAction`。プロファイル紐づきAction解決。 |
| 9 | `IProfileActionChainService` | `ProfileActionChainService` | **Legacy委譲（新規）** | 静的 `Mapping.DispatchProfileActionEdge` へ委譲。連鎖アクション処理。 |
| 10 | `IProfileApplicationService` | `ProfileApplicationService` | **Legacy委譲** | `Global.ApplyProfile`／`Global.LoadTempProfile`／`Global.CompleteProfileApplication`、`Mapping.TakePendingRestoreProfileName`。 |
| 11 | `IProfileSwitcher` | `DefaultProfileSwitcher` | **Legacy委譲（新規）** | `Global.ApplyProfile`（3箇所）、`Program.rootHub`（2箇所）。#10と重複。 |
| 12 | `ISpecialActionRepository` | `SpecialActionRepository` | **Legacy委譲** | `Global.LoadActions`／`Global.SaveActions`（実データ `BackingStore.actions` との二重管理・非同期バグあり）。 |
| 13 | `IPathService` | `PathService` | Legacy参照（軽微） | `Global.appdatapath` の読み取り・キャッシュ（起動時競合リスクあり）。 |
| 14 | `IEnvironmentService` | `EnvironmentService` | クリーン（要確認） | Legacy参照なし。永続化経路の検証要。 |
| 15 | `INotificationService` | `AppNotificationService` | クリーン（要確認） | Legacy参照なし。購読側の実装確認要。 |
| 16 | `IDeviceStateService` | `DeviceStateService` | クリーン | インメモリ配列管理のみ。 |
| 17 | `IDs4DeviceRegistry` | `Ds4DeviceRegistryAdapter` | **Legacy委譲（新規）** | 静的 `DS4Devices` クラスへ全操作委譲（デバイス検出・列挙）。 |
| 18 | `ControlService` | `ControlService` | 対象外 | `Program.rootHub` の後継実体そのもの。 |
| 19 | `DS4Windows.Services.IDeviceStateAccessor` | `ControlService`へ委譲 | 仕様通り | Phase3で確認済みのFactory-delegate登録パターン。 |
| 20 | `IOutputSlotService` | `OutputSlotService` | クリーン | インメモリ管理（ただし `OutputSlotManager` 実体は rootHub に依存）。 |
| 21 | `IElevatedProcessLauncher` | `DefaultElevatedProcessLauncher` | Legacy参照（軽微） | `Global.exelocation` の読み取りのみ。 |
| 22 | `IProcessInspector` | `DefaultProcessInspector` | クリーン | Phase3 Step3-6で移設済み。 |
| 23 | `IViewModelFactory` | `ViewModelFactory` | クリーン | DI登録済みサービス経由でViewModelを生成するのみ。 |

---

## 3. Phase5初期Step（Step2〜6）との対応関係

| Step | 対象サービス | 本調査での確認結果 |
|---|---|---|
| Step2（プロファイルXML読込・保存） | `IProfileRepository` | 一致（#7）。`IProfileXmlStore` を新設し戻り値 `bool` 化を先行確定。 |
| Step3（プロファイル適用・復帰） | `IProfileApplicationService` | 一致（#10）。`IProfileSwitcher`（#11）を統合し `Program.rootHub` 参照を完全排除。 |
| Step4（Save/Apply操作結果と通知） | 横断的整流化 | 一致。通知引数を `bool?` としサービス内部で自動解決する方針を策定。 |
| Step5（SpecialAction永続化） | `ISpecialActionRepository` | 一致（#12）。`BackingStore.actions` との二重管理・非同期バグを特定、調査先行で修正。 |
| Step6（残存サービス境界） | `IPathService`／`IProfileSettingsService` | 一致。`PathService` キャッシュ撤廃、`IDeviceStateAccessor` 活用、KBM調査を確定。 |

---

## 4. 新規発見事項（Step2〜6でカバーされない機能）

### 4-1. プロファイルアクション解決・連鎖処理（#8, #9）
- `IProfileActionProvider` と `IProfileActionChainService`（静的 `Mapping.DispatchProfileActionEdge` 依存）。
- 巨大ファイル `Mapping.cs` を解体せず `IMappingActionDispatcher` で薄く境界化して解決（Step7）。

### 4-2. Actions基盤の静的委譲（#2）
- `IManagedActionManager`（`DefaultActionManager`）が、静的 `ActionManager`／`ActionFactory`／`ActionRegistry` に依存（Step8）。

### 4-3. デバイス検出・列挙の静的委譲（#17）
- `IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）が、静的 `DS4Devices` クラスへ全委譲（Step9）。

### 4-4. IProfileSwitcherとProfileApplicationServiceの機能重複（#11）
- `DefaultProfileSwitcher` が `Global.ApplyProfile`／`Program.rootHub` を独自直呼び出し。Step3に統合して解消。

### 4-5. 【追加調査結果】コード全体・DI未登録コンポーネントにおける4大ブラインドスポット
第1次監査（登録済み23サービス）の枠外でコード全体を精査した結果、以下の4領域で静的アクセス・Singleton直参照が残存していることが判明した。

1. **バックグラウンド自律実行系（AutoProfile & UdpServer）**:
   - `AutoProfileChecker.cs` / `AutoProfileHolder.cs` はバックグラウンドでウィンドウを監視しプロファイルを自動切替するが、DIコンテナの管理外で動作し `Global.ApplyProfile` や `Program.rootHub` を直接呼んでいる。
   - `UdpServer.cs`（モーション・パット外部配信）が `Program.rootHub.DS4Controllers` や `Global` を直接ポーリング・参照している。
2. **アプリ全体設定（AppSettings）の永続化と状態管理**:
   - `Profiles.xml` 内の `<AppSettings>`（スタートアップ起動、トレイ最小化、通知設定、UDPポート等）の保存・読込（`Global.SaveSettings` / `LoadSettings`）がDI化されておらず、専用契約（`IAppSettingsService` 等）が存在しない。
3. **アクション実行・出力スロット層の内部委譲**:
   - `DefaultMacroPlayer.cs` がキー入力送出のために静的 `Global.outputKBMHandler` や `Mapping` に依存している。
   - `OutputSlotService.cs` の内部実装が `Program.rootHub.outputSlotManager` に直結しており、スロット永続化（`OutputSlotPersist.cs`）も静的ファイルI/Oを行っている。
4. **UI層（ViewModels）内部に残存する直接静的参照（Phase4未達分）**:
   - Phase4 で ViewModel Factory が導入されたが、`ControllersViewModel.cs`、`SettingsViewModel.cs`、`ProfileSettingsViewModel.cs` などの内部業務ロジックに依然として `Global.ProfilePath[...]` や `Program.rootHub.DS4Controllers` への直アクセスが残っている。

---

## 5. 既知課題（Legacy委譲ではないが要確認）

- `INotificationService`（#15）／`IEnvironmentService`（#14）: Legacy委譲はないが、実環境での購読・反映経路の確認が必要。
- `OutputKBMHandlerAdapter.cs`: 実装が存在するが `ServiceRegistration.cs` に登録されておらず、静的 `Global.outputKBMHandler` が多用されている。

---

## 6. Step再構成案（本レポートに基づく提案）

第1次監査（Step7〜9新設）および第2次追加調査（Step10〜13新設）を統合し、Phase5 を全15ステップとして再構成する。

```
【確定版 Phase5 ロードマップ】
Phase5-Step1: 詳細監査と優先度付け【完了】
Phase5-Step2: プロファイル XML 読込・保存（IProfileXmlStore新設・bool化）
Phase5-Step3: プロファイル適用・復帰（IProfileApplicationService一本化）
Phase5-Step4: Save／Apply の操作結果と通知（通知自動解決・[DI]ログ統一）
Phase5-Step5: SpecialAction 永続化（BackingStore.actions一本化）
Phase5-Step6: 残存サービス境界（PathServiceキャッシュ安全化・ProfileSettings・KBM調査）
Phase5-Step7: プロファイルアクション解決・連鎖処理（IMappingActionDispatcher新設）
Phase5-Step8: Actions基盤（ActionManager / ActionFactory / ActionRegistry の整理）
Phase5-Step9: デバイス検出・列挙（Ds4DeviceRegistry / DS4Devices の整理）
Phase5-Step10【新設】: AutoProfile（自動プロファイル切替）の自律実行系DI化
Phase5-Step11【新設】: アプリ全体設定（AppSettings）の永続化・状態管理のDI化
Phase5-Step12【新設】: アクション実行・出力スロット層の内部委譲（DefaultMacroPlayer & OutputSlot）
Phase5-Step13【新設】: UI層（ViewModels）のDIサービス接続・残存静的参照の撲滅
Phase5-Step14（旧Step7/10）: 自動テストと実機検証（統合検証）
Phase5-Step15（旧Step8/11）: Legacy shim の削除判断
```

---

## 7. 次のアクション

1. `Phase5-Plan.md` を上記全15ステップ構成に更新し、各Stepの責務と境界を確定する。
2. 承認済みの `Phase5-Step2-Plan.md` に基づき、Step2（プロファイル XML 読込・保存の責務分離）の実装に着手する。
