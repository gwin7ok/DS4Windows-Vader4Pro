# Phase7-Step2 完了報告書: DI インターフェースの 0 件メンバーの個別判断

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step2-Plan.md`（各 Wave の記録は §8）  
状態: 実装完了。W1〜W6 のビルド（警告 0）・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行）。W7 の結果と、実アプリでの確認（U-11）は待ち。

---

## 1. 結果の要約

- 走査で、本体の呼び出しが 0 件の DI 契約のメンバーを 61 件見つけ、うち 12 件（`Actions/` の未配線の抽象）を Phase8 へ移管した。残りを、`.github/copilot-instructions.md` §2.4 に従い、経緯と「本来使うべきか」を調べて個別に判断した。
- 判断の結果:
  - **削除 56 メンバー**（`Phase7Step2RemovedContractMembersGuardTests` で固定）。計画の 49 のうち 45 件に、Wave 内の再確認で見つけた 11 件を加えた（`ResetToDefaults`、`RemoveAction`、`GetAction`、`ActionExists`、`SaveActions`、`IPathService.ActionsPath`、`IDs4DeviceRegistry.IsHidHideInstalled`、`RumbleAutostopTime`、`IVirtualKBM.Version`、`IDeviceStateService` の関連 3 件）。
  - **接続（呼び出し側の切り替えが未実施。TODO 付きで残した）**: `IEnvironmentService.UsingMaxControllers`（Step10）、`IDs4DeviceRegistry.ReEnableDevice`（Step4）、`IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（Step5）、`IUdpServerService` の契約全体（Step5 以降）。
  - **温存**: `IOutputSlotService.OutputSlotChanged`（Step6 で再判断）。
- 削除したファイル: `DeviceStateChangedEventArgs.cs`、`ProfileSettingChangedEventArgs.cs`、`ControllersViewModel.cs`。
- ラチェット基準値（全域）: `Global.*` 732 → **723**（Step1 完了時 733）、`AppHost` **40**、`Program.rootHub` **22**、`ServiceProviderHolder` **23**（いずれも不変）。

## 2. Wave ごとの実績

- W1: `IProfileSettingsService` の重複アクセサ 17 名（色、配列、スクロール感度ほか）。
- W2: `ProfileSettingChanged` 系（イベント、Wire/Unwire、記述子、GUI ログ `[DI] ProfileSettingsService.SettingChanged: …`）。実装 168 行の削除。購読者は Phase5-Step14 で撤去済みだった。
- W3: `IDeviceStateService` のデバイス系 9 メンバー（常に空を返す孤立実装）と、使われない `ControllersViewModel`。
- W4: `IOutputSlotService`（孤立配列）、`IVirtualKBM.ErrorMessage`（上流から常に空）、`IUdpServerService.ListenAddress`、`INotificationService`、`IPathService.GetAutoProfilesPath`。`UsingMaxControllers` は調査で「接続」に変更した。
- W5: `IProfileRepository` 4 名、`ISpecialActionRepository` の `AddAction`／`ReplaceAction`／`GetActionIndex`（メモリ上だけの並行実装）、`IDs4DeviceRegistry` の 2 名。`ReEnableDevice` は「接続」。
- W6: `RemoveAction`（メモリ上のみ）と `ResetToDefaults`（`LoadDefaultProfile` の削除で呼び出し元が 0 件）。TODO、ガードテスト、Phase8 申し送り、台帳。型を意識した再走査で、追加候補 7 件を発見。
- W7: 追加候補のうち 6 件を削除、`IUdpServerService` に TODO。

## 3. 調査で判明した重要事項

- **「契約だけ作って、呼び出し側の切り替えが未実施」だったもの**（削除してはいけなかったもの）: `UsingMaxControllers`（`ControlService.USING_MAX_CONTROLLERS` の静的シムの置換先。呼び出し 3 箇所が未切り替え）、`ReEnableDevice`、`Load/SaveOutputSlots`（同じ処理が `App`／`ControlService` に二重に存在）、`IUdpServerService`（`ControlService` が `UdpServer` を直接 new）。
- **名前の衝突による走査の漏れ**: 名前だけで数える走査は、同名の別の型のメンバー（`Global.RemoveAction`、`ProfileActions.ActionsChanged` ほか）の参照を、当該契約の参照と誤認する。W6 で、受け手の変数名による型を意識した再走査を行った。**限界**: 参照が 3 件以上のメンバーは抜き取り確認にとどまる。見落としの可能性は残る。
- **誤った値を返す孤立実装**: `DeviceStateService`（内部配列が一度も設定されない）、`OutputSlotService`（常に null）、`IVirtualKBM.ErrorMessage`（2020 年の上流から一度も代入されない）。
- **回帰ではない**: 削除した契約は、いずれも本体の呼び出しが 0 件で、機能・ログ・安全弁への影響は無い。唯一のログの変更は、承認済みの Q2（`ProfileSettingChanged` 系の GUI ログ）と、`SetOutputDevice` の Trace ログ（メソッドごと削除）。

## 4. テストの変更

- 追加: `Phase7Step2RemovedContractMembersGuardTests`（3 件）。
- 削除したテスト（旧シムの確認だけで、目的が消えた）: `ProfileSettingsServiceSubSettingsTests`（ファイルごと）、`ProfileUnifiedPersistenceTests`（ファイルごと）、`DeviceStateServiceTests`（ファイルごと）、`ProfileSettingsServiceTests` の通知・リセット、`PatternBViewModelTests` の `ControllersViewModel` 2 件、`Step10GroupTwoGlobalReferenceGuardTests` の 1 件、`OutputSlotServiceTests` 3 件、`NotificationServiceTests` 3 件、`SpecialActionRepositoryTests` の `ReplaceAction`・`RemoveAction`・通知、`Phase6Step10GroupThreeContractTests.RemoveAction_InMemoryOnly_DoesNotChangeFile`、`ProfileRepositoryTests.LoadDefaultProfile_…`、`ProfileSettingsServiceReverseX360ButtonMappingTests` のコピー版の確認、ほか各テストの該当アサーション。
- 書き換え: 配列プロパティの直接参照への置き換え（`ProfileSettingsServiceTests`、`ProfileRepositoryTests`）、`ActionList.Add` による準備（`SpecialActionRepositoryTests`）、`Global.Notifications` への置き換え（`NotificationServiceTests`）。
- 件数の増減は、ユーザー側の実行結果と照合すること（実行結果の件数は未記録）。

## 5. 未完了・持ち越し

- **U-11（実アプリでの確認）**: 起動→接続→メイン画面→出力スロット画面の Plug／Unplug（チェックリスト §11 に記録済み。未）。
- 接続の実装: `UsingMaxControllers`（Step10）、`ReEnableDevice`（Step4）、`Load/SaveOutputSlots`（Step5）、`IUdpServerService`（Step5 以降の別計画書）。温存の判断: `OutputSlotChanged`（Step6）。
- Step10 で再判断: `ProfileSubSettingBase.OnSubPropertyChanged`（購読者がいなくなった）、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`。
- Phase8 へ: `Actions/` の未配線の抽象 12 メンバーと Service Locator 17 件（`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記済み）。

## 6. 次の Step

- **Step3: `BackingStore` の DI 化と `Global` の転送の削除**（`Phase7-Plan.md` §2）。3a: `BackingStore` を DI のシングルトンとして登録（実体は `Global.store` と同一）。3b: 純粋な転送の約 90 名を `Global` から削除。3c: イベントとプロファイル適用・プリセットの移管。
