# フェーズ4-Step10-2-A-2 完了報告書: シム接続拡張（トリガー(L2/R2)関連）

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク2）

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク2「トリガー(L2/R2)関連」を実施した。対象は`m_Config`（`BackingStore`）へ委譲する10プロパティおよび4メソッド。

| Global メンバー | 型 | 備考 |
|---|---|---|
| `L2ModInfo` / `R2ModInfo` | `TriggerDeadZoneZInfo[]` | L2/R2デッドゾーン設定 |
| `L2Sens` / `R2Sens` | `double[]` | L2/R2感度 |
| `L2OutputSettings` / `R2OutputSettings` | `TriggerOutputSettings[]` | 出力モード・トリガー設定 |
| `l2OutBezierCurveObj` / `r2OutBezierCurveObj` | `BezierCurve[]` | L2/R2出力カーブ |
| `OutputVirtualTriggerButton` | `bool[]` | 仮想トリガーボタン出力設定 |
| `OutputDS4TriggerMode` | `DS4TriggerOutputMode[]` | DS4トリガー出力モード |
| `getL2OutCurveMode` / `setL2OutCurveMode` | メソッド | L2出力カーブモード取得・設定 |
| `getR2OutCurveMode` / `setR2OutCurveMode` | メソッド | R2出力カーブモード取得・設定 |

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: 上記10プロパティおよび4メソッドの宣言を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - A-1で追加した`BackingStore _config`を継続利用し、`Global.store`（既存の単一`BackingStore`インスタンス）を参照する形で実装した（データの二重管理なし）。
  - L2/R2関連の10プロパティを`_config`の該当配列へ委譲。
  - L2/R2出力カーブモードの取得・設定を`_config`へ委譲。
  - `SetL2OutCurveMode`/`SetR2OutCurveMode`に`[DI]` Traceログを追加（ユーザー操作起点のため）。
- `DS4Windows/DS4Control/ScpUtil.cs`:
  - A-2対象メンバーを`ProfileSettingsServiceInstance`経由の後方互換シムへ統合。
  - L2/R2出力カーブモードのsetterに`[Legacy]` Traceログを追加（`IsTraceEnabled`ガード付き）。
  - 既存のL2/R2設定配列および出力カーブの永続化・実体定義は変更していない。

## 3. 外部呼び出し元への影響

`Global.L2ModInfo`等のAPI形状（型・シグネチャ）は変更していないため、既存の外部呼び出し元は互換性を維持している。

- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`
- `DS4Windows/DS4Control/ControlService.cs`
- `DS4Windows/DS4Control/Mapping.cs`

これらの呼び出し元は本サブタスクではDI直接参照化していない。Stage2（呼び出し元のDI直接参照化）は、Stage1の全サブタスク完了後に別途着手する。

## 4. 検証状況

- デバッグビルド: 成功（警告0、エラー0）。
- テストビルド: 成功。
- テスト実行: 成功（Actionsテスト85件を含む全件成功）。
- A-2専用テスト: 配列のBackingStore共有およびL2/R2出力カーブモード委譲を検証し、8件すべて成功。
- 実機検証は、Stage1（Step10-2-A、9サブタスク）が全て完了した時点でまとめて実施する。A-2単独の実機確認は本サブタスクの完了条件として実施していない。

## 5. 残作業（Stage1 サブタスク3〜9）

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | **完了** |
| Step10-2-A-2 | トリガー(L2/R2)関連 | **完了（本報告書）** |
| Step10-2-A-3 | タッチパッド関連 | 未着手 |
| Step10-2-A-4 | ジャイロ関連 | 未着手 |
| Step10-2-A-5 | ライトバー・ランブル関連 | 未着手 |
| Step10-2-A-6 | ボタン/マウス出力関連 | 未着手 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 未着手 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 未着手 |
| Step10-2-A-9 | Mapping.cs専用 | 未着手 |
