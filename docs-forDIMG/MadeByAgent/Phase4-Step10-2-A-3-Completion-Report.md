# フェーズ4-Step10-2-A-3 完了報告書: シム接続拡張（タッチパッド関連）

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク3）

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク3「タッチパッド関連」を実施した。対象は`m_Config`（`BackingStore`）へ委譲する12プロパティ。

| Global メンバー | 型 | 備考 |
|---|---|---|
| `TouchSensitivity` | `byte[]` | タッチ感度 |
| `TapSensitivity` | `byte[]` | タップ感度 |
| `TouchpadInvert` | `int[]` | タッチパッド反転設定 |
| `TouchpadJitterCompensation` | `bool[]` | ジッター補正 |
| `TouchClickPassthru` | `bool[]` | タッチクリックパススルー |
| `TouchpadButtonMode` | `TouchButtonActivationMode[]` | タッチボタン動作モード |
| `StartTouchpadOff` | `bool[]` | 起動時タッチパッド無効化 |
| `TouchOutMode` | `TouchpadOutMode[]` | タッチパッド出力モード |
| `TouchDisInvertTriggers` | `int[][]` | 反転トリガー設定 |
| `TouchMouseStickInf` | `TouchMouseStickInfo[]` | タッチマウススティック設定 |
| `TouchAbsMouse` | `TouchpadAbsMouseSettings[]` | 絶対座標マウス設定 |
| `TouchRelMouse` | `TouchpadRelMouseSettings[]` | 相対座標マウス設定 |

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: 上記12プロパティの宣言を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - A-1/A-2で追加した`BackingStore _config`を継続利用し、`Global.store`（既存の単一`BackingStore`インスタンス）を参照する形で実装した（データの二重管理なし）。
  - タッチパッド関連の12プロパティを`_config`の該当配列へ委譲。
  - `TouchButtonActivationMode` は既存の`Mouse`型に定義されたenumを参照する形で追加した。
- `DS4Windows/DS4Control/ScpUtil.cs`:
  - A-3対象メンバーを`ProfileSettingsServiceInstance`経由の後方互換シムへ変更。
  - `TouchOutMode` は既存のフィールド参照からプロパティ参照へ変更し、既存の配列アクセス互換性を維持。
  - 既存のタッチパッド設定配列、XML読込・保存処理、初期値処理は変更していない。

## 3. 外部呼び出し元への影響

`Global.TouchSensitivity`等のAPI形状（型・配列アクセス形態）は維持しているため、既存の外部呼び出し元は互換性を維持している。

- `DS4Windows/DS4Forms/ViewModels/ProfileSettingsViewModel.cs`
- `DS4Windows/DS4Control/ScpUtil.cs` 内のプロファイル読込・保存処理
- `DS4Windows/DS4Control/Mapping.cs` 等の既存タッチパッド処理

これらの呼び出し元は本サブタスクではDI直接参照化していない。Stage2（呼び出し元のDI直接参照化）は、Stage1の全サブタスク完了後に別途着手する。

## 4. 検証状況

- デバッグビルド: 成功（警告0、エラー0）。
- テストビルド: 成功（Actions、Standalone）。
- テスト実行: 成功。Actionsテスト85件、StandaloneTests 13件が2回連続で全件成功。
- A-3単独の実機検証は実施していない。計画どおり、Stage1（Step10-2-A、9サブタスク）完了後にまとめて実施する。

## 5. 残作業（Stage1 サブタスク4〜9）

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | 完了 |
| Step10-2-A-2 | トリガー(L2/R2)関連 | 完了 |
| Step10-2-A-3 | タッチパッド関連 | **完了（本報告書）** |
| Step10-2-A-4 | ジャイロ関連 | 未着手 |
| Step10-2-A-5 | ライトバー・ランブル関連 | 未着手 |
| Step10-2-A-6 | ボタン/マウス出力関連 | 未着手 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 未着手 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 未着手 |
| Step10-2-A-9 | Mapping.cs専用 | 未着手 |
