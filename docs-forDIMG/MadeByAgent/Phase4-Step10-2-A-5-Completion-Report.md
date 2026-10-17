# フェーズ4-Step10-2-A-5 完了報告書: シム接続拡張（ライトバー・ランブル関連）

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク5）

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク5「ライトバー・ランブル関連」を実施した。対象は`m_Config`（`BackingStore`）へ委譲するライトバー・ランブル設定と、接続中デバイスへ反映する更新処理である。

対象メンバー:

- `LightbarSettingsInfo`
- `MainColor`、`LowColor`、`ChargingColor`、`CustomColor`、`FlashColor`、`UseCustomLed`相当の取得API
- `RumbleBoost`
- `InverseRumbleMotors`
- `RumbleAutostopTime`
- `DualSenseRumbleEmulationMode`
- `UseGenericRumbleStrRescaleForDualSenses`
- `DualSenseHapticPowerLevel`
- ランブル強度・オートストップ・ライトバー設定の取得／更新API

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: ライトバー・ランブル設定、`ref`色取得、取得／更新メソッドの契約を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - A-1〜A-4で使用している`BackingStore _config`を継続利用。
  - ライトバー・ランブル設定を既存の配列・設定オブジェクトへ委譲。
  - 色設定の`ref`戻り値を維持。
  - ランブルオートストップ更新時の接続中デバイスへの反映を維持。
  - DualSenseのランブル設定を既存のgetter/setter形状のまま委譲。
- `DS4Windows/DS4Control/ScpUtil.cs`:
  - 対象のGlobal公開メンバーを`ProfileSettingsServiceInstance`経由の後方互換シムへ変更。
  - 既存の配列API、`ref`色取得API、デバイス反映処理、XML読込・保存処理は維持。

## 3. 外部呼び出し元への影響

`Global`の既存API形状とライトバー・ランブルの動作を維持しているため、既存呼び出し元との互換性を維持している。Stage1では呼び出し元のDI直接参照化は行わず、Stage2で別途実施する。

## 4. 検証状況

- デバッグビルド: 成功（警告0、エラー0）。
- テストビルド: 成功（Actions、Standalone）。
- テスト実行: 成功（Actions、Standalone、全件成功）。
- 変更コミット: 完了。作業ツリーはクリーン。
- A-5単独の実機検証は実施していない。計画どおり、Stage1（Step10-2-A、9サブタスク）完了後にまとめて実施する。

## 5. 残作業（Stage1 サブタスク6〜9）

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | 完了 |
| Step10-2-A-2 | トリガー(L2/R2)関連 | 完了 |
| Step10-2-A-3 | タッチパッド関連 | 完了 |
| Step10-2-A-4 | ジャイロ関連 | 完了 |
| Step10-2-A-5 | ライトバー・ランブル関連 | **完了（本報告書）** |
| Step10-2-A-6 | ボタン/マウス出力関連 | 未着手 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 未着手 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 未着手 |
| Step10-2-A-9 | Mapping.cs専用 | 未着手 |
