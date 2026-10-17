# Actions インターフェース仕様

この仕様書は、DS4Windows の入力→出力経路を安定してリファクタするための最小限のインターフェース群と責務を定義します。以降の実装・テストはこの仕様を基準として進めます。

---

**目的**: 入力判定（IInputAction）と出力送出（IOutputAction）を明確に分離し、繰返し（IRepeater）や実行コンテキスト（IActionController）を抽象化することで、単体テスト可能で拡張しやすい設計にする。

## 主要インターフェース（C# 風シグネチャ）

- **ITriggerContext** (データ DTO)
  - プロパティ:
    - `int Device { get; }`
    - `bool IsEdgeEstablished { get; }` // established=true / released=false
    - `ushort LogicalValue { get; }`
    - `uint NativeValue { get; }`
    - `DateTime Timestamp { get; }`
    - `DS4Windows.DS4Control.VirtualKBMBase OutputHandler { get; }` // 実装で必要なハンドラ参照
  - 役割: トリガ（物理入力）を表現する軽量 DTO。副作用なし。

- **IOutputContext** (データ DTO)
  - プロパティ:
    - `int Device { get; }`
    - `DS4Windows.DS4Control.VirtualKBMBase OutputHandler { get; }`
    - `IDictionary<string, object> Meta { get; }` // 必要に応じた拡張メタ
  - 役割: 出力アクションが送出を行うためのコンテキスト。

- **IInputAction** (入力判定)
  - プロパティ: `string Name { get; }`
  - メソッド:
    - `ITriggerContext Evaluate(DS4Windows.DS4Library.DS4State state)` // 単回評価、未発生は null
    - `void Reset()`
  - 役割: 単一ボタン、複合条件、長押し等の判定ロジックをカプセル化。副作用なし。

- **IOutputAction** (出力実処理)
  - プロパティ: `string Id { get; }`
  - メソッド:
    - `void Execute(IOutputContext ctx)` // 押下／マクロの開始
    - `void Stop(IOutputContext ctx)` // 解放や終了処理
  - 役割: VirtualKBMBase 等を用いた低レベル送出ロジックを提供。

- **IActionBinding** (入力と出力の結合)
  - プロパティ:
    - `IInputAction Input { get; }`
    - `IReadOnlyList<IOutputAction> Outputs { get; }`
  - メソッド:
    - `void OnTriggered(ITriggerContext trigger)` // established
    - `void OnReleased(ITriggerContext trigger)` // released
  - 役割: 入力→出力のライフサイクル（押下時 Execute、解除時 Stop）を定義。Toggle/Press の振る舞い差分はここで表現可能。

- **IActionController** (デバイス×バインドのランタイム実行器)
  - プロパティ/イベント: `int ControllerId { get; }` (必要に応じ Event を追加)
  - メソッド:
    - `void Start(IActionBinding binding, ITriggerContext trigger)` // binding を開始（PressedOnce の管理やリピータ生成）
    - `void Stop(IActionBinding binding, ITriggerContext trigger)` // binding を停止
    - `void Clear()` // 内部状態とリソースを解放
  - 役割: `RepeatHelper`（IRepeater）を生成・管理し、`ActionManager.SetPressedOnce(...)` を呼ぶ（single-writer ポリシーの順守）。

- **IRepeater** (繰返し抽象)
  - メソッド:
    - `void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)`
    - `void Stop()`
    - `void Dispose()`
  - 役割: タイミング責務のみを担う。DI/モック可能で単体テスト容易。

- **IOutputHandler** (低レベル出力ハンドラ抽象)
  - プロパティ: `bool FakeKeyRepeat { get; }` // 実稼働では `false` 推奨
  - メソッド: `void PerformKeyPress(uint vk)`, `void PerformKeyRelease(uint vk)`, 必要に応じ Mouse 等のメソッド
  - 役割: `VirtualKBMBase` をラップする抽象。SendInput 等の具体実装がこれを実装する。

- **IActionRegistry / IControllerRegistry** (補助レジストリ)
  - 役割: `IActionBinding` と `IActionController` の登録・検索・ライフサイクル管理を行う（ActionManager の下位責務）。

## 実装済み（このリファクタで追加／変更した主なファイル）

- `DS4Windows/Actions/IRepeater.cs` — 繰返し抽象の定義。
- `DS4Windows/Actions/RepeatHelperToIRepeaterAdapter.cs` — 既存 `RepeatHelper` を `IRepeater` として使えるアダプタ。
- `DS4Windows/Actions/TriggerContextImpl.cs` — `ITriggerContext` の具象。
- `DS4Windows/Actions/KeyActionBinding.cs` — `IActionBinding` の簡易実装（SpecialAction をラップ）。
- `DS4Windows/Actions/KeyButtonActionControllerAdapter.cs` — 既存 `KeyButtonActionController` を `IActionController` としてラップするアダプタ。
- `DS4Windows/DS4Control/KeyAction.cs` — トリガ検知から `IActionController.Start/Stop` を呼ぶよう変更。
- `DS4Windows/DS4Control/Mapping.cs` — 合成経路において `IActionController` を利用するよう変更。
- `DS4Windows/DS4Control/ActionManager.cs` — コントローラ取得経路を `IActionController` を返すように更新。
- `DS4Windows/DS4Control/OutputKBM/SendInputHandler.cs` — `fakeKeyRepeat` を `false` に設定（内部偽リピート無効化）。

これらの変更により、現時点ではキー系 SpecialAction（Key 型、Press/Toggle）は既存ロジックを維持しつつ `IActionController`/`IRepeater` 抽象を通して呼び出されます。段階的に他 SA を同様に移行できます。

## 設計上の注意点

- Single-writer ポリシー: `PressedOnce` の変更は `ActionManager.SetPressedOnce(...)` を通じてのみ行う。コントローラはこれを呼ぶ唯一の書き手となる。
- テスト容易性: `IRepeater` と `IOutputHandler` をモックできるため、タイミング依存コードの単体テストが可能。
- スレッド安全: `IActionController` 内でタイマー（IRepeater）から呼ばれる `tickAction` はスレッドセーフに扱う。UI へ戻す必要がある場合は `SynchronizationContext` を使う。
- 互換性: まずはアダプタで既存コントローラをラップし、段階的に内部を差し替える。互換性破壊は最小限に留める。

## 今後の推奨作業（優先順）

1. `IRepeater` の純粋なタイマー実装（テスト用）を追加し、DI 可能な形で `KeyButtonActionController` に注入する。
2. マウス・マクロなど他の SA に対して `IActionController` 実装を追加し、`Mapping` 経路を通して一貫した実行モデルに統一する。
3. ストレステストを自動化して Start/Stop/Dispose の時系列をログで検証する。
4. `IActionRegistry` を作成してコントローラのライフサイクル管理・一覧取得・テスト用フックを提供する。

---

ファイル参照: 実装ファイルは `DS4Windows/Actions/` と `DS4Windows/DS4Control/` 下にあります。今後の実装・レビューはこの仕様書を基準にしてください。

作成日: 2025-12-19
