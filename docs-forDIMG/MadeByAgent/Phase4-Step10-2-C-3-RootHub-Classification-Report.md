# フェーズ4-Step10-2-C C-3 `rootHub` 呼び出し元分類報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-C-Plan.md`

## 1. 目的

`App.rootHub`／`Program.rootHub` への直接依存を、呼び出し元の責務と呼出頻度に応じて分類する。ここでは実装方式を決め打ちせず、C-1（最小アクセサ）と C-2（ControlService 注入）の適用候補を整理する。

## 2. 分類結果

### 2.1 C-1: 最小アクセサ方式を第一候補とするもの

| 呼び出し元 | 主な使用内容 | 頻度 | 理由 |
|---|---|---:|---|
| `Mapping.cs` | `DS4Device` 取得、入力状態・TouchPad 参照 | 高 | 入力ループに `ControlService` 全体を渡すと依存が大きくなり、循環依存を強めるため |
| `DS4Sixaxis.cs` | TouchPad のジャイロトリガー状態参照 | 高 | 必要な状態取得機能が限定され、低レイヤ処理に属するため |
| `ControllerReadingsControl.xaml.cs` の状態取得部分 | 指定デバイスの状態取得 | UI更新 | 状態取得だけなら `IDeviceStateAccessor` で十分なため |
| `BindingWindow.xaml.cs` のデバイス取得部分 | 対象デバイスの取得 | UI操作 | コントローラー全体の管理機能を必要としないため |

想定する最小契約は、既存 `IDeviceStateAccessor` を拡張または用途別アクセサへ分割する方式である。ただし、`DS4Device` の直接公開、`TouchPad`、状態取得メソッドのどこまでを契約に含めるかは確認が必要である。

### 2.2 C-2: `ControlService` 注入方式を第一候補とするもの

| 呼び出し元 | 主な使用内容 | 理由 |
|---|---|---|
| `MainWindow.xaml.cs` | Start／Stop、イベント購読、OSC／UDP／Motion、OutputSlot | ControlService の複数機能を画面ライフサイクルと結合して使用するため |
| `ProfileEditor.xaml.cs` | rumble、TouchPad リセット、状態取得、入力停止 | 複数のデバイス操作があり、画面操作に応じた同期処理が必要なため |
| `RecordBox.xaml.cs`／`RecordBoxViewModel.cs` | 録画状態、デバイス／TouchPad 参照 | 画面操作と録画ライフサイクルの複数機能を使用するため |
| `StickCalibrationWindow.xaml.cs` | 状態取得とキャリブレーション関連操作 | 画面単位のデバイス操作として依存をまとめた方が明確なため |

ただし、UI へ直接 `ControlService` を注入すると依存が大きくなるため、画面用の `IControllerInteractionService` を新設する案もある。この案は C-2 の派生案であり、採用には確認が必要である。

### 2.3 判断保留

| 呼び出し元 | 保留理由 |
|---|---|
| `AutoProfileChecker.cs` | 自動切替、接続状態監視、プロファイル適用、待機ループを含み、単一のアクセサか専用サービス分割か判断が必要 |
| `PresetOption.cs` | Blank／Default プロファイル生成とデバイス操作を含む。`IProfileRepository` とデバイス操作を分離する必要がある |
| `MainWindow.xaml.cs` のプロファイル適用箇所 | `IProfileRepository`、`IProfileSwitcher`、通知、デバイス停止のどこまでを担当させるか整理が必要 |
| `App.rootHub` と `Program.rootHub` の併存箇所 | 互換代入を維持する期間と、どちらを正規参照にするか確認が必要 |

## 3. 実装前に確認したい事項

### 確認 A: C-1 の契約範囲

`Mapping`／`DS4Sixaxis` では、まず `GetController(int)` のみを最小契約とし、TouchPad や状態取得は別のアクセサへ分ける方針でよいか。

### 確認 B: UI の依存方式

`MainWindow`、`ProfileEditor`、`RecordBox` は `ControlService` 具象型を直接注入するか、画面用の `IControllerInteractionService` を新設するか。

### 確認 C: AutoProfile

`AutoProfileChecker` は `IProfileSwitcher`、`IProfileRepository`、`IDeviceStateAccessor` 等へ分割して注入するか、まず `ControlService` 注入で動作経路を固定するか。

### 確認 D: 移行中の互換代入

`App.rootHub`／`Program.rootHub` は DI 解決インスタンスとの同一性を検証しながら、CP4 完了まで互換代入として残す方針でよいか。

## 4. 今回の結論

現段階では分類まで完了し、C-1／C-2 の具体的なコード変更は行っていない。上記 A〜D の方針確定後、承認された分類だけを小さな単位で実装する。
