# Observed Corpus CI Strategy

## 背景

observed corpus は現在 observation-only で、required gate から分離されています。  
これは正しいですが、完全に人手前提のままだと継続観測が弱くなります。

## 方針

observed corpus を次の 2 層に分けます。これは人間向け出力の信頼度を分けるためでもあります。

### 1. CI 実行候補

- 環境差分が小さい
- setup が比較的安定している
- skip ではなく再現しやすい

対象カテゴリの例:

- `modern-di-root`
- `settings-report-heavy-root`

### 2. 手動観測候補

- ローカル workspace 依存が強い
- runner の権限や配置に依存する
- hosted runner での再現性が低い

対象カテゴリの例:

- `legacy-codebehind-root`
- `dialog-heavy-root`

## 実行モデル

- required gate
  - synthetic only
- observed corpus nightly / manual
  - observation-only
- self-hosted または専用環境
  - 必要になったときに導入検討

## 成功条件

- observed corpus の各カテゴリについて、`どこで回すか` が決まっている
- skip / fail / pass の意味が review 側で明確
- artifact だけ見れば、どの observed corpus がどの運用層に属するか分かる
