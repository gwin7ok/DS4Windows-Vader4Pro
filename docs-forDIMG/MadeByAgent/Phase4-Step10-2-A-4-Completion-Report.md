# フェーズ4-Step10-2-A-4 完了報告書: シム接続拡張（ジャイロ関連）

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク4）

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク4「ジャイロ関連」を実施した。対象は`m_Config`（`BackingStore`）へ委譲するジャイロ設定および状態更新メソッドである。

対象メンバー:

- `GyroMouseStickInf`
- `GyroMouseInfo`
- `GyroSwipeInf`
- `GyroControlsInf`
- `GyroInvert`
- `GyroSensitivity`
- `GyroSensVerticalScale`
- `GyroOutputMode`
- `GyroTriggerTurns`
- `GyroMouseStickTriggerTurns`
- `GyroMouseHorizontalAxis`
- `GyroMouseStickHorizontalAxis`
- `GyroMouseDeadZone`
- `GyroMouseToggle`
- `GyroMouseStickToggle`
- `GetGyro*`／`getGyro*`取得メソッド
- `SetGyroMouseDeadZone`、`SetGyroMouseToggle`、`SetGyroControlsToggle`、`SetGyroMouseStickToggle`

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: ジャイロ設定プロパティ、取得メソッド、状態更新メソッドの契約を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - A-1〜A-3で使用している`BackingStore _config`を継続利用。
  - ジャイロ設定を`_config`の既存配列・情報オブジェクトへ委譲。
  - ジャイロ状態更新メソッドは既存の`BackingStore`処理へ委譲し、`ControlService`への状態反映を維持。
- `DS4Windows/DS4Control/ScpUtil.cs`:
  - ジャイロ関連のGlobal公開メンバーを`ProfileSettingsServiceInstance`経由の後方互換シムへ変更。
  - 既存の配列API、取得API、`ControlService`引数付き更新APIを維持。
  - ジャイロ設定のXML読込・保存処理および既存の初期値処理は変更していない。

## 3. 外部呼び出し元への影響

`Global`の既存API形状（型、配列アクセス、取得メソッド、更新メソッド）は維持しているため、既存の呼び出し元との互換性を維持している。Stage1では呼び出し元のDI直接参照化は行わず、Stage2で別途実施する。

## 4. 検証状況

- デバッグビルド: 成功（警告0、エラー0）。
- テストビルド: 成功（Actions、Standalone）。
- テスト実行: 成功（Actions、Standalone、全件成功）。
- 変更コミット: 完了。作業ツリーはクリーン。
- A-4単独の実機検証は実施していない。計画どおり、Stage1（Step10-2-A、9サブタスク）完了後にまとめて実施する。

## 5. 残作業（Stage1 サブタスク5〜9）

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | 完了 |
| Step10-2-A-2 | トリガー(L2/R2)関連 | 完了 |
| Step10-2-A-3 | タッチパッド関連 | 完了 |
| Step10-2-A-4 | ジャイロ関連 | **完了（本報告書）** |
| Step10-2-A-5 | ライトバー・ランブル関連 | 未着手 |
| Step10-2-A-6 | ボタン/マウス出力関連 | 未着手 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 未着手 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 未着手 |
| Step10-2-A-9 | Mapping.cs専用 | 未着手 |
