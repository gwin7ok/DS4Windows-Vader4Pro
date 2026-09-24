# Phase6-Step4 計画書: マウスエミュレーション系（`Mouse.cs`, `MouseCursor.cs`, `MouseWheel.cs`）のGlobal直参照解消

作成日: 2026-09-09  
改訂日: 2026-09-24（Step3 完了後の実地突き合わせにより、参照台帳を現行コードから全面再作成。Abs Mouse 削除の反映、契約追加ゼロの確認、フォールバックなしの必須引数への方針変更）  
旧改訂: 2026-09-18（Pure DI コンストラクタ引数注入 ＋ `MouseWheel.cs` 統合の採用）  
状態: **Step4-0（台帳再作成）完了・承認待ち（実装未着手）**  
対象ブランチ: `For-DI-migration-work`  
台帳作成時の HEAD: `c6eb492f`  
上位計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`  
準拠指針: `.github/copilot-instructions.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`  
参照エビデンス:  
  - `Phase6-Step1-Core-Reference-Evidence.md` §4, §5, §8（旧台帳の根拠。**行番号・メンバー名は現行と一致しないため、本書 §2 を正本とする**）  
  - `Phase6-Step3-Reality-Check-Ledger.md`（同形式の実地突き合わせ台帳）  

---

## 0. 背景と改訂の経緯

### 0.1 旧台帳が使えなくなった理由
旧台帳（2026-09-18）は Step1 の走査結果に基づき「73件（Mouse 47 / MouseCursor 20 / MouseWheel 6）」としていた。Step3 完了後に現行コードと突き合わせた結果、次の乖離が見つかった。

- **参照メンバーの名前が別物**: 旧台帳は `Global.TouchpadButton[..]` の配列参照38箇所などを想定していた。現行コードは `Global.GetGyroOutMode`、`Global.getGyroSensitivity`、`Global.GyroMouseStickInf[..]`、`Global.TouchOutMode[..]`、`Global.GetTouchMouseStickInfo`、`Global.GetDS4CSetting` などである。
- **Abs Mouse 関連（旧 C4-C17〜C19）は消滅**: `absUseAllMonitors`、`TranslateCoorToAbsDisplay`、`MoveAbsoluteMouse` は、Step3-3／`Phase6-Status.md` §6.8 でユーザー承認のもと機能ごと削除済み。旧計画にあった「`IAppSettingsService.AbsUseAllMonitors` の追加」「`IEnvironmentService` への座標変換シム追加」は**不要**。
- **生成元が違う**: 旧計画は `DS4Device.cs` が `new Mouse` するとしていたが、実際の生成元は `ControlService.cs:2187`（`touchPad[index] = new Mouse(index, device);`）の1箇所のみ。`MouseCursor`／`MouseWheel` は `Mouse.cs:122-123` で生成される。テストコードに `new Mouse` はない。
- **件数が変わった**: 実参照（コメント・偽陽性・除外を除く）は **81行**（Mouse 56 / MouseCursor 19 / MouseWheel 6）、除外は `Global.Clamp` の4行。

### 0.2 方針（確定）
1. **Pure DI コンストラクタ引数注入**: 各クラスが `IProfileSettingsService` と `IVirtualKBM` を受け取り、`private readonly` フィールドに保持する。毎レポート実行されるホットパスでは、フィールド参照＋既存契約の呼び出しのみ（ゼロアロケーション）。
2. **フォールバックなしの必須引数（ユーザー決定 2026-09-24）**: 旧計画にあった `= null` の省略可能引数と `?? AppHost.GetService<T>()`／`?? Global.ProfileSettingsServiceInstance` は**採用しない**。理由は、生成元が `ControlService` 1箇所のみで、そこは既に Pure DI 済みであること（Step2 決定D2「新規引数は必須」と整合）、`copilot-instructions.md` §3.1 の Service Locator 排除に沿うこと。引数が null なら `ArgumentNullException` を投げる（`ControlService` の既存の書き方に合わせる）。
3. **`MouseWheel.cs` の正式統合**: マウスエミュレーションを構成する3ファイルを1つの Step で解消する（旧計画から継続）。
4. **契約の追加は不要（実地確認済み）**: 現行の `IProfileSettingsService`／`IVirtualKBM` に、置換に必要なメンバーがすべて揃っている（§2.4）。ユーザー決定「契約に無い `GetTouchActive` 等の追加は Step4-2/3 内で行う」は、確認の結果 `GetTouchActive` は `IProfileSettingsService.GetTouchpadActive` で代替でき、**追加対象なし**となった。実装中に不足が見つかった場合のみ Step4-2/3 内で最小限に追加する。
5. **明示的除外（4行）**: `Global.Clamp`（Mouse.cs 785, 786、MouseCursor.cs 334, 335）は状態を持たない純粋計算のため除外する（Phase6-Plan §0.3-5）。

---

## 1. 目的

1. `Mouse`, `MouseCursor`, `MouseWheel` の3クラスにコンストラクタ経由の Pure DI 注入を確立する。
2. 3ファイル内の実参照81行を、注入されたサービス経由の呼び出しへ置換する。
3. マウス・ジャイロ・ホイールの超高頻度ホットパス（〜1000Hz）で、GC アロケーションと入力遅延を増やさない。

---

## 2. 参照台帳（現行コードからの再作成、実参照81行）

行番号は台帳作成時（HEAD `c6eb492f`）のもの。実装時は各バッチの直前に再確認する。

### 2.1 `MouseWheel.cs`（6行）
すべて分類(a)（既存契約で置換可）、ホットパス。

- 55行 `Global.ScrollSensitivity[deviceNumber]` → `_profileSettings.ScrollSensitivity[deviceNumber]`
- 82, 83, 85, 86行 `Global.outputKBMMapping.WHEEL_TICK_DOWN`／`WHEEL_TICK_UP` → `_profileSettings.OutputKBMMapping.WHEEL_TICK_DOWN`／`WHEEL_TICK_UP`
- 88行 `Global.outputKBMHandler.PerformMouseWheelEvent(yAction, xAction)` → `_virtualKBM.PerformMouseWheelEvent(yAction, xAction)`

### 2.2 `MouseCursor.cs`（19行＋除外2行）
すべて分類(a)。

- **`GyroMouseInfo[..]`（9行）**: 34, 35, 40, 46, 47, 48, 49, 92, 274行。`Global.GyroMouseInfo[deviceNum]` → `_profileSettings.GyroMouseInfo[deviceNum]`。34・35行はコンストラクタ内、40・46〜49行はフィルター再設定系のメソッド内（ホットパスではない）。92・274行はホットパス。
- **ジャイロ軸・感度・反転（4行）**: 86行 `getGyroMouseHorizontalAxis` → `GetGyroMouseHorizontalAxis`、96行 `getGyroSensitivity` → `GetGyroSensitivity`、143行 `getGyroSensVerticalScale` → `GetGyroSensVerticalScale`、252行 `getGyroInvert` → `GetGyroInvert`。
- **マウス移動出力（2行）**: 260, 428行 `Global.outputKBMHandler.MoveRelativeMouse` → `_virtualKBM.MoveRelativeMouse`。
- **タッチ相対マウス（1行）**: 327行 `Global.TouchRelMouse[deviceNumber]` → `_profileSettings.TouchRelMouse[deviceNumber]`。
- **タッチ感度・ジッター・反転（3行）**: 343行 `getTouchSensitivity` → `TouchSensitivity[deviceNumber]`、344行 `getTouchpadJitterCompensation` → `TouchpadJitterCompensation[deviceNumber]`、419行 `getTouchpadInvert` → `TouchpadInvert[deviceNumber]`。
- **除外（2行）**: 334, 335行 `Global.Clamp`。

### 2.3 `Mouse.cs`（56行＋除外2行、コメント5行は対象外）
すべて分類(a)。

- **ジャイロ出力モード（3行）**: 228, 420, 444行 `Global.GetGyroOutMode(deviceNum)` → `_profileSettings.GetGyroOutMode(deviceNum)`。
- **ジャイロ感度（1行）**: 248行 `getGyroSensitivity` → `GetGyroSensitivity`。
- **ジャイロトリガー系（8行）**: 279行 `GetGyroSwipeInfo`、330行 `getGyroTriggerTurns` → `GetGyroTriggerTurns`、333行 `GetGyroControlsInfo`、339行 `getSATriggers` → `GetSATriggers`、340行 `getSATriggerCond` → `GetSATriggerCond`、344行 `GetGyroMouseStickTriggerTurns`、345行 `GetSAMouseStickTriggers`、346行 `GetSAMouseStickTriggerCond`。
- **`GyroMouseStickInf[..]`（7行）**: 390, 399, 400, 401, 402, 423, 447行 → `_profileSettings.GyroMouseStickInf[deviceNum]`。
- **ジャイロスティック情報・軸（3行）**: 463, 482行 `GetGyroMouseStickInfo`、475行 `getGyroMouseStickHorizontalAxis(0)` → `GetGyroMouseStickHorizontalAxis(0)`。※475行の引数が `deviceNum` でなく `0` 固定なのは既存の挙動。挙動維持のためそのまま移す（K4-1 として記録）。
- **`TouchOutMode[..]`（9行）**: 407, 432, 1105, 1212, 1271, 1297, 1538, 1789, 1834行 → `_profileSettings.TouchOutMode[deviceNum]`。
- **`GetTouchMouseStickInfo`（4行）**: 410, 778, 1164, 1384行 → `_profileSettings.TouchMouseStickInf[deviceNum]`。※`Global.GetTouchMouseStickInfo` は `m_Config.touchMStickInfo[device]`、サービス側は `SafeConfig?.touchMStickInfo` を返す。通常は同じ配列（`SafeConfig` は null 時のみ別扱い）。等価性は Step4-2/3 で単体テストにより固定する。
- **タッチパッド有効状態（2行）**: 1108, 1165行 `GetTouchActive` → `_profileSettings.GetTouchpadActive(deviceNum)`。※`Global.touchpadActive` はプロファイル設定サービスの `TouchpadActiveArray` と同一の状態（`Global` 側はシムで委譲）。範囲外の添字では、`Global` は例外、サービスは `true` を返すが、`deviceNum` は常に有効範囲内。
- **タッチ無効化の反転トリガー（3行）**: 1110, 1300, 1547行 `getTouchDisInvertTriggers` → `TouchDisInvertTriggers[deviceNum]`。
- **トラックボール（3行）**: 1118, 1134, 1308行 `getTrackballMode` → `GetTrackballMode`。
- **ダブルタップ（2行）**: 1241, 1284行 `getDoubleTap` → `GetDoubleTap`。
- **タップ感度（3行）**: 1244, 1839行 `TapSensitivity[deviceNum]` → `_profileSettings.TapSensitivity[deviceNum]`、1270行 `getTapSensitivity` → `TapSensitivity[deviceNum]`。※`Global.getTapSensitivity` の実装が単純な配列参照であることを Step4-3 の直前に確認する。
- **タッチボタンモード（2行）**: 1656, 1865行 `TouchpadButtonMode[deviceNum]`。
- **タッチクリックパススルー（1行）**: 1792行 `TouchClickPassthru[deviceNum]`。
- **`GetDS4CSetting`（4行）**: 1805, 1816, 1822, 1828行 `GetDS4CSetting(deviceNum, DS4Controls.Touch*)` → `_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.Touch*)`。
- **`LowerRCOn`（1行）**: 1878行 → `_profileSettings.LowerRCOn[deviceNum]`。
- **除外（2行）**: 785, 786行 `Global.Clamp`。
- **コメント内の参照（対象外、5行）**: 129, 130, 777, 1187, 1385行。既存コメントは消さない（copilot-instructions §3.3 原則4）。

### 2.4 契約の充足確認（追加ゼロ）
- `IProfileSettingsService` に存在（確認済み）: `ScrollSensitivity`、`OutputKBMMapping`、`GyroMouseInfo`、`GyroMouseStickInf`、`TouchMouseStickInf`、`TouchRelMouse`、`TouchOutMode`、`TouchDisInvertTriggers`、`TouchpadButtonMode`、`TouchClickPassthru`、`TapSensitivity`、`TouchSensitivity`、`TouchpadInvert`、`TouchpadJitterCompensation`、`LowerRCOn`、`GetGyroOutMode`、`GetGyroSensitivity`、`GetGyroSensVerticalScale`、`GetGyroInvert`、`GetGyroMouseHorizontalAxis`、`GetGyroMouseStickHorizontalAxis`、`GetGyroMouseStickInfo`、`GetGyroSwipeInfo`、`GetGyroControlsInfo`、`GetGyroTriggerTurns`、`GetGyroMouseStickTriggerTurns`、`GetSATriggers`、`GetSATriggerCond`、`GetSAMouseStickTriggers`、`GetSAMouseStickTriggerCond`、`GetTrackballMode`、`GetDoubleTap`、`GetTouchpadActive`、`GetDS4CSetting`。
- `IVirtualKBM`（名前空間 `DS4Windows.Services`）に存在: `MoveRelativeMouse`、`PerformMouseWheelEvent`。実装 `OutputKBMHandlerAdapter` は `Global.outputKBMHandler` へ**呼び出しごとに遅延委譲**するため、KBM ハンドラの差し替え・null 化があっても、`Mouse` がハンドラを保持し続けて古い実体を呼ぶ問題は起きない。
- 注入元の実体: `ControlService` が既に `_profileSettings`（`ControlService.cs:108`）と `_virtualKBM`（同 131）を保持している。

---

## 3. アーキテクチャ設計・Pure DI 伝搬

### 3.1 コンストラクタ設計（フォールバックなし）
```csharp
// Mouse.cs
public Mouse(int deviceID, DS4Device d, IProfileSettingsService profileSettings, IVirtualKBM virtualKBM)
{
    _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
    _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
    deviceNum = deviceID;
    dev = d;
    cursor = new MouseCursor(deviceNum, d.GyroMouseSensSettings, _profileSettings, _virtualKBM);
    wheel = new MouseWheel(deviceNum, _profileSettings, _virtualKBM);
    ...
}
```
- `MouseCursor(int, GyroMouseSens, IProfileSettingsService, IVirtualKBM)`、`MouseWheel(int, IProfileSettingsService, IVirtualKBM)` も同様（null なら `ArgumentNullException`）。
- 3ファイルとも、`IProfileSettingsService` は `DS4Windows.DI`、`IVirtualKBM` は `DS4Windows.Services` の名前空間。必要な `using` のみ追加し、名前空間は変更しない（§3.3 原則3）。
- `MouseCursor` のコンストラクタ（34, 35行）は `_profileSettings` を使うため、フィールド代入をコンストラクタ冒頭で行う。

### 3.2 生成元の配線
`ControlService.cs:2187` を `touchPad[index] = new Mouse(index, device, _profileSettings, _virtualKBM);` に変更する。これ以外に生成元はない。

---

## 4. ホットパス性能維持とガードレール

1. **ゼロアロケーション**: `sixaxisMoved`／`touchesMoved` 等でオブジェクト生成・クロージャ・LINQ を追加しない。`Get*` 系は既存契約と同じ実装（配列の参照返し、コピーなし）を呼ぶ。
2. **配列の参照を維持**: `TouchOutMode[deviceNum]` のような配列参照は、`Global` 側とサービス側で同じ `BackingStore` の同じ配列を返す（`Clone()`・ログ・キャッシュなし）ことを、各バッチでテストにより固定する。
3. **ホットパス内でのサービス解決・ログの禁止**: `AppHost.GetService` や `[DI]` ログを呼ばない。コンストラクタで受け取った `readonly` フィールドだけを使う。
4. **挙動の完全維持**: 置換は読み取り元の差し替えのみとし、条件分岐・評価順・引数（475行の `0` 固定を含む）を変えない。

---

## 5. マイクロステップ

各ステップの後に `dotnet build` と `dotnet test` の全件グリーンを確認する（この環境で dotnet を実行できない場合はユーザー側で実行）。コミットは Step ごと。

```text
【Phase6-Step4 マイクロステップ構成】
├─ Step4-0: 台帳の再作成・計画書改訂（本書。実装なし）  ← 完了
├─ Step4-1: MouseWheel.cs（6行）
├─ Step4-2: MouseCursor.cs（19行）
├─ Step4-3: Mouse.cs（56行）
├─ Step4-4: ControlService.cs の生成箇所の配線（1箇所）
└─ Step4-5: テスト・文書・実機確認・完了報告
```

### Step4-0: 台帳の再作成・ベースライン記録（実装なし）【完了】
- 現行コードから §2 の台帳を再作成し、契約の充足（§2.4）を確認した。
- **ベースライン記録（2026-09-24、ユーザー確認）**: HEAD `c6eb492f` の時点で、ビルド・テストビルド・テスト実行がすべて成功。Step3-7 の確認結果は `Phase6-Status.md` §3.8 を参照。

### Step4-1: `MouseWheel.cs`
- `MouseWheel` のコンストラクタに `IProfileSettingsService`／`IVirtualKBM` を追加。§2.1 の6行を置換。
- この時点では `Mouse.cs:123` の生成が旧シグネチャのままだとビルドできないため、**Step4-1〜4-4 は1つのブランチ上で連続して実装し、Step4-4 まで完了してからビルド・テストを行う**（各ステップの単体確認は、テスト側で新しいコンストラクタを直接呼ぶ形で先行して行える）。
- テスト: `MouseWheelShimTests`（`PerformMouseWheelEvent` の呼び出し値、`WHEEL_TICK_*` の乗算）。

### Step4-2: `MouseCursor.cs`
- コンストラクタに2サービスを追加。§2.2 の19行を置換。Abs 系はなし。
- テスト: `MouseCursorMovementTests`（`MoveRelativeMouse` の呼び出し）、`GyroMouseInfo`／`TouchRelMouse` 等が同じ配列であることの検証。

### Step4-3: `Mouse.cs`
- コンストラクタを `(int, DS4Device, IProfileSettingsService, IVirtualKBM)` に変更し、`cursor`／`wheel` へ伝搬。§2.3 の56行を置換。
- `GetTouchMouseStickInfo`（4行）と `GetTouchActive`（2行）の等価性テストを追加（§2.3 の注記）。

### Step4-4: 生成元の配線
- `ControlService.cs:2187` を `new Mouse(index, device, _profileSettings, _virtualKBM)` に変更。
- ここで全体をビルドし、`dotnet test` 全件成功を確認する。

### Step4-5: テスト・文書・実機確認・完了報告
- テスト（1ファイル1型、`DS4WindowsTests` に新規）:
  - `MouseDiWiringTests`: `Mouse` 生成時に `cursor`／`wheel` へ同じサービスが伝搬すること、null 引数で `ArgumentNullException`。
  - `MouseWheelShimTests`、`MouseCursorMovementTests`（上記）。
  - `MouseGlobalReferenceGuardTests`: 3ファイルのソースを走査し、`Global.Clamp` 以外の `Global.` 参照（コメントを除く）が0であることを固定（Step3 の `Mapping*GlobalReferenceGuardTests` と同形式）。
  - ホットパスの割り当て0バイトのテスト（`MappingHotPathAllocationTests` と同形式。`sixaxisMoved`／`touchesMoved` は実際の入力を送出するため、直接呼べない経路は読み取り部分のみ検証し、残りは実機確認で担保）。
- 文書更新: `Phase6-Status.md`（Step4 の進捗、§3.8 相当の詳細）、`Phase6-Plan.md`（Step4 の件数と方針）。契約の追加がなければモデル図の変更は不要。
- 完了報告書 `Phase6-Step4-Completion-Report.md` を作成。

---

## 6. テスト・性能・実機検証計画

### 6.1 自動テスト
§5 Step4-5 のとおり。既存テストが全件成功を維持すること。

### 6.2 ホットパス性能
- 判定基準: 置換前後で、読み取り経路の割り当てが0バイトのままであること。マイクロベンチマーク（処理時間 ±5%）は、テストで直接駆動できる範囲でのみ実施し、困難な場合は実機での体感確認に代える（Mapping の直接駆動が困難だった Step3 の前例と同様）。

### 6.3 実機動作検証項目（Vader 4 Pro／DS4／DualSense）
1. **タッチパッド（マウスモード）**: 指の移動によるカーソル移動、タップ、左右クリック、ダブルタップ。
2. **タッチパッド（スティック・ホイール等のモード）**: タッチによるスティック出力、上下・左右のスクロールホイール。
3. **タッチボタン**: 左・右・上・マルチの各割り当てとクリックパススルー。
4. **ジャイロ**: ジャイロマウスの追従、スムージング、デッドゾーン、ジャイロ→スティック、トリガー条件（`SATriggers`）。
5. **接続・切断の繰り返し**: `Mouse` の再生成経路（`ControlService.cs:2187`）が壊れていないこと。
- 環境がなく実施できない項目は、`Phase6-Step11-Plan.md` §3.3 の先送り台帳に登録する。

---

## 7. ロールバック方針

1. **ステップ単位の差し戻し**: 不具合時は該当コミットを `git revert` する。
2. **フォールバックの扱い**: 旧計画にあった `= null` フォールバックは設けない。旧経路へ戻す場合はコミットの差し戻しで行う（生成元が1箇所のため影響範囲は小さい）。

---

## 8. 持ち越し・記録事項
- **K4-1**: `Mouse.cs:475` の `getGyroMouseStickHorizontalAxis(0)` は引数が `0` 固定（`deviceNum` でない）。他のデバイスでは設定と異なる軸を読む可能性がある既存の挙動。Step4 では挙動維持のためそのまま移し、修正要否はユーザー判断とする。
- **K4-2**: `Mouse.cs`／`MouseCursor.cs` の `Global.Clamp`（4行）は除外のまま残る。Step12 の呼出元0件判定でも削除対象にしない（純粋計算ユーティリティ）。

---

## 9. 完了判定チェックリスト

- [x] Step4-0: 台帳再作成（本書 §2）完了、ベースラインのビルド・テスト成功を記録（2026-09-24）
- [ ] `MouseWheel`／`MouseCursor`／`Mouse` の全コンストラクタが必須引数の Pure DI 注入になっている（フォールバックなし）
- [ ] 3ファイルの `Global.` 参照が、コメントと `Global.Clamp`（4行）を除いて0件
- [ ] `ControlService.cs:2187` から `_profileSettings`／`_virtualKBM` が配線されている
- [ ] ホットパスの割り当て0バイトがテストで固定されている
- [ ] `dotnet build -c Release` の警告・エラー0件、`dotnet test` 全件成功
- [ ] 実機確認（§6.3）が完了、または Step11 の先送り台帳に登録済み
- [ ] `Phase6-Status.md`／`Phase6-Plan.md` を更新、完了報告書を作成
