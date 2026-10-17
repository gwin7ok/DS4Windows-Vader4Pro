# フェーズ4-Step10-2-B-1-6 完了報告書: ProfileSettingsViewModel ボタン・マウス出力設定の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` のボタン・マウス出力設定を、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- ボタンマウス感度、垂直スケール、オフセット、マウス加速
- 絶対マウスの幅、高さ、中心、スナップ、アンチ半径
- タッチパッド切替、スクロール有効化・感度
- ダブルタップ、トラックボールモード、摩擦

既存の変更通知、値のスケール変換、設定オブジェクトへの書き込み形状は維持した。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- B-1-6 対象の直接 `Global` 参照: なし

## 3. 次の作業

B-1-7「SA・デッドゾーン関連」の `ProfileSettingsViewModel` 呼び出し元 DI 直接参照化を進める。`SXAntiDeadzone`／`SZAntiDeadzone` は現時点で `IProfileSettingsService` 契約に含まれないため、契約追加時に移行する。
