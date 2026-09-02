# フェーズ5計画書: DIサービス内部 Legacy 経路監査と責務分離

作成日: 2026-09-02
最終更新日: 2026-09-02（Step1監査結果反映）
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase4-Plan.md`
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
Step1監査レポート: `docs-forDIMG/MadeByAgent/Phase5-Step1-legacy-delegation-audit-report.md`

## 1. 位置づけ

Phase4 では DI 基盤、Composition Root、主要呼び出し元、ViewModel Factory を整備した。Phase5 では、DI サービスの内部実装が `Global`／`Program.rootHub`／Legacy 実体へ再委譲している経路を監査し、影響範囲と優先度を確定したうえで責務分離を進める。

DI インターフェース経由で呼ばれているだけでは、内部実体の DI 化完了とは判定しない。

## 2. Phase4 からの引継ぎ

- `ProfileRepository` 内部の `Global.LoadProfile`／`Global.SaveProfile`
- `ProfileApplicationService` 内部の `Global.ApplyProfile`／`Global.LoadProfile`／`Global.LoadTempProfile`
- `SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions`
- `PathService` 内部の `Global.appdatapath`
- `ProfileSettingsService` 内部の `Program.rootHub` 参照
- Save／Apply の操作ログ、保存成否、再適用成否、通知経路の統一
- CP4 前の自動テスト化、CP4 実機検証、Legacy shim 削除判断

監査の基準結果は `Phase4-Step10-2-C-5-3-Nested-Legacy-Audit-Report.md` に記録する。

### Step1監査による更新（2026-09-02）

Phase5-Step1で DI 登録済み全23サービス（`ServiceRegistration.cs` 登録分、ViewModel群を除く）を実コードベースで再監査した結果、上記の引継ぎ事項に加えて以下が新たに判明した。詳細は `Phase5-Step1-legacy-delegation-audit-report.md` を参照。

- `IProfileActionProvider`／`IProfileActionChainService` が `Global.getProfileActions`／`Global.GetProfileAction`／静的 `Mapping.DispatchProfileActionEdge` へ委譲している（プロファイルアクション解決・連鎖処理）。
- `IManagedActionManager`（`DefaultActionManager`）が、静的 `ActionManager`／`ActionFactory`／`ActionRegistry` という3つの静的クラス群へ委譲している（Actions基盤）。
- `IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）が、静的 `DS4Devices` クラスへ全操作を委譲している（デバイス検出・列挙）。
- `IProfileSwitcher`（`DefaultProfileSwitcher`）が `Global.ApplyProfile`／`Program.rootHub` を直接呼び出しており、`ProfileApplicationService` と機能領域が重複している。
- Step6で想定していた「KBMアダプター」に該当する DI 登録サービスが現行の `ServiceRegistration.cs` からは確認できず、`OutputKBMHandlerAdapter.cs` がDI未登録のまま使用されている可能性がある（Step6実施時に要確認）。
- `INotificationService`／`IEnvironmentService` はLegacy委譲は無いが、実際の購読先・永続化経路が本調査範囲では未確認（Legacy委譲とは別種の課題として記録）。

これを受けて、Step3・Step6の対象範囲を明確化するとともに、Step7〜9を新設し、従来のStep7（自動テストと実機検証）・Step8（Legacy shim削除判断）をそれぞれStep10・Step11へ繰り下げる。

## 3. 実施ステップ

### Phase5-Step1: 詳細監査と優先度付け【完了】

1. DI 登録済み全サービスの `Global`／`rootHub`／静的実体参照を棚卸しする。
2. 各参照を、設定データ、XML永続化、プロファイル適用、副作用、通知、パス、デバイス状態、KBMに分類する。
3. 既存動作、ログ、スレッド、イベント、失敗時挙動を固定する。
4. 移行単位、リスク、テスト方法、実機確認要否を決定する。

成果物: `Phase5-Step1-legacy-delegation-audit-report.md`（DI登録済み全23サービスの判定結果、Step2〜6との対応関係、新規発見4件、Step再構成案を記録）。

### Phase5-Step2: プロファイル XML 読込・保存

`ProfileRepository` が `Global.LoadProfile`／`Global.SaveProfile` に再委譲する構造を対象とする。XMLパース、設定値反映、保存を専用契約へ分離する。入力停止、出力デバイス操作、Action再構築は適用サービスへ混在させない。

ロード順、既定値、欠落設定、カルチャ、ファイルパス、例外、ログを維持する。

### Phase5-Step3: プロファイル適用・復帰

`ProfileApplicationService` を実体の所有者とし、通常／一時プロファイルのロード、デバイス停止・再開、状態更新、通知、Action再構築を責務別に整理する。

通常 GUI 切替、編集画面 Save／Apply、SpecialAction、AutoProfile のすべてが同じ適用契約を使用することを目標とする。二重ロード、`isTemp`、復帰スナップショット、通知回数を回帰検証する。

**Step1監査で追加確定した対象**: `IProfileSwitcher`（`DefaultProfileSwitcher`）が SpecialAction 経由のプロファイル切替・復帰時に独自に `Global.ApplyProfile`／`Program.rootHub` を直接呼び出している。これは本Stepが目標とする「SpecialAction経由の適用契約統一」と同一領域であるため、独立Stepとはせず本Stepのスコープに含めて統一する。`ProfileApplicationService` と `DefaultProfileSwitcher` の間で重複しているプロファイル切替ロジックの統合方針を、実装着手時に検討する。

### Phase5-Step4: Save／Apply の操作結果と通知

保存成否と再適用成否を戻り値で扱い、操作単位の `[DI]` ログを追加する。再適用対象の判定を明示し、`ProfileChanged` 通知が通常切替と同じ経路で発生することを確認する。

通知設定による抑制と、ログ出力を分けて扱う。

### Phase5-Step5: SpecialAction 永続化

`SpecialActionRepository` 内部の `Global.LoadActions`／`Global.SaveActions` を分離する。Actions XML の CRUD、一覧更新、runtime ActionManager の再構築を適切な境界に整理する。

### Phase5-Step6: 残存サービス境界

`PathService`、`ProfileSettingsService`、デバイス状態、KBMアダプターを Phase2／Phase3 の既存方針と重複しないよう整理する。共有 `BackingStore` を当面維持する場合は、その理由と終了条件を記録する。

**Step1監査での確認事項**: 「デバイス状態」（`IDeviceStateService`）はLegacy委譲なしと確認済み（対象外）。「KBMアダプター」に該当するDI登録サービスが現行 `ServiceRegistration.cs` に見当たらず、`OutputKBMHandlerAdapter.cs` のDI登録状況を本Step着手時に確認する。

### Phase5-Step7（新規）: プロファイルアクション解決・連鎖処理の責務分離

`IProfileActionProvider`（`Global.getProfileActions`／`Global.GetProfileAction`）と `IProfileActionChainService`（静的 `Mapping.DispatchProfileActionEdge`）を対象とする。両者は依存関係が密（ChainServiceがProviderを利用）であるため、1つの責務分離単位として扱う。

プロファイルに紐づくSpecialAction名の解決処理、SpecialAction発火後の連鎖アクション（同時トリガー・カスケード）処理の既存動作、ログ、失敗時挙動を維持したまま、専用契約へ分離する。

### Phase5-Step8（新規）: Actions基盤（ActionManager）の静的委譲分離

`IManagedActionManager`（`DefaultActionManager`）を対象とする。トグル状態管理（静的 `ActionManager.SetToggledOn`／`FireToggledOnChanged`）、アクション生成（静的 `ActionFactory.CreateFrom`）、アクション一覧管理（`ActionRegistry`）という3系統の静的クラス依存を分離する。

DI登録された `IActionFactory`→`DefaultActionFactory` と、静的 `ActionFactory` クラスが同名で別物として共存している点に注意し、混同を避けるため命名整理を含めて検討する。トグル状態のスレッド安全性、`FireToggledOnChanged` イベント発火順序、既存ログを維持する。

### Phase5-Step9（新規）: デバイス検出・列挙（Ds4DeviceRegistry）の静的委譲分離

`IDs4DeviceRegistry`（`Ds4DeviceRegistryAdapter`）を対象とする。デバイス検出・取得・削除・シリアル更新等、全操作が委譲している静的 `DS4Devices` クラスとの境界を整理する。

HID関連の実機依存が強い領域のため、自動テストで代替できない範囲は実機確認へ残す。既存の検出タイミング、イベント発火順序、エラー処理を維持する。

### Phase5-Step10（旧Step7）: 自動テストと実機検証

各ステップ完了時に Debug ビルド、Actions／Standalone テストを実行する。自動テストで代替できない HID、WPF、ドライバ、長時間安定性は実機確認へ残す。

### Phase5-Step11（旧Step8）: Legacy shim の削除判断

全呼び出し元、内部再委譲、実機経路、フォールバック使用実績を確認した後、shim ごとに削除可否を判断する。削除は別変更として実施する。

## 4. 完了条件

- DI サービス内部の Legacy 再委譲が分類・記録されている。
- プロファイル読込・保存・適用・復帰の担当境界が明確である（`IProfileSwitcher`との重複解消を含む）。
- 通常 GUI 切替と編集画面 Save／Apply が同じ適用契約を使用する。
- Save／Apply の保存結果、再適用結果、ログ、通知が検証可能である。
- SpecialAction永続化、プロファイルアクション解決・連鎖処理、Actions基盤、デバイス検出・列挙の各境界が整理されている。
- 残存サービス境界（PathService／ProfileSettingsService／KBMアダプターのDI登録状況）が整理されている。
- 自動テストと必要な実機検証が完了している。
- shim の削除可否が根拠付きで判断されている。

## 5. 進行ルール

一度に複数の責務を移行しない。各ステップで既存機能、ログ、状態遷移を確認し、Legacy shim は新経路の動作確認前に削除しない。

新規Step（Step7〜9）は、Step1監査で発見された独立したLegacy委譲経路に対応するものであり、着手順はStep番号の昇順を基本とするが、依存関係（例: Step7はStep2〜3の完了後の方が影響範囲を把握しやすい）を踏まえて前後してよい。着手順を変更する場合はその理由を各Stepの計画書に記録する。