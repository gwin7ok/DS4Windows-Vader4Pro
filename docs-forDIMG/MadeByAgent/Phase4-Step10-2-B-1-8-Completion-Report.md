# フェーズ4-Step10-2-B-1-8 完了報告書: ProfileSettingsViewModel 残余設定・デバイスオプションの DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` の残余設定・デバイスオプションを、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- 左右スティックのドリフト補正
- DS3 ピッチ／ロールシミュレーション
- DS4 Mapping と DS4 出力データ
- DInput 専用モード
- アイドル切断の有効化・時間
- Bluetooth ポーリングレートの初期値取得

既存の変更通知、既定値、単位変換、互換用のコメントアウトコードは維持した。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- B-1-8 対象の直接 `Global` 参照: なし（コメントアウト行を除く）

## 3. 次の作業

B-1-9「Mapping 関連」の `ProfileSettingsViewModel` 呼び出し元 DI 直接参照化を進める。
