# Phase7-Step2 計画書: DI インターフェースの 0 件メンバーの個別判断

作成日: 2026-10-17  
対象ブランチ: `For-DI-migration-work`  
前提: `Phase7-Plan.md`（Step2 の位置づけ）、`Phase7-0-Survey.md` §1.6、`.github/copilot-instructions.md` §2.4（未参照コードの扱い）  
状態: **調査完了・判断案の提示（承認待ち）。実装はこの計画書の承認後に Wave 単位で行う。**  
ルール: ビルド・テストビルド・テスト実行・コミットはユーザーが行う。エージェントはローカルファイルの編集のみ。

---

## 0. 結論（先に）

- 対象は 61 メンバー（本体の呼び出しが 0 件）。うち **12 は Phase8 へ移管**（`Actions/` の未配線の抽象）。Phase7 で判断するのは **49**。
- 判断案（49）: **削除 45／接続 3（後続 Step で呼び出し側を切り替える）／温存 1**。
- 削除の理由は 3 種類に分かれる（§2.4 の「使うべきでないもの」）:
  - 重複: 同じ値を返す別の入口があり、本体はそちらを使っている（約 27）。
  - 孤立した実体: サービス内の配列が本体で一度も設定されず、常に空・null・既定値を返す（`IDeviceStateService`、`IOutputSlotService`、`IVirtualKBM.ErrorMessage`。約 13）。
  - 購読者のいない通知: 購読側を撤去済みのイベントの仕組み（`ProfileSettingChanged` 系。3）。
  - 残りは並行実装（メモリだけに足す `AddAction` など。数件）。
- 「使うべきなのに未接続」と判断したもの: `Load/SaveOutputSlots`（Step5）、`ReEnableDevice`（Step4）。

## 1. 調査の方法

- 走査: `scripts/audit/Step12/iface.py`（インターフェースの宣言ファイルと実装クラスのファイルを除く、本体の参照が 0 件。名前で数えるため、0 件の判定は確実）。
- 各メンバーについて、(1) `git log -S` による導入の経緯、(2) 実装の本体、(3) 同じ値を本体が別の入口から取っているか、(4) テストの参照、を確認した。
- 判断基準（§2.4）: 「本来その API を使うべきか」。使うべきなら削除せず TODO を付けて接続を計画に入れる。使うべきでなければ削除する。

## 2. 判断の一覧

凡例: **削** 削除／**接** 接続（後続 Step で呼び出し側を切り替える）／**温** 温存／**P8** Phase8 へ移管。「テスト」はテストからの参照数。

### 2.1 IProfileSettingsService（21）

| メンバー | テスト | 判断 | 理由 |
|---|---|---|---|
| `GetLowColor`／`GetChargingColor`／`GetFlashColor`／`GetCustomColor`／`GetUseCustomLed` | 0 | 削 | Step10-2-A-5（2026-09-02）で追加。本体の `DS4LightBar` は `GetLightbarSettingsInfo` が返す `LightbarSettingInfo` を直接読む（コメントに旧名が残るだけ）。重複 |
| `GetDS4CSettings` | 0 | 削 | 単数の `GetDS4CSetting` が 69 箇所で使われている。重複 |
| `GetGyroMouseDeadZone` | 1 | 削 | 本体は配列 `GyroMouseDeadZone[device]` を使う。重複 |
| `GetScrollSensitivity`（2 オーバーロード） | 0 | 削 | 本体は `ScrollSensitivity[device]` を使う。重複 |
| `GetUseDInputOnly`／`SetUseDInputOnly` | 2／1 | 削 | 本体は `DinputOnly`、`GetDInputOnly`、`UseDInputOnlyArray` を使う。重複 |
| `GetTempProfileDistance` | 3 | 削 | 本体は `TempProfileDistanceArray`、`SetTempProfileDistance` を使う。重複 |
| `GetReverseX360ButtonMapping` | 1 | 削 | `Clone()` を返す版。本体はゼロアロケーションの `ReverseX360ButtonMapping` を使う（インターフェースの注記のとおり）。書き換える呼び出し元は無い |
| `SAMouseStickTriggerCond`（配列） | 1 | 削 | 本体は `GetSAMouseStickTriggerCond`／`SetSaMouseStickTriggerCond` を使う。重複 |
| `SetTouchpadActive` | 5 | 削 | 本体は `TouchpadActiveArray[device]`（`ControlService`）と `GetTouchpadActive` を使う。重複 |
| `LinkedProfileCheckArray`／`TempProfileNameArray` | 1／1 | 削 | 本体は `Get/SetLinkedProfileCheck`、`Get/SetTempProfileName` を使う。重複 |
| `ConfigDecimalCulture` | 1 | 削 | 本体の DTO は `Global.configFileDecimalCulture`（静的。XML のシリアライザーが使う）を使う。契約側は孤立（`new CultureInfo("en-US")` を別に持つ） |
| `ResetAllToDefaults` | 0 | 削 | 呼び出し元なし。スロット別の `ResetToDefaults` は残る。プリセットの読み込みが別経路（`ProfilePresetLoader`）で担う |
| `ProfileSettingChanged`／`WireSubSettingsEvents`／`UnwireSubSettingsEvents` | 10／10／1 | 削（要承認、§3 Q2） | Phase5-Step14（2026-09-16〜17）で追加した、サブ設定の変更の中継。唯一の購読者（プロファイル編集画面の適用ボタンの活性化）は、同 Step で撤去済み（適用ボタンは常時有効）。`Wire` の呼び出しも、本体には残っていない。購読者のいない仕組み |

### 2.2 IDeviceStateService（6）と周辺

| メンバー | テスト | 判断 | 理由 |
|---|---|---|---|
| `SetDevice`、`DeviceStateChanged`、`IsDeviceConnected`、`GetDeviceMacAddress`、`GetConnectionType`、`GetBatteryLevel` | 4／1／4／2／0／2 | 削 | 2026-08-31 に追加。サービス内の配列 `_devices` は、本体で `SetDevice` が呼ばれず、常に空。接続中のデバイスの実体は `ControlService.DS4Controllers`（`IDeviceStateAccessor` が公開）。誤った値（常に未接続）を返す孤立実装 |
| （関連）`Devices`、`GetDevice`、`ConnectedControllersCount` | — | 削（同じ理由。§3 Q3） | 本体の使用は `ControllersViewModel.HasControllers` だけで、そのプロパティも使用箇所が無い |
| （関連）`ControllersViewModel` | 2 | 削（要承認、§3 Q3） | `HasControllers` だけを持つ殻。DI に登録されているが、使う画面・クラスが無い。テスト 2 件（`PatternBViewModelTests` の解決と Singleton の確認）は削除 |

残す: `IsFirstConnection`、`MarkConnected`、`ResetConnectionFlags`、`OnDeviceSerialChange`（本体で使用中）。

### 2.3 IOutputSlotService（6）

| メンバー | テスト | 判断 | 理由 |
|---|---|---|---|
| `OutputDevices`、`GetOutputDevice`、`IsSlotPlugin`（と実装の `SetOutputDevice`、配列 `_outputDevices`） | 0／3／1 | 削 | 2026-08-31 に追加。`SetOutputDevice` の呼び出しは、`UnplugSlot` が null を入れるだけ。常に null／false を返す孤立配列。実体の入口は `OutputSlots`／`GetOutSlotDevice`（`OutputSlotManager` 連動） |
| `LoadOutputSlots`／`SaveOutputSlots` | 1／1 | **接**（Step5） | 2026-09-05 に永続化の窓口として追加。現状は、`App.xaml.cs:541` が `rootHub.LoadPermanentSlotsConfig()`、`ControlService:702` が `OutputSlotPersist.WriteConfig` を直接呼び、同じ処理が二重に存在する。`rootHub` の削除（Step5）で、サービス経由へ切り替える。TODO を付ける（§4） |
| `OutputSlotChanged` | 3 | 温（Step6 で再判断） | `PluginSlot`／`UnplugSlot` の実行後に発行されるが、本体の購読者はいない。出力スロット画面の更新が `OutputSlotManager` 側のイベントで足りているかは未確認。所有権の移動（Step6）で、`outDevType` の通知と合わせて判断する。TODO を付ける |

### 2.4 その他（各 1〜4）

| インターフェース.メンバー | テスト | 判断 | 理由 |
|---|---|---|---|
| `IDs4DeviceRegistry.ConnectedDevices`／`DeviceCount` | 0 | 削 | `GetDS4Controllers()` と同じ値。重複 |
| `IDs4DeviceRegistry.ReEnableDevice` | 0 | **接**（Step4） | 2026-09-20 に Actions/ から移動した契約。`App.xaml.cs:712` が `DS4Devices.reEnableDevice` を静的に直接呼ぶ。Pre-Host の経路（コンテナ構築前）のため、起動の近代化（Step4）で切り替えられるか判断する。TODO を付ける |
| `IEnvironmentService.UsingMaxControllers` | 2 | 削 | `ControllerSlotLimit` との比較で置き換えられる。本体の使用なし |
| `INotificationService.NotificationsEnabled`／`FlashTaskbar` | 7／5 | 削 | 設定の読み書きは `IAppSettingsService.Notifications`／`FlashWhenLate` が担う。重複（`NotificationsEnabled` は実装内の `SendNotification` が使うため、実装には private として残す） |
| `IPathService.GetAutoProfilesPath` | 0 | 削 | プロパティ `AutoProfilesPath` と同じ値。重複。副次: `AutoProfileHolder`、`AutoProfiles.xaml.cs` が `"Auto Profiles.xml"` を文字列連結している（置換は Step4 の候補） |
| `IProfileRepository.GetProfileNames`／`ProfileExists`／`LoadProfileToSlot`／`LoadDefaultProfile` | 0／0／0／1 | 削 | 2026-08-31 の初期契約。本体は `LoadProfile`（11 箇所）、プリセットは `ProfilePresetLoader`、一覧は UI の `ProfileList` が担う。重複 |
| `ISpecialActionRepository.AddAction`／`ReplaceAction`／`GetActionIndex` | 5／1／0 | 削 | `AddAction`／`ReplaceAction` はメモリ上の一覧だけを書き換える並行実装で、本体の保存経路（`SaveAction`＝ファイルへ保存して再読込）と別。`GetActionIndex` は実装内部（`RemoveAction` など）が使うため、実装には残し、契約からのみ外す |
| `IUdpServerService.ListenAddress` | 1 | 削 | 本体は `IAppSettingsService.UdpServerListenAddress` を読む。重複 |
| `IVirtualKBM.ErrorMessage` | 1 | 削 | `VirtualKBMBase.errorMessage` は 2020 年の上流のコミットから、**一度も代入されず**、常に空文字を返す。アダプター（`OutputKBMHandlerAdapter`）の実装と契約のみ外す（上流の基底クラスのフィールドは変更しない） |

### 2.5 Phase8 へ移管（12）

- `IActionBinding`（`OnReleased`、`OnTriggered`、`Outputs`）、`IKeyController`（`ClearAll`、`OnDown`、`OnUp`）、`IActionRegistry.GetBindingsForDevice`、`IControllerRegistry.GetControllersForDevice`、`IInputAction.Evaluate`、`IMacroPlayer.IsPlaying`（テスト 11）、`ITrigger.Context`、`IOutputHandler.FakeKeyRepeat`。
- Phase6-Step9b から Phase8 へ申し送り済みの、未配線の抽象（系統 A）。Phase8 の台帳（`Phase8-Unified-Trigger-Dispatch-Plan.md`）に追記する。Phase7 では触らない。

### 2.6 集計

- 削除 45: `IProfileSettingsService` 21、`IDeviceStateService` 6、`IOutputSlotService` 3、`IDs4DeviceRegistry` 2、`IEnvironmentService` 1、`INotificationService` 2、`IPathService` 1、`IProfileRepository` 4、`ISpecialActionRepository` 3、`IUdpServerService` 1、`IVirtualKBM` 1。
- 接続 3（Step4 に 1、Step5 に 2）。温存 1（Step6 で再判断）。Phase8 へ 12。

## 3. ご判断をお願いしたい事項（選択肢と推奨）

- **Q1: 全体の方針** — 上の判断案（削除 45／接続 3／温存 1）で進めてよいか。推奨: 承認。
- **Q2: `ProfileSettingChanged` 系の削除（3 メンバーと、仕組み一式）** — 削除すると、`OnProfileSettingChanged` が出している GUI ログ（`[DI] ProfileSettingsService.SettingChanged: Slot …`）も無くなる（Trace 相当の GUI ログ）。§2.3 の「ログの維持」に当たるため、明示的な承認が要る。
  - 案 A（推奨）: 削除する。購読者がなく、設定の変更の追跡は、適用・保存の経路のログで足りる。
  - 案 B: イベントだけ削除し、ログを出す箇所は残す（変更の度に GUI ログが出る現状を維持）。
  - 案 C: 温存して Phase8 以降で、変更の追跡（Dirty フラグ）が必要になったときに再判断する。
- **Q3: `ControllersViewModel`（殻）と `IDeviceStateService` のデバイス系の削除** — 推奨: 削除（`HasControllers` の使用箇所なし。デバイスの実体は `IDeviceStateAccessor`）。テスト 2 件（`PatternBViewModelTests`）と、`DeviceStateServiceTests` の該当ケースを削除または書き換える。
- **Q4: `OutputSlotChanged` の温存** — 推奨: 温存して Step6 で判断。
- **Q5: 接続 3 件の TODO の付与** — 推奨: 付与（§4）。

## 4. 接続・温存の項目へ付ける TODO（copilot-instructions §3.3 原則4）

- `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`: 「作られた経緯（2026-09-05、永続化の窓口）、未接続の理由（`App.xaml.cs` が `rootHub.LoadPermanentSlotsConfig` を直接呼ぶ。`ControlService` も `OutputSlotPersist` を直接呼ぶ）、接続予定（Phase7-Step5: `rootHub` 削除時）、現状の差（二重の呼び出し）」。
- `IDs4DeviceRegistry.ReEnableDevice`: 「経緯（2026-09-20）、`App.xaml.cs:712` が `DS4Devices.reEnableDevice` を静的に呼ぶ（Pre-Host）、予定（Phase7-Step4）」。
- `IOutputSlotService.OutputSlotChanged`: 「購読者なし。Phase7-Step6 で再判断」。

## 5. Wave 計画（各 Wave ＝ 1 コミット。ビルド・テストはユーザーが実行）

- **W1: `IProfileSettingsService` の重複アクセサ 18 名の削除**（§2.1 の `ProfileSettingChanged` 系を除く）。契約・実装（`ProfileSettingsService`）・テストの書き換え（該当テストは、生存する入口へ書き換えるか、重複の確認だけのテストは削除）。
- **W2: `ProfileSettingChanged` 系 3 名と仕組み一式の削除**（Q2 の案による）。`SubSettingDescriptor`、`_subSettingsUnwireMap`、`OnSubSettingChanged`、`OnProfileSettingChanged` の発火、`ProfileSettingChangedEventArgs`（他に使われない場合）。テスト約 11 件の削除または書き換え。
- **W3: `IDeviceStateService` のデバイス系と `ControllersViewModel` の削除**（Q3）。`DeviceStateService` の配列・イベント、`DeviceStateChangedEventArgs`（他に使われない場合）、DI 登録、テスト。
- **W4: 小さな契約の整理** — `IOutputSlotService` の 3 名（と `_outputDevices`、`SetOutputDevice`）、`IVirtualKBM.ErrorMessage`（と `OutputKBMHandlerAdapter`）、`IUdpServerService.ListenAddress`、`INotificationService` の 2 名、`IEnvironmentService.UsingMaxControllers`、`IPathService.GetAutoProfilesPath`。
- **W5: `IProfileRepository` 4 名、`ISpecialActionRepository` 3 名、`IDs4DeviceRegistry` 2 名の削除**。
- **W6: 接続・温存の TODO の付与、ガードテスト、台帳**
  - TODO（§4）。
  - ガード: 新規 `Phase7Step2RemovedContractMembersGuardTests`（削除した名前が、DI のインターフェースに再び現れないこと。残す契約の名前が残っていること）。
  - 台帳: `Phase7-Residual-Ledger.md` と `Phase7-Status.md` の更新。Phase8 の台帳へ、12 メンバーを追記。モデル図 `03-Class-Interface-Diagram.md` と `04-Service-Lifecycle-Spec.md` のインターフェースの記述を、削除に合わせて更新。

各 Wave の共通作業:
- 削除の直前に、`iface.py` で参照が 0 件であることを再走査する（コメントを除く）。
- 動的な参照（リフレクション、XAML の Binding、`nameof`）が無いことを確認する。
- ラチェットの基準値（`Global.*`／`AppHost`／`Program.rootHub`）は、削除で減ったファイルの分だけ更新（減る方向のみ）。
- 既存のコメント（設計意図・TODO）を消さない。

## 6. リスク

- 削除の対象は、本体の呼び出しが 0 件。ただし、テストの書き換えが多い（`ProfileSettingsService` 系、`DeviceStateServiceTests`、`OutputSlotServiceTests`、`PatternBViewModelTests` ほか）。旧シムと新サービスの等価性だけを確認するテストは削除、生存する入口で確認できるものは書き換える（Step12 と同じ方針）。
- W2 は、`ProfileSubSettingBase` のイベント（`OnSubPropertyChanged`）自体は、Step14 のサブ設定のクラスが持つ。それは削除しない（購読側の仕組みだけを削除する）。
- 名前が同じ別のメンバー（例: `Devices`）を誤って削除しない。削除はインターフェースごとに、型を明示して行う。
- 実機確認: 本体の呼び出しが 0 件の削除なので、実機の項目は追加しない。W3（`ControllersViewModel`）と W4（`IOutputSlotService`）の後に、起動・接続・出力スロットの付け外しを、Step2 完了時に 1 回確認する。

## 7. 完了判定チェックリスト

- [ ] W1〜W7 のビルド（警告 0）・テストビルド・テスト実行が成功している（W1〜W6 は成功。W7 は結果待ち）。
- [x] 削除 56 メンバー（計画の 49 のうち削除 45、W5・W6・W7 の追加分を含む）が、契約・実装・テストから消えている（`Phase7Step2RemovedContractMembersGuardTests`）。
- [x] 接続・温存の契約に、TODO が付いている（`UsingMaxControllers`、`ReEnableDevice`、`Load/SaveOutputSlots`、`OutputSlotChanged`、`IUdpServerService`）。
- [x] Phase8 の台帳に、12 メンバーと Service Locator 17 件が追記されている。
- [x] `Phase7-Residual-Ledger.md`、`Phase7-Status.md`、モデル図が更新されている。
- [ ] 起動・接続・出力スロットの付け外しの実機確認（チェックリスト §11 の U-11。未実施）。
- [x] `Phase7-Step2-Completion-Report.md` が作成されている。

---

## 8. 承認と実施記録

### 承認（2026-10-17）
- Q1〜Q5 をすべて推奨どおり承認（Q2＝案 A: `ProfileSettingChanged` 系は GUI ログごと削除、Q3＝削除、Q4＝温存、Q5＝TODO 付与）。

### W1 の実施記録（2026-10-17、ビルド・テストはユーザーの実行待ち）
- 削除（契約 `IProfileSettingsService` から 19 行、実装 `ProfileSettingsService` から 17 メンバー）: `GetLowColor`／`GetChargingColor`／`GetCustomColor`／`GetFlashColor`／`GetUseCustomLed`、`GetDS4CSettings`、`GetGyroMouseDeadZone`、`GetScrollSensitivity`（2 オーバーロード）、`GetUseDInputOnly`／`SetUseDInputOnly`、`GetTempProfileDistance`、`GetReverseX360ButtonMapping`（コピー版）、`SAMouseStickTriggerCond`、`SetTouchpadActive`、`ConfigDecimalCulture`、`ResetAllToDefaults`。
- 契約のみ削除（実装は W2 まで残す）: `TempProfileNameArray`、`LinkedProfileCheckArray`。実装の `OnProfileSettingChanged(…, nameof(…))` が参照しているため。W2 で、イベントの仕組みとあわせて実装も削除する。
- 実装から消した `Set…`／`Get…` は、サービス内の同じ配列（`_touchpadActive`、`_useDInputOnly`、`_tempProfileDistance`）を扱っていたため、本体が使う配列プロパティ（`TouchpadActiveArray` など）と状態は同一。挙動は変わらない。
- `ReverseX360ButtonMapping` のコピー版は、Phase6-Step3-6a の決定 R1 で「書き換える呼び出し元のため」に残していたが、呼び出し元が 0 件のため削除した。契約の説明文を更新した。
- テストの書き換え:
  - `ProfileSettingsServiceTests`: 削除したメンバーの呼び出しを、配列プロパティの直接参照に置き換えた。`ConfigDecimalCulture`、`TempProfileNameArray`／`LinkedProfileCheckArray` の長さの確認は削除した。イベントのテストは `SetUseTempProfile` を使う形に変更した（W2 で削除予定）。範囲外の安全確認は `SetUseTempProfile(99, …)` に置き換えた。
  - `ProfileRepositoryTests`: 2 箇所を配列プロパティの参照に置き換えた。
  - `ProfileSettingsServiceReverseX360ButtonMappingTests`: コピー版の確認テスト 1 件を削除した。
- ラチェット: `ProfileSettingsService.cs` の `Global.*` 5 → 4。全域は `Global.*` 733 → 732（走査で確認）。`AppHost` 40、`Program.rootHub` 22 は不変。
- 確認済み: 削除した名前の参照が、本体・テスト・XAML に残っていない（`Step12DeadShimRemovalGuardTests` の文字列は、`Global` 用の名簿）。契約の別の実装クラスは存在しない。

### W2 の実施記録（2026-10-17、W1 のビルド・テストは成功。W2 のビルド・テストはユーザーの実行待ち）
- 削除（契約 `IProfileSettingsService`）: `ProfileSettingChanged` イベント、`WireSubSettingsEvents`、`UnwireSubSettingsEvents`（と説明文）。
- 削除（実装 `ProfileSettingsService`、168 行）: イベント、`OnProfileSettingChanged`（**GUI ログ `[DI] ProfileSettingsService.SettingChanged: Slot …` を含む。Q2＝案 A の承認済み**）とその呼び出し 6 箇所（`SetUseTempProfile`／`SetTempProfileName`／`SetTempProfileDistance`／`SetLinkedProfileCheck`／`ProfileActions` の setter／`ResetToDefaults`）、`SubSettingDescriptor`、`SubSettingDescriptors`、`WireSubSettingsEvents`／`UnwireSubSettingsEvents`、`OnSubSettingChanged`、`_subSettingsUnwireMap`、W1 で契約のみ外していた `TempProfileNameArray`／`LinkedProfileCheckArray`（内部の配列 `_tempProfileName`／`_linkedProfileCheck` は残る）。
- 削除したファイル: `DI/ProfileSettingChangedEventArgs.cs`。
- 値の更新（`SetUseTempProfile` など）の挙動は、通知を出さなくなった点を除き同一。
- 残したもの: サブ設定の基底クラス `ProfileSubSettingBase` の `OnSubPropertyChanged` イベント（Step14 のサブ設定クラスが持つ。購読者はいなくなった）。Step10 の `BackingStore` 整理で、不要になったかを再判断する（台帳に登録）。
- テスト:
  - 削除: `ProfileSettingsServiceSubSettingsTests`（6 件のイベント中継のテストと、`SubSettingDescriptors` の完全性テスト）、`ProfileUnifiedPersistenceTests`（2 件）、`ProfileSettingsServiceTests.SettingChangedEvent_ShouldFire`。
  - 移動: `ProfileActions_GetSet_ShouldPersistAndNotify` → `ProfileSettingsServiceTests.ProfileActions_GetSet_ShouldPersist`（通知の確認を除いた）。
- ラチェット: 変更なし（`Global.*` 732、`AppHost` 40、`Program.rootHub` 22）。
- コメントだけが残る参照: `EnvironmentServiceInjectionTests`、`ProfileEditorBackendViewModelTests` の注記（削除した旧テストの名前。動作に影響なし）。

### W3 の実施記録（2026-10-17、W2 のビルド・テストは成功。W3 のビルド・テストはユーザーの実行待ち）
- 削除（契約 `IDeviceStateService`）: `Devices`、`GetDevice`、`IsDeviceConnected`、`ConnectedControllersCount`、`GetDeviceMacAddress`、`GetConnectionType`、`GetBatteryLevel`、`SetDevice`、`DeviceStateChanged`（理由の説明文を契約に残した）。残る: `IsFirstConnection`、`MarkConnected`、`ResetConnectionFlags`、`OnDeviceSerialChange`。
- 削除（実装 `DeviceStateService`）: 内部配列 `_devices`、`MAX_SLOTS`、`_syncLock`、イベント、`NotifyDeviceStateChanged` と上記メンバー。
- 削除したファイル: `DI/DeviceStateChangedEventArgs.cs`、`DS4Forms/ViewModels/ControllersViewModel.cs`、`DS4WindowsTests/DeviceStateServiceTests.cs`（全 4 件が削除したメンバーの確認だけだった）。
- `ServiceRegistration`: `ControllersViewModel` の登録を削除。
- テスト: `PatternBViewModelTests` の 2 件（`ControllersViewModel` の解決と Singleton）、`Step10GroupTwoGlobalReferenceGuardTests` の `ControllersViewModel_RequiresAllServices` と許可リストの 1 行を削除。
- モデル図: `02-Layer-Architecture-Diagram.md`（Singleton ViewModels から `ControllersViewModel` を除く）、`04-Service-Lifecycle-Spec.md`（`IDeviceStateService` の責務の説明、`ControllersViewModel` の行の削除、注記）を更新。
- ラチェット: 変更なし（削除したファイルに `Global`／`AppHost`／`rootHub` の参照はなかった。`Global.*` 732、`AppHost` 40、`Program.rootHub` 22）。
- 気づいた点（未対応）: `ProfileApplicationService` は `IDeviceStateService` を受け取るが、フィールド `_deviceState` を使っていない。依存の整理は Step10（または Step3 の `ProfileApplicationService` の移管）で判断する。

### W4 の実施記録（2026-10-17、W3 のビルド・テストは成功。W4 のビルド・テストはユーザーの実行待ち）
- **計画からの変更（調査で判明）: `IEnvironmentService.UsingMaxControllers` は「削除」ではなく「接続」に変更した。** 理由: Phase6-Step2-1b（2026-09-19、決定 O2=C）で `ControllerSlotLimit` と同時に追加された、`ControlService.USING_MAX_CONTROLLERS`（static 互換シム）の置換先だった。`ControlService.cs` のコメントに「値の SSOT は `IEnvironmentService`。各呼び出し元は IEnvironmentService の注入へ置換し、最終的に削除する」とあり、呼び出し側（`AutoProfilesViewModel.cs:85`、`ProgramItem.cs:227`、`AutoProfilesDTO.cs:52`）の切り替えが未実施。§2.4 の「使うべきもの」に当たるため、契約に TODO（経緯・差・接続予定 Phase7-Step10）を付けて残した。集計は、削除 44／接続 4／温存 1（Phase8 へ 12）に変わる。
- 削除（契約と実装）:
  - `IOutputSlotService`: `OutputDevices`、`GetOutputDevice`、`IsSlotPlugin`。実装の `SetOutputDevice`（と配列 `_outputDevices`）も削除。`UnplugSlot` は `SetOutputDevice(…, null)` の代わりに `RaiseOutputSlotChanged(slot, None, null)` を呼ぶ（イベントの発行は同一）。`PluginSlot` のイベントの `OutputDevice` は、孤立配列が常に null だったため null を渡す（同一）。削除した `SetOutputDevice` の Trace ログ（`[DI] OutputSlotService.SetOutputDevice: Slot … output device updated`）はなくなった（`OutputSlotChanged` の Trace ログは残る）。
  - `IVirtualKBM.ErrorMessage`（と `OutputKBMHandlerAdapter` の実装）。基底クラス `VirtualKBMBase` の public プロパティ `ErrorMessage` は、上流のコードのため触れない。
  - `IUdpServerService.ListenAddress`（実装は private の `_listenAddress` を残す）。
  - `INotificationService.NotificationsEnabled`／`FlashTaskbar`。実装 `AppNotificationService` は、`NotificationsEnabled` を private の判定（`Global.Notifications != 0`）として残し、`FlashTaskbar` を削除した。
  - `IPathService.GetAutoProfilesPath`（と実装）。プロパティ `AutoProfilesPath` は残る。
- テスト:
  - `OutputSlotServiceTests`: 3 件を削除（`InitialState_ShouldHaveNoOutputDevices`、`OutputSlotChangedEvent_ShouldFireOnSetOutputDevice`、`SetOutputDevice_OutOfBounds_DoesNotFireEvent`）、範囲外のテストから `GetOutputDevice` の 2 行を削除。
  - `NotificationServiceTests`: 設定の同期だけを確認する 3 件（`Defaults_ShouldSynchronizeWithGlobal`、`GlobalShim_ShouldSynchronizeWithService`、`Service_ShouldDirectlyReflectGlobalChanges`）を削除。`service.NotificationsEnabled = …` は `Global.Notifications = 1／0` に置き換えた。
  - `MockNotificationService`、`MockVirtualKBM`、`RecordingVirtualKbm`、`VirtualKBMTests`、`UdpServerServiceTests`: 削除したメンバーの定義・確認を削除。
- ラチェット: `AppNotificationService.cs` 6 → 1、`OutputKBMHandlerAdapter.cs` 21 → 20。全域は `Global.*` 732 → **726**（走査で確認）。`AppHost` 40、`Program.rootHub` 22 は不変。

### W5 の実施記録（2026-10-17、W4 のビルド・テストは成功。W5 のビルド・テストはユーザーの実行待ち）
- 削除（契約と実装）:
  - `IProfileRepository`: `LoadDefaultProfile`、`LoadProfileToSlot`、`GetProfileNames`、`ProfileExists`（46 行）。
  - `ISpecialActionRepository`: `AddAction`、`ReplaceAction`（メモリ上のリストだけを変える並行実装）、`GetActionIndex`（契約のみ。実装は、`RemoveAction` が内部で使うため private に変更）。契約の説明文の `cref` を `RemoveAction` に更新。
  - `IDs4DeviceRegistry`: `ConnectedDevices`、`DeviceCount`（`GetDS4Controllers()` と同じ値）。
- 接続（削除しない）: `IDs4DeviceRegistry.ReEnableDevice` に TODO（経緯・差・接続予定 Phase7-Step4）を付けた。
- テスト: `ProfileRepositoryTests.LoadDefaultProfile_ShouldResetSlotSettings` を削除。`SpecialActionRepositoryTests` は、`AddAction` による準備を `ActionList.Add` に置き換え、`ReplaceAction` のテストを削除し、`ActionsChanged` のテストは `RemoveAction` で発火させる形に変更。`Ds4DeviceRegistryTests` はモックと確認から削除したメンバーを除いた。
- ラチェット: 変更なし（`Global.*` 726、`AppHost` 40、`Program.rootHub` 22）。
- **調査で見つけた、承認済みの範囲の外の事項（判断をお願いしたい）**:
  1. `ISpecialActionRepository.RemoveAction`（メモリ上のみ）は、`AddAction`／`ReplaceAction` と同じ並行実装の仲間で、本体の呼び出しが 0 件。名前が `Global.RemoveAction`／`BackingStore.RemoveAction` と衝突するため、W5 の前の走査（名前のみで数える）から漏れていた。`Phase6Step10GroupThreeContractTests.RemoveAction_InMemoryOnly_DoesNotChangeFile` が、保存する `RemoveActionAndSave` との違いを固定するために使っている。推奨: 削除する（W6 で、そのテストと一緒に）。
  2. `IProfileSettingsService.ResetToDefaults` は、`LoadDefaultProfile` の削除で、本体の呼び出しが 0 件になった（残るのは `ProfileSettingsServiceTests` のみ）。推奨: 削除する（W6 で）。
  3. 名前の衝突で走査から漏れる同様の事例を探すため、W6 の最初に、型を明示した走査（受け手の型で数える）で再確認する。

### W6 の実施記録（2026-10-17、W5 のビルド・テストは成功。W6 のビルド・テストはユーザーの実行待ち）
- 承認（2026-10-17）: `RemoveAction`（メモリ上のみ）と `ResetToDefaults` の削除、名前衝突で漏れたメンバーの再確認。
- 削除:
  - `ISpecialActionRepository.RemoveAction`（と実装、private 化していた `GetActionIndex`）。テスト: `SpecialActionRepositoryTests` の `RemoveAction_ShouldRemoveItem`、`ActionsChangedEvent_ShouldFireOnMutation`（保存経路の通知は `Phase6Step10GroupThreeContractTests.SaveAction_AddsNewAction_SavesFileAndNotifies` が確認する）、`Phase6Step10GroupThreeContractTests.RemoveAction_InMemoryOnly_DoesNotChangeFile`。
  - `IProfileSettingsService.ResetToDefaults`（と実装。`LoadDefaultProfile` の削除で、本体の呼び出しが 0 件になっていた）。テスト: `ProfileSettingsServiceTests.ResetToDefaults_ShouldRestoreValues`。
- TODO を付与（Q5）: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（接続予定 Step5）、`IOutputSlotService.OutputSlotChanged`（温存、Step6 で再判断）。`IEnvironmentService.UsingMaxControllers`（W4）と `IDs4DeviceRegistry.ReEnableDevice`（W5）は付与済み。
- ガード: 新規 `Phase7Step2RemovedContractMembersGuardTests`（削除した 51 メンバーが契約に再び現れないこと、削除した 3 ファイルの不在、残した 9 メンバーの存在。ソースの実ファイルと突き合わせて、不一致が 0 件であることを確認した）。
- Phase8 の台帳: `Phase8-Unified-Trigger-Dispatch-Plan.md` に、12 メンバーと Service Locator 17 件を追記した。
- モデル図: `03-Class-Interface-Diagram.md`・`04-Service-Lifecycle-Spec.md` のインターフェース記述は W3 の注記に加え、下記の注記を追記した。
- ラチェット: 変更なし（`Global.*` 726、`AppHost` 40、`Program.rootHub` 22）。

#### 名前の衝突で漏れたメンバーの再確認の結果（型を意識した再走査）
- 方法: 全インターフェースのメンバーについて、「契約・実装以外にも同名の宣言がある」ものを抽出し（214 件）、参照の受け手の変数名で、当該インターフェースの変数か、別の型かを確認した（`scripts/audit/Step12/iface.py` を拡張した手元の走査）。参照が 2 件以下の 76 件を目視で確認し、3 件以上の分は、受け手の変数名がインターフェースの変数であることを抜き取りで確認した。
- **新たに見つかった、本体の呼び出しが 0 件のメンバー（承認待ち、いずれも未削除）**:
  1. `IUdpServerService` **契約全体**（`IsRunning`／`Port`／`Start`／`Stop`）: DI に登録されているが、使う側が無い。`ControlService` は `UdpServer` を直接 `new` して使う（`ControlService.cs:949`）。Phase5-Step10（2026-09-04）で「UDP サーバーの起動・停止の窓口」として作られ、呼び出し側の切り替えが未実施。§2.4 の「使うべきもの」に当たる可能性が高い（推奨: 接続。`ControlService` の UDP の起動・停止をこのサービス経由にする。時期は Step5 以降）。
  2. `ISpecialActionRepository.GetAction`、`ActionExists`、`SaveActions`: 受け手が別の型（`Global`／`m_Config`／`BackingStore`）のものだけだった。推奨: 削除（`GetActionOrPlaceholder` が使われている。`ActionExists` は `RemoveActionAndSave` の内部判定として実装に残す）。
  3. `IPathService.ActionsPath`: 受け手が `specialActionRepo`（`ISpecialActionRepository.ActionsPath`）で、別の契約のもの。推奨: 削除。
  4. `IDs4DeviceRegistry.IsHidHideInstalled`: 同じ値を `IEnvironmentService.IsHidHideInstalled()` が提供し、使われているのはそちら。推奨: 削除。
  5. `IProfileSettingsService.RumbleAutostopTime`（配列版）: 受け手が `DS4Device`／`RumbleSettings` のもの。使われているのは `GetRumbleAutostopTime(int)`。推奨: 削除。
  6. `IVirtualKBM.Version`: 受け手が別の型（`OSVersion` ほか）で、`Global.outputKBMHandler.Version` は具象の基底クラスのもの。推奨: 削除（アダプターの実装は契約のためのもの）。
- 限界: 参照が 3 件以上のメンバーは、受け手の抜き取り確認にとどまる。見落としの可能性は残る（W7 で、さらに確認するかを判断）。

### W7 の実施記録（2026-10-17、W6 のビルド・テストは成功。W7 のビルド・テストはユーザーの実行待ち）
- 承認（2026-10-17）: W6 で見つけた追加候補 7 件のうち、`IUdpServerService` は「接続」として TODO を付けて残し、他の 6 件は削除する（推奨どおり）。
- 削除（契約と実装）:
  - `ISpecialActionRepository.SaveActions`（と実装）、`GetAction`、`ActionExists`（契約から外した。実装の `GetAction`／`ActionExists` は、`RemoveActionAndSave` の内部判定とテストの確認で使うため、クラスには残した）。契約の説明文の `cref` を修正。
  - `IPathService.ActionsPath`（と実装。`SpecialActionRepository` は自前の `ActionsPath` を持つ）。
  - `IDs4DeviceRegistry.IsHidHideInstalled`（と実装、テストのモック）。
  - `IProfileSettingsService.RumbleAutostopTime`（配列版。実装も削除。`BackingStore.rumbleAutostopTime` は残る）。
  - `IVirtualKBM.Version`（と `OutputKBMHandlerAdapter`、`MockVirtualKBM`、`RecordingVirtualKbm`、`VirtualKBMTests` の確認）。基底クラスの `Version` は上流のため触れない。
- 接続（削除しない）: `IUdpServerService` に TODO（経緯、`ControlService` が `UdpServer` を直接 new する差、接続予定 Phase7-Step5 以降）。
- ガード: `Phase7Step2RemovedContractMembersGuardTests` に、上記の削除分を追加し、`IUdpServerService` の 4 メンバーを「残す契約」に加えた（削除 56 メンバー。W6 の記録の「51」は、数え間違いだった）。契約の実ファイルと突き合わせて、不一致が 0 件であることを確認した。
- ラチェット: `Ds4DeviceRegistryAdapter.cs` 1 → 0、`OutputKBMHandlerAdapter.cs` 20 → 19、`SpecialActionRepository.cs` 4 → 3。全域は `Global.*` 726 → **723**（走査で確認）。`AppHost` 40、`Program.rootHub` 22 は不変。

#### W7 のテスト失敗と是正（2026-10-17）
- 失敗: `Phase6Step7ContractExtensionTests.SpecialActionRepository_CreateStandardActions_DelegatesToGlobal_AndLoadActionsHasNoFileExistsShortcut`（「LoadActions の本体を特定できなかった」）。
- 原因: このテストは、ソースの文字列で `LoadActions` の本体の終端を、次のメンバー `public bool SaveActions()` で判定していた。W7 で `SaveActions` を削除したため、終端が見つからなくなった（実装の挙動の問題ではなく、テストの終端の指定）。
- 是正: 終端を、`LoadActions` の次のメンバー `public SpecialAction GetAction(` に変更した（テストの意図「`LoadActions` に `File.Exists` の早期 return が再混入していないこと」は維持）。
- 教訓: ソースの文字列でメンバーの範囲を切り出すテストは、隣のメンバーの削除で壊れる。メンバーを削除する前に、`public bool <名前>` のような隣接メンバーの名前を使う正規表現を検索する（今後の削除前の確認項目に追加）。
