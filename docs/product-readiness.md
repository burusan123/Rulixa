# Rulixa Product Readiness

## 目的

この文書は、`Rulixa` を実運用できる品質に仕上げるためのチェックリストです。  
Phase 1 以降の実装結果を踏まえつつ、「動く」ではなく「公開して継続運用できる」状態に達しているかを確認します。

## 評価軸

- 技術
  - 互換性
  - 安定性
  - 設計一貫性
  - 観測可能性
- 品質保証
  - corpus
  - regression
  - release gate
- UX
  - pack の読みやすさ
  - unknown guidance の使いやすさ
- サポート運用
  - diagnostics
  - evidence bundle
  - 再現性

## KPI

| 指標 | 意味 | 最低基準 | 観測方法 |
|---|---|---|---|
| `pack success rate` | pack が返る割合 | required corpus で 100% | regression / smoke |
| `partial pack rate` | degraded だが有用な pack が返る割合 | failure との差分として観測 | diagnostics / artifact |
| `crash-free rate` | 例外で止まらない割合 | release gate で 100% | CI / local gate |
| `first useful map time` | 最初の有用な map が返るまでの時間 | baseline 比較で退行しない | benchmark / artifact |
| `unknown guidance hit rate` | handoff 候補が次の探索に効く割合 | advisory で継続観測 | case assertion / review |
| `false confidence rate` | 分かったふりをした割合 | required corpus で 0% | regression |
| `deterministic rate` | 同一入力で同一結果が返る割合 | required corpus で 100% | regression |

## チェックリスト

### 1. 技術

- [ ] modern WPF + DI 構成で system pack が返る
- [ ] legacy WPF + code-behind 構成で crash しない
- [ ] `App.xaml StartupUri` を root 解決に使える
- [ ] `DataContext = new XxxViewModel()` を root binding として扱える
- [ ] service locator を限定的に扱える
- [ ] `new Window()` / `ShowDialog()` を route として扱える
- [ ] ResourceDictionary / merged dictionaries で top-level failure にならない
- [ ] unsupported construct でも degraded pack と diagnostics を返せる
- [ ] false confidence を抑制できている

### 2. 品質保証

- [ ] synthetic corpus が modern / legacy / dialog-heavy / weak-signal を含む
- [ ] observed corpus が複数カテゴリで観測できる
- [ ] pack 本文の regression テストがある
- [ ] diagnostics の regression テストがある
- [ ] deterministic regression がある
- [ ] release gate が CI で実行される
- [ ] advisory 指標が artifact に残る

### 3. UX

- [ ] plugin 説明が日本語で読める
- [ ] `pack -> 必要時のみ全文検索` の導線が明確
- [ ] `entry=file` / `entry=symbol` の選び方が分かる
- [ ] system map が root entry で読める
- [ ] `unknowns` が「次に見る候補」として読める
- [ ] docs / examples が GitHub でそのまま読める

### 4. サポート運用

- [ ] diagnostics から再現に必要な情報を取れる
- [ ] evidence bundle を比較に使える
- [ ] local quality gate を開発者が毎回回せる
- [ ] GitHub Actions の required gate が運用されている
- [ ] optional smoke が observation-only として分離されている

## フェーズとの関係

- Phase 1
  基礎と evidence
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
