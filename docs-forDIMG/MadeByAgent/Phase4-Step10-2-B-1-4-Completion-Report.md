# フェーズ4-Step10-2-B-1-4 完了報告書: ProfileSettingsViewModel ジャイロ設定の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` のジャイロ設定・取得・更新処理を、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- ジャイロ出力モード、感度、反転、トリガー条件
- ジャイロマウス／ジャイロスティックのデッドゾーン、出力先、平滑化、ジッター補正
- ジャイロコントロール／スワイプの条件、軸、デッドゾーン、遅延、トリガー一覧
- ジャイロ設定の初期化・補助検索処理

`ProfileSettingsViewModel(int, IProfileSettingsService)` の互換フォールバックと既存の `App.rootHub` を使用するデバイス反映処理は維持した。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- ジャイロ対象の直接 `Global` 参照: なし

## 3. 次の作業

B-1-5「ライトバー・ランブル関連」の `ProfileSettingsViewModel` 呼び出し元 DI 直接参照化を開始する。
