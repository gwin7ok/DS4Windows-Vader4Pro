# Phase6-Step5 計画書: OutputSlotService の孤立配列撤廃と UDP 診断コマンドの是正

作成日: 2026-09-11  
改訂日: 2026-09-24（Step4 完了後に現行コードと突き合わせ、台帳を再作成。旧改訂: 2026-09-18）  
状態: **完了確定（2026-09-24）。Step5-1・5-2 はビルド・テストビルド・テスト実行成功（ユーザー確認）。Step5-3 の追加テストは確認待ち。詳細は `Phase6-Step5-Completion-Report.md`**  
対象ブランチ: `For-DI-migration-work`  
台帳作成時の HEAD: Step4 完了コミット直後  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Service-Reference-Evidence.md` §1-1、`Phase6-Step1-UI-Reference-Evidence.md` §2  
  - `Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`、`Phase5-Step14-Issue7-fix-implementation-report.md`  

---

## 0. 背景と、旧計画書からの乖離

### 0.1 課題（変わらず）
- UDP 診断コマンド `query.<device>.outconttype` が、正本のプロファイル設定（`IProfileSettingsService.OutContType`）ではなく `outputSlotService.GetOutputDeviceType` を参照している。
- `OutputSlotService` 内の配列 `_deviceTypes` は、`Global`／`BackingStore` と連動しない孤立配列で、`GetOutputDeviceType` は常に `OutContType.None` を返す（バグの温床）。
- `OutputSlotService.cs` の `OutDevTypeTemp`／`ActiveOutDevType` が `Global` の配列を直接返している。

### 0.2 現行コードとの乖離（2026-09-24 に確認）
- **行番号が変わっている**: UDP 診断コマンドの呼び出しは `MainWindow.xaml.cs:1432`（旧記載 1412）。
- **旧計画のコード例が現行と一致しない**:
  - `_deviceTypes` の宣言は `OutputSlotService.cs:21`、要素数は `MAX_SLOTS = 8`（旧記載は 33 行・要素数 4）。範囲判定も `>= MAX_SLOTS`（8）で、旧計画のコード例の `>= 4` は誤り。
  - コンストラクタは `OutputSlotService(OutputSlotManager slotManager = null, IOutputSlotStore store = null, ControlService control = null)`（旧記載は `(IOutputSlotStore store = null)`）。
- **旧計画の見落とし（設計上の危険）**: `_deviceTypes` に書き込むのは `SetOutputDeviceType` で、これを呼ぶのは `PluginSlot`／`UnplugSlot`（どちらもアプリ本体からの呼出元 0 件）と、テストだけである。旧計画のとおり `SetOutputDeviceType` を `IProfileSettingsService.OutContType[slot] = type` へ委譲すると、`UnplugSlot` が呼ばれたときに、**永続化されるプロファイル設定へ `OutContType.None` を書き込んでしまう**（実行時の状態を永続設定へ混入させる）。
- **インデックスの意味が違う**: `PluginSlot`／`UnplugSlot` の引数は出力スロット（`OutputSlotManager` のスロット番号）だが、`MainWindow` の `GetOutputDeviceType(tdevice)` の引数はコントローラー番号である。どちらも 0〜7 で型は合うが、意味が違うため、孤立配列が正しい値を持つことはなかった。
- **`Global` 参照の性質**: `OutDevTypeTemp => Global.outDevTypeTemp`（193 行）、`ActiveOutDevType => Global.activeOutDevType`（196 行）は、`ProfileSettingsService` が `Global.store` を裏づけに使うのと同じ、「`Global` の静的配列を裏づけとするサービスの公開アクセサ」である。配列の実体は、`ScpUtil.cs` の 8 箇所（`Global.activeOutDevType[device]`）、`ProfileEditor.xaml.cs:1263`、`BindingWindowViewModel.cs:57`、`PressKeyViewModel.cs:221`（`Global.outDevTypeTemp`）、`ControlService`（`ActiveOutDevType` プロパティ経由）から、直接または間接に読み書きされている。
- **コンストラクタの依存追加の影響**: `OutputSlotService` は `ScpUtil.cs:863` の静的初期化（`new OutputSlotService()`）と、テスト 10 箇所（`OutputSlotServiceTests` 9、`ProfileSettingsViewModelTests` 1）で引数なしに生成されている。`IProfileSettingsService` を必須引数にすると、これらがすべて壊れる。

---

## 1. 目的
1. `MainWindow.xaml.cs:1432` の UDP 診断コマンド `outconttype` を、正本である `IProfileSettingsService.OutContType` へ是正する。
2. 孤立配列 `_deviceTypes` と、その読み書き API を撤廃し、二重管理・値の乖離の根本原因を抹消する。
3. 出力デバイス状態の三態（永続設定／実行時接続状態／UI 一時状態）を明文化し、Step6・Step7b・Step8 との整合を保証する。

---

## 2. 出力デバイス設定・状態の「三態」SSOT 台帳
- **永続設定（Profile）**
  - 参照先: `IProfileSettingsService.OutContType[slot]`（正本は `BackingStore.outContType`、XML に永続化）。
  - 役割: プロファイルに保存される出力コントローラー種別。値は `X360`／`DS4`。
- **実行時接続状態（Runtime）**
  - 参照先: `IOutputSlotService.ActiveOutDevType[slot]`（裏づけは `Global.activeOutDevType`）。
  - 役割: 現在、仮想バスに実際にプラグインされている仮想コントローラーの種別。未接続時は `None`。インデックスはコントローラー番号。
- **UI 一時編集状態（Temp）**
  - 参照先: `IOutputSlotService.OutDevTypeTemp[slot]`（裏づけは `Global.outDevTypeTemp`）。
  - 役割: プロファイル編集画面で選択中の未確定の種別。ボタン名の表示（A/B/X/Y ⇄ Cross/Circle）に使う。編集スロット（Step7b 後は常に 8）に対して読み書きする。
- **孤立配列 `_deviceTypes`**: 上記三態のいずれにも属さない「第4の偽状態」。撤廃する。
- **UDP 診断コマンド**: `query.<device>.outconttype` は永続設定（プロファイルの値）を返すのが仕様。`activeoutdevtype` は実行時接続状態を返す（既に `ActiveOutDevType` を参照しており、変更不要）。

---

## 3. 全件修正対象台帳（現行コードから再作成）

### 3.1 実参照・実装（要修正）
- **C5-01**: `DS4Forms/MainWindow.xaml.cs:1432` `outputSlotService.GetOutputDeviceType(tdevice).ToString()` → `profileSettingsService.OutContType[tdevice].ToString()`。`profileSettingsService` は同じ分岐内（1426 行）で既に使用しており、追加の注入は不要。
- **C5-02**: `DS4Control/Services/OutputSlotService.cs:21` `_deviceTypes` の宣言 → 削除。
- **C5-03**: 同 `GetOutputDeviceType`（59〜70 行）→ 決定1に従う。
- **C5-04**: 同 `SetOutputDeviceType`（72〜86 行）→ 決定1に従う。
- **C5-05**: 同 `SetOutputDevice`（98 行）が `OutputSlotChanged` イベントの引数に `_deviceTypes[slotIndex]` を渡している → 決定1に従う。
- **C5-06**: 同 `PluginSlot`（139 行）／`UnplugSlot`（169 行）が `SetOutputDeviceType` を呼んでいる → 決定1に従う。
- **C5-07**: 同 193 行 `Global.outDevTypeTemp`、196 行 `Global.activeOutDevType` → 決定2に従う。
- **C5-08**: `DI/IOutputSlotService.cs:35〜49` `GetOutputDeviceType`／`SetOutputDeviceType` の宣言と TODO コメント → 決定1に従う。

### 3.2 テストの影響
- `OutputSlotServiceTests.cs`: `GetOutputDeviceType`／`SetOutputDeviceType` を使うテストが 5 件（`InitialState_ShouldHaveDefaultTypes`、`SetOutputDeviceType_ShouldUpdateSlot`、`OutputSlotChangedEvent_ShouldFire`、`OutOfBounds_ShouldBeHandledSafely` の一部、`GlobalShim_ShouldSynchronizeWithService`）。決定1に応じて更新または削除する。
- `ProfileSettingsViewModelTests.cs:104〜106`、`ProfileSettingsServiceTests.cs:168`（コメント）: 影響は軽微。

### 3.3 対象外（記録のみ）
- `ScpUtil.cs` の `Global.activeOutDevType[device]` 8 箇所、`ProfileEditor.xaml.cs:1263`、`BindingWindowViewModel.cs:57`、`PressKeyViewModel.cs:221`（`Global.outDevTypeTemp`）: Step5 の対象外。それぞれ、レガシー経路（Step12／Phase7）、Step8、Step10 で扱う。
- `ProfileSettingsViewModel.cs:868`、`SpecialActionsListViewModel.cs:53` のコメント（`GetOutputDeviceType` に言及）: 決定1 の実装時にコメントも更新する。
- `PluginSlot`／`UnplugSlot`（呼出元 0 件）の非推奨化: Step6 の対象。

---

## 4. 決定事項と設計

### 決定結果（ユーザー決定、2026-09-24）
- **決定1＝案B**（孤立配列と `GetOutputDeviceType`／`SetOutputDeviceType` を削除）。
- **決定2＝案X**（現段階の Step では `Global` の静的配列を裏づけのまま維持し、TODO を付与）。**ただし将来的には案Y の理想構成（配列の所有をサービスへ移し、`Global` 側を転送プロパティにする）にする**。Phase7 または Phase8 の計画のうち、似た作業を含む部分があれば、そこへ加える方針。調査結果は §9 に記録した。

### 決定1: `GetOutputDeviceType`／`SetOutputDeviceType`（孤立配列）の扱い【案B に確定】
- **案A（旧計画どおり）**: 両方を `IProfileSettingsService.OutContType` へ委譲し、`[Obsolete]` を付与する。
  - 問題点: `UnplugSlot` 経由で永続設定へ `None` が書き込まれる（§0.2）。これを避けるには `Set` は委譲せず、追加の対策が要る。`IProfileSettingsService` の依存追加で、`ScpUtil.cs:863` の静的初期化とテスト 10 箇所の修正が必要。
- **案B（推奨）**: `MainWindow` の呼び出し（C5-01）を是正した後、両メソッドの呼出元はテストと `PluginSlot`／`UnplugSlot`（内部）のみとなる。**両メソッドと `_deviceTypes` をインターフェース・実装から削除する**。`OutputSlotChanged` イベントの `DeviceType` は、`SetOutputDevice` では `None`、`PluginSlot`／`UnplugSlot` では引数の `devType`／`None` を、イベント発行用の private ヘルパーから直接渡す。イベントの購読者はテストのみで、アプリ本体にはいない。
  - 利点: 永続設定の汚染リスクがなく、`OutputSlotService` に新しい依存を足さない。コンストラクタ・`ScpUtil.cs:863`・テストの変更が不要。`GetOutputDeviceType` は常に `None` を返すバグを持つ API で、是正後の呼出元は 0 件となるため、削除による挙動の変化はない。
  - 留意: `copilot-instructions.md` §2.1（薄いシムの温存）は、機能を持つ旧経路が対象。本件は、常に誤った値を返す孤立 API で、呼出元が 0 件になる。
- **案C**: `Get` のみ `IProfileSettingsService.OutContType` へ委譲して `[Obsolete]` で温存し、`Set` は削除する。依存追加とテスト修正が必要（案A の一部）。

### 決定2: `OutDevTypeTemp`／`ActiveOutDevType` の `Global` 直参照（193、196 行）【案X に確定。将来は案Y】
- **案X（推奨）**: 裏づけを `Global` の静的配列のまま維持し、`TODO(技術的負債)` コメントを付与する（理由: 配列の実体が `ScpUtil.cs` の 8 箇所と UI 3 箇所から直接読み書きされており、Phase7／Step8／Step10／Step12 で解消されるため。`ProfileSettingsService` が `Global.store` を裏づけに使う前例と同じ）。既存のテスト（`Assert.Same(Global.outDevTypeTemp, service.OutDevTypeTemp)` 等）も維持できる。
- **案Y**: 配列の所有をサービスへ移し、`Global` 側を転送プロパティにする。旧計画の意図に近いが、`ControlService` が配列参照を 1 回だけキャッシュする前提（`ControlService.cs:121〜124`）、`ScpUtil.cs` の静的初期化順序、`Global.outDevTypeTemp` の直接参照 5 箇所への影響が大きく、Step5 の範囲を超える。

---

## 5. マイクロステップ（決定1＝案B、決定2＝案X の場合）

```text
【Phase6-Step5 マイクロステップ構成】
├─ Step5-0: 台帳の再作成・計画書改訂（本書）  ← 完了
├─ Step5-1: MainWindow.xaml.cs の UDP 診断コマンド是正（C5-01）
├─ Step5-2: OutputSlotService の孤立配列と Get/Set の撤廃、IOutputSlotService の更新（C5-02〜C5-06、C5-08）＋ Global 直参照への TODO 付与（C5-07）
└─ Step5-3: テスト更新・三態検証・文書・完了報告書
```

各ステップの後に、ユーザー側で `dotnet build` と `dotnet test` を実行する。コミットもユーザー側で行う。

### Step5-1: UDP 診断コマンドの是正
- `MainWindow.xaml.cs:1432` を `profileSettingsService.OutContType[tdevice].ToString()` に変更する。
- この時点で、`GetOutputDeviceType` の呼出元はテストのみになる。

### Step5-2: 孤立配列の撤廃
- `OutputSlotService.cs`: `_deviceTypes`、`GetOutputDeviceType`、`SetOutputDeviceType` を削除。イベント発行用の private ヘルパー（スロット番号、種別、出力デバイスを受けて `OutputSlotChanged` を発行）を追加し、`SetOutputDevice`／`PluginSlot`／`UnplugSlot` から呼ぶ。
- `IOutputSlotService.cs`: 2 メソッドの宣言と TODO コメントを削除。三態を説明するコメントを `OutDevTypeTemp`／`ActiveOutDevType` に追加。
- `OutDevTypeTemp`／`ActiveOutDevType` に、`Global` 静的配列が裏づけである理由と解消予定のフェーズを明記した TODO を付与する（既存コメントは消さない）。
- `ProfileSettingsViewModel.cs:868`、`SpecialActionsListViewModel.cs:53` のコメントを、現状に合わせて更新する（コメントのみ）。

### Step5-3: テスト・文書・完了報告
- `OutputSlotServiceTests.cs`: 削除した API のテストを、`PluginSlot`／`UnplugSlot` の範囲外安全性と、`SetOutputDevice` でのイベント発行のテストに置き換える。
- 新規 `OutputSlotServiceSsotTests.cs`: `OutDevTypeTemp`／`ActiveOutDevType` が `Global` の同一配列であること（状態の複製がないこと）、三態が互いに独立していること（`OutContType`［永続］を変えても `ActiveOutDevType` が変わらない等）。
- 新規 `MainWindowUdpOutContTypeGuardTests.cs`（ソース走査ガード）: `MainWindow.xaml.cs` の `outconttype` 分岐が `profileSettingsService.OutContType` を参照し、`GetOutputDeviceType` を呼ばないこと。
- `Phase6-Status.md`、`Phase6-Plan.md`、`Phase6-Step6-Plan.md`（`GetOutputDeviceType`／`SetOutputDeviceType` の削除を反映）を更新し、完了報告書 `Phase6-Step5-Completion-Report.md` を作成する。

---

## 5.1 §5 の TODO に書く内容（案Y への移行予定の明記）
`OutDevTypeTemp`／`ActiveOutDevType` に付与する TODO には、次を含める。
- 現状: 配列の実体は `Global.outDevTypeTemp`／`Global.activeOutDevType`（静的配列）で、本サービスは裏づけとして返しているだけである。
- 理由: 配列は `ScpUtil.cs` の 8 箇所、`ProfileEditor.xaml.cs:1263`、`BindingWindowViewModel.cs:57`、`PressKeyViewModel.cs:221` から直接読み書きされ、`ControlService` は配列の参照を 1 回だけキャッシュする前提で使っているため（`ControlService.cs:121〜124`）。
- 解消予定: Phase7 で、配列の所有をこのサービスへ移し、`Global` 側を転送プロパティ（または削除）にする（`Phase6-Step12-Plan.md` §4、`DI-App-Wide-Migration-Plan.md` §6.9.3 に登録済み）。

## 6. 実機確認項目
1. UDP 診断コマンド `query.<device>.outconttype` が、プロファイルの出力種別（`X360` または `DS4`）を返し、`None` を返さないこと（UDP クライアントがない場合は、単体テストとコードの確認で担保し、先送り台帳へ登録する）。
2. `query.<device>.activeoutdevtype` が従来どおり動くこと。
3. コントローラーの接続時に、出力種別（X360／DS4）がプロファイルどおりに切り替わること（ホットスワップ経路への影響なしの確認）。

### 6.1 実機確認の扱い（2026-09-24）
- 項目1（UDP 診断コマンド `outconttype`）・項目2（`activeoutdevtype`）: UDP クライアントの環境がないため、Step2-PR-1 の OSC／UDP と同様に Step11 の先送り台帳へ登録した。`MainWindowUdpOutContTypeGuardTests`（ソース走査ガード）で分岐の参照先を固定している。
- 項目3（接続時の出力種別の切り替え）: Step5 の変更は、アプリ本体から呼ばれない API（`GetOutputDeviceType`／`SetOutputDeviceType`、呼出元 0 件の `PluginSlot`／`UnplugSlot` の内部）と UDP 診断コマンドの 1 行のみで、接続時のホットスワップ経路（`ScpUtil.cs` の `PostLoadSnippet`、`ControlService.PluginOutDev`）には触れていない。Step11 の先送り台帳へ登録した（必要なら、次の実機確認時に合わせて確認する）。

## 7. ロールバック方針
- ステップ単位で `git revert` する。Step5-1 は 1 行の変更なので、単独で戻せる。

## 8. 完了判定チェックリスト
- [x] `MainWindow.xaml.cs` の UDP 診断コマンド `outconttype` が `profileSettingsService.OutContType[tdevice]` を参照している
- [x] `_deviceTypes` と、`GetOutputDeviceType`／`SetOutputDeviceType` が削除され、`OutputSlotChanged` イベントが引き続き発行される（決定1＝案B の場合）
- [x] `OutDevTypeTemp`／`ActiveOutDevType` の裏づけと解消予定フェーズが TODO コメントで明記されている
- [x] 三態の SSOT 境界がコメントとテストで固定されている
- [ ] `dotnet build`、`dotnet test` 全件成功（Step5-2 までは成功。Step5-3 の追加テストは確認待ち）
- [x] 実機確認（§6）が完了、または Step11 の先送り台帳に登録済み（§6 の結果を参照）
- [x] `Phase6-Status.md`／`Phase6-Plan.md`／`Phase6-Step6-Plan.md` を更新、完了報告書を作成

---

## 9. 将来の理想構成（案Y）と、Phase7／Phase8 への引き継ぎ（2026-09-24 調査）
- **Phase7**: 個別計画書 `Phase7-Plan.md` は未作成（Phase6 完了後に事前監査を経て策定する予定。`DI-App-Wide-Migration-Plan.md` §6.9.3）。現時点で、Phase7 の射程は `Mapping.cs` の完全 instance 化と `ScpUtil.cs` の最終解体で、`Phase6-Step12-Plan.md` §4 の最終引き継ぎ台帳が `ScpUtil.cs` 最終解体のロードマップである。案Y（実行時状態の配列の所有権をサービスへ移す）は、この「`ScpUtil.cs` の最終解体」の一部として自然に収まるため、次の 2 か所へ登録した。
  - `Phase6-Step12-Plan.md` §4 の引き継ぎ台帳に、行「`Global.outDevTypeTemp`／`Global.activeOutDevType`（実行時状態の静的配列）」を追加。
  - `DI-App-Wide-Migration-Plan.md` §6.9.3 に、Phase7 の射程に含める項目として同内容を追記。
  - Phase7 の個別計画書を作る際は、この項目を取り込むこと。
- **Phase8**（`Phase8-Unified-Trigger-Dispatch-Plan.md`）: Controls／SpecialActions の統一トリガーディスパッチが目的で、出力デバイスの種別や実行時状態の所有権とは無関係。加える項目はない。
- 案Y を実施するときの前提条件（Phase7 の計画時に確認）: ①`ScpUtil.cs` の `Global.activeOutDevType` 8 箇所が、サービス経由へ置換されていること（Step12／Phase7）。②`ProfileEditor`（Step8）、`BindingWindowViewModel`・`PressKeyViewModel`（Step10）の `Global.outDevTypeTemp` 直接参照が、サービス経由へ置換されていること。③`ControlService` の配列参照キャッシュ（`ControlService.cs:121〜124`）が、所有権の移動後も有効であること（配列の参照が再代入されない設計を維持する）。④静的初期化の順序（`Global` の静的フィールドとサービスの生成順）が、確定していること。
- 上記①②は、Phase6 の Step8／Step10／Step12 の完了により、Phase7 の着手時点で満たされる見込み。
