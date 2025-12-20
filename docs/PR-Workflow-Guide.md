# PR ワークフローガイド

目的: ブランチ→修正→プルリクエスト（PR）→レビュー→マージの一連手順を標準化し、段階的な DI 移行作業で安全に変更を行うための手順を示す。

前提
- あなたは主にローカルで単独開発を行っている。だが PR を使うことで履歴、CI、自己レビューが得られるため推奨する。
- リポジトリは GitHub 上にホストされている想定。

1) ブランチ戦略
- `main` : 安定
- `integration` : 統合検証用（複数の機能を集めて実機検証するブランチ）
- `feature/<short-desc>` : 機能単位の作業用ブランチ（例: `feature/key-output-action`）

2) 基本フロー（1機能 = 1 PR）
- ブランチ作成
  ```bash
  git checkout -b feature/your-feature
  ```
- 実装を小さなコミットで進める（機能単位でまとめる）
- ローカルでビルドとテスト
  ```bash
  dotnet restore
  dotnet build ./DS4Windows/DS4WinWPF.csproj -c Debug
  dotnet test ./DS4WindowsTests/DS4WindowsTests.csproj --no-build
  ```
- リモートに Push
  ```bash
  git push origin feature/your-feature
  ```
- GitHub 上で Draft PR を作成（タイトル: `feature/<short-desc>: short summary`）
  - PR 本文に変更目的、影響範囲、回帰テスト手順、既知のリスクを記載

3) PR 内容のチェックリスト（PR 作成時に埋める）
- [ ] ビルドが通過する (`dotnet build`) 
- [ ] 単体テストが通る (`dotnet test`) 
- [ ] 変更は小さく、1 機能に限定されている
- [ ] 回帰テスト手順（手動）が PR に記載されている
- [ ] `docs/` への必要な更新が含まれている

4) 自己 / 他者レビュー手順
- 自己レビュー:
  - コードを俯瞰して読み直す（命名、例外処理、null 安全）
  - ログ出力やエラーケースを確認
  - 影響範囲の確認（特に `Mapping.cs` 周り）
- 他者レビューがいる場合:
  - レビュー担当者に PR を依頼する
  - 指摘を受けたら小さな修正をコミットして push する（`--no-ff` は不要）

5) CI とマージ方針
- PR は自動で CI（ビルド + テスト）を走らせることを必須にする
- マージ方式: 小規模変更は `Squash and merge`、履歴を残したい大改修は `Merge commit` を選択
- マージ前に CI が全て通過し、少なくとも自分のセルフチェックを完了させる

6) ポストマージ（統合検証）
- 小さな機能は `main` にマージしても良いが、複数機能をまとめて検証したい場合は `integration` にマージして実機プレイで検証
- 実機での検証ポイント例:
  - Key send の遅延/リピート動作が変わっていないか
  - マクロの順序・タイミングが維持されているか
  - ApplyProfile / 外部起動に副作用が出ていないか
- 問題なければ `integration` → `main` へマージ

7) 緊急修正ポリシー
- 重要なバグやクラッシュを発見した場合、`hotfix/<short-desc>` ブランチを切り、直接 `main` へ PR を作成して CI を通した上でマージする

8) PR テンプレート（推奨）
- リポジトリの `.github/PULL_REQUEST_TEMPLATE.md` に以下を置くと便利:
  - 概要、関連チケット、変更点、回帰テスト手順、スクリーンショット（必要なら）

9) 参考コマンド（頻出）
```bash
# 新しいブランチを作る
git checkout -b feature/key-output-action

# 変更をコミットして push
git add .
git commit -m "Add KeyOutputAction and tests"
git push origin feature/key-output-action

# リモートブランチを削除（不要になった場合）
git push origin --delete feature/old-branch
```

10) 自己レビュー Checklist（簡易版）
- ビルド・テストは通ったか
- 変更は最小限化されているか
- 既存機能のマニュアル回帰手順を実行したか
- PR 本文に回帰手順が明記されているか

---

保存場所: `g:/Cursor_Folder/DS4Windows-Vader4Pro/docs/PR-Workflow-Guide.md`

このガイドをベースに `.github/PULL_REQUEST_TEMPLATE.md` と簡易的な PR チェックリストを自動で追加しますか？