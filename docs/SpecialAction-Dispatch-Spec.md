# SpecialAction Dispatch 設計仕様書

最終更新: 2025-12-16
作成者: （自動生成）

## 目的
- SpecialAction（Key / Button）のトリガ成立以降の振る舞いをモード別（Press / Toggle）に明確に分離し、合成イベント（KeyPress 等）の送出処理を共通モジュールで再利用可能にする。
- 既存の `ToggleRepeatController` を負担能力として残しつつ、責務を拡張して `ToggleActionController` として整備する。Press モード用には `PressActionController` を新設する。
- ButtonType（ゲームパッドボタン等）にも適用可能な汎用設計とし、実装中の仕様変更は必ずこのドキュメントに反映する運用ルールを採用する。

## 運用ルール（必須）
- 仕様の確認はこの `.md` ファイルを基準に行う。疑問点はまずこのファイルを参照してから質問すること。
- 実装作業中に仕様変更が必要になった場合、必ず私（ユーザ）に確認を取り、承認後にこのファイルへ変更点を追記する。追記は「変更履歴」セクションへ日時と内容、承認者を明記する。

## 用語定義
- トリガ成立（Trigger）：ユーザが定義した SpecialAction の入力条件が満たされた状態。
- Toggle ON/OFF：トグル型 SpecialAction の内部状態（ON のとき合成キーを継続送出する場合がある）。
- Press：押下中に合成キーを送出するモード（長押しで繰り返し送出する場合あり）。
- Synthetic send：仮想キー/ボタンの送出（PerformKeyPress/Release 等）。

## 主要コンポーネント
- `ToggleActionController`（旧 `ToggleRepeatController` の拡張）
  - 役割: Toggle モードのライフサイクル（トリガ成立 → ON/OFF）管理と、ON 時の繰り返し送出を担当する。
  - API:
    - `OnToggleOn(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)`
    - `OnToggleOff(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)`
    - `Update()` — 主ループから周期的に呼ぶ（繰り返し送出）。
    - `ClearKeyEntries(ushort kvpKey)` — キーに紐づく内部エントリを消去。

- `PressActionController`（新設）
  - 役割: Press モードのトリガ成立中の送出（初回押下、オプションでの長押し繰り返し）を管理する。Toggle と異なり「トリガが押されている間」を基準に動作する。
  - API（Toggle と整合）:
    - `OnPressDown(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler, bool enableRepeat)`
    - `OnPressUp(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)`
    - `Update()` — 主ループから周期的に呼ぶ。
    - `ClearKeyEntries(ushort kvpKey)`

- `SyntheticDispatcher`（新設、共通モジュール）
  - 役割: 実際の合成送出ロジックを集中管理する（nativeKey 解決、ScanCode/VK の優先、送出スロットリング、送出ログ、`lastSyntheticSendUtcTicks` と `repeatCount` の更新）。
  - API（概略）:
    - `SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)`
    - `SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, VirtualKBMBase handler)`
    - `ResetKeyTiming(int device, ushort kvpKey)`
    - `ResolveNativeKey(ushort kvpKey)`

### 現状の実装（2025-12-16 以降の反映）
- `KeyButtonActionController`（実装上の中核）
  - 役割: デバイス毎に生成される SpecialAction 用コントローラ。Toggle / Press のいずれのモードでも Mapping が選択して該当する挙動を `KeyButtonActionController` 経由で処理する設計になっています。これによりグローバルな静的コントローラは廃止され、デバイス独立のライフサイクル管理が可能になりました。
  - 特徴:
    - デバイス単位で `KeyButtonActionController` インスタンスを生成/保持する（`Mapping.GetOrCreateKeyButtonController(device, mode)` 相当）。
    - 各キーごとに内部で `RepeatHelper` を保持し、トリガ成立／解除に応じて `Start()` / `Stop()` を呼ぶ。
    - プロファイル適用時に `Mapping.ClearKeyButtonControllersForDevice(device)` を呼んで当該デバイスのコントローラを破棄し、その際に `RepeatHelper.Dispose()` を実行してリソースを解放する。

- `RepeatHelper`（合成繰り返しユーティリティ）
  - 役割: 合成キーの繰り返し送出を行う小さな再利用可能コンポーネント。
  - 現状のセマンティクス:
    - `Start()`：初回送出／タイマー開始（内部で InitialDelay を考慮）
    - `Stop()`：繰り返しを停止し、必要に応じて一度だけ Release を送るが、インスタンス自体は破棄しない（再利用可能）。
    - `Dispose()`：完全破棄とリソース解放。主にプロファイル適用時のクリーンアップで呼ばれる。

注: 上記により、従来設計で想定されていたグローバルな `ToggleActionController` / `PressActionController` の `Update()` を常に主ループで呼ぶ方式は廃止されつつあります。各 `KeyButtonActionController` は必要に応じて内部でタイマーや `RepeatHelper` を用いるため、グローバルな周期呼び出しによる制御から脱却しています。

## データ構造（変更案）
- deviceState 内のキー管理をモード別に分離する（段階的移行を推奨）:
  - `deviceState.toggleKeyPresses: Dictionary<ushort, KeyPresses>`
  - `deviceState.pressKeyPresses: Dictionary<ushort, KeyPresses>`
- `pressedonce` の廃止 → per-action タイムスタンプに置換（例: `Dictionary<string, DateTime> actionPressedAt`）。
- `SyntheticState.KeyPress` の `lastSyntheticSendUtcTicks` / `repeatCount` 等は引き続き保持するが、どのコントローラが管理するかを明確にする。

## 動作仕様（要点）
- Mapping（トリガ判定）は、該当 SpecialAction の mode を見て以下を呼ぶ:
  - Toggle: `ToggleActionController.OnToggleOn/OnToggleOff`
  - Press: `PressActionController.OnPressDown/OnPressUp`
- Controller は送出タイミングを判断し、実際の送出は `SyntheticDispatcher` に委譲する。
- Toggle ON 状態では `ToggleActionController` が `SyntheticDispatcher.SendPress` を継続的に呼ぶ（内部で初期遅延と繰り返し間隔を管理）。
- Press モードでは `PressActionController` がトリガ押下中に SendPress を行い、必要であれば独自のリピート（InitialDelay/RepeatInterval）を行う。
- 両者とも `ClearKeyEntries` を提供し、外部から既存のエントリ／タイミングを強制リセットできる。

## 排他性と実装ガイドライン

設計上、同一キーに対して同時に複数のコントローラが継続的送信（長押し／トグル繰り返し）を行ってはならない。以下を実装ルールとして採用します。

- **排他決定は Mapping が行う**: トリガ判定時に SpecialAction の `mode`（Toggle または Press）を見て、どちらのコントローラを起動するかを決定する。Mapping は常に一方のコントローラのみを呼ぶ。
- **コントローラ間での呼び出し禁止**: `PressActionController` が `ToggleActionController` の継続送信処理を呼ぶこと、またはその逆は行わない。各コントローラは独立して `SyntheticDispatcher.SendPress/SendRelease` を呼ぶ責務を持つ。
- **外部強制リセット**: Mapping はモード切替やトリガ状態変化の際に、選択しなかった側のコントローラに対して `ClearKeyEntries(kvpKey)` を呼び、残存エントリやタイミングを必ずクリアする。
- **SyntheticDispatcher は中立的**: `SyntheticDispatcher` は単純な送出とタイミング更新を行い、どのコントローラが呼んだかを考慮しない。排他ロジックはコントローラ/Mapping 側に置く。

実装のヒント:
- Mapping の呼び出し直前で `ToggleActionController.ClearKeyEntries(kvpKey)` と `PressActionController.ClearKeyEntries(kvpKey)` の双方を呼ぶのではなく、"選択された" コントローラだけを呼ぶ前に**未選択側だけを**クリアする。これにより race 条件や二重送信を防げる。
- オプションとしてランタイム検査を入れることもできる（デバッグビルドのみ、キーごとの現在のコントローラ所有者を追跡して不整合をログ出力）。ただし常時トラッキングは複雑化するため最小限に留めること。


## 移行計画（段階的）
1. `SyntheticDispatcher` の最小実装を追加（ラッパ: SendPress/SendRelease/ResetKeyTiming）。
2. `PressActionController` を最小実装（OnPressDown→即時 SendPress、OnPressUp→SendRelease、Update no-op）として Mapping の Press パスへ差し替える。
3. `ToggleActionController` を既存コードから移行し、API を整える。
4. deviceState をモード別に分離し、`Commit()` のマージロジックを段階的に簡素化する。
5. `pressedonce` を per-action timestamp に置き換え、`ShouldClearPressedOnce` を簡潔化。
6. Press の repeat を `PressActionController.Update` に追加し、パラメータ調整を行う。
7. ButtonType のサポート追加（`SyntheticDispatcher` の API 拡張）。

## テスト項目
- Press→Toggle 切替時に二重送信が発生しないこと（TRACE ログで `synthetic-press queued` を確認）。
- Toggle ON/OFF と繰り返し動作が既存仕様に準拠すること（タイミングの目安: InitialDelay=100ms, RepeatInterval=25ms を初期値）。
- Press の長押し（有効時）が期待どおりに動作すること（初期遅延/間隔確認）。
- per-action timestamp による `pressedonce` のクリア動作が安定すること。

## 変更履歴
- 2025-12-16: 初版作成（設計案、移行計画、運用ルールを記載）。

---

### 管理メモ
- このファイルがこの機能の正式な仕様ソースです。実装または設計の議論で出た変更は、必ずここに追記して承認者（ユーザ）を記録してください。内容変更のない確認はこのファイルを参照することで完結します。

