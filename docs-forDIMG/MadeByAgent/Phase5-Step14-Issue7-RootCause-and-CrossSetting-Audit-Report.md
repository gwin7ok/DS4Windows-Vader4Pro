# Phase 5 - Step 14 調査報告書: Issue 7 真因再調査 ＆ プロファイルXML全設定項目 横断監査

作成日: 2026-09-11
対象ブランチ: `For-DI-migration-work`
調査実施方法: `git clone --depth 1 --branch For-DI-migration-work` によるリポジトリ全文 `grep` 実地確認
関連ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Plan.md`（フェーズC: タスク(c)-1, (c)-2 の前提を修正する内容を含む）
- `docs-forDIMG/MadeByAgent/Phase5-Step14-FormSettings-Unification-Status.md`
- `docs-forDIMG/MadeByAgent/Phase5-Step14-RealDevice-Investigation-and-Fix-Report.md`（§3.6 Issue 6, §6.4 Issue 7）
- `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（§4.5 カテゴリA〜F、Phase6スコープ）
- `.github/copilot-instructions.md`（3.1 Pure DI原則）

---

## 1. 調査の経緯・目的

`Phase5-Step14-FormSettings-Unification-Plan.md` フェーズCでは、「プロファイル編集画面の `Emulated Controller` コンボボックスが、XMLに `<OutputContDevice>DS4</OutputContDevice>` が存在するにもかかわらず `Xbox 360` と誤表示される」(Issue 7) について、タスク(c)-1「`ProfileSettingsViewModel.cs` ロード時 `tempConType` 再同期」として着手予定であった。

着手前に、gwin7ok 氏より **「`Emulated Controller` 項目だけでなく、`[プロファイル名].xml`（`Profiles\{ProfileName}.xml`）に保存されている設定値全てで同種の問題が起きていないか」** の横断確認指示を受け、本調査を実施した。

調査の結果、**Issue 7 の真因はタスク(c)-1が想定していたもの（ロード時の再同期漏れ）ではなく、より根が深い「DIサービス内部の孤立フィールド」バグ（Issue 6と同カテゴリ）の第3の発生箇所であること**が判明した。これにより、フェーズCのタスク内容そのものの見直しが必要となったため、実装着手前に本報告書として整理する。

---

## 2. 調査範囲・方法

### 2.1 調査対象

`Profiles\{ProfileName}.xml`（個別プロファイルファイル。app全体設定を保持する `Profiles.xml` とは別ファイル）に保存される設定値を編集・表示する経路として、以下を対象とした。

- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`（4,258行、プロファイル編集の中核ViewModel）
- `DS4Windows/DS4Forms/ViewModels/SpecialActionsListViewModel.cs`
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`（`IProfileSettingsService` 実装、552行）
- `DS4Windows/DS4Control/Services/OutputSlotService.cs`（`IOutputSlotService` 実装）
- `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`（プロファイル読込・保存フロー）
- `DS4Windows/DS4Forms/MainWindow.xaml.cs`（関連する外部公開経路の副次確認）

### 2.2 監査手法

Issue 6 是正時に確立した観点（「DIサービスがGetter/Setterを持っていても、内部実体が `Global`/`BackingStore` と連動しているか」）を、`Profiles\{ProfileName}.xml` に保存される設定値の全経路に対して機械的に適用した。

1. `ProfileSettingsViewModel.cs` 内で使用されている全サービス参照（`profileSettings.`, `outputSlotService.`, `controlService.`, `profileRepo.`）を出現回数で棚卸し。
2. 各サービスの実装ファイルにおいて、`private` フィールド（特に配列）を全て抽出し、それが `Global`/`_config`（`BackingStore`）へ委譲しているか、独自に状態を保持しているかを確認。
3. 独自に状態を保持しているフィールドについては、その値を更新する setter メソッド（`SetXxx`）の呼び出し箇所を全文 `grep` で確認し、実際に値が更新されうるか（＝孤立していないか）を検証。
4. Issue 7 相当の症状（表示は誤りだが実際の信号出力は正しい）を再現する経路を特定。

---

## 3. 調査結果サマリ

| 監査対象 | 結果 | 詳細 |
| --- | --- | --- |
| `ProfileSettingsService`（デッドゾーン・ジャイロ・タッチパッド・振動・ライトバー等、`profileSettings.` 経由の約560箇所） | ✅ **問題なし** | 全プロパティが `_config`（`Global.store`）への直接委譲であることを確認（§4） |
| `ProfileSettingsService` 内の独自フィールド（`_touchpadActive` 等6個） | ✅ **問題なし（対象外）** | プロファイルXMLへの永続化対象ではない実行時専用フラグであり、`Global` 側に対応する永続化コードが存在しないことを確認済み。孤立バグの定義（永続化対象なのに非連動）に該当しない（§4.3） |
| `OutputSlotService.GetOutputDeviceType()` / `SetOutputDeviceType()` | 🔴 **新規発見・孤立バグ確定** | Issue 7 の真因。詳細は§5 |
| `OutputSlotService.OutDevTypeTemp` / `ActiveOutDevType` | ✅ **問題なし** | `Global.outDevTypeTemp` / `Global.activeOutDevType` への薄い参照委譲であり、既存単体テスト（`OutDevTypeTemp_ShouldReferenceSameArrayAsGlobal` 等）でも同一配列であることが保証されている |
| `SpecialActionsListViewModel.cs`（ボタン名表示） | 🔴 **新規発見・副次症状** | §5系の孤立バグを踏んで発生する未報告の表示不具合（§6.1） |
| `MainWindow.xaml.cs`（UDP `query` 診断コマンド） | 🔴 **新規発見・副次症状（軽微）** | 同上（§6.2） |
| 実際のコントローラー出力ロジック（`ControlService.cs` / `Mapping.cs`） | ✅ **問題なし** | `Global.OutContType[device]` を直接参照しており、`OutputSlotService` の孤立バグの影響を受けていないことを確認（§5.4） |

**結論**: `[プロファイル名].xml` に保存される約70項目のうち、新たに孤立バグが確認されたのは **`OutputContDevice`（Emulated Controller）に関する `OutputSlotService` 経由の1系統のみ**である。他の約560箇所の `profileSettings.` 経由プロパティについては、Step10／Step14(a)の是正により健全であることを再確認した。

---

## 4. `ProfileSettingsService` の健全性確認（問題なし）

`ProfileSettingsService.cs`（552行）を全文確認した結果を記録する。

### 4.1 確認方法

コンストラクタで受け取る `BackingStore _config`（既定値 `Global.store`）に対して、`LSModInfo`, `GyroMouseInfo`, `TouchpadButtonMode`, `RumbleBoost`, `LightbarSettingsInfo` などプロファイル永続化対象の全プロパティが `=> _config.xxx` の形で直接委譲されていることを確認した（例）。

```csharp
public StickDeadZoneInfo[] LSModInfo => _config.lsModInfo;
public GyroMouseInfo[] GyroMouseInfo => _config.gyroMouseInfo;
public byte[] RumbleBoost => _config.rumble;
public LightbarSettingInfo[] LightbarSettingsInfo => _config.lightbarSettingInfo;
```

独自キャッシュを一切保持しない構造であるため、Issue 6・Issue 7のような「実体と非連動の孤立フィールド」は**発生し得ない**設計になっている。

### 4.2 Step14(a)-5 で是正済みの項目

カラム幅5プロパティ（`ProfileEditorLeftWidth` 等）は元々このパターンから逸脱した孤立フィールドとして存在していたが、Phase5-Step14フェーズAのタスク(a)-5にて完全削除・`IAppSettingsService` へ統合済みであることを本調査でも再確認した（該当コードが `ProfileSettingsService.cs` に存在しないことを確認）。

### 4.3 独自フィールド6個は対象外（孤立バグではない）

`_touchpadActive`, `_useTempProfile`, `_tempProfileName`, `_tempProfileDistance`, `_useDInputOnly`, `_linkedProfileCheck` の6フィールドは `ProfileSettingsService` が独自に保持しているが、以下を確認した。

- `DS4Windows/DS4Control/ScpUtil.cs` のXML保存／読込処理（`SaveProfile`/`LoadProfile` 相当箇所）に、これらの名前に対応する `<Xxx>` ノードの入出力コードが存在しない。
- すなわちこれらは**プロファイルXMLへの永続化対象ではない、実行時専用の一時フラグ**であり、`ProfileSettingsService` 自身がSSOT（唯一の実体）として保持することが正しい設計である。

このため、「孤立フィールド」の定義（＝本来`Global`/`BackingStore`と連動すべきなのに独自に分離してしまっている）には該当せず、是正対象外と判定する。

---

## 5. 新規発見: `OutputSlotService` の孤立バグ（Issue 7 の真因）

### 5.1 該当コード

`DS4Windows/DS4Control/Services/OutputSlotService.cs`:

```csharp
private readonly OutContType[] _deviceTypes = new OutContType[MAX_SLOTS];
...
public OutContType GetOutputDeviceType(int slotIndex)
{
    ...
    return _deviceTypes[slotIndex];
}

public void SetOutputDeviceType(int slotIndex, OutContType deviceType)
{
    ...
    _deviceTypes[slotIndex] = deviceType;
    ...
}

public bool PluginSlot(int slotNumber, OutContType devType)
{
    ...
    SetOutputDeviceType(slotNumber, devType);
    ...
}

public bool UnplugSlot(int slotNumber)
{
    ...
    SetOutputDeviceType(slotNumber, OutContType.None);
    ...
}
```

`_deviceTypes` は `Global`/`BackingStore` と一切連動しない**独自の実行時配列**であり、初期値はC#既定値である `OutContType.None`（`enum OutContType : uint { None = 0, X360, DS4 }`）のまま保持される。

### 5.2 実地確認: setterが一度も呼ばれていないことの確認

リポジトリ全文 `grep` により、以下を確認した。

```
$ grep -rn "\.SetOutputDeviceType(" DS4Windows/
（0件）

$ grep -rn "\.PluginSlot(\|\.UnplugSlot(" DS4Windows/
（0件）
```

`SetOutputDeviceType` を直接呼ぶ箇所、および `SetOutputDeviceType` を内部で呼ぶ `PluginSlot`/`UnplugSlot` を呼ぶ箇所が、アプリ本体コード中に**1件も存在しない**（自身の単体テスト `OutputSlotServiceTests.cs` を除く）ことを確認した。

このため、アプリ実行中いかなるタイミングにおいても `_deviceTypes[]` の値は変化せず、`GetOutputDeviceType(int)` は**常に `OutContType.None` を返す**。

### 5.3 Issue 7 症状との整合性確認

`ProfileSettingsViewModel.cs` の `ControllerTypeIndex` プロパティ:

```csharp
public int ControllerTypeIndex
{
    get
    {
        int type = 0;
        switch (outputSlotService.GetOutputDeviceType(device))
        {
            case OutContType.X360: type = 0; break;
            case OutContType.DS4: type = 1; break;
            default: break; // OutContType.None はここに落ちる
        }
        return type;
    }
}
```

`GetOutputDeviceType(device)` が常に `OutContType.None` を返すため、`switch` は `default` 節に落ち、`type` は初期値の `0`（Xbox 360 に対応するインデックス）のまま返却される。**プロファイルXMLの `<OutputContDevice>` の実際の値（`DS4`/`X360`）に一切関係なく、常にインデックス0（Xbox 360）が返る。**

これは Issue 7 で報告された症状（`<OutputContDevice>DS4</OutputContDevice>` を持つプロファイルでも `Xbox 360` と表示される）と完全に一致する。

### 5.4 重要な確認事項: 実際の信号出力ロジックは無関係・正常

`ControlService.cs` / `Mapping.cs` における実際のコントローラー出力判定ロジックを確認したところ、`OutputSlotService` を一切経由せず、`Global.OutContType[device]`（＝プロファイルの永続化された実体）を直接参照していることを確認した。

```csharp
// ControlService.cs
OutContType contType = Global.OutContType[index];

// Mapping.cs
if (Global.OutContType[device] == OutContType.DS4)
```

このため、**実際の信号出力（ユーザーが体感する動作）は本バグの影響を受けておらず、正常にDS4として出力される**。Issue 7の実機報告内容（「実際の信号出力もDS4である」）と矛盾しない。

**結論として、本バグは「表示・UI層限定の不具合」であり、「保存・実行時動作」には影響しない。** これはPhase5-Step14計画書冒頭の記載（「保存処理の実行経路自体は正常に完走している」）とも整合する。

### 5.5 タスク(c)-1・(c)-2への影響

現行計画のタスク(c)-1「`UpdateProperties` 時の `tempConType` 再代入」は、**`ControllerTypeIndex`（＝常に0を返す壊れたgetter）から値を再取得して `tempConType` に上書きする**という内容であるため、実装しても症状は改善しない（壊れた値を再度コピーするだけになる）。

タスク(c)-1・(c)-2は、後述§8の代替案に置き換える必要がある。

---

## 6. 波及範囲: 同一の孤立バグに起因する未報告の副次症状

`outputSlotService.GetOutputDeviceType(` の呼び出し箇所をリポジトリ全文で確認したところ、アプリ本体中に3箇所存在し、ProfileEditorのコンボボック
ス表示（Issue 7として既に報告済み）以外に、以下2箇所が同一原因で影響を受けていることが判明した。いずれも本調査で新規に発見したものであり、これまで独立した不具合として報告されていない。

### 6.1 `SpecialActionsListViewModel.cs`（Special Action 一覧のボタン名表示）

```csharp
// Step13-5: IOutputSlotService.GetOutputDeviceType が既存の正規DIラッパー
OutContType outType = outputSlotService.GetOutputDeviceType(deviceNum);
displayName = Global.getX360ControlString((X360Controls)btnId, outType);
```

`Global.getX360ControlString(key, conType)` の実装:

```csharp
public static string getX360ControlString(X360Controls key, OutContType conType)
{
    string result = string.Empty;
    if (conType == OutContType.X360) { xboxDefaultNames.TryGetValue(key, out result); }
    else if (conType == OutContType.DS4) { ds4DefaultNames.TryGetValue(key, out result); }
    return result;
}
```

`conType` が常に `OutContType.None` であるため、`X360`/`DS4` いずれの分岐にも該当せず、`result` は初期値の空文字列のまま返る。

**症状**: プロファイルの Emulated Controller 設定に関わらず、Special Action 一覧画面で「ボタン」タイプのアクションに割り当てられたコントローラーボタン名が**常に空欄表示**になる（Issue 7のような「誤表示」ではなく「無表示」であり、実質的にはより気づきにくい形で表面化している可能性がある）。

コード中のコメント `// Step13-5: IOutputSlotService.GetOutputDeviceType が既存の正規DIラッパー` から、Phase5-Step13時点でこのAPIを「正規のDIラッパー」として信頼して呼び出し先に採用したことが確認できる。当時の監査では `GetOutputDeviceType` 自体が孤立バグを抱えていることは検出されていなかったと考えられる。

### 6.2 `MainWindow.xaml.cs`（UDP外部連携の `query` 診断コマンド）

```csharp
else if (propName == "outconttype")
    propValue = outputSlotService.GetOutputDeviceType(tdevice).ToString();
```

外部プログラムがUDP経由で `query.<device>.outconttype` を問い合わせた場合、プロファイルの実際の設定に関わらず常に文字列 `"None"` が返却される。

**影響度**: 直接プロファイルXMLの表示ではなく、外部連携APIの一機能であるため優先度は低いが、同一原因であるため本報告書に記録する。`activeoutdevtype`（`ActiveOutDevType` 経由）は `Global.activeOutDevType` に正しく連動しており、影響を受けない。

---

## 7. 対象外・優先度低と判定した既知事項（参考）

以下は本調査で存在を確認したが、`[プロファイル名].xml` の孤立バグとは異なる性質のものであり、本報告書のスコープには含めない。

- `IUdpServerService` の `ControlService` への未接続: 既にPhase5-Step14計画書 §6 でスコープ外の技術的負債として登録済み（本調査でも該当箇所を再確認したのみで、新規事項ではない）。
- `IDeviceStateService._devices` 等、物理デバイスインスタンスを保持する配列: `DI-App-Wide-Migration-Plan.md` §4.5 カテゴリE（動的に生成される実行時ドメインオブジェクト）に該当し、そもそもプロファイルXMLの永続化対象ではないため対象外。

---

## 8. 推奨対応方針（案・要承認）

以下は実装着手前の提案であり、gwin7ok 氏の承認を得てから着手する。

### 8.1 タスク(c)-1・(c)-2の置き換え案

| # | 内容 | 対象ファイル |
| --- | --- | --- |
| (c)-1（改訂） | `ControllerTypeIndex` の参照元を `outputSlotService.GetOutputDeviceType(device)` から、プロファイルの永続化実体（`Global.OutContType[device]`）に置き換える。Pure DI原則（§3.1）に沿うため、`IProfileSettingsService` に `OutContType` 相当のプロパティ（例: `OutContType[] OutputContDevice => _config.outputDevType;`）を新設し、そちら経由でアクセスする形を推奨する。 | `IProfileSettingsService.cs`, `ProfileSettingsService.cs`, `ProfileSettingsViewModel.cs` |
| (c)-1b（新規） | §6.1で判明した `SpecialActionsListViewModel.cs` の同一パターンを、同じ新プロパティ経由に置き換える。 | `SpecialActionsListViewModel.cs` |
| (c)-1c（新規・任意） | §6.2で判明した `MainWindow.xaml.cs` のUDP `query...outconttype` を同様に置き換える。影響度が低いため、本Stepに含めるか次Stepに回すかは要判断。 | `MainWindow.xaml.cs` |
| (c)-2（維持） | `ControllerTypeIndex`（是正後）と `EnableOutputDataToDS4` の双方向連動ガードは、原計画通り実施する。 | `ProfileSettingsViewModel.cs` |

### 8.2 `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` の扱いについて（要確認事項）

これらは実装上「デッドコード」（誰からも呼ばれない）と確認されたが、以下の2案が考えられ、**方針についてgwin7ok氏の判断を仰ぎたい**。

- **案1（最小修正・今回採用推奨）**: 上記8.1の置き換えのみ行い、`GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` 自体には手を加えない（削除もObsolete化もしない）。理由: これらを実際のViGEmプラグイン/アンプラグ処理に接続する作業は、`IUdpServerService`未接続の技術的負債（Phase5-Step14計画書§6で見積り2〜3人日と判定・スコープ外化された前例）と同種の規模の改修になり、Step14のスコープを超える。
- **案2（将来対応として明記）**: 案1に加え、`IOutputSlotService.cs` の該当4メンバに `// TODO(Phase6候補): 実ViGEmアタッチ処理と未接続` 等のコメントを付与し、`DI-App-Wide-Migration-Plan.md` の技術的負債一覧、または `Phase6-Plan.md` の監査対象候補として記録する。

本報告書としては**案2を推奨**する。理由は、`.github/copilot-instructions.md` のPure DI原則に照らし、「呼ばれないAPIが実体を持つふりをしている」状態を放置すると、Phase6以降の呼び出し元監査で再び見落とされるリスクがあるためである。

### 8.3 単体テストへの影響

`OutputSlotServiceTests.cs` の既存テスト（`InitialState_ShouldHaveDefaultTypes`, `SetOutputDeviceType_ShouldUpdateSlot` 等）は `OutputSlotService` 単体の動作を検証するものであり、8.1の変更（`ProfileSettingsViewModel` 側の参照先変更）によって影響を受けない。8.1の変更に対しては、`ProfileSettingsViewModelTests.cs`（存在する場合）または新規テストで、`Global.OutContType[device] = OutContType.DS4` 設定時に `ControllerTypeIndex` が `1` を返すことを検証するテストの追加を推奨する。

---

## 9. 本調査のNo Feature Drop 確認

本調査は読み取り専用の`grep`ベース監査のみであり、コード変更は一切行っていない。§8の対応方針は次のマイクロタスクとして別途承認を得てから実施する。

---

## 10. 次のアクション（承認待ち）

1. 本報告書の内容（特に§5の真因特定、§8.1の置き換え案、§8.2の案1/案2選択）についてgwin7ok氏の確認・承認を得る。
2. 承認後、`Phase5-Step14-FormSettings-Unification-Plan.md` フェーズCのタスク(c)-1・(c)-2の記載を本報告書の内容に基づき改訂する。
3. 改訂後の計画に基づき実装マイクロタスクに着手する。