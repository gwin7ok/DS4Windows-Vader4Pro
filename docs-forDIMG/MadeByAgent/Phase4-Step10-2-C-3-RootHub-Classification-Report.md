# フェーズ4-Step10-2-C C-3 `rootHub` 呼び出し元分類報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`

## 1. 目的

`App.rootHub`／`Program.rootHub` への直接依存を、呼び出し元の責務と呼出頻度に応じて分類する。C-1（最小アクセサ）と C-2（ControlService 注入）の大分類は決定済みであり、本書ではその根拠と実装時の確認事項を管理する。

## 2. 分類結果

### 2.1 C-1: 最小アクセサ方式を第一候補とするもの

| 呼び出し元 | 主な使用内容 | 頻度 | 理由 |
|---|---|---:|---|
| `Mapping.cs` | `DS4Device` 取得、入力状態・TouchPad 参照 | 高 | 入力ループに `ControlService` 全体を渡すと依存が大きくなり、循環依存を強めるため |
| `DS4Sixaxis.cs` | TouchPad のジャイロトリガー状態参照 | 高 | 必要な状態取得機能が限定され、低レイヤ処理に属するため |
| `ControllerReadingsControl.xaml.cs` の状態取得部分 | 指定デバイスの状態取得 | UI更新 | 状態取得だけなら `IDeviceStateAccessor` で十分なため |
| `BindingWindow.xaml.cs` のデバイス取得部分 | 対象デバイスの取得 | UI操作 | コントローラー全体の管理機能を必要としないため |

最小契約は、既存 `IDeviceStateAccessor` の `GetController(int)` を基本とする。TouchPad や状態取得メソッドが必要な場合は、必要性を確認したうえで別の小さなアクセサへ分ける。

### 2.2 C-2: `ControlService` 注入方式を第一候補とするもの

| 呼び出し元 | 主な使用内容 | 理由 |
|---|---|---|
| `MainWindow.xaml.cs` | Start／Stop、イベント購読、OSC／UDP／Motion、OutputSlot | ControlService の複数機能を画面ライフサイクルと結合して使用するため |
| `ProfileEditor.xaml.cs` | rumble、TouchPad リセット、状態取得、入力停止 | 複数のデバイス操作があり、画面操作に応じた同期処理が必要なため |
| `RecordBox.xaml.cs`／`RecordBoxViewModel.cs` | 録画状態、デバイス／TouchPad 参照 | 画面操作と録画ライフサイクルの複数機能を使用するため |
| `StickCalibrationWindow.xaml.cs` | 状態取得とキャリブレーション関連操作 | 画面単位のデバイス操作として依存をまとめた方が明確なため |

UI へ直接 `ControlService` を注入すると依存が大きくなるため、実装時には `IControllerInteractionService` を新設する派生案も比較する。ただし、分類としては C-2 を採用する。

### 2.3 今回確定した4項目

| 呼び出し元 | 決定 | 適用方針 |
|---|---|---|
| `AutoProfileChecker.cs` | **C-1** | `IDeviceStateAccessor`、`IProfileSwitcher`、`IProfileRepository` 等へ責務別に分割する。接続監視・判定・切替実行を `ControlService` 全体へまとめて依存させない |
| `PresetOption.cs` | **C-2 から開始** | `ControlService` 注入で既存のプリセット適用・デバイス反映を維持する。安定後に専用サービス化を再評価する |
| `MainWindow.xaml.cs` のプロファイル適用箇所 | **C-1** | `IProfileSwitcher`、`IProfileRepository`、通知等へ責務分割し、UI に適用処理の詳細を持たせない |
| `App.rootHub` と `Program.rootHub` の併存箇所 | **C-1/C-2 の分類対象外** | 呼び出し元ごとの C-1／C-2 を適用する。互換代入自体は CP4 完了まで維持し、DI 解決インスタンスとの同一性を検証する |

## 3. 実装時に確認する事項

分類方針は確定しているため、以下は方式の再選択ではなく、具体的な契約範囲と副作用を確認するための事項である。

### 確認 A: C-1 の契約範囲

`Mapping`／`DS4Sixaxis` では、まず `GetController(int)` を最小契約とし、TouchPad や状態取得は必要に応じて別アクセサへ分ける。

### 確認 B: UI の依存方式

`MainWindow`、`ProfileEditor`、`RecordBox` は C-2 を適用する。各画面で `ControlService` 具象型を直接注入するか、画面用の `IControllerInteractionService` を新設するかは、使用メンバー確認後に決定する。

### 確認 C: AutoProfile

`AutoProfileChecker` は C-1 を適用し、`IProfileSwitcher`、`IProfileRepository`、`IDeviceStateAccessor` 等の責務分担を実装時に確定する。

### 確認 D: 移行中の互換代入

`App.rootHub`／`Program.rootHub` は分類対象外として、DI 解決インスタンスとの同一性を検証しながら CP4 完了まで互換代入を維持する。

## 4. 今回の結論

4項目の C-1／C-2 分類と、`rootHub` 互換代入を CP4 まで維持する方針は確定した。今後は、実装単位ごとに契約範囲、副作用、UI の注入形態を確認しながら、小さな単位で実装する。
