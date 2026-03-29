# Rulixa Product Readiness

## 位置づけ

この文書は、`Rulixa` を継続利用できる製品として仕上げるためのチェックリストです。  
単に `pack` が動くことではなく、`理解の入口を安定して返せること`、`比較・監査に使えること`、`人間向け出力まで含めて運用できること` を確認対象にします。

## 現在の到達点

- `pack` / `scan` / `resolve-entry` / `compare-evidence` がある
- local quality gate と GitHub Actions の required gate がある
- handoff / corpus / performance の advisory 指標がある
- `render-human` で `review` / `audit` / `knowledge` の Markdown を出せる
- local quality gate で `release-review.md` と synthetic root cases 向け `human-outputs/` まで揃えられる
- release review の一次資料は `summary.md`、補助資料は `release-review.md` と `human-outputs/` に固定している

## KPI

| 指標 | 意味 | 現在の扱い | 確認方法 |
|---|---|---|---|
| `pack success rate` | pack が返る割合 | required corpus で 100% を目標 | regression / smoke |
| `partial pack rate` | degraded だが有用な pack が返る割合 | advisory | diagnostics / artifact |
| `crash-free rate` | 実行中に未処理例外が出ない割合 | release gate で 100% | CI / local gate |
| `first useful map time` | 最初の有用な map が返るまでの時間 | advisory | baseline 比較 |
| `unknown guidance hit rate` | handoff 候補が次の調査に効く割合 | advisory | case review |
| `false confidence rate` | 分かったふりをした割合 | required corpus で 0% | regression |
| `deterministic rate` | 同一入力で同一結果が返る割合 | required corpus で 100% | regression |

## チェックリスト

### 1. 技術

- [ ] modern WPF + DI で system pack が安定して返る
- [ ] legacy WPF + code-behind で crash せず返る
- [ ] root entry を `App.xaml` / `DataContext` / `new Window()` から解決できる
- [ ] unsupported construct は degraded pack + diagnostics に落とせる
- [ ] false confidence を抑止できる
- [ ] `render-human` が review / audit / knowledge を生成できる

### 2. 品質保証

- [ ] synthetic corpus が modern / legacy / dialog-heavy / weak-signal を含む
- [ ] observed corpus を observation-only で継続観測できる
- [ ] required gate が CI で動く
- [ ] advisory 指標が artifact に残る
- [ ] compare-evidence の regression が維持される

### 3. UX

- [ ] `entry=file` / `entry=symbol` の選び方が docs で明確
- [ ] `pack -> 必要時のみ全文検索` の導線が分かる
- [ ] `render-human` の 3 mode の違いが README と skill で分かる
- [ ] unknown guidance の読み方が public docs で説明されている
- [ ] GitHub 上で壊れるローカル絶対パスが public docs に残っていない

### 4. サポート運用

- [ ] evidence bundle を監査の根拠として保存できる
- [ ] `summary.md` / `gate.json` / `kpi.json` が release review に使える
- [ ] `release-review.md` と `human-outputs/` を release review の補助資料として使える
- [ ] optional smoke を observation-only として運用できる
- [ ] `render-human --mode audit` を監査ドラフトとして使える
- [ ] `render-human --mode knowledge` を設計知の叩き台として使える

## フェーズとの関係

- Phase 1
  土台と evidence
- Phase 2
  高シグナル sections
- Phase 3
  system pack
- Phase 4
  legacy WPF compatibility
- Phase 5
  quality artifact と local quality gate
- Phase 6
  GitHub Actions と release gate
- Phase 7
  handoff scoring と corpus / performance 比較
- Phase 8
  human output (`render-human`)
