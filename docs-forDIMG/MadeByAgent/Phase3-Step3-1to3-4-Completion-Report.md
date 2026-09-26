# フェーズ3 Step 3-1〜3-4 完了報告書

作成日: 2026-08-30
対象ブランチ: For-DI-migration-work
前提ドキュメント: docs-forDIMG/MadeByAgent/Phase3-Plan.md

## 0. 本報告書の位置づけ

本作業は、外部エージェント経由で提示されたPowerShellスクリプト（保存先フォルダが生成されない不具合をgwin7ok氏が修正の上、実行）により、Phase3-Plan.mdのStep 3-1〜3-4を一括で実施したものである。
Phase3-Plan.md §5「エージェントへの行動指示」およびcopilot-instructions.md §5.1は「1ステップ完了ごとに確認を挟む」ことを原則としているが、今回は4ステップがまとめて1つのスクリプトとして提示・実行された。これは原則からの逸脱であるため、本報告書は4ステップ分をまとめて記録しつつ、各ステップの達成状況を個別に検証する形で記述する。

## 1. 実施内容の対応付け（コマンド内容 → Phase3-Planのステップ）

| 実施内容 | 対応するステップ |
|---|---|
| IDs4DeviceRegistry.cs 新規作成 | Step 3-1 |
| Ds4DeviceRegistryAdapter.cs 新規作成 | Step 3-2 |
| ServiceRegistration.cs へのDI登録追加 | Step 3-3 |
| IDeviceStateAccessor.cs 新規作成 + ControlService.cs 実装追加 + Mapping.cs ピンポイント置換 | Step 3-4 |

Step 3-5（権限昇格の抽象化）・Step 3-6（多重起動チェックの抽象化）は本作業のスコープ外であり、未着手。

## 2. Step 3-1: IDs4DeviceRegistry インターフェース設計

### 2.1 作成物
- パス: DS4Windows/DS4Control/Services/IDs4DeviceRegistry.cs
- 名前空間: DS4Windows.Services

### 2.2 Phase3-Planとの差異（重要）
- **名前空間の変更**: Phase3-Plan.md §3 Step 3-1のコード例では `namespace DS4Windows.Actions` を想定していたが、実際には `DS4Windows.Services`（DS4Control/Services 配下）を採用した。これはPhase2の実際の配置規約（Step 2-2のOutputKBMHandlerAdapter等と同様の置き場所）に合わせた意図的な変更であり、Phase3-Plan.md自体がStep 3-2で「Step 2-2のOutputKBMHandlerAdapterと同じ設計思想」を踏襲するよう指示していることとも整合する。→ Phase3-Plan.md側の記載更新が今後必要（本報告書§7参照）。
- **メンバー追加**: `ReEnableDevice(string deviceInstanceId)` をインターフェースに追加した。Phase3-Plan.md本文のコード例には含まれていなかったメンバーである。DS4Devices.reEnableDevice(instanceId) はコマンドライン `-re-enabledevice` 経由の再有効化フローで使用される既存の静的メソッドであり、Step 3-5（権限昇格の抽象化、IElevatedProcessLauncher）と機能的に隣接する。今回追加したことで、Step 3-5着手時に本メンバーをどちらのインターフェースが担うか（IDs4DeviceRegistry vs IElevatedProcessLauncher）の重複整理が必要になる可能性がある。

### 2.3 未実施の確認事項
Phase3-Plan.md §3 Step 3-1は「grep -n "DS4Devices\." でControlService.cs等の全参照を再抽出し、インターフェース案に漏れがないか確認する」ことを着手前提としていたが、本作業ではこの再抽出は行われていない。IDs4DeviceRegistryが DS4Devices の全public staticメンバーを過不足なく反映しているかは未検証。

## 3. Step 3-2: Ds4DeviceRegistryAdapter 実装

### 3.1 作成物
- パス: DS4Windows/DS4Control/Services/Ds4DeviceRegistryAdapter.cs
- IDs4DeviceRegistryの全メンバーへの委譲実装（状態を持たないアダプター、Step 2-2 OutputKBMHandlerAdapterと同一の設計思想）

### 3.2 確認事項
実装はIDs4DeviceRegistryの全メンバー（ReEnableDevice含む）を静的DS4Devicesクラスへ委譲しており、設計上の矛盾はない。ビルド確認（コンパイル成功）は本報告書の時点では未実施。

## 4. Step 3-3: DI登録

### 4.1 変更内容
- ServiceRegistration.cs に `services.AddSingleton<IDs4DeviceRegistry, Ds4DeviceRegistryAdapter>();` を1行追加。
- 既存の `return services;` の直前に挿入する形で実装。

### 4.2 スコープの確認
Phase3-Plan.md §3 Step 3-3の想定通り、ControlServiceのコンストラクタ引数変更（IDs4DeviceRegistry注入）は行っていない。DIコンテナへの登録のみにとどめる、という方針が正しく守られている。

## 5. Step 3-4: IDeviceStateAccessorによる依存解消（範囲限定）

### 5.1 作成物・変更内容
- IDeviceStateAccessor.cs 新規作成（DS4Windows.Services名前空間、GetController(int)のみを持つ最小インターフェース）
- ControlService.cs: クラス宣言に IDeviceStateAccessor を追加、GetController(int deviceIndex) メソッドを新設（DS4Controllers配列への境界チェック付きアクセス）
- Mapping.cs: Program.rootHub.DS4Controllers[device] の直接参照（6504行目相当）を、DI解決優先＋失敗時フォールバックの形に置換

### 5.2 Phase3-Plan.mdとの整合性確認
Phase3-Plan.md §1.2および§3 Step 3-4が定めた「対応するのはMapping.cs 6504行目相当（既存の読み取り専用参照）のみ」という範囲限定方針が正しく守られている。ApplyProfileDirect/RestoreProfileDirect内のctrl（ControlServiceそのもの）への依存2箇所は、意図通り本作業のスコープ外として変更されていない。

### 5.3 未実施の確認事項（要フォローアップ）
Phase3-Plan.md §5の完了判定基準は「ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub依存2箇所が、フェーズ4への既知の残課題として明示的に文書化されていること」を求めているが、本作業ではこの文書化は行われていない。本報告書の§7・および姉妹文書Phase3-Status.mdで改めて明示する。

## 6. ビルド・実機動作確認の状況

本作業はコード生成（ファイル作成・ピンポイント置換）のみであり、以下はいずれも未実施（要ユーザー確認）:
- ビルド成功の確認（コンパイルエラーの有無）
- 実機でのデバイス接続/切断シナリオの回帰テスト
- Mapping.cs側のフォールバック経路（DI解決失敗時にProgram.rootHubへ戻る分岐）が実際に機能するかの動作確認

## 7. Phase3-Plan.mdに対して更新が必要な事項（申し送り）

1. Step 3-1のコード例のnamespaceを DS4Windows.Actions → DS4Windows.Services に修正する。
2. IDs4DeviceRegistryにReEnableDeviceが追加されたことを明記し、Step 3-5着手時に権限昇格系メンバーとの重複整理が必要である旨を追記する。
3. ApplyProfileDirect/RestoreProfileDirectのProgram.rootHub依存2箇所が、Step 3-4完了時点でもまだ文書化未実施であることをPhase3-Status.mdに残課題として明記する。

## 8. 完了判定チェックリスト（Phase3-Plan.md §5 に対する現時点の充足状況）

| 項目 | 状況 |
|---|---|
| IDs4DeviceRegistryがDS4Devicesの全public staticメンバーを過不足なく反映 | 未検証（grepによる再抽出が未実施） |
| Ds4DeviceRegistryAdapterがコンパイル成功しDIコンテナにSingleton登録 | DI登録は完了、ビルド確認は未実施 |
| Mapping.cs 6504行目相当がIDeviceStateAccessor経由に置換、フォールバック保持 | 完了 |
| ApplyProfileDirect/RestoreProfileDirectの2箇所がフェーズ4への既知の残課題として文書化 | 未実施（本報告書とPhase3-Status.mdで今回着手） |
| IElevatedProcessLauncher/IProcessInspector新設 | 未着手（Step 3-5/3-6） |
| 実機での接続/切断・権限昇格シナリオの動作確認記録 | 未実施 |
| 各ステップの記録がMadeByAgentに保存 | 本報告書で対応 |
| Phase3-Status.mdの新設 | 本報告書と同時に作成 |

## 9. 次のアクション

1. ビルド確認（Step 3-1〜3-4分のコンパイルエラー有無）
2. Step 3-4残課題（ApplyProfileDirect/RestoreProfileDirectのctrl依存2箇所）をPhase3-Status.mdに反映
3. Step 3-5（IElevatedProcessLauncher、権限昇格の抽象化）に着手するかどうかの確認
4. Phase3-Plan.md §3 Step 3-1のnamespace記載を実態（DS4Windows.Services）に合わせて修正