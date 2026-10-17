# フェーズ4-Step10-2-A-1 完了報告書: シム接続拡張（スティック関連）

作成日: 2026-09-01
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク1）

---

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク1「スティック関連」を実施した。対象は`m_Config`（`BackingStore`）へ委譲していた13メンバー。

| Global メンバー | 型 | 備考 |
|---|---|---|
| `LSModInfo` / `RSModInfo` | `StickDeadZoneInfo[]` | デッドゾーン設定 |
| `LSRotation` / `RSRotation` | `double[]` | スティック回転補正 |
| `LSSens` / `RSSens` | `double[]` | 感度 |
| `SquStickInfo` | `SquareStickInfo[]` | スクエアスティック設定 |
| `LSAntiSnapbackInfo` / `RSAntiSnapbackInfo` | `StickAntiSnapbackInfo[]` | アンチスナップバック |
| `LSOutputSettings` / `RSOutputSettings` | `StickOutputSetting[]` | 出力モード（Flick/Standard等） |
| `lsOutBezierCurveObj` / `rsOutBezierCurveObj` | `BezierCurve[]` | 出力カーブ |
| `getLsOutCurveMode`/`setLsOutCurveMode`/`getRsOutCurveMode`/`setRsOutCurveMode` | メソッド | カーブモード取得・設定 |

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: 上記13メンバー相当のプロパティ・メソッド宣言を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - `BackingStore _config`フィールドを追加し、コンストラクタで`Global.store`（既存の単一`BackingStore`インスタンス）を参照する形にした（データの二重管理なし）。
  - 13メンバー分の委譲実装を追加。`SetLsOutCurveMode`/`SetRsOutCurveMode`には`[DI]`Traceログを追加（ユーザー操作起点のため）。
  - 読み取り専用の配列プロパティ（`LSModInfo`等）にはログを追加していない（Mapping.cs等のポーリングループから高頻度アクセスされる可能性があるため。§4リスクと回避策 参照）。
- `DS4Windows/DS4Control/ScpUtil.cs`: 対象13メンバーを1行委譲（`ProfileSettingsServiceInstance.X`）に置換。カーブモードのsetterには`[Legacy]`Traceログを追加（`IsTraceEnabled`ガード付き）。

## 3. 外部呼び出し元への影響

`Global.LSModInfo`等のAPI形状（型・シグネチャ）は変更していないため、以下の外部呼び出し元は**無修正で動作する**ことを確認した（grep調査、コード変更なし）。

- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`（約110箇所）
- `DS4Windows/DS4Forms/ProfileEditor.xaml.cs`（4箇所）
- `DS4Windows/DS4Control/ControlService.cs`（2箇所）
- `DS4Windows/DS4Control/Mapping.cs`（2箇所）

これらのファイルは本サブタスクでは変更していない。呼び出し元のDI直接参照化（Stage2）は、Stage1の全サブタスク完了後に別途着手する。

## 4. 検証状況

- 静的検証: ブレース平衡チェック、型整合性、DI登録（`ServiceRegistration.cs`の`AddSingleton<IProfileSettingsService, ProfileSettingsService>()`）との互換性、既存テスト（`ProfileSettingsServiceTests.cs`, `ProfileRepositoryTests.cs`の`new ProfileSettingsService()`呼び出し）への非破壊性を確認済み。
- **ビルド確認・自動テスト実行は未実施**（本作業環境にWindows/.NET 8 WPFビルド環境がないため）。同梱スクリプト実行後、`dotnet build DS4WindowsWPF.sln --nologo`および`DS4WindowsTests`/`StandaloneTests`の実行をお願いします。
- 実機検証は、Stage1（Step10-2-A、9サブタスク）が全て完了した時点で`Phase4-Step10-2-A-RealDevice-Verification-Checklist.md`としてまとめて実施予定（計画書§2.4）。

## 5. 残作業（Stage1 サブタスク2〜9）

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | **完了（本報告書）** |
| Step10-2-A-2 | トリガー(L2/R2)関連 | 未着手 |
| Step10-2-A-3 | タッチパッド関連 | 未着手 |
| Step10-2-A-4 | ジャイロ関連 | 未着手 |
| Step10-2-A-5 | ライトバー・ランブル関連 | 未着手 |
| Step10-2-A-6 | ボタン/マウス出力関連 | 未着手 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 未着手 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 未着手 |
| Step10-2-A-9 | Mapping.cs専用 | 未着手 |
