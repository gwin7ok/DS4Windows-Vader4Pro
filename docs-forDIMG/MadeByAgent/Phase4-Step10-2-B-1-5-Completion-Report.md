# フェーズ4-Step10-2-B-1-5 完了報告書: ProfileSettingsViewModel ライトバー・ランブル設定の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` のライトバー・ランブル設定を、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- ライトバーのモード、メイン／低輝度／充電／点滅色
- 点滅、充電表示、レインボー設定
- ランブル強度、ランブルモーター反転、オートストップ
- DualSense ランブルエミュレーション、ハプティック出力、ランブル再スケール
- 色選択ダイアログからのライトバー色更新処理

既存の色変換、変更通知、単位変換（GUI 秒と XML ミリ秒）、設定配列の形状は維持した。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- B-1-5 対象の直接 `Global` 参照: なし

## 3. 次の作業

B-1-6「ボタン・マウス出力関連」の `ProfileSettingsViewModel` 呼び出し元 DI 直接参照化を開始する。
