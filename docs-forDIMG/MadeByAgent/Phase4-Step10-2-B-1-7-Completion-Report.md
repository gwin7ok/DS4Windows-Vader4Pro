# フェーズ4-Step10-2-B-1-7 完了報告書: ProfileSettingsViewModel SA・デッドゾーン設定の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` の SA・デッドゾーン設定を、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- SA ステアリング軸、範囲、ファジング、平滑化
- SX/SZ デッドゾーン、最大値、アンチデッドゾーン、感度
- SX/SZ 出力カーブとカスタムカーブ

不足していた `SXAntiDeadzone`／`SZAntiDeadzone` の契約と `ProfileSettingsService` 公開プロパティも追加した。既存の値変換、変更通知、カーブ初期化処理は維持した。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- B-1-7 対象の直接 `Global` 参照: なし

## 3. 次の作業

B-1-8「残余設定・デバイスオプション」のユーザー検証後、B-1-9「Mapping 関連」へ進む。
