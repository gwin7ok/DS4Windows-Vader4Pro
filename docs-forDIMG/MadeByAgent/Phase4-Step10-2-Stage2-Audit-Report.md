# フェーズ4-Step10-2 Stage2 監査報告書

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 監査対象

- `ProfileSettingsViewModel`
- `ProfileEditor`
- `ControlService`
- `Mapping`
- `IProfileSettingsService` と DI 登録

## 2. 監査結果

- `ProfileSettingsViewModel`: B-1-1〜B-1-9 対象設定を DI 直接参照化済み。
- `ProfileEditor`: プロファイル読込・保存・手動適用を Repository／Switcher 経由へ接続済み。
- `ControlService`: 設定サービスを注入し、通知、L2/R2 設定、出力データ、ランブル、ライトバー関連の設定参照を DI 化済み。
- `Mapping`: 設定サービスを静的キャッシュし、デバウンス、通知、LS/RS、L2/R2、ジャイロ、ランブル、ライトバー、SA キャリブレーション参照を DI 化済み。
- 高頻度入力ループ内の `AppHost.GetService` 毎回解決: なし。
- 監査対象の設定系直接 `Global` 参照: なし。残存する `Global` 参照は定数、純粋関数、出力ハンドラ、デバイス状態、コメントアウトコードなど計画上の対象外。

## 3. 検証状況

- 最新の Debug x64 ビルド: 成功（警告 0、エラー 0）
- 静的エラー確認: 問題なし
- 今回監査・追加変更分のユーザー側テストビルド・テスト実行: 未実施
- 今回監査・追加変更分のコミット・リモート反映: 未実施

## 4. 残作業

1. 今回の `ControlService`／`Mapping` 追加変更を Actions／Standalone で検証する。
2. 問題がなければコミット・push する。
3. Stage2 後の実機検証を実施し、Stage1 の `△`・未実施項目を再評価する。
4. `Mapping.cs` の対象外参照を含む残存シム整理を Phase4 最終段階で検討する。
