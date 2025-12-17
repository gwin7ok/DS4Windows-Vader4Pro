# SpecialAction インスタンス管理設計

## 概要
この仕様は現行コード（`Mapping.cs` 等）から抽出したスペシャルアクション（SpecialAction）の型と振る舞いを整理し、各スペシャルアクションをインスタンスとして管理するための設計仕様を提示します。目的は以下：

- スペシャルアクションごとに状態を一意に保持し、複数デバイス／同一出力キーを用いる別 SA 間の干渉を防ぐ。 
- 可読性・保守性を高め、新しい SA タイプの拡張を容易にする。

## 現状（コードからの抽出要点）
- SpecialAction は設定上は名前（`action.name`）、トリガ（`action.trigger`）、untrigger（`action.uTrigger`）、`action.typeID`（Key/Button/Macro/…）などを持つ。
- `action.details` はタイプにより意味が変わる（Key 型なら論理キー番号、Button 型なら X360Controls 値、Macro 型ならパラメータ等）。
- 現状の状態管理は混在:
  - `actionDone[index].dev[device]` のように SA インデックス×デバイスで管理されている状態がある。
  - 一方 `pressedonce` は `bool[] pressedonce = new bool[2400]` でキー単位のグローバル配列になっており、同一出力キーを複数 SA が使うと干渉する。
  - Synthetic キー送出は `TryDispatchSATriggerEstablished/Released` を経て `KeyButtonActionController` に委譲される。
- 既存の実装で確認される主な SA タイプ：
  - Key（押下／トグル／repeat／ScanCode フラグ）
  - Button（X360Controls 出力、マウス出力等）
  - Macro（`action.macro` と `action.details` を使う）
  - MultiAction / XboxGameDVR / その他（DisconnectBT, BatteryCheck 等）

## 問題点（抜粋）
- `pressedonce` がキー単位でグローバル管理され、SA 単位／デバイス単位の抑止ができていない。
- `Mapping.cs` 内でロギングや処理が重複していた箇所があり、ログの二重出力等が発生していた（既に一部修正済）。
- 行為の分離が行われていないため、`Mapping` にロジックが集中し可読性が低下している。

## 目標設計（要旨）
1. 各 SpecialAction をオブジェクト（インスタンス）で表現する。
2. 各インスタンスはデバイス毎の内部状態（actionDone、pressedOnce など）を持つ。
3. `Mapping` はトリガ検出までに専念し、トリガ／リリース通知を該当インスタンスへ委譲する。
4. 既存外部 API（`TryDispatchSATriggerEstablished/Released` 等）は暫定的にラップして互換性を保つ。

## クラス設計（骨格）

- `abstract class SpecialActionBase`
  - プロパティ（共通）:
    - `string Name`、`SpecialAction.ActionTypeId TypeId`、`string Details`、`List<DS4Controls> Trigger`、`List<DS4Controls> UTrigger`、`DS4KeyType KeyType`、`KeyButtonSwitchMode? KeyButtonSwitchMode`、`string Macro`、`string Extras` など
  - デバイス毎状態（基底に保持）:
    - `private ActionInstanceState[] states` または `Dictionary<int, ActionInstanceState>`（サイズ: `Global.MAX_DS4_CONTROLLER_COUNT`）
    - `class ActionInstanceState { bool ActionDone; bool PressedOnce; SyntheticState.KeyPresses KeyState; DateTime LastTriggered; /* 他 */ }`
  - メソッド（仮）:
    - `virtual void OnTrigger(int device, DeviceState context)`
    - `virtual void OnRelease(int device, DeviceState context)`
    - `virtual void ResetDevice(int device)`
    - `ActionInstanceState GetState(int device)`

- 派生クラス（例）
  - `class KeyAction : SpecialActionBase`
    - 追加フィールド: `ushort KeyId`（`Details` をパース済み）
    - `override OnTrigger`:
      - toggle / press 判定、デバウンス（`LastToggleTime`）、状態反転、`KeyButtonActionController` 経由で合成送信、`states[device].PressedOnce` を制御する。
    - `override OnRelease`:
      - untrigger ロジック（`untriggerindex` 相当）、KeyButtonActionController 経由で Release。
  - `class ButtonAction : SpecialActionBase` — ゲームパッド出力、Mouse 出力等の実装
  - `class MacroAction : SpecialActionBase` — `PlayMacro` を呼ぶ、Extras で Repeat 等を考慮
  - `class MultiAction : SpecialActionBase` — 内部で複数アクションを順次呼ぶ

## 重要データ構造例
- `List<SpecialActionBase> Actions` — プロファイル読み込み時にファクトリで生成
- 各インスタンス内部:
  - `ActionInstanceState[] states = new ActionInstanceState[Global.MAX_DS4_CONTROLLER_COUNT];`
  - `struct ActionInstanceState { bool actionDone; bool pressedOnce; long lastToggleTimeUtcTicks; /* 必要なら native key alias 参照 */ }

## API（Mapping との接続）
- Mapping 側でトリガ判定が行われたら:
  - `var sa = SpecialActionManager.GetActionByIndex(index);`（または name）
  - `if (triggered) sa.OnTrigger(device, context); else sa.OnRelease(device, context);`
- 既存の `TryDispatch...` は新 API の内部から呼ぶか置き換える。

## 移行計画（段階的）
1. `SpecialActionBase` のインターフェースと `KeyAction` の最小実装を追加（並列で動作可能にする）。
2. Mapping から該当トリガ検出後、`SpecialActionBase.OnTrigger` を呼ぶパスを追加（旧ロジックは残す）。
3. 動作比較（ログ／実機）を行い、差分を潰す。
4. 他の派生（Button/Macro/…）を逐次移行。
5. 旧グローバル状態（`pressedonce` など）を削除してクリーンアップ。

## ログと診断
- 各インスタンスは自身のログプレフィックス（例: `SA[<index>|<name>]`）を持ち、`KeyTriggered`/`KeyReleased`/`toggle-flipped` を出力する。
- 既存の多重ログ発生箇所は排除する（Mapping でのログを最小化し、インスタンス側で一貫してログを出す）。

## テストケース（必須）
- 単一 SA（Key, Toggle の on/off）を 1 台のデバイスで確認。
- 同一 `Details`（同一出力キー）を使う複数 SA を 1 台・複数デバイスで動作確認（干渉がないこと）。
- Repeat/Delay の既存動作と互換性を確認（`KeyboardSettings` の値に従うこと）。
- Edge cases: rapid toggles（debounce）、uTrigger を持つ SA、Macro の Repeat オプション。

## 実装リスクと留意点
- 大規模リファクタのため段階的移行が必須。まず `KeyAction` だけ実装して Mapping に並列パスを用意すること。
- パフォーマンス: 大規模プロファイルでのループ回数増加に注意（インスタンス呼び出しが増えるが、C# の呼び出しオーバヘッドは小さい）。
- 既存プロファイルとの互換性: シリアライズ／デシリアライズ層で migration ルールを用意する。

---

作業の次のステップ提案:
- 1) `SpecialActionBase` + `KeyAction` の最小プロトタイプを実装して Mapping の並列ルートを追加（私がパッチ作成可）。
- 2) テスト実行（Toggle/Press/Repeat を含むケース）。

どちらを希望しますか？
