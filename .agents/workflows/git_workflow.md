---
description: AI向け 厳格なGitブランチ・マージのワークフロー
---

# Git ブランチ運用ワークフロー

このプロジェクトでは、**「AIエージェントがユーザーの許可なしに `main` ブランチへ変更を反映させること」を厳禁**とし、以下のワークフローを必須とします。

## 1. 作業開始時のブランチ作成
新しいIssueやタスクに取り組む際は、**必ず** `main` ブランチの最新状態から新しい作業用ブランチを作成スること。
```bash
git checkout main
git pull origin main
git checkout -b feature/issueX_description
```

## 2. 作業中のコミットとプッシュ
作業中は、切ったブランチ（`feature/...`など）に対してコミットを積み重ね、リモートの同ブランチへプッシュします。
```bash
git add .
git commit -m "feat: xxx"
git push -u origin feature/issueX_description
```

## 3. 【重要】マージの禁止
作業が完了し動作確認が終わっても、**AIエージェントが自己判断で `git checkout main && git merge feature/...` を実行してはいけません。**
AIのロールは「作業ブランチをプッシュして報告する」ところまでです。

## 4. ユーザーへの報告と承認待ち
ブランチをプッシュしたら、ユーザーに「Pushが完了したこと」「内容の確認をお願いしたいこと」を通知（`notify_user`）し、待機してください。
ユーザーから「マージして下さい」「次のIssueに行っていいよ（=マージ承認と同義）」などの**明確な許可が出た場合にのみ**、初めて `main` へのマージを行ってください。
